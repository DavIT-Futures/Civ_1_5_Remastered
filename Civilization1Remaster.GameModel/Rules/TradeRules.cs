using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class TradeRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		public static int ComputeDistance(int firstHorizontal, int firstVertical, int secondHorizontal, int secondVertical)
		{
			int deltaHorizontal = Math.Abs(firstHorizontal - secondHorizontal);
			if (deltaHorizontal > WorldWidth / 2)
			{
				deltaHorizontal = WorldWidth - deltaHorizontal;
			}

			int deltaVertical = Math.Abs(firstVertical - secondVertical);

			if (deltaHorizontal > deltaVertical)
			{
				return (deltaVertical / 2) + deltaHorizontal;
			}
			else
			{
				return (deltaHorizontal / 2) + deltaVertical;
			}
		}

		public static int ComputeCityPlunder(int victimGold, int citySize, int totalEmpirePopulation)
		{
			if (victimGold <= 0 || citySize <= 0)
			{
				return 0;
			}

			int empireDenominator = totalEmpirePopulation + 1;
			if (empireDenominator <= 0)
			{
				empireDenominator = 1;
			}

			if (victimGold >= 0x7d00 / citySize)
			{
				return (victimGold * citySize) / empireDenominator;
			}
			else
			{
				return (victimGold / empireDenominator) * citySize;
			}
		}
	}
}
