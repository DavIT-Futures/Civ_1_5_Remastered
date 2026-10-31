using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Calculations for the world demographics screen (DisplayCensus).
	/// </summary>
	public static class CensusRules
	{
		/// <summary>
		/// Approval rating (0 - 100%): 50 * approval / size.
		/// classic: as the 16-bit DOS CIV.EXE computed it, where 50 * approval overflows 16-bit signed integer
		/// (32767) when approval exceeds 655, yielding negative approval ratings in large late-game empires.
		/// off: uses 32-bit arithmetic and clamps to [0, 100].
		/// </summary>
		public static short ApprovalRating(short approval, short size, bool classic)
		{
			if (classic)
			{
				return (short)((short)(0x32 * approval) / size);
			}

			if (size <= 0)
			{
				return 0;
			}

			int numerator = 50 * (int)approval;
			return (short)Math.Clamp(numerator / (int)size, 0, 100);
		}

		/// <summary>
		/// Productivity: 10 * output / size.
		/// classic: as the 16-bit DOS CIV.EXE computed it, where 10 * output overflows 16-bit signed integer
		/// (32767) when output exceeds 3276, yielding negative productivity in large late-game empires.
		/// off: uses 32-bit arithmetic and clamps to >= 0.
		/// </summary>
		public static short Productivity(short output, short size, bool classic)
		{
			if (classic)
			{
				return (short)((short)(0xa * output) / size);
			}

			if (size <= 0)
			{
				return 0;
			}

			int numerator = 10 * (int)output;
			return (short)Math.Max(0, numerator / (int)size);
		}

		/// <summary>
		/// Disease rate (0 - 100%): 50 * size / (size + health).
		/// classic: as the 16-bit DOS CIV.EXE computed it, overflowing when 50 * size > 32767 (size > 655).
		/// off: uses 32-bit arithmetic and clamps to [0, 100].
		/// </summary>
		public static short DiseaseRate(short size, short health, bool classic)
		{
			if (classic)
			{
				return (short)((short)(0x32 * size) / (short)(size + health));
			}

			int totalHealth = size + health;
			if (totalHealth <= 0)
			{
				return 0;
			}

			int numerator = 50 * (int)size;
			return (short)Math.Clamp(numerator / totalHealth, 0, 100);
		}

		/// <summary>
		/// Family size: 40 * unrest / size + 20 tenths.
		/// classic: as the 16-bit DOS CIV.EXE computed it, overflowing when 40 * unrest > 32767 (unrest > 819).
		/// off: uses 32-bit arithmetic and clamps to >= 0.
		/// </summary>
		public static short FamilySize(short unrest, short size, bool classic)
		{
			if (classic)
			{
				return (short)(((short)(0x28 * unrest) / size) + 0x14);
			}

			if (size <= 0)
			{
				return 20;
			}

			int numerator = 40 * (int)unrest;
			return (short)Math.Max(0, (numerator / (int)size) + 20);
		}
	}
}
