using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The choices and the start of a new game (ConfigureFreshGame) against its original code: every menu is answered with
	/// "down" a number of times and Enter, so line k is chosen for the difficulty, the number of civilizations (rivals 6 - k)
	/// and the tribe (k > rivals: the second leader of slot k - rivals). A new game expects a fresh map: the city bits of the
	/// saved game's map are cleared (else a settler next to a forgotten city makes ConstructUnitsFromCityList throw in both
	/// versions), and every scenario gets a fresh pair of games.
	/// </summary>
	public class BatchFiftyEightDifferentialTests(ITestOutputHelper output)
	{
		[Theory]
		[InlineData(2, 0, 0x4e, "first leader, easiest")]
		[InlineData(3, 1, 0x41, "first leader, easiest")]
		[InlineData(5, 2, 0x4e, "first leader")]
		[InlineData(8, 3, 0x41, "first leader")]
		[InlineData(3, 4, 0x4e, "second leader")]
		[InlineData(5, 4, 0x41, "second leader")]
		[InlineData(3, -1, 0x4e, "random tribe")]
		public void ConfigureFreshGameMatchesOriginalCode(int bay, int downs, int sound, string branch)
		{
			// -1: Escape for the tribe (the first key goes before the first menu): a random slot with its own names
			int[] script = downs < 0 ? [0x0d, 0x0d, 0x0d, 0x1b] : Enumerable.Repeat(0x5000, downs).Append(0x0d).ToArray();
			int foundedMessages = 0;

			Check(bay, 1, (chance, pair) =>
				(foundedMessages = Calls(pair.Rewritten.Engine, pair, "DisplayCityFoundedMessage")) < 0 ? null : new Scenario($"ConfigureFreshGame(), line {downs}, sound driver 0x{sound:x2}",
					game =>
					{
						game.World.RoundTally = 0;
						game.Processor.EmitGlobalByte(0x1a30, (byte)sound);
						game.Processor.EmitGlobalWord(0x1a3c, 0);

						for (int horizLoc = 0; horizLoc < 80; horizLoc++)
							for (int vertLoc = 0; vertLoc < 50; vertLoc++)
								game.MapOps.Chart.ClearImprovements(1, horizLoc, vertLoc);
					},
					game => new StartGameMenuReference(game).ConfigureFreshGame(),
					game => game.NewGameWizard.ConfigureFreshGame(),
					Register.Kept | Register.Acc,
					game => Calls(game, pair, "DisplayCityFoundedMessage") > foundedMessages ? "random tribe" : (game.World.Participants[game.World.PersonParticipantIdx].ChiefVisuals >= 8 ? "second leader" : "first leader") +
						(game.World.HardnessTier == 0 ? ", easiest" : "")),
				output, [branch], script);
		}
	}
}
