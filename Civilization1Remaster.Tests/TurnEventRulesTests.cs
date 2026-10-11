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

		[Theory]
		[InlineData(0, 2080)] // Chieftain
		[InlineData(1, 2060)] // Warlord
		[InlineData(2, 2040)] // Prince
		[InlineData(3, 2020)] // King
		[InlineData(4, 2000)] // Emperor
		public void ComputeRetirementWarningYear_MatchesCiv1DifficultyFormula(int difficulty, int expectedYear)
		{
			Assert.Equal(expectedYear, TurnEventRules.ComputeRetirementWarningYear(difficulty));
		}

		[Theory]
		[InlineData(0, 2100)] // Chieftain
		[InlineData(1, 2080)] // Warlord
		[InlineData(2, 2060)] // Prince
		[InlineData(3, 2040)] // King
		[InlineData(4, 2020)] // Emperor
		public void ComputeMaxGameYear_MatchesCiv1DifficultyFormula(int difficulty, int expectedYear)
		{
			Assert.Equal(expectedYear, TurnEventRules.ComputeMaxGameYear(difficulty));
		}

		[Fact]
		public void CheckPalaceUpgradeEligible_ValidatesAllConditions()
		{
			// Normal case: eligible
			Assert.True(TurnEventRules.CheckPalaceUpgradeEligible(
				palaceScoreRequired: 100,
				currentScore: 120,
				currentCastleTier: 5,
				palaceEnabled: true,
				starshipMarkers: 0));

			// Score too low
			Assert.False(TurnEventRules.CheckPalaceUpgradeEligible(
				palaceScoreRequired: 100,
				currentScore: 90,
				currentCastleTier: 5,
				palaceEnabled: true,
				starshipMarkers: 0));

			// Max palace tier (37) reached
			Assert.False(TurnEventRules.CheckPalaceUpgradeEligible(
				palaceScoreRequired: 100,
				currentScore: 120,
				currentCastleTier: 37,
				palaceEnabled: true,
				starshipMarkers: 0));

			// Palace disabled in preferences
			Assert.False(TurnEventRules.CheckPalaceUpgradeEligible(
				palaceScoreRequired: 100,
				currentScore: 120,
				currentCastleTier: 5,
				palaceEnabled: false,
				starshipMarkers: 0));

			// Starship arrived (victory bit 0x100 set)
			Assert.False(TurnEventRules.CheckPalaceUpgradeEligible(
				palaceScoreRequired: 100,
				currentScore: 120,
				currentCastleTier: 5,
				palaceEnabled: true,
				starshipMarkers: 0x100));
		}

		[Fact]
		public void CheckBankruptcyWarning_TriggersWhenLosingGoldTowardsNegative()
		{
			// Safe: only 1 city
			Assert.False(TurnEventRules.CheckBankruptcyWarning(townTally: 1, currentGold: 20, turnStartGold: 30));

			// Safe: gold >= 100
			Assert.False(TurnEventRules.CheckBankruptcyWarning(townTally: 3, currentGold: 105, turnStartGold: 120));

			// Safe: positive balance (delta >= 0)
			Assert.False(TurnEventRules.CheckBankruptcyWarning(townTally: 3, currentGold: 50, turnStartGold: 40));

			// In danger: 3 cities, 50g, lost 10g => projected = 50 + (-10)*10 = -50 < 0
			Assert.True(TurnEventRules.CheckBankruptcyWarning(townTally: 3, currentGold: 50, turnStartGold: 60));
		}
	}
}
