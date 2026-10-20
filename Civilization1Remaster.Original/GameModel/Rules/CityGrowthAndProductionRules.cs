using System;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for city growth, food requirements, granaries, and rush buy production costs.
	/// Translated from original assembly in CIV.EXE (CityManagement).
	/// </summary>
	public static class CityGrowthAndProductionRules
	{
		/// <summary>
		/// Calculates the total food storage capacity (food box size) required for a city to grow to the next population point.
		/// Original CIV.EXE 0x1e3f:0x072b.
		/// </summary>
		/// <param name="citySize">Current population size (TrueExtent) of the city.</param>
		/// <param name="boxScale">Scale factor per citizen (typically 10 for human players; 14-tier on AI).</param>
		/// <returns>Food points required to fill the food box.</returns>
		public static int ComputeFoodBoxCapacity(int citySize, int boxScale = 10)
		{
			// Formula: (citySize + 1) * boxScale
			return (citySize + 1) * boxScale;
		}

		/// <summary>
		/// Calculates the retained food in the food box after a city population growth occurs.
		/// With Granary (structure flag 0x4), the city retains half of the food box towards the next population point!
		/// Formula: hasGranary ? ((5 * citySize) + 5) : 0
		/// </summary>
		/// <param name="newCitySize">City size after the growth step.</param>
		/// <param name="hasGranary">True if the city possesses a Granary or enjoys Pyramids wonder effect.</param>
		/// <returns>Food points retained in the food box.</returns>
		public static int ComputeRetainedFoodAfterGrowth(int newCitySize, bool hasGranary)
		{
			if (hasGranary)
			{
				return (5 * newCitySize) + 5;
			}
			return 0;
		}

		/// <summary>
		/// Calculates the gold cost to rush-buy (hurry) the production of a unit or improvement.
		/// Original CIV.EXE 0x1e3f:0x4c66.
		/// </summary>
		/// <param name="totalCostShields">Total shield cost of the item being produced.</param>
		/// <param name="currentShieldsInBox">Shields already invested in the production box.</param>
		/// <param name="isUnit">True if producing a military or civilian unit, false for city improvement or Wonder.</param>
		/// <param name="isWonder">True if producing a Wonder of the World (doubles cost, disallowed under some rules).</param>
		/// <returns>Gold cost required to rush buy production.</returns>
		public static int ComputeRushBuyCost(int totalCostShields, int currentShieldsInBox, bool isUnit, bool isWonder)
		{
			int remainingShields = Math.Max(0, totalCostShields - currentShieldsInBox);
			if (remainingShields == 0)
			{
				return 0;
			}

			int cost;

			if (isUnit)
			{
				// Unit rush-buy formula:
				// cost = remainingShields * 2 + (remainingShields * remainingShields) / 20
				cost = (remainingShields * 2) + ((remainingShields * remainingShields) / 20);
			}
			else
			{
				// Building rush-buy formula:
				// Base cost = remainingShields * 2
				cost = remainingShields * 2;

				// Wonders of the World (index > 0x15) cost double
				if (isWonder)
				{
					cost *= 2;
				}
			}

			// If the city has 0 shields in the production box, buying costs DOUBLE! (Original Civ1 rule: L4cdb)
			if (currentShieldsInBox == 0)
			{
				cost *= 2;
			}

			return cost;
		}
	}
}
