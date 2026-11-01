namespace Civilization1Remaster.Core.Shared.Services
{
	public interface INewGameSetup
	{
		void ConfigureFreshGame();

		ushort CreateCivilization(short participantIndex);

		void DestroyCivilization(short participantIndex, short firstParticipantIndex);

		ushort CharacterizeStartLocation();

		void ShowTribeSelection();

		void LoadGovernmentGraphics();

		void ShowRevolutionScreen();

		void ClearSpaceshipContent(short participantIndex);
	}
}
