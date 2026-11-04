using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public class TandySoundSynthesizer
	{
		private const double BaseClock = 3579545.0;
		private const double VoiceBaseFrequency = BaseClock / 32.0;
		private const int VoiceCount = 4;
		private const int ToneVoiceCount = 3;
		private const int NoiseVoiceIndex = 3;

		private static readonly double[] AttenuationTable = new double[]
		{
			1.0,
			0.794328,
			0.630957,
			0.501187,
			0.398107,
			0.316228,
			0.251189,
			0.199526,
			0.158489,
			0.125893,
			0.100000,
			0.079433,
			0.063096,
			0.050119,
			0.039811,
			0.0,
		};

		private readonly object latch = new object();
		private readonly int sliceRatio;
		private readonly int[] divisors = new int[ToneVoiceCount];
		private readonly int[] attenuations = new int[VoiceCount] { 15, 15, 15, 15 };
		private readonly double[] stages = new double[ToneVoiceCount];

		private int latchedChannel;
		private bool latchedAttenuation;
		private int noiseControl = 0x03;
		private int noiseShiftRegister = 0x4000;
		private double noiseStage;
		private double filterTier;

		public TandySoundSynthesizer(int sliceRatio)
		{
			this.sliceRatio = sliceRatio;
		}

		public void EmitPort(ushort gate, byte amount)
		{
			lock (this.latch)
			{
				if ((amount & 0x80) != 0)
				{
					this.latchedChannel = (amount >> 5) & 0x03;
					this.latchedAttenuation = (amount & 0x10) != 0;

					if (this.latchedAttenuation)
					{
						this.attenuations[this.latchedChannel] = amount & 0x0f;
					}
					else if (this.latchedChannel < ToneVoiceCount)
					{
						this.divisors[this.latchedChannel] = (this.divisors[this.latchedChannel] & 0x3f0) | (amount & 0x0f);
					}
					else
					{
						this.noiseControl = amount & 0x07;
						this.noiseShiftRegister = 0x4000;
					}
				}
				else
				{
					if (this.latchedAttenuation)
					{
						this.attenuations[this.latchedChannel] = amount & 0x0f;
					}
					else if (this.latchedChannel < ToneVoiceCount)
					{
						this.divisors[this.latchedChannel] = (this.divisors[this.latchedChannel] & 0x0f) | ((amount & 0x3f) << 4);
					}
				}
			}
		}

		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				for (int index = 0; index < tally; index++)
				{
					double compositeSample = 0.0;

					for (int channel = 0; channel < ToneVoiceCount; channel++)
					{
						int divisor = this.divisors[channel];
						int attenuation = this.attenuations[channel];

						if (attenuation < 15 && divisor > 1)
						{
							double frequency = VoiceBaseFrequency / divisor;
							if (frequency < this.sliceRatio / 2)
							{
								this.stages[channel] += frequency / this.sliceRatio;
								this.stages[channel] -= Math.Floor(this.stages[channel]);
								double wave = this.stages[channel] < 0.5 ? 1.0 : -1.0;
								compositeSample += wave * AttenuationTable[attenuation];
							}
						}
					}

					int noiseAttenuation = this.attenuations[NoiseVoiceIndex];
					if (noiseAttenuation < 15)
					{
						double noiseFrequency = ObtainNoiseFrequency();
						if (noiseFrequency > 0.0 && noiseFrequency < this.sliceRatio / 2)
						{
							this.noiseStage += noiseFrequency / this.sliceRatio;
							while (this.noiseStage >= 1.0)
							{
								this.noiseStage -= 1.0;
								StepNoise();
							}

							double noiseWave = (this.noiseShiftRegister & 1) != 0 ? 1.0 : -1.0;
							compositeSample += noiseWave * AttenuationTable[noiseAttenuation];
						}
					}

					this.filterTier += (compositeSample - this.filterTier) * 0.4;
					int sample = staging[displacement + index] + (int)(this.filterTier * 2800.0);
					staging[displacement + index] = (short)Math.Clamp(sample, short.MinValue, short.MaxValue);
				}
			}
		}

		private double ObtainNoiseFrequency()
		{
			int rateSelect = this.noiseControl & 0x03;
			return rateSelect switch
			{
				0 => VoiceBaseFrequency / 16.0,
				1 => VoiceBaseFrequency / 32.0,
				2 => VoiceBaseFrequency / 64.0,
				_ => this.divisors[2] > 1 ? VoiceBaseFrequency / this.divisors[2] : 0.0,
			};
		}

		private void StepNoise()
		{
			bool isWhiteNoise = (this.noiseControl & 0x04) != 0;
			int feedback;

			if (isWhiteNoise)
			{
				feedback = (this.noiseShiftRegister & 1) ^ ((this.noiseShiftRegister >> 1) & 1);
			}
			else
			{
				feedback = this.noiseShiftRegister & 1;
			}

			this.noiseShiftRegister = (this.noiseShiftRegister >> 1) | (feedback << 14);
		}
	}
}
