using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CityHappinessRulesTests
	{
		[Theory]
		[InlineData(2, 3, true)]   // unhappy > happy -> Civil Disorder!
		[InlineData(3, 2, false)]  // happy > unhappy -> Calm
		[InlineData(2, 2, false)]  // equal -> No disorder
		[InlineData(0, 1, true)]   // 1 unhappy, 0 happy -> Disorder
		public void EvaluatesCivilDisorder_TriggersWhenUnhappyStrictlyExceedsHappy(int happy, int unhappy, bool expectedDisorder)
		{
			Assert.Equal(expectedDisorder, CityHappinessRules.EvaluatesCivilDisorder(happy, unhappy));
		}

		[Fact]
		public void EvaluatesCelebration_RequiresZeroUnhappyAndSufficientHappy()
		{
			// City size 5: required happy = (5 + 1) / 2 = 3.
			// 3 happy, 0 unhappy, no anarchy -> Celebrates!
			Assert.True(CityHappinessRules.EvaluatesCelebration(citySize: 5, happyCitizens: 3, unhappyCitizens: 0, isAnarchy: false));

			// 2 happy, 0 unhappy -> Not enough happy
			Assert.False(CityHappinessRules.EvaluatesCelebration(citySize: 5, happyCitizens: 2, unhappyCitizens: 0, isAnarchy: false));

			// 3 happy, 1 unhappy -> Discontented citizen prevents celebration!
			Assert.False(CityHappinessRules.EvaluatesCelebration(citySize: 5, happyCitizens: 3, unhappyCitizens: 1, isAnarchy: false));

			// Under Anarchy -> Never celebrates
			Assert.False(CityHappinessRules.EvaluatesCelebration(citySize: 5, happyCitizens: 4, unhappyCitizens: 0, isAnarchy: true));

			// Small village (size 2) -> Too small to celebrate
			Assert.False(CityHappinessRules.EvaluatesCelebration(citySize: 2, happyCitizens: 2, unhappyCitizens: 0, isAnarchy: false));
		}

		[Fact]
		public void CalculateCitizenMoods_PacificationCascade()
		{
			// City size 6, no specialists, base unhappy 3 (so 3 unhappy, 3 content).
			// Pacified by temple = 2.
			// 2 unhappy pacified -> 1 unhappy, 5 content, 0 happy.
			var (h1, c1, u1) = CityHappinessRules.CalculateCitizenMoods(citySize: 6, specialistCount: 0, baseUnhappy: 3, pacifiedCitizens: 2);
			Assert.Equal(0, h1);
			Assert.Equal(5, c1);
			Assert.Equal(1, u1);

			// Pacified by temple + colosseum + luxuries = 5.
			// 3 unhappy converted to content (leaving 0 unhappy).
			// remaining 2 pacification converts 2 content to happy!
			// Result: 2 happy, 4 content, 0 unhappy.
			var (h2, c2, u2) = CityHappinessRules.CalculateCitizenMoods(citySize: 6, specialistCount: 0, baseUnhappy: 3, pacifiedCitizens: 5);
			Assert.Equal(2, h2);
			Assert.Equal(4, c2);
			Assert.Equal(0, u2);
		}
	}
}
