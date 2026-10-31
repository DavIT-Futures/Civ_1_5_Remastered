using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Calculations for the world demographics screen (DisplayCensus), in 32-bit arithmetic (the 16-bit original
	/// overflowed in large late-game empires).
	/// </summary>
	public static class CensusRules
	{
		/// <summary>Approval rating (0 - 100%): 50 * approval / size</summary>
		public static short ApprovalRating(short approval, short size)
		{
			if (size <= 0)
			{
				return 0;
			}

			return (short)Math.Clamp(50 * approval / size, 0, 100);
		}

		/// <summary>Productivity: 10 * output / size, at least 0</summary>
		public static short Productivity(short output, short size)
		{
			if (size <= 0)
			{
				return 0;
			}

			return (short)Math.Max(0, 10 * output / size);
		}

		/// <summary>Disease rate (0 - 100%): 50 * size / (size + health)</summary>
		public static short DiseaseRate(short size, short health)
		{
			int totalHealth = size + health;

			if (totalHealth <= 0)
			{
				return 0;
			}

			return (short)Math.Clamp(50 * size / totalHealth, 0, 100);
		}

		/// <summary>Family size in tenths: 40 * unrest / size + 20, at least 0</summary>
		public static short FamilySize(short unrest, short size)
		{
			if (size <= 0)
			{
				return 20;
			}

			return (short)Math.Max(0, 40 * unrest / size + 20);
		}
	}
}
