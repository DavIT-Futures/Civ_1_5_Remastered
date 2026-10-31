using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// A whole turn (HandleGameEnd) against its original code, with the years and turns the golden master does not reach:
	/// the 50th turns (status and ranking into the replay), the steps of the years, the year 0, the retirement and the end
	/// year, conquest, the pollution flag, the palace option and a human player about to go broke
	/// </summary>
	public class BatchSixtyFourDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void HandleGameEndMatchesOriginalCode()
		{
			// Chosen scenarios (year, turn, difficulty, conquest, 0x3484, prefs, gold); the human player's turn ends with the keys
			// of the golden master (Enter alone never ends it: both versions stop at the key limit, which looks like a match)
			(short Season, short Round, short Hardness, ushort Conquest, ushort PollutionFlag, short Prefs, short Gold)[] chosen =
			[
				(999, 49, 2, 0, 0xfffd, 0x80, 1500),
				(1849, 120, 1, 1, 0, 0, 500),
				(-20, 149, 0, 0, 0xfffe, 0x80, 20),
				(0x820 - 0x14 - 1, 230, 1, 0, 0, 0x80, 30),
				(0x834 - 0x28 - 1, 299, 2, 0, 0, 0, 900),
			];
			int scenarioIdx = 0;

			Check(3, chosen.Length, (chance, pair) =>
			{
				(short season, short round, short hardness, ushort conquest, ushort pollutionFlag, short prefs, short gold) = chosen[scenarioIdx++];

				return new Scenario($"HandleGameEnd(), year {season}, turn {round}, difficulty {hardness}, conquest {conquest}, " +
					$"0x3484 0x{pollutionFlag:x4}, prefs 0x{prefs:x2}, gold {gold}",
					game =>
					{
						game.World.HardnessTier = hardness;
						game.World.Season = season;
						game.World.RoundTally = round;
						game.World.SessionPrefMarkers = (short)((game.World.SessionPrefMarkers & ~0x80) | prefs);
						game.World.Participants[game.World.PersonParticipantIdx].Gold = gold;
						game.World.ParticipantMarkers |= (short)(1 << game.World.PersonParticipantIdx);
						game.Processor.EmitGlobalWord(0xb884, conquest);
						game.Processor.EmitGlobalWord(0x3484, pollutionFlag);
						game.World.DirtyCellTally = 3;
						game.Processor.EmitGlobalWord(0xdc48, 0);
						game.Processor.EmitGlobalWord(0xdf60, 0);
					},
					game => new WorldEventsReference(game).HandleGameEnd(),
					game => game.TurnHooks.HandleGameEnd(),
					Register.Kept,
					game => game.World.RoundTally == round ? "turn not finished" :
						game.Processor.Storage.PullUInt16(0x3b01, 0xdc48) != 0 ? "game over" : "turn");
			}, output, ["turn", "game over"]);
		}
	}
}
