using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CombatResolverTests
	{
		// Strengths as the game calculates them: unit strength * 8 (sea and air units ignore terrain)
		private const int Galley = 1 * 8;
		private const int Sloop = 2 * 8;
		private const int Armored = 4 * 8;
		private const int Dreadnought = 12 * 8;
		private const int SeasonedDreadnoughtGuard = Dreadnought + Dreadnought / 2;

		private const int Fights = 100000;

		/// <summary>
		/// The original Civilization 1 battle: one roll for each side, higher roll wins
		/// </summary>
		private static bool LegacyAttackerVictory(int assault, int guard, Random chance)
		{
			int guardThrow = chance.Next(guard);
			int assaultThrow = chance.Next(assault);

			return assaultThrow > guardThrow;
		}

		private static double SampledVictoryOdds(int assault, int guard, int root = 1)
		{
			Random chance = new Random(root);
			int victories = 0;

			for (int idx = 0; idx < Fights; idx++)
			{
				if (CombatResolver.AssailantPrevails(assault, guard, chance.Next))
					victories++;
			}

			return (double)victories / Fights;
		}

		private static double LegacyVictoryOdds(int assault, int guard, int root = 1)
		{
			Random chance = new Random(root);
			int victories = 0;

			for (int idx = 0; idx < Fights; idx++)
			{
				if (LegacyAttackerVictory(assault, guard, chance))
					victories++;
			}

			return (double)victories / Fights;
		}

		[Theory]
		[InlineData(Galley)]
		[InlineData(Sloop)]
		public void FeebleVesselSeldomDownsBattleship(int assault)
		{
			Assert.True(CombatResolver.AssailantPrevailOdds(assault, Dreadnought) < 0.025);
			Assert.True(SampledVictoryOdds(assault, Dreadnought) < 0.03);
		}

		[Fact]
		public void TriremeOftenDefeatedBattleshipInOriginalGame()
		{
			// Documents the bug that this resolver fixes: about 3.6% with the original single roll
			double initial = LegacyVictoryOdds(Galley, Dreadnought);

			Assert.InRange(initial, 0.03, 0.045);
			Assert.True(SampledVictoryOdds(Galley, Dreadnought) < initial / 5);
		}

		[Fact]
		public void BattleshipNearlyAlwaysBeatsTrireme()
		{
			Assert.True(CombatResolver.AssailantPrevailOdds(Dreadnought, Galley) > 0.99);
			Assert.True(SampledVictoryOdds(Dreadnought, Galley) > 0.99);
		}

		[Fact]
		public void IroncladSeldomBeatsVeteranBattleship()
		{
			Assert.True(CombatResolver.AssailantPrevailOdds(Armored, SeasonedDreadnoughtGuard) < 0.05);
		}

		[Theory]
		[InlineData(8)]
		[InlineData(32)]
		[InlineData(96)]
		public void IdenticalUnitsShareEvenOdds(int power)
		{
			Assert.Equal(0.5, CombatResolver.AssailantPrevailOdds(power, power), 6);
			Assert.InRange(SampledVictoryOdds(power, power), 0.49, 0.51);
		}

		[Fact]
		public void TightClashesRemainUnclear()
		{
			// 25% stronger attacker should be favored, but still lose sometimes
			double odds = CombatResolver.AssailantPrevailOdds(40, 32);

			Assert.InRange(odds, 0.6, 0.8);
		}

		[Fact]
		public void BetterAttackerSucceedsMoreFrequently()
		{
			double earlier = 0.0;

			for (int assault = 1; assault <= 200; assault++)
			{
				double odds = CombatResolver.AssailantPrevailOdds(assault, 50);

				Assert.True(odds > earlier, $"Win chance must grow with attack, failed at attack {assault}");
				earlier = odds;
			}
		}

		[Theory]
		[InlineData(Galley, Dreadnought)]
		[InlineData(Armored, Sloop)]
		[InlineData(40, 32)]
		[InlineData(16, 16)]
		public void SamplingAgreesWithExactOdds(int assault, int guard)
		{
			Assert.InRange(SampledVictoryOdds(assault, guard) - CombatResolver.AssailantPrevailOdds(assault, guard), -0.01, 0.01);
		}

		[Fact]
		public void NoAttackNeverSucceeds()
		{
			// Barbarians get attack 0 against the last city of a player
			Assert.False(CombatResolver.AssailantPrevails(0, 8, _ => 0));
			Assert.Equal(0.0, CombatResolver.AssailantPrevailOdds(0, 8));
		}

		[Fact]
		public void NoDefenseAlwaysFails()
		{
			Assert.True(CombatResolver.AssailantPrevails(8, 0, _ => 7));
			Assert.Equal(1.0, CombatResolver.AssailantPrevailOdds(8, 0));
		}

		[Fact]
		public void IdenticalRollsYieldIdenticalOutcome()
		{
			Random leading = new Random(42);
			Random other = new Random(42);

			for (int idx = 0; idx < 1000; idx++)
			{
				Assert.Equal(
					CombatResolver.AssailantPrevails(24, 32, leading.Next),
					CombatResolver.AssailantPrevails(24, 32, other.Next));
			}
		}

		[Fact]
		public void FightDemandsAtLeastHitpointPhases()
		{
			int phases = 0;

			// Attacker wins every round
			Assert.True(CombatResolver.AssailantPrevails(10, 10, peak => { phases++; return 0; }));
			Assert.Equal(CombatResolver.RegularStrikeMarks, phases);

			phases = 0;

			// Defender wins every round
			Assert.False(CombatResolver.AssailantPrevails(10, 10, peak => { phases++; return peak - 1; }));
			Assert.Equal(CombatResolver.RegularStrikeMarks, phases);
		}

		[Fact]
		public void SeasonedAndAceUnitsGetExtraHitpoints()
		{
			Assert.Equal(3, CombatResolver.TopHitPoints(false, false));
			Assert.Equal(4, CombatResolver.TopHitPoints(true, false));
			Assert.Equal(5, CombatResolver.TopHitPoints(true, true));
		}

		[Fact]
		public void FightReportsPhasesAndSurvivingHitpoints()
		{
			// Rounds: attacker, defender, attacker, attacker (random numbers below 10 are attacker's rounds)
			int[] figures = { 0, 19, 0, 0 };
			int following = 0;

			CombatResolver.Battle fight = CombatResolver.Clash(10, 10, 4, 3, 1, peak => figures[following++]);

			Assert.True(fight.AssailantVictories);
			Assert.Equal(new[] { true, false, true, true }, fight.Phases);
			Assert.Equal(3, fight.AssailantStrikeMarks);
			Assert.Equal(0, fight.GuardianStrikeMarks);
		}

		[Fact]
		public void InjuredUnitFailsMoreFrequently()
		{
			double unhurt = CombatResolver.AssailantPrevailOdds(32, 32, 3, 3);
			double hurt = CombatResolver.AssailantPrevailOdds(32, 32, 1, 3);

			Assert.Equal(0.5, unhurt, 6);
			Assert.Equal(0.125, hurt, 6);
		}

		[Fact]
		public void TwoLifeDefenderNeedsTwoDefeats()
		{
			// Barbarians against a city: the attacker wins every round, the defender is beaten twice
			CombatResolver.Battle fight = CombatResolver.Clash(10, 10, 3, 3, 2, peak => 0);

			Assert.True(fight.AssailantVictories);
			Assert.Equal(6, fight.Phases.Count);
		}

		[Theory]
		[InlineData(5, 2)]
		[InlineData(1, 3)]
		[InlineData(4, 4)]
		public void SamplingAgreesWithExactOddsAndHitpoints(int assailantStrikeMarks, int guardianStrikeMarks)
		{
			Random chance = new Random(7);
			int victories = 0;

			for (int idx = 0; idx < Fights; idx++)
			{
				if (CombatResolver.Clash(24, 32, assailantStrikeMarks, guardianStrikeMarks, 1, chance.Next).AssailantVictories)
					victories++;
			}

			Assert.InRange((double)victories / Fights - CombatResolver.AssailantPrevailOdds(24, 32, assailantStrikeMarks, guardianStrikeMarks), -0.01, 0.01);
		}
	}
}
