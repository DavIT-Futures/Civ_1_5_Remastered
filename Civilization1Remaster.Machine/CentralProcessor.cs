using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Civilization1Remaster.Machine
{
	public partial class CentralProcessor : IStackProbe
	{
		private const ushort ClockContentSection = 0x3b01;

		private const ushort ClockDenominatorDisplacement = 0x48;
		private const double ClockCircuitHertz = 1193181.667;

		private const int PeakWaitingClockBeats = 6;

		private readonly CentralProcessorFlags markers = new CentralProcessorFlags();

		private readonly CentralProcessorRegister registerAccumulator = new CentralProcessorRegister();

		private readonly CentralProcessorRegister registerBase = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerCount = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerData = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerFrame = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerStack = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerSource = new CentralProcessorRegister();
		private readonly CentralProcessorRegister registerDestination = new CentralProcessorRegister();
		private readonly CentralProcessorRegister segmentData = new CentralProcessorRegister();
		private readonly CentralProcessorRegister segmentExtra = new CentralProcessorRegister();
		private readonly CentralProcessorRegister segmentStack = new CentralProcessorRegister();
		private readonly CentralProcessorRegister segmentCode = new CentralProcessorRegister();

		private readonly CentralProcessorRegister scratch = new CentralProcessorRegister();

		private readonly CentralProcessorMemory storage;

		private readonly Thread clockWorker;

		private readonly FileStream?[] documentGrips = new FileStream?[256];

		private readonly IMachineHost host;

		private bool breakActive = false;

		private bool allowSignal = true;

		private bool allowClock = false;
		private bool insideClock = false;
		private volatile bool applicationQuit = false;

		private int waitingClockBeats = 0;

		private volatile int clockDenominator = 0x4dae;

		private Point pointerSite = new Point(-1, -1);

		private ushort pointerClicks = 0;
		private string standardFolder;
		private CallJournal journal;

		public CentralProcessor(IMachineHost host, CallJournal journal)
		{
			this.host = host;
			this.journal = journal;
			this.storage = new CentralProcessorMemory(this);
			this.clockWorker = new Thread(ClockWorker) { IsBackground = true, Name = "Timer", Priority = ThreadPriority.AboveNormal };
			this.clockWorker.Start();

			for (int index = 0; index < this.documentGrips.Length; index++)
			{
				this.documentGrips[index] = null;
			}

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

			string? matchFolder = OperatingSystemPaths.LocateGameDirectory("CIVILIZATION");
			if (matchFolder != null)
			{
				this.standardFolder = matchFolder;
			}
		}

		public CallJournal Journal
		{
			get { return this.journal; }
			set { this.journal = value; }
		}

		public bool AllowClock
		{
			get
			{
				return this.allowClock;
			}

			set
			{
				this.allowClock = value;
				this.waitingClockBeats = 0;
			}
		}

		public CentralProcessorFlags FlagSet
		{
			get { return this.markers; }
		}

		public CentralProcessorRegister RegisterAccumulator
		{
			get { return this.registerAccumulator; }
		}

		public CentralProcessorRegister RegisterBase
		{
			get { return this.registerBase; }
		}

		public CentralProcessorRegister RegisterCount
		{
			get { return this.registerCount; }
		}

		public CentralProcessorRegister RegisterData
		{
			get { return this.registerData; }
		}

		public CentralProcessorRegister RegisterFrame
		{
			get { return this.registerFrame; }
		}

		public CentralProcessorRegister RegisterStack
		{
			get { return this.registerStack; }
		}

		public CentralProcessorRegister RegisterSource
		{
			get { return this.registerSource; }
		}

		public CentralProcessorRegister RegisterDestination
		{
			get { return this.registerDestination; }
		}

		public CentralProcessorRegister SegmentData
		{
			get { return this.segmentData; }
		}

		public CentralProcessorRegister SegmentExtra
		{
			get { return this.segmentExtra; }
		}

		public CentralProcessorRegister SegmentStack
		{
			get { return this.segmentStack; }
		}

		public CentralProcessorRegister SegmentCode
		{
			get { return this.segmentCode; }
		}

		public CentralProcessorRegister Scratch
		{
			get { return this.scratch; }
		}

		public CentralProcessorMemory Storage
		{
			get { return this.storage; }
		}

		public string StandardFolder
		{
			get { return this.standardFolder; }
			set { this.standardFolder = value; }
		}

		ushort IStackProbe.StackPointer
		{
			get { return this.registerStack.Word; }
		}

		public static uint AsLinearAddress(ushort section, ushort displacement)
		{
			return ((uint)((uint)section << 4) + (uint)displacement) & 0xfffff;
		}

		public void Idle()
		{
			Thread.Sleep(1);
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
			if (this.applicationQuit)
			{
				throw new ApplicationExitException();
			}

			while (this.breakActive)
			{
				if (this.applicationQuit)
				{
					throw new ApplicationExitException();
				}

				Thread.Sleep(200);
			}

			ServeClock();
		}

		public void SplitDoubleWord(CentralProcessorRegister cellLower, CentralProcessorRegister cellUpper, uint amount)
		{
			cellLower.Word = (ushort)(amount & 0xffff);
			cellUpper.Word = (ushort)((amount & 0xffff0000) >> 16);
		}

		public byte PullByte(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullByte(section, displacement);
		}

		public sbyte PullSignedByte(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullSignedByte(section, displacement);
		}

		public ushort PullWord(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullWord(section, displacement);
		}

		public short PullSignedWord(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullSignedWord(section, displacement);
		}

		public uint PullDoubleWord(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullDoubleWord(section, displacement);
		}

		public int PullSignedDoubleWord(ushort section, ushort displacement)
		{
			this.PumpMessages();
			return this.storage.PullSignedDoubleWord(section, displacement);
		}

		public void EmitByte(ushort section, ushort displacement, byte amount)
		{
			this.PumpMessages();
			this.storage.EmitByte(section, displacement, amount);
		}

		public void EmitSignedByte(ushort section, ushort displacement, sbyte amount)
		{
			this.PumpMessages();
			this.storage.EmitSignedByte(section, displacement, amount);
		}

		public void EmitWord(ushort section, ushort displacement, ushort amount)
		{
			this.PumpMessages();
			this.storage.EmitWord(section, displacement, amount);
		}

		public void EmitSignedWord(ushort section, ushort displacement, short amount)
		{
			this.PumpMessages();
			this.storage.EmitSignedWord(section, displacement, amount);
		}

		public void EmitDoubleWord(ushort section, ushort displacement, uint amount)
		{
			this.PumpMessages();
			this.storage.EmitDoubleWord(section, displacement, amount);
		}

		public void EmitSignedDoubleWord(ushort section, ushort displacement, int amount)
		{
			this.PumpMessages();
			this.storage.EmitSignedDoubleWord(section, displacement, amount);
		}

		public string PullText(uint location)
		{
			if (location == 0)
			{
				return string.Empty;
			}

			StringBuilder sbyteAlternate = new StringBuilder();
			byte character;

			while ((character = this.storage.PullByte(location)) != 0)
			{
				sbyteAlternate.Append((char)character);
				location++;
			}

			return sbyteAlternate.ToString();
		}

		public void EmitText(uint location, string words, int peakSpan)
		{
			if (location == 0)
			{
				return;
			}

			for (int index = 0; index < words.Length && index < peakSpan; index++)
			{
				this.storage.EmitByte(location, (byte)words[index]);
				location++;
			}

			this.storage.EmitByte(location, 0);
		}

		public void StackWord(ushort amount)
		{
			if ((int)this.registerStack.Word - 2 < 0)
			{
				throw new Exception("Stack overflow");
			}

			this.registerStack.Word -= 1;
			this.storage.EmitByte(this.segmentStack.Word, this.registerStack.Word, (byte)((amount & 0xff00) >> 8));
			this.registerStack.Word -= 1;
			this.storage.EmitByte(this.segmentStack.Word, this.registerStack.Word, (byte)(amount & 0xff));
		}

		public ushort UnstackWord()
		{
			if ((int)this.registerStack.Word + 2 >= 0x10000)
			{
				throw new Exception("Stack underflow");
			}

			return (ushort)((ushort)this.storage.PullByte(this.segmentStack.Word, this.registerStack.Word++) |
				((ushort)this.storage.PullByte(this.segmentStack.Word, this.registerStack.Word++) << 8));
		}

		public void SignExtendWord(CentralProcessorRegister accumulatorArgument, CentralProcessorRegister dataArgument)
		{
			if ((accumulatorArgument.Word & 0x8000) != 0)
			{
				dataArgument.Word = 0xffff;
			}
			else
			{
				dataArgument.Word = 0;
			}
		}

		public ushort DecrementWord(ushort firstAmount)
		{
			ushort result = (ushort)(firstAmount - 1);
			this.markers.IsOverflow = result == 0x7fff;
			this.markers.IsSigned = (result & 0x8000) != 0;
			this.markers.IsZero = result == 0;

			return result;
		}

		public void DivideSignedWord(CentralProcessorRegister accumulatorArgument, CentralProcessorRegister dataArgument, ushort amount)
		{
			if (amount == 0)
			{
				throw new Exception("Division by zero");
			}

			int divisor = (short)amount;
			int number = (int)(((uint)dataArgument.Word << 16) | (uint)accumulatorArgument.Word);
			int quotient = number / divisor;
			short remnant = (short)(number % divisor);
			short wordQuotient = (short)((uint)quotient & 0xffff);
			if (quotient != wordQuotient)
			{
				throw new Exception("Division error");
			}

			dataArgument.Word = (ushort)remnant;
			accumulatorArgument.Word = (ushort)wordQuotient;
		}

		public void MultiplySignedWord(CentralProcessorRegister accumulatorArgument, CentralProcessorRegister dataArgument, ushort firstAmount)
		{
			uint result = (uint)((int)((short)accumulatorArgument.Word) * (int)((short)firstAmount));
			accumulatorArgument.Word = (ushort)(result & 0xffff);
			dataArgument.Word = (ushort)((result & 0xffff0000) >> 16);

			if ((result & 0xffff8000) == 0xffff8000 || (result & 0xffff8000) == 0x0)
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

		public ushort IncrementWord(ushort firstAmount)
		{
			ushort result = (ushort)(firstAmount + 1);
			this.markers.IsOverflow = result == 0x8000;
			this.markers.IsSigned = (result & 0x8000) != 0;
			this.markers.IsZero = result == 0;

			return result;
		}

		public byte BitOrByte(byte firstAmount, byte secondAmount)
		{
			byte result = (byte)(firstAmount | secondAmount);
			this.markers.IsCarry = false;
			this.markers.IsOverflow = false;
			this.markers.IsSigned = (result & 0x80) != 0;
			this.markers.IsZero = result == 0;

			return result;
		}

		public byte ShiftLeftByte(byte firstAmount, byte secondAmount)
		{
			if (secondAmount == 0)
			{
				return firstAmount;
			}

			byte result = (byte)(firstAmount << secondAmount);
			this.markers.IsCarry = (secondAmount > 8) ? false : ((firstAmount >> (8 - secondAmount)) & 1) != 0;
			this.markers.IsOverflow = ((result ^ firstAmount) & 0x80) != 0;
			this.markers.IsSigned = (result & 0x80) != 0;
			this.markers.IsZero = result == 0;

			return result;
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
					break;

				case 0x40:
				case 0x43:
					break;

				case 0x61:
					break;

				default:
					this.journal.EmitLine($"Output to port 0x{gate:x4}, byte value 0x{amount:x2}");
					break;
			}
		}

		public void WhenApplicationQuits()
		{
			this.applicationQuit = true;
		}

		public void Quit(int cipher)
		{
			this.AllowClock = false;

			this.journal.EmitLine($"Exiting program with code {cipher}");

			throw new ApplicationExitException();
		}

		public void Invoke(ushort displacement)
		{
			this.journal.EmitLine($"Trying to call function at 0x{this.segmentCode.Word:x4}:0x{displacement:x4}");
		}

		public void Invoke(ushort section, ushort displacement)
		{
			this.journal.EmitLine($"Trying to call function at 0x{section:x4}:0x{displacement:x4}");
		}

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint RequestTimerResolution(uint interval);

		private void ClockWorker()
		{
			if (OperatingSystem.IsWindows())
			{
				RequestTimerResolution(1);
			}

			Stopwatch timer = Stopwatch.StartNew();
			double followingBeat = 0.0;

			while (!this.applicationQuit)
			{
				double present = timer.Elapsed.TotalSeconds;

				if (present < followingBeat)
				{
					Thread.Sleep(1);
					continue;
				}

				if (this.allowClock && Volatile.Read(ref this.waitingClockBeats) < PeakWaitingClockBeats)
				{
					Interlocked.Increment(ref this.waitingClockBeats);
				}

				followingBeat += this.clockDenominator / ClockCircuitHertz;

				if (present - followingBeat > 0.5)
				{
					followingBeat = present;
				}
			}
		}

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

					(ushort accumulator, ushort count, ushort data, ushort basis, ushort frame, ushort source, ushort destination, ushort dataSection,
						ushort extraSection, ushort flags) = (this.registerAccumulator.Word, this.registerCount.Word, this.registerData.Word,
						this.registerBase.Word, this.registerFrame.Word, this.registerSource.Word, this.registerDestination.Word, this.segmentData.Word,
						this.segmentExtra.Word, this.markers.Datum);

					this.registerAccumulator.Word = 0;
					if (this.pointerSite != this.host.PointerSite)
					{
						this.registerAccumulator.Word |= 1;
					}

					this.registerCount.Word = (ushort)this.host.PointerSite.X;
					this.registerData.Word = (ushort)this.host.PointerSite.Y;

					this.registerBase.Word = 0;
					ushort remainingShort = (ushort)(this.host.PointerClicks & 1);
					this.registerBase.Word |= remainingShort;
					if ((this.pointerClicks & 1) != remainingShort)
					{
						if (remainingShort != 0)
						{
							this.registerAccumulator.Word |= 2;
						}
						else
						{
							this.registerAccumulator.Word |= 4;
						}
					}

					ushort eastShort = (ushort)(this.host.PointerClicks & 2);
					this.registerBase.Word |= eastShort;
					if ((this.pointerClicks & 2) != eastShort)
					{
						if (eastShort != 0)
						{
							this.registerAccumulator.Word |= 8;
						}
						else
						{
							this.registerAccumulator.Word |= 0x10;
						}
					}

					this.pointerSite = this.host.PointerSite;
					this.pointerClicks = this.host.PointerClicks;

					this.host.OnPointerEvent();

					(this.registerAccumulator.Word, this.registerCount.Word, this.registerData.Word, this.registerBase.Word, this.registerFrame.Word,
						this.registerSource.Word, this.registerDestination.Word, this.segmentData.Word, this.segmentExtra.Word, this.markers.Datum) =
						(accumulator, count, data, basis, frame, source, destination, dataSection, extraSection, flags);
				}

				CallJournal journalScratch = this.journal;
				this.journal = this.host.InterruptJournal;

				ushort interruptedFlags = this.markers.Datum;
				this.host.OnClockInterrupt();
				this.markers.Datum = interruptedFlags;

				int splitter = this.storage.PullWord(ClockContentSection, ClockDenominatorDisplacement);
				this.clockDenominator = splitter == 0 ? 0x10000 : Math.Max(splitter, 1193);

				this.journal = journalScratch;

				this.insideClock = false;
			}
		}
	}
}
