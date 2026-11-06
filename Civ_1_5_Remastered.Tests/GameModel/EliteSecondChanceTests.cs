using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class EliteSecondChanceTests
	{
		[Fact]
		public void EliteAttackerRerollsLostRoundsUpToLimit()
		{
			Queue<int> rolls = new Queue<int>(new[] { 9, 0, 9, 0, 9, 9, 9 });

			CombatResolver.Battle fight = CombatResolver.Clash(5, 5, 1, 3, 1, total => rolls.Dequeue(), CombatResolver.AceSecondChances, 0);

			Assert.Equal(2, fight.AssailantSecondChancesUsed);
			Assert.Equal(new[] { true, true, false }, fight.Phases);
			Assert.False(fight.AssailantVictories);
			Assert.Equal(1, fight.GuardianStrikeMarks);
		}

		[Fact]
		public void EliteGuardianRerollsRoundsItLost()
		{
			Queue<int> rolls = new Queue<int>(new[] { 0, 9, 9, 9, 9 });

			CombatResolver.Battle fight = CombatResolver.Clash(5, 5, 3, 1, 1, total => rolls.Dequeue(), 0, CombatResolver.AceSecondChances);

			Assert.Equal(1, fight.GuardianSecondChancesUsed);
			Assert.False(fight.AssailantVictories);
			Assert.Equal(new[] { false, false, false }, fight.Phases);
		}

		[Fact]
		public void RegularUnitsGetNoSecondChance()
		{
			Assert.Equal(0, CombatResolver.SecondChances(false));
			Assert.Equal(2, CombatResolver.SecondChances(true));

			CombatResolver.Battle fight = CombatResolver.Clash(5, 5, 1, 1, 1, total => 9);

			Assert.Equal(0, fight.AssailantSecondChancesUsed);
			Assert.False(fight.AssailantVictories);
		}

		[Theory]
		[InlineData(false, false, "", "")]
		[InlineData(true, false, "Veteran", " (V)")]
		[InlineData(true, true, "Elite", " (E)")]
		public void RankNames(bool seasoned, bool elite, string title, string suffix)
		{
			Assert.Equal(title, UnitRankRules.Title(seasoned, elite));
			Assert.Equal(suffix, UnitRankRules.Suffix(seasoned, elite));
		}
	}
}
