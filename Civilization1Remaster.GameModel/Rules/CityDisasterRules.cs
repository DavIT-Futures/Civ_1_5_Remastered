using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for city disasters and building damages.
	/// Translated from original assembly in CIV.EXE overlay 20 (CityFoundingAndScore.HandleCityDisasters).
	/// </summary>
	public static class CityDisasterRules
	{
		/// <summary>
		/// Determines whether an earthquake can strike a city (requires mountain terrain in 3x3 vicinity and non-palace buildings).
		/// </summary>
		public static bool CanEarthquakeStrike(ushort nearbyTerrainMask, ushort structureMarkers0)
		{
			// bit 4 (0x10) represents mountain terrain in nearby 3x3 tiles
			bool hasMountains = (nearbyTerrainMask & 0x10) != 0;
			// 0xfffe checks if any improvements exist besides the Palace (bit 0)
			bool hasDestructibleBuildings = (structureMarkers0 & 0xfffe) != 0;
			return hasMountains && hasDestructibleBuildings;
		}

		/// <summary>
		/// Determines whether a plague / disease can strike a city (requires city size > 0 and no Aqueduct / Medicine protection).
		/// </summary>
		public static bool CanPlagueStrike(bool knowsMedicine, ushort structureMarkers0, bool knowsConstruction)
		{
			if (knowsMedicine) return false;
			// bit 8 (0x100) represents Aqueduct
			bool hasAqueduct = (structureMarkers0 & 0x100) != 0;
			if (hasAqueduct) return false;
			return knowsConstruction;
		}

		/// <summary>
		/// Computes plague population reduction (reduces population by 1/4).
		/// </summary>
		public static int ComputePlaguePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0) return 0;
			return currentPopulation / 4;
		}

		/// <summary>
		/// Determines whether a flood can strike a city (requires river terrain in 3x3 and no City Walls / Barracks).
		/// </summary>
		public static bool CanFloodStrike(ushort nearbyTerrainMask, ushort structureMarkers0, bool knowsConstruction, int citySize)
		{
			// bit 11 (0x800) represents river terrain
			bool hasRiver = (nearbyTerrainMask & 0x800) != 0;
			// bit 7 (0x80) represents City Walls
			bool hasCityWalls = (structureMarkers0 & 0x80) != 0;
			return hasRiver && !hasCityWalls && knowsConstruction && citySize > 1;
		}

		/// <summary>
		/// Determines whether a famine can strike a city (requires swamp or desert in 3x3 and no Granary).
		/// </summary>
		public static bool CanFamineStrike(ushort nearbyTerrainMask, ushort structureMarkers0, bool knowsPottery, int citySize)
		{
			// bit 5 (0x20) represents swamp/desert
			bool hasSwampOrDesert = (nearbyTerrainMask & 0x20) != 0;
			// bit 3 (0x8) represents Granary
			bool hasGranary = (structureMarkers0 & 0x8) != 0;
			return hasSwampOrDesert && !hasGranary && knowsPottery && citySize > 1;
		}

		/// <summary>
		/// Computes famine population loss (reduces population by 1/3).
		/// </summary>
		public static int ComputeFaminePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0) return 0;
			return currentPopulation / 3;
		}

		/// <summary>
		/// Determines whether a fire can strike a city (requires city size > 0 and no Aqueduct).
		/// </summary>
		public static bool CanFireStrike(ushort structureMarkers0, bool knowsConstruction, int citySize)
		{
			if (citySize <= 0) return false;
			// bit 2 (0x4) represents Aqueduct / Temple check
			bool hasProtection = (structureMarkers0 & 0x4) != 0;
			return !hasProtection && knowsConstruction;
		}

		/// <summary>
		/// Determines whether pirates / coastal raiders can raid a city (requires ocean in 3x3 and no Barracks).
		/// </summary>
		public static bool CanPiratesRaid(ushort nearbyTerrainMask, ushort structureMarkers0)
		{
			// bit 10 (0x400) represents ocean
			bool hasOcean = (nearbyTerrainMask & 0x400) != 0;
			// bit 1 (0x2) represents Barracks
			bool hasBarracks = (structureMarkers0 & 0x2) != 0;
			return hasOcean && !hasBarracks;
		}
	}
}
