using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class TradeRulesTests
	{
		[Theory]
		[InlineData(10, 10, 10, 10, 0)]    // Identical position
		[InlineData(10, 10, 14, 10, 4)]    // Direct horizontal dx=4, dy=0 -> (0/2) + 4 = 4
		[InlineData(10, 10, 10, 14, 4)]    // Direct vertical dx=0, dy=4 -> (0/2) + 4 = 4
		[InlineData(10, 10, 14, 14, 6)]    // Diagonal dx=4, dy=4 -> (4/2) + 4 = 6
		[InlineData(10, 10, 20, 14, 12)]   // dx=10, dy=4 -> (4/2) + 10 = 12
		[InlineData(0, 10, 79, 10, 1)]     // Cylindrical wrap: distance across edge 0 <-> 79 is 1
		[InlineData(5, 20, 75, 20, 10)]    // dx=70 wraps to 80-70=10, dy=0 -> 10
		public void ComputeDistanceComputesCorrectCylindricalDistance(int x1, int y1, int x2, int y2, int expectedDistance)
		{
			int dist = TradeRules.ComputeDistance(x1, y1, x2, y2);
			Assert.Equal(expectedDistance, dist);
		}

		[Fact]
		public void ComputeOneTimeTradeBonus_StandardForeignDifferentContinent()
		{
			// Distance 14, source commerce 8, dest commerce 12. Combined = 20.
			// Base = (14 + 10) * 20 / 24 = 24 * 20 / 24 = 20.
			// Different continent, foreign civilization, no railroad, no flight.
			int bonus = TradeRules.ComputeOneTimeTradeBonus(
				distance: 14,
				sourceCityBaseTrade: 8,
				destCityBaseTrade: 12,
				sameContinent: false,
				sameCivilization: false,
				destHasRailroad: false,
				destHasFlight: false);

			Assert.Equal(20, bonus);
		}

		[Fact]
		public void ComputeOneTimeTradeBonus_SameContinentHalved()
		{
			// Base = 20 -> Halved for same continent = 10.
			int bonus = TradeRules.ComputeOneTimeTradeBonus(
				distance: 14,
				sourceCityBaseTrade: 8,
				destCityBaseTrade: 12,
				sameContinent: true,
				sameCivilization: false,
				destHasRailroad: false,
				destHasFlight: false);

			Assert.Equal(10, bonus);
		}

		[Fact]
		public void ComputeOneTimeTradeBonus_DomesticHalved()
		{
			// Base = 20 -> Halved for domestic = 10.
			int bonus = TradeRules.ComputeOneTimeTradeBonus(
				distance: 14,
				sourceCityBaseTrade: 8,
				destCityBaseTrade: 12,
				sameContinent: false,
				sameCivilization: true,
				destHasRailroad: false,
				destHasFlight: false);

			Assert.Equal(10, bonus);
		}

		[Fact]
		public void ComputeOneTimeTradeBonus_SameContinentAndDomesticQuartered()
		{
			// Base = 20 -> Halved for continent (10) -> Halved for domestic (5).
			int bonus = TradeRules.ComputeOneTimeTradeBonus(
				distance: 14,
				sourceCityBaseTrade: 8,
				destCityBaseTrade: 12,
				sameContinent: true,
				sameCivilization: true,
				destHasRailroad: false,
				destHasFlight: false);

			Assert.Equal(5, bonus);
		}

		[Fact]
		public void ComputeOneTimeTradeBonus_RailroadAndFlightPenalties()
		{
			// Distance 38, combined commerce 30 -> Base = (38 + 10) * 30 / 24 = 48 * 30 / 24 = 60.
			// Railroad penalty: 60 - (60 / 3) = 40.
			int bonusRailroad = TradeRules.ComputeOneTimeTradeBonus(
				distance: 38,
				sourceCityBaseTrade: 15,
				destCityBaseTrade: 15,
				sameContinent: false,
				sameCivilization: false,
				destHasRailroad: true,
				destHasFlight: false);

			Assert.Equal(40, bonusRailroad);

			// Railroad and flight penalties: 60 - 20 = 40, then 40 - (40 / 3) = 40 - 13 = 27.
			int bonusBoth = TradeRules.ComputeOneTimeTradeBonus(
				distance: 38,
				sourceCityBaseTrade: 15,
				destCityBaseTrade: 15,
				sameContinent: false,
				sameCivilization: false,
				destHasRailroad: true,
				destHasFlight: true);

			Assert.Equal(27, bonusBoth);
		}

		[Theory]
		[InlineData(10, 10, true, 1)]   // (10 + 10 + 4) / 16 = 24 / 16 = 1
		[InlineData(10, 10, false, 3)]  // (10 + 10 + 4) / 8 = 24 / 8 = 3
		[InlineData(20, 20, true, 2)]   // (20 + 20 + 4) / 16 = 44 / 16 = 2
		[InlineData(20, 20, false, 5)]  // (20 + 20 + 4) / 8 = 44 / 8 = 5
		[InlineData(0, 0, false, 0)]    // (0 + 0 + 4) / 8 = 4 / 8 = 0
		public void ComputeOngoingTradeRouteArrowsCalculatesCorrectTurnYield(int trade1, int trade2, bool domestic, int expected)
		{
			int arrows = TradeRules.ComputeOngoingTradeRouteArrows(trade1, trade2, domestic);
			Assert.Equal(expected, arrows);
		}

		[Fact]
		public void ComputeCityPlunder_CalculatesProportionalTreasuryLoot()
		{
			// City size 4, total empire size 19 (denominator = 20), victim gold 500.
			// 0x7d00 / 4 = 32000 / 4 = 8000. 500 < 8000, so plunder = (500 / 20) * 4 = 25 * 4 = 100.
			int plunderSmallTreasury = TradeRules.ComputeCityPlunder(500, 4, 19);
			Assert.Equal(100, plunderSmallTreasury);

			// Rich victim: gold 10000 >= 8000.
			// plunder = (10000 * 4) / 20 = 40000 / 20 = 2000.
			int plunderRichTreasury = TradeRules.ComputeCityPlunder(10000, 4, 19);
			Assert.Equal(2000, plunderRichTreasury);
		}

		[Fact]
		public void ComputeCityPlunder_HandlesZeroGoldOrZeroSize()
		{
			Assert.Equal(0, TradeRules.ComputeCityPlunder(0, 5, 20));
			Assert.Equal(0, TradeRules.ComputeCityPlunder(100, 0, 20));
			Assert.Equal(0, TradeRules.ComputeCityPlunder(-50, 4, 20));
		}

		[Fact]
		public void TryAssignTradeRouteSlot_AllocatesEmptySlotOrReplacesWeakest()
		{
			// Slots initially: [5, empty, empty]
			sbyte[] routes = new sbyte[] { 5, -1, -1 };

			// Should assign to slot 1 (first empty)
			int assignedSlot = TradeRules.TryAssignTradeRouteSlot(routes, newCityId: 10, newRouteValue: 8, _ => 4);
			Assert.Equal(1, assignedSlot);
			Assert.Equal(10, routes[1]);

			// Already existing city 5 returns existing slot (0)
			int existingSlot = TradeRules.TryAssignTradeRouteSlot(routes, newCityId: 5, newRouteValue: 8, _ => 4);
			Assert.Equal(0, existingSlot);

			// Fill slot 2 with city 12 (trade value 2)
			routes[2] = 12;

			// Now routes are: [5 (val 6), 10 (val 8), 12 (val 2)]
			// City 15 with value 10 should replace weakest slot (slot 2, city 12)
			int replacedSlot = TradeRules.TryAssignTradeRouteSlot(
				routes,
				newCityId: 15,
				newRouteValue: 10,
				routeEvaluator: cityId => cityId switch { 5 => 6, 10 => 8, 12 => 2, _ => 0 });

			Assert.Equal(2, replacedSlot);
			Assert.Equal(15, routes[2]);
		}
	}
}
