using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class UnitUpgradeRulesTests
	{
		[Theory]
		[InlineData(6, 10)]
		[InlineData(10, 7)]
		[InlineData(7, 11)]
		[InlineData(1, 2)]
		[InlineData(2, 4)]
		[InlineData(3, 4)]
		[InlineData(4, 5)]
		[InlineData(5, 12)]
		[InlineData(8, 9)]
		[InlineData(9, 13)]
		[InlineData(16, 17)]
		[InlineData(17, 24)]
		[InlineData(18, 19)]
		[InlineData(19, 20)]
		[InlineData(20, 21)]
		public void ChainsOfUpgrades(int kind, int successor) => Assert.Equal(successor, UnitUpgradeRules.Successor(kind));

		[Theory]
		[InlineData(0)]
		[InlineData(11)]
		[InlineData(12)]
		[InlineData(22)]
		[InlineData(26)]
		public void UnitsWithoutUpgrade(int kind) => Assert.Equal(UnitUpgradeRules.NoSuccessor, UnitUpgradeRules.Successor(kind));

		[Theory]
		[InlineData(3, 3, 30)]
		[InlineData(1, 2, 30)]
		[InlineData(2, 4, 60)]
		[InlineData(4, 8, 160)]
		[InlineData(8, 16, 480)]
		public void CostOfTheMissingShields(int formerPrice, int successorPrice, int cost) => Assert.Equal(cost, UnitUpgradeRules.Cost(formerPrice, successorPrice));

		[Theory]
		[InlineData(60, 30, true)]
		[InlineData(59, 30, false)]
		public void ComputerKeepsHalfOfItsGold(int gold, int cost, bool upgrades) => Assert.Equal(upgrades, UnitUpgradeRules.ComputerUpgrades(gold, cost));
	}
}
