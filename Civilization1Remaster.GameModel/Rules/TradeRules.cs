using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class TradeRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		public static int ComputeDistance(int x1, int y1, int x2, int y2)
		{
			int dx = Math.Abs(x1 - x2);
			if (dx > WorldWidth / 2)
			{
				dx = WorldWidth - dx;
			}

			int dy = Math.Abs(y1 - y2);

			if (dx > dy)
			{
				return (dy / 2) + dx;
			}
			else
			{
				return (dx / 2) + dy;
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
