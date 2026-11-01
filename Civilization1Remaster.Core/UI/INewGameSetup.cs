namespace Civilization1Remaster.Core.UI
{
	public interface INewGameSetup
	{
		void ConfigureFreshGame();

		ushort CreateCivilization(short participantIdx);

		void DestroyCivilization(short participantIdx, short participantIdx1);

		ushort CharacterizeStartLocation();

		void ShowTribeSelection();

		void LoadGovernmentGraphics();

		void ShowRevolutionScreen();

		void ClearSpaceshipContent(short participantIdx);
	}
}
