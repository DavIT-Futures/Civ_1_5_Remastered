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
			CombatResolver.Clash(5, 7, 1, 1, 1, total => { asked.Add(total);
			return 0; });

			Assert.Equal(new[] { 12 }, asked);
		}

		[Fact]
		public void AlternatingRoundsFollowTheStrikeMarks()
		{
			int round = 0;
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
	}

	public class CombatRulesTests
	{
		[Theory]
		[InlineData(0, 2, 2, 2, false, false, 64)]
		[InlineData(0, 2, 2, 2, true, false, 96)]
		[InlineData(0, 2, 2, 2, true, true, 144)]
		[InlineData(1, 3, 2, 4, true, false, 48)]
		[InlineData(2, 3, 10, 4, false, false, 48)]
		[InlineData(0, 3, 10, 4, false, false, -1)]
		[InlineData(1, 3, 10, 4, false, false, -1)]
		public void GuardianScore(int landGroup, int guard, int terrain, int terrainFactor, bool fortified, bool veteran, int expected) =>
			Assert.Equal(expected, CombatRules.ComputeGuardianScore(landGroup, guard, terrain, terrainFactor, fortified, veteran));
	}
}
