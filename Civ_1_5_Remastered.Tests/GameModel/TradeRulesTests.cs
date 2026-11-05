using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class TradeRulesTests
	{
		[Fact]
		public void TradeDistanceAgreesWithMapRules()
		{
			for (int horizontal = 0; horizontal < 80; horizontal += 7)
			{
				for (int vertical = 0; vertical < 50; vertical += 5)
				{
					Assert.Equal(MapRules.Distance(3, 4, horizontal, vertical), TradeRules.ComputeDistance(3, 4, horizontal, vertical));
				}
			}
		}

		[Theory]
		[InlineData(0, 5, 10, 0)]
		[InlineData(100, 0, 10, 0)]
		[InlineData(110, 3, 10, 30)]
		[InlineData(20000, 3, 10, 5454)]
		public void CityPlunder(int gold, int size, int empire, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeCityPlunder(gold, size, empire));

		[Theory]
		[InlineData(316, 177, 139)]
		[InlineData(100, 150, -50)]
		[InlineData(0, 0, 0)]
		public void ComputeNetIncome(int income, int costs, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeNetIncome(income, costs));

		[Theory]
		[InlineData(800, 333, 3)]
		[InlineData(100, 333, 1)]
		[InlineData(0, 333, 1)]
		[InlineData(500, 0, 0)]
		public void ComputeResearchTurns(int needed, int science, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeResearchTurns(needed, science));
	}
}
