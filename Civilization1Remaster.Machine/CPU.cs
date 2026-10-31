using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Civilization1Remaster;
using Civilization1Remaster.Containers;
using Civilization1Remaster.Emulation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Civilization1Remaster.Emulation
{
	public partial class CPU : IStackProbe
	{
		// The fixed clock: a waiting loop that only reads the timer counter gets an interrupt every this many reads
		private const int TimerPollSpan = 16;

		// The timer variables of the game in the data segment (the counters the waiting loops read)
		private const ushort TimerVariablesBegin = 0x40;

		private const ushort TimerVariablesEnd = 0x70;

		// The game programs the timer chip (8253) with this divisor, normally for 60 interrupts per second
		private const ushort ClockContentSection = 0x3b01;

		private const ushort ClockDenominatorDisplacement = 0x48;
		private const double ClockCircuitHertz = 1193181.667;

		// When the game is busy for a while, it catches up at most this many timer interrupts
		private const int PeakWaitingClockBeats = 6;

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

		private readonly Thread clockWorker;

		// DOS file stuff
		private readonly FileStream[] documentGrips = new FileStream[256];

		private readonly IMachineHost host;

		// state
		private bool breakActive = false;

		// Timer(s)
		private bool allowSignal = true;

		private bool allowClock = false;
		private bool insideClock = false;
		private volatile bool appQuit = false;

		// Timer interrupts that are due and not yet run by DoEvents, counted by the timer thread
		private int waitingClockBeats = 0;

		// Timer chip divisor, read from the game memory by the game thread (0x4dae is 60 interrupts per second)
		private volatile int clockDenominator = 0x4dae;

		// Test seam: when not 0, the timer interrupts are counted from the calls of game functions instead of the real time
		private int fixedClockSpan = 0;

		private int fixedClockSteps = 0;
		private int timerPolls = 0;

		// Mouse
		private Point pointerSite = new Point(-1, -1);

		private ushort pointerClicks = 0;
		private string standardFolder;
		private LogWrapper journal;
		private int heartbeatSteps;

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
			string matchFolder = DosPaths.LocateGameDirectory("CIVILIZATION");
			if (matchFolder != null)
			{
				this.standardFolder = matchFolder;
			}
		}

		/// <summary>
		/// The journal of the game functions called now (the game switches it, for example to the journal of the intro); the
		/// fixed test clock counts the calls in every journal the game switches to
		/// </summary>
		public LogWrapper Journal
		{
			get { return this.journal; }
			set
			{
				this.journal = value;

				if (this.fixedClockSpan != 0)
					value.ScopeOpened = CountClockScope;
			}
		}

		public bool AllowClock
		{
			get { return this.allowClock; }
			set { this.allowClock = value; this.waitingClockBeats = 0; }
		}

		/// <summary>
		/// Test seam for runs without a window that must be reproducible: when not 0, a timer interrupt comes after this many
		/// calls of game functions (scopes of the journal) instead of after the real time, and the timer thread stops counting.
		/// The interrupt comes at the call itself, so it does not depend on the memory accesses of the code (B6: state moved
		/// from the memory into C# would move it otherwise).
		/// </summary>
		public int FixedClockSpan
		{
			get { return this.fixedClockSpan; }
			set
			{
				this.fixedClockSpan = value;
				this.fixedClockSteps = 0;
				this.waitingClockBeats = 0;
				this.journal.ScopeOpened = value != 0 ? CountClockScope : null;
			}
		}

		/// <summary>
		/// Test seam: called when the game looks for a key and none is waiting. The argument is true when the game waits for the key.
		/// A scripted host puts the next key into the input queue here.
		/// </summary>
		public Action<bool> InputStarved { get; set; }

		/// <summary>Test seam: sees the frames and the accesses of the stack segment (see IStackWatcher)</summary>
		public IStackWatcher StackWatcher { get; set; }

		public bool Hold
		{
			get { return this.breakActive; }
			set { this.breakActive = value; }
		}

		/// <summary>
		/// When set, called every 0x40000 memory reads (the tests check the time of a scenario in it: endless loops that call
		/// no game function fail then too). It must not change the state of the game.
		/// </summary>
		public Action Heartbeat { get; set; }

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

		public CPUMemory Storage
		{
			get { return this.storage; }
		}

		public string StandardFolder
		{
			get { return this.standardFolder; }
			set { this.standardFolder = value; }
		}

		ushort IStackProbe.StackPointer { get { return this.regStack.Value16; } }
		ushort IStackProbe.StackSegment { get { return this.segStack.Value16; } }
		ushort IStackProbe.DataSegment { get { return this.segData.Value16; } }
		ushort IStackProbe.FramePointer { get { return this.regFrame.Value16; } }

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
					ServeClock();
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

		public void EnableInterrupts()
		{
			this.allowSignal = true;
		}

		public void DisableInterrupts()
		{
			this.allowSignal = false;
		}

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

			// Under the fixed test clock the interrupts come only at the clock points (CountClockScope, CountPoll, Idle)
			if (this.fixedClockSpan == 0)
				ServeClock();
		}

		/// <summary>
		/// Test seam: the game asks for the position of the music (the intro and the title wait for it in loops without a call
		/// of a game function). Under the fixed clock it counts like a read of the timer variables (PollTimer), else nothing.
		/// </summary>
		public void PollMusic()
		{
			if (this.fixedClockSpan != 0)
				CountPoll();
		}

		public void SplitDoubleWord(CPURegister cellLower, CPURegister cellUpper, uint amount)
		{
			cellLower.Value16 = (ushort)(amount & 0xffff);
			cellUpper.Value16 = (ushort)((amount & 0xffff0000) >> 16);
		}

		public uint JoinDoubleWord(ushort lowerAmount, ushort upperAmount)
		{
			return ((uint)upperAmount << 16) | (uint)lowerAmount;
		}

		public byte PullUInt8(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 1);

			this.PumpMessages();
			PollTimer(section, displacement);
			return this.storage.PullUInt8(section, displacement);
		}

		public sbyte PullInt8(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 1);

			this.PumpMessages();
			PollTimer(section, displacement);
			return this.storage.PullInt8(section, displacement);
		}

		public ushort PullUInt16(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 2);

			this.PumpMessages();
			PollTimer(section, displacement);
			return this.storage.PullUInt16(section, displacement);
		}

		public short PullInt16(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 2);

			this.PumpMessages();
			PollTimer(section, displacement);
			return this.storage.PullInt16(section, displacement);
		}

		public uint PullUInt32(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 4);

			this.PumpMessages();
			return this.storage.PullUInt32(section, displacement);
		}

		public int PullInt32(ushort section, ushort displacement)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Read(displacement, 4);

			this.PumpMessages();
			return this.storage.PullInt32(section, displacement);
		}

		public void EmitUInt8(ushort section, ushort displacement, byte amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 1);

			this.PumpMessages();
			this.storage.EmitUInt8(section, displacement, amount);
		}

		public void EmitInt8(ushort section, ushort displacement, sbyte amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 1);

			this.PumpMessages();
			this.storage.EmitInt8(section, displacement, amount);
		}

		public void EmitUInt16(ushort section, ushort displacement, ushort amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 2);

			this.PumpMessages();
			this.storage.EmitUInt16(section, displacement, amount);
		}

		public void EmitInt16(ushort section, ushort displacement, short amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 2);

			this.PumpMessages();
			this.storage.EmitInt16(section, displacement, amount);
		}

		public void EmitUInt32(ushort section, ushort displacement, uint amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 4);

			this.PumpMessages();
			this.storage.EmitUInt32(section, displacement, amount);
		}

		public void EmitInt32(ushort section, ushort displacement, int amount)
		{
			if (this.StackWatcher != null && section == this.segStack.Value16)
				this.StackWatcher.Write(displacement, 4);

			this.PumpMessages();
			this.storage.EmitInt32(section, displacement, amount);
		}

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
			this.StackWatcher?.Write((ushort)(this.regStack.Value16 - 2), 2);
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
			this.StackWatcher?.Write((ushort)(this.regStack.Value16 - 4), 4);
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

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint RequestTimerResolution(uint interval);

		/// <summary>A call of a game function under the fixed test clock: every FixedClockSpan calls the timer interrupt comes</summary>
		private void CountClockScope()
		{
			if (this.allowClock && ++this.fixedClockSteps >= this.fixedClockSpan)
			{
				this.fixedClockSteps = 0;
				this.waitingClockBeats = 1;
			}

			// Also an interrupt that could not come before (interrupts disabled, or inside the timer function)
			ServeClock();
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

		/// <summary>
		/// The fixed clock: a read of a timer variable. The loops that wait for the timer only by reading its counter (no call
		/// of a game function, no Idle) get an interrupt every TimerPollSpan such reads, the same in every version of the code.
		/// </summary>
		private void PollTimer(ushort section, ushort displacement)
		{
			if (this.fixedClockSpan != 0 && section == this.segData.Value16 &&
				displacement >= TimerVariablesBegin && displacement < TimerVariablesEnd)
				CountPoll();
		}

		/// <summary>A poll of the timer or of the music under the fixed clock: every TimerPollSpan polls the interrupt comes</summary>
		private void CountPoll()
		{
			if (this.allowClock && ++this.timerPolls >= TimerPollSpan)
			{
				this.timerPolls = 0;
				this.waitingClockBeats = 1;
			}

			// Under the fixed test clock the interrupts come only at the calls of game functions, the waits and the polls of the
			// timer, never at a memory access: code that makes fewer memory accesses gets them at the same places
			ServeClock();
		}

		/// <summary>The timer interrupt (and the mouse interrupt before it) when one is due and the interrupts are allowed</summary>
		private void ServeClock()
		{
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
	}
}
