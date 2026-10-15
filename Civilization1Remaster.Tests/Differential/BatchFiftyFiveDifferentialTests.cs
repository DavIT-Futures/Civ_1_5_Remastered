using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The main menu after the title (a new game, a saved game, EARTH, a customized world, the Hall of Fame) against its
	/// original code; the new worlds are made with the story and the tribe is chosen with Enter
	/// </summary>
	public class BatchFiftyFiveDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>
		/// One choice on a fresh pair of games: the title menu comes once in a game, and world after world in one game the
		/// generator can hang in both versions (see the backlog, porcja 55). 4 = the Hall of Fame, then the menu again
		/// (Enter: a new game); -1 = the menu (Enter: a new game). The title music is at the start or past it (then the
		/// choice waits for a measure, 0x120 steps after 0x1495): the timer variables (0x42 - 0x5b, left out of the snapshot)
		/// show how long it waited.
		/// </summary>
		[Theory]
		[InlineData(0xffff, 3, 0, "choice 0")]
		[InlineData(0, 2, 0x1495 + 0x100, "choice 0")]
		[InlineData(0, 3, 0, "choice 0")]
		[InlineData(1, 3, 0x1495 + 0x2b0, "choice 1")]
		[InlineData(2, 3, 0, "choice 2")]
		[InlineData(2, 5, 0x1495 + 0x10, "choice 2")]
		[InlineData(3, 3, 0, "choice 3")]
		[InlineData(4, 5, 0, "choice 0")]
		[InlineData(5, 3, 0x1495 + 0x121, "choice 5")]
		public void TitleLogoAndMainChoicesMatchesOriginalCode(int choice, int bay, int songPosition, string branch)
		{
			Check(bay, 1, (chance, pair) =>
			{
				ushort colours = (ushort)chance.Next(2);

				return new Scenario($"TitleLogoAndMainChoices(), choice {(short)choice}, VGA {colours}, song at 0x{songPosition:x4}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x6b32, (ushort)choice);
						game.Processor.EmitGlobalWord(0xd762, colours);
						game.Processor.EmitGlobalWord(0x2f9c, 0);

						game.Audio.Prepare();

						for (int step = 0; step < songPosition; step++)
							game.Audio.PerformNextStep();
					},
					game => new GameStartupReference(game).TitleLogoAndMainChoices(),
					game => game.Startup.TitleLogoAndMainChoices(),
					Register.Kept,
					game => $"choice {(short)game.Processor.Storage.PullUInt16(0x3b01, 0x6b32)}",
					[(0x3b01, 0x42, 0x1a)]);
			}, output, [branch], [0x0d]);
		}
	}
}
