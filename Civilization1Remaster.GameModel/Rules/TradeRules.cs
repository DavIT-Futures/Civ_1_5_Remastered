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
		/// Computes the one-time delivery bonus (gold and science) when establishing a trade route.
		/// Original CIV.EXE 0x2459:0x0827 (FoundTradeRoute).
		/// </summary>
		/// <param name="distance">Geometric distance between source and destination cities.</param>
		/// <param name="sourceCityBaseTrade">Base commerce of source city.</param>
		/// <param name="destCityBaseTrade">Base commerce of destination city.</param>
		/// <param name="sameContinent">True if both cities are on the same continent/island.</param>
		/// <param name="sameCivilization">True if both cities belong to the same player.</param>
		/// <param name="destHasRailroad">True if destination player has Railroad tech (cancels 1/3 bonus).</param>
		/// <param name="destHasFlight">True if destination player has Flight tech (cancels 1/3 bonus).</param>
		/// <returns>Gold and Science one-time bonus payout.</returns>
		public static int ComputeOneTimeTradeBonus(
			int distance,
			int sourceCityBaseTrade,
			int destCityBaseTrade,
			bool sameContinent,
			bool sameCivilization,
			bool destHasRailroad,
			bool destHasFlight)
		{
			// Base revenue = (Distance + 10) * (SourceTrade + DestTrade) / 24
			int combinedTrade = sourceCityBaseTrade + destCityBaseTrade;
			int revenue = ((distance + 10) * combinedTrade) / 24;

			// Same continent penalty: halved
			if (sameContinent)
			{
				revenue /= 2;
			}

			// Domestic route penalty: halved
			if (sameCivilization)
			{
				revenue /= 2;
			}

			// Railroad discovery reduces payout by 1/3 (multiplied by 2/3)
			if (destHasRailroad)
			{
				revenue -= (revenue / 3);
			}

			// Flight discovery reduces payout by another 1/3 (multiplied by 2/3)
			if (destHasFlight)
			{
				revenue -= (revenue / 3);
			}

			return revenue;
		}

		/// <summary>
		/// Computes the ongoing trade arrows added to a city each turn for an active trade route.
		/// Original CIV.EXE CityManagement (0x1e3f:0x3355 & 0x5650).
		/// </summary>
		/// <param name="cityBaseTrade1">Base commerce arrows of the local city.</param>
		/// <param name="cityBaseTrade2">Base commerce arrows of the partner city.</param>
		/// <param name="isDomestic">True if trading with own city, false if trading with foreign city.</param>
		/// <returns>Additional trade arrows generated per turn.</returns>
		public static int ComputeOngoingTradeRouteArrows(int cityBaseTrade1, int cityBaseTrade2, bool isDomestic)
		{
			int sum = cityBaseTrade1 + cityBaseTrade2 + 4;
			return isDomestic ? (sum / 16) : (sum / 8);
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

		/// <summary>
		/// Evaluates and assigns a trade route into the city's 3 route slots.
		/// Original CIV.EXE 0x2459:0x0b23 (FoundTradeRoute slot allocation).
		/// </summary>
		/// <param name="currentRoutes">3-element array of current destination town IDs (-1/0xff = empty).</param>
		/// <param name="newCityId">City ID to establish a trade route with.</param>
		/// <param name="newRouteValue">Trade value of the new route.</param>
		/// <param name="routeEvaluator">Function returning trade value of an existing route destination.</param>
		/// <returns>Index of slot assigned (0..2), or -1 if the route could not be established.</returns>
		public static int TryAssignTradeRouteSlot(
			sbyte[] currentRoutes,
			short newCityId,
			int newRouteValue,
			Func<short, int> routeEvaluator)
		{
			// If already trading with this city, succeed without modification
			for (int i = 0; i < currentRoutes.Length; i++)
			{
				if (currentRoutes[i] == (sbyte)newCityId)
				{
					return i;
				}
			}

			// Look for an empty slot or the weakest existing route
			int worstSlot = -1;
			int worstValue = newRouteValue;

			for (int i = 0; i < currentRoutes.Length; i++)
			{
				sbyte existingCity = currentRoutes[i];
				if (existingCity == -1 || existingCity == unchecked((sbyte)0xff))
				{
					worstSlot = i;
					worstValue = -1;
					break;
				}

				int existingVal = routeEvaluator(existingCity);
				if (existingVal < worstValue)
				{
					worstValue = existingVal;
					worstSlot = i;
				}
			}

			if (worstSlot != -1)
			{
				currentRoutes[worstSlot] = (sbyte)newCityId;
				return worstSlot;
			}

			return -1;
		}
	}
}
