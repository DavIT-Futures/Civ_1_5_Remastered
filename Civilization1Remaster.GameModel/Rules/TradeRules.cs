using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for city trade routes, distance calculation, and plunder yields.
	/// Translated from original assembly in CIV.EXE segment 0x2459 (TradeAndConquest).
	/// </summary>
	public static class TradeRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		/// <summary>
		/// Calculates distance between two map coordinates on the cylindrical world map.
		/// Original CIV.EXE CommonUtilities.ObtainDistance / MergeCoordinates.
		/// </summary>
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

		/// <summary>
		/// Computes plunder gold looted when a city is captured.
		/// Original CIV.EXE 0x2459:0x0550 (ComputeCityPlunder).
		/// </summary>
		/// <param name="victimGold">Gold in the victim civilization's treasury.</param>
		/// <param name="citySize">Population size (TrueExtent) of the captured city.</param>
		/// <param name="totalEmpirePopulation">Sum of all city sizes in the victim empire (GrandTownExtent).</param>
		/// <returns>Gold plundered from the treasury.</returns>
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

			// Civ1 formula:
			// If victimGold > 32000 / citySize (i.e. victimGold >= 0x7d00 / citySize):
			//   plunder = (citySize * victimGold) / empireDenominator
			// Else:
			//   plunder = (victimGold / empireDenominator) * citySize
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
