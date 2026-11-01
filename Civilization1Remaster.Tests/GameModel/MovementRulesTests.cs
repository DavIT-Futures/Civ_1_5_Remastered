using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class MovementRulesTests
	{
		[Theory]
		[InlineData(-1, 79)]
		[InlineData(0, 0)]
		[InlineData(79, 79)]
		[InlineData(80, 0)]
		[InlineData(85, 5)]
		public void WrapX(int x, int expected) => Assert.Equal(expected, MovementRules.WrapX(x));

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
			MovementRules.HasAdjacentForeignUnit(0, 10, 1, (x, y) =>
			{
				asked.Add((x, y));
				return -1;
			});

			Assert.Equal(8, asked.Count);
			Assert.Contains((79, 10), asked);
			Assert.All(asked, place => Assert.InRange(place.Item1, 0, 79));
		}
	}
}
