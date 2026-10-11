using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CityConquestRulesTests
	{
		[Fact]
		public void ComputeCapturedBuildingFlags_MilitaryConquest_DestroysBuildingsAccordingToDice()
		{
			ushort initialBuildings = 0xffff; // all buildings constructed

			// Dice = 0: destructionMask = 0xaaaa >> 0 = 0xaaaa
			// StructureMarkers0 &= 0xfbb6 & 0xaaaa = 0xaaa2
			ushort resultDice0 = CityConquestRules.ComputeCapturedBuildingFlags(initialBuildings, 0, isPeacefulCapture: false);
			Assert.Equal((ushort)(0xfbb6 & 0xaaaa), resultDice0);

			// Palace bit 0 should always be cleared (0x1 & 0xfbb6 == 0)
			Assert.Equal(0, resultDice0 & 0x1);

			// Dice = 1: destructionMask = 0xaaaa >> 1 = 0x5555
			ushort resultDice1 = CityConquestRules.ComputeCapturedBuildingFlags(initialBuildings, 1, isPeacefulCapture: false);
			Assert.Equal((ushort)(0xfbb6 & 0x5555), resultDice1);
			Assert.Equal(0, resultDice1 & 0x1);
		}

		[Fact]
		public void ComputeCapturedBuildingFlags_PeacefulCapture_PreservesNonPalaceBuildings()
		{
			ushort initialBuildings = 0xffff;

			ushort result = CityConquestRules.ComputeCapturedBuildingFlags(initialBuildings, 0, isPeacefulCapture: true);
			// Only base mask 0xfbb6 applied, no random dice destruction
			Assert.Equal((ushort)0xfbb6, result);
			Assert.Equal(0, result & 0x1); // palace destroyed
		}

		[Fact]
		public void ComputeCapturedCitySize_MilitaryCapture_DecreasesSizeByOne()
		{
			Assert.Equal(4, CityConquestRules.ComputeCapturedCitySize(5, isPeacefulCapture: false));
			Assert.Equal(0, CityConquestRules.ComputeCapturedCitySize(1, isPeacefulCapture: false));
			Assert.Equal(0, CityConquestRules.ComputeCapturedCitySize(0, isPeacefulCapture: false));
		}

		[Fact]
		public void ComputeCapturedCitySize_PeacefulCapture_PreservesSizeOne()
		{
			Assert.Equal(1, CityConquestRules.ComputeCapturedCitySize(1, isPeacefulCapture: true));
			Assert.Equal(3, CityConquestRules.ComputeCapturedCitySize(4, isPeacefulCapture: true));
		}

		[Fact]
		public void IsCityDestroyed_ZeroOrNegativeSize_ReturnsTrue()
		{
			Assert.True(CityConquestRules.IsCityDestroyed(0));
			Assert.True(CityConquestRules.IsCityDestroyed(-1));
			Assert.False(CityConquestRules.IsCityDestroyed(1));
			Assert.False(CityConquestRules.IsCityDestroyed(5));
		}

		[Fact]
		public void ComputeGoldTransfer_CalculatesPlunderCorrectly()
		{
			var (plundered, victimRemaining, captorNew) = CityConquestRules.ComputeGoldTransfer(
				plunderAmount: 150,
				victimGold: 100,
				captorGold: 200);

			Assert.Equal(100, plundered); // limited to victimGold
			Assert.Equal(0, victimRemaining);
			Assert.Equal(300, captorNew);
		}

		[Theory]
		[InlineData(100, 250, 100)] // less than victim gold
		[InlineData(300, 150, 150)] // more than victim gold -> clamped
		[InlineData(100, 0, 0)]     // victim bankrupt -> 0
		[InlineData(-50, 100, 0)]   // negative plunder -> 0
		public void ClampPlunder_CorrectlyLimitsPlunder(int plunder, int victimGold, int expected)
		{
			Assert.Equal(expected, CityConquestRules.ClampPlunder(plunder, victimGold));
		}

		[Fact]
		public void ShouldEliminateGarrison_CorrectlyDisbandsUnits()
		{
			int homeCity = 5;
			int otherCity = 2;
			int cityX = 10, cityY = 15;

			// 1. Unit belonging to another city -> never eliminated by this city's capture
			Assert.False(CityConquestRules.ShouldEliminateGarrison(otherCity, homeCity, 20, 25, cityX, cityY, cityTrueExtent: 3));
			Assert.False(CityConquestRules.ShouldEliminateGarrison(otherCity, homeCity, cityX, cityY, cityX, cityY, cityTrueExtent: 0));

			// 2. Unit belonging to home city, stationed outside -> eliminated regardless of city extent
			Assert.True(CityConquestRules.ShouldEliminateGarrison(homeCity, homeCity, 11, 15, cityX, cityY, cityTrueExtent: 3));
			Assert.True(CityConquestRules.ShouldEliminateGarrison(homeCity, homeCity, 10, 16, cityX, cityY, cityTrueExtent: 3));

			// 3. Unit belonging to home city, stationed inside, city survives -> not eliminated
			Assert.False(CityConquestRules.ShouldEliminateGarrison(homeCity, homeCity, cityX, cityY, cityX, cityY, cityTrueExtent: 2));

			// 4. Unit belonging to home city, stationed inside, city wiped out (size 0) -> eliminated
			Assert.True(CityConquestRules.ShouldEliminateGarrison(homeCity, homeCity, cityX, cityY, cityX, cityY, cityTrueExtent: 0));
		}

		[Theory]
		[InlineData(true, false, 5, 10, 20, true)]   // Capital, military capture, 5 towns, victim standing higher -> SCHISM
		[InlineData(true, true, 5, 10, 20, false)]   // Peaceful capture (diplomat bribe) -> NO schism
		[InlineData(false, false, 5, 10, 20, false)]  // Non-capital (no palace) -> NO schism
		[InlineData(true, false, 4, 10, 20, false)]  // <= 4 towns -> NO schism
		[InlineData(true, false, 5, 20, 20, false)]  // Human standing >= victim standing -> NO schism
		[InlineData(true, false, 5, 25, 20, false)]  // Human standing > victim standing -> NO schism
		public void ShouldTriggerSchism_EvaluatesCivilWarEligibility(
			bool hasPalace, bool isPeacefulCapture, int victimTownCount, int humanStanding, int victimStanding, bool expected)
		{
			bool result = CityConquestRules.ShouldTriggerSchism(
				hasPalace, isPeacefulCapture, victimTownCount, humanStanding, victimStanding);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(1, 0, true)]   // Weak unit (power 1), flag 0 -> razed
		[InlineData(2, 0, true)]   // Weak unit (power 2), flag 0 -> razed
		[InlineData(3, 0, false)]  // Strong unit (power 3), flag 0 -> not razed
		[InlineData(1, 1, false)]  // Weak unit, flag != 0 -> not razed
		public void ShouldBarbarianRaidRazeCity_EvaluatesRazingCondition(int assaultPower, ushort globalFlag, bool expected)
		{
			bool result = CityConquestRules.ShouldBarbarianRaidRazeCity(assaultPower, globalFlag);
			Assert.Equal(expected, result);
		}
	}
}
