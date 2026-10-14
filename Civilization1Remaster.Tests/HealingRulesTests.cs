using System.Drawing;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class HealingRulesTests
	{
		private const int LandGroupLand = 0;
		private const int LandGroupAir = 1;
		private const int LandGroupSea = 2;

		[Fact]
		public void UnitsInCityHealTwoHpPerTurn()
		{
			// Land unit in city
			int landHeal = HealingRules.CalculateHealAmount(
				landGroup: LandGroupLand,
				inFriendlyCity: true,
				isResting: false,
				isAdjacentToLand: false,
				distToNearestFriendlyCity: 0);

			Assert.Equal(2, landHeal);

			// Sea unit in city
			int seaHeal = HealingRules.CalculateHealAmount(
				landGroup: LandGroupSea,
				inFriendlyCity: true,
				isResting: false,
				isAdjacentToLand: false,
				distToNearestFriendlyCity: 0);

			Assert.Equal(2, seaHeal);

			// Air unit in city
			int airHeal = HealingRules.CalculateHealAmount(
				landGroup: LandGroupAir,
				inFriendlyCity: true,
				isResting: false,
				isAdjacentToLand: false,
				distToNearestFriendlyCity: 0);

			Assert.Equal(2, airHeal);
		}

		[Fact]
		public void LandUnitOutsideCityHealsOneHpIfFortified()
		{
			int fortified = HealingRules.CalculateHealAmount(
				landGroup: LandGroupLand,
				inFriendlyCity: false,
				isResting: true,
				isAdjacentToLand: true,
				distToNearestFriendlyCity: 10);

			Assert.Equal(1, fortified);

			int notFortified = HealingRules.CalculateHealAmount(
				landGroup: LandGroupLand,
				inFriendlyCity: false,
				isResting: false,
				isAdjacentToLand: true,
				distToNearestFriendlyCity: 10);

			Assert.Equal(0, notFortified);
		}

		[Fact]
		public void ShipOutsideCityHealsOnlyNearCoastAndWithinDistanceThree()
		{
			// Fortified, adjacent to coast, distance 3 -> Heals 1 HP
			int validHeal = HealingRules.CalculateHealAmount(
				landGroup: LandGroupSea,
				inFriendlyCity: false,
				isResting: true,
				isAdjacentToLand: true,
				distToNearestFriendlyCity: 3);

			Assert.Equal(1, validHeal);

			// Fortified, adjacent to coast, but distance 4 -> Cannot heal (0 HP)
			int tooFar = HealingRules.CalculateHealAmount(
				landGroup: LandGroupSea,
				inFriendlyCity: false,
				isResting: true,
				isAdjacentToLand: true,
				distToNearestFriendlyCity: 4);

			Assert.Equal(0, tooFar);

			// Fortified, distance 2, but deep ocean (not adjacent to land) -> Cannot heal (0 HP)
			int deepOcean = HealingRules.CalculateHealAmount(
				landGroup: LandGroupSea,
				inFriendlyCity: false,
				isResting: true,
				isAdjacentToLand: false,
				distToNearestFriendlyCity: 2);

			Assert.Equal(0, deepOcean);

			// Adjacent to coast, distance 1, but NOT fortified -> Cannot heal (0 HP)
			int notFortified = HealingRules.CalculateHealAmount(
				landGroup: LandGroupSea,
				inFriendlyCity: false,
				isResting: false,
				isAdjacentToLand: true,
				distToNearestFriendlyCity: 1);

			Assert.Equal(0, notFortified);
		}

		[Fact]
		public void IsAdjacentToLandDetectsSurroundingCoast()
		{
			// Grid where (10, 10) is ocean, and (11, 10) is land (not ocean)
			bool oceanOnly(int x, int y) => !(x == 11 && y == 10);

			Assert.True(HealingRules.IsAdjacentToLand(10, 10, oceanOnly));

			// Grid where all neighbors are ocean
			bool allOcean(int x, int y) => true;

			Assert.False(HealingRules.IsAdjacentToLand(10, 10, allOcean));

			// Wrap-around test at world border x = 0 with land at x = 79
			bool borderLand(int x, int y) => !(x == 79 && y == 10);

			Assert.True(HealingRules.IsAdjacentToLand(0, 10, borderLand));
		}

		[Fact]
		public void ComputeDistanceToNearestFriendlyCityCalculatesMinDistance()
		{
			var cities = new[]
			{
				new Point(20, 20),
				new Point(5, 5),
				new Point(75, 5)
			};

			// Distance from (2, 5) to (5, 5) is 3; to (75, 5) wrapped is (80 - 73) = 7
			int dist = HealingRules.ComputeDistanceToNearestFriendlyCity(2, 5, cities);
			Assert.Equal(3, dist);

			// Distance from (78, 5) to (75, 5) is 3
			int distWrap = HealingRules.ComputeDistanceToNearestFriendlyCity(78, 5, cities);
			Assert.Equal(3, distWrap);
		}

		[Fact]
		public void MendAmountHealsSpecifiedPoints()
		{
			var vitals = new UnitHealth();
			vitals.Assign(0, 0, 1, 3, false); // 3 missing HP

			int healed = vitals.MendAmount(0, 0, 1, 2);
			Assert.Equal(2, healed);
			Assert.Equal(1, vitals.ObtainLostHitPoints(0, 0, 1));

			// Mend 2 more, but only 1 missing
			int healedRemaining = vitals.MendAmount(0, 0, 1, 2);
			Assert.Equal(1, healedRemaining);
			Assert.Equal(0, vitals.ObtainLostHitPoints(0, 0, 1));
		}
	}
}
