namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface ITurnController
	{
		void HandlePlayerTurn(short participantIndex);

		bool RideForFree(short participantIndex, short troopIndex, short horizontalLocation, short verticalLocation);

		void RepaintTileIfVisible(int horizontalLocation, int verticalLocation);

		void RepaintUnitIfVisible(short participantIndex, short troopIndex);

		ushort ObtainTileMoveCost(int horizontalLocation, int verticalLocation);

		ushort CheckCoastalTile(int horizontalLocation, int verticalLocation);

		void DrawUnitInformation(short participantIndex, ushort troopIndex);

		ushort CheckPositionOnScreen(int horizontalLocation, int verticalLocation);

		void WaitForKey();

		ushort FindBestGuardian(short participantIndex, ushort troopIndex);

		void ShowHelpForHuman(GameText topic);
	}
}
