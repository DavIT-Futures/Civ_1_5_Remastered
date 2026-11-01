namespace Civ_1_5_Remastered.Core.Shared.Services
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
