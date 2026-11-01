namespace Civilization1Remaster.Core.UI
{
	public interface IIntroSequence
	{
		void ShowMainIntro();

		void ShowCivilizationScreen(short participantIdx);

		void ShowTerrainTutorial();

		void ShowInterfaceTutorial();

		void DrawTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2);
	}
}
