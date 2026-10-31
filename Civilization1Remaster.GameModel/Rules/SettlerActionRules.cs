using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for Settler workforce actions (Road, RailRoad, Irrigation, Mine, Fortress).
	/// Translated from original assembly in CIV.EXE segment 0x19a0 (PlayerTurn) and 0x2283.
	/// </summary>
	public static class SettlerActionRules
	{
		/// <summary>
		/// Turns required to construct a Fortress.
		/// Standard Civ 1 formula: 4 turns.
		/// </summary>
		public const int FortressTurns = 4;

		public enum SettlerTask
		{
			None = 0,
			Road = 'r',
			Irrigation = 'i',
			Mine = 'm',
			Fortress = 'f'
		}

		/// <summary>
		/// Terrain turns required for road construction by Settlers.
		/// Plains, Grassland, Desert: 2 turns.
		/// Forest, Hills: 4 turns.
		/// Mountains: 10 turns.
		/// </summary>
		public static int GetRoadTurns(int terrainType)
		{
			return terrainType switch
			{
				0 => 2,  // Desert
				1 => 2,  // Plains
				2 => 2,  // Grassland
				3 => 4,  // Forest
				4 => 4,  // Hills
				5 => 10, // Mountains
				6 => 3,  // Tundra
				7 => 2,  // Arctic
				8 => 6,  // Swamp
				9 => 4,  // Jungle
				_ => 2
			};
		}

		/// <summary>
		/// Terrain turns required for irrigation by Settlers.
		/// </summary>
		public static int GetIrrigationTurns(int terrainType)
		{
			return terrainType switch
			{
				0 => 5,  // Desert
				1 => 5,  // Plains
				2 => 5,  // Grassland
				3 => 10, // Forest (clears to Plains)
				4 => 10, // Hills
				8 => 15, // Swamp (clears to Grassland)
				9 => 15, // Jungle (clears to Grassland)
				_ => 5
			};
		}

		/// <summary>
		/// Terrain turns required for mine construction by Settlers.
		/// </summary>
		public static int GetMiningTurns(int terrainType)
		{
			return terrainType switch
			{
				0 => 5,  // Desert
				1 => 10, // Plains (converts to Forest)
				4 => 10, // Hills
				5 => 10, // Mountains
				_ => 10
			};
		}

		/// <summary>
		/// Checks whether a Settler can irrigate a given tile.
		/// Tile must either be adjacent to water/river, an already-irrigated tile, or be coastal.
		/// </summary>
		public static bool CanIrrigate(int terrainType, bool hasAdjacentWaterOrIrrigation, bool alreadyIrrigated)
		{
			if (alreadyIrrigated)
			{
				return false;
			}

			// Ocean/Mountains/Arctic cannot be irrigated
			if (terrainType == 5 || terrainType == 7 || terrainType == 10)
			{
				return false;
			}

			return hasAdjacentWaterOrIrrigation;
		}

		/// <summary>
		/// Checks whether a Settler can build a mine on a given tile.
		/// Mines are permitted on Hills, Mountains, Desert, or Plains (clearing to Forest).
		/// </summary>
		public static bool CanBuildMine(int terrainType, bool alreadyMined)
		{
			if (alreadyMined)
			{
				return false;
			}

			return terrainType == 0 || terrainType == 1 || terrainType == 4 || terrainType == 5;
		}

		/// <summary>
		/// Checks whether a Settler can build a road or railroad on a given tile.
		/// </summary>
		public static bool CanBuildRoad(bool hasRoad, bool hasRailroad, bool hasRailroadTech)
		{
			if (!hasRoad)
			{
				return true; // Can build standard road
			}

			if (!hasRailroad && hasRailroadTech)
			{
				return true; // Can upgrade road to railroad
			}

			return false;
		}

		/// <summary>
		/// Evaluates progress increment and whether work task completes.
		/// </summary>
		public static (int newTurnsWorked, bool isCompleted) AdvanceWork(int turnsWorked, int requiredTurns)
		{
			int next = turnsWorked + 1;
			return (next, next >= requiredTurns);
		}
	}
}
