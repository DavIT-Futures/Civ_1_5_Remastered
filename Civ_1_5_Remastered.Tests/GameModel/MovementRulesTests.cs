using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class MovementRulesTests
	{
		[Theory]
		[InlineData(-1, 79)]
		[InlineData(0, 0)]
		[InlineData(79, 79)]
		[InlineData(80, 0)]
		[InlineData(85, 5)]
		public void WrapHorizontal(int horizontal, int expected) => Assert.Equal(expected, MovementRules.WrapHorizontal(horizontal));

		[Theory]
		[InlineData(false, false, 0)]
		[InlineData(true, false, 1)]
		[InlineData(false, true, 1)]
		[InlineData(true, true, 2)]
		public void LighthouseAndMagellanAddUp(bool lighthouse, bool magellan, int moves) => Assert.Equal(moves, MovementRules.SeaWonderMoves(lighthouse, magellan));

		[Fact]
		public void AdjacentForeignUnitIgnoresOwnAndEmptyTiles()
		{
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (horizontal, vertical) => -1));
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (horizontal, vertical) => 0xffff));
			Assert.False(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (horizontal, vertical) => 2));
			Assert.True(MovementRules.HasAdjacentForeignUnit(10, 10, 2, (horizontal, vertical) => horizontal == 11 && vertical == 9 ? 3 : -1));
		}

		[Fact]
		public void AdjacentForeignUnitWrapsAcrossDateLine()
		{
			List<(int, int)> asked = new List<(int, int)>();
			MovementRules.HasAdjacentForeignUnit(0, 10, 1, (horizontal, vertical) =>
			{
				asked.Add((horizontal, vertical));
				return -1;
			});

			Assert.Equal(8, asked.Count);
			Assert.Contains((79, 10), asked);
			Assert.All(asked, place => Assert.InRange(place.Item1, 0, 79));
		}
	}
}
