using System;
using System.Collections.Generic;
using System.IO;
using Civ_1_5_Remastered.Acoustics.Synthesis;

namespace Civ_1_5_Remastered.Acoustics.Drivers
{
	public class SoundDriverHost
	{
		public const int AdvanceRatio = 60;

		public const int BeatRatio = VaryRatio;

		private const ushort FetchSection = 0x1000;

		private const ushort PileSection = 0x9000;
		private const ushort PileHead = 0xfff0;
		private const int RecordLookupDisplacement = 0x32;
		private const int RecordTally = 6;
		private const ushort FrequencyModulationCardLocationGate = 0x388;
		private const ushort FrequencyModulationCardContentGate = 0x389;
		private const ushort SynthesizerContentGate = 0x330;
		private const ushort SynthesizerConditionGate = 0x331;
		private const int VaryEachAdvance = 5;
		private const int VaryRatio = AdvanceRatio * VaryEachAdvance;

		private readonly MachineCodeInterpreter processor = new MachineCodeInterpreter();
		private readonly ushort[] recordMarks = new ushort[RecordTally];
		private readonly object latch = new object();

		private int frequencyModulationCardLatch;

		private bool synthesizerConfirm;

		private int slicesTowardVary;
		private int sliceLeftover;
		private int varyTally;

		private int impactBeat = -1;

		private short[]? impactSlices;
		private int impactPlace = -1;

		public SoundDriverHost(byte[] handlerDocument, int sliceRatio, string label)
		{
			this.Label = label;
			this.FrequencyModulation = new FrequencyModulationSynthesizer(sliceRatio);
			this.Buzzer = new BuiltInSpeaker(sliceRatio);
			this.Synthesizer = new RolandToGeneralMidi((condition, firstContent, secondContent) => { });
			this.TandySound = new TandySoundSynthesizer(sliceRatio);

			ImportExecutable(handlerDocument);

			for (int index = 0; index < RecordTally; index++)
			{
				this.recordMarks[index] = this.processor.PullWord(FetchSection, (ushort)(RecordLookupDisplacement + (index * 2)));
			}

			this.processor.GatePull = PullPort;
			this.processor.GateEmit = EmitPort;
			this.processor.Signal = ManageInterrupt;
			this.processor.SegmentStack = PileSection;
			this.processor.RegisterStack = PileHead;
		}

		public string Label { get; }
		public FrequencyModulationSynthesizer FrequencyModulation { get; }
		public BuiltInSpeaker Buzzer { get; }

		public RolandToGeneralMidi Synthesizer { get; }
		public TandySoundSynthesizer TandySound { get; }

		public bool HasSynthesizer => string.Equals(this.Label, "RSOUND.CVL", StringComparison.OrdinalIgnoreCase);

		public static IEnumerable<SoundDriverHost> ConstructAll(string sessionFolder, int sliceRatio, string? preference = null)
		{
			string? chosenOption = preference ?? Environment.GetEnvironmentVariable("CIVILIZATION1REMASTER_SOUND");
			foreach (string document in DriverFileNames(chosenOption))
			{
				string? route = LocateFile(sessionFolder, document);

				if (route != null)
				{
					yield return new SoundDriverHost(File.ReadAllBytes(route), sliceRatio, document);
				}
			}
		}

		public static string[] DriverFileNames(string? option)
		{
			switch (option?.Trim().ToLowerInvariant())
			{
				case "none":
				case "rawmidi":
				case "generalmidi":
				case "mid":
					return Array.Empty<string>();
				case "speaker":
				case "ibm":
					return new[] { "ISOUND.CVL" };
				case "tandy":
					return new[] { "TSOUND.CVL", "ISOUND.CVL" };
				case "adlib":
				case "soundblaster":
					return new[] { "ASOUND.CVL" };
				case "asound2":
				case "adlib2":
				case "fast486":
				case "asound3":
				case "adlib3":
				case "ym262":
					return new[] { "ASOUND2.CVL", "ASOUND3.CVL", "ASOUND.CVL" };
				case "roland":
				case "mt32":
				case "midi":
					return new[] { "RSOUND.CVL" };
				case "custom":
				default:
					return new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" };
			}
		}

		public ushort Prepare() => Invoke(0, 0);

		public void Perform(ushort audio, ushort argument) => Invoke(1, audio, argument);

		public ushort Halt() => Invoke(2);

		public ushort Advance() => Invoke(3);

		public void Vary() => Invoke(4);

		public ushort CurrentOffset() => Invoke(5);

		public void PerformChopping()
		{
			lock (this.latch)
			{
				if (this.HasSynthesizer)
				{
					this.impactBeat = 0;
				}
				else
				{
					this.impactSlices ??= ChoppingEffect.Synthesize(this.FrequencyModulation.SliceRatio);
					this.impactPlace = 0;
				}
			}
		}

		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				this.FrequencyModulation.Produce(staging, displacement, tally);
				this.Buzzer.Produce(staging, displacement, tally);
				this.TandySound.Produce(staging, displacement, tally);

