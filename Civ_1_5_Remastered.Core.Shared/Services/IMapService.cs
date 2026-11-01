namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IMapService
	{
		IMapPlanes Chart { get; }

		void FocusMapView(short participantIndex, int horizontalLocation, int verticalLocation);

		ushort DrawMapTile(int horizontalLocation, int verticalLocation);

		ushort DrawUnitOnWorldMap(short participantIndex, short troopIndex);

		void DrawHealthBar(int horizontalLocation, int verticalLocation, int completeStrikeMarks, int strikeMarks);

		void DrawUnitSprite(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation);

		void RepaintMapTile(int horizontalLocation, int verticalLocation);

		void RepaintUnitTile(short participantIndex, short troopIndex);

		ushort CheckMapLimits(int horizontalLocation, int verticalLocation);

		ushort ObtainTerrainType(int horizontalLocation, int verticalLocation);

		ushort ObtainTileOwnerBits(int horizontalLocation, int verticalLocation);

		void MapAssignCityOwner(short participantIndex, int horizontalLocation, int verticalLocation);

		void PlaceUnitOnTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation);

		void RemoveUnitFromTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation);

		ushort ObtainUnitAtTile(int horizontalLocation, int verticalLocation);

		ushort ObtainTileOwner(int horizontalLocation, int verticalLocation);

		void TagTileOwned(int horizontalLocation, int verticalLocation);

		ushort ContainsRoad(int horizontalLocation, int verticalLocation);

		ushort ObtainTileImprovements(int horizontalLocation, int verticalLocation);

		ushort ObtainKnownTileImprovements(int horizontalLocation, int verticalLocation);

		void KeepKnownTileImprovements(int horizontalLocation, int verticalLocation);

		void AssignTileImprovements(ushort improvements, int horizontalLocation, int verticalLocation);

		void RemoveTileImprovements(ushort improvements, int horizontalLocation, int verticalLocation);

		ushort FindCityAtTile(int horizontalLocation, int verticalLocation);

		ushort ContainsSpecialResource(int horizontalLocation, int verticalLocation);

		ushort ContainsHut(ushort terrain, int horizontalLocation, int verticalLocation);

		ushort ObtainContinentIdentifier(int horizontalLocation, int verticalLocation);

		ushort ObtainLandMassSize(int horizontalLocation, int verticalLocation);
	}
}
