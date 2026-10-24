using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The news of a technology a computer player researched (source 0), in a loaded game: with ClassicTechnologyNews off
	/// (the game) it ends with the technology, on (the tests) it names "the Barbarians" as the translated code did. The
	/// autosaves have no contact between the human and the computer players yet, so the test makes one.
	/// </summary>
	public class TechnologyNewsInGameTests
	{
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ResearchOfComputerPlayerIsNews(bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));

			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);

			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicTechnologyNews = classic;

			// A computer player the human player has contact with (bit 0x40: the human player hears its news)
			short human = engine.World.PersonParticipantIdx;
			short player = (short)Enumerable.Range(1, 7).First(idx => idx != human && engine.World.Participants[idx].TownTally > 0);
			engine.World.Participants[human].Relations[player] |= 0x40;

			// A technology the computer player does not know
			short technology = (short)Enumerable.Range(0, 0x47).First(idx =>
				engine.Advisors.PlayerKnowsTechnology(player, (short)idx) == 0);

			// The text of each dialog when it waits for a key
			List<string> dialogs = [];
			game.Keyboard.KeyTyping = () => dialogs.Add(ReadText(engine, 0xba06));

			engine.Advisors.EarnTechnology(player, technology, 0);

			// "Babylonians develop Mass Transit!" (with " from the Barbarians" before the "!" in the classic news)
			string news = dialogs.First(text => text.Contains(" develop"));
			Assert.Equal(classic, news.Contains("from\nthe Barbarians"));
		}

		private static string ReadText(GameEngine engine, ushort address)
		{
			StringBuilder text = new StringBuilder();

			for (ushort at = address; engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, at) != 0; at++)
				text.Append((char)engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, at));

			return text.ToString();
		}
	}
}
