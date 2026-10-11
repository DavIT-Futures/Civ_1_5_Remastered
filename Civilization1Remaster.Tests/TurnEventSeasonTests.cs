using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class TurnEventSeasonTests
	{
		[Theory]
		[InlineData(-4000, 0, -3980)]
		[InlineData(999, 0, 1019)]
		[InlineData(1000, 0, 1010)]
		[InlineData(1499, 0, 1509)]
		[InlineData(1500, 0, 1505)]
		[InlineData(1750, 0, 1752)]   // no starship launched
		[InlineData(1750, 2, 1751)]   // starship flag set => slow down
		[InlineData(1850, 0, 1851)]
		[InlineData(-20, 0, 0)]
		public void ComputeNextSeason_FollowsCalendarTiers(int season, short starship, int expected)
		{
			int next = TurnEventRules.ComputeNextSeason(season, starship);
			// Season 0 is mapped to 1 by the rule; adapt expectation accordingly.
			Assert.Equal(expected == 0 ? 1 : expected, next);
		}

		[Fact]
		public void ComputeNextSeason_Overshoot21_NormalizesTo20()
		{
			Assert.Equal(20, TurnEventRules.ComputeNextSeason(1, 0));
		}

		[Theory]
		[InlineData(1, 50, 50, false)]    // single city
		[InlineData(5, 150, 100, false)]  // rich
		[InlineData(5, 50, 50, false)]    // stable
		[InlineData(5, 50, 100, true)]    // losing 50/turn => projected < 0
		public void CheckBankruptcyWarning_Works(int towns, int gold, int startGold, bool expected)
		{
			Assert.Equal(expected, TurnEventRules.CheckBankruptcyWarning(towns, gold, startGold));
		}
	}
}
