namespace Civ_1_5_Remastered.Core.Shared.Services
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

		void CutPlayerCitySprites(int playerIndex);

		void CutPolishUnitSprites(int playerIndex);

		void ApplyCivilizationColours();
	}
}
