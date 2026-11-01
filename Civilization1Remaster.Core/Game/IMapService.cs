namespace Civilization1Remaster.Core.Game
{
	public interface IMapService
	{
		IMapPlanes Chart { get; }

		void FocusMapView(short participantIdx, int horizLoc, int vertLoc);

		ushort DrawMapTile(int horizLoc, int vertLoc);

		ushort DrawUnitOnWorldMap(short participantIdx, short troopIdx);

		void DrawHealthBar(int horizLoc, int vertLoc, int completeStrikeMarks, int strikeMarks);

		void DrawUnitSprite(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		void RepaintMapTile(int horizLoc, int vertLoc);

		void RepaintUnitTile(short participantIdx, short troopIdx);

		ushort CheckMapLimits(int horizLoc, int vertLoc);

		ushort ObtainTerrainType(int horizLoc, int vertLoc);

		ushort ObtainTileOwnerBits(int horizLoc, int vertLoc);

		void MapAssignCityOwner(short participantIdx, int horizLoc, int vertLoc);

		void PlaceUnitOnTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		void RemoveUnitFromTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		ushort ObtainUnitAtTile(int horizLoc, int vertLoc);

		ushort ObtainTileOwner(int horizLoc, int vertLoc);

		void TagTileOwned(int horizLoc, int vertLoc);

		ushort ContainsRoad(int horizLoc, int vertLoc);

		ushort ObtainTileImprovements(int horizLoc, int vertLoc);

		ushort ObtainKnownTileImprovements(int horizLoc, int vertLoc);

		void KeepKnownTileImprovements(int horizLoc, int vertLoc);

		void AssignTileImprovements(ushort improvements, int horizLoc, int vertLoc);

		void RemoveTileImprovements(ushort improvements, int horizLoc, int vertLoc);

		ushort FindCityAtTile(int horizLoc, int vertLoc);

		ushort ContainsSpecialResource(int horizLoc, int vertLoc);

		ushort ContainsHut(ushort terrain, int horizLoc, int vertLoc);

		ushort ObtainContinentID(int horizLoc, int vertLoc);

		ushort ObtainLandMassSize(int horizLoc, int vertLoc);
	}
}
