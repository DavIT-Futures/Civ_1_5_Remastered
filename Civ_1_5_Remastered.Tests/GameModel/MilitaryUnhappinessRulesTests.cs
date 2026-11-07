using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class MilitaryUnhappinessRulesTests
	{
		[Theory]
		[InlineData(2, 1, true)]
		[InlineData(MilitaryUnhappinessRules.Trireme, 1, false)]
		[InlineData(0x1b, 0, false)]
		[InlineData(0, 0, false)]
		public void TriremeIsSparedLikeTheTransport(int kind, int assault, bool unhappy) =>
			Assert.Equal(unhappy, MilitaryUnhappinessRules.CausesUnhappinessAway(kind, assault));
	}
}