				if (this.impactSlices != null && this.impactPlace >= 0)
				{
					for (int index = 0; index < tally && this.impactPlace < this.impactSlices.Length; index++, this.impactPlace++)
					{
						staging[displacement + index] = (short)Math.Clamp(staging[displacement + index] + this.impactSlices[this.impactPlace], short.MinValue, short.MaxValue);
					}

					if (this.impactPlace >= this.impactSlices.Length)
					{
						this.impactPlace = -1;
					}
				}
			}
		}

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
					this.impactBeat = ChoppingEffect.MidiTick(this.impactBeat, this.Synthesizer.Emitted) ? this.impactBeat + 1 : -1;
				}
			}
		}

		public void Mix(short[] staging, int displacement, int tally)
		{
			int sliceRatio = this.FrequencyModulation.SliceRatio;

			while (tally > 0)
			{
				if (this.slicesTowardVary <= 0)
				{
					Beat();

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
			{
				folder = ".";
			}

			string[] candidateFolders = new[]
			{
				folder,
				Path.Combine(folder, "Assets", "Drivers"),
				Path.Combine(folder, "Drivers"),
				Path.Combine(AppContext.BaseDirectory, "Assets", "Drivers"),
				AppContext.BaseDirectory,
			};

			foreach (string candidateFolder in candidateFolders)
			{
				if (Directory.Exists(candidateFolder))
				{
					string directPath = Path.Combine(candidateFolder, label);
					if (File.Exists(directPath))
					{
						return directPath;
					}

					foreach (string route in Directory.GetFiles(candidateFolder))
					{
						if (string.Equals(Path.GetFileName(route), label, StringComparison.OrdinalIgnoreCase))
						{
							return route;
						}
					}
				}
			}

			return null;
		}

		private void ImportExecutable(byte[] document)
		{
			if (document.Length < 0x20 || document[0] != 'M' || document[1] != 'Z')
			{
				throw new InvalidDataException("Sound driver is not a DOS executable");
			}

			int finalSheetOctets = BitConverter.ToUInt16(document, 2);
			int leaves = BitConverter.ToUInt16(document, 4);
			int rebaseTally = BitConverter.ToUInt16(document, 6);
			int preambleExtent = BitConverter.ToUInt16(document, 8) * 16;
			int rebaseDisplacement = BitConverter.ToUInt16(document, 0x18);
			int visualExtent = (leaves * 512) - (finalSheetOctets != 0 ? 512 - finalSheetOctets : 0) - preambleExtent;
			visualExtent = Math.Min(visualExtent, document.Length - preambleExtent);

			Array.Copy(document, preambleExtent, this.processor.Storage, FetchSection << 4, visualExtent);

			for (int index = 0; index < rebaseTally; index++)
			{
				ushort displacement = BitConverter.ToUInt16(document, rebaseDisplacement + (index * 4));
				ushort section = BitConverter.ToUInt16(document, rebaseDisplacement + (index * 4) + 2);
				ushort patchSection = (ushort)(FetchSection + section);
				this.processor.EmitWord(patchSection, displacement, (ushort)(this.processor.PullWord(patchSection, displacement) + FetchSection));
			}
		}

		private byte PullPort(ushort gate)
		{
			switch (gate)
			{
				case FrequencyModulationCardLocationGate:
					return this.FrequencyModulation.PullStatus();
				case SynthesizerConditionGate:
					return (byte)(this.synthesizerConfirm ? 0x00 : 0x80);
				case SynthesizerContentGate:
					this.synthesizerConfirm = false;
					return 0xfe;
				case 0x61:
					return this.Buzzer.SpeakerGate;
				default:
					return 0xff;
			}
		}

		private void EmitPort(ushort gate, byte amount)
		{
			switch (gate)
			{
				case FrequencyModulationCardLocationGate:
					this.frequencyModulationCardLatch = amount;
					break;
				case FrequencyModulationCardContentGate:
					this.FrequencyModulation.EmitRegister(this.frequencyModulationCardLatch, amount);
					break;
				case SynthesizerConditionGate:
					this.synthesizerConfirm = true;
					break;
				case SynthesizerContentGate:
					this.Synthesizer.Emit(amount);
					break;
				case 0x42:
				case 0x43:
				case 0x61:
					this.Buzzer.EmitPort(gate, amount);
					break;
				case 0xc0:
				case 0xc1:
				case 0xc2:
				case 0xc3:
				case 0xc4:
				case 0xc5:
				case 0xc6:
				case 0xc7:
				case 0x1e0:
				case 0x1e1:
					this.TandySound.EmitPort(gate, amount);
					break;
			}
		}

		private void ManageInterrupt(MachineCodeInterpreter processor, byte numeral)
		{
			switch (numeral)
			{
				case 0x1a:
					processor.RegisterCount = 0;
					processor.RegisterData = 0;
					processor.CarryMarker = false;
					break;
				case 0x21:
					processor.RegisterAccumulator = (ushort)(processor.RegisterAccumulator & 0xff00);
					processor.CarryMarker = false;
					break;
			}
		}

		private ushort Invoke(int item, params ushort[] arguments)
		{
			lock (this.latch)
			{
				this.processor.SegmentStack = PileSection;
				this.processor.RegisterStack = PileHead;
				this.processor.InvokeFar(FetchSection, this.recordMarks[item], arguments);
				return this.processor.RegisterAccumulator;
			}
		}
	}
}
