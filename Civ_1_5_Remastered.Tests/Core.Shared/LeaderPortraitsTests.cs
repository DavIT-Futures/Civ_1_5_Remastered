using Civ_1_5_Remastered.Core.Shared.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class LeaderPortraitsTests
	{
		[Theory]
		[InlineData(4, true, LeaderPortraits.CasimirSuit)]
		[InlineData(5, true, LeaderPortraits.CasimirSuit)]
		[InlineData(4, false, LeaderPortraits.Casimir)]
		[InlineData(1, true, LeaderPortraits.Casimir)]
		[InlineData(2, true, LeaderPortraits.Casimir)]
		[InlineData(3, true, LeaderPortraits.Casimir)]
		public void CasimirWearsTheSuitInTheModernRepublic(int regime, bool modern, int expected)
		{
			Assert.Equal(expected, LeaderPortraits.ForScenery(LeaderPortraits.Casimir, regime, modern));
		}

		[Fact]
		public void OtherLeadersKeepTheirPortrait()
		{
			Assert.Equal(LeaderPortraits.Tokugawa, LeaderPortraits.ForScenery(LeaderPortraits.Tokugawa, 4, true));
			Assert.Equal(10, LeaderPortraits.ForScenery(10, 5, true));
		}

		[Fact]
		public void TheSuitHasItsFaceAndStandIn()
		{
			Assert.True(LeaderPortraits.IsAdded(LeaderPortraits.CasimirSuit));
			Assert.Equal(((short)123, (short)24), LeaderPortraits.Face(LeaderPortraits.CasimirSuit));
			Assert.Equal(2, LeaderPortraits.StandIn(LeaderPortraits.CasimirSuit));
			Assert.Equal(5, LeaderPortraits.Disk(LeaderPortraits.CasimirSuit));
		}
	}
}
