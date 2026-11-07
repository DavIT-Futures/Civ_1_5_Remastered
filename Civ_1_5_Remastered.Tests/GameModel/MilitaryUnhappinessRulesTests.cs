using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class MilitaryUnhappinessRulesTests
	{
		[Theory]
		[InlineData(2, 1, true)]
		[InlineData(ShipRules.Trireme, 1, false)]
		[InlineData(ShipRules.Sail, 1, false)]
		[InlineData(ShipRules.Transport, 0, false)]
		[InlineData(ShipRules.Frigate, 2, true)]
		[InlineData(ShipRules.Ironclad, 4, true)]
		[InlineData(ShipRules.Battleship, 18, true)]
		public void CivilianShipsAreSparedWarshipsAreNot(int kind, int assault, bool unhappy) =>
			Assert.Equal(unhappy, MilitaryUnhappinessRules.CausesUnhappinessAway(kind, assault));

		[Fact]
		public void CivilianAndWarshipUpgradeChains()
		{
			Assert.Equal(ShipRules.Sail, UnitUpgradeRules.Successor(ShipRules.Trireme));
			Assert.Equal(ShipRules.Transport, UnitUpgradeRules.Successor(ShipRules.Sail));
			Assert.Equal(ShipRules.Ironclad, UnitUpgradeRules.Successor(ShipRules.Frigate));
			Assert.Equal(ShipRules.Cruiser, UnitUpgradeRules.Successor(ShipRules.Ironclad));
			Assert.Equal(ShipRules.Battleship, UnitUpgradeRules.Successor(ShipRules.Cruiser));
			Assert.Equal(UnitUpgradeRules.NoSuccessor, UnitUpgradeRules.Successor(ShipRules.Transport));
		}
	}
}
