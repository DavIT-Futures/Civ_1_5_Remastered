using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Civilization1Original;
using Civilization1Original.Containers;
using Civilization1Original.Emulation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Civilization1Original.Emulation
{
	public partial class CPU : IStackProbe
	{
		// state
		private bool breakActive = false;

		// flags
		private readonly CPUFlags markers = new CPUFlags();

		// registers
		private readonly CPURegister regAcc = new CPURegister();
		private readonly CPURegister regBase = new CPURegister();
		private readonly CPURegister regCount = new CPURegister();
		private readonly CPURegister regData = new CPURegister();
		private readonly CPURegister regFrame = new CPURegister();
		private readonly CPURegister regStack = new CPURegister();
		private readonly CPURegister regSource = new CPURegister();
		private readonly CPURegister regDest = new CPURegister();
		private readonly CPURegister segData = new CPURegister();
		private readonly CPURegister segExtra = new CPURegister();
		private readonly CPURegister segStack = new CPURegister();
		private readonly CPURegister segCode = new CPURegister();

		// temporary registers
		private readonly CPURegister scratch = new CPURegister();

		// memory
		private readonly CPUMemory storage;

		// Timer(s)
		private bool allowSignal = true;
		private bool allowClock = false;
		private bool insideClock = false;
		private volatile bool appQuit = false;
		// Timer interrupts that are due and not yet run by DoEvents, counted by the timer thread
		private int waitingClockBeats = 0;
		// Timer chip divisor, read from the game memory by the game thread (0x4dae is 60 interrupts per second)
		private volatile int clockDenominator = 0x4dae;
		private readonly Thread clockWorker;
		// Test seam: when not 0, the timer interrupts are counted from the memory accesses of the game instead of the real time
		private int fixedClockSpan = 0;
		private int fixedClockSteps = 0;

		// The game programs the timer chip (8253) with this divisor, normally for 60 interrupts per second
		private const ushort ClockContentSection = 0x3b01;
		private const ushort ClockDenominatorDisplacement = 0x48;
		private const double ClockCircuitHertz = 1193181.667;
		// When the game is busy for a while, it catches up at most this many timer interrupts
		private const int PeakWaitingClockBeats = 6;

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint RequestTimerResolution(uint interval);

		// Mouse
		private Point pointerSite = new Point(-1, -1);
		private ushort pointerClicks = 0;

		// DOS file stuff
		private uint dta = 0;
		private int documentTally = 0;
		private readonly FileStream?[] documentGrips = new FileStream?[256];

		private string standardFolder;
		private short documentHndTally = 0x20;
		private readonly BDictionary<short, FileStreamItem> accessDocuments = new BDictionary<short, FileStreamItem>();

		private readonly IMachineHost host;
		private LogWrapper journal;

		public CPU(IMachineHost host, LogWrapper journal)
		{
			this.host = host;
			this.journal = journal;
			this.storage = new CPUMemory(this);
			this.clockWorker = new Thread(ClockWorker) { IsBackground = true, Name = "Timer", Priority = ThreadPriority.AboveNormal };
			this.clockWorker.Start();

			for (int idx = 0; idx < this.documentGrips.Length; idx++)
			{
				this.documentGrips[idx] = null;
			}

			// We need this because of different paths between Debug and Release versions
			// The Release executable is intended to be put directly into CIV 1 directory
			// Also, for Debug there are different platforms and paths
#if DEBUG
			if (Environment.OSVersion.Platform != PlatformID.Unix)
			{
				this.standardFolder = string.Format("C:{0}Dos{0}Civ1{0}", Path.DirectorySeparatorChar);
			}
			else
			{
				this.standardFolder = string.Format("{0}{1}Dos{1}Civ1{1}", 
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Path.DirectorySeparatorChar);
			}
#else
			this.standardFolder = "";
#endif

			// If the original game is in a "CIVILIZATION" folder next to the executable
			// or in any parent folder (for example in the repository root), use it
			string? matchFolder = DosPaths.LocateGameDirectory("CIVILIZATION");
			if (matchFolder != null)
			{
				this.standardFolder = matchFolder;
			}
		}

		ushort IStackProbe.StackPointer { get { return this.regStack.Value16; } }
		ushort IStackProbe.StackSegment { get { return this.segStack.Value16; } }
		ushort IStackProbe.DataSegment { get { return this.segData.Value16; } }
		ushort IStackProbe.FramePointer { get { return this.regFrame.Value16; } }

		public LogWrapper Journal
		{
			get { return this.journal; }
			set { this.journal = value; }
		}

		public bool AllowClock
		{
			get { return this.allowClock; }
			set { this.allowClock = value; this.waitingClockBeats = 0; }
		}

		/// <summary>
		/// Test seam for runs without a window that must be reproducible: when not 0, a timer interrupt is due after this many
		/// memory accesses of the game instead of after the real time, and the timer thread stops counting.
		/// </summary>
		public int FixedClockSpan
		{
			get { return this.fixedClockSpan; }
			set { this.fixedClockSpan = value; this.fixedClockSteps = 0; this.waitingClockBeats = 0; }
		}

		/// <summary>
		/// Test seam: called when the game looks for a key and none is waiting. The argument is true when the game waits for the key.
		/// A scripted host puts the next key into the input queue here.
		/// </summary>
		public Action<bool>? InputStarved { get; set; }

		/// <summary>
		/// The game waits for the timer. With the real time clock this sleeps a bit, with the fixed clock (FixedClockSpan)
		/// the next timer interrupt is due at once, so the waits take no time and don't depend on the speed of the computer.
		/// </summary>
		public void Idle()
		{
			if (this.fixedClockSpan != 0)
			{
				if (this.allowClock)
				{
					this.fixedClockSteps = 0;
					this.waitingClockBeats = 1;
				}
			}
			else
			{
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Stops the timer thread, for a game that is dropped without leaving through Quit
		/// </summary>
		public void Shutdown()
		{
			this.appQuit = true;
		}

		public bool Hold
		{
			get { return this.breakActive; }
			set { this.breakActive = value; }
		}

		/// <summary>
		/// Counts the timer interrupts at the rate the game programmed into the timer chip, like the PC timer does.
		/// The game waits and animates in units of these interrupts (normally 1/60 s), so the rate must be exact.
		/// </summary>
		private void ClockWorker()
		{
			// The default Windows timer resolution is 15.6 ms, too coarse for 60 interrupts per second.
			// This also makes the waits of the game (Thread.Sleep) more precise.
			if (OperatingSystem.IsWindows())
			{
				RequestTimerResolution(1);
			}

			Stopwatch timer = Stopwatch.StartNew();
			double followingBeat = 0.0;

			while (!this.appQuit)
			{
				double present = timer.Elapsed.TotalSeconds;

				if (present < followingBeat)
				{
					Thread.Sleep(1);
					continue;
				}

				if (this.fixedClockSpan == 0 && this.allowClock && Volatile.Read(ref this.waitingClockBeats) < PeakWaitingClockBeats)
				{
					Interlocked.Increment(ref this.waitingClockBeats);
				}

				followingBeat += this.clockDenominator / ClockCircuitHertz;

				// After a long pause (computer sleep, debugger) continue from now
				if (present - followingBeat > 0.5)
				{
					followingBeat = present;
				}
			}
		}

		#region Interrupts and events
		public void EnableInterrupts()
		{
			this.allowSignal = true;
		}

		public void DisableInterrupts()
		{
			this.allowSignal = false;
		}

		/// <summary>
		/// When set, called every 0x40000 memory reads (the tests check the time of a scenario in it: endless loops that call
		/// no game function fail then too). It must not change the state of the game.
		/// </summary>
		public Action? Heartbeat { get; set; }

		private int heartbeatSteps;

		public void PumpMessages()
		{
			if (this.Heartbeat != null && ++this.heartbeatSteps >= 0x40000)
			{
				this.heartbeatSteps = 0;
				this.Heartbeat();
			}

			if (this.appQuit)
			{
				throw new ApplicationExitException();
			}

			while (this.breakActive)
			{
				if (this.appQuit)
				{
					throw new ApplicationExitException();
				}

				Thread.Sleep(200);
			}

			if (this.fixedClockSpan != 0 && this.allowClock && ++this.fixedClockSteps >= this.fixedClockSpan)
			{
				this.fixedClockSteps = 0;
				this.waitingClockBeats = 1;
			}

			if (this.allowSignal && this.allowClock && Volatile.Read(ref this.waitingClockBeats) > 0 && !this.insideClock)
			{
				this.insideClock = true;
				Interlocked.Decrement(ref this.waitingClockBeats);

				if (this.pointerSite != this.host.PointerSite ||
					this.pointerClicks != this.host.PointerClicks)
				{
					if (this.pointerClicks != this.host.PointerClicks)
					{
						this.journal.EmitLine($"Mouse buttons changed from 0x{this.pointerClicks:x2} to 0x{this.host.PointerClicks:x2}");
					}

					// The interrupted registers are kept outside the emulated stack: what an interrupt leaves below SP would
					// depend on when it comes, and the game reads uninitialized locals there (ShiftAIUnit)
					(ushort acc, ushort count, ushort data, ushort basis, ushort frame, ushort source, ushort dest, ushort dataSection,
						ushort extraSection, ushort flags) = (this.regAcc.Value16, this.regCount.Value16, this.regData.Value16,
						this.regBase.Value16, this.regFrame.Value16, this.regSource.Value16, this.regDest.Value16, this.segData.Value16,
						this.segExtra.Value16, this.markers.Datum);

					this.regAcc.Value16 = 0;
					if (this.pointerSite != this.host.PointerSite)
					{
						this.regAcc.Value16 |= 1;
					}
					this.regCount.Value16 = (ushort)this.host.PointerSite.X;
					this.regData.Value16 = (ushort)this.host.PointerSite.Y;

					this.regBase.Value16 = 0;
					ushort remainingShort = (ushort)(this.host.PointerClicks & 1);
					this.regBase.Value16 |= remainingShort;
					if ((this.pointerClicks & 1) != remainingShort)
					{
						if (remainingShort != 0)
							this.regAcc.Value16 |= 2;
						else
							this.regAcc.Value16 |= 4;
					}

					ushort eastShort = (ushort)(this.host.PointerClicks & 2);
					this.regBase.Value16 |= eastShort;
					if ((this.pointerClicks & 2) != eastShort)
					{
						if (eastShort != 0)
							this.regAcc.Value16 |= 8;
						else
							this.regAcc.Value16 |= 0x10;
					}

					this.pointerSite = this.host.PointerSite;
					this.pointerClicks = this.host.PointerClicks;

					this.host.OnPointerEvent();

					(this.regAcc.Value16, this.regCount.Value16, this.regData.Value16, this.regBase.Value16, this.regFrame.Value16,
						this.regSource.Value16, this.regDest.Value16, this.segData.Value16, this.segExtra.Value16, this.markers.Datum) =
						(acc, count, data, basis, frame, source, dest, dataSection, extraSection, flags);
				}

				LogWrapper journalScratch = this.journal;
				this.journal = this.host.IrqJournal;

				// The flags are kept outside the emulated stack, like the registers in ClockInterrupt
				ushort interruptedFlags = this.markers.Datum;
				this.host.OnClockInterrupt();
				this.markers.Datum = interruptedFlags;

				// The timer function changes the divisor when the game changes the timer speed
				int splitter = this.storage.PullUInt16(ClockContentSection, ClockDenominatorDisplacement);
				this.clockDenominator = splitter == 0 ? 0x10000 : Math.Max(splitter, 1193);

				this.journal = journalScratch;

				this.insideClock = false;
			}
		}
		#endregion

		#region Flags & registers
		public CPUFlags FlagSet
		{
			get { return this.markers; }
		}

		public CPURegister RegAcc
		{
			get { return this.regAcc; }
		}

		public CPURegister RegBase
		{
			get { return this.regBase; }
		}

		public CPURegister RegCount
		{
			get { return this.regCount; }
		}

		public CPURegister RegData
		{
			get { return this.regData; }
		}

		public CPURegister RegFrame
		{
			get { return this.regFrame; }
		}

		public CPURegister RegStack
		{
			get { return this.regStack; }
		}

		public CPURegister RegSource
		{
			get { return this.regSource; }
		}

		public CPURegister RegDest
		{
			get { return this.regDest; }
		}

		public CPURegister SegData
		{
			get { return this.segData; }
		}

		public CPURegister SegExtra
		{
			get { return this.segExtra; }
		}

		public CPURegister SegStack
		{
			get { return this.segStack; }
		}

		public CPURegister SegCode
		{
			get { return this.segCode; }
		}

		public CPURegister Scratch
		{
			get { return this.scratch; }
		}
		#endregion

		#region Helper functions
		public void SplitDoubleWord(CPURegister cellLower, CPURegister cellUpper, uint amount)
		{
			cellLower.Value16 = (ushort)(amount & 0xffff);
			cellUpper.Value16 = (ushort)((amount & 0xffff0000) >> 16);
		}

		public uint JoinDoubleWord(ushort lowerAmount, ushort upperAmount)
		{
			return ((uint)upperAmount << 16) | (uint)lowerAmount;
		}

		#endregion

		#region Memory
		public CPUMemory Storage
		{
			get { return this.storage; }
		}

		public byte PullUInt8(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullUInt8(section, displacement);
		}

		public sbyte PullInt8(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullInt8(section, displacement);
		}

		public ushort PullUInt16(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullUInt16(section, displacement);
		}

		public short PullInt16(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullInt16(section, displacement);
		}

		public uint PullUInt32(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullUInt32(section, displacement);
		}

		public int PullInt32(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullInt32(section, displacement);
		}

		public void EmitUInt8(ushort section, ushort displacement, byte amount)
		{
			this.PumpMessages();
			this.storage.EmitUInt8(section, displacement, amount);
		}

		public void EmitInt8(ushort section, ushort displacement, sbyte amount)
		{
			this.PumpMessages();
			this.storage.EmitInt8(section, displacement, amount);
		}

		public void EmitUInt16(ushort section, ushort displacement, ushort amount)
		{
			this.PumpMessages();
			this.storage.EmitUInt16(section, displacement, amount);
		}

		public void EmitInt16(ushort section, ushort displacement, short amount)
		{
			this.PumpMessages();
			this.storage.EmitInt16(section, displacement, amount);
		}

		public void EmitUInt32(ushort section, ushort displacement, uint amount)
		{
			this.PumpMessages();
			this.storage.EmitUInt32(section, displacement, amount);
		}

		public void EmitInt32(ushort section, ushort displacement, int amount)
		{
			this.PumpMessages();
			this.storage.EmitInt32(section, displacement, amount);
		}

		public static uint AsLinearAddress(ushort section, ushort displacement)
		{
			// 1MB limit!
			return ((uint)((uint)section << 4) + (uint)displacement) & 0xfffff;
		}

		public static void SnapToSegment(ref uint location)
		{
			if ((location & 0xf) != 0)
			{
				location &= 0xffff0;
				location += 0x10;
			}
		}
		#endregion

		#region String instructions
		public string PullText(uint location)
		{
			if (location == 0)
				return "";

			StringBuilder sbyteAlt = new StringBuilder();
			byte chr;

			while ((chr = this.storage.PullUInt8(location)) != 0)
			{
				sbyteAlt.Append((char)chr);
				location++;
			}

			return sbyteAlt.ToString();
		}

		public string PullDosText(uint location)
		{
			if (location == 0)
				return "";

			StringBuilder sbyteAlt = new StringBuilder();
			byte chr;

			while ((chr = this.storage.PullUInt8(location)) != (byte)'$')
			{
				sbyteAlt.Append((char)chr);
				location++;
			}

			return sbyteAlt.ToString();
		}

		public void EmitText(uint location, string words, int peakSpan)
		{
			if (location == 0)
				return;

			for (int idx = 0; idx < words.Length && idx < peakSpan; idx++)
			{
				this.storage.EmitUInt8(location, (byte)words[idx]);
				location++;
			}

			this.storage.EmitUInt8(location, 0);
		}
		#endregion

		#region File operations
		public string StandardFolder
		{
			get { return this.standardFolder; }
			set { this.standardFolder = value; }
		}

		public short DocumentHndTally
		{
			get { return this.documentHndTally; }
			set { this.documentHndTally = value; }
		}

		public BDictionary<short, FileStreamItem> Documents
		{
			get { return this.accessDocuments; }
		}
		#endregion

		#region CPU stack
		public void StackAllWords(CPURegister argAcc, CPURegister argCount, CPURegister argData, CPURegister argBase,
			CPURegister argSource, CPURegister argDest)
		{
			ushort scratchUint = this.regStack.Value16;
			this.StackWord(argAcc.Value16);
			this.StackWord(argCount.Value16);
			this.StackWord(argData.Value16);
			this.StackWord(argBase.Value16);
			this.StackWord(scratchUint);
			this.StackWord(this.regFrame.Value16);
			this.StackWord(argSource.Value16);
			this.StackWord(argDest.Value16);
		}

		public void StackFlags()
		{
			StackWord(this.markers.Datum);
		}

		public void StackWord(ushort amount)
		{
			if ((int)this.regStack.Value16 - 2 < 0)
			{
				throw new Exception("Stack overflow");
			}
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)((amount & 0xff00) >> 8));
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)(amount & 0xff));
		}

		public void StackDoubleWord(uint amount)
		{
			if ((int)this.regStack.Value16 - 4 < 0)
			{
				throw new Exception("Stack overflow");
			}
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)((amount & 0xff000000) >> 24));
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)((amount & 0xff0000) >> 16));
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)((amount & 0xff00) >> 8));
			this.regStack.Value16 -= 1;
			this.storage.EmitUInt8(this.segStack.Value16, this.regStack.Value16, (byte)(amount & 0xff));
		}

		public void UnstackAllWords(CPURegister argAcc, CPURegister argCount, CPURegister argData, CPURegister argBase,
			CPURegister argSource, CPURegister argDest)
		{
			argDest.Value16 = this.UnstackWord();
			argSource.Value16 = this.UnstackWord();
			this.regFrame.Value16 = this.UnstackWord();
			this.UnstackWord();
			argBase.Value16 = this.UnstackWord();
			argData.Value16 = this.UnstackWord();
			argCount.Value16 = this.UnstackWord();
			argAcc.Value16 = this.UnstackWord();
		}

		public void UnstackFlags()
		{
			this.markers.Datum = UnstackWord();
		}

		public ushort UnstackWord()
		{
			if ((int)this.regStack.Value16 + 2 >= 0x10000)
			{
				throw new Exception("Stack underflow");
			}

			return (ushort)((ushort)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++) |
				((ushort)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++) << 8));
		}

		public uint UnstackDoubleWord()
		{
			if ((int)this.regStack.Value16 + 4 >= 0x10000)
			{
				throw new Exception("Stack underflow");
			}

			return (uint)(((uint)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++)) |
				((uint)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++) << 8) |
				((uint)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++) << 16) |
				((uint)this.storage.PullUInt8(this.segStack.Value16, this.regStack.Value16++) << 24));
		}
		#endregion

		#region CPU instructions
		public byte SumCarryByte(byte amount1, byte amount2)
		{
			byte chrMarker = (byte)((this.markers.IsCarry) ? 1 : 0);
			byte ret = (byte)(amount1 + amount2 + chrMarker);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (ret < amount1) || (chrMarker != 0 && (ret == amount1));
			this.markers.IsOverflow = (((amount1 ^ amount2 ^ 0x80) & (ret ^ amount2)) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort SumCarryWord(ushort amount1, ushort amount2)
		{
			ushort chrMarker = (ushort)((this.markers.IsCarry) ? 1 : 0);
			ushort ret = (ushort)(amount1 + amount2 + chrMarker);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (ret < amount1) || (chrMarker != 0 && (ret == amount1));
			this.markers.IsOverflow = (((amount1 ^ amount2 ^ 0x8000) & (ret ^ amount2)) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public byte SumByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 + amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (ret < amount1);
			this.markers.IsOverflow = (((amount1 ^ amount2 ^ 0x80) & (ret ^ amount2)) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort SumWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 + amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (ret < amount1);
			this.markers.IsOverflow = (((amount1 ^ amount2 ^ 0x8000) & (ret ^ amount2)) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public byte BitAndByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 & amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort BitAndWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 & amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public void CompareByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 - amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < amount2);
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);
		}

		public void CompareWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 - amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < amount2);
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);
		}

		public void CompareStringByte(CPURegister argExtra, CPURegister argDest, CPURegister cell, CPURegister argSource)
		{
			CompareByte(this.storage.PullUInt8(argExtra.Value16, argDest.Value16), this.storage.PullUInt8(cell.Value16, argSource.Value16));

			if (this.markers.IsBackward)
			{
				this.regSource.Value16--;
				this.regDest.Value16--;
			}
			else
			{
				this.regSource.Value16++;
				this.regDest.Value16++;
			}
		}

		public void RepeatCompareStringByte(CPURegister argExtra, CPURegister argDest, CPURegister cell, CPURegister argSource)
		{
			while (this.regCount.Value16 != 0)
			{
				CompareStringByte(argExtra, argDest, cell, argSource);
				this.regCount.Value16--;

				if (this.markers.IsZero)
					break;
			}
		}

		public void SignExtendByte(CPURegister argAcc)
		{
			if ((argAcc.LowByte & 0x80) != 0)
				argAcc.HighByte = 0xff;
			else
				argAcc.HighByte = 0;
		}

		public void SignExtendWord(CPURegister argAcc, CPURegister argData)
		{
			if ((argAcc.Value16 & 0x8000) != 0)
				argData.Value16 = 0xffff;
			else
				argData.Value16 = 0;
		}

		public void AdjustAfterSubtract()
		{
			byte signedvalue = (byte)(this.regAcc.LowByte & 0x80);
			if (((this.regAcc.LowByte & 0x0f) > 9))
			{
				if ((this.regAcc.LowByte > 0x99) || this.markers.IsCarry)
				{
					this.regAcc.LowByte -= 0x60;
					this.markers.IsCarry = true;
				}
				else
				{
					this.markers.IsCarry = this.regAcc.LowByte <= 0x05;
				}
				this.regAcc.LowByte -= 6;
			}
			else
			{
				if ((this.regAcc.LowByte > 0x99) || this.markers.IsCarry)
				{
					this.regAcc.LowByte -= 0x60;
					this.markers.IsCarry = true;
				}
				else
				{
					this.markers.IsCarry = false;
				}
			}
			this.markers.IsOverflow = signedvalue != 0 && (this.regAcc.LowByte & 0x80) == 0;
			this.markers.IsSigned = (this.regAcc.LowByte & 0x80) != 0;
			this.markers.IsZero = this.regAcc.LowByte == 0;
		}

		public byte DecrementByte(byte amount1)
		{
			byte ret = (byte)(amount1 - 1);
			// Modifies flags: AF OF PF SF ZF
			this.markers.IsOverflow = (ret == 0x7f);
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort DecrementWord(ushort amount1)
		{
			ushort ret = (ushort)(amount1 - 1);
			// Modifies flags: AF OF PF SF ZF
			this.markers.IsOverflow = (ret == 0x7fff);
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public void DivideWord(CPURegister argAcc, CPURegister argData, ushort amount)
		{
			if (amount == 0)
				throw new Exception("Division by zero");
			uint nr = ((uint)argData.Value16 << 16) | (uint)argAcc.Value16;
			uint quot = nr / amount;
			ushort remnant = (ushort)(nr % amount);
			ushort quot16 = (ushort)(quot & 0xffff);
			if (quot != quot16)
				throw new Exception("Division error");
			argData.Value16 = remnant;
			argAcc.Value16 = quot16;
		}

		public void DivideSignedByte(CPURegister argAcc, byte amount)
		{
			if (amount == 0)
				throw new Exception("Division by zero");
			short quantitya = (sbyte)amount;
			short nr = (short)argAcc.Value16;
			short quot = (short)(nr / quantitya);
			sbyte remnant = (sbyte)(nr % quantitya);
			sbyte quot8 = (sbyte)((ushort)quot & 0xff);
			if (quot != quot8)
				throw new Exception("Division error");
			argAcc.HighByte = (byte)remnant;
			argAcc.LowByte = (byte)quot8;
		}

		public void DivideSignedWord(CPURegister argAcc, CPURegister argData, ushort amount)
		{
			if (amount == 0)
				throw new Exception("Division by zero");
			int quantitya = (short)amount;
			int nr = (int)(((uint)argData.Value16 << 16) | (uint)argAcc.Value16);
			int quot = nr / quantitya;
			short remnant = (short)(nr % quantitya);
			short quot16 = (short)((uint)quot & 0xffff);
			if (quot != quot16)
				throw new Exception("Division error");
			argData.Value16 = (ushort)remnant;
			argAcc.Value16 = (ushort)quot16;
		}

		public void MultiplySignedByte(CPURegister argAcc, byte amount1)
		{
			ushort ret = (ushort)((short)((sbyte)argAcc.LowByte) * (short)((sbyte)amount1));
			argAcc.Value16 = ret;

			if ((ret & 0xff80) == 0xff80 || (ret & 0xff80) == 0x0)
			{
				this.markers.IsCarry = false;
				this.markers.IsOverflow = false;
			}
			else
			{
				this.markers.IsCarry = true;
				this.markers.IsOverflow = true;
			}
		}

		public void MultiplySignedWord(CPURegister argAcc, CPURegister argData, ushort amount1)
		{
			uint ret = (uint)((int)((short)argAcc.Value16) * (int)((short)amount1));
			argAcc.Value16 = (ushort)(ret & 0xffff);
			argData.Value16 = (ushort)((ret & 0xffff0000) >> 16);

			if ((ret & 0xffff8000) == 0xffff8000 || (ret & 0xffff8000) == 0x0)
			{
				this.markers.IsCarry = false;
				this.markers.IsOverflow = false;
			}
			else
			{
				this.markers.IsCarry = true;
				this.markers.IsOverflow = true;
			}
		}

		public ushort MultiplySignedShortWord(ushort amount1, ushort amount2)
		{
			uint ret = (uint)((uint)amount1 * (uint)amount2);

			if ((ret & 0xffff8000) == 0xffff8000 || (ret & 0xffff8000) == 0x0)
			{
				this.markers.IsCarry = false;
				this.markers.IsOverflow = false;
			}
			else
			{
				this.markers.IsCarry = true;
				this.markers.IsOverflow = true;
			}

			return (ushort)(ret & 0xffff);
		}

		public byte IncrementByte(byte amount1)
		{
			byte ret = (byte)(amount1 + 1);
			// Modifies flags: AF OF PF SF ZF
			this.markers.IsOverflow = (ret == 0x80);
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort IncrementWord(ushort amount1)
		{
			ushort ret = (ushort)(amount1 + 1);
			// Modifies flags: AF OF PF SF ZF
			this.markers.IsOverflow = (ret == 0x8000);
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public bool RepeatLoop(CPURegister argCount)
		{
			argCount.Value16--;

			return argCount.Value16 != 0;
		}

		/// <summary>
		/// LODS - Load String (Byte, Word or Double) from DS:SI
		/// </summary>
		public void LoadStringByte()
		{
			this.regAcc.LowByte = storage.PullUInt8(this.segData.Value16, this.regSource.Value16);

			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				this.regSource.Value16--;
			}
			else
			{
				this.regSource.Value16++;
			}
		}

		/// <summary>
		/// LODS - Load String (Byte, Word or Double) from DS:SI
		/// </summary>
		public void LoadStringWord()
		{
			this.regAcc.Value16 = storage.PullUInt16(this.segData.Value16, this.regSource.Value16);

			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				this.regSource.Value16 -= 2;
			}
			else
			{
				this.regSource.Value16 += 2;
			}
		}

		public ushort CopySignedWord(ushort amount1)
		{
			amount1 &= 0xff;
			if ((amount1 & 0x80) != 0)
				return (ushort)(0xff00 | amount1);

			return amount1;
		}

		public uint CopySignedDoubleWord(uint amount1)
		{
			amount1 &= 0xffff;
			if ((amount1 & 0x8000) != 0)
				return 0xffff0000 | amount1;

			return amount1;
		}

		public ushort CopyUnsignedWord(ushort amount1)
		{
			return (ushort)(amount1 & 0xff);
		}

		public uint CopyUnsignedDoubleWord(uint amount1)
		{
			return amount1 & 0xffff;
		}

		/// <summary>
		/// MOVS - Move String (Byte or Word) ES:DI = DS:SI
		/// </summary>
		public void CopyStringByte(CPURegister cell, CPURegister argSource, CPURegister argExtra, CPURegister argDest)
		{
			byte ret = storage.PullUInt8(cell.Value16, argSource.Value16);
			storage.EmitUInt8(argExtra.Value16, argDest.Value16, ret);
			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				argSource.Value16--;
				argDest.Value16--;
			}
			else
			{
				argSource.Value16++;
				argDest.Value16++;
			}
		}

		/// <summary>
		/// MOVS - Move String (Byte or Word) ES:DI = DS:SI
		/// </summary>
		public void RepeatCopyStringByte(CPURegister cell, CPURegister argSource, CPURegister argExtra, CPURegister argDest, CPURegister argCount)
		{
			while (argCount.Value16 != 0)
			{
				CopyStringByte(cell, argSource, argExtra, argDest);

				argCount.Value16--;
			}
		}

		/// <summary>
		/// MOVS - Move String (Byte or Word) ES:DI = DS:SI
		/// </summary>
		public void CopyStringWord(CPURegister cell, CPURegister argSource, CPURegister argExtra, CPURegister argDest)
		{
			storage.EmitUInt16(argExtra.Value16, argDest.Value16, storage.PullUInt16(cell.Value16, argSource.Value16));
			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				argSource.Value16 -= 2;
				argDest.Value16 -= 2;
			}
			else
			{
				argSource.Value16 += 2;
				argDest.Value16 += 2;
			}
		}

		/// <summary>
		/// MOVS - Move String (Byte or Word) ES:DI = DS:SI
		/// </summary>
		public void RepeatCopyStringWord(CPURegister cell, CPURegister argSource, CPURegister argExtra, CPURegister argDest, CPURegister argCount)
		{
			while (argCount.Value16 != 0)
			{
				CopyStringWord(cell, argSource, argExtra, argDest);

				argCount.Value16--;
			}
		}

		public void MultiplyByte(CPURegister argAcc, byte amount)
		{
			argAcc.Value16 = (ushort)((ushort)argAcc.LowByte * (ushort)amount);
			markers.IsZero = argAcc.LowByte == 0;
			if (argAcc.HighByte != 0)
			{
				markers.IsCarry = true;
				markers.IsOverflow = true;
			}
			else
			{
				markers.IsCarry = false;
				markers.IsOverflow = false;
			}
		}

		public void MultiplyWord(CPURegister argData, CPURegister argAcc, ushort amount)
		{
			uint tmpu = (uint)argAcc.Value16 * (uint)amount;
			argAcc.Value16 = (ushort)(tmpu & 0xfffful);
			argData.Value16 = (ushort)((tmpu & 0xffff0000ul) >> 16);
			markers.IsZero = argAcc.Value16 == 0;
			if (argData.Value16 != 0)
			{
				markers.IsCarry = true;
				markers.IsOverflow = true;
			}
			else
			{
				markers.IsCarry = false;
				markers.IsOverflow = false;
			}
		}

		public byte NegateByte(byte amount1)
		{
			byte ret = (byte)(((ushort)0x100 - (ushort)amount1) & 0xff);

			this.markers.IsCarry = amount1 != 0;
			this.markers.IsZero = ret == 0;
			this.markers.IsSigned = (amount1 & 0x80) != 0;
			this.markers.IsOverflow = (amount1 == 0x80);

			return ret;
		}

		public ushort NegateWord(ushort amount1)
		{
			ushort ret = (ushort)(((uint)0x10000 - (uint)amount1) & 0xffff);

			this.markers.IsCarry = amount1 != 0;
			this.markers.IsZero = ret == 0;
			this.markers.IsSigned = (amount1 & 0x8000) != 0;
			this.markers.IsOverflow = (amount1 == 0x8000);

			return ret;
		}

		public byte InvertByte(byte amount1)
		{
			byte ret = (byte)(~amount1);
			// Modifies flags: None

			return ret;
		}

		public ushort InvertWord(ushort amount1)
		{
			ushort ret = (ushort)(~amount1);
			// Modifies flags: None

			return ret;
		}

		public byte BitOrByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 | amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort BitOrWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 | amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public byte RotateCarryLeftByte(byte amount1, byte amount2)
		{
			if ((amount2 % 9) == 0) return amount1;
			amount2 %= 9;
			byte carry = (byte)((markers.IsCarry) ? 1 : 0);
			byte ret = (byte)((amount1 << amount2) |
				(carry << (amount2 - 1)) |
				(amount1 >> (9 - amount2)));
			markers.IsOverflow = ((markers.IsCarry ? 1 : 0) ^ (ret >> 7)) != 0;
			markers.IsCarry = (((amount1 >> (8 - amount2)) & 1)) != 0;

			return ret;
		}

		public ushort RotateCarryLeftWord(ushort amount1, byte amount2)
		{
			if ((amount2 % 17) == 0) return amount1;
			amount2 %= 17;
			ushort carry = (ushort)((markers.IsCarry) ? 1 : 0);
			ushort ret = (ushort)((amount1 << amount2) |
				(carry << (amount2 - 1)) |
				(amount1 >> (17 - amount2)));
			markers.IsOverflow = ((markers.IsCarry ? 1 : 0) ^ (ret >> 15)) != 0;
			markers.IsCarry = (((amount1 >> (16 - amount2)) & 1)) != 0;

			return ret;
		}

		public byte RotateCarryRightByte(byte amount1, byte amount2)
		{
			if ((amount2 % 9) == 0) return amount1;
			amount2 %= 9;
			byte carry = (markers.IsCarry) ? (byte)1 : (byte)0;
			byte ret = (byte)((amount1 >> amount2) |
				(carry << (8 - amount2)) |
				(amount1 << (9 - amount2)));

			this.markers.IsCarry = ((amount1 >> (amount2 - 1)) & 1) != 0;
			this.markers.IsOverflow = ((ret ^ (ret << 1)) & 0x80) != 0;

			return ret;
		}

		public ushort RotateCarryRightWord(ushort amount1, ushort amount2)
		{
			if ((amount2 % 17) == 0) return amount1;
			amount2 %= 17;
			ushort carry = (markers.IsCarry) ? (ushort)1 : (ushort)0;
			ushort ret = (ushort)((amount1 >> amount2) |
				(carry << (16 - amount2)) |
				(amount1 << (17 - amount2)));

			this.markers.IsCarry = ((amount1 >> (amount2 - 1)) & 1) != 0;
			this.markers.IsOverflow = ((ret ^ (ret << 1)) & 0x8000) != 0;

			return ret;
		}

		public byte RotateLeftByte(byte amount1, byte amount2)
		{
			if ((amount2 & 0x7) == 0)
			{
				if ((amount2 & 0x18) != 0)
				{
					markers.IsCarry = (amount1 & 1) != 0;
					markers.IsOverflow = ((amount1 & 1) ^ (amount1 >> 7)) != 0;
				}
				return amount1;
			}
			amount2 &= 0x7;
			byte ret = (byte)((amount1 << amount2) | (amount1 >> (8 - amount2)));
			markers.IsCarry = (ret & 1) != 0;
			markers.IsOverflow = ((ret & 1) ^ (ret >> 7)) != 0;

			return ret;
		}

		public ushort RotateLeftWord(ushort amount1, byte amount2)
		{
			if ((amount2 & 0xf) == 0)
			{
				if ((amount2 & 0x10) != 0)
				{
					markers.IsCarry = (amount1 & 1) != 0;
					markers.IsOverflow = ((amount1 & 1) ^ (amount1 >> 15)) != 0;
				}
				return amount1;
			}
			amount2 &= 0xf;
			ushort ret = (ushort)((amount1 << amount2) | (amount1 >> (16 - amount2)));
			markers.IsCarry = (ret & 1) != 0;
			markers.IsOverflow = ((ret & 1) ^ (ret >> 15)) != 0;

			return ret;
		}

		public byte RotateRightByte(byte amount1, byte amount2)
		{
			if ((amount2 & 0x7) == 0)
			{
				if ((amount2 & 0x18) != 0)
				{
					markers.IsCarry = (amount1 >> 7) != 0;
					markers.IsOverflow = ((amount1 >> 7) ^ ((amount1 >> 6) & 0x1)) != 0;
				}
				return amount1;
			}
			amount2 &= 0x7;
			byte ret = (byte)((amount1 >> amount2) | (amount1 << (8 - amount2)));
			markers.IsCarry = (ret & 0x80) != 0;
			markers.IsOverflow = ((ret ^ (ret << 1)) & 0x80) != 0;

			return ret;
		}

		public ushort RotateRightWord(ushort amount1, byte amount2)
		{
			if ((amount2 & 0xf) == 0)
			{
				if ((amount2 & 0x10) != 0)
				{
					markers.IsCarry = (amount1 >> 15) != 0;
					markers.IsOverflow = ((amount1 >> 15) ^ ((amount1 >> 14) & 0x1)) != 0;
				}
				return amount1;
			}
			amount2 &= 0xf;
			ushort ret = (ushort)((amount1 >> amount2) | (amount1 << (16 - amount2)));
			markers.IsCarry = (ret & 0x8000) != 0;
			markers.IsOverflow = ((ret ^ (ret << 1)) & 0x8000) != 0;

			return ret;
		}

		public byte ShiftRightSignedByte(byte amount1, byte amount2)
		{
			byte ret;

			if (amount2 == 0)
				return amount1;

			if (amount2 > 8) amount2 = 8;
			if ((amount1 & 0x80) != 0)
			{
				ret = (byte)((amount1 >> amount2) | (0xff << (8 - amount2)));
			}
			else
			{
				ret = (byte)(amount1 >> amount2);
			}
			this.markers.IsCarry = (((byte)(amount1) >> (amount2 - 1)) & 1) != 0;
			this.markers.IsZero = (ret == 0);
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsOverflow = false;

			return ret;
		}

		public ushort ShiftRightSignedWord(ushort amount1, byte amount2)
		{
			ushort ret;

			if (amount2 == 0)
				return amount1;

			if (amount2 > 16) amount2 = 16;
			if ((amount1 & 0x8000) != 0)
			{
				ret = (ushort)((amount1 >> amount2) | (0xffff << (16 - amount2)));
			}
			else
			{
				ret = (ushort)(amount1 >> amount2);
			}
			this.markers.IsCarry = (((ushort)(amount1) >> (amount2 - 1)) & 1) != 0;
			this.markers.IsZero = (ret == 0);
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsOverflow = false;

			return ret;
		}

		public byte SubtractBorrowByte(byte amount1, byte amount2)
		{
			byte chrMarker = (byte)((this.markers.IsCarry) ? 1 : 0);
			byte ret = (byte)(amount1 - (amount2 + chrMarker));

			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < ret) || (this.markers.IsCarry && (amount2 == 0xff));
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort SubtractBorrowWord(ushort amount1, ushort amount2)
		{
			ushort chrMarker = (ushort)((this.markers.IsCarry) ? 1 : 0);
			ushort ret = (ushort)(amount1 - (amount2 + chrMarker));

			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < ret) || (this.markers.IsCarry && (amount2 == 0xffff));
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public void ScanStringByte()
		{
			CompareByte(this.regAcc.LowByte, storage.PullUInt8(this.segExtra.Value16, this.regDest.Value16));
			if (this.markers.IsBackward)
			{
				this.regDest.Value16--;
			}
			else
			{
				this.regDest.Value16++;
			}
		}

		public void RepeatScanStringByte()
		{
			while (this.regCount.Value16 != 0)
			{
				CompareByte(this.regAcc.LowByte, storage.PullUInt8(this.segExtra.Value16, this.regDest.Value16));
				if (this.markers.IsBackward)
				{
					this.regDest.Value16--;
				}
				else
				{
					this.regDest.Value16++;
				}
				this.regCount.Value16--;

				if (!this.markers.IsZero)
					break;
			}
		}

		public byte ShiftLeftByte(byte amount1, byte amount2)
		{
			if (amount2 == 0)
				return amount1;

			byte ret = (byte)(amount1 << amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = (amount2 > 8) ? false : ((amount1 >> (8 - amount2)) & 1) != 0;
			this.markers.IsOverflow = ((ret ^ amount1) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort ShiftLeftWord(ushort amount1, byte amount2)
		{
			if (amount2 == 0)
				return amount1;

			ushort ret = (ushort)(amount1 << amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = (amount2 > 16) ? false : ((amount1 >> (16 - amount2)) & 1) != 0;
			this.markers.IsOverflow = ((ret ^ amount1) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort ShiftLeftDoubleWord(ushort amount1, ushort amount2, byte amount3)
		{
			if (amount3 == 0)
				return amount1;

			uint ret = (uint)((uint)amount1 << amount3) | (uint)(amount2 & ((uint)Math.Pow(2.0, amount3) - 1));

			// Modifies flags: SF, ZF, PF, CF (AF, OF undefined)
			this.markers.IsCarry = (ret & 0x10000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret & 0xffff) == 0;

			return (ushort)(ret & 0xffff);
		}

		public byte ShiftRightByte(byte amount1, byte amount2)
		{
			if (amount2 == 0)
				return amount1;

			byte ret = (byte)(amount1 >> amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = (amount2 > 8) ? false : ((amount1 >> (amount2 - 1)) & 1) != 0;
			this.markers.IsOverflow = ((amount2 & 0x1f) == 1) ? (amount1 > 0x80) : false;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort ShiftRightWord(ushort amount1, byte amount2)
		{
			if (amount2 == 0)
				return amount1;

			ushort ret = (ushort)(amount1 >> amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = (amount2 > 16) ? false : ((amount1 >> (amount2 - 1)) & 1) != 0;
			this.markers.IsOverflow = ((amount2 & 0x1f) == 1) ? (amount1 > 0x8000) : false;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		/// <summary>
		/// STOS - Store String (Byte, Word or Doubleword) to ES:DI
		/// </summary>
		public void StoreStringByte()
		{
			storage.EmitUInt8(this.segExtra.Value16, this.regDest.Value16, this.regAcc.LowByte);
			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				this.regDest.Value16--;
			}
			else
			{
				this.regDest.Value16++;
			}
		}

		/// <summary>
		/// STOS - Store String (Byte, Word or Doubleword) to ES:DI
		/// </summary>
		public void RepeatStoreStringByte()
		{
			while (this.regCount.Value16 != 0)
			{
				StoreStringByte();
				this.regCount.Value16--;
			}
		}

		/// <summary>
		/// STOS - Store String (Byte, Word or Doubleword) to ES:DI
		/// </summary>
		public void StoreStringWord()
		{
			storage.EmitUInt16(this.segExtra.Value16, this.regDest.Value16, this.regAcc.Value16);
			// Modifies flags: None
			if (this.markers.IsBackward)
			{
				this.regDest.Value16 -= 2;
			}
			else
			{
				this.regDest.Value16 += 2;
			}
		}

		/// <summary>
		/// STOS - Store String (Byte, Word or Doubleword) to ES:DI
		/// </summary>
		public void RepeatStoreStringWord()
		{
			while (this.regCount.Value16 != 0)
			{
				StoreStringWord();
				this.regCount.Value16--;
			}
		}

		public byte SubtractByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 - amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < amount2);
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x80) != 0;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort SubtractWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 - amount2);
			// Modifies flags: AF CF OF PF SF ZF
			this.markers.IsCarry = (amount1 < amount2);
			this.markers.IsOverflow = (((amount1 ^ amount2) & (amount1 ^ ret)) & 0x8000) != 0;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public void TestBitsByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 & amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);
		}

		public void TestBitsWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 & amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);
		}

		public void TableLookup(CPURegister argAcc, CPURegister cell, CPURegister argBase)
		{
			argAcc.LowByte = this.storage.PullUInt8(cell.Value16, (ushort)(argBase.Value16 + argAcc.LowByte));
		}

		public byte BitXorByte(byte amount1, byte amount2)
		{
			byte ret = (byte)(amount1 ^ amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x80) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}

		public ushort BitXorWord(ushort amount1, ushort amount2)
		{
			ushort ret = (ushort)(amount1 ^ amount2);
			// Modifies flags: CF OF PF SF ZF (AF undefined)
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (ret & 0x8000) != 0;
			this.markers.IsZero = (ret == 0);

			return ret;
		}
		#endregion

		#region IO instructions
		public byte ReadPortByte(ushort gate)
		{
			switch (gate)
			{
				case 0x40:
					// 8253 or 8254 Programmable Interval Timer
					break;

				case 0x61:
					// 8255 Port B output - reset keyboard
					break;

				default:
					this.journal.EmitLine($"Input byte from port 0x{gate:x4}");
					break;
			}

			return 0;
		}

		public void WritePortByte(byte gate, byte amount)
		{
			this.WritePortByte((ushort)gate, amount);
		}

		public void WritePortByte(ushort gate, byte amount)
		{
			switch (gate)
			{
				case 0x20:
					// 8259A Master Programmable Interrupt Controller
					break;

				case 0x40:
				case 0x43:
					// 8253 or 8254 Programmable Interval Timer
					break;

				case 0x61:
					// system control port (for compatibility with 8255)
					break;

				default:
					this.journal.EmitLine($"Output to port 0x{gate:x4}, byte value 0x{amount:x2}");
					break;
			}
		}

		public void WritePortWord(ushort gate, ushort amount)
		{
			WritePortByte(gate, (byte)(amount & 0xff));
			WritePortByte((ushort)(gate + 1), (byte)((amount & 0xff00) >> 8));
		}

		public void RepeatWritePortsByte(CPURegister cellSect, CPURegister argSource, CPURegister argCount)
		{
			while (this.regCount.Value16 != 0)
			{
				WritePortByte(this.regData.Value16, this.storage.PullUInt8(cellSect.Value16, argSource.Value16));
				this.regCount.Value16--;
			}
		}
		#endregion

		#region Flow control instructions

		public void WhenApplicationQuits()
		{
			this.appQuit = true;
		}

		public void Quit(int cipher)
		{
			this.AllowClock = false;

			this.journal.EmitLine($"Exiting program with code {cipher}");

			throw new ApplicationExitException();
		}

		public void Invoke(ushort displacement)
		{
			this.journal.EmitLine($"Trying to call function at 0x{this.segCode.Value16:x4}:0x{displacement:x4}");
		}

		public void Invoke(ushort section, ushort displacement)
		{
			this.journal.EmitLine($"Trying to call function at 0x{section:x4}:0x{displacement:x4}");
		}

		public void Summon(uint location)
		{
			this.journal.EmitLine($"Trying to call function at 0x{location:x8}");
		}

		public void Branch(ushort displacement)
		{
			this.journal.EmitLine($"Trying to jump to 0x{this.SegCode.Value16:x4}:0x{displacement:x4}");
		}

		public void BranchFar(uint location)
		{
			this.journal.EmitLine($"Trying to jump to 0x{location:x8}");
		}
		#endregion

		#region Interrupt instruction
		private byte videoStyle = 3;

		public void RaiseInterrupt(byte amount)
		{
			int total;
			int remainingInt;
			int headInt;

			switch (amount)
			{
				case 0x10:
					switch (this.regAcc.HighByte)
					{
						case 0x0:
							this.videoStyle = this.regAcc.LowByte;
							this.markers.IsCarry = false;
							break;

						case 0x2:
							Console.CursorLeft = this.regData.LowByte;
							Console.CursorTop = this.regData.HighByte;
							this.markers.IsCarry = false;
							break;

						case 0x9:
							remainingInt = Console.CursorLeft;
							headInt = Console.CursorTop;
							total = this.regCount.Value16;
							for (; total > 0; total--)
							{
								Console.Write((char)this.regAcc.LowByte);
							}
							Console.CursorLeft = remainingInt;
							Console.CursorTop = headInt;
							this.markers.IsCarry = false;
							break;

						case 0xe:
							Console.Write((char)this.regAcc.LowByte);
							this.markers.IsCarry = false;
							break;

						case 0xf:
							this.regAcc.HighByte = 80;
							this.regAcc.LowByte = this.videoStyle;
							this.regBase.HighByte = 0;
							this.markers.IsCarry = false;
							break;

						default:
							throw new Exception($"Unknown 0x10 interrupt 0x{this.regAcc.HighByte:x2}");
					}
					break;

				case 0x11:
					this.regAcc.Value16 = 0x22;
					break;

				case 0x16:
					switch (this.regAcc.HighByte)
					{
						case 0:
							this.host.ReadKeyBlocking();
							if (this.regAcc.Value16 == 0)
							{
								this.host.ReadKeyBlocking();
								switch (this.regAcc.Value16)
								{
									case 0x4b:
										this.regAcc.Value16 = 0x4b00; // Left
										break;

									case 0x48:
										this.regAcc.Value16 = 0x4800; // Up
										break;

									case 0x4d:
										this.regAcc.Value16 = 0x4d00; // Right
										break;

									case 0x50:
										this.regAcc.Value16 = 0x5000; // Down
										break;
								}
							}
							break;

						default:
							throw new Exception($"Unknown 0x16 interrupt 0x{this.regAcc.HighByte:x2}");
					}
					break;

				case 0x21:
					switch (this.regAcc.HighByte)
					{
						case 0:
							DOSTerminateHandle();
							break;

						case 1:
							DosReadKeyShowing();
							break;

						case 2:
							DosEmitChar();
							break;

						case 5:
							DosPrinterWrite();
							break;

						case 6:
							DosRawConsole();
							break;

						case 9:
							DOSPrintText();
							break;

						case 0xe:
							this.regAcc.LowByte = 3;
							break;

						case 0xf:
							DosAcquireFile();
							break;

						case 0x10:
							DosShutFile();
							break;

						case 0x11:
							DOSSearchForFirstRecordUsingFCB();
							break;

						case 0x16:
							DOSConstructFile();
							break;

						case 0x19:
							DOSObtainCurrentDefaultDrive();
							break;

						case 0x1a:
							DOSAssignFileTransferArea();
							break;

						case 0x21:
							DOSRandomFilePull();
							break;

						case 0x22:
							DOSRandomFileEmit();
							break;

						case 0x25:
							DOSAssignInterruptVector();
							break;

						case 0x29:
							DosSplitNameForFcb();
							break;

						case 0x2c:
							DOSObtainCurrentTime();
							break;

						case 0x30:
							// DOS version 6.33
							this.regAcc.Value16 = 0x1606;
							this.markers.IsCarry = false;
							break;

						case 0x35:
							DOSObtainInterruptVector();
							break;

						case 0x3c:
							DOSConstructFileUsingHandle();
							break;

						case 0x3d:
							DosAcquireFileByHandle();
							break;

						case 0x3e:
							DosShutFileByHandle();
							break;

						case 0x3f:
							DOSPullFromFileOrDeviceUsingHandle();
							break;

						case 0x40:
							DOSEmitFileUsingHandle();
							break;

						case 0x42:
							DosSeekByHandle();
							break;

						case 0x44:
							DosDeviceControl();
							break;

						case 0x47:
							DosObtainCurrentDirectory();
							break;

						case 0x48:
							DOSReserveChunk();
							break;

						case 0x49:
							DOSFreeChunk();
							break;

						case 0x4a:
							DOSAdjustChunkSize();
							break;

						case 0x4b:
							DOSImportOrExecuteProgram();
							break;

						case 0x4c:
							DOSTerminateHandleWithReturnCode();
							break;

						default:
							throw new Exception($"Unknown 0x21 interrupt 0x{this.regAcc.HighByte:x2}");
					}
					break;

				case 0x33:
					break;

				default:
					throw new Exception(string.Format("Unknown 0x{0:x2} interrupt", amount));
			}
		}

		private void DosReadKeyShowing()
		{
			while (this.host.InputTokens.Count == 0)
			{
				this.InputStarved?.Invoke(true);
				if (this.host.InputTokens.Count > 0)
					break;

				Thread.Sleep(10);
				this.PumpMessages();
			}

			lock (this.host.InputLatch)
			{
				int scratchInt = this.host.InputTokens.Dequeue();
				if (scratchInt < 0x80)
				{
					this.regAcc.LowByte = (byte)(scratchInt & 0xff);
					Console.Write((char)this.regAcc.LowByte);
				}
				else
				{
					this.regAcc.LowByte = 0;
				}
			}
		}

		private void DOSImportOrExecuteProgram()
		{
			if (this.regAcc.LowByte == 3)
			{
				string title = DosPaths.ObtainDOSFileLabel(this.PullText(CPU.AsLinearAddress(this.SegData.Value16, this.RegData.Value16)).ToUpper());
				string trail = Path.Combine(this.standardFolder, title);
				ushort seg = PullUInt16(this.segExtra.Value16, this.regBase.Value16);
				ushort rebaseSection = PullUInt16(this.segExtra.Value16, (ushort)(this.regBase.Value16 + 2));

				this.journal.EmitLine($"Loading overlay '{trail}' at segment 0x{seg:x4}");
				MZExecutable cover = new MZExecutable(trail);
				cover.EnactRelocations(rebaseSection);
				this.storage.EmitChunk(seg, 0, cover.Content, 0, cover.Content.Length);
				this.markers.IsCarry = false;
			}
			else
			{
				throw new Exception("Load and Execute Program");
			}
		}

		private void DOSAssignInterruptVector()
		{
			this.journal.EmitLine($"Setting interrupt vector 0x{this.regAcc.LowByte:x2} to address 0x{this.segData.Value16:x4}:0x{this.regData.Value16:x4}");
			this.markers.IsCarry = false;
		}

		private void DOSObtainInterruptVector()
		{
			this.journal.EmitLine($"Getting interrupt vector 0x{this.regAcc.LowByte:x2} address");
			this.segExtra.Value16 = 0;
			this.regBase.Value16 = 0;
			this.markers.IsCarry = false;
		}

		/// <summary>
		/// <para>INT 21,2 - Display Output</para>
		/// <para>Parameters: AH = 02; DL = character to output</para>
		/// <para>Returns: nothing</para>
		/// <para>Remarks: Outputs character to STDOUT.
		/// Backspace is treated as non-destructive cursor left.
		/// If Ctrl-Break is detected, INT 23 is executed.</para>
		/// </summary>
		private void DosEmitChar()
		{
			Console.Write((char)this.regData.LowByte);
			this.markers.IsCarry = false;
		}

		/// <summary>
		/// <para>INT 21,5 - Printer Output</para>
		/// <para>Parameters: AH = 05; DL = character to output</para>
		/// <para>Returns: nothing</para>
		/// <para>Remarks: Sends character in DL to STDPRN.
		/// Waits until STDPRN device is ready before output.</para>
		/// </summary>
		private void DosPrinterWrite()
		{
			throw new Exception("Printer output");
		}

		private void DosDeviceControl()
		{
			// I/O Control for Devices
			if (this.regAcc.LowByte == 0)
			{
				this.journal.EmitLine($"Get Device Information for handle 0x{this.regBase.Value16:x4}");
				switch (this.regBase.Value16)
				{
					case 0:
					case 1:
					case 2:
						this.regData.Value16 = 0x80d3;
						this.markers.IsCarry = false;
						break;

					case 3:
						this.regData.Value16 = 0x80c0;
						this.markers.IsCarry = false;
						break;

					case 4:
						this.regData.Value16 = 0xa8c0;
						this.markers.IsCarry = false;
						break;

					default:
						this.regData.Value16 = 0xa8c0;
						this.markers.IsCarry = false;
						break;
				}
			}
		}

		///<summary>
		/// <para>INT 21,6 - Direct Console I/O</para>
		/// <para>Parameters: DL = (0-0xFE) character to output; 0xFF if console input request</para>
		/// <para>Returns: AL = input character if console input request (DL=FF)</para>
		/// <para>ZF = 0 if console request character available (in AL); 
		/// 1 if no character is ready, and function request was console input</para>
		/// <para>Remarks: reads from or writes to the console device depending on the value of DL, 
		/// cannot output character FF (DL=FF indicates read function), 
		/// for console read, no echo is produced,
		/// returns 0 for extended keystroke, then function must be called again to return scan code,
		/// ignores Ctrl-Break and Ctrl-PrtSc</para>
		///</summary>
		private void DosRawConsole()
		{
			if (this.regData.LowByte != 0xff)
			{
				Console.Write((char)this.regData.LowByte);
			}
			else
			{
				if (this.host.InputTokens.Count == 0)
				{
					this.InputStarved?.Invoke(false);
				}

				if (this.host.InputTokens.Count > 0)
				{
					markers.IsZero = false;
					lock (this.host.InputLatch)
					{
						int scratchInt = this.host.InputTokens.Dequeue();
						if (scratchInt < 0x80)
						{
							this.regAcc.LowByte = (byte)(scratchInt & 0xff);
						}
						else
						{
							this.regAcc.LowByte = 0;
						}
					}
				}
				else
				{
					markers.IsZero = true;
				}
			}
			this.markers.IsCarry = false;
		}

		/// <summary>
		/// <para>INT 21,9 - Print String</para>
		/// <para>Parameters: DS:DX = pointer to string ending in "$"</para>
		/// <para>Returns: nothing</para>
		/// <para>Remarks: outputs character string to STDOUT up to "$",
		/// backspace is treated as non-destructive,
		/// if Ctrl-Break is detected, INT 23 is executed</para>
		/// </summary>
		private void DOSPrintText()
		{
			string scratch = this.PullDosText(CPU.AsLinearAddress(this.segData.Value16, this.regData.Value16));
			if (scratch.Length > 0)
			{
				Console.Write(scratch);
			}
			this.markers.IsCarry = false;
		}

		private void DOSConstructFileUsingHandle()
		{
			// open file
			string title = DosPaths.ObtainDOSFileLabel(this.PullText(CPU.AsLinearAddress(this.SegData.Value16, this.RegData.Value16)).ToUpper());
			string trail = Path.Combine(this.standardFolder, title);
			FileAccess permission = FileAccess.ReadWrite;

			if (this.documentHndTally > 0x7fff)
			{
				this.markers.IsCarry = true;
			}
			else
			{
				try
				{
					this.journal.EmitLine($"Creating file '{trail}'");
					this.accessDocuments.Add(this.documentHndTally, new FileStreamItem(new FileStream(trail, FileMode.Create, permission), FileStreamTypeEnum.Binary));
					this.regAcc.Value16 = (ushort)this.documentHndTally;
					this.documentHndTally++;
					this.markers.IsCarry = false;
				}
				catch
				{
					this.markers.IsCarry = true;
				}
			}
		}

		private void DOSEmitFileUsingHandle()
		{
			if (this.accessDocuments.IncludesKey((short)this.regBase.Value16))
			{
				FileStreamItem documentElement = this.accessDocuments.ObtainValueByKey((short)this.regBase.Value16);
				ushort span = this.regCount.Value16;
				uint location = CPU.AsLinearAddress(this.segData.Value16, this.regData.Value16);

				for (int idx = 0; idx < span; idx++)
				{
					documentElement.Flow.WriteByte(this.storage.PullUInt8(location));
					location++;
				}

				this.regAcc.Value16 = span;
				this.markers.IsCarry = false;
			}
			else
			{
				this.markers.IsCarry = true;
			}
		}

		private void DosSeekByHandle()
		{
			if (this.accessDocuments.IncludesKey((short)this.regBase.Value16))
			{
				FileStreamItem documentElement = this.accessDocuments.ObtainValueByKey((short)this.regBase.Value16);
				SeekOrigin anchor = SeekOrigin.Begin;

				switch (this.regAcc.LowByte)
				{
					case 0:
						anchor = SeekOrigin.Begin;
						break;

					case 1:
						anchor = SeekOrigin.Current;
						break;

					case 2:
						anchor = SeekOrigin.End;
						break;
				}

				int place = (int)(((uint)this.regCount.Value16 << 16) | (uint)this.regData.Value16);

				documentElement.Flow.Seek(place, anchor);

				this.regData.Value16 = (ushort)((documentElement.Flow.Position & 0xffff0000) >> 16);
				this.regAcc.Value16 = (ushort)(documentElement.Flow.Position & 0xffff);
				this.markers.IsCarry = false;
			}
			else
			{
				this.markers.IsCarry = true;
			}
		}

		private void DosAcquireFileByHandle()
		{
			// open file
			string title = this.PullText(CPU.AsLinearAddress(this.SegData.Value16, this.RegData.Value16)).ToUpper();
			string trail = Path.Combine(this.standardFolder, title);
			FileAccess permission = FileAccess.Read;

			switch (this.regAcc.LowByte & 0x7)
			{
				case 0:
					permission = FileAccess.Read;
					break;

				case 1:
					permission = FileAccess.Write;
					break;

				case 2:
					permission = FileAccess.ReadWrite;
					break;

				default:
					break;

			}
			if (this.documentHndTally > 0x7fff)
			{
				this.markers.IsCarry = true;
			}
			else
			{
				try
				{
					this.journal.EmitLine($"Opening file '{trail}'");
					this.accessDocuments.Add(this.documentHndTally, new FileStreamItem(new FileStream(trail, FileMode.Open, permission), FileStreamTypeEnum.Binary));
					this.regAcc.Value16 = (ushort)this.documentHndTally;
					this.documentHndTally++;
					this.markers.IsCarry = false;
				}
				catch
				{
					this.markers.IsCarry = true;
				}
			}
		}

		private void DosShutFileByHandle()
		{
			short hnd = (short)this.regBase.Value16;
			if (this.accessDocuments.IncludesKey(hnd))
			{
				this.accessDocuments.ObtainValueByKey(hnd).Flow.Close();
				this.accessDocuments.DiscardByKey(hnd);

				this.markers.IsCarry = false;
			}
			else
			{
				this.markers.IsCarry = true;
			}
		}

		private void DOSPullFromFileOrDeviceUsingHandle()
		{
			short hnd = (short)this.regBase.Value16;
			if (this.accessDocuments.IncludesKey(hnd))
			{
				byte[] cache = new byte[this.regCount.Value16];
				int pullOctets = this.accessDocuments.ObtainValueByKey(hnd).Flow.Read(cache, 0, cache.Length);
				if (pullOctets >= 0)
				{
					this.storage.EmitChunk(this.segData.Value16, this.regData.Value16, cache, 0, pullOctets);
					this.regAcc.Value16 = (ushort)pullOctets;
					this.markers.IsCarry = false;
				}
				else
				{
					this.markers.IsCarry = true;
				}
			}
			else
			{
				this.markers.IsCarry = true;
			}
		}

		/// <summary>
		/// <para>INT 21,F - Open a File Using FCB</para>
		/// <para>Parameters: AH = 0F;
		/// DS:DX = pointer to unopened FCB</para>
		/// <para>Returns: AL = 00 if file opened; = FF if unable to open</para>
		/// <para>Remarks: Opens an existing file using a previously setup FCB.
		/// The FCB fields drive identifier, filename and extension
		/// must be filled in before call.
		/// Sets default FCB fields; current block number is set to 0;
		/// record size is set to 80h; file size, date and time are set
		/// to the values from the directory.
		/// Does not create file, see  INT 21,16.
		/// DOS 2.x allows opening of subdirectories, DOS 3.x does not.</para>
		/// </summary>
		private void DosAcquireFile()
		{
			if (DosFcbAcquire(this.segData.Value16, this.regData.Value16))
			{
				this.regAcc.LowByte = 0;
			}
			else
			{
				this.regAcc.LowByte = 0xff;
			}
		}

		private void DOSObtainCurrentDefaultDrive()
		{
			// Force C drive
			this.regAcc.LowByte = 2; // (byte)(sTemp[0] - 'A');
			this.markers.IsCarry = false;
		}

		private void DosObtainCurrentDirectory()
		{
			if (this.regData.LowByte != 0 && this.regData.LowByte != 2)
			{
				this.journal.EmitLine($"Wrong drive number {this.regData.LowByte}");
			}
			string scratch = this.standardFolder.Substring(3).ToUpper();
			this.EmitText(CPU.AsLinearAddress(this.segData.Value16, this.regSource.Value16), scratch, scratch.Length);
			this.markers.IsCarry = false;
		}

		bool DosFcbAcquire(ushort sect, ushort displacement)
		{
			DosFcb fileblock = new DosFcb(this.storage, sect, displacement);
			string brief;
			int hnd = -1;
			FileStream? document;
			brief = fileblock.ObtainLabel();

			// First check if the name is correct
			if (!File.Exists(brief))
				return false;

			// Check, if file is already opened
			for (int idx = 0; idx < this.documentGrips.Length; idx++)
			{
				document = this.documentGrips[idx];
				if (document != null && document.Name.Equals(brief, StringComparison.CurrentCultureIgnoreCase))
				{
					hnd = idx;
					fileblock.AcquireFile((byte)hnd);
					return true;
				}
			}

			if (!File.Exists(brief) || this.documentTally > 255)
				return false;
			document = new FileStream(brief, FileMode.Open, FileAccess.ReadWrite);
			for (int idx = 0; idx < this.documentGrips.Length; idx++)
			{
				if (this.documentGrips[idx] == null)
				{
					hnd = idx;
					this.documentTally++;
					break;
				}
			}
			if (hnd < 0)
				return false;

			this.documentGrips[hnd] = document;
			fileblock.AcquireFile((byte)hnd);

			return true;
		}

		/// <summary>
		/// <para>INT 21,10 - Close a File Using FCB</para>
		/// <para>Parameters: AH = 10h;
		/// DS:DX = pointer to an opened FCB</para>
		/// <para>Returns: AL = 00  if file closed;
		/// = FF  if file not closed</para>
		/// <para>Remarks: Closes a previously opened file opened with an FCB
		/// FCB must be setup with drive id, filename, and extension
		/// before call.</para>
		/// </summary>
		private void DosShutFile()
		{
			if (DosFcbRelease(this.segData.Value16, this.regData.Value16))
			{
				this.regAcc.LowByte = 0;
			}
			else
			{
				this.regAcc.LowByte = 0xff;
			}
			this.markers.IsCarry = false;
		}

		void DosSplitNameForFcb()
		{
			DosFcb fileblock = new DosFcb(this.storage, this.segExtra.Value16, this.regDest.Value16);
			string docnameShort = DosPaths.ObtainDOSFileLabel(this.PullText(CPU.AsLinearAddress(this.segData.Value16, this.regSource.Value16)).ToUpper());
			string title = Path.GetFileNameWithoutExtension(docnameShort);
			string suffixShort = Path.GetExtension(docnameShort).Substring(1);
			string trail = Path.Combine(this.standardFolder, docnameShort);

			if (File.Exists(trail))
			{
				fileblock.AssignLabel(2, ref title, ref suffixShort);

				this.regSource.Value16 = (ushort)(this.regSource.Value16 + docnameShort.Length);
				this.regAcc.LowByte = 0;
			}
			else
			{
				title = "";
				fileblock.AssignLabel(2, ref title, ref suffixShort);
				this.regSource.Value16 = (ushort)(this.regSource.Value16 + docnameShort.Length);
				this.regAcc.LowByte = 0;
			}
		}

		void DOSSearchForFirstRecordUsingFCB()
		{
			DosFcb fileblock = new DosFcb(this.storage, this.segData.Value16, this.regData.Value16);
			string docfileTitle = fileblock.ObtainLabel();
			string trail = Path.Combine(this.standardFolder, docfileTitle.ToUpper());

			if (File.Exists(trail))
			{
				this.regAcc.LowByte = 0;
			}
			else
			{
				this.regAcc.LowByte = 0xff;
			}
		}

		bool DosFcbRelease(ushort sect, ushort displacement)
		{
			DosFcb fileblock = new DosFcb(this.storage, sect, displacement);
			if (!fileblock.IsConsistent())
				return false;

			byte filehnd;
			fileblock.ReleaseFile(out filehnd);
			if (filehnd >= this.documentTally)
				return false;

			if (this.documentGrips[filehnd] != null)
			{
				this.documentGrips[filehnd]!.Close();
			}

			this.documentGrips[filehnd] = null;

			return true;
		}

		/// <summary>
		/// <para>INT 21,16 - Create a File Using FCB</para>
		/// <para>Parameters: AH = 16h;
		/// DS:DX = pointer to an unopened FCB</para>
		/// <para>Returns: AL = 00 if file created;
		/// = FF if file creation failed</para>
		/// <para>Remarks: Creates file using FCB and leaves open for later output.
		/// FCB must be setup with drive id, filename, and extension before call.</para>
		/// </summary>
		private void DOSConstructFile()
		{
			throw new Exception("Create file");
		}

		/// <summary>
		/// <para>INT 21,1A - Set Disk Transfer Address (DTA)</para>
		/// <para>Parameters: AH = 1A;
		/// DS:DX = pointer to disk transfer address (DTA)</para>
		/// <para>Returns: nothing</para>
		/// <para>Remarks: Specifies the disk transfer address to DOS.
		/// DTA cannot overlap 64K segment boundary.
		/// Offset 80h in the PSP is a 128 byte default DTA supplied
		/// by DOS upon program load. 
		/// Use of the DTA provided by DOS will result in the loss
		/// of the program command tail which also occupies the 128
		/// bytes starting at offset 80h of the PSP.</para>
		/// </summary>
		private void DOSAssignFileTransferArea()
		{
			this.dta = CPU.AsLinearAddress(this.segData.Value16, this.regData.Value16);
		}

		/// <summary>
		/// <para>INT 21,21 - Random Read Using FCB</para>
		/// <para>Parameters: AH = 21h;
		/// DS:DX = pointer to an opened FCB</para>
		/// <para>Returns: AL = 00 if read successful;
		/// = 01 if EOF (no data read);
		/// = 02 if DTA is too small;
		/// = 03 if EOF (partial record read)</para>
		/// <para>Remarks: Reads random records from a file opened with an FCB
		/// to the DTA.
		/// FCB must be setup with drive id, filename, extension,
		/// record position and record length before call.
		/// Current record position field in FCB is not updated.</para>
		/// </summary>
		private void DOSRandomFilePull()
		{
			this.regAcc.LowByte = DosFcbRandomPull(this.segData.Value16, this.regData.Value16, 1, true);
		}

		byte DosFcbRandomPull(ushort sect, ushort displacement, ushort nrRecord, bool recover)
		{
			/* if restore is true :random read else random block read. 
			 * random read updates old block and old record to reflect the random data
			 * before the read!!!!!!!!! and the random data is not updated! (user must do this)
			 * Random block read updates these fields to reflect the state after the read!
			 */

			/* BUG: numRec should return the amount of records read! 
			 * Not implemented yet as I'm unsure how to count on error states (partial/failed) 
			 */

			DosFcb fileblock = new DosFcb(this.storage, sect, displacement);
			uint chance;
			ushort priorChunk = 0;
			byte priorRecord = 0;
			byte fault = 0;

			// Set the correct record from the random data
			fileblock.ObtainRandom(out chance);
			fileblock.AssignRecord((ushort)(chance / 128), (byte)(chance & 127));
			if (recover)
				fileblock.ObtainRecord(out priorChunk, out priorRecord); //store this for after the read.

			// Read records
			for (int idx = 0; idx < nrRecord; idx++)
			{
				fault = DosFcbPull(sect, displacement, (ushort)idx);
				if (fault != 0x0) break;
			}

			ushort freshChunk;
			byte freshRecord;
			fileblock.ObtainRecord(out freshChunk, out freshRecord);
			if (recover)
				fileblock.AssignRecord(priorChunk, priorRecord);
			// Update the random record pointer with new position only when restore is false
			if (!recover)
				fileblock.AssignRandom((uint)freshChunk * 128 + (uint)freshRecord);

			return fault;
		}

		byte DosFcbPull(ushort sect, ushort displacement, ushort recnum)
		{
			DosFcb fileblock = new DosFcb(this.storage, sect, displacement);
			byte filehnd, nowRecord;
			ushort nowChunk, recordExtent;
			fileblock.ObtainSeqContent(out filehnd, out recordExtent);
			fileblock.ObtainRecord(out nowChunk, out nowRecord);

			uint loc = (((uint)nowChunk * 128) + (uint)nowRecord) * (uint)recordExtent;
			byte[] msdosCopybuffer = new byte[recordExtent];
			FileStream? document = this.documentGrips[filehnd];
			if (document == null)
				return 1;

			long nextpos = document.Seek(loc, SeekOrigin.Current);

			ushort remainingAlt = recordExtent;
			remainingAlt = (ushort)document.Read(msdosCopybuffer, 0, remainingAlt);
			if (remainingAlt == 0)
				return 0x1;
			if (remainingAlt < recordExtent)
			{
				//Zero pad copybuffer to rec_size
				ushort idx = remainingAlt;
				while (idx < recordExtent)
					msdosCopybuffer[idx++] = 0;
			}
			for (int idx = 0; idx < msdosCopybuffer.Length; idx++)
			{
				this.storage.EmitUInt8((uint)(this.dta + ((uint)recnum * (uint)recordExtent) + idx), msdosCopybuffer[idx]);
			}
			if (++nowRecord > 127)
			{
				nowChunk++;
				nowRecord = 0;
			}
			fileblock.AssignRecord(nowChunk, nowRecord);

			if (remainingAlt == recordExtent)
				return 0;

			if (remainingAlt == 0)
				return 1;

			return 3;
		}

		/// <summary>
		/// <para>INT 21,22 - Random Write Using FCB</para>
		/// <para>Parameters: AH = 21h;
		/// DS:DX = pointer to an opened FCB</para>
		/// <para>Returns: AL = 00 if write successful; 
		/// = 01 if diskette full or read only; 
		/// = 02 if DTA is too small</para>
		/// <para>Remarks: Write records to random location in file opened with FCB.
		/// FCB must be setup with drive id, filename, extension,
		/// record position and record length before call.
		/// Current record position field in FCB is not updated.</para>
		/// </summary>
		private void DOSRandomFileEmit()
		{
			throw new Exception("File write");
		}

		/// <summary>
		/// <para>INT 21,2C - Get Time</para>
		/// <para>Parameters: -</para>
		/// <para>Returns: CH = hour (0-23); CL = minutes (0-59); DH = seconds (0-59); DL = hundredths (0-99)</para>
		/// <para>Remarks: retrieves DOS maintained clock time</para>
		/// </summary>
		private void DOSObtainCurrentTime()
		{
			DateTime amount = DateTime.Now;
			this.regCount.HighByte = (byte)amount.Hour;
			this.regCount.LowByte = (byte)amount.Minute;
			this.regData.HighByte = (byte)amount.Second;
			this.regData.LowByte = (byte)(amount.Millisecond / 10);
			this.markers.IsCarry = false;
		}

		/// <summary>
		/// <para>INT 21,48 - Allocate Memory</para>
		/// <para>Parameters: BX = number of memory paragraphs requested</para>
		/// <para>Returns: AX = segment address of allocated memory block (MCB + 1para), 
		/// or error code if CF set;
		/// BX = size in paras of the largest block of memory available, or 
		/// if CF set, and AX = 08 (Not Enough Mem); 
		/// CF = 0 if successful, 1 if error</para>
		/// <para>Remarks: returns segment address of allocated memory block AX:0000,
		/// each allocation requires a 16 byte overhead for the MCB,
		/// returns maximum block size available if error</para>
		/// </summary>
		private void DOSReserveChunk()
		{
			ushort interim;
			if (this.regBase.Value16 == 0xffff)
			{
				this.regBase.Value16 = (ushort)((this.storage.UnusedStorage.Extent >> 4) & 0xffff);
				this.markers.IsCarry = true;
			}
			else if (this.storage.ReserveStorageChunk(this.regBase.Value16, out interim))
			{
				markers.IsCarry = false;
				this.regAcc.Value16 = interim;
			}
			else
			{
				markers.IsCarry = true;
				this.regBase.Value16 = interim;
				this.regAcc.Value16 = 8; // Insufficient memory
			}
		}

		/// <summary>
		/// <para>INT 21,49 - Free Allocated Memory</para>
		/// <para>Parameters: ES = segment of the block to be returned (MCB + 1para)</para>
		/// <para>Returns: AX = error code if CF set</para>
		/// <para>Remarks: releases memory and MCB allocated by INT 21,48. 
		/// May cause unpredictable results if memory wasn't allocated using 
		/// INT 21,48 or if memory wasn't allocated by the current process. 
		/// Checks for valid MCB id, but does NOT check for process ownership. 
		/// Care must be taken when freeing the memory of another process, to 
		/// assure the segment isn't in use by a TSR or ISR. 
		/// This function is unreliable in a TSR once resident, since 
		/// COMMAND.COM and many other .COM files take all available memory 
		/// when they load.</para>
		/// </summary>
		private void DOSFreeChunk()
		{
			if (this.storage.ReleaseStorageChunk(this.segExtra.Value16))
			{
				markers.IsCarry = false;
			}
			else
			{
				markers.IsCarry = true;
				this.regAcc.Value16 = 9; // Invalid memory block address
			}
		}

		/// <summary>
		/// <para>INT 21,4A - Modify Allocated Memory Block (SETBLOCK)</para>
		/// <para>Parameters: BX = new requested block size in paragraphs,
		/// ES = segment of the block (MCB + 1 para)</para>
		/// <para>Returns: AX = error code if CF set; 
		/// BX = maximum block size possible, if CF set and AX = 8</para>
		/// <para>Remarks: modifies memory blocks allocated by  INT 21,48,
		/// can be used by programs to shrink or increase the size of allocated memory, 
		/// PC-DOS version 2.1 and DOS 3.x will actually allocate the largest
		/// available block if CF is set.  BX will equal the size allocated.</para>
		/// </summary>
		private void DOSAdjustChunkSize()
		{
			if (this.storage.RescaleStorageChunk(this.segExtra.Value16, this.regBase.Value16))
			{
				markers.IsCarry = false;
				this.regAcc.Value16 = 0;
			}
			else
			{
				markers.IsCarry = true;
				this.regAcc.Value16 = 8;
				this.regBase.Value16 = 0;
			}
		}

		private void DOSTerminateHandleWithReturnCode()
		{
			this.Quit(this.regAcc.LowByte);
		}

		private void DOSTerminateHandle()
		{
			this.Quit(-1);
		}

		#endregion
	}
}
