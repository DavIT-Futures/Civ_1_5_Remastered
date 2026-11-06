using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	internal sealed class FakeSettlerMap : ISettlerMap
	{
		public FakeSettlerMap(int terrain)
		{
			for (int column = 0; column < MapRules.Width; column++)
			{
				for (int row = 0; row < MapRules.Height; row++)
				{
					this.Terrain[column, row] = terrain;
				}
			}
		}

		public int[,] Terrain { get; } = new int[MapRules.Width, MapRules.Height];

		public int[,] Improvements { get; } = new int[MapRules.Width, MapRules.Height];

		public bool Water { get; set; } = true;

		int ISettlerMap.Terrain(int horizontalLocation, int verticalLocation) => this.Terrain[horizontalLocation, verticalLocation];

		int ISettlerMap.Improvements(int horizontalLocation, int verticalLocation) => this.Improvements[horizontalLocation, verticalLocation];

		public int Continent(int horizontalLocation, int verticalLocation) =>
			this.Terrain[horizontalLocation, verticalLocation] == MapRules.OceanTerrain ? 0 : 1;

		public bool HasWater(int horizontalLocation, int verticalLocation) => this.Water;

		public int IrrigationEffect(int terrain) => terrain is SettlerPlanner.Desert or SettlerPlanner.Plains or SettlerPlanner.Grassland or SettlerPlanner.Hills ? -2 : -1;

		public int MiningEffect(int terrain) => terrain is SettlerPlanner.Hills or SettlerPlanner.Mountains or SettlerPlanner.Desert ? -2 : -1;
	}
}
