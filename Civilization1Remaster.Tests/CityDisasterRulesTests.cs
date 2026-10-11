using Xunit;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Tests
{
	public class CityDisasterRulesTests
	{
		[Fact]
		public void CanEarthquakeStrike_RequiresMountainAndDestructibleBuildings()
		{
			// 0x10 is mountain, 0x2 is Barracks
			Assert.True(CityDisasterRules.CanEarthquakeStrike(0x10, 0x2));

			// No mountain
			Assert.False(CityDisasterRules.CanEarthquakeStrike(0x0, 0x2));

			// Only palace (0x1) - earthquake cannot destroy palace
			Assert.False(CityDisasterRules.CanEarthquakeStrike(0x10, 0x1));
		}

		[Fact]
		public void CanPlagueStrike_BlockedByMedicineOrAqueduct()
		{
			// Knows medicine -> immune
			Assert.False(CityDisasterRules.CanPlagueStrike(true, 0x0, true));

			// Has aqueduct (0x100) -> immune
			Assert.False(CityDisasterRules.CanPlagueStrike(false, 0x100, true));

			// No medicine, no aqueduct, knows construction -> can strike
			Assert.True(CityDisasterRules.CanPlagueStrike(false, 0x0, true));
		}

		[Fact]
		public void ComputePlaguePopulationLoss_QuartersPopulation()
		{
			Assert.Equal(2, CityDisasterRules.ComputePlaguePopulationLoss(8));
			Assert.Equal(1, CityDisasterRules.ComputePlaguePopulationLoss(4));
			Assert.Equal(0, CityDisasterRules.ComputePlaguePopulationLoss(3));
		}

		[Fact]
		public void CanFloodStrike_RequiresRiverAndNoWalls()
		{
			// 0x800 is river, 0x80 is walls
			Assert.True(CityDisasterRules.CanFloodStrike(0x800, 0x0, true, 4));

			// Protected by City Walls
			Assert.False(CityDisasterRules.CanFloodStrike(0x800, 0x80, true, 4));

			// Population too small (<= 1)
			Assert.False(CityDisasterRules.CanFloodStrike(0x800, 0x0, true, 1));
		}

		[Fact]
		public void CanFamineStrike_RequiresSwampAndNoGranary()
		{
			// 0x20 is swamp/desert, 0x8 is granary
			Assert.True(CityDisasterRules.CanFamineStrike(0x20, 0x0, true, 4));

			// Protected by Granary
			Assert.False(CityDisasterRules.CanFamineStrike(0x20, 0x8, true, 4));
		}

		[Fact]
		public void ComputeFaminePopulationLoss_ThirdsPopulation()
		{
			Assert.Equal(3, CityDisasterRules.ComputeFaminePopulationLoss(9));
			Assert.Equal(2, CityDisasterRules.ComputeFaminePopulationLoss(6));
			Assert.Equal(1, CityDisasterRules.ComputeFaminePopulationLoss(4));
		}

		[Fact]
		public void CanPiratesRaid_RequiresOceanAndNoBarracks()
		{
			// 0x400 is ocean, 0x2 is barracks
			Assert.True(CityDisasterRules.CanPiratesRaid(0x400, 0x0));

			// Protected by Barracks
			Assert.False(CityDisasterRules.CanPiratesRaid(0x400, 0x2));
		}
	}
}
