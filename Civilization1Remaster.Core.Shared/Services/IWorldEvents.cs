namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IWorldEvents
	{
		void MendFortifiedUnits();

		ushort ShowDialog(ushort charactersAddress, int horizontalLocation, int verticalLocation);

		void HandleGameEnd();

		void ShowEndGameSequence();

		void ShowTitleScreen();

		void ShowTravellersReport();

		void RefreshPeaceGraphContent();

		void ClearStatusLine();

		void DrawStatusBar();

		void DrawPalace(short participantIndex, short castleTier, short horizontalLocation, short verticalLocation);

		ushort ObtainPalaceScoreForLevel(short castleTier);

		void FormatCurrentYear();

		void HandleEndOfTurnEvents();

		void RepaintMainScreen();

		void FillBoxWithShadow(int horizontalLocation, int verticalLocation, int breadth, int altitude);

		void ShowCivilizationScenery();

		short ObtainPalaceLevel(short rating);
	}
}
