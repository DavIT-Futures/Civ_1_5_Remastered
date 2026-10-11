using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CityGrowthAndProductionRulesTests
	{
		[Theory]
		[InlineData(1, 10, 20)]   // Size 1 -> (1 + 1) * 10 = 20
		[InlineData(2, 10, 30)]   // Size 2 -> (2 + 1) * 10 = 30
		[InlineData(5, 10, 60)]   // Size 5 -> (5 + 1) * 10 = 60
		[InlineData(10, 10, 110)] // Size 10 -> (10 + 1) * 10 = 110
		public void ComputeFoodBoxCapacity_ScalesWithCitySize(int size, int scale, int expectedCapacity)
		{
			Assert.Equal(expectedCapacity, CityGrowthAndProductionRules.ComputeFoodBoxCapacity(size, scale));
		}

		[Fact]
		public void ComputeRetainedFoodAfterGrowth_GranaryRetainsHalf()
		{
			// Growing to size 2:
			// Without granary: 0 retained
			// With granary: (5 * 2) + 5 = 15 retained (half of 30 food box)
			Assert.Equal(0, CityGrowthAndProductionRules.ComputeRetainedFoodAfterGrowth(newCitySize: 2, hasGranary: false));
			Assert.Equal(15, CityGrowthAndProductionRules.ComputeRetainedFoodAfterGrowth(newCitySize: 2, hasGranary: true));

			// Growing to size 5:
			// With granary: (5 * 5) + 5 = 30 retained (half of 60 food box)
			Assert.Equal(30, CityGrowthAndProductionRules.ComputeRetainedFoodAfterGrowth(newCitySize: 5, hasGranary: true));
		}

		[Fact]
		public void ComputeRushBuyCost_BuildingWithPartialShields()
		{
			// Building costs 40 shields, has 10 shields in box -> remaining 30.
			// Base building cost = 30 * 2 = 60 gold.
			int cost = CityGrowthAndProductionRules.ComputeRushBuyCost(
				totalCostShields: 40,
				currentShieldsInBox: 10,
				isUnit: false,
				isWonder: false);

			Assert.Equal(60, cost);
		}

		[Fact]
		public void ComputeRushBuyCost_ZeroShieldsInBoxDoublesCost()
		{
			// Building costs 40 shields, has 0 shields in box -> remaining 40.
			// Base = 40 * 2 = 80 -> doubled for 0 shields = 160 gold!
			int cost = CityGrowthAndProductionRules.ComputeRushBuyCost(
				totalCostShields: 40,
				currentShieldsInBox: 0,
				isUnit: false,
				isWonder: false);

			Assert.Equal(160, cost);
		}

		[Fact]
		public void ComputeRushBuyCost_UnitNonlinearPenalty()
		{
			// Unit costs 20 shields, has 10 shields -> remaining 10.
			// Formula: (10 * 2) + (10 * 10) / 20 = 20 + 5 = 25 gold.
			int cost = CityGrowthAndProductionRules.ComputeRushBuyCost(
				totalCostShields: 20,
				currentShieldsInBox: 10,
				isUnit: true,
				isWonder: false);

			Assert.Equal(25, cost);
		}

		[Fact]
		public void ComputeRushBuyCost_WonderDoublesBuildingCost()
		{
			// Wonder costs 200 shields, has 100 in box -> remaining 100.
			// Base building cost = 100 * 2 = 200. Wonder doubles it = 400 gold.
			int cost = CityGrowthAndProductionRules.ComputeRushBuyCost(
				totalCostShields: 200,
				currentShieldsInBox: 100,
				isUnit: false,
				isWonder: true);

			Assert.Equal(400, cost);
		}
	}
}
