using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public partial class FrequencyModulationSynthesizer
	{
		private const double CircuitTimerRatio = 49716.0;

		private static readonly double[] ScalerLookup = { 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15 };

		private static readonly double[] PitchScaleLevelTable = { 0, 9, 12, 13.875, 15, 16.125, 16.875, 17.625, 18, 18.75, 19.125, 19.5, 19.875, 20.25, 20.625, 21 };

		private static readonly double[] PitchScaleLevelShift = { 0, 1.0 / 2, 1.0 / 4, 1 };

		private static readonly int[] LatchTowardOperation = { 0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 8, 9, 10, 11, -1, -1, 12, 13, 14, 15, 16, 17, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

		private static readonly int[,] LaneOperations =
		{
			{ 0, 3 },
			{ 1, 4 },
			{ 2, 5 },
			{ 6, 9 },
			{ 7, 10 },
			{ 8, 11 },
			{ 12, 15 },
			{ 13, 16 },
			{ 14, 17 }
		};

		private readonly Operator[] operations = new Operator[18];
		private readonly Channel[] lanes = new Channel[9];
		private readonly byte[] latches = new byte[256];
		private readonly object latch = new object();
		private readonly int sliceRatio;

		private bool shapePick;
		private bool heavyWobble;
		private bool heavyQuiver;
		private bool drumsStyle;
		private int drumsTokens;
		private double wobbleStage;
		private double quiverStage;
		private uint noiseRegister = 1;

		private bool clockFirstBusy;
		private bool clockSecondBusy;
		private bool clockFirstBlocked;
		private bool clockSecondBlocked;

		private byte condition;

		public FrequencyModulationSynthesizer(int sliceRatio)
		{
			this.sliceRatio = sliceRatio;

			for (int index = 0; index < this.operations.Length; index++)
			{
				this.operations[index] = new Operator();
			}

			for (int index = 0; index < this.lanes.Length; index++)
			{
				this.lanes[index] = new Channel();
			}
		}

		public int SliceRatio => this.sliceRatio;

		public int LatchEmitTally { get; private set; }

		public byte PullStatus()
		{
			lock (this.latch)
			{
				return this.condition;
			}
		}

		public void EmitRegister(int latch, byte amount)
		{
			lock (this.latch)
			{
				this.LatchEmitTally++;
				latch &= 0xff;
				this.latches[latch] = amount;

				if (latch == 0x01)
				{
					this.shapePick = (amount & 0x20) != 0;
				}
				else if (latch == 0x04)
				{
					if ((amount & 0x80) != 0)
					{
						this.condition = 0;
					}
					else
					{
						this.clockFirstBlocked = (amount & 0x40) != 0;
						this.clockSecondBlocked = (amount & 0x20) != 0;
						this.clockFirstBusy = (amount & 0x01) != 0;
						this.clockSecondBusy = (amount & 0x02) != 0;

						if (this.clockFirstBusy && !this.clockFirstBlocked)
						{
							this.condition |= 0xc0;
						}

						if (this.clockSecondBusy && !this.clockSecondBlocked)
						{
							this.condition |= 0xa0;
						}
					}
				}
				else if ((latch >= 0x20 && latch < 0xa0) || latch >= 0xe0)
				{
					int operation = LatchTowardOperation[latch & 0x1f];
					if (operation < 0)
					{
						return;
					}

					Operator instanceAlternate = this.operations[operation];

					switch (latch & 0xe0)
					{
						case 0x20:
							instanceAlternate.Wobble = (amount & 0x80) != 0;
							instanceAlternate.Quiver = (amount & 0x40) != 0;
							instanceAlternate.HoldMode = (amount & 0x20) != 0;
							instanceAlternate.PitchScaleRate = (amount & 0x10) != 0;
							instanceAlternate.Scaler = amount & 0xf;
							break;
						case 0x40:
							instanceAlternate.PitchScaleLevel = amount >> 6;
							instanceAlternate.OverallLevel = amount & 0x3f;
							break;
						case 0x60:
							instanceAlternate.OnsetRate = amount >> 4;
							instanceAlternate.FallRate = amount & 0xf;
							break;
						case 0x80:
							instanceAlternate.HoldLevel = amount >> 4;
							instanceAlternate.ReleaseRate = amount & 0xf;
							break;
						case 0xe0:
							instanceAlternate.Shape = amount & 3;
							break;
					}
				}
				else if (latch >= 0xa0 && latch <= 0xa8)
				{
					Channel characterAlternate = this.lanes[latch - 0xa0];
					characterAlternate.FieldNumeral = (characterAlternate.FieldNumeral & 0x300) | amount;
				}
				else if (latch >= 0xb0 && latch <= 0xb8)
				{
					int ordinal = latch - 0xb0;
					Channel characterAlternate = this.lanes[ordinal];
					characterAlternate.FieldNumeral = (characterAlternate.FieldNumeral & 0xff) | ((amount & 3) << 8);
					characterAlternate.Chunk = (amount >> 2) & 7;
					bool noteOn = (amount & 0x20) != 0;

					if (noteOn != characterAlternate.NoteOn)
					{
						characterAlternate.NoteOn = noteOn;
						AssignKey(LaneOperations[ordinal, 0], noteOn);
						AssignKey(LaneOperations[ordinal, 1], noteOn);
					}
				}
				else if (latch == 0xbd)
				{
					this.heavyWobble = (amount & 0x80) != 0;
					this.heavyQuiver = (amount & 0x40) != 0;
					this.drumsStyle = (amount & 0x20) != 0;

					int tokens = this.drumsStyle ? amount & 0x1f : 0;
					int altered = tokens ^ this.drumsTokens;
					this.drumsTokens = tokens;

					if ((altered & 0x10) != 0)
					{
						AssignKey(12, (tokens & 0x10) != 0);
						AssignKey(15, (tokens & 0x10) != 0);
					}

					if ((altered & 0x08) != 0)
					{
						AssignKey(16, (tokens & 0x08) != 0);
					}

					if ((altered & 0x04) != 0)
					{
						AssignKey(14, (tokens & 0x04) != 0);
					}

					if ((altered & 0x02) != 0)
					{
						AssignKey(17, (tokens & 0x02) != 0);
					}

					if ((altered & 0x01) != 0)
					{
						AssignKey(13, (tokens & 0x01) != 0);
					}
				}
				else if (latch >= 0xc0 && latch <= 0xc8)
				{
					Channel characterAlternate = this.lanes[latch - 0xc0];
					characterAlternate.Response = (amount >> 1) & 7;
					characterAlternate.Summing = (amount & 1) != 0;
				}
			}
		}

		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				double millisecondsEachSlice = 1000.0 / this.sliceRatio;

				for (int index = 0; index < tally; index++)
				{
					this.wobbleStage += 3.7 / this.sliceRatio;
					this.quiverStage += 6.1 / this.sliceRatio;
					if (this.wobbleStage >= 1.0)
					{
						this.wobbleStage -= 1.0;
					}

					if (this.quiverStage >= 1.0)
					{
						this.quiverStage -= 1.0;
					}

					double wobble = (this.heavyWobble ? 4.8 : 1.0) * (0.5 - (0.5 * Math.Cos(this.wobbleStage * 2.0 * Math.PI)));
					double quiver = Math.Pow(2.0, (this.heavyQuiver ? 14.0 : 7.0) / 1200.0 * Math.Sin(this.quiverStage * 2.0 * Math.PI));
					double hiss = AdvanceNoise();
					double blend = 0.0;

					int tunedLanes = this.drumsStyle ? 6 : 9;

					for (int character = 0; character < 9; character++)
					{
						Channel lane = this.lanes[character];
						Operator varier = this.operations[LaneOperations[character, 0]];
						Operator transport = this.operations[LaneOperations[character, 1]];

						RefreshEnvelope(varier, lane, millisecondsEachSlice);
						RefreshEnvelope(transport, lane, millisecondsEachSlice);

						if (character < tunedLanes || character == 6)
						{
							double response = lane.Response == 0 ? 0.0 : (varier.FirstFinalEmitted + varier.SecondFinalEmitted) / 2.0 * Math.Pow(2.0, lane.Response - 6);
							double modifierResult = OperatorSignal(varier, lane, response, wobble);
							varier.SecondFinalEmitted = varier.FirstFinalEmitted;
							varier.FirstFinalEmitted = modifierResult;

							double emitted;
							if (lane.Summing && !(this.drumsStyle && character == 6))
							{
								emitted = modifierResult + OperatorSignal(transport, lane, 0.0, wobble);
							}
							else
							{
								emitted = OperatorSignal(transport, lane, modifierResult * 4.0, wobble);
							}

							blend += this.drumsStyle && character == 6 ? emitted * 2.0 : emitted;
						}
						else if (character == 7)
						{
							double upCap = OperatorSignal(varier, lane, 0.0, wobble);
							double rattle = OperatorSignal(transport, lane, 0.0, wobble);
							blend += (Math.Abs(upCap) * hiss * 0.7) + ((rattle * 0.5) + (Math.Abs(rattle) * hiss * 0.5));
						}
						else
						{
							double drum = OperatorSignal(varier, lane, 0.0, wobble);
							double crash = OperatorSignal(transport, lane, 0.0, wobble);
							blend += drum + (Math.Abs(crash) * hiss * 0.6);
						}

						varier.Stage += OperatorPitchRatio(lane, varier, quiver) / this.sliceRatio;
						transport.Stage += OperatorPitchRatio(lane, transport, quiver) / this.sliceRatio;
						if (varier.Stage >= 1024.0)
						{
							varier.Stage -= 1024.0;
						}

						if (transport.Stage >= 1024.0)
						{
							transport.Stage -= 1024.0;
						}
					}

					int slice = (int)(blend * 5000.0);
					staging[displacement + index] = (short)Math.Clamp(slice, short.MinValue, short.MaxValue);
				}
			}
		}

		private static double RateDuration(int ratio, int pitchScaleOffset, bool assault)
		{
			if (ratio == 0)
			{
				return double.PositiveInfinity;
			}

			int real = Math.Min(63, (ratio * 4) + pitchScaleOffset);

			if (assault && real >= 60)
			{
				return 0.0;
			}

			double rootMoment = assault ? 2826.24 : 39280.64;
			return rootMoment / Math.Pow(2.0, (real - 4) / 4.0);
		}

		private static double WaveShape(int shape, double stage)
		{
			stage -= Math.Floor(stage);
			double sine = Math.Sin(stage * 2.0 * Math.PI);

			switch (shape)
			{
				case 1: return stage < 0.5 ? sine : 0.0;
				case 2: return Math.Abs(sine);
				case 3: return (stage % 0.5) < 0.25 ? Math.Abs(sine) : 0.0;
				default: return sine;
			}
		}

		private void AssignKey(int operation, bool enabled)
		{
			Operator instanceAlternate = this.operations[operation];

			if (enabled && !instanceAlternate.NoteOn)
			{
				instanceAlternate.Stage = 0;
				instanceAlternate.Phase = EnvelopeStage.Attack;
			}
			else if (!enabled && instanceAlternate.NoteOn)
			{
				if (instanceAlternate.Phase != EnvelopeStage.Off)
				{
					instanceAlternate.Phase = EnvelopeStage.Release;
				}
			}

			instanceAlternate.NoteOn = enabled;
		}

		private double OperatorPitchRatio(Channel lane, Operator operation, double quiver)
		{
			double hertz = lane.FieldNumeral * CircuitTimerRatio / Math.Pow(2.0, 20 - lane.Chunk);
			hertz *= ScalerLookup[operation.Scaler];

			if (operation.Quiver)
			{
				hertz *= quiver;
			}

			return hertz;
		}

		private void RefreshEnvelope(Operator operation, Channel lane, double millisecondsEachSlice)
		{
			int pitchScaleOffset = operation.PitchScaleRate ? (lane.Chunk << 1) | ((lane.FieldNumeral >> 9) & 1) : lane.Chunk >> 1;

			switch (operation.Phase)
			{
				case EnvelopeStage.Off:
					operation.Shaper = 96.0;
					return;
				case EnvelopeStage.Attack:
					{
						double moment = RateDuration(operation.OnsetRate, pitchScaleOffset, true);
						if (moment <= 0.0)
						{
							operation.Shaper = 0.0;
						}
						else if (!double.IsInfinity(moment))
						{
							double advance = 96.0 * millisecondsEachSlice / moment;
							operation.Shaper -= advance * (1.0 + (operation.Shaper / 12.0)) / 3.0;
						}

						if (operation.Shaper <= 0.0)
						{
							operation.Shaper = 0.0;
							operation.Phase = EnvelopeStage.Decay;
						}
					}

					return;
				case EnvelopeStage.Decay:
					{
						double holdLevel = operation.HoldLevel == 15 ? 93.0 : operation.HoldLevel * 3.0;
						double moment = RateDuration(operation.FallRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
						{
							operation.Shaper += 96.0 * millisecondsEachSlice / moment;
						}

						if (operation.Shaper >= holdLevel)
						{
							operation.Shaper = holdLevel;
							operation.Phase = EnvelopeStage.Sustain;
						}
					}

					return;
				case EnvelopeStage.Sustain:
					if (!operation.HoldMode)
					{
						double moment = RateDuration(operation.ReleaseRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
						{
							operation.Shaper += 96.0 * millisecondsEachSlice / moment;
						}
					}

					break;
				case EnvelopeStage.Release:
					{
						double moment = RateDuration(operation.ReleaseRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
						{
							operation.Shaper += 96.0 * millisecondsEachSlice / moment;
						}
					}

					break;
			}

			if (operation.Shaper >= 96.0)
			{
				operation.Shaper = 96.0;
				if (operation.Phase == EnvelopeStage.Release)
				{
					operation.Phase = EnvelopeStage.Off;
				}
			}
		}

		private double OperatorSignal(Operator operation, Channel lane, double variation, double wobble)
		{
			if (operation.Shaper >= 96.0)
			{
				return 0.0;
			}

			double pitchScaleBits = PitchScaleLevelTable[lane.FieldNumeral >> 6] - ((7 - lane.Chunk) * 6.0);
			double pitchScale = pitchScaleBits > 0 ? pitchScaleBits * PitchScaleLevelShift[operation.PitchScaleLevel] : 0.0;
			double damping = operation.Shaper + (operation.OverallLevel * 0.75) + pitchScale + (operation.Wobble ? wobble : 0.0);

			if (damping >= 96.0)
			{
				return 0.0;
			}

			double loudness = Math.Pow(10.0, -damping / 20.0);
			int shape = this.shapePick ? operation.Shape : 0;

			return WaveShape(shape, operation.Stage + variation) * loudness;
		}

		private double AdvanceNoise()
		{
			uint binary = ((this.noiseRegister >> 22) ^ (this.noiseRegister >> 8)) & 1;
			this.noiseRegister = ((this.noiseRegister << 1) | binary) & 0x7fffff;
			return (this.noiseRegister & 1) != 0 ? 1.0 : -1.0;
		}
	}
}
