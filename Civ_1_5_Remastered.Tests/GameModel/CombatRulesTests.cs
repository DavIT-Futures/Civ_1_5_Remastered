using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class CombatRulesTests
	{
		[Theory]
		[InlineData(0, 2, 2, 2, false, false, 64)]
		[InlineData(0, 2, 2, 2, true, false, 96)]
		[InlineData(0, 2, 2, 2, true, true, 144)]
		[InlineData(1, 3, 2, 4, true, false, 48)]
		[InlineData(2, 3, 10, 4, false, false, 48)]
		[InlineData(0, 3, 10, 4, false, false, -1)]
		[InlineData(1, 3, 10, 4, false, false, -1)]
		public void GuardianScore(int landGroup, int guard, int terrain, int terrainFactor, bool fortified, bool veteran, int expected) =>
			Assert.Equal(expected, CombatRules.ComputeGuardianScore(landGroup, guard, terrain, terrainFactor, fortified, veteran));
	}
}
