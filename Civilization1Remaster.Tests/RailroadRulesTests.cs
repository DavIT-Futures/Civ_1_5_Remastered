using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class RailroadRulesTests
	{
		private const int City = RailroadRules.CityBit | RailroadRules.RoadBit;
		private const int Railroad = RailroadRules.RoadBit | RailroadRules.RailroadBit;

		[Fact]
		public void CityHasRailroadOnceThePlayerKnowsRailroad()
		{
			Assert.True(RailroadRules.HasRailroad(City, playerKnowsRailroad: true, classic: false));
			Assert.False(RailroadRules.HasRailroad(City, playerKnowsRailroad: false, classic: false));
			Assert.True(RailroadRules.HasRailroad(Railroad, playerKnowsRailroad: false, classic: false));
			Assert.False(RailroadRules.HasRailroad(RailroadRules.RoadBit, playerKnowsRailroad: true, classic: false));
		}

		[Fact]
		public void ClassicCityHasOnlyARoad()
		{
			Assert.False(RailroadRules.HasRailroad(City, playerKnowsRailroad: true, classic: true));
			Assert.True(RailroadRules.HasRailroad(Railroad, playerKnowsRailroad: true, classic: true));
		}

		[Fact]
		public void UnitsNotMovedByHandRideForFreeThirtyTimesATurn()
		{
			Assert.True(RailroadRules.IsFreeStep(movedByHand: false, freeStepsThisTurn: 0, classic: false));
			Assert.True(RailroadRules.IsFreeStep(movedByHand: false, freeStepsThisTurn: 29, classic: false));
			Assert.False(RailroadRules.IsFreeStep(movedByHand: false, freeStepsThisTurn: 30, classic: false));
			Assert.True(RailroadRules.IsFreeStep(movedByHand: true, freeStepsThisTurn: 1000, classic: false));
		}

		[Fact]
		public void ClassicOnlyTheHumanByHandRidesForFree()
		{
			Assert.True(RailroadRules.IsFreeStep(movedByHand: true, freeStepsThisTurn: 0, classic: true));
			Assert.False(RailroadRules.IsFreeStep(movedByHand: false, freeStepsThisTurn: 0, classic: true));
		}
	}
}
