using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class MovementRulesTests
	{
		[Theory]
		[InlineData(0, 0)]
		[InlineData(79, 79)]
		[InlineData(80, 0)]
		[InlineData(81, 1)]
		[InlineData(-1, 79)]
		[InlineData(-2, 78)]
		public void WrapXAdjustsCoordinatesCorrectly(int input, int expected)
		{
			Assert.Equal(expected, MovementRules.WrapX(input));
		}

		[Theory]
		[InlineData(10, 11, 1)]
		[InlineData(10, 9, -1)]
		[InlineData(0, 79, -1)]   // Wrapping west across map boundary
		[InlineData(79, 0, 1)]    // Wrapping east across map boundary
		[InlineData(0, 40, -40)]  // Halfway boundary
		[InlineData(0, 39, 39)]
		public void WrappedDeltaXCalculatesShortestPath(int fromX, int targetX, int expectedDx)
		{
			Assert.Equal(expectedDx, MovementRules.WrappedDeltaX(fromX, targetX));
		}

		[Theory]
		[InlineData(10, 10, 10, 9, 1)]   // North
		[InlineData(10, 10, 11, 9, 2)]   // North-East
		[InlineData(10, 10, 11, 10, 3)]  // East
		[InlineData(10, 10, 11, 11, 4)]  // South-East
		[InlineData(10, 10, 10, 11, 5)]  // South
		[InlineData(10, 10, 9, 11, 6)]   // South-West
		[InlineData(10, 10, 9, 10, 7)]   // West
		[InlineData(10, 10, 9, 9, 8)]    // North-West
		[InlineData(0, 10, 79, 10, 7)]   // West wrapped across 0 boundary
		[InlineData(79, 10, 0, 10, 3)]   // East wrapped across 79 boundary
		[InlineData(10, 10, 10, 10, 0)]  // Same tile -> 0
		[InlineData(10, 10, 12, 10, 0)]  // Not adjacent -> 0
		public void DirectionToIdentifiesCorrectCompassDirection(int fromX, int fromY, int toX, int toY, int expectedDir)
		{
			Assert.Equal(expectedDir, MovementRules.DirectionTo(fromX, fromY, toX, toY));
		}

		[Fact]
		public void IsAdjacentVerifies8Neighbors()
		{
			for (int dir = 1; dir <= 8; dir++)
			{
				var (dx, dy) = MovementRules.DirectionVectors[dir];
				int targetX = MovementRules.WrapX(0 + dx);
				int targetY = 10 + dy;
				Assert.True(MovementRules.IsAdjacent(0, 10, targetX, targetY));
			}

			Assert.False(MovementRules.IsAdjacent(0, 10, 0, 10)); // Same tile
			Assert.False(MovementRules.IsAdjacent(0, 10, 2, 10)); // 2 tiles away
		}

		[Fact]
		public void HasAdjacentForeignUnitDetectsEnemiesAcrossAllDirectionsAndBoundary()
		{
			short myPlayerId = 1;
			short enemyPlayerId = 2;

			// Scenario 1: Empty surroundings -> false
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, myPlayerId, (x, y) => -1));

			// Scenario 2: Friendly unit nearby -> false
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, myPlayerId, (x, y) => myPlayerId));

			// Scenario 3: Enemy at each of the 8 directions -> true
			for (int dir = 1; dir <= 8; dir++)
			{
				var (dx, dy) = MovementRules.DirectionVectors[dir];
				int enemyX = MovementRules.WrapX(10 + dx);
				int enemyY = 10 + dy;

				bool detected = MovementRules.HasAdjacentForeignUnit(10, 10, myPlayerId, (x, y) =>
				{
					return (x == enemyX && y == enemyY) ? enemyPlayerId : -1;
				});

				Assert.True(detected);
			}

			// Scenario 4: Enemy at wrapped map border (0, 10) with enemy at (79, 10)
			bool borderDetected = MovementRules.HasAdjacentForeignUnit(0, 10, myPlayerId, (x, y) =>
			{
				return (x == 79 && y == 10) ? enemyPlayerId : -1;
			});
			Assert.True(borderDetected);
		}
	}
}
