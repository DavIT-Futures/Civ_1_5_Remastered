namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IRivalOverview
	{
		void ShowCivilizationOverview(short participantIndex);

		void ShowCivilizationScoreList();

		void DrawScoreListRecord();
	}
}
