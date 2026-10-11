using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class AdvisorStatisticsTests
	{
		[Theory]
		[InlineData(0, 100, 10, 10)]       // 100 remaining, 10/turn = 10 turns
		[InlineData(50, 100, 10, 5)]       // 50 remaining, 10/turn = 5 turns
		[InlineData(91, 100, 10, 1)]       // 9 remaining, 10/turn = 1 turn (ceiling)
		[InlineData(100, 100, 10, 0)]      // 0 remaining = 0 turns (completed)
		[InlineData(120, 100, 10, 0)]      // Overflown = 0 turns
		[InlineData(20, 100, 0, -1)]       // 0/turn = -1 (no progress)
		[InlineData(20, 100, -5, -1)]      // Negative rate = -1
		public void CalculateTurnsToDiscovery_WorksAccurately(int current, int required, int rate, int expectedTurns)
		{
			int actual = AdvisorStatistics.CalculateTurnsToDiscovery(current, required, rate);
			Assert.Equal(expectedTurns, actual);
		}

		[Fact]
		public void CalculateTotalYields_SumsComponents()
		{
			int food = 40;
			int prod = 25;
			int trade = 35;
			Assert.Equal(100, AdvisorStatistics.CalculateTotalYields(food, prod, trade));
		}

		[Fact]
		public void FormatCityStatusSummary_ContainsAllStats()
		{
			string summary = AdvisorStatistics.FormatCityStatusSummary(4, 18, 45, 30, 40);
			Assert.Contains("4 cities", summary);
			Assert.Contains("Pop 18", summary);
			Assert.Contains("Food 45", summary);
			Assert.Contains("Prod 30", summary);
			Assert.Contains("Trade 40", summary);
			Assert.Contains("Yields: 115", summary);
		}

		[Fact]
		public void FormatTradeTotals_ContainsBreakdown()
		{
			string summary = AdvisorStatistics.FormatTradeTotals(50, 20, 20, 10);
			Assert.Contains("Total Trade: 50", summary);
			Assert.Contains("Tax:20$", summary);
			Assert.Contains("Sci:20~", summary);
			Assert.Contains("Lux:10\\/", summary);
		}

		[Fact]
		public void FormatScienceSummary_FormatsCorrectly()
		{
			string summary = AdvisorStatistics.FormatScienceSummary(45, 90, 9);
			Assert.Contains("45/90 (50%)", summary);
			Assert.Contains("+9/turn", summary);
			Assert.Contains("5 turns", summary);
		}
	}
}
