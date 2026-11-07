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
		public void LeaderEnvyMakesThemHostileButMuchStrongerArmyKeepsTheTreaty()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 181, theirArmy: 79));

			Assert.Contains("We lead the world without nukes.", forecast.HostileReasons);
			Assert.False(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.Contains("Our army is 1.5x stronger: they keep the treaty.", forecast.CalmingReasons);
		}

		[Fact]
		public void SlightlyStrongerArmyDoesNotStopTheSecretPlan()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 100, theirArmy: 79));

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
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 60, theirArmy: 79));

			Assert.True(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.True(forecast.Tribute > 0);
		}

		[Fact]
		public void ThreeTimesStrongerArmyEndsTheHostility()
		{
			DiplomacyForecast forecast = Forecast(Scenario(ourArmy: 240, theirArmy: 79));

			Assert.False(forecast.Hostile);
			Assert.False(forecast.PlansBetrayal);
			Assert.Contains("Our army is over 3x stronger.", forecast.CalmingReasons);
		}

		[Fact]
		public void NoEnvyBeforeTurnTwoHundredOrWithNukes()
		{
			Assert.DoesNotContain("We lead the world without nukes.", Forecast(Scenario(181, 79), roundTally: 150).HostileReasons);

			Player[] armed = Scenario(181, 79);
			armed[Human].LiveTroops[DiplomacyForecast.NuclearUnitKind] = 1;
			Assert.DoesNotContain("We lead the world without nukes.", Forecast(armed).HostileReasons);
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

		private static Player[] Scenario(int ourArmy, int theirArmy)
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
			DiplomacyRules.SetPeaceUnits(participants, Human, Mongols, DiplomacyRules.MaximumUnits);

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
