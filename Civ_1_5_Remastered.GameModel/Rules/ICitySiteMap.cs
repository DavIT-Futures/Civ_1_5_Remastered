namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Read-only view of the map used by the city site planner.</summary>
	public interface ICitySiteMap
	{
		int Terrain(int horizontalLocation, int verticalLocation);

		int Continent(int horizontalLocation, int verticalLocation);

		/// <summary>Number of land tiles of the landmass the tile belongs to.</summary>
		int LandMassSize(int horizontalLocation, int verticalLocation);

		/// <summary>Precomputed site value of the map (8..15); lower values mean the tile cannot hold a city.</summary>
		int SiteValue(int horizontalLocation, int verticalLocation);

		bool HasSpecialResource(int horizontalLocation, int verticalLocation);

		/// <summary>Food, shields and trade of the bare terrain.</summary>
		(int Food, int Shields, int Trade) TerrainYield(int terrain);
	}
}
