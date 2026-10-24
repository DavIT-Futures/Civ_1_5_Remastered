using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The strength of an own stack a computer unit weighs (AIPlayer.ScoreTile): divided by the tally + 1, or with
	/// ClassicStackStrength (the tests) by SI as the second tally leaves it (0x600 * player), as the translated code did.
	/// </summary>
	public class StackStrengthTests
	{
		[Fact]
		public void DivisorIsTheTallyPlusOne()
		{
			Assert.Equal(4, StackStrengthRules.Divisor(4, 0x600 * 3, classic: false));
			Assert.Equal(0x600 * 3, StackStrengthRules.Divisor(4, 0x600 * 3, classic: true));
		}

		/// <summary>
		/// The computer players move their units otherwise once they weigh their stacks: the same turns of the same game end
		/// elsewhere in at least one of the autosaves (a unit next to an own stack it sees is rare in the first turns)
		/// </summary>
		[Fact]
		public void ComputerPlayersWeighTheirStacks()
		{
			Assert.Contains(Enumerable.Range(1, 11), autosave => PlayTurns(autosave, classic: true) != PlayTurns(autosave, classic: false));
		}

		private static string PlayTurns(int autosave, bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(autosave, GoldenMasterTests.RandomSeed);

			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicStackStrength = classic;

			for (int turn = 0; turn < 5; turn++)
				game.PlayTurn();

			// Where the units of the computer players stand
			short human = engine.World.PersonParticipantIdx;
			return string.Join(";", Enumerable.Range(1, 7).Where(player => player != human).SelectMany(player =>
				Enumerable.Range(0, 0x80).Select(unit => Unit(engine, player, unit))));
		}

		private static string Unit(GameEngine engine, int player, int unit)
		{
			ushort address = (ushort)(0x81d4 + 0x600 * player + 0xc * unit);
			byte Pull(int field) => engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, (ushort)(address + field));

			return Pull(3) == 0xff ? "-" : $"{Pull(3)}@{Pull(1)},{Pull(2)}";
		}
	}
}
