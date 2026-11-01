using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class ScienceRulesTests
	{
		[Theory]
		[InlineData(0x22, true)]
		[InlineData(0x25, true)]
		[InlineData(0x23, false)]
		public void BarracksObsolescence(int tech, bool expected) => Assert.Equal(expected, ScienceRules.DoesTechObsoleteBarracks(tech));

		[Fact]
		public void PlayerKnowsTechnologyReadsBitPerTech()
		{
			ushort[] markers = new ushort[5];
			markers[1] = 1 << 1;

			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 1, 17));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 16));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 0, 17));
			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 0, -1));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 71));
			Assert.False(ScienceRules.PlayerKnowsTechnology(new ushort[1], 1, 20));
		}

		[Fact]
		public void TechnologyInfo() => Assert.Equal(13, ScienceRules.CalculateTechnologyInfo(3, 2, 5));
	}

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

	public class TradeRulesTests
	{
		[Fact]
		public void TradeDistanceAgreesWithMapRules()
		{
			for (int x = 0; x < 80; x += 7)
				for (int y = 0; y < 50; y += 5)
					Assert.Equal(MapRules.Distance(3, 4, x, y), TradeRules.ComputeDistance(3, 4, x, y));
		}

		[Theory]
		[InlineData(0, 5, 10, 0)]
		[InlineData(100, 0, 10, 0)]
		[InlineData(110, 3, 10, 30)]
		[InlineData(20000, 3, 10, 5454)]
		public void CityPlunder(int gold, int size, int empire, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeCityPlunder(gold, size, empire));
	}
}
