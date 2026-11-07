namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	/// <summary>Adapter of the live game map for the city site planner.</summary>
	internal sealed class GameCitySiteMap : ICitySiteMap
	{
		private const ushort LandMassSizesAddress = 0xdcfe;

		private readonly IGameEngine owner;

		public GameCitySiteMap(IGameEngine owner)
		{
			this.owner = owner;
		}

		public int Terrain(int horizontalLocation, int verticalLocation) => this.owner.MapOperations.Chart.Terrain(horizontalLocation, verticalLocation);

		public int Continent(int horizontalLocation, int verticalLocation) => this.owner.MapOperations.Chart.Continent(horizontalLocation, verticalLocation);

		public int LandMassSize(int horizontalLocation, int verticalLocation)
		{
			// Same table as IMapService.ObtainLandMassSize, read directly: the planner asks for it many times.
			int continent = this.owner.MapOperations.Chart.Continent(horizontalLocation, verticalLocation);

			return this.owner.Processor.PullGlobalWord((ushort)(LandMassSizesAddress + (continent << 1)));
		}

		public int SiteValue(int horizontalLocation, int verticalLocation) => this.owner.VideoCard.ObtainDot(2, horizontalLocation + 80, verticalLocation);

		public bool HasSpecialResource(int horizontalLocation, int verticalLocation) =>
			this.owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) != 0;

		public (int Food, int Shields, int Trade) TerrainYield(int terrain) => (
			(sbyte)this.owner.Tables.TerrainField(terrain, TerrainTable.Food),
			(sbyte)this.owner.Tables.TerrainField(terrain, TerrainTable.Shields),
			(sbyte)this.owner.Tables.TerrainField(terrain, TerrainTable.Trade));
	}
}
