using System;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class CensusRules
	{
		public static short ApprovalRating(short approval, short size)
		{
			if (size <= 0)
			{
				return 0;
			}

			return (short)Math.Clamp(50 * approval / size, 0, 100);
		}

		public static short Productivity(short output, short size)
		{
			if (size <= 0)
			{
				return 0;
			}

			return (short)Math.Max(0, 10 * output / size);
		}

		public static short DiseaseRate(short size, short health)
		{
			int totalHealth = size + health;

			if (totalHealth <= 0)
			{
				return 0;
			}

			return (short)Math.Clamp(50 * size / totalHealth, 0, 100);
		}

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
