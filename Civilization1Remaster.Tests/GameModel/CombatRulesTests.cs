using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class CombatResolverTests
	{
		[Theory]
		[InlineData(false, false, 3)]
		[InlineData(true, false, 4)]
		[InlineData(false, true, 5)]
		[InlineData(true, true, 5)]
		public void TopHitPoints(bool seasoned, bool ace, int expected) => Assert.Equal(expected, CombatResolver.TopHitPoints(seasoned, ace));

		[Fact]
		public void AttackerWinningEveryRoundBeatsAllDefenderChances()
		{
			CombatResolver.Battle fight = CombatResolver.Clash(4, 2, 3, 3, 2, total => 0);

			Assert.True(fight.AssailantVictories);
			Assert.Equal(3, fight.AssailantStrikeMarks);
			Assert.Equal(0, fight.GuardianStrikeMarks);
			Assert.Equal(6, fight.Phases.Count);
			Assert.All(fight.Phases, Assert.True);
		}

		[Fact]
		public void AttackerLosingEveryRoundLeavesDefenderUntouched()
		{
			CombatResolver.Battle fight = CombatResolver.Clash(4, 2, 3, 4, 1, total => total - 1);

			Assert.False(fight.AssailantVictories);
			Assert.Equal(0, fight.AssailantStrikeMarks);
			Assert.Equal(4, fight.GuardianStrikeMarks);
			Assert.Equal(3, fight.Phases.Count);
		}

		[Fact]
		public void ClashAsksForChanceOutOfAssaultPlusGuard()
		{
			List<int> asked = new List<int>();
			CombatResolver.Clash(5, 7, 1, 1, 1, total => { asked.Add(total); return 0; });

			Assert.Equal(new[] { 12 }, asked);
		}

		[Fact]
		public void AlternatingRoundsFollowTheStrikeMarks()
		{
			int round = 0;
			// win, lose, win, lose, ... with 2 marks each: W L W -> defender dead after its 2nd loss
			CombatResolver.Battle fight = CombatResolver.Clash(1, 1, 2, 2, 1, total => round++ % 2 == 0 ? 0 : 1);

			Assert.True(fight.AssailantVictories);
			Assert.Equal(1, fight.AssailantStrikeMarks);
			Assert.Equal(new[] { true, false, true }, fight.Phases);
		}

		[Fact]
		public void NoAssaultMeansNoFight()
		{
			CombatResolver.Battle fight = CombatResolver.Clash(0, 3, 3, 3, 1, total => throw new InvalidOperationException());

			Assert.False(fight.AssailantVictories);
			Assert.Empty(fight.Phases);
			Assert.Equal(3, fight.GuardianStrikeMarks);
		}

		[Fact]
		public void NoGuardMeansAttackerWinsWithoutFight()
		{
			CombatResolver.Battle fight = CombatResolver.Clash(3, 0, 3, 3, 1, total => throw new InvalidOperationException());

			Assert.True(fight.AssailantVictories);
			Assert.Empty(fight.Phases);
			Assert.Equal(3, fight.AssailantStrikeMarks);
		}

		[Fact]
		public void ClashClampsMarksAndChancesToOne()
		{
			CombatResolver.Battle fight = CombatResolver.Clash(1, 1, 0, -2, 0, total => 0);

			Assert.Equal(1, fight.AssailantBeginStrikeMarks);
			Assert.Equal(1, fight.GuardianBeginStrikeMarks);
			Assert.Equal(1, fight.GuardianChances);
			Assert.Single(fight.Phases);
		}

		[Fact]
		public void AssailantPrevailsUsesRegularMarks()
		{
			Assert.True(CombatResolver.AssailantPrevails(1, 1, total => 0));
			Assert.False(CombatResolver.AssailantPrevails(1, 1, total => 1));
		}

		[Theory]
		[InlineData(0, 5, 0.0)]
		[InlineData(5, 0, 1.0)]
		[InlineData(3, 3, 0.5)]
		public void OddsEdgeCases(int assault, int guard, double expected) =>
			Assert.Equal(expected, CombatResolver.AssailantPrevailOdds(assault, guard), 12);

		[Fact]
		public void OneRoundOddsAreTheRoundOdds()
		{
			Assert.Equal(0.75, CombatResolver.AssailantPrevailOdds(3, 1, 1, 1), 12);
		}

		[Theory]
		[InlineData(1, 2, 3, 3)]
		[InlineData(5, 3, 4, 3)]
		[InlineData(2, 9, 5, 2)]
		public void OddsOfBothSidesAddUpToOne(int assault, int guard, int assailantMarks, int guardianMarks)
		{
			double forward = CombatResolver.AssailantPrevailOdds(assault, guard, assailantMarks, guardianMarks);
			double backward = CombatResolver.AssailantPrevailOdds(guard, assault, guardianMarks, assailantMarks);

			Assert.Equal(1.0, forward + backward, 12);
		}

		[Fact]
		public void OddsMatchSimulatedClashes()
		{
			Random random = new Random(12345);
			int victories = 0;
			const int Fights = 20000;

			for (int idx = 0; idx < Fights; idx++)
				victories += CombatResolver.Clash(3, 2, 3, 3, 1, total => random.Next(total)).AssailantVictories ? 1 : 0;

			Assert.Equal(CombatResolver.AssailantPrevailOdds(3, 2), (double)victories / Fights, 1);
		}
	}

	public class CombatRulesTests
	{
		[Theory]
		[InlineData(3, 1, 12, 0)]
		[InlineData(5, 2, 13, 1)]
		[InlineData(0, 4, 0, 0)]
		public void EstimateOdds(int assault, int guard, int chances, int remainder) =>
			Assert.Equal((chances, remainder), CombatRules.EstimateOdds(assault, guard));

		[Theory]
		[InlineData(0, 0, 2, 2, 2, false, false, 64)]
		[InlineData(0, 0, 2, 2, 2, true, false, 96)]
		[InlineData(0, 0, 2, 2, 2, true, true, 144)]
		[InlineData(0, 1, 3, 2, 4, true, false, 48)]
		[InlineData(0, 2, 3, 10, 4, false, false, 48)]
		[InlineData(0, 0, 3, 10, 4, false, false, -1)]
		[InlineData(0, 1, 3, 10, 4, false, false, -1)]
		public void GuardianScore(int kind, int landGroup, int guard, int terrain, int terrainFactor, bool fortified, bool veteran, int expected) =>
			Assert.Equal(expected, CombatRules.ComputeGuardianScore(kind, landGroup, guard, terrain, terrainFactor, fortified, veteran));

		[Theory]
		[InlineData(8, 0, 1, 1, 3, 8)]
		[InlineData(8, 0, 1, 1, 0, 2)]
		[InlineData(8, 0, 2, 1, 3, 4)]
		[InlineData(8, 2, 1, 1, 1, 4)]
		[InlineData(8, 2, 1, 1, 2, 8)]
		[InlineData(8, 1, 2, 1, 0, 16)]
		[InlineData(8, 1, 2, 1, 1, 8)]
		[InlineData(8, 2, 3, 1, 0, 8)]
		public void AdjustAttackerStrength(int assault, int attacker, int defender, int human, int tier, int expected) =>
			Assert.Equal(expected, CombatRules.AdjustAttackerStrength(assault, attacker, defender, human, tier));

		[Theory]
		[InlineData(9, 0, 0)]
		[InlineData(9, 1, 3)]
		[InlineData(9, 2, 6)]
		[InlineData(9, 3, 9)]
		[InlineData(9, 7, 9)]
		public void AdjustForRemainingSteps(int assault, int steps, int expected) =>
			Assert.Equal(expected, CombatRules.AdjustForRemainingSteps(assault, steps));

		[Fact]
		public void SdiIsBitZero()
		{
			Assert.True(CombatRules.IsSDIProtected(1));
			Assert.False(CombatRules.IsSDIProtected(0xfffe));
		}

		[Theory]
		[InlineData(1, 1)]
		[InlineData(7, 4)]
		[InlineData(8, 4)]
		public void NuclearCityPopulation(int size, int expected) => Assert.Equal(expected, CombatRules.CalculateNuclearCityPopulation(size));

		[Theory]
		[InlineData(10, 1, false)]
		[InlineData(2, 0, false)]
		[InlineData(2, 1, true)]
		public void Fallout(int terrain, int dice, bool expected) => Assert.Equal(expected, CombatRules.ShouldAttachFallout(terrain, dice));
	}
}
