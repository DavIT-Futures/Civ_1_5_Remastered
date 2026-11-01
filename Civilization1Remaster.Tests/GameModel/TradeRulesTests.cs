using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class TradeRulesTests
	{
		[Fact]
		public void TradeDistanceAgreesWithMapRules()
		{
			for (int x = 0; x < 80; x += 7)
			{
				for (int y = 0; y < 50; y += 5)
				{
					Assert.Equal(MapRules.Distance(3, 4, x, y), TradeRules.ComputeDistance(3, 4, x, y));
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
	}
}
