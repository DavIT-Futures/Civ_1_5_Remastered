using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class ScoreRulesTests
	{
		[Fact]
		public void Components()
		{
			Assert.Equal(8, ScoreRules.ComputeCitizenScore(2, 3, 1));
			Assert.Equal(250, ScoreRules.ComputeSpaceshipScore(50, 10));
			Assert.Equal(0, ScoreRules.ComputeSpaceshipScore(-1, 10));
			Assert.Equal(30, ScoreRules.ComputePeaceBonus(10));
			Assert.Equal(100, ScoreRules.ComputePeaceBonus(40));
			Assert.Equal(0, ScoreRules.ComputePeaceBonus(-1));
			Assert.Equal(15, ScoreRules.ComputeFutureTechScore(3));
			Assert.Equal(20, ScoreRules.ComputePollutionPenalty(2));
			Assert.Equal(1600, ScoreRules.ComputeConquestBonus(3, 100));
		}
	}
}
