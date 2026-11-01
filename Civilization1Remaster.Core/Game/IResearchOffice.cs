namespace Civilization1Remaster.Core.Game
{
	public interface IResearchOffice
	{
		void HandleAllCities(short participantIdx);

		void FinalizeProduction(short townIdx, ushort horizLoc, ushort vertLoc);

		void DrawAdvisorScenery();

		void RecoverAdvisorScenery();

		void RenewCityScreen(short townIdx);

		ushort PickCityProduction(short participantIdx, short townIdx);

		void ComposeProductionOption(short townIdx, ushort price);

		ushort UncoverTechnology(short participantIdx, short participantIdx1);

		void EarnTechnology(short participantIdx, short scienceIdx, short participantIdx1);

		ushort PlayerKnowsTechnology(short participantIdx, short scienceIdx);

		ushort ObtainTechnologyInfo(short participantIdx, ushort scienceIdx);
	}
}
