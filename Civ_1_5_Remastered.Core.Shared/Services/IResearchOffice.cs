namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IResearchOffice
	{
		void HandleAllCities(short participantIndex);

		void FinalizeProduction(short townIndex, ushort horizontalLocation, ushort verticalLocation);

		void DrawAdvisorScenery();

		void RecoverAdvisorScenery();

		void RenewCityScreen(short townIndex);

		ushort PickCityProduction(short participantIndex, short townIndex);

		void ComposeProductionOption(short townIndex, ushort price);

		ushort UncoverTechnology(short participantIndex, short firstParticipantIndex);

		void EarnTechnology(short participantIndex, short scienceIndex, short firstParticipantIndex);

		ushort PlayerKnowsTechnology(short participantIndex, short scienceIndex);

		ushort ObtainTechnologyInformation(short participantIndex, ushort scienceIndex);
	}
}
