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
		/// The expressions of MapService.ContainsSpecialResource and ContainsHut before the refactoring (commit 4df3d73)
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

		/// <summary>
		/// The expressions of GameHelpers.ObtainDistance, MergeCoordinates and ObtainDistanceFromOffsets before the refactoring
		/// </summary>
		[Fact]
		public void Distances_MatchTheTranslatedOriginal()
		{
			static int Merge(int horizLoc, int vertLoc)
			{
				horizLoc = Math.Abs(horizLoc);
				vertLoc = Math.Abs(vertLoc);

				return horizLoc > vertLoc ? (short)((vertLoc / 2) + horizLoc) : (short)((horizLoc / 2) + vertLoc);
			}

			for (int horizLoc1 = -2; horizLoc1 < MapRules.Width + 2; horizLoc1 += 3)
			{
				for (int horizLoc2 = 0; horizLoc2 < MapRules.Width; horizLoc2++)
				{
					for (int vertStep = -55; vertStep <= 55; vertStep += 5)
					{
						int horizStep = Math.Abs(horizLoc1 - horizLoc2);
						if (horizStep > 40)
							horizStep = 0x50 - horizStep;

						Assert.Equal(Merge(horizStep, Math.Abs(vertStep)), MapRules.Distance(horizLoc1, 10 + vertStep, horizLoc2, 10));

						int offsetHoriz = Math.Abs(horizLoc1 - horizLoc2);
						if (offsetHoriz > 40)
							offsetHoriz = 80 - offsetHoriz;
						int offsetVert = Math.Abs(vertStep);
						int doubled = offsetHoriz > offsetVert ? (short)offsetHoriz * 2 + offsetVert : (short)offsetVert * 2 + offsetHoriz;

						Assert.Equal(doubled, MapRules.DoubledDistance(horizLoc1 - horizLoc2, vertStep));
					}
				}
			}
		}

		[Theory]
		[InlineData(-1, 0)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		[InlineData(4, 10)]
		[InlineData(12, 78)]
		public void CityPopulation_IsTheSumOneToSize(int citySize, int expected)
		{
			Assert.Equal(expected, PopulationRules.CityPopulation(citySize));
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
