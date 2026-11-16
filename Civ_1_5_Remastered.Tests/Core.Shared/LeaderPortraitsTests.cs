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

		[Theory]
		[InlineData(LeaderPortraits.PolishLeader, true, false, false)]
		[InlineData(LeaderPortraits.PolishLeader, false, false, true)]
		[InlineData(LeaderPortraits.JapaneseLeader, true, false, true)]
		[InlineData(LeaderPortraits.JapaneseLeader, false, false, false)]
		[InlineData(LeaderPortraits.JapaneseLeader, true, true, false)]
		public void PolandAndJapanAreRebornAsEachOther(int leader, bool identity, bool human, bool reborn)
		{
			Assert.Equal(reborn, LeaderPortraits.IsReborn(leader, identity, human));
		}

		[Theory]
		[InlineData(1, false, 0)]
		[InlineData(1, true, 7)]
		[InlineData(3, false, 2)]
		[InlineData(3, true, 9)]
		[InlineData(7, true, 13)]
		public void CityBlocksAreTheSlotsOwn(int slot, bool reborn, int block)
		{
			// Slot 1 begins with the Romans' names (block 0); reborn, it takes the Russians' (block 7), and so on.
			Assert.Equal(block, LeaderPortraits.CityBlock(slot, reborn));
		}

		[Fact]
		public void TheComputerPlaysPolandInSlotOneUnlessTheHumanPlayerIsThere()
		{
			// The human player in any other slot: Poland takes slot 1, without drawing.
			for (int personSlot = 2; personSlot <= 7; personSlot++)
			{
				Assert.Equal(1, LeaderPortraits.PolandSlot(false, personSlot, 7, _ => throw new InvalidOperationException()));
			}

			// The human player plays Poland: the computer does not.
			Assert.Equal(-1, LeaderPortraits.PolandSlot(true, 1, 7, _ => 0));
			Assert.Equal(-1, LeaderPortraits.PolandSlot(true, 4, 7, _ => 0));
		}

		[Fact]
		public void WithTheHumanPlayerInSlotOnePolandTakesAnotherSlotOfTheGame()
		{
			HashSet<int> slots = new HashSet<int>();

			for (int drawn = 0; drawn < 6; drawn++)
			{
				int slot = LeaderPortraits.PolandSlot(false, 1, 7, choices => Math.Min(drawn, choices - 1));
				Assert.InRange(slot, 2, 7);
				slots.Add(slot);
			}

			// Every other slot of a game of seven can be drawn.
			Assert.Equal(6, slots.Count);

			// In a game of three only slots 2 and 3; alone in the game, none.
			Random random = new Random(3);

			for (int game = 0; game < 50; game++)
			{
				Assert.InRange(LeaderPortraits.PolandSlot(false, 1, 3, random.Next), 2, 3);
			}

			Assert.Equal(-1, LeaderPortraits.PolandSlot(false, 1, 1, _ => 0));
		}
	}
}
