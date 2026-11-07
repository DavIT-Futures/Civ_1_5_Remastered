using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	internal sealed class FakeCitySiteMap : ICitySiteMap
	{
		public FakeCitySiteMap()
		{
			for (int column = 0; column < MapRules.Width; column++)
			{
				for (int row = 0; row < MapRules.Height; row++)
				{
					this.Terrains[column, row] = MapRules.OceanTerrain;
					this.Values[column, row] = 10;
				}
			}
		}

		public int[,] Terrains { get; } = new int[MapRules.Width, MapRules.Height];

		public int[,] Values { get; } = new int[MapRules.Width, MapRules.Height];

		public int[,] Continents { get; } = new int[MapRules.Width, MapRules.Height];

		public Dictionary<int, int> Sizes { get; } = new Dictionary<int, int>();

		public void AddLand(int continent, int left, int top, int width, int height, int terrain = 2)
		{
			for (int column = left; column < left + width; column++)
			{
				for (int row = top; row < top + height; row++)
				{
					this.Terrains[column, row] = terrain;
					this.Continents[column, row] = continent;
				}
			}

			this.Sizes[continent] = this.Sizes.GetValueOrDefault(continent) + (width * height);
		}

		public int Terrain(int horizontalLocation, int verticalLocation) => this.Terrains[horizontalLocation, verticalLocation];

		public int Continent(int horizontalLocation, int verticalLocation) => this.Continents[horizontalLocation, verticalLocation];

		public int LandMassSize(int horizontalLocation, int verticalLocation) => this.Sizes.GetValueOrDefault(this.Continents[horizontalLocation, verticalLocation]);

		public int SiteValue(int horizontalLocation, int verticalLocation) => this.Values[horizontalLocation, verticalLocation];

		public bool HasSpecialResource(int horizontalLocation, int verticalLocation) => false;

		public (int Food, int Shields, int Trade) TerrainYield(int terrain) => terrain switch
		{
			MapRules.OceanTerrain => (1, 0, 2),
			2 => (2, 0, 0),
			4 => (1, 0, 0),
			_ => (1, 1, 0),
		};
	}
}
