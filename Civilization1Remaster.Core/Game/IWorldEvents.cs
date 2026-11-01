namespace Civilization1Remaster.Core.Game
{
	public interface IWorldEvents
	{
		void MendFortifiedUnits();

		ushort ShowDialog(ushort charsAddr, int horizLoc, int vertLoc);

		void HandleGameEnd();

		void ShowEndGameSequence();

		void ShowTitleScreen();

		void ShowTravellersReport();

		void RefreshPeaceGraphContent();

		void ClearStatusLine();

		void DrawStatusBar();

		void DrawPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc);

		ushort ObtainPalaceScoreForLevel(short castleTier);

		void FormatCurrentYear();

		void HandleEndOfTurnEvents();

		void RepaintMainScreen();

		void FillBoxWithShadow(int horizLoc, int vertLoc, int breadth, int altitude);

		void ShowCivilizationScenery();

		short ObtainPalaceLevel(short rating);
	}
}
