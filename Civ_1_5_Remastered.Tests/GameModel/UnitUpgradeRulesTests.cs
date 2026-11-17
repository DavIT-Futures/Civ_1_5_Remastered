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
		[InlineData(2, UnitKinds.Pikemen)]
		[InlineData(UnitKinds.Pikemen, 4)]
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
		[InlineData(30, 30, 30)]
		[InlineData(10, 20, 30)]
		[InlineData(20, 25, 30)]
		[InlineData(25, 30, 30)]
		[InlineData(20, 40, 60)]
		[InlineData(40, 80, 160)]
		[InlineData(80, 160, 480)]
		public void CostOfTheMissingShields(int formerShields, int successorShields, int cost) => Assert.Equal(cost, UnitUpgradeRules.Cost(formerShields, successorShields));

		[Theory]
		[InlineData(UnitKinds.Phalanx, UnitKinds.Pikemen, UnitKinds.Pikemen)]
		[InlineData(UnitKinds.Phalanx, UnitKinds.Musketeers, UnitKinds.Musketeers)]
		[InlineData(1, UnitKinds.Musketeers, UnitKinds.Musketeers)]
		[InlineData(UnitKinds.Pikemen, UnitKinds.Musketeers, UnitKinds.Musketeers)]
		[InlineData(UnitKinds.Phalanx, UnitKinds.Phalanx, UnitUpgradeRules.NoSuccessor)]
		public void UpgradesGoAsFarAsTheKnownKinds(int kind, int lastKnown, int furthest)
		{
			// Kinds along the chain Militia, Phalanx, Pikemen, Musketeers are known up to lastKnown.
			int[] chain = { 1, UnitKinds.Phalanx, UnitKinds.Pikemen, UnitKinds.Musketeers };
			bool Known(int candidate) => Array.IndexOf(chain, candidate) >= 0 && Array.IndexOf(chain, candidate) <= Array.IndexOf(chain, lastKnown);

			Assert.Equal(furthest, UnitUpgradeRules.FurthestSuccessor(kind, Known));
		}

		[Theory]
		[InlineData(60, 30, true)]
		[InlineData(59, 30, false)]
		public void ComputerKeepsHalfOfItsGold(int gold, int cost, bool upgrades) => Assert.Equal(upgrades, UnitUpgradeRules.ComputerUpgrades(gold, cost));
	}
}
