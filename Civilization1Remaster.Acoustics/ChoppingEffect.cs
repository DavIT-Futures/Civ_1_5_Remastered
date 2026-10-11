using System;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>
	/// Sound of chopping wood, played when the player founds a new city (the original game is silent there).
	/// With the MIDI output it is played with General MIDI percussion (low wood block and a low tom),
	/// with the AdLib and PC speaker outputs it is synthesized and mixed into the audio.
	/// </summary>
	public static partial class ChoppingEffect
	{
		// Axe strikes: time in seconds and strength, a little irregular like real chopping
		private static readonly double[] StrikeMoments = { 0.0, 0.43, 0.82, 1.28, 1.66 };
		private static readonly double[] StrikePower = { 0.85, 0.75, 0.9, 0.8, 1.0 };

		private const byte DrumsLane = 9;
		private const byte LowerTimberChunk = 77;
		private const byte LowerGroundDrum = 41;

		/// <summary>
		/// MIDI messages (status, note, velocity) to send on the given tick, the tick rate is SoundDriverHost.TickRate.
		/// Returns false when the effect is finished.
		/// </summary>
		public static bool MidiTick(int beat, Action<byte, byte, byte> emitted)
		{
			bool sounding = false;

			for (int idx = 0; idx < StrikeMoments.Length; idx++)
			{
				int begin = (int)(StrikeMoments[idx] * SoundDriverHost.BeatRatio);
				int finish = begin + SoundDriverHost.BeatRatio / 10;
				byte pace = (byte)(127 * StrikePower[idx]);

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

		/// <summary>
		/// Synthesizes the effect: each strike is a short low thump and a cracking noise filtered to sound like wood
		/// </summary>
		public static short[] Synthesize(int sliceRatio)
		{
			double span = StrikeMoments[StrikeMoments.Length - 1] + 0.4;
			double[] blend = new double[(int)(span * sliceRatio)];
			Random chance = new Random(1234);

			for (int idx = 0; idx < StrikeMoments.Length; idx++)
			{
				int begin = (int)(StrikeMoments[idx] * sliceRatio);
				double power = StrikePower[idx];
				int strikeSpan = Math.Min((int)(0.3 * sliceRatio), blend.Length - begin);

				// Two resonators excited by noise give the hollow "tock" of wood
				Resonator torso = new Resonator(sliceRatio, 650.0 + 40.0 * idx, 0.9965);
				Resonator fissure = new Resonator(sliceRatio, 2300.0 - 60.0 * idx, 0.992);
				double thudStage = 0.0;

				for (int cnt = 0; cnt < strikeSpan; cnt++)
				{
					double tmp = (double)cnt / sliceRatio;
					double hiss = chance.NextDouble() * 2.0 - 1.0;
					double stimulus = hiss * Math.Exp(-tmp / 0.004);

					double timber = torso.Handle(stimulus) * 2.0 + fissure.Handle(stimulus) * 1.0;
					double staticRest = hiss * 0.18 * Math.Exp(-tmp / 0.018);

					// The thump falls from 140 Hz to 70 Hz
					double thudHertz = 70.0 + 70.0 * Math.Exp(-tmp / 0.03);
					thudStage += 2.0 * Math.PI * thudHertz / sliceRatio;
					double thud = Math.Sin(thudStage) * 0.55 * Math.Exp(-tmp / 0.05);

					// Very short attack, so the strike doesn't click
					double assault = Math.Min(1.0, tmp / 0.0008);

					blend[begin + cnt] += power * assault * (timber + staticRest + thud);
				}
			}

			// The loudest strike is a little louder than the music
			double summit = 1e-9;

			foreach (double amount in blend)
				summit = Math.Max(summit, Math.Abs(amount));

			short[] slices = new short[blend.Length];

			for (int cnt = 0; cnt < blend.Length; cnt++)
			{
				slices[cnt] = (short)Math.Clamp(blend[cnt] * 16000.0 / summit, short.MinValue, short.MaxValue);
			}

			return slices;
		}
	}
}
