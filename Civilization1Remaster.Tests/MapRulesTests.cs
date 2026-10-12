using System;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class MapRulesTests
	{
		[Theory]
		[InlineData(0, 0, true)]
		[InlineData(79, 49, true)]
		[InlineData(-1, 0, false)]
		[InlineData(80, 0, false)]
		[InlineData(0, -1, false)]
		[InlineData(0, 50, false)]
		public void IsOnMap_ChecksTheMapSize(int horizLoc, int vertLoc, bool expected)
		{
			Assert.Equal(expected, MapRules.IsOnMap(horizLoc, vertLoc));
		}

		/// <summary>
		/// The expressions of MapManagement.ContainsSpecialResource and ContainsHut before the refactoring (commit 4df3d73)
		/// </summary>
		[Fact]
		public void SpecialResourceAndHut_MatchTheTranslatedOriginal()
		{
			for (int worldSeed = 0; worldSeed < 0x8000; worldSeed += 97)
			{
				for (int horizLoc = 0; horizLoc < MapRules.Width; horizLoc++)
				{
					for (int vertLoc = 0; vertLoc < MapRules.Height; vertLoc++)
					{
						bool special = !(vertLoc <= 0x1 || vertLoc >= 48 ||
							(((horizLoc & 3) * 4) + (vertLoc & 3)) != ((((horizLoc / 4) * 13) + ((vertLoc / 4) * 11) + worldSeed) & 0xf));

						bool hutPlace = !(vertLoc <= 1 || vertLoc >= 48) &&
							((horizLoc & 3) * 4 + (vertLoc & 3)) == ((((horizLoc / 4) * 13) + ((vertLoc / 4) * 11) + worldSeed + 8) & 0x1f);

						Assert.Equal(special, MapRules.HasSpecialResource(horizLoc, vertLoc, worldSeed));
						Assert.Equal(hutPlace, MapRules.IsHutPlace(horizLoc, vertLoc, worldSeed));
					}
				}
			}
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1234)]
		[InlineData(0x7fff)]
		public void SpecialResource_OneTileInEveryFullBlock_NoneAtThePoles(int worldSeed)
		{
			// Blocks of rows 4 - 43 lie fully between the poles
			for (int blockHoriz = 0; blockHoriz < MapRules.Width / 4; blockHoriz++)
			{
				for (int blockVert = 1; blockVert <= 10; blockVert++)
				{
					int tally = 0;

					for (int idx = 0; idx < 16; idx++)
					{
						if (MapRules.HasSpecialResource(blockHoriz * 4 + idx / 4, blockVert * 4 + idx % 4, worldSeed))
							tally++;
					}

					Assert.Equal(1, tally);
				}
			}

			for (int horizLoc = 0; horizLoc < MapRules.Width; horizLoc++)
			{
				foreach (int vertLoc in new[] { 0, 1, 48, 49 })
				{
					Assert.False(MapRules.HasSpecialResource(horizLoc, vertLoc, worldSeed));
					Assert.False(MapRules.IsHutPlace(horizLoc, vertLoc, worldSeed));
				}
			}
		}
	}
}
