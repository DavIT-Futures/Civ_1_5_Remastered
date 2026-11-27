using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SiegePlannerTests
	{
		[Fact]
		public void FarAttackersAreNotPartOfTheSiege()
		{
			Assert.Equal(SiegeOrder.None, SiegePlanner.Decide(View(distance: SiegePlanner.GatherRange + 1)));
		}

		[Fact]
		public void AttackersGatherAroundTheCityBeforeAttacking()
		{
			Assert.Equal(SiegeOrder.Advance, SiegePlanner.Decide(View(distance: 4)));
			Assert.Equal(SiegeOrder.Hold, SiegePlanner.Decide(View(ring: 2, ringKills: 1.6)));
		}

		[Fact]
		public void EnoughAttackersAroundTheCityAttackTogether()
		{
			Assert.Equal(SiegeOrder.Assault, SiegePlanner.Decide(View(ring: 4, ringKills: 2.5)));

			// Once the assault started this turn, every attacker next to the city joins it, even when the first ones died.
			Assert.Equal(SiegeOrder.Assault, SiegePlanner.Decide(View(ring: 1, ringKills: 0.3, underway: true)));
			Assert.Equal(SiegeOrder.Advance, SiegePlanner.Decide(View(distance: 3, underway: true)));
		}

		[Fact]
		public void ManyAttackersAreNotEnoughWhenTheyCannotWin()
		{
			Assert.Equal(SiegeOrder.Hold, SiegePlanner.Decide(View(ring: 5, ringKills: 1.0, forceKills: 3)));
		}

		[Fact]
		public void AnEmptyCityIsTakenAtOnce()
		{
			Assert.Equal(SiegeOrder.Assault, SiegePlanner.Decide(View(defenders: 0, ring: 1)));
			Assert.Equal(SiegeOrder.Advance, SiegePlanner.Decide(View(distance: 3, defenders: 0)));
		}

		[Fact]
		public void HopelessSiegesPillageTheLand()
		{
			Assert.Equal(SiegeOrder.Raid, SiegePlanner.Decide(View(distance: 3, defenders: 4, forceKills: 1.5)));
		}

		[Fact]
		public void AfterWaitingTheRingAttacksWhenStrongEnoughAndRaidsOtherwise()
		{
			Assert.Equal(SiegeOrder.Assault, SiegePlanner.Decide(View(ring: 2, ringKills: 1.4, waiting: SiegePlanner.PatienceRounds)));
			Assert.Equal(SiegeOrder.Raid, SiegePlanner.Decide(View(ring: 2, ringKills: 0.9, waiting: SiegePlanner.PatienceRounds)));
		}

		[Fact]
		public void TheRingWaitsForTwoMoreAttackersThanDefendersWithinItsTiles()
		{
			Assert.Equal(3, SiegePlanner.WantedRing(0, 8));
			Assert.Equal(3, SiegePlanner.WantedRing(1, 8));
			Assert.Equal(6, SiegePlanner.WantedRing(4, 8));
			Assert.Equal(8, SiegePlanner.WantedRing(12, 8));
			Assert.Equal(2, SiegePlanner.WantedRing(4, 2));
		}

		[Fact]
		public void WinChanceFollowsTheRoundsOfABattle()
		{
			Assert.Equal(0.5, SiegePlanner.WinChance(8, 8, 3, 3), 6);
			Assert.Equal(0.5, SiegePlanner.WinChance(8, 8, 1, 1), 6);
			Assert.Equal(0.75, SiegePlanner.WinChance(24, 8, 1, 1), 6);

			// Several rounds favour the stronger side more than one roll would.
			Assert.True(SiegePlanner.WinChance(24, 8, 3, 3) > 0.85);
			Assert.True(SiegePlanner.WinChance(8, 24, 3, 3) < 0.15);
			Assert.True(SiegePlanner.WinChance(8, 8, 3, 1) > SiegePlanner.WinChance(8, 8, 1, 3));
			Assert.Equal(0, SiegePlanner.WinChance(0, 8, 3, 3));
			Assert.Equal(1, SiegePlanner.WinChance(8, 0, 3, 3));
		}

		private static SiegeView View(int distance = 1, int defenders = 2, int ring = 1, double ringKills = 0.8, double forceKills = 4,
			int ringTiles = 8, int waiting = 0, bool underway = false) => new SiegeView
			{
				Distance = distance,
				Defenders = defenders,
				RingAttackers = ring,
				RingKills = ringKills,
				ForceKills = forceKills,
				RingTiles = ringTiles,
				RoundsWaiting = waiting,
				AssaultUnderway = underway,
			};
	}
}
