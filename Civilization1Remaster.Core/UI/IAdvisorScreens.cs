namespace Civilization1Remaster.Core.UI
{
	public interface IAdvisorScreens
	{
		void ShowReportPreamble(GameText title, ushort style);

		void DrawReportPreamble(GameText title, ushort style);

		void FinishReportScreen();

		void ShowScienceReport(short participantIdx);

		void DrawUnitCountsChart(short participantIdx);

		void DrawPlayerComparisonChart(short participantIdx);

		void ShowTradeReport(short participantIdx);

		void ShowMilitaryReport();

		void ShowIntelligenceReport(short participantIdx);

		void ShowAttitudeReport(short participantIdx);

		void AffixProductionCount(short count, short total);

		void ShowCityStatusReport(short participantIdx);
	}
}
