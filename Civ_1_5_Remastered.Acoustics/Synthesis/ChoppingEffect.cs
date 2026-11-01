using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public static partial class ChoppingEffect
	{
		private const byte DrumsLane = 9;
		private const byte LowerTimberChunk = 77;
		private const byte LowerGroundDrum = 41;

		private static readonly double[] StrikeMoments = { 0.0, 0.43, 0.82, 1.28, 1.66 };

		private static readonly double[] StrikePower = { 0.85, 0.75, 0.9, 0.8, 1.0 };

		public static bool MidiTick(int beat, Action<byte, byte, byte> emitted)
		{
			bool sounding = false;

			for (int index = 0; index < StrikeMoments.Length; index++)
			{
				int begin = (int)(StrikeMoments[index] * SoundDriverHost.BeatRatio);
				int finish = begin + SoundDriverHost.BeatRatio / 10;
				byte pace = (byte)(127 * StrikePower[index]);

				if (beat == begin)
				{
					emitted((byte)(0x90 | DrumsLane), LowerTimberChunk, pace);
					emitted((byte)(0x90 | DrumsLane), LowerGroundDrum, (byte)(pace * 2 / 3));
				}
				else if (beat == finish)
				{
					emitted((byte)(0x80 | DrumsLane), LowerTimberChunk, 0);
					emitted((byte)(0x80 | DrumsLane), LowerGroundDrum, 0);
				}

				if (beat < finish)
					sounding = true;
			}

			return sounding;
		}

		public static short[] Synthesize(int sliceRatio)
		{
			double span = StrikeMoments[StrikeMoments.Length - 1] + 0.4;
			double[] blend = new double[(int)(span * sliceRatio)];
			Random chance = new Random(1234);

			for (int index = 0; index < StrikeMoments.Length; index++)
			{
				int begin = (int)(StrikeMoments[index] * sliceRatio);
				double power = StrikePower[index];
				int strikeSpan = Math.Min((int)(0.3 * sliceRatio), blend.Length - begin);

				Resonator torso = new Resonator(sliceRatio, 650.0 + 40.0 * index, 0.9965);
				Resonator fissure = new Resonator(sliceRatio, 2300.0 - 60.0 * index, 0.992);
				double thudStage = 0.0;

				for (int count = 0; count < strikeSpan; count++)
				{
					double temporary = (double)count / sliceRatio;
					double hiss = chance.NextDouble() * 2.0 - 1.0;
					double stimulus = hiss * Math.Exp(-temporary / 0.004);

					double timber = torso.Handle(stimulus) * 2.0 + fissure.Handle(stimulus) * 1.0;
					double staticRest = hiss * 0.18 * Math.Exp(-temporary / 0.018);

					double thudHertz = 70.0 + 70.0 * Math.Exp(-temporary / 0.03);
					thudStage += 2.0 * Math.PI * thudHertz / sliceRatio;
					double thud = Math.Sin(thudStage) * 0.55 * Math.Exp(-temporary / 0.05);

					double assault = Math.Min(1.0, temporary / 0.0008);

					blend[begin + count] += power * assault * (timber + staticRest + thud);
				}
			}

			double summit = 1e-9;

			foreach (double amount in blend)
				summit = Math.Max(summit, Math.Abs(amount));

			short[] slices = new short[blend.Length];

			for (int count = 0; count < blend.Length; count++)
			{
				slices[count] = (short)Math.Clamp(blend[count] * 16000.0 / summit, short.MinValue, short.MaxValue);
			}

			return slices;
		}
	}
}
