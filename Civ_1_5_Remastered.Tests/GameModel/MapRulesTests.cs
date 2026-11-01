using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
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
		public void IsOnMap(int horizontal, int vertical, bool expected) => Assert.Equal(expected, MapRules.IsOnMap(horizontal, vertical));

		[Theory]
		[InlineData(0, 0, 0, 0, 0)]
		[InlineData(0, 0, 3, 0, 3)]
		[InlineData(0, 0, 3, 2, 4)]
		[InlineData(0, 0, 2, 3, 4)]
		[InlineData(0, 10, 79, 10, 1)]
		[InlineData(5, 0, 75, 0, 10)]
		[InlineData(0, 0, 40, 0, 40)]
		[InlineData(0, 0, 41, 0, 39)]
		public void DistanceWrapsHorizontally(int firstHorizontal, int firstVertical, int secondHorizontal, int secondVertical, int expected)
		{
			Assert.Equal(expected, MapRules.Distance(firstHorizontal, firstVertical, secondHorizontal, secondVertical));
			Assert.Equal(expected, MapRules.Distance(secondHorizontal, secondVertical, firstHorizontal, firstVertical));
		}

		[Theory]
		[InlineData(4, 2, 5)]
		[InlineData(-4, 2, 5)]
		[InlineData(2, -4, 5)]
		[InlineData(3, 3, 4)]
		[InlineData(0, 7, 7)]
		public void LongerPlusHalfShorter(int deltaHorizontal, int deltaVertical, int expected) => Assert.Equal(expected, MapRules.LongerPlusHalfShorter(deltaHorizontal, deltaVertical));

		[Theory]
		[InlineData(4, 2, 10)]
		[InlineData(2, 4, 10)]
		[InlineData(79, 0, 2)]
		[InlineData(-3, -3, 9)]
		public void DoubledDistance(int deltaHorizontal, int deltaVertical, int expected) => Assert.Equal(expected, MapRules.DoubledDistance(deltaHorizontal, deltaVertical));

		[Fact]
		public void NoSpecialResourceOrHutOnPolarRows()
		{
			for (int horizontal = 0; horizontal < MapRules.Width; horizontal++)
			{
				foreach (int vertical in new[] { 0, 1, 48, 49 })
				{
					for (int seed = 0; seed < 16; seed++)
					{
						Assert.False(MapRules.HasSpecialResource(horizontal, vertical, seed));
						Assert.False(MapRules.IsHutPlace(horizontal, vertical, seed));
					}
				}
			}
		}

		[Fact]
		public void EveryInnerFourByFourBlockHasExactlyOneSpecialResource()
		{
			for (int seed = 0; seed < 16; seed++)
			{
				for (int block = 0; block < 20; block++)
				{
					for (int by = 1; by < 12; by++)
					{
						int found = 0;

						for (int horizontal = block * 4; horizontal < (block * 4) + 4; horizontal++)
						{
							for (int vertical = by * 4; vertical < (by * 4) + 4; vertical++)
							{
								found += MapRules.HasSpecialResource(horizontal, vertical, seed) ? 1 : 0;
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
		public void SpecialResourceKnownPlaces(int horizontal, int vertical, int seed, bool expected)
		{
			Assert.Equal(expected, MapRules.HasSpecialResource(horizontal, vertical, seed + 0x10 - 11));
		}

		[Fact]
		public void HutPlaceFollowsBlockHash()
		{
			Assert.True(MapRules.IsHutPlace(4, 4, 0));
			Assert.False(MapRules.IsHutPlace(5, 4, 0));
		}
	}
}
