using Civilization1Remaster.GameModel.Rules;
using static Civilization1Remaster.GameModel.Rules.CitizenSpecialistRules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class CityGrowthAndHappinessTests
	{
		[Theory]
		[InlineData(1, 10, 20)]
		[InlineData(3, 10, 40)]
		[InlineData(3, 15, 60)]
		public void FoodBoxCapacity(int size, int scale, int expected) => Assert.Equal(expected, CityGrowthAndProductionRules.ComputeFoodBoxCapacity(size, scale));

		[Theory]
		[InlineData(4, true, 25)]
		[InlineData(4, false, 0)]
		public void RetainedFood(int size, bool granary, int expected) => Assert.Equal(expected, CityGrowthAndProductionRules.ComputeRetainedFoodAfterGrowth(size, granary));

		[Theory]
		[InlineData(40, 10, false, false, 60)]
		[InlineData(40, 0, false, false, 160)]
		[InlineData(100, 50, false, true, 200)]
		[InlineData(40, 10, true, false, 105)]
		[InlineData(40, 0, true, false, 320)]
		[InlineData(10, 10, false, false, 0)]
		[InlineData(10, 20, true, false, 0)]
		public void RushBuyCost(int total, int box, bool unit, bool wonder, int expected) =>
			Assert.Equal(expected, CityGrowthAndProductionRules.ComputeRushBuyCost(total, box, unit, wonder));

		[Theory]
		[InlineData(1, 2, true)]
		[InlineData(2, 2, false)]
		[InlineData(3, 0, false)]
		public void CivilDisorder(int happy, int unhappy, bool expected) => Assert.Equal(expected, CityHappinessRules.EvaluatesCivilDisorder(happy, unhappy));

		[Theory]
		[InlineData(4, 2, 0, false, true)]
		[InlineData(5, 2, 0, false, false)]
		[InlineData(5, 3, 0, false, true)]
		[InlineData(2, 2, 0, false, false)]
		[InlineData(4, 4, 1, false, false)]
		[InlineData(4, 4, 0, true, false)]
		public void Celebration(int size, int happy, int unhappy, bool anarchy, bool expected) =>
			Assert.Equal(expected, CityHappinessRules.EvaluatesCelebration(size, happy, unhappy, anarchy));

		[Theory]
		[InlineData(6, 1, 4, 2, 0, 3, 2)]
		[InlineData(4, 0, 1, 3, 2, 2, 0)]
		[InlineData(2, 5, 1, 0, 0, 0, 0)]
		[InlineData(5, 0, 9, 0, 0, 0, 5)]
		[InlineData(5, 0, -3, 1, 1, 4, 0)]
		public void CitizenMoods(int size, int specialists, int baseUnhappy, int pacified, int happy, int content, int unhappy) =>
			Assert.Equal((happy, content, unhappy), CityHappinessRules.CalculateCitizenMoods(size, specialists, baseUnhappy, pacified));

		[Theory]
		[InlineData(-3, 0)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		[InlineData(4, 10)]
		[InlineData(10, 55)]
		public void CityPopulation(int size, int expected) => Assert.Equal(expected, PopulationRules.CityPopulation(size));

		[Theory]
		[InlineData(27, false, 2, 7)]
		[InlineData(27, true, 2, 2)]
		[InlineData(5, false, 0, 5)]
		public void Tenths(short value, bool classic, short whole, short tenth)
		{
			Assert.Equal(whole, TenthsRules.Whole(value));
			Assert.Equal(tenth, TenthsRules.Tenth(value, classic));
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
							Assert.Equal((SpecialistType)3, GetSpecialistType(markers, other));
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

	public class CityConquestAndDisasterTests
	{
		[Fact]
		public void PeacefulCaptureKeepsAllowedBuildings()
		{
			Assert.Equal(0xfbb6, CityConquestRules.ComputeCapturedBuildingFlags(0xffff, 0, true));
			Assert.Equal(0xaaa2, CityConquestRules.ComputeCapturedBuildingFlags(0xffff, 0, false));
			Assert.Equal(0x5114, CityConquestRules.ComputeCapturedBuildingFlags(0xffff, 1, false));
		}

		[Theory]
		[InlineData(1, true, 1)]
		[InlineData(1, false, 0)]
		[InlineData(5, true, 4)]
		[InlineData(0, false, 0)]
		public void CapturedCitySize(int size, bool peaceful, int expected) => Assert.Equal(expected, CityConquestRules.ComputeCapturedCitySize(size, peaceful));

		[Fact]
		public void GoldTransferIsClampedToVictimGold()
		{
			Assert.Equal((50, 0, 150), CityConquestRules.ComputeGoldTransfer(80, 50, 100));
			Assert.Equal((30, 20, 130), CityConquestRules.ComputeGoldTransfer(30, 50, 100));
			Assert.Equal((0, 50, 100), CityConquestRules.ComputeGoldTransfer(-5, 50, 100));
			Assert.True(CityConquestRules.IsCityDestroyed(0));
			Assert.False(CityConquestRules.IsCityDestroyed(1));
		}

		[Theory]
		[InlineData(1, 2, 5, 5, 5, 5, 3, false)]
		[InlineData(2, 2, 5, 5, 5, 5, 3, false)]
		[InlineData(2, 2, 6, 5, 5, 5, 3, true)]
		[InlineData(2, 2, 5, 5, 5, 5, 0, true)]
		public void GarrisonElimination(int home, int town, int ux, int uy, int cx, int cy, int extent, bool expected) =>
			Assert.Equal(expected, CityConquestRules.ShouldEliminateGarrison(home, town, ux, uy, cx, cy, extent));

		[Theory]
		[InlineData(true, false, 5, 1, 2, true)]
		[InlineData(false, false, 5, 1, 2, false)]
		[InlineData(true, true, 5, 1, 2, false)]
		[InlineData(true, false, 4, 1, 2, false)]
		[InlineData(true, false, 5, 2, 2, false)]
		public void Schism(bool palace, bool peaceful, int towns, int human, int victim, bool expected) =>
			Assert.Equal(expected, CityConquestRules.ShouldTriggerSchism(palace, peaceful, towns, human, victim));

		[Theory]
		[InlineData(2, 0, true)]
		[InlineData(3, 0, false)]
		[InlineData(1, 1, false)]
		public void BarbarianRaze(int power, ushort flag, bool expected) => Assert.Equal(expected, CityConquestRules.ShouldBarbarianRaidRazeCity(power, flag));

		[Fact]
		public void DisasterConditions()
		{
			Assert.True(CityDisasterRules.CanEarthquakeStrike(0x10, 0x2));
			Assert.False(CityDisasterRules.CanEarthquakeStrike(0x10, 0x1));
			Assert.False(CityDisasterRules.CanEarthquakeStrike(0, 0xffff));

			Assert.True(CityDisasterRules.CanPlagueStrike(false, 0, true));
			Assert.False(CityDisasterRules.CanPlagueStrike(true, 0, true));
			Assert.False(CityDisasterRules.CanPlagueStrike(false, 0x100, true));
			Assert.False(CityDisasterRules.CanPlagueStrike(false, 0, false));

			Assert.True(CityDisasterRules.CanFloodStrike(0x800, 0, true, 2));
			Assert.False(CityDisasterRules.CanFloodStrike(0x800, 0x80, true, 2));
			Assert.False(CityDisasterRules.CanFloodStrike(0x800, 0, true, 1));

			Assert.True(CityDisasterRules.CanFamineStrike(0x20, 0, true, 2));
			Assert.False(CityDisasterRules.CanFamineStrike(0x20, 0x8, true, 2));
			Assert.False(CityDisasterRules.CanFamineStrike(0x20, 0, false, 2));

			Assert.True(CityDisasterRules.CanFireStrike(0, true, 1));
			Assert.False(CityDisasterRules.CanFireStrike(0x4, true, 1));
			Assert.False(CityDisasterRules.CanFireStrike(0, true, 0));

			Assert.True(CityDisasterRules.CanPiratesRaid(0x400, 0));
			Assert.False(CityDisasterRules.CanPiratesRaid(0x400, 0x2));
			Assert.False(CityDisasterRules.CanPiratesRaid(0, 0));
		}

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
		[InlineData(3, 6, false, 25)]
		[InlineData(3, 6, true, 25)]
		[InlineData(10, 2, false, 100)]
		[InlineData(10, 2, true, 250)]
		[InlineData(3, 0, false, 0)]
		[InlineData(-4, 2, false, 0)]
		public void ApprovalRating(short approval, short size, bool classic, short expected) => Assert.Equal(expected, CensusRules.ApprovalRating(approval, size, classic));

		[Theory]
		[InlineData(5, 2, false, 25)]
		[InlineData(5, 2, true, 25)]
		[InlineData(5, 0, false, 0)]
		[InlineData(-5, 2, false, 0)]
		[InlineData(-5, 2, true, -25)]
		public void Productivity(short output, short size, bool classic, short expected) => Assert.Equal(expected, CensusRules.Productivity(output, size, classic));

		[Theory]
		[InlineData(2, 3, false, 20)]
		[InlineData(2, 3, true, 20)]
		[InlineData(0, 0, false, 0)]
		public void DiseaseRate(short size, short health, bool classic, short expected) => Assert.Equal(expected, CensusRules.DiseaseRate(size, health, classic));

		[Theory]
		[InlineData(3, 6, false, 40)]
		[InlineData(3, 6, true, 40)]
		[InlineData(3, 0, false, 20)]
		public void FamilySize(short unrest, short size, bool classic, short expected) => Assert.Equal(expected, CensusRules.FamilySize(unrest, size, classic));

		[Fact]
		public void ClassicDividesByZeroLikeTheOriginal()
		{
			Assert.Throws<DivideByZeroException>(() => CensusRules.ApprovalRating(3, 0, true));
		}
	}
}
