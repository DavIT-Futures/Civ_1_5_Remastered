using Civ_1_5_Remastered.Core.Shared.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class LeaderPortraitsTests
	{
		[Theory]
		[InlineData(0, false, LeaderPortraits.CasimirWarlord)]
		[InlineData(1, false, LeaderPortraits.CasimirWarlord)]
		[InlineData(1, true, LeaderPortraits.CasimirFieldUniform)]
		[InlineData(2, false, LeaderPortraits.Casimir)]
		[InlineData(2, true, LeaderPortraits.CasimirParadeUniform)]
		[InlineData(3, false, LeaderPortraits.CasimirTunic)]
		[InlineData(3, true, LeaderPortraits.CasimirTunic)]
		[InlineData(4, false, LeaderPortraits.CasimirSenator)]
		[InlineData(5, false, LeaderPortraits.CasimirSenator)]
		[InlineData(4, true, LeaderPortraits.CasimirSuit)]
		[InlineData(5, true, LeaderPortraits.CasimirSuit)]
		public void CasimirDressesForTheScenery(int regime, bool modern, int expected)
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
		public void EveryAddedPortraitHasItsFaceAndStandIn()
		{
			for (int portrait = LeaderPortraits.Casimir; portrait <= LeaderPortraits.CasimirSenator; portrait++)
			{
				Assert.True(LeaderPortraits.IsAdded(portrait));
				Assert.Equal((short)123, LeaderPortraits.Face(portrait).Horizontal);
				Assert.Equal(portrait == LeaderPortraits.Tokugawa ? 10 : 2, LeaderPortraits.StandIn(portrait));
				Assert.Equal(5, LeaderPortraits.Disk(portrait));
			}

			Assert.False(LeaderPortraits.IsAdded(13));
		}
	}
}
