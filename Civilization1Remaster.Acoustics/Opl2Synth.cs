using System;

namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private const double CircuitTimerRatio = 49716.0;

		private static readonly double[] ScalerLookup = { 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15 };

		private static readonly double[] PitchScaleLevelTable = { 0, 9, 12, 13.875, 15, 16.125, 16.875, 17.625, 18, 18.75, 19.125, 19.5, 19.875, 20.25, 20.625, 21 };

		private static readonly double[] PitchScaleLevelShift = { 0, 1.0 / 2, 1.0 / 4, 1 };

		private static readonly int[] LatchTowardOper = { 0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 8, 9, 10, 11, -1, -1, 12, 13, 14, 15, 16, 17, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

		private static readonly int[,] LaneOps = { { 0, 3 }, { 1, 4 }, { 2, 5 }, { 6, 9 }, { 7, 10 }, { 8, 11 }, { 12, 15 }, { 13, 16 }, { 14, 17 } };

		private readonly Operator[] ops = new Operator[18];
		private readonly Channel[] lanes = new Channel[9];
		private readonly byte[] latches = new byte[256];
		private readonly object latch = new object();
		private readonly int sliceRatio;

		private bool shapePick;
		private bool heavyWobble, heavyQuiver, drumsStyle;
		private int drumsTokens;
		private double wobbleStage, quiverStage;
		private uint noiseRegister = 1;

		private bool clock1Busy, clock2Busy, clock1Blocked, clock2Blocked;

		private byte condition;

		public Opl2Synth(int sliceRatio)
		{
			this.sliceRatio = sliceRatio;

			for (int idx = 0; idx < this.ops.Length; idx++)
				this.ops[idx] = new Operator();

			for (int idx = 0; idx < this.lanes.Length; idx++)
				this.lanes[idx] = new Channel();
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
						this.clock1Blocked = (amount & 0x40) != 0;
						this.clock2Blocked = (amount & 0x20) != 0;
						this.clock1Busy = (amount & 0x01) != 0;
						this.clock2Busy = (amount & 0x02) != 0;

						if (this.clock1Busy && !this.clock1Blocked) this.condition |= 0xc0;
						if (this.clock2Busy && !this.clock2Blocked) this.condition |= 0xa0;
					}
				}
				else if (latch >= 0x20 && latch < 0xa0 || latch >= 0xe0)
				{
					int oper = LatchTowardOper[latch & 0x1f];
					if (oper < 0)
						return;

					Operator objAlt = this.ops[oper];

					switch (latch & 0xe0)
					{
						case 0x20:
							objAlt.Wobble = (amount & 0x80) != 0;
							objAlt.Quiver = (amount & 0x40) != 0;
							objAlt.HoldMode = (amount & 0x20) != 0;
							objAlt.PitchScaleRate = (amount & 0x10) != 0;
							objAlt.Scaler = amount & 0xf;
							break;
						case 0x40:
							objAlt.PitchScaleLevel = amount >> 6;
							objAlt.OverallLevel = amount & 0x3f;
							break;
						case 0x60:
							objAlt.OnsetRate = amount >> 4;
							objAlt.FallRate = amount & 0xf;
							break;
						case 0x80:
							objAlt.HoldLevel = amount >> 4;
							objAlt.LetGoRate = amount & 0xf;
							break;
						case 0xe0:
							objAlt.Shape = amount & 3;
							break;
					}
				}
				else if (latch >= 0xa0 && latch <= 0xa8)
				{
					Channel chrAlt = this.lanes[latch - 0xa0];
					chrAlt.FldNumeral = (chrAlt.FldNumeral & 0x300) | amount;
				}
				else if (latch >= 0xb0 && latch <= 0xb8)
				{
					int ordinal = latch - 0xb0;
					Channel chrAlt = this.lanes[ordinal];
					chrAlt.FldNumeral = (chrAlt.FldNumeral & 0xff) | ((amount & 3) << 8);
					chrAlt.Chunk = (amount >> 2) & 7;
					bool noteOn = (amount & 0x20) != 0;

					if (noteOn != chrAlt.NoteOn)
					{
						chrAlt.NoteOn = noteOn;
						AssignKey(LaneOps[ordinal, 0], noteOn);
						AssignKey(LaneOps[ordinal, 1], noteOn);
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

					if ((altered & 0x10) != 0) { AssignKey(12, (tokens & 0x10) != 0); AssignKey(15, (tokens & 0x10) != 0); }
					if ((altered & 0x08) != 0) AssignKey(16, (tokens & 0x08) != 0);
					if ((altered & 0x04) != 0) AssignKey(14, (tokens & 0x04) != 0);
					if ((altered & 0x02) != 0) AssignKey(17, (tokens & 0x02) != 0);
					if ((altered & 0x01) != 0) AssignKey(13, (tokens & 0x01) != 0);
				}
				else if (latch >= 0xc0 && latch <= 0xc8)
				{
					Channel chrAlt = this.lanes[latch - 0xc0];
					chrAlt.Response = (amount >> 1) & 7;
					chrAlt.Summing = (amount & 1) != 0;
				}
			}
		}

		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				double millisEachSlice = 1000.0 / this.sliceRatio;

				for (int idx = 0; idx < tally; idx++)
				{
					this.wobbleStage += 3.7 / this.sliceRatio;
					this.quiverStage += 6.1 / this.sliceRatio;
					if (this.wobbleStage >= 1.0) this.wobbleStage -= 1.0;
					if (this.quiverStage >= 1.0) this.quiverStage -= 1.0;

					double wobble = (this.heavyWobble ? 4.8 : 1.0) * (0.5 - 0.5 * Math.Cos(this.wobbleStage * 2.0 * Math.PI));
					double quiver = Math.Pow(2.0, (this.heavyQuiver ? 14.0 : 7.0) / 1200.0 * Math.Sin(this.quiverStage * 2.0 * Math.PI));
					double hiss = AdvanceNoise();
					double blend = 0.0;

					int tunedLanes = this.drumsStyle ? 6 : 9;

					for (int chr = 0; chr < 9; chr++)
					{
						Channel lane = this.lanes[chr];
						Operator varier = this.ops[LaneOps[chr, 0]];
						Operator transport = this.ops[LaneOps[chr, 1]];

						RefreshEnvelope(varier, lane, millisEachSlice);
						RefreshEnvelope(transport, lane, millisEachSlice);

						if (chr < tunedLanes || chr == 6)
						{
							double response = lane.Response == 0 ? 0.0 :
								(varier.FinalEmitted1 + varier.FinalEmitted2) / 2.0 * Math.Pow(2.0, lane.Response - 6);
							double modifierResult = OperatorSignal(varier, lane, response, wobble);
							varier.FinalEmitted2 = varier.FinalEmitted1;
							varier.FinalEmitted1 = modifierResult;

							double emitted;
							if (lane.Summing && !(this.drumsStyle && chr == 6))
								emitted = modifierResult + OperatorSignal(transport, lane, 0.0, wobble);
							else
								emitted = OperatorSignal(transport, lane, modifierResult * 4.0, wobble);

							blend += this.drumsStyle && chr == 6 ? emitted * 2.0 : emitted;
						}
						else if (chr == 7)
						{
							double upCap = OperatorSignal(varier, lane, 0.0, wobble);
							double rattle = OperatorSignal(transport, lane, 0.0, wobble);
							blend += Math.Abs(upCap) * hiss * 0.7 + (rattle * 0.5 + Math.Abs(rattle) * hiss * 0.5);
						}
						else
						{
							double drum = OperatorSignal(varier, lane, 0.0, wobble);
							double crash = OperatorSignal(transport, lane, 0.0, wobble);
							blend += drum + Math.Abs(crash) * hiss * 0.6;
						}

						varier.Stage += OperatorPitchRatio(lane, varier, quiver) / this.sliceRatio;
						transport.Stage += OperatorPitchRatio(lane, transport, quiver) / this.sliceRatio;
						if (varier.Stage >= 1024.0) varier.Stage -= 1024.0;
						if (transport.Stage >= 1024.0) transport.Stage -= 1024.0;
					}

					int slice = (int)(blend * 5000.0);
					staging[displacement + idx] = (short)Math.Clamp(slice, short.MinValue, short.MaxValue);
				}
			}
		}

		private static double RateDuration(int ratio, int pitchScaleOffset, bool assault)
		{
			if (ratio == 0)
				return double.PositiveInfinity;

			int real = Math.Min(63, ratio * 4 + pitchScaleOffset);

			if (assault && real >= 60)
				return 0.0;

			double rootMoment = assault ? 2826.24 : 39280.64;
			return rootMoment / Math.Pow(2.0, (real - 4) / 4.0);
		}

		private static double WaveShape(int shape, double stage)
		{
			stage -= Math.Floor(stage);
			double sinus = Math.Sin(stage * 2.0 * Math.PI);

			switch (shape)
			{
				case 1: return stage < 0.5 ? sinus : 0.0;
				case 2: return Math.Abs(sinus);
				case 3: return (stage % 0.5) < 0.25 ? Math.Abs(sinus) : 0.0;
				default: return sinus;
			}
		}

		private void AssignKey(int oper, bool enabled)
		{
			Operator objAlt = this.ops[oper];

			if (enabled && !objAlt.NoteOn)
			{
				objAlt.Stage = 0;
				objAlt.Phase = EnvelopeStage.Attack;
			}
			else if (!enabled && objAlt.NoteOn)
			{
				if (objAlt.Phase != EnvelopeStage.Off)
					objAlt.Phase = EnvelopeStage.Release;
			}

			objAlt.NoteOn = enabled;
		}

		private double OperatorPitchRatio(Channel lane, Operator oper, double quiver)
		{
			double hertz = lane.FldNumeral * CircuitTimerRatio / Math.Pow(2.0, 20 - lane.Chunk);
			hertz *= ScalerLookup[oper.Scaler];

			if (oper.Quiver)
				hertz *= quiver;

			return hertz;
		}

		private void RefreshEnvelope(Operator oper, Channel lane, double millisEachSlice)
		{
			int pitchScaleOffset = oper.PitchScaleRate ? (lane.Chunk << 1) | ((lane.FldNumeral >> 9) & 1) : lane.Chunk >> 1;

			switch (oper.Phase)
			{
				case EnvelopeStage.Off:
					oper.Shaper = 96.0;
					return;
				case EnvelopeStage.Attack:
					{
						double moment = RateDuration(oper.OnsetRate, pitchScaleOffset, true);
						if (moment <= 0.0)
						{
							oper.Shaper = 0.0;
						}
						else if (!double.IsInfinity(moment))
						{
							double advance = 96.0 * millisEachSlice / moment;
							oper.Shaper -= advance * (1.0 + oper.Shaper / 12.0) / 3.0;
						}

						if (oper.Shaper <= 0.0)
						{
							oper.Shaper = 0.0;
							oper.Phase = EnvelopeStage.Decay;
						}
					}
					return;
				case EnvelopeStage.Decay:
					{
						double holdLevel = oper.HoldLevel == 15 ? 93.0 : oper.HoldLevel * 3.0;
						double moment = RateDuration(oper.FallRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
							oper.Shaper += 96.0 * millisEachSlice / moment;

						if (oper.Shaper >= holdLevel)
						{
							oper.Shaper = holdLevel;
							oper.Phase = EnvelopeStage.Sustain;
						}
					}
					return;
				case EnvelopeStage.Sustain:
					if (!oper.HoldMode)
					{
						double moment = RateDuration(oper.LetGoRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
							oper.Shaper += 96.0 * millisEachSlice / moment;
					}
					break;
				case EnvelopeStage.Release:
					{
						double moment = RateDuration(oper.LetGoRate, pitchScaleOffset, false);
						if (!double.IsInfinity(moment))
							oper.Shaper += 96.0 * millisEachSlice / moment;
					}
					break;
			}

			if (oper.Shaper >= 96.0)
			{
				oper.Shaper = 96.0;
				if (oper.Phase == EnvelopeStage.Release)
					oper.Phase = EnvelopeStage.Off;
			}
		}

		private double OperatorSignal(Operator oper, Channel lane, double variation, double wobble)
		{
			if (oper.Shaper >= 96.0)
				return 0.0;

			double pitchScaleBits = PitchScaleLevelTable[lane.FldNumeral >> 6] - (7 - lane.Chunk) * 6.0;
			double pitchScale = pitchScaleBits > 0 ? pitchScaleBits * PitchScaleLevelShift[oper.PitchScaleLevel] : 0.0;
			double damping = oper.Shaper + oper.OverallLevel * 0.75 + pitchScale + (oper.Wobble ? wobble : 0.0);

			if (damping >= 96.0)
				return 0.0;

			double loudness = Math.Pow(10.0, -damping / 20.0);
			int shape = this.shapePick ? oper.Shape : 0;

			return WaveShape(shape, oper.Stage + variation) * loudness;
		}

		private double AdvanceNoise()
		{
			uint binary = ((this.noiseRegister >> 22) ^ (this.noiseRegister >> 8)) & 1;
			this.noiseRegister = ((this.noiseRegister << 1) | binary) & 0x7fffff;
			return (this.noiseRegister & 1) != 0 ? 1.0 : -1.0;
		}
	}
}
