namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Read-only view of the map used by the settler planner.</summary>
	public interface ISettlerMap
	{
		int Terrain(int horizontalLocation, int verticalLocation);

		int Improvements(int horizontalLocation, int verticalLocation);

		int Continent(int horizontalLocation, int verticalLocation);

		bool HasWater(int horizontalLocation, int verticalLocation);

		/// <summary>Irrigation entry of the terrain table: -1 impossible, -2 irrigation needing water, 0 or more a terrain change.</summary>
		int IrrigationEffect(int terrain);

		/// <summary>Mining entry of the terrain table: -1 impossible, -2 or less a mine, 0 or more a terrain change.</summary>
		int MiningEffect(int terrain);
	}
}
