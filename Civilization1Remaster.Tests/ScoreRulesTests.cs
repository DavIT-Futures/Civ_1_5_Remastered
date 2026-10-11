using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class ScoreRulesTests
	{
		[Fact]
		public void ComputeCitizenScore_CorrectWeights()
		{
			// Happy = 2 points each, Content = 1 point each
			int score = ScoreRules.ComputeCitizenScore(happyCitizens: 5, contentCitizens: 10);
			Assert.Equal(20, score); // (5 * 2) + 10 = 20
		}

		[Fact]
		public void ComputeWonderScore_TwentyPointsPerWonder()
		{
			Assert.Equal(0, ScoreRules.ComputeWonderScore(0));
			Assert.Equal(20, ScoreRules.ComputeWonderScore(1));
			Assert.Equal(140, ScoreRules.ComputeWonderScore(7));
		}

		[Fact]
		public void ComputeSpaceshipScore_HalvedProduct()
		{
			// Formula: (victoryRatio * inhabitants) / 2
			int score = ScoreRules.ComputeSpaceshipScore(victoryRatio: 100, inhabitants: 10);
			Assert.Equal(500, score);

			int zeroScore = ScoreRules.ComputeSpaceshipScore(0, 10);
			Assert.Equal(0, zeroScore);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(10, 30)]
		[InlineData(33, 99)]
		[InlineData(34, 100)]   // Clamped at 100
		[InlineData(50, 100)]   // Clamped at 100
		public void ComputePeaceBonus_ThreePointsPerTurnClampedAt100(int truceRounds, int expectedBonus)
		{
			Assert.Equal(expectedBonus, ScoreRules.ComputePeaceBonus(truceRounds));
		}

		[Fact]
		public void ComputeFutureTechScore_FivePointsEach()
		{
			Assert.Equal(0, ScoreRules.ComputeFutureTechScore(0));
			Assert.Equal(15, ScoreRules.ComputeFutureTechScore(3));
			Assert.Equal(50, ScoreRules.ComputeFutureTechScore(10));
		}

		[Fact]
		public void ComputePollutionPenalty_TenPointsPerDirtyTile()
		{
			Assert.Equal(0, ScoreRules.ComputePollutionPenalty(0));
			Assert.Equal(30, ScoreRules.ComputePollutionPenalty(3));
			Assert.Equal(100, ScoreRules.ComputePollutionPenalty(10));
		}

		[Fact]
		public void ComputeConquestBonus_Formula()
		{
			// Formula: 400 + (100 * rivals) + ((550 - round) * 2)
			// E.g., 7 rivals eliminated at round 250:
			// 400 + 700 + (300 * 2) = 1100 + 600 = 1700
			int bonus = ScoreRules.ComputeConquestBonus(rivalCount: 7, roundTally: 250);
			Assert.Equal(1700, bonus);
		}

		[Fact]
		public void ComputeTotalScore_AggregatesCategories()
		{
			// Citizens: 50, Wonders: 60 (3 wonders), Spaceship: 0, Peace: 90, FutureTech: 20, Pollution: 10
			// Total = 50 + 60 + 0 + 90 + 20 - 10 = 210
			int total = ScoreRules.ComputeTotalScore(
				citizenScore: 50,
				wonderScore: 60,
				spaceshipScore: 0,
				peaceBonus: 90,
				futureTechScore: 20,
				pollutionPenalty: 10);

			Assert.Equal(210, total);
		}

		[Fact]
		public void ComputeTotalScore_EarlyConquestBonusTakesPrecedenceIfHigher()
		{
			int regularScore = 250;
			int conquestBonus = 1500;

			int total = ScoreRules.ComputeTotalScore(
				citizenScore: regularScore,
				wonderScore: 0,
				spaceshipScore: 0,
				peaceBonus: 0,
				futureTechScore: 0,
				pollutionPenalty: 0,
				earlyConquestBonus: conquestBonus);

			Assert.Equal(1500, total);
		}
	}
}
