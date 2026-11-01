namespace Civilization1Remaster.Core.UI
{
	public interface IRivalOverview
	{
		void ShowCivilizationOverview(short participantIdx);

		void ShowCivilizationScoreList();

		void DrawScoreListRecord();
	}
}
