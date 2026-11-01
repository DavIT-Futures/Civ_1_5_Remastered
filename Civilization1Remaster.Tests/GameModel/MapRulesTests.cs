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

						for (int x = bx * 4; x < (bx * 4) + 4; x++)
						{
							for (int y = by * 4; y < (by * 4) + 4; y++)
							{
								found += MapRules.HasSpecialResource(x, y, seed) ? 1 : 0;
							}
						}

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
			Assert.Equal(expected, MapRules.HasSpecialResource(x, y, seed + 0x10 - 11));
		}

		[Fact]
		public void HutPlaceFollowsBlockHash()
		{
			Assert.True(MapRules.IsHutPlace(4, 4, 0));
			Assert.False(MapRules.IsHutPlace(5, 4, 0));
		}
	}
}
