using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class DiplomacyForecastTests
	{
		private const int Human = 1;
		private const int Rival = 2;

		[Fact]
		public void FacingArmiesMakeRulerHostileAndDemandTribute()
		{
			Player[] participants = CreateParticipants();
			Contact(participants);
			participants[Human].Gold = 1000;
			participants[Rival].Landmasses[1].Assault = 40;
			participants[Rival].Landmasses[1].Guard = 200;
			participants[Rival].Landmasses[1].TownTally = 3;
			participants[Human].Landmasses[1].Assault = 5;
			participants[Human].Landmasses[1].TownTally = 1;
			participants[Rival].MartialMight = 50;
			participants[Human].MartialMight = 40;

			DiplomacyForecast forecast = Compute(participants, aggression: 1);

			Assert.True(forecast.Hostile);
			Assert.True(forecast.Tribute > 0);
			Assert.Contains("Our armies face each other.", forecast.HostileReasons);
			Assert.False(forecast.OffersPeace);
		}

		[Fact]
		public void OverwhelmingArmyCalmsRuler()
		{
			Player[] participants = CreateParticipants();
			Contact(participants);
			participants[Rival].Landmasses[1].Assault = 40;
			participants[Rival].Landmasses[1].Guard = 200;
			participants[Rival].Landmasses[1].TownTally = 3;
			participants[Human].Landmasses[1].Assault = 5;
			participants[Human].Landmasses[1].TownTally = 1;
			participants[Rival].MartialMight = 10;
			participants[Human].MartialMight = 100;

			DiplomacyForecast forecast = Compute(participants, aggression: 1);

			Assert.False(forecast.Hostile);
			Assert.True(forecast.OffersPeace);
		}

		[Fact]
		public void TroopsNearTheirCitiesCreateFearAndPeaceGold()
		{
			Player[] participants = CreateParticipants();
			Contact(participants);
			participants[Rival].Gold = 500;
			participants[Rival].TownTally = 3;
			participants[Rival].Landmasses[2].TownTally = 3;
			participants[Rival].Landmasses[2].Assault = 1;
			participants[Human].Landmasses[2].Guard = 10;

			DiplomacyForecast forecast = Compute(participants, aggression: 0);

			Assert.True(forecast.Fear > 2);
			Assert.True(forecast.PeaceGold > 0);
			Assert.False(forecast.Hostile);
		}

		[Fact]
		public void VendettaUnderPeaceBecomesHiddenBetrayalAndAttitudeBreakdownAddsUp()
		{
			Player[] participants = CreateParticipants();
			Contact(participants);
			participants[Human].Relations[Rival] |= DiplomacyRules.PeaceRelation;
			participants[Rival].Relations[Human] |= DiplomacyRules.PeaceRelation;
			participants[Human].Gold = 1000;
			participants[Rival].Standing = 5;
			participants[Rival].MartialMight = 50;
			participants[Human].MartialMight = 40;
			DiplomacyRules.BeginVendetta(participants, Rival, Human, DiplomacyRules.SneakAttackGrudge);

			DiplomacyForecast forecast = Compute(participants, aggression: 1);

			Assert.True(forecast.PlansBetrayal);
			Assert.False(forecast.Hostile);
			Assert.Equal(-2, forecast.VendettaScore);
			Assert.Equal(forecast.CharacterScore + forecast.VendettaScore + forecast.RivalWarsScore + forecast.MightScore + forecast.PeaceScore, forecast.Attitude);
			Assert.Contains("Vendetta: they want revenge.", forecast.HostileReasons);
		}

		private static DiplomacyForecast Compute(Player[] participants, int aggression) =>
			DiplomacyForecast.Compute(participants, Rival, Human, aggression, 2, 100, false, 10, 10, true);

		private static void Contact(Player[] participants)
		{
			participants[Human].Relations[Rival] |= DiplomacyRules.ContactRelation;
			participants[Rival].Relations[Human] |= DiplomacyRules.ContactRelation;
		}

		private static Player[] CreateParticipants()
		{
			Player[] participants = new Player[8];

			for (int index = 0; index < participants.Length; index++)
			{
				participants[index] = new Player();
			}

			return participants;
		}
	}
}
