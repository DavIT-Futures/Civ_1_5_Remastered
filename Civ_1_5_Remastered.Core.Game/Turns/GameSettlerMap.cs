namespace Civ_1_5_Remastered.Core.Game.Turns
{
	internal sealed class GameSettlerMap : ISettlerMap
	{
		private readonly IGameEngine owner;
		private readonly GameTables tables;

		public GameSettlerMap(IGameEngine owner, GameTables tables)
		{
			this.owner = owner;
			this.tables = tables;
		}

		public int Terrain(int horizontalLocation, int verticalLocation) => this.owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

		public int Improvements(int horizontalLocation, int verticalLocation) => this.owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

		public int Continent(int horizontalLocation, int verticalLocation) => this.owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation);

		public bool HasSpecialResource(int horizontalLocation, int verticalLocation) =>
			this.owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) != 0;

		public bool HasWater(int horizontalLocation, int verticalLocation) => this.owner.TurnLogic.CheckCoastalTile(horizontalLocation, verticalLocation) != 0;

		public int IrrigationEffect(int terrain) => (short)this.tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation);

		public int MiningEffect(int terrain) => (short)this.tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Mining);
	}
}
