using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class MapRulesTests
	{
		[Theory]
		[InlineData(0, 0, true)]
		[InlineData(79, 49, true)]
		[InlineData(80, 0, false)]
		[InlineData(0, 50, false)]
		[InlineData(-1, 10, false)]
		[InlineData(10, -1, false)]
		public void IsOnMap(int x, int y, bool expected) => Assert.Equal(expected, MapRules.IsOnMap(x, y));

		[Theory]
		[InlineData(0, 0, 0, 0, 0)]
		[InlineData(0, 0, 3, 0, 3)]
		[InlineData(0, 0, 3, 2, 4)]
		[InlineData(0, 0, 2, 3, 4)]
		[InlineData(0, 10, 79, 10, 1)]
		[InlineData(5, 0, 75, 0, 10)]
		[InlineData(0, 0, 40, 0, 40)]
		[InlineData(0, 0, 41, 0, 39)]
		public void DistanceWrapsHorizontally(int x1, int y1, int x2, int y2, int expected)
		{
			Assert.Equal(expected, MapRules.Distance(x1, y1, x2, y2));
			Assert.Equal(expected, MapRules.Distance(x2, y2, x1, y1));
		}

		[Theory]
		[InlineData(4, 2, 5)]
		[InlineData(-4, 2, 5)]
		[InlineData(2, -4, 5)]
		[InlineData(3, 3, 4)]
		[InlineData(0, 7, 7)]
		public void LongerPlusHalfShorter(int dx, int dy, int expected) => Assert.Equal(expected, MapRules.LongerPlusHalfShorter(dx, dy));

		[Theory]
		[InlineData(4, 2, 10)]
		[InlineData(2, 4, 10)]
		[InlineData(79, 0, 2)]
		[InlineData(-3, -3, 9)]
		public void DoubledDistance(int dx, int dy, int expected) => Assert.Equal(expected, MapRules.DoubledDistance(dx, dy));

		[Fact]
		public void NoSpecialResourceOrHutOnPolarRows()
		{
			for (int x = 0; x < MapRules.Width; x++)
			{
				foreach (int y in new[] { 0, 1, 48, 49 })
				{
					for (int seed = 0; seed < 16; seed++)
					{
						Assert.False(MapRules.HasSpecialResource(x, y, seed));
						Assert.False(MapRules.IsHutPlace(x, y, seed));
					}
				}
			}
		}

		[Fact]
		public void EveryInnerFourByFourBlockHasExactlyOneSpecialResource()
		{
			for (int seed = 0; seed < 16; seed++)
			{
				for (int bx = 0; bx < 20; bx++)
				{
					for (int by = 1; by < 12; by++)
					{
						int found = 0;

						for (int x = bx * 4; x < bx * 4 + 4; x++)
							for (int y = by * 4; y < by * 4 + 4; y++)
								found += MapRules.HasSpecialResource(x, y, seed) ? 1 : 0;

						Assert.Equal(1, found);
					}
				}
			}
		}

		[Theory]
		[InlineData(0, 4, 0, true)]
		[InlineData(1, 4, 0, false)]
		[InlineData(2, 5, 0, false)]
		public void SpecialResourceKnownPlaces(int x, int y, int seed, bool expected)
		{
			// Block (0,1): hash = 11 + seed; place in block = (x&3)*4 + (y&3)
			Assert.Equal(expected, MapRules.HasSpecialResource(x, y, seed + 0x10 - 11));
		}

		[Fact]
		public void HutPlaceFollowsBlockHash()
		{
			// Block (1,1): hash = 13 + 11 = 24; (24 + 8) & 0x1f = 0, so the hut is at place 0 = (4,4)
			Assert.True(MapRules.IsHutPlace(4, 4, 0));
			Assert.False(MapRules.IsHutPlace(5, 4, 0));
		}
	}

	public class MovementRulesTests
	{
		[Theory]
		[InlineData(-1, 79)]
		[InlineData(0, 0)]
		[InlineData(79, 79)]
		[InlineData(80, 0)]
		[InlineData(85, 5)]
		public void WrapX(int x, int expected) => Assert.Equal(expected, MovementRules.WrapX(x));

		[Theory]
		[InlineData(0, 79, -1)]
		[InlineData(79, 0, 1)]
		[InlineData(10, 20, 10)]
		[InlineData(0, 40, -40)]
		[InlineData(40, 0, -40)]
		[InlineData(0, 39, 39)]
		public void WrappedDeltaX(int from, int target, int expected) => Assert.Equal(expected, MovementRules.WrappedDeltaX(from, target));

		[Theory]
		[InlineData(5, 5, 5, 4, 1)]
		[InlineData(5, 5, 6, 4, 2)]
		[InlineData(5, 5, 6, 5, 3)]
		[InlineData(5, 5, 6, 6, 4)]
		[InlineData(5, 5, 5, 6, 5)]
		[InlineData(5, 5, 4, 6, 6)]
		[InlineData(5, 5, 4, 5, 7)]
		[InlineData(5, 5, 4, 4, 8)]
		[InlineData(5, 5, 5, 5, 0)]
		[InlineData(5, 5, 7, 5, 0)]
		[InlineData(0, 5, 79, 5, 7)]
		[InlineData(79, 5, 0, 4, 2)]
		public void DirectionTo(int fx, int fy, int tx, int ty, int expected) => Assert.Equal(expected, MovementRules.DirectionTo(fx, fy, tx, ty));

		[Fact]
		public void DirectionVectorsLeadBackToTheirDirection()
		{
			for (int dir = 1; dir <= 8; dir++)
			{
				var (dx, dy) = MovementRules.DirectionVectors[dir];
				Assert.Equal(dir, MovementRules.DirectionTo(10, 10, 10 + dx, 10 + dy));
				Assert.True(MovementRules.IsAdjacent(10, 10, 10 + dx, 10 + dy));
			}
		}

		[Theory]
		[InlineData(10, 10, 10, 10, false)]
		[InlineData(10, 10, 12, 10, false)]
		[InlineData(0, 10, 79, 11, true)]
		public void IsAdjacent(int fx, int fy, int tx, int ty, bool expected) => Assert.Equal(expected, MovementRules.IsAdjacent(fx, fy, tx, ty));

		[Fact]
		public void AdjacentForeignUnitIgnoresOwnAndEmptyTiles()
		{
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (x, y) => -1));
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (x, y) => 0xffff));
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (x, y) => 2));
			Assert.True(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (x, y) => x == 11 && y == 9 ? 3 : -1));
		}

		[Fact]
		public void AdjacentForeignUnitWrapsAcrossDateLine()
		{
			List<(int, int)> asked = new List<(int, int)>();
			MovementRules.HasAdjacentForeignUnit(0, 10, 1, (x, y) => { asked.Add((x, y)); return -1; });

			Assert.Equal(8, asked.Count);
			Assert.Contains((79, 10), asked);
			Assert.All(asked, place => Assert.InRange(place.Item1, 0, 79));
		}
	}

	public class PathfindingRulesTests
	{
		[Theory]
		[InlineData(0, 0)]
		[InlineData(3, 0)]
		[InlineData(4, 1)]
		[InlineData(79, 19)]
		public void ToQuadrant(int value, int expected)
		{
			Assert.Equal(expected, PathfindingRules.ToQuadrantX(value));
			Assert.Equal(expected, PathfindingRules.ToQuadrantY(value));
		}

		[Theory]
		[InlineData(-1, 19)]
		[InlineData(20, 0)]
		[InlineData(7, 7)]
		public void WrapQuadrantX(int value, int expected) => Assert.Equal(expected, PathfindingRules.WrapQuadrantX(value));

		[Theory]
		[InlineData(true, true, false, 1, 1)]
		[InlineData(true, false, false, 2, 6)]
		[InlineData(false, false, true, 2, 3)]
		[InlineData(false, false, false, 0, 1)]
		[InlineData(false, false, false, 3, 9)]
		public void StepMoveCost(bool fromRoad, bool toRoad, bool railroad, int baseCost, int expected) =>
			Assert.Equal(expected, PathfindingRules.CalculateStepMoveCost(fromRoad, toRoad, railroad, baseCost));

		[Theory]
		[InlineData(0xff, 3, 0)]
		[InlineData(0, 3, 0)]
		[InlineData(3, 0, 0)]
		[InlineData(3, 3, 0)]
		[InlineData(1, 2, 1)]
		[InlineData(1, 5, 16)]
		[InlineData(1, 8, 1)]
		[InlineData(2, 7, 9)]
		public void TurnPenalty(int lastDir, int newDir, int expected) => Assert.Equal(expected, PathfindingRules.CalculateTurnPenalty(lastDir, newDir));

		[Fact]
		public void CanStepQuadrantReadsOneBitPerDirection()
		{
			for (int dir = 1; dir <= 8; dir++)
			{
				byte mask = (byte)(1 << (dir - 1));
				Assert.True(PathfindingRules.CanStepQuadrant(mask, dir));
				Assert.False(PathfindingRules.CanStepQuadrant((byte)~mask, dir));
			}

			Assert.False(PathfindingRules.CanStepQuadrant(0xff, 0));
			Assert.False(PathfindingRules.CanStepQuadrant(0xff, 9));
		}

		[Fact]
		public void DiagonalDistanceAgreesWithMapRules()
		{
			for (int dx = -10; dx <= 10; dx++)
				for (int dy = -10; dy <= 10; dy++)
					Assert.Equal(MapRules.LongerPlusHalfShorter(dx, dy), PathfindingRules.DiagonalDistance(dx, dy));
		}
	}
}
