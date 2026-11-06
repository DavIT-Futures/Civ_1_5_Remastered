using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class DiplomacyRulesTests
	{
		private const int Human = 1;
		private const int Rival = 2;

		[Fact]
		public void SneakAttackStartsTimedVendettaInSpareBits()
		{
			Player[] participants = CreateParticipants();
			participants[Rival].Relations[Human] = 0x0003;

			DiplomacyRules.BeginVendetta(participants, Rival, Human, DiplomacyRules.SneakAttackGrudge);

			ushort relation = participants[Rival].Relations[Human];
			Assert.Equal(0x0003, relation & 0x03ff & ~DiplomacyRules.VendettaRelation);
			Assert.NotEqual(0, relation & DiplomacyRules.VendettaRelation);
			Assert.Equal(48, DiplomacyRules.GrudgeTurns(relation));
		}

		[Fact]
		public void RepeatOffenceLengthensVendettaUpToMaximum()
		{
			Player[] participants = CreateParticipants();

			DiplomacyRules.BeginVendetta(participants, Rival, Human, DiplomacyRules.SneakAttackGrudge);
			DiplomacyRules.BeginVendetta(participants, Rival, Human, DiplomacyRules.SneakAttackGrudge);

			Assert.Equal(DiplomacyRules.MaximumUnits, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));
		}

		[Fact]
		public void LegacyVendettaWithoutCounterCountsAsFullGrudge()
		{
			Assert.Equal(DiplomacyRules.LegacyGrudge, DiplomacyRules.GrudgeUnits(DiplomacyRules.VendettaRelation));
			Assert.Equal(0, DiplomacyRules.GrudgeUnits(0x0003));
		}

		[Fact]
		public void VendettaFadesDuringPeaceAndExpires()
		{
			Player[] participants = CreateParticipants();
			MakePeace(participants);
			DiplomacyRules.BeginVendetta(participants, Rival, Human, 2);

			Assert.False(DiplomacyRules.AdvanceCounters(participants, Rival, Human, 3));
			Assert.False(DiplomacyRules.AdvanceCounters(participants, Rival, Human, 4));
			Assert.Equal(1, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));

			Assert.True(DiplomacyRules.AdvanceCounters(participants, Rival, Human, 8));
			Assert.Equal(0, participants[Rival].Relations[Human] & DiplomacyRules.VendettaRelation);
			Assert.Equal(DiplomacyRules.PeaceRelation, participants[Rival].Relations[Human] & 0x03ff);
		}

		[Fact]
		public void VendettaDoesNotFadeWhileFighting()
		{
			Player[] participants = CreateParticipants();
			DiplomacyRules.BeginVendetta(participants, Rival, Human, 3);
			participants[Rival].Relations[Human] |= DiplomacyRules.RecentCombatRelation;

			DiplomacyRules.AdvanceCounters(participants, Rival, Human, 8);

			Assert.Equal(3, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));
		}

		[Fact]
		public void VendettaFadesAtHalfSpeedInQuietWar()
		{
			Player[] participants = CreateParticipants();
			DiplomacyRules.BeginVendetta(participants, Rival, Human, 3);

			DiplomacyRules.AdvanceCounters(participants, Rival, Human, 4);
			Assert.Equal(3, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));

			DiplomacyRules.AdvanceCounters(participants, Rival, Human, 8);
			Assert.Equal(2, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));
		}

		[Fact]
		public void PeaceCounterGrowsSaturatesAndResetsWithoutTouchingFlags()
		{
			Player[] participants = CreateParticipants();
			MakePeace(participants);

			for (int round = 4; round <= 100; round += 4)
			{
				DiplomacyRules.AdvanceCounters(participants, Human, Rival, round);
			}

			Assert.Equal(DiplomacyRules.MaximumUnits, DiplomacyRules.PeaceUnits(participants, Rival, Human));
			Assert.Equal(60, DiplomacyRules.PeaceTurns(participants, Human, Rival));
			Assert.Equal(DiplomacyRules.PeaceRelation, participants[Human].Relations[Rival] & 0x03ff);

			participants[Human].Relations[Rival] &= 0xfffd;
			DiplomacyRules.AdvanceCounters(participants, Human, Rival, 104);

			Assert.Equal(0, DiplomacyRules.PeaceUnits(participants, Human, Rival));
		}

		[Fact]
		public void PeaceCounterAndGrudgeShareWordsIndependently()
		{
			Player[] participants = CreateParticipants();

			DiplomacyRules.BeginVendetta(participants, Human, Rival, 9);
			DiplomacyRules.BeginVendetta(participants, Rival, Human, 5);
			DiplomacyRules.SetPeaceUnits(participants, Human, Rival, 11);

			Assert.Equal(9, DiplomacyRules.GrudgeUnits(participants[Human].Relations[Rival]));
			Assert.Equal(5, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));
			Assert.Equal(11, DiplomacyRules.PeaceUnits(participants, Rival, Human));
		}

		[Fact]
		public void ReparationsAreExpensiveAndScaleWithDifficulty()
		{
			Assert.Equal(0, DiplomacyRules.ComputeReparations(0, 2));
			Assert.Equal(800, DiplomacyRules.ComputeReparations(12, 0));
			Assert.Equal(1600, DiplomacyRules.ComputeReparations(12, 2));
			Assert.Equal(2400, DiplomacyRules.ComputeReparations(12, 4));
			Assert.Equal(DiplomacyRules.MinimumReparations, DiplomacyRules.ComputeReparations(1, 0));
			Assert.Equal(3000, DiplomacyRules.ComputeReparations(15, 4));
		}

		[Fact]
		public void SofteningAndEndingVendetta()
		{
			Player[] participants = CreateParticipants();
			DiplomacyRules.BeginVendetta(participants, Rival, Human, 12);

			DiplomacyRules.SoftenVendetta(participants, Rival, Human);
			Assert.Equal(6, DiplomacyRules.GrudgeUnits(participants[Rival].Relations[Human]));

			DiplomacyRules.EndVendetta(participants, Rival, Human);
			Assert.Equal(0, participants[Rival].Relations[Human]);
		}

		[Fact]
		public void BreakingPeaceNeedsMinimumTreatyAndSufficientStrength()
		{
			Assert.Equal(0, DiplomacyRules.WeighBreakingPeace(3, 100, 50, false));
			Assert.Equal(1, DiplomacyRules.WeighBreakingPeace(4, 100, 50, false));
			Assert.Equal(-1, DiplomacyRules.WeighBreakingPeace(10, 100, 150, false));
			Assert.Equal(1, DiplomacyRules.WeighBreakingPeace(10, 100, 150, true));
		}

		[Fact]
		public void AttitudeRewardsLongPeaceAndPunishesVendetta()
		{
			Player[] participants = CreateParticipants();
			MakePeace(participants);
			participants[Rival].MartialMight = 10;
			participants[Human].MartialMight = 10;

			Assert.Equal(0, DiplomacyRules.ComputeAttitude(participants, Rival, Human, 0));

			DiplomacyRules.SetPeaceUnits(participants, Rival, Human, DiplomacyRules.TrustedPeaceUnits);
			Assert.Equal(1, DiplomacyRules.ComputeAttitude(participants, Rival, Human, 0));

			DiplomacyRules.BeginVendetta(participants, Rival, Human, 12);
			Assert.Equal(-1, DiplomacyRules.ComputeAttitude(participants, Rival, Human, 0));
			Assert.Equal("Vengeful", DiplomacyRules.AttitudeName(-1, true));
			Assert.Equal("Friendly", DiplomacyRules.AttitudeName(2, false));
			Assert.Equal("Hostile", DiplomacyRules.AttitudeName(-3, false));
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

		private static void MakePeace(Player[] participants)
		{
			participants[Human].Relations[Rival] |= DiplomacyRules.PeaceRelation;
			participants[Rival].Relations[Human] |= DiplomacyRules.PeaceRelation;
		}
	}
}
