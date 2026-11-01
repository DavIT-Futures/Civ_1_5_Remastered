using Civilization1Remaster.GameModel.Rules;
using static Civilization1Remaster.GameModel.Rules.CitizenSpecialistRules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class CitizenSpecialistRulesTests
	{
		[Fact]
		public void RadiusOffsetsAreTwentyOneDistinctTiles()
		{
			Assert.Equal(21, CityRadiusOffsets.Length);
			Assert.Equal(21, CityRadiusOffsets.Distinct().Count());
			Assert.All(CityRadiusOffsets, offset => Assert.True(Math.Abs(offset.X) <= 2 && Math.Abs(offset.Y) <= 2 && !(Math.Abs(offset.X) == 2 && Math.Abs(offset.Y) == 2)));
		}

		[Theory]
		[InlineData(0, -1, 0)]
		[InlineData(0, 0, 20)]
		[InlineData(-2, -1, 19)]
		[InlineData(2, 2, -1)]
		public void FindCityTileIndex(int deltaHorizontal, int deltaVertical, int expected) => Assert.Equal(expected, CitizenSpecialistRules.FindCityTileIndex(deltaHorizontal, deltaVertical));

		[Fact]
		public void SetThenGetEverySlot()
		{
			for (int slot = 0; slot < 8; slot++)
			{
				foreach (SpecialistType type in Enum.GetValues<SpecialistType>())
				{
					ushort markers = SetSpecialistType(0xffff, slot, type);
					Assert.Equal(type, GetSpecialistType(markers, slot));

					for (int other = 0; other < 8; other++)
					{
						if (other != slot)
						{
							Assert.Equal((SpecialistType)3, GetSpecialistType(markers, other));
						}
					}
				}
			}
		}

		[Fact]
		public void SlotBitsAreTwoPerSlot()
		{
			Assert.Equal(0x0080, SetSpecialistType(0, 3, SpecialistType.Scientist));
			Assert.Equal(0x4000, SetSpecialistType(0, 7, SpecialistType.Taxman));
		}

		[Fact]
		public void SlotsBeyondEightAreIgnored()
		{
			Assert.Equal(SpecialistType.Taxman, GetSpecialistType(0, 8));
			Assert.Equal(0x1234, SetSpecialistType(0x1234, 8, SpecialistType.Scientist));
		}

		[Fact]
		public void CountSpecialists()
		{
			ushort markers = 0;
			markers = SetSpecialistType(markers, 0, SpecialistType.Scientist);
			markers = SetSpecialistType(markers, 1, SpecialistType.Taxman);
			markers = SetSpecialistType(markers, 2, SpecialistType.Scientist);

			Assert.Equal(2, CitizenSpecialistRules.CountSpecialists(markers, SpecialistType.Scientist));
			Assert.Equal(1, CitizenSpecialistRules.CountSpecialists(markers, SpecialistType.Taxman));
			Assert.Equal(5, CitizenSpecialistRules.CountSpecialists(markers, SpecialistType.Entertainer));
		}
	}
}
