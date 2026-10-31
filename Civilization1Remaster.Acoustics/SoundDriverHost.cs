using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>
	/// Loads and runs an original Civilization sound driver (RSOUND.CVL for Roland MT-32, ASOUND.CVL for AdLib,
	/// ISOUND.CVL for PC speaker).
	/// The driver is a small DOS program with a table of entry points, it is run by X86Interpreter
	/// and its port writes go to the emulated sound hardware.
	/// </summary>
	public class SoundDriverHost
	{
		// Original game timing: song steps at 60 Hz, modulation at 5 times that rate
		public const int AdvanceRatio = 60;

		public const int BeatRatio = VaryRatio;

		// Where the driver is loaded in the interpreter memory, and the stack used to call it
		private const ushort FetchSection = 0x1000;

		private const ushort PileSection = 0x9000;
		private const ushort PileHead = 0xfff0;
		private const int RecordLookupDisplacement = 0x32;
		private const int RecordTally = 6;
		private const ushort OplCardLocationGate = 0x388;
		private const ushort OplCardContentGate = 0x389;
		private const ushort SynthContentGate = 0x330;
		private const ushort SynthConditionGate = 0x331;
		private const int VaryEachAdvance = 5;
		private const int VaryRatio = AdvanceRatio * VaryEachAdvance;

		private readonly X86Interpreter processor = new X86Interpreter();
		private readonly ushort[] recordMarks = new ushort[RecordTally];
		private readonly object latch = new object();

		private int oplCardLatch;

		// The MIDI interface (Roland MPU-401) answers each command with an acknowledge byte
		private bool synthConfirm;

		private int slicesTowardVary;
		private int sliceLeftover;
		private int varyTally;

		// Chopping wood effect (new city): MIDI percussion ticks or synthesized samples mixed into the audio
		private int impactBeat = -1;

		private short[]? impactSlices;
		private int impactPlace = -1;

		public SoundDriverHost(byte[] handlerDocument, int sliceRatio, string label)
		{
			this.Label = label;
			this.Fm = new Opl2Synth(sliceRatio);
			this.Buzzer = new PcSpeaker(sliceRatio);
			this.Synth = new Mt32ToGeneralMidi((condition, content1, content2) => { });

			ImportExecutable(handlerDocument);

			for (int idx = 0; idx < RecordTally; idx++)
			{
				this.recordMarks[idx] = this.processor.PullWord(FetchSection, (ushort)(RecordLookupDisplacement + idx * 2));
			}

			this.processor.GatePull = PullPort;
			this.processor.GateEmit = EmitPort;
			this.processor.Signal = ManageInterrupt;
			this.processor.SegStack = PileSection;
			this.processor.RegStack = PileHead;
		}

		public string Label { get; }
		public Opl2Synth Fm { get; }
		public PcSpeaker Buzzer { get; }

		/// <summary>
		/// MIDI output of the Roland driver, translated to General MIDI
		/// </summary>
		public Mt32ToGeneralMidi Synth { get; }

		/// <summary>
		/// The driver plays music through MIDI (Roland MT-32) instead of generating sound
		/// </summary>
		public bool HasSynth => string.Equals(this.Label, "RSOUND.CVL", StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Finds the first sound driver in the game folder, in the order of DriverFiles
		/// </summary>
		public static SoundDriverHost? Construct(string sessionFolder, int sliceRatio)
		{
			foreach (SoundDriverHost handler in ConstructAll(sessionFolder, sliceRatio))
				return handler;

			return null;
		}

		/// <summary>
		/// Loads the sound drivers from the game folder, in the order of DriverFiles,
		/// so the next one can be used when the sound output for a driver is missing
		/// </summary>
		public static IEnumerable<SoundDriverHost> ConstructAll(string sessionFolder, int sliceRatio)
		{
			foreach (string document in DriverFileNames(Environment.GetEnvironmentVariable("CIVILIZATION1REMASTER_SOUND")))
			{
				string? route = LocateFile(sessionFolder, document);

				if (route != null)
					yield return new SoundDriverHost(File.ReadAllBytes(route), sliceRatio, document);
			}
		}

		/// <summary>
		/// Sound drivers to try, best first: Roland MT-32 music (played by the General MIDI synthesizer of the computer),
		/// AdLib, PC speaker. The CIVILIZATION1REMASTER_SOUND environment variable can choose one: "roland", "adlib", "speaker" or "none".
		/// </summary>
		public static string[] DriverFileNames(string? option)
		{
			switch (option?.Trim().ToLowerInvariant())
			{
				case "none":
					return Array.Empty<string>();
				case "speaker":
					return new[] { "ISOUND.CVL" };
				case "adlib":
					return new[] { "ASOUND.CVL" };
				case "roland":
				case "mt32":
				case "midi":
					return new[] { "RSOUND.CVL" };
				default:
					return new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" };
			}
		}

		/// <summary>
		/// Entry 0: initializes the driver and the sound card
		/// </summary>
		public ushort Prepare() => Invoke(0, 0);

		/// <summary>
		/// Entry 1: starts a song or a sound effect, it also resets the song position
		/// </summary>
		public void Perform(ushort audio, ushort argument) => Invoke(1, audio, argument);

		/// <summary>
		/// Entry 2: stops the sound and shuts the driver down
		/// </summary>
		public ushort Halt() => Invoke(2);

		/// <summary>
		/// Entry 3: plays the next step of the song, the game calls it 60 times per second.
		/// Returns 0xffff when nothing is playing.
		/// </summary>
		public ushort Advance() => Invoke(3);

		/// <summary>
		/// Entry 4: updates the vibrato and tremolo effects, the game calls it from its fast timer (300 times per second)
		/// </summary>
		public void Vary() => Invoke(4);

		/// <summary>
		/// Entry 5: returns the song position (number of steps since the song started),
		/// the intro uses it to synchronize pictures with the music
		/// </summary>
		public ushort CurrentOffset() => Invoke(5);

		/// <summary>
		/// Plays the sound of chopping wood together with the current song, the game plays it when a city is founded
		/// </summary>
		public void PerformChopping()
		{
			lock (this.latch)
			{
				if (this.HasSynth)
				{
					this.impactBeat = 0;
				}
				else
				{
					this.impactSlices ??= ChoppingEffect.Synthesize(this.Fm.SliceRatio);
					this.impactPlace = 0;
				}
			}
		}

		/// <summary>
		/// Generates audio samples of the emulated sound hardware, without running the driver
		/// </summary>
		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				this.Fm.Produce(staging, displacement, tally);
				this.Buzzer.Produce(staging, displacement, tally);

				if (this.impactSlices != null && this.impactPlace >= 0)
				{
					for (int idx = 0; idx < tally && this.impactPlace < this.impactSlices.Length; idx++, this.impactPlace++)
					{
						staging[displacement + idx] = (short)Math.Clamp(staging[displacement + idx] + this.impactSlices[this.impactPlace], short.MinValue, short.MaxValue);
					}

					if (this.impactPlace >= this.impactSlices.Length)
						this.impactPlace = -1;
				}
			}
		}

		/// <summary>
		/// Runs the driver for one tick of the original fast game timer (TickRate times per second):
		/// Modulate on every tick and Step on every fifth tick.
		/// </summary>
		public void Beat()
		{
			Vary();

			if (++this.varyTally >= VaryEachAdvance)
			{
				this.varyTally = 0;
				Advance();
			}

			lock (this.latch)
			{
				if (this.impactBeat >= 0)
				{
					this.impactBeat = ChoppingEffect.MidiTick(this.impactBeat, this.Synth.Emitted) ? this.impactBeat + 1 : -1;
				}
			}
		}

		/// <summary>
		/// Generates audio samples and runs the driver at the same rate as the original game timer:
		/// Step 60 times per second and Modulate 300 times per second.
		/// </summary>
		public void Mix(short[] staging, int displacement, int tally)
		{
			int sliceRatio = this.Fm.SliceRatio;

			while (tally > 0)
			{
				if (this.slicesTowardVary <= 0)
				{
					Beat();

					// Accumulate the remainder so the average rate stays exact
					this.sliceLeftover += sliceRatio;
					this.slicesTowardVary += this.sliceLeftover / VaryRatio;
					this.sliceLeftover %= VaryRatio;
				}

				int slab = Math.Min(tally, Math.Max(1, this.slicesTowardVary));
				Produce(staging, displacement, slab);
				displacement += slab;
				tally -= slab;
				this.slicesTowardVary -= slab;
			}
		}

		private static string? LocateFile(string folder, string label)
		{
			if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
				folder = ".";

			foreach (string route in Directory.GetFiles(folder))
			{
				if (string.Equals(Path.GetFileName(route), label, StringComparison.OrdinalIgnoreCase))
					return route;
			}

			return null;
		}

		private void ImportExecutable(byte[] document)
		{
			if (document.Length < 0x20 || document[0] != 'M' || document[1] != 'Z')
				throw new InvalidDataException("Sound driver is not a DOS executable");

			int finalSheetOctets = BitConverter.ToUInt16(document, 2);
			int leaves = BitConverter.ToUInt16(document, 4);
			int rebaseTally = BitConverter.ToUInt16(document, 6);
			int preambleExtent = BitConverter.ToUInt16(document, 8) * 16;
			int rebaseDisplacement = BitConverter.ToUInt16(document, 0x18);
			int visualExtent = leaves * 512 - (finalSheetOctets != 0 ? 512 - finalSheetOctets : 0) - preambleExtent;
			visualExtent = Math.Min(visualExtent, document.Length - preambleExtent);

			Array.Copy(document, preambleExtent, this.processor.Storage, FetchSection << 4, visualExtent);

			for (int idx = 0; idx < rebaseTally; idx++)
			{
				ushort displacement = BitConverter.ToUInt16(document, rebaseDisplacement + idx * 4);
				ushort section = BitConverter.ToUInt16(document, rebaseDisplacement + idx * 4 + 2);
				ushort patchSection = (ushort)(FetchSection + section);
				this.processor.EmitWord(patchSection, displacement, (ushort)(this.processor.PullWord(patchSection, displacement) + FetchSection));
			}
		}

		private byte PullPort(ushort gate)
		{
			switch (gate)
			{
				case OplCardLocationGate:
					return this.Fm.PullStatus();
				case SynthConditionGate:
					// Bit 6 clear: ready to receive, bit 7 clear: a byte is waiting to be read
					return (byte)(this.synthConfirm ? 0x00 : 0x80);
				case SynthContentGate:
					this.synthConfirm = false;
					return 0xfe;
				case 0x61:
					return this.Buzzer.Gate61;
				default:
					return 0xff;
			}
		}

		private void EmitPort(ushort gate, byte amount)
		{
			switch (gate)
			{
				case OplCardLocationGate:
					this.oplCardLatch = amount;
					break;
				case OplCardContentGate:
					this.Fm.EmitRegister(this.oplCardLatch, amount);
					break;
				case SynthConditionGate:
					// Reset and UART mode commands
					this.synthConfirm = true;
					break;
				case SynthContentGate:
					this.Synth.Emit(amount);
					break;
				case 0x42:
				case 0x43:
				case 0x61:
					this.Buzzer.EmitPort(gate, amount);
					break;
			}
		}

		private void ManageInterrupt(X86Interpreter processor, byte numeral)
		{
			// Drivers only use the clock services (int 0x1a, int 0x21 get/set time), they don't need real values
			switch (numeral)
			{
				case 0x1a:
					processor.RegCount = 0;
					processor.RegData = 0;
					processor.CyMarker = false;
					break;
				case 0x21:
					processor.RegAcc = (ushort)(processor.RegAcc & 0xff00);
					processor.CyMarker = false;
					break;
			}
		}

		private ushort Invoke(int item, params ushort[] arguments)
		{
			lock (this.latch)
			{
				this.processor.SegStack = PileSection;
				this.processor.RegStack = PileHead;
				this.processor.InvokeFar(FetchSection, this.recordMarks[item], arguments);
				return this.processor.RegAcc;
			}
		}
	}
}
