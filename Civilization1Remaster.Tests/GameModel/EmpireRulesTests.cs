using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class ScienceRulesTests
	{
		[Theory]
		[InlineData(true, 0, 0, true, 1)]
		[InlineData(true, 0, 3, true, 24)]
		[InlineData(true, 2, 5, true, 50)]
		[InlineData(true, 2, 5, false, 100)]
		[InlineData(false, 4, 5, true, 50)]
		[InlineData(false, 0, 10, false, 280)]
		public void TechnologyCost(bool human, int tier, int found, bool bc, int expected) =>
			Assert.Equal(expected, ScienceRules.ComputeTechnologyCost(human, tier, found, bc));

		[Fact]
		public void CanResearchNeedsBothKnownPrerequisites()
		{
			Assert.True(ScienceRules.CanResearch(-1, -1, tech => false));
			Assert.False(ScienceRules.CanResearch(3, -1, tech => false));
			Assert.False(ScienceRules.CanResearch(3, 4, tech => tech == 3));
			Assert.True(ScienceRules.CanResearch(3, 4, tech => true));
		}

		[Fact]
		public void ResearchCompletesAtCost()
		{
			Assert.True(ScienceRules.IsResearchComplete(10, 10));
			Assert.False(ScienceRules.IsResearchComplete(9, 10));
		}

		[Theory]
		[InlineData(0x22, true)]
		[InlineData(0x25, true)]
		[InlineData(0x23, false)]
		public void BarracksObsolescence(int tech, bool expected) => Assert.Equal(expected, ScienceRules.DoesTechObsoleteBarracks(tech));

		[Fact]
		public void PlayerKnowsTechnologyReadsBitPerTech()
		{
			ushort[] markers = new ushort[5];
			markers[1] = 1 << 1; // tech 17

			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 1, 17));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 16));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 0, 17));
			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 0, -1));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 71));
			Assert.False(ScienceRules.PlayerKnowsTechnology(new ushort[1], 1, 20));
		}

		[Fact]
		public void TechnologyInfo() => Assert.Equal(13, ScienceRules.CalculateTechnologyInfo(3, 2, 5));
	}

	public class GovernmentRulesTests
	{
		[Theory]
		[InlineData(0, -1)]
		[InlineData(1, -1)]
		[InlineData(2, 5)]
		[InlineData(3, 43)]
		[InlineData(4, 44)]
		[InlineData(5, 4)]
		public void RequiredTech(short regime, short tech) => Assert.Equal(tech, GovernmentRules.GetRequiredTechForGovernment(regime));

		[Fact]
		public void CanAdoptGovernment()
		{
			Assert.True(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeDespotism, tech => false, false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeAnarchy, tech => true, true));
			Assert.False(GovernmentRules.CanAdoptGovernment(6, tech => true, true));
			Assert.True(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeDemocracy, tech => false, true));
			Assert.True(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeMonarchy, tech => tech == 5, false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeRepublic, tech => tech == 5, false));
		}

		[Theory]
		[InlineData(0, 4)]
		[InlineData(1, 4)]
		[InlineData(2, 3)]
		[InlineData(3, 3)]
		[InlineData(4, 2)]
		[InlineData(5, 2)]
		public void DefaultLevyRatio(short regime, short expected) => Assert.Equal(expected, GovernmentRules.ComputeDefaultLevyRatio(regime));

		private static bool Treaty(bool marker = true, ushort p1Human = 0, ushort p2Human = 0, int p1Might = 10, int p2Might = 10, int humanMight = 5,
			short p1Nukes = 0, short p2Nukes = 0, int displacement = 0, Func<short, ushort>? relation = null,
			Func<short, (int, int, int)>? p1Land = null, Func<short, (int, int)>? p2Land = null) =>
			GovernmentRules.EvaluatePeaceTreaty(marker, 1, 2, 3, p1Human, p2Human, p1Might, p2Might, humanMight, p1Nukes, p2Nukes, displacement,
				relation ?? (other => 0), p1Land ?? (land => (0, 0, 0)), p2Land ?? (land => (0, 0)));

		[Fact]
		public void TreatyAcceptedWhenMarkerOff() => Assert.True(Treaty(marker: false, p1Nukes: 0, p2Nukes: 5));

		[Fact]
		public void TreatyAcceptedWithNoArmiesAnywhere() => Assert.True(Treaty());

		[Fact]
		public void TreatyRefusedWhenFewerNukes() => Assert.False(Treaty(p1Nukes: 1, p2Nukes: 2));

		[Fact]
		public void TreatyRefusedWhenAtWarWithStrongHuman()
		{
			Assert.False(Treaty(p1Human: 1, p2Might: 4, humanMight: 5));
			Assert.True(Treaty(p1Human: 1 | 4, p2Might: 4, p2Human: 4, humanMight: 5));
		}

		[Fact]
		public void TreatyRefusedWhenOutguardedOnOwnLand() =>
			Assert.False(Treaty(p1Land: land => land == 3 ? (1, 4, 1) : (0, 0, 0), p2Land: land => (10, 0)));

		[Fact]
		public void TreatyNeedsEnoughGuardAgainstThreatenedCities()
		{
			// p1 has 10 guard and 1 city where p2 has 2 assault: ratio = 40 / 3 = 13 >= 4
			Assert.True(Treaty(p1Land: land => land == 2 ? (1, 10, 5) : (0, 0, 0), p2Land: land => (0, 2)));
			// with many wars the ratio is not enough and p1 has a threatened city
			Assert.False(Treaty(p1Land: land => land == 2 ? (1, 10, 5) : (0, 0, 0), p2Land: land => (0, 2), relation: other => 1, displacement: -5));
		}
	}

	public class ScoreRulesTests
	{
		[Fact]
		public void Components()
		{
			Assert.Equal(8, ScoreRules.ComputeCitizenScore(2, 3, 1));
			Assert.Equal(60, ScoreRules.ComputeWonderScore(3));
			Assert.Equal(250, ScoreRules.ComputeSpaceshipScore(50, 10));
			Assert.Equal(0, ScoreRules.ComputeSpaceshipScore(-1, 10));
			Assert.Equal(30, ScoreRules.ComputePeaceBonus(10));
			Assert.Equal(100, ScoreRules.ComputePeaceBonus(40));
			Assert.Equal(0, ScoreRules.ComputePeaceBonus(-1));
			Assert.Equal(15, ScoreRules.ComputeFutureTechScore(3));
			Assert.Equal(20, ScoreRules.ComputePollutionPenalty(2));
			Assert.Equal(1600, ScoreRules.ComputeConquestBonus(3, 100));
		}

		[Theory]
		[InlineData(10, 20, 5, 5, 5, 5, -1, 40)]
		[InlineData(10, 20, 0, 0, 0, 50, -1, 0)]
		[InlineData(10, 20, 0, 0, 0, 50, 5, 5)]
		[InlineData(10, 20, 5, 5, 5, 5, 30, 40)]
		[InlineData(10, 20, 5, 5, 5, 5, 41, 41)]
		public void Total(int citizens, int wonders, int spaceship, int peace, int future, int pollution, int conquest, int expected) =>
			Assert.Equal(expected, ScoreRules.ComputeTotalScore(citizens, wonders, spaceship, peace, future, pollution, conquest));
	}

	public class SettlerActionRulesTests
	{
		[Theory]
		[InlineData(0, 2, 5, 5)]
		[InlineData(2, 2, 5, 10)]
		[InlineData(3, 4, 10, 10)]
		[InlineData(5, 10, 5, 10)]
		[InlineData(6, 3, 5, 10)]
		[InlineData(8, 6, 15, 10)]
		[InlineData(9, 4, 15, 10)]
		[InlineData(10, 2, 5, 10)]
		public void WorkTurns(int terrain, int road, int irrigation, int mine)
		{
			Assert.Equal(road, SettlerActionRules.GetRoadTurns(terrain));
			Assert.Equal(irrigation, SettlerActionRules.GetIrrigationTurns(terrain));
			Assert.Equal(mine, SettlerActionRules.GetMiningTurns(terrain));
		}

		[Fact]
		public void WhatCanBeBuilt()
		{
			Assert.True(SettlerActionRules.CanIrrigate(2, true, false));
			Assert.False(SettlerActionRules.CanIrrigate(2, false, false));
			Assert.False(SettlerActionRules.CanIrrigate(2, true, true));
			Assert.False(SettlerActionRules.CanIrrigate(5, true, false));
			Assert.False(SettlerActionRules.CanIrrigate(7, true, false));
			Assert.False(SettlerActionRules.CanIrrigate(10, true, false));

			Assert.True(SettlerActionRules.CanBuildMine(4, false));
			Assert.False(SettlerActionRules.CanBuildMine(4, true));
			Assert.False(SettlerActionRules.CanBuildMine(2, false));

			Assert.True(SettlerActionRules.CanBuildRoad(false, false, false));
			Assert.True(SettlerActionRules.CanBuildRoad(true, false, true));
			Assert.False(SettlerActionRules.CanBuildRoad(true, false, false));
			Assert.False(SettlerActionRules.CanBuildRoad(true, true, true));
		}

		[Fact]
		public void AdvanceWork()
		{
			Assert.Equal((1, false), SettlerActionRules.AdvanceWork(0, 3));
			Assert.Equal((3, true), SettlerActionRules.AdvanceWork(2, 3));
		}
	}

	public class DiplomatAndTradeRulesTests
	{
		[Theory]
		[InlineData(250, 3, 2, 0, 400)]
		[InlineData(250, 3, 2, 1, 200)]
		[InlineData(0, -2, 1, 0, 750)]
		public void BribeCost(int gold, int distance, int shields, int type, int expected) =>
			Assert.Equal(expected, DiplomatRules.ComputeBribeCost(gold, distance, shields, type));

		[Theory]
		[InlineData(200, 7, 4, false, 480)]
		[InlineData(200, 7, 4, true, 240)]
		[InlineData(0, -3, 2, false, 2000)]
		public void InciteCost(int gold, int distance, int size, bool disorder, int expected) =>
			Assert.Equal(expected, DiplomatRules.ComputeInciteRevoltCost(gold, distance, size, disorder));

		[Theory]
		[InlineData(100, 100, true, true, false)]
		[InlineData(200, 100, true, true, true)]
		[InlineData(200, 100, false, true, false)]
		[InlineData(99, 100, true, false, false)]
		public void InciteOptions(int gold, int cost, bool peace, bool incite, bool subvert) =>
			Assert.Equal((incite, subvert), DiplomatRules.EvaluateInciteOptions(gold, cost, peace));

		[Fact]
		public void TechnologyToStealWrapsFromSeed()
		{
			Assert.Equal(5, DiplomatRules.FindTechnologyToSteal(70, tech => tech == 5, tech => false));
			Assert.Equal(71, DiplomatRules.FindTechnologyToSteal(70, tech => true, tech => tech == 70));
			Assert.Equal(-1, DiplomatRules.FindTechnologyToSteal(0, tech => true, tech => true));
		}

		[Fact]
		public void SabotageTargetSkipsPalaceAndMissingBuildings()
		{
			Queue<int> rolls = new Queue<int>(new[] { 0, 3, 5 });
			Assert.Equal(5, DiplomatRules.PickIndustrialSabotageTarget(false, true, 1u << 5, () => rolls.Dequeue()));
			Assert.Equal(-1, DiplomatRules.PickIndustrialSabotageTarget(true, true, 0xffff, () => 1));
			Assert.Equal(-1, DiplomatRules.PickIndustrialSabotageTarget(false, true, 0, () => 1));
		}

		[Fact]
		public void TradeDistanceAgreesWithMapRules()
		{
			for (int x = 0; x < 80; x += 7)
				for (int y = 0; y < 50; y += 5)
					Assert.Equal(MapRules.Distance(3, 4, x, y), TradeRules.ComputeDistance(3, 4, x, y));
		}

		[Theory]
		[InlineData(14, 10, 14, false, false, false, false, 24)]
		[InlineData(14, 10, 14, true, false, false, false, 12)]
		[InlineData(14, 10, 14, true, true, false, false, 6)]
		[InlineData(14, 10, 14, false, false, true, false, 16)]
		[InlineData(14, 10, 14, false, false, true, true, 11)]
		public void OneTimeTradeBonus(int distance, int sourceTrade, int destTrade, bool continent, bool civilization, bool railroad, bool flight, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeOneTimeTradeBonus(distance, sourceTrade, destTrade, continent, civilization, railroad, flight));

		[Theory]
		[InlineData(10, 10, false, 3)]
		[InlineData(10, 10, true, 1)]
		public void OngoingTradeRouteArrows(int a, int b, bool domestic, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeOngoingTradeRouteArrows(a, b, domestic));

		[Theory]
		[InlineData(0, 5, 10, 0)]
		[InlineData(100, 0, 10, 0)]
		[InlineData(110, 3, 10, 30)]
		[InlineData(20000, 3, 10, 5454)]
		public void CityPlunder(int gold, int size, int empire, int expected) =>
			Assert.Equal(expected, TradeRules.ComputeCityPlunder(gold, size, empire));

		[Fact]
		public void TradeRouteSlots()
		{
			sbyte[] routes = { 4, -1, 6 };
			Assert.Equal(0, TradeRules.TryAssignTradeRouteSlot(routes, 4, 10, city => 0));
			Assert.Equal(1, TradeRules.TryAssignTradeRouteSlot(routes, 9, 10, city => 0));
			Assert.Equal(new sbyte[] { 4, 9, 6 }, routes);

			// full: the worst route below the new value is replaced
			Assert.Equal(2, TradeRules.TryAssignTradeRouteSlot(routes, 11, 10, city => city == 6 ? 1 : 5));
			Assert.Equal(new sbyte[] { 4, 9, 11 }, routes);

			// full and every route is better: nothing changes
			Assert.Equal(-1, TradeRules.TryAssignTradeRouteSlot(routes, 12, 3, city => 5));
			Assert.Equal(new sbyte[] { 4, 9, 11 }, routes);
		}
	}
}
