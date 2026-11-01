namespace Civilization1Remaster.Core.Game
{
	public interface ICities
	{
		ushort ObtainCityTileIndex(short horizLoc, short vertLoc);

		void DrawCityTile(short townIdx, ushort townTile, short marker);

		ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort resource);

		ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent);

		ushort ObtainWonderCityID(short miracleIdent);

		void AttachSmog(int horizLoc, int vertLoc);

		void AssignCitizenType(ushort citizen, ushort specialist);

		ushort ObtainCitizenType(ushort citizen);

		ushort TallySpecialists(ushort specialist);

		void ReshuffleCitizens(short townIdx, ushort workerTally);

		ushort DrawCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort specialists, ushort breadth);

		void DrawResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo);

		void DrawFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1);

		void FillBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude);

		void DrawLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, GameText label, ushort style);

		void DrawMiniMap();

		void DrawMiniMapDot(int horizLoc, int vertLoc, ushort style);

		ushort HandleCity(short townIdx, short marker);
	}
}
