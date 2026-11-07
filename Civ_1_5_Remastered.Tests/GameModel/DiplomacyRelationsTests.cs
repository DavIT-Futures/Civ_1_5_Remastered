using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	/// <summary>
	/// Relations between the player and a computer ruler, modelled on a real War Advisor screen: the Mongols
	/// (Genghis Khan, aggressive) at peace for 60+ turns, army 79 against our 181, while we lead the world
	/// after turn 200 without nuclear weapons.
	/// </summary>
	public class DiplomacyRelationsTests
	{
		private const int Human = 1;
		private const int Mongols = 2;
		private const int GenghisAggression = 1;

		[Fact]
		public void AttitudeBreakdownMatchesTheAdvisor()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 181, theirArmy: 79));

			Assert.Equal(-1, forecast.CharacterScore);
			Assert.Equal(0, forecast.VendettaScore);
			Assert.Equal(0, forecast.RivalWarsScore);
			Assert.Equal(0, forecast.MightScore);
			Assert.Equal(2, forecast.PeaceScore);
			Assert.Equal(1, forecast.Attitude);
		}

		[Fact]
		public void SixtyTurnsOfPeaceEndTheEnvy()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 181, theirArmy: 79));

			Assert.True(forecast.EnvyActive);
			Assert.Equal(2, forecast.EnvyEase);
			Assert.DoesNotContain("We lead the world without nukes.", forecast.HostileReasons);
			Assert.Contains("60+ turns of peace ended their envy.", forecast.CalmingReasons);
			Assert.False(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
		}

		[Fact]
		public void ShorterPeaceLeavesEnvyButStrongerArmyKeepsTheTreaty()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 181, theirArmy: 79, peaceUnits: 10));

			Assert.Equal(1, forecast.EnvyEase);
			Assert.Contains("We lead the world without nukes.", forecast.HostileReasons);
			Assert.False(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.Contains("Our army is 1.5x stronger: they keep the treaty.", forecast.CalmingReasons);
		}

		[Fact]
		public void AfterThirtyTwoTurnsAGoodMoodOutweighsTheEnvy()
		{
			DiplomacyForecast friendly = DiplomacyForecast.Compute(Scenario(181, 79, peaceUnits: 10), Mongols, Human, -1, 2, 300, false, 30, 40, false);
			DiplomacyForecast fresh = DiplomacyForecast.Compute(Scenario(181, 79, peaceUnits: 5), Mongols, Human, -1, 2, 300, false, 30, 40, false);

			Assert.Contains("Their good attitude outweighs it.", friendly.CalmingReasons);
			Assert.DoesNotContain("Their good attitude outweighs it.", fresh.CalmingReasons);
			Assert.Equal(0, DiplomacyRules.EnvyEase(false, DiplomacyRules.MaximumUnits));
		}

		[Fact]
		public void SlightlyStrongerArmyDoesNotStopTheSecretPlan()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 100, theirArmy: 79, peaceUnits: 5));

			Assert.False(forecast.Hostile);
			Assert.True(forecast.PlansBetrayal);
		}

		[Fact]
		public void VendettaKeepsThePlanWhateverOurArmy()
		{
			Player[] participants = Scenario(ourArmy: 181, theirArmy: 79);
			DiplomacyRules.BeginVendetta(participants, Mongols, Human, DiplomacyRules.SneakAttackGrudge);

			DiplomacyForecast forecast = Forecast(participants);

			Assert.True(forecast.PlansBetrayal);
			Assert.Equal(-2, forecast.VendettaScore);
		}

		[Fact]
		public void StrongerRulerDemandsTributeOpenlyInsteadOfPlotting()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 60, theirArmy: 79, peaceUnits: 5));

			Assert.True(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.True(forecast.Tribute > 0);
		}

		[Fact]
		public void ThreeTimesStrongerArmyEndsTheHostility()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 240, theirArmy: 79, peaceUnits: 5));

			Assert.False(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.Contains("Our army is over 3x stronger.", forecast.CalmingReasons);
		}

		[Fact]
		public void NoEnvyBeforeTurnTwoHundredOrWithNukes()
		{
			Assert.DoesNotContain("We lead the world without nukes.", Forecast(Scenario(181, 79, peaceUnits: 5), roundTally: 150).HostileReasons);

			Player[] armed = Scenario(181, 79, peaceUnits: 5);
			armed[Human].LiveTroops[DiplomacyForecast.NuclearUnitKind] = 1;
			Assert.DoesNotContain("We lead the world without nukes.", Forecast(armed).HostileReasons);
		}

		[Fact]
		public void ConditionsPageListsEveryRuleWithItsEffect()
		{
			List<(string Text, int Effect)> lines = DiplomacyConditions.Lines(Forecast(Scenario(ourArmy: 181, theirArmy: 79)));

			Assert.Contains(("Envy of the leader: gone, 60+ turns of peace.", 1), lines);
			Assert.Contains(("No armies facing each other.", 1), lines);
			Assert.Contains(("No vendetta against us.", 1), lines);
			Assert.Contains(("Army: ours 181, theirs 79 (2.2x).", 0), lines);
			Assert.Contains(("  Over 3x ends all hostility (238+).", -1), lines);
			Assert.Contains(("  1.5x keeps them to the treaty (119+).", 1), lines);
			Assert.Contains(("Mood +1 must beat 0 to drop demands.", 1), lines);
			Assert.Contains(("Treaty 60+ turns: trust +2.", 1), lines);
			Assert.Equal(("RESULT: they keep the peace.", 1), lines[^1]);
		}

		[Fact]
		public void ConditionsPageWarnsAboutEnvyVendettaAndPlots()
		{
			Player[] participants = Scenario(ourArmy: 100, theirArmy: 79, peaceUnits: 5);
			List<(string Text, int Effect)> plotting = DiplomacyConditions.Lines(Forecast(participants));

			Assert.Contains(("Envy: we lead after turn 200 without nukes.", -1), plotting);
			Assert.Contains(("  1.5x keeps them to the treaty (119+).", -1), plotting);
			Assert.Equal(("RESULT: plotting to break the peace.", -1), plotting[^1]);

			DiplomacyRules.BeginVendetta(participants, Mongols, Human, DiplomacyRules.SneakAttackGrudge);
			List<(string Text, int Effect)> vengeful = DiplomacyConditions.Lines(Forecast(participants));

			Assert.Contains(("Vendetta: 48 turns left.", -1), vengeful);
			Assert.Contains(("  1.5x keeps treaty, not under vendetta.", -1), vengeful);
			Assert.Equal("1.5x", DiplomacyConditions.Ratio(3, 2));
			Assert.Equal("no army", DiplomacyConditions.Ratio(3, 0));
		}

		[Fact]
		public void FreshTreatyIsKeptForSixteenTurnsEvenByAWeakerRuler()
		{
			Assert.Equal(0, DiplomacyRules.WeighBreakingPeace(3, 79, 100, false));
			Assert.Equal(1, DiplomacyRules.WeighBreakingPeace(4, 79, 100, false));
			Assert.Equal(-1, DiplomacyRules.WeighBreakingPeace(15, 79, 181, false));
			Assert.Equal(1, DiplomacyRules.WeighBreakingPeace(15, 79, 181, true));
		}

		private static DiplomacyForecast Forecast(Player[] participants, int roundTally = 300) =>
			DiplomacyForecast.Compute(participants, Mongols, Human, GenghisAggression, 2, roundTally, false, 30, 40, false);

		private static Player[] Scenario(int ourArmy, int theirArmy, int peaceUnits = DiplomacyRules.MaximumUnits)
		{
			Player[] participants = new Player[8];

			for (int index = 0; index < participants.Length; index++)
			{
				participants[index] = new Player();
			}

			Player human = participants[Human];
			Player mongols = participants[Mongols];

			human.Relations[Mongols] = DiplomacyRules.ContactRelation | DiplomacyRules.PeaceRelation;
			mongols.Relations[Human] = DiplomacyRules.ContactRelation | DiplomacyRules.PeaceRelation;
			DiplomacyRules.SetPeaceUnits(participants, Human, Mongols, peaceUnits);

			human.MartialMight = (short)ourArmy;
			mongols.MartialMight = (short)theirArmy;
			human.Standing = 7;
			human.TownTally = 12;
			human.Gold = 500;
			mongols.TownTally = 6;
			mongols.Standing = 3;

			return participants;
		}
	}
}
