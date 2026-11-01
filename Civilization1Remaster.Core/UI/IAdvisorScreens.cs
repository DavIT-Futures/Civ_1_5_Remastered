namespace Civilization1Remaster.Core.UI
{
	public interface IAdvisorScreens
	{
		void ShowReportPreamble(GameText title, ushort style);

		void DrawReportPreamble(GameText title, ushort style);

		void FinishReportScreen();

		void ShowScienceReport(short participantIndex);

		void DrawUnitCountsChart(short participantIndex);

		void DrawPlayerComparisonChart(short participantIndex);

		void ShowTradeReport(short participantIndex);

		void ShowMilitaryReport();

		void ShowIntelligenceReport(short participantIndex);

		void ShowAttitudeReport(short participantIndex);

		void AffixProductionCount(short count, short total);

		void ShowCityStatusReport(short participantIndex);
	}
}
