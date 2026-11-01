namespace Civilization1Remaster.Core.Game
{
	public interface ITurnController
	{
		void HandlePlayerTurn(short participantIdx);

		bool RideForFree(short participantIdx, short troopIdx, short horizLoc, short vertLoc);

		void RepaintTileIfVisible(int horizLoc, int vertLoc);

		void RepaintUnitIfVisible(short participantIdx, short troopIdx);

		ushort ObtainTileMoveCost(int horizLoc, int vertLoc);

		ushort CheckCoastalTile(int horizLoc, int vertLoc);

		void DrawUnitInfo(short participantIdx, ushort troopIdx);

		ushort CheckPositionOnScreen(int horizLoc, int vertLoc);

		void WaitForKey();

		ushort FindBestGuardian(short participantIdx, ushort troopIdx);

		void ShowHelpForHuman(GameText topic);
	}
}
