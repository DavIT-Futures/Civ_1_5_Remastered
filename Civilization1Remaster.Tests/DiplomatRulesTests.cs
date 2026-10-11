using System;
using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class DiplomatRulesTests
	{
		[Fact]
		public void ComputeBribeCost_SettlersVersusMilitaryUnit()
		{
			// Victim gold: 250 -> 250 + 750 = 1000
			// Distance to city: 8 -> 8 + 2 = 10
			// 1000 / 10 = 100
			// Unit cost: 40 shields
			// For settlers (type 0): cost = 100 * 40 = 4000 (not halved)
			// For military unit (type 1): cost = (100 * 40) / 2 = 2000 (halved)
			int settlerCost = DiplomatRules.ComputeBribeCost(victimGold: 250, distanceToNearestCity: 8, unitCostShields: 40, unitType: 0);
			int militaryCost = DiplomatRules.ComputeBribeCost(victimGold: 250, distanceToNearestCity: 8, unitCostShields: 40, unitType: 1);

			Assert.Equal(4000, settlerCost);
			Assert.Equal(2000, militaryCost);
		}

		[Fact]
		public void ComputeBribeCost_CloserUnitCostsMore()
		{
			// Distance 1 vs Distance 23
			int closeCost = DiplomatRules.ComputeBribeCost(victimGold: 100, distanceToNearestCity: 1, unitCostShields: 20, unitType: 2);
			int farCost = DiplomatRules.ComputeBribeCost(victimGold: 100, distanceToNearestCity: 23, unitCostShields: 20, unitType: 2);

			Assert.True(closeCost > farCost, "A unit closer to its home city should be much harder/more expensive to bribe.");
		}

		[Fact]
		public void ComputeInciteRevoltCost_CivilDisorderHalvesPrice()
		{
			// Victim gold: 500 -> 500 + 1000 = 1500
			// Distance to capital: 12 -> 12 + 3 = 15
			// 1500 / 15 = 100
			// City size: 6 -> 100 * 6 = 600
			int normalCost = DiplomatRules.ComputeInciteRevoltCost(victimGold: 500, distanceToCapital: 12, citySize: 6, inDisorder: false);
			int disorderCost = DiplomatRules.ComputeInciteRevoltCost(victimGold: 500, distanceToCapital: 12, citySize: 6, inDisorder: true);

			Assert.Equal(600, normalCost);
			Assert.Equal(300, disorderCost);
		}

		[Fact]
		public void EvaluateInciteOptions_VerifiesGoldAndTreaty()
		{
			// Case 1: Not enough gold
			var (canIncite1, canSubvert1) = DiplomatRules.EvaluateInciteOptions(playerGold: 500, baseCost: 600, hasPeaceTreaty: true);
			Assert.False(canIncite1);
			Assert.False(canSubvert1);

			// Case 2: Enough for revolt, but not for subversion
			var (canIncite2, canSubvert2) = DiplomatRules.EvaluateInciteOptions(playerGold: 800, baseCost: 600, hasPeaceTreaty: true);
			Assert.True(canIncite2);
			Assert.False(canSubvert2);

			// Case 3: Enough for subversion (1200) and treaty present
			var (canIncite3, canSubvert3) = DiplomatRules.EvaluateInciteOptions(playerGold: 1300, baseCost: 600, hasPeaceTreaty: true);
			Assert.True(canIncite3);
			Assert.True(canSubvert3);

			// Case 4: Enough gold for subversion, but no treaty
			var (canIncite4, canSubvert4) = DiplomatRules.EvaluateInciteOptions(playerGold: 1300, baseCost: 600, hasPeaceTreaty: false);
			Assert.True(canIncite4);
			Assert.False(canSubvert4);
		}

		[Fact]
		public void FindTechnologyToSteal_SelectsKnownByVictimAndUnknownByPlayer()
		{
			var victimTechs = new HashSet<int> { 5, 10, 20 };
			var playerTechs = new HashSet<int> { 5 }; // Player already knows tech 5

			// Seed starting at 0: should skip 5 (known by player), and pick 10
			int stolenTech = DiplomatRules.FindTechnologyToSteal(0, victimTechs.Contains, playerTechs.Contains);
			Assert.Equal(10, stolenTech);

			// If seed starts at 15: should find 20
			int stolenTechSeed15 = DiplomatRules.FindTechnologyToSteal(15, victimTechs.Contains, playerTechs.Contains);
			Assert.Equal(20, stolenTechSeed15);

			// If victim has no tech player doesn't already have: returns -1
			var playerKnowsAll = new HashSet<int> { 5, 10, 20 };
			int noTech = DiplomatRules.FindTechnologyToSteal(0, victimTechs.Contains, playerKnowsAll.Contains);
			Assert.Equal(-1, noTech);
		}
	}
}
