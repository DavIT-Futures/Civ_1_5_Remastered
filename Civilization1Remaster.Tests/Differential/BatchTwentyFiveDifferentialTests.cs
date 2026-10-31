using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The city status and attitude reports, the opening news and a unit's picture against their original code
	/// </summary>
	public class BatchTwentyFiveDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>
		/// A report scenario: the cities of a player get random production, sometimes all cities become the player's (more
		/// than one page)
		/// </summary>
		private static Scenario ReportScenario(Random chance, DifferentialPair pair, string name, Action<GameEngine, short> original,
			Action<GameEngine, short> rewritten, Func<int, sbyte[], string> branch)
		{
			GameEngine engine = pair.Original.Engine;
			short participantIdx = (short)chance.Next(8);
			bool all = chance.Next(4) == 0;
			short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff &&
				(all || engine.World.Towns[idx].ParticipantIdx == participantIdx)).ToArray();
			sbyte[] making = towns.Select(_ => (sbyte)(chance.Next(3) switch { 0 => chance.Next(-24, 0), 1 => chance.Next(0, 28), _ => chance.Next(0x40, 0x80) })).ToArray();

			return new Scenario($"{name}({participantIdx}), {towns.Length} cities, all cities {all}",
				game =>
				{
					for (int idx = 0; idx < towns.Length; idx++)
					{
						game.World.Towns[towns[idx]].ParticipantIdx = participantIdx;
						game.World.Towns[towns[idx]].PresentMakingIdx = making[idx];
					}
				},
				game => original(game, participantIdx),
				game => rewritten(game, participantIdx),
				Register.Kept,
				game => branch(towns.Length, making));
		}

		[Fact]
		public void DisplayCityStatusReportMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) => ReportScenario(chance, pair, "DisplayCityStatusReport",
				(game, participantIdx) => new AdvisorScreensReference(game).ShowCityStatusReport(participantIdx),
				(game, participantIdx) => game.Briefings.ShowCityStatusReport(participantIdx),
				(tally, making) => (tally > 20 ? "pages, " : "") + (making.Any(idx => idx < 0) && making.Any(idx => idx >= 0 && idx < 0x40) &&
					making.Any(idx => idx >= 0x40) ? "all productions" : "some productions")),
				output, ["pages, all productions", "all productions", "some productions"]);
		}

		[Fact]
		public void DisplayAttitudeReportMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) => ReportScenario(chance, pair, "DisplayAttitudeReport",
				(game, participantIdx) => new AdvisorScreensReference(game).ShowAttitudeReport(participantIdx),
				(game, participantIdx) => game.Briefings.ShowAttitudeReport(participantIdx),
				(tally, making) => tally > 16 ? "pages" : tally > 0 ? "one page" : "no cities"),
				output, ["pages", "one page", "no cities"]);
		}

		[Fact]
		public void DisplayTerrainTutorialMatchesOriginalCode()
		{
			Check(3, 6, (chance, pair) =>
			{
				ushort shadow = (ushort)chance.Next(2);

				return new Scenario($"DisplayTerrainTutorial(), 0xd762 {shadow}",
					game => game.Processor.EmitGlobalWord(0xd762, shadow),
					game => new IntroSequenceReference(game).ShowTerrainTutorial(),
					game => game.Prologue.ShowTerrainTutorial(),
					Register.Kept,
					game => shadow != 0 ? "shadow" : "plain");
			}, output, ["shadow", "plain"]);
		}

		[Fact]
		public void RenderUnitSpriteMatchesOriginalCode()
		{
			Check(3, 400, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				byte status = (byte)(chance.Next(3) == 0 ? 0 : 1 << chance.Next(8) | (chance.Next(3) == 0 ? 1 << chance.Next(8) : 0));
				byte kind = (byte)(chance.Next(4) == 0 ? new[] { 0, 14, 15, 16, 17 }[chance.Next(5)] : chance.Next(28));
				byte travel = (byte)(chance.Next(2) == 0 ? 0xff : chance.Next(80));
				int horizLoc = chance.Next(0, 300), vertLoc = chance.Next(0, 180);

				return new Scenario($"RenderUnitSprite({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc}), status 0x{status:x2}, kind {kind}, go to {travel}",
					game =>
					{
						UnitMemory memory = new UnitMemory(game.Processor);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Status, status);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Kind, kind);
						memory.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, travel);
					},
					game => new MapServiceReference(game).DrawUnitSprite(participantIdx, troopIdx, horizLoc, vertLoc),
					game => game.MapOps.DrawUnitSprite(participantIdx, troopIdx, horizLoc, vertLoc),
					// (no AX: the 0x600 x player the original left, which every caller overwrites, is gone in Uderzenie 6/6)
					Register.Kept,
					game => (status & 8) != 0 ? "fortified" : (status & 0xc2) != 0 ? "letter" : (status & 1) != 0 ? "sleeping" : "plain");
			}, output, ["fortified", "letter", "sleeping", "plain"]);
		}
	}
}
