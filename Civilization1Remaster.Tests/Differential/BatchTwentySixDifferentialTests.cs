using System;
using System.Drawing;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The science report, the destruction of a city, the tile hints and the rivers of a new map against their original code
	/// </summary>
	public class BatchTwentySixDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayScienceReportMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				bool allKnown = chance.Next(4) == 0;
				short knownTally = (short)chance.Next(0, 72);
				short tier = (short)chance.Next(5);
				ushort extra = (ushort)chance.Next(0, 5);
				short season = (short)(chance.Next(2) == 0 ? -1000 : 1500);
				short headway = (short)chance.Next(0, 2000);
				short inquiry = (short)(chance.Next(3) == 0 ? -1 : chance.Next(72));

				return new Scenario($"DisplayScienceReport({participantIdx}), known {knownTally}, all {allKnown}, difficulty {tier}, 0xd2de {extra}, " +
					$"year {season}, progress {headway}, research {inquiry}",
					game =>
					{
						Game.State.Player player = game.World.Participants[participantIdx];
						player.FoundScienceTally = knownTally;
						player.InquiryHeadway = headway;
						game.World.Participants[game.World.PersonParticipantIdx].PresentInquiryIdx = inquiry;
						game.World.HardnessTier = tier;
						game.World.Season = season;
						game.Processor.EmitGlobalWord(0xd2de, extra);

						if (allKnown)
						{
							for (int idx = 0; idx < player.FoundScienceMarkers.Length; idx++)
								player.FoundScienceMarkers[idx] = unchecked((ushort)0xffff);
						}
					},
					game => new AdvisorScreensReference(game).DisplayScienceReport(participantIdx),
					game => game.Briefings.DisplayScienceReport(participantIdx),
					Register.Kept,
					game => allKnown ? "page full" : inquiry == -1 ? "no research" : "research");
			}, output, ["page full", "no research", "research"]);
		}

		[Fact]
		public void FinalizeProductionMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();

				if (towns.Length == 0)
					return null;

				short townIdx = towns[chance.Next(towns.Length)];
				(ushort horizLoc, ushort vertLoc) = ((ushort)engine.World.Towns[townIdx].Place.X, (ushort)engine.World.Towns[townIdx].Place.Y);
				sbyte making = (sbyte)(chance.Next(2) == 0 ? chance.Next(-20, 0) : chance.Next(0, 28));
				bool wonder = chance.Next(3) == 0;
				int wonderIdx = chance.Next(1, 22);
				bool route = chance.Next(2) == 0;
				short otherIdx = towns[chance.Next(towns.Length)];

				return new Scenario($"FinalizeProduction({townIdx}, {horizLoc}, {vertLoc}), making {making}, wonder {wonder}, route from {otherIdx} {route}",
					game =>
					{
						game.World.Towns[townIdx].PresentMakingIdx = making;

						if (wonder)
							game.World.MarvelTownIdx[wonderIdx] = townIdx;

						if (route)
							game.World.Towns[otherIdx].CommerceTownIndices[0] = (sbyte)townIdx;
					},
					game => new ResearchOfficeReference(game).FinalizeProduction(townIdx, horizLoc, vertLoc),
					game => game.Advisors.FinalizeProduction(townIdx, horizLoc, vertLoc),
					Register.Kept,
					game => (making >= 0 ? "unit, " : "building, ") +
						((engine.World.ChartSight[horizLoc, vertLoc] & (1 << engine.World.PersonParticipantIdx)) != 0 ? "seen" : "unseen"));
			}, output, ["unit, seen", "unit, unseen", "building, seen", "building, unseen"]);
		}

		[Fact]
		public void DisplayTileInfoMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				UnitMemory troops = new UnitMemory(engine.Processor);
				(int horizLoc, int vertLoc) = (troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz), troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));

				// Mostly next to a city of the player (the hints for its tiles)
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff &&
					engine.World.Towns[idx].ParticipantIdx == participantIdx).ToArray();

				if (towns.Length > 0 && chance.Next(3) != 0)
				{
					short townIdx = towns[chance.Next(towns.Length)];
					int heading = chance.Next(1, 9);
					horizLoc = (engine.World.Towns[townIdx].Place.X + engine.MapOps.Chart.DirectionHoriz(heading) + MapRules.Width) % MapRules.Width;
					vertLoc = engine.World.Towns[townIdx].Place.Y + engine.MapOps.Chart.DirectionVert(heading);
				}

				ushort arg2 = (ushort)(chance.Next(10) == 0 ? 0x80 + chance.Next(4) : troopIdx);

				// Sometimes a high value of map layer 2 (at column + 80) under the unit, as at the start of a game: a city place
				bool valued = chance.Next(3) == 0;
				int popups = Calls(pair.Rewritten.Engine, pair, "DisplayHelpPopup");
				int focus = Calls(pair.Rewritten.Engine, pair, "FocusMapView");

				return new Scenario($"DisplayTileInfo({participantIdx}, {arg2}), unit at {horizLoc}, {vertLoc}",
					game =>
					{
						// The hints show only with the help option and before year 0
						game.World.SessionPrefMarkers |= 1;
						game.World.Season = -1000;

						UnitMemory memory = new UnitMemory(game.Processor);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);

						if (valued)
						{
							for (int heading = 1; heading <= 8; heading++)
								game.VideoCard.AssignDot(2, horizLoc + game.MapOps.Chart.DirectionHoriz(heading) + 80,
									vertLoc + game.MapOps.Chart.DirectionVert(heading), 10, 0);

							game.VideoCard.AssignDot(2, horizLoc + 80, vertLoc, 15, 0);
						}
					},
					game => new HelpPopupsReference(game).DisplayTileInfo(participantIdx, arg2),
					game => game.HelpSystem.DisplayTileInfo(participantIdx, arg2),
					Register.Kept,
					game => arg2 >= 0x80 ? "no unit" : Calls(game, pair, "FocusMapView") > focus ? "city place" :
						Calls(game, pair, "DisplayHelpPopup") > popups ? "improvement" : "nothing");
			}, output, ["no unit", "city place", "improvement", "nothing"], [0x0d]);
		}

		[Fact]
		public void ProduceElevationMatchesOriginalCode()
		{
			Check(3, 6, (chance, pair) =>
			{
				// A made map in screen 1: ocean (1) around an island of grassland (2) with hills (12) and some value 13
				int[] hills = Enumerable.Range(0, 40).Select(_ => chance.Next(80 * 50)).ToArray();
				int[] others = Enumerable.Range(0, 20).Select(_ => chance.Next(80 * 50)).ToArray();
				ushort wanted = (ushort)chance.Next(0, 3);
				ushort story = (ushort)(chance.Next(2) == 0 ? 0 : 2);

				return new Scenario($"ProduceElevation(), 0x7ef6 {wanted}, 0x3b62 {story}",
					game =>
					{
						game.VideoCard.FloodBox(1, new Rectangle(0, 0, 320, 200), 0, 0);
						game.VideoCard.FloodBox(1, new Rectangle(0, 0, 80, 50), 1, 0);
						game.VideoCard.FloodBox(1, new Rectangle(8, 6, 64, 38), 2, 0);

						foreach (int spot in hills)
							game.VideoCard.AssignDot(1, 8 + (spot % 64), 6 + ((spot / 80) % 38), 12, 0);

						foreach (int spot in others)
							game.VideoCard.AssignDot(1, 8 + (spot % 64), 6 + ((spot / 80) % 38), 13, 0);

						game.Processor.EmitGlobalWord(0x7ef6, wanted);
						game.Processor.EmitGlobalWord(0x7efa, 0);
						game.Processor.EmitGlobalWord(0x3b62, story);
					},
					game =>
					{
						new WorldBuilderReference(game).ProduceElevation();
						HideStoryClock(game);
					},
					game =>
					{
						game.WorldSetup.ProduceElevation();
						HideStoryClock(game);
					},
					Register.Kept,
					game => "rivers");
			}, output, ["rivers"]);
		}

		/// <summary>
		/// DisplayStoryIntro keeps the clock / 60 at 0x6800; the clock of the tests ticks every 10 000 memory accesses, so it
		/// depends on how many the code makes (the clock itself is left out of the snapshot too)
		/// </summary>
		private static void HideStoryClock(GameEngine game)
		{
			game.Processor.EmitGlobalWord(0x6800, 0);
		}
	}
}
