using Civ_1_5_Remastered.Core.Shared.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class LeaderPortraitsTests
	{
		[Theory]
		[InlineData(4, true, LeaderPortraits.CasimirSuit)]
		[InlineData(5, true, LeaderPortraits.CasimirSuit)]
		[InlineData(4, false, LeaderPortraits.Casimir)]
		[InlineData(0, true, LeaderPortraits.CasimirFieldUniform)]
		[InlineData(1, true, LeaderPortraits.CasimirFieldUniform)]
		[InlineData(1, false, LeaderPortraits.Casimir)]
		[InlineData(2, true, LeaderPortraits.CasimirParadeUniform)]
		[InlineData(2, false, LeaderPortraits.Casimir)]
		[InlineData(3, true, LeaderPortraits.CasimirTunic)]
		[InlineData(3, false, LeaderPortraits.CasimirTunic)]
		public void CasimirDressesForTheModernScenery(int regime, bool modern, int expected)
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
		public void TheModernDressesHaveTheirFaceAndStandIn()
		{
			Assert.True(LeaderPortraits.IsAdded(LeaderPortraits.CasimirSuit));
			Assert.Equal(((short)123, (short)24), LeaderPortraits.Face(LeaderPortraits.CasimirSuit));
			Assert.Equal(2, LeaderPortraits.StandIn(LeaderPortraits.CasimirSuit));
			Assert.Equal(5, LeaderPortraits.Disk(LeaderPortraits.CasimirSuit));
			Assert.True(LeaderPortraits.IsAdded(LeaderPortraits.CasimirFieldUniform));
			Assert.Equal(((short)123, (short)28), LeaderPortraits.Face(LeaderPortraits.CasimirFieldUniform));
			Assert.Equal(2, LeaderPortraits.StandIn(LeaderPortraits.CasimirFieldUniform));
			Assert.True(LeaderPortraits.IsAdded(LeaderPortraits.CasimirTunic));
			Assert.Equal(((short)123, (short)28), LeaderPortraits.Face(LeaderPortraits.CasimirTunic));
			Assert.Equal(2, LeaderPortraits.StandIn(LeaderPortraits.CasimirTunic));
			Assert.True(LeaderPortraits.IsAdded(LeaderPortraits.CasimirParadeUniform));
			Assert.Equal(((short)123, (short)27), LeaderPortraits.Face(LeaderPortraits.CasimirParadeUniform));
			Assert.Equal(2, LeaderPortraits.StandIn(LeaderPortraits.CasimirParadeUniform));
		}
	}
}
