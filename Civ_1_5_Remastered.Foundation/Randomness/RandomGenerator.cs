using System;

namespace Civ_1_5_Remastered.Foundation.Randomness
{
	public class RandomGenerator
	{
		private const uint GoldenStep = 0x9e3779b9u;

		private readonly uint[] state = new uint[4];

		public RandomGenerator()
			: this((uint)Environment.TickCount)
		{
		}

		public RandomGenerator(uint seed)
		{
			uint spread = seed;

			for (int index = 0; index < this.state.Length; index++)
			{
				spread += GoldenStep;
				this.state[index] = Mixed(spread);
			}
		}

		public uint DrawUnsigned()
		{
			uint result = RotatedLeft(this.state[1] * 5, 7) * 9;
			uint shifted = this.state[1] << 9;

			this.state[2] ^= this.state[0];
			this.state[3] ^= this.state[1];
			this.state[1] ^= this.state[2];
			this.state[0] ^= this.state[3];
			this.state[2] ^= shifted;
			this.state[3] = RotatedLeft(this.state[3], 11);

			return result;
		}

		public int DrawInteger(int peakAmount)
		{
			if (peakAmount < 0)
			{
				return -DrawInteger(-peakAmount);
			}

			return (int)(((ulong)DrawUnsigned() * (uint)peakAmount) >> 32);
		}

		private static uint Mixed(uint value)
		{
			value = (value ^ (value >> 16)) * 0x85ebca6bu;
			value = (value ^ (value >> 13)) * 0xc2b2ae35u;
			return value ^ (value >> 16);
		}

		private static uint RotatedLeft(uint value, int distance) => (value << distance) | (value >> (32 - distance));
	}
}
