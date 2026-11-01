using Civilization1Remaster.GameModel.Rules;
using static Civilization1Remaster.GameModel.Rules.CitizenSpecialistRules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class PopulationAndTenthsTests
	{
		[Theory]
		[InlineData(-3, 0)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		[InlineData(4, 10)]
		[InlineData(10, 55)]
		public void CityPopulation(int size, int expected) => Assert.Equal(expected, PopulationRules.CityPopulation(size));

		[Theory]
		[InlineData(27, 2, 7)]
		[InlineData(5, 0, 5)]
		[InlineData(40, 4, 0)]
		public void Tenths(short value, short whole, short tenth)
		{
			Assert.Equal(whole, TenthsRules.Whole(value));
			Assert.Equal(tenth, TenthsRules.Tenth(value));
		}
	}

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
		public void FindCityTileIndex(int dx, int dy, int expected) => Assert.Equal(expected, CitizenSpecialistRules.FindCityTileIndex(dx, dy));

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

	public class CityDisasterRulesTests
	{
		[Theory]
		[InlineData(-1, 0, 0)]
		[InlineData(9, 2, 3)]
		[InlineData(12, 3, 4)]
		public void DisasterLosses(int population, int plague, int famine)
		{
			Assert.Equal(plague, CityDisasterRules.ComputePlaguePopulationLoss(population));
			Assert.Equal(famine, CityDisasterRules.ComputeFaminePopulationLoss(population));
		}
	}

	public class CensusRulesTests
	{
		[Theory]
		[InlineData(3, 6, 25)]
		[InlineData(10, 2, 100)]
		[InlineData(3, 0, 0)]
		[InlineData(-4, 2, 0)]
		[InlineData(1000, 10, 100)]
		public void ApprovalRating(short approval, short size, short expected) => Assert.Equal(expected, CensusRules.ApprovalRating(approval, size));

		[Theory]
		[InlineData(5, 2, 25)]
		[InlineData(5, 0, 0)]
		[InlineData(-5, 2, 0)]
		[InlineData(4000, 100, 400)]
		public void Productivity(short output, short size, short expected) => Assert.Equal(expected, CensusRules.Productivity(output, size));

		[Theory]
		[InlineData(2, 3, 20)]
		[InlineData(0, 0, 0)]
		[InlineData(1000, 0, 50)]
		public void DiseaseRate(short size, short health, short expected) => Assert.Equal(expected, CensusRules.DiseaseRate(size, health));

		[Theory]
		[InlineData(3, 6, 40)]
		[InlineData(3, 0, 20)]
		[InlineData(1000, 100, 420)]
		public void FamilySize(short unrest, short size, short expected) => Assert.Equal(expected, CensusRules.FamilySize(unrest, size));
	}
}
