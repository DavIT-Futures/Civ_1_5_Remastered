namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IRivalOverview
	{
		void ShowCivilizationOverview(short participantIndex);

		void ShowCivilizationScoreList();

		void DrawScoreListRecord();
	}
}
