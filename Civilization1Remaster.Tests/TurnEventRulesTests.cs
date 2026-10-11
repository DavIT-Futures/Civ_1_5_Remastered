using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class TurnEventRulesTests
	{
		[Fact]
		public void ComputeGlobalWarmingStep_IncreasesTowardsEquilibrium()
		{
			// Dirty cells = 10, heating tally = 0, current smog = 0.
			// delta = (10 * 2) - 0 - 0 = 20 > 0 => step = +1. Next = 1.
			var (nextSmog, warning, warming) = TurnEventRules.ComputeGlobalWarmingStep(
				dirtyCellTally: 10,
				worldHeatingTally: 0,
				currentSmogTier: 0);

			Assert.Equal(1, nextSmog);
			Assert.False(warning);
			Assert.False(warming);
		}

		[Fact]
		public void ComputeGlobalWarmingStep_TriggersWarningAtTier12WithHighPollution()
		{
			// Current tier 11, step +1 => tier 12.
			// Dirty cells = 8 (> 6) => warning should trigger!
			var (nextSmog, warning, warming) = TurnEventRules.ComputeGlobalWarmingStep(
				dirtyCellTally: 8,
				worldHeatingTally: 0,
				currentSmogTier: 11);

			Assert.Equal(12, nextSmog);
			Assert.True(warning);
			Assert.False(warming);
		}

		[Fact]
		public void ComputeGlobalWarmingStep_TriggersWarmingWhenExceeding16AndResets()
		{
			// Current tier 16, step +1 => 17 (> 16).
			// Warming triggers and resets nextSmogTier to 0!
			var (nextSmog, warning, warming) = TurnEventRules.ComputeGlobalWarmingStep(
				dirtyCellTally: 15,
				worldHeatingTally: 0,
				currentSmogTier: 16);

			Assert.True(warming);
			Assert.Equal(0, nextSmog);
		}

		[Fact]
		public void ComputeGlobalWarmingStep_DecreasesWhenPollutionCleanedUp()
		{
			// Dirty cells = 1, current smog = 10, heating tally = 0.
			// delta = (1 * 2) - 0 - 10 = -8 < 0 => step = -1. Next = 9.
			var (nextSmog, warning, warming) = TurnEventRules.ComputeGlobalWarmingStep(
				dirtyCellTally: 1,
				worldHeatingTally: 0,
				currentSmogTier: 10);

			Assert.Equal(9, nextSmog);
			Assert.False(warning);
			Assert.False(warming);
		}

		[Theory]
		[InlineData(0, 0, 1)]  // sum = 0 => (0/2) + 1 = 1
		[InlineData(1, 0, 4)]  // diffFactor = 7, i=0..1 => sum = 0 + 7 = 7 => (7/2) + 1 = 4
		[InlineData(2, 0, 11)] // i=0..2 => sum = 0 + 7 + 14 = 21 => (21/2) + 1 = 11
		public void ComputePalaceScoreForLevel_MatchesAssemblyProgression(int level, int difficulty, int expectedScore)
		{
			int score = TurnEventRules.ComputePalaceScoreForLevel(level, difficulty);
			Assert.Equal(expectedScore, score);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(5, 0)]
		[InlineData(6, 1)]
		[InlineData(15, 1)]
		[InlineData(16, 2)]
		public void ComputePalaceLevel_CalculatesTiers(int rating, int expectedLevel)
		{
			Assert.Equal(expectedLevel, TurnEventRules.ComputePalaceLevel(rating));
		}
	}
}
