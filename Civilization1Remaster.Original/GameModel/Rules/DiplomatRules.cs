using System;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for Diplomat espionage missions, bribery costs, and city revolt incitement.
	/// Translated from original assembly in CIV.EXE overlay 22 (DiplomatActions).
	/// </summary>
	public static class DiplomatRules
	{
		/// <summary>
		/// Calculates the gold cost required to bribe an enemy unit to desert and switch sides.
		/// Original CIV.EXE 0x1bfb:0x065b (BuyUnit).
		/// </summary>
		/// <param name="victimGold">Treasury gold of the victim civilization owning the unit.</param>
		/// <param name="distanceToNearestCity">Distance from the unit's tile to its owner's nearest city.</param>
		/// <param name="unitCostShields">Base shield build cost of the unit (from unit definitions table).</param>
		/// <param name="unitType">Unit type ID. Settlers (type 0) cost half as much.</param>
		/// <returns>Gold cost required for bribery.</returns>
		public static int ComputeBribeCost(int victimGold, int distanceToNearestCity, int unitCostShields, int unitType)
		{
			// Formula: ((victimGold + 750) / (distance + 2)) * unitCostShields
			int denominator = distanceToNearestCity + 2;
			if (denominator <= 0)
			{
				denominator = 1;
			}

			int baseNumerator = victimGold + 0x2ee; // 750 in decimal
			int cost = (baseNumerator / denominator) * unitCostShields;

			// Settlers (unitType 0) cost half as much to bribe
			if (unitType != 0)
			{
				cost /= 2;
			}

			return Math.Max(0, cost);
		}

		/// <summary>
		/// Calculates the gold cost to incite a revolt in a target city.
		/// Original CIV.EXE 0x1bfb:0x0442 & 0x046b (DiplomatArrivesAtCity).
		/// </summary>
		/// <param name="victimGold">Gold in the target civilization's treasury.</param>
		/// <param name="distanceToCapital">Distance from the target city to the owner's capital.</param>
		/// <param name="citySize">Population size (TrueExtent) of the target city.</param>
		/// <param name="inDisorder">True if the city is in Civil Disorder (halves the revolt cost).</param>
		/// <returns>Base gold cost required to incite a revolt.</returns>
		public static int ComputeInciteRevoltCost(int victimGold, int distanceToCapital, int citySize, bool inDisorder)
		{
			// Formula: ((victimGold + 1000) / (distanceToCapital + 3)) * citySize
			int denominator = distanceToCapital + 3;
			if (denominator <= 0)
			{
				denominator = 1;
			}

			int numerator = victimGold + 0x3e8; // 1000 in decimal
			int cost = (numerator / denominator) * citySize;

			// If the city is currently experiencing civil disorder, dissidents revolt for half price
			if (inDisorder)
			{
				cost /= 2;
			}

			return Math.Max(0, cost);
		}

		/// <summary>
		/// Checks whether a diplomat has enough gold to incite a revolt, and whether subversion (double cost) is possible.
		/// </summary>
		public static (bool canIncite, bool canSubvert) EvaluateInciteOptions(int playerGold, int baseCost, bool hasPeaceTreaty)
		{
			bool canIncite = playerGold >= baseCost;
			// Subversion costs double (x2) and is available if player has peace treaty / alliance
			bool canSubvert = (playerGold >= baseCost * 2) && hasPeaceTreaty;
			return (canIncite, canSubvert);
		}

		/// <summary>
		/// Finds the next technology to steal when diplomat chooses Steal Technology.
		/// Original CIV.EXE 0x1bfb:0x0200 (DiplomatArrivesAtCity).
		/// Scans up to 72 technologies starting from a random seed technology offset.
		/// </summary>
		/// <param name="startTechSeed">Random starting index (0..71).</param>
		/// <param name="victimKnowsTech">Predicate checking if victim knows the tech ID.</param>
		/// <param name="stealerKnowsTech">Predicate checking if stealer already knows the tech ID.</param>
		/// <returns>Tech ID to steal, or -1 if no eligible technology is found.</returns>
		public static int FindTechnologyToSteal(int startTechSeed, Func<int, bool> victimKnowsTech, Func<int, bool> stealerKnowsTech)
		{
			const int TotalTechs = 72;
			for (int i = 0; i < TotalTechs; i++)
			{
				int techId = (startTechSeed + i) % TotalTechs;
				if (victimKnowsTech(techId) && !stealerKnowsTech(techId))
				{
					return techId;
				}
			}
			return -1;
		}

		/// <summary>
		/// Evaluates whether industrial sabotage targets a building or current production.
		/// Returns the building index (1..23) to destroy, or -1 to sabotage current production.
		/// </summary>
		public static int PickIndustrialSabotageTarget(
			bool rollToDirectSabotage,
			bool hasMaterialsInProduction,
			uint cityStructuresMask,
			Func<int> randomBuildingRoll)
		{
			if (rollToDirectSabotage && hasMaterialsInProduction)
			{
				return -1;
			}

			for (int attempts = 0; attempts < 99; attempts++)
			{
				int cand = randomBuildingRoll();
				if (cand <= 0) // Palace (0) cannot be destroyed
					continue;

				if ((cityStructuresMask & (1u << cand)) != 0)
				{
					return cand;
				}
			}

			return -1;
		}
	}
}
