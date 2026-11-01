namespace Civilization1Remaster.Core.UI
{
	public interface IIntroSequence
	{
		void ShowMainIntro();

		void ShowCivilizationScreen(short participantIndex);

		void ShowTerrainTutorial();

		void ShowInterfaceTutorial();

		void DrawTutorialPointer(int firstHorizontalSpot, int firstVerticalSpot, int secondHorizontalSpot, int secondVerticalSpot);
	}
}
