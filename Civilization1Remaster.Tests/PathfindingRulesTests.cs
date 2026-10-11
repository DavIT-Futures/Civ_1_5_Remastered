using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class PathfindingRulesTests
	{
		[Theory]
		[InlineData(0, 0)]
		[InlineData(3, 0)]
		[InlineData(4, 1)]
		[InlineData(79, 19)]
		public void ToQuadrantXCalculatesCorrectQuadrantIndex(int x, int expected)
		{
			Assert.Equal(expected, PathfindingRules.ToQuadrantX(x));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(3, 0)]
		[InlineData(4, 1)]
		[InlineData(49, 12)]
		public void ToQuadrantYCalculatesCorrectQuadrantIndex(int y, int expected)
		{
			Assert.Equal(expected, PathfindingRules.ToQuadrantY(y));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(19, 19)]
		[InlineData(20, 0)]
		[InlineData(21, 1)]
		[InlineData(-1, 19)]
		[InlineData(-2, 18)]
		public void WrapQuadrantXWrapsCylinderAccurately(int qx, int expected)
		{
			Assert.Equal(expected, PathfindingRules.WrapQuadrantX(qx));
		}

		[Theory]
		[InlineData(true, true, false, 1, 1)]    // Road to road -> 1 point (1/3 move)
		[InlineData(true, true, true, 1, 1)]     // Road to road with railroad unit -> 1 point
		[InlineData(true, false, true, 1, 3)]    // No road, railroad unit -> 3 points (1 full move)
		[InlineData(false, false, true, 2, 3)]   // No road, railroad unit on hills -> 3 points
		[InlineData(false, false, false, 1, 3)]  // Plains (cost 1) -> 3 points
		[InlineData(false, false, false, 2, 6)]  // Hills (cost 2) -> 6 points
		[InlineData(false, false, false, 3, 9)]  // Mountains (cost 3) -> 9 points
		public void CalculateStepMoveCostMatchesCiv1MovementScale(bool fromRoad, bool toRoad, bool isRailroad, int baseCost, int expected)
		{
			Assert.Equal(expected, PathfindingRules.CalculateStepMoveCost(fromRoad, toRoad, isRailroad, baseCost));
		}

		[Theory]
		[InlineData(0xff, 1, 0)]  // First move (no previous direction)
		[InlineData(0, 1, 0)]     // No previous direction
		[InlineData(1, 1, 0)]     // Straight ahead -> 0 penalty
		[InlineData(1, 2, 1)]     // 45 degrees turn -> 1
		[InlineData(1, 3, 4)]     // 90 degrees turn -> 4
		[InlineData(1, 4, 9)]     // 135 degrees turn -> 9
		[InlineData(1, 5, 16)]    // 180 degrees U-turn -> 16
		[InlineData(1, 8, 1)]     // 45 degrees left (wrapping 1 to 8) -> 1
		[InlineData(2, 7, 9)]     // 135 degrees turn (wrapping around 8 directions) -> 9
		[InlineData(3, 7, 16)]    // Opposite directions (3 and 7) -> 16
		public void CalculateTurnPenaltyMatchesDos8086SteeringRules(int lastDir, int newDir, int expected)
		{
			Assert.Equal(expected, PathfindingRules.CalculateTurnPenalty(lastDir, newDir));
		}

		[Theory]
		[InlineData(0x01, 1, true)]
		[InlineData(0x01, 2, false)]
		[InlineData(0x02, 2, true)]
		[InlineData(0x80, 8, true)]
		[InlineData(0x7f, 8, false)]
		[InlineData(0xff, 1, true)]
		[InlineData(0xff, 8, true)]
		[InlineData(0xff, 0, false)] // Invalid direction
		[InlineData(0xff, 9, false)] // Invalid direction
		public void CanStepQuadrantVerifiesPassableQuadrantMask(byte mask, int dir, bool expected)
		{
			Assert.Equal(expected, PathfindingRules.CanStepQuadrant(mask, dir));
		}

		[Theory]
		[InlineData(0, 0, 0)]
		[InlineData(4, 0, 4)]
		[InlineData(0, 4, 4)]
		[InlineData(4, 2, 5)]  // max(4,2) + min(4,2)/2 = 4 + 1 = 5
		[InlineData(10, 6, 13)] // 10 + 6/2 = 13
		[InlineData(6, 10, 13)] // 10 + 6/2 = 13
		public void DiagonalDistanceComputesCiv1CoordinateMetric(int dx, int dy, int expected)
		{
			Assert.Equal(expected, PathfindingRules.DiagonalDistance(dx, dy));
		}
	}
}
