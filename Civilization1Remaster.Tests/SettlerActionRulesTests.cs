using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class SettlerActionRulesTests
	{
		[Fact]
		public void GetRoadTurns_MountainsTakeLongest()
		{
			Assert.Equal(2, SettlerActionRules.GetRoadTurns(1));  // Plains
			Assert.Equal(4, SettlerActionRules.GetRoadTurns(4));  // Hills
			Assert.Equal(10, SettlerActionRules.GetRoadTurns(5)); // Mountains
		}

		[Fact]
		public void CanIrrigate_RequiresWaterAndNonOcean()
		{
			// Plains with water, not yet irrigated -> true
			Assert.True(SettlerActionRules.CanIrrigate(terrainType: 1, hasAdjacentWaterOrIrrigation: true, alreadyIrrigated: false));

			// Plains without water -> false
			Assert.False(SettlerActionRules.CanIrrigate(terrainType: 1, hasAdjacentWaterOrIrrigation: false, alreadyIrrigated: false));

			// Already irrigated -> false
			Assert.False(SettlerActionRules.CanIrrigate(terrainType: 1, hasAdjacentWaterOrIrrigation: true, alreadyIrrigated: true));

			// Mountains (type 5) -> cannot irrigate
			Assert.False(SettlerActionRules.CanIrrigate(terrainType: 5, hasAdjacentWaterOrIrrigation: true, alreadyIrrigated: false));
		}

		[Fact]
		public void CanBuildMine_AllowedOnHillsAndMountains()
		{
			Assert.True(SettlerActionRules.CanBuildMine(terrainType: 4, alreadyMined: false));  // Hills
			Assert.True(SettlerActionRules.CanBuildMine(terrainType: 5, alreadyMined: false));  // Mountains
			Assert.False(SettlerActionRules.CanBuildMine(terrainType: 2, alreadyMined: false)); // Grassland (cannot mine)
			Assert.False(SettlerActionRules.CanBuildMine(terrainType: 4, alreadyMined: true));  // Already mined
		}

		[Fact]
		public void CanBuildRoad_RoadAndRailroadUpgrades()
		{
			// No road yet -> can build road
			Assert.True(SettlerActionRules.CanBuildRoad(hasRoad: false, hasRailroad: false, hasRailroadTech: false));

			// Has road, no railroad, but no tech -> cannot upgrade
			Assert.False(SettlerActionRules.CanBuildRoad(hasRoad: true, hasRailroad: false, hasRailroadTech: false));

			// Has road, has railroad tech -> can upgrade to railroad
			Assert.True(SettlerActionRules.CanBuildRoad(hasRoad: true, hasRailroad: false, hasRailroadTech: true));

			// Already has railroad -> nothing more to build
			Assert.False(SettlerActionRules.CanBuildRoad(hasRoad: true, hasRailroad: true, hasRailroadTech: true));
		}

		[Fact]
		public void AdvanceWork_TracksTurnsToCompletion()
		{
			var (t1, done1) = SettlerActionRules.AdvanceWork(turnsWorked: 0, requiredTurns: 2);
			Assert.Equal(1, t1);
			Assert.False(done1);

			var (t2, done2) = SettlerActionRules.AdvanceWork(turnsWorked: 1, requiredTurns: 2);
			Assert.Equal(2, t2);
			Assert.True(done2);
		}
	}
}
