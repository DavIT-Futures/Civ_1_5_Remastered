using System;
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
	/// The transports sent to a city, the replay map, the report of the units lost and the screen tutorial against their original code
	/// </summary>
	public class BatchTwentyOneDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void PostCityDefendersMatchesOriginalCode()
		{
			Check(3, 150, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				(int townHoriz, int townVert) = (engine.World.Towns[townIdx].Place.X, engine.World.Towns[townIdx].Place.Y);
				sbyte[] ferries = Enumerable.Range(0, 28).Select(kind => (sbyte)kind)
					.Where(kind => UnitKindTable.Field(engine.Processor, kind, UnitKindTable.FerryRoom) != 0).ToArray();

				// A transport of an AI player planted on the ocean near the city (loaded or not)
				short ownerIdx = (short)chance.Next(1, 8);
				short troopIdx = (short)chance.Next(UnitMemory.TroopTally);
				sbyte kind = ferries[chance.Next(ferries.Length)];
				bool loaded = chance.Next(4) != 0;
				bool contact = chance.Next(4) == 0;
				int reach = chance.Next(1, 14);
				(int horizLoc, int vertLoc) = (-1, -1);

				for (int attempt = 0; attempt < 50 && horizLoc < 0; attempt++)
				{
					int tryHoriz = (townHoriz + chance.Next(-reach, reach + 1) + MapRules.Width) % MapRules.Width;
					int tryVert = Math.Clamp(townVert + chance.Next(-reach, reach + 1), 1, MapRules.Height - 2);

					if (engine.MapOps.Chart.Terrain(tryHoriz, tryVert) == MapRules.OceanTerrain)
						(horizLoc, vertLoc) = (tryHoriz, tryVert);
				}

				bool plant = horizLoc >= 0 && chance.Next(3) != 0;

				return new Scenario($"PostCityDefenders({townIdx}), transport {kind} of {ownerIdx} at {horizLoc}, {vertLoc} planted {plant}, " +
					$"loaded {loaded}, relation bit 2 {contact}",
					game =>
					{
						if (!plant)
							return;

						UnitMemory troops = new UnitMemory(game.Processor);
						troops.Assign(ownerIdx, troopIdx, UnitMemory.Kind, (byte)kind);
						troops.Assign(ownerIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
						troops.Assign(ownerIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
						troops.Assign(ownerIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
						troops.AssignFollowing(ownerIdx, troopIdx, (byte)(loaded ? troopIdx : 0xff));

						ref ushort relation = ref game.World.Participants[ownerIdx].Relations[game.World.PersonParticipantIdx];
						relation = (ushort)(contact ? relation | 2 : relation & ~2);
					},
					game => new AIPlayerReference(game).PostCityDefenders(townIdx),
					game => game.Bot.PostCityDefenders(townIdx),
					Register.Kept,
					game => !plant ? "as loaded" : ownerIdx == game.World.PersonParticipantIdx ? "human" : contact ? "contact" : !loaded ? "empty" :
						new UnitMemory(game.Processor).Pull(ownerIdx, troopIdx, UnitMemory.TravelHoriz) == (byte)townHoriz ? "sent" : "too far");
			}, output, ["as loaded", "contact", "empty", "sent", "too far"]);
		}

		[Fact]
		public void RenderReplayMapTileMatchesOriginalCode()
		{
			Check(3, 150, (chance, pair) =>
			{
				// Mostly a few cities (sometimes none: then the nearest entry is what was left on the stack)
				int tally = chance.Next(5) == 0 ? 0 : chance.Next(1, 12);
				ushort[] firsts = Enumerable.Range(0, 0x80).Select(idx => (ushort)(idx < tally ? chance.Next(8) : 0xffff)).ToArray();
				short[] horizs = Enumerable.Range(0, 0x80).Select(_ => (short)chance.Next(80)).ToArray();
				short[] verts = Enumerable.Range(0, 0x80).Select(_ => (short)chance.Next(2, 48)).ToArray();
				(int horizLoc, int vertLoc) = tally > 0 && chance.Next(2) == 0 ? (horizs[0], verts[0]) : RandomTile(pair.Original.Engine, chance);
				ushort mode = (ushort)(chance.Next(6) == 0 ? 4 : chance.Next(4));

				return new Scenario($"RenderReplayMapTile({horizLoc}, {vertLoc}), {tally} cities, 0x680a {mode}",
					game =>
					{
						for (int idx = 0; idx < 0x80; idx++)
						{
							game.Processor.EmitUInt16(0x3725, (ushort)(idx << 1), firsts[idx]);
							game.Processor.EmitUInt16(0x3725, (ushort)((idx << 1) + 0x100), (ushort)horizs[idx]);
							game.Processor.EmitUInt16(0x3725, (ushort)((idx << 1) + 0x200), (ushort)verts[idx]);
						}

						game.Processor.EmitGlobalWord(0x680a, mode);
					},
					game => new GameReplayReference(game).RenderReplayMapTile(horizLoc, vertLoc),
					game => game.Replayer.RenderReplayMapTile(horizLoc, vertLoc),
					Register.Kept,
					game => mode == 4 ? "off" : tally == 0 ? "no cities" : "cities",
					[ScreenAndMapDifferentialTests.ReplayCities]);
			}, output, ["off", "no cities", "cities"]);
		}

		[Fact]
		public void RenderPlayerComparisonChartMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				// Losses of at most 20 unit types: with more the rows go below the screen and both codes throw
				short participantIdx = (short)chance.Next(8);
				int[] lostKinds = Enumerable.Range(0, 28).OrderBy(_ => chance.Next()).Take(chance.Next(0, 21)).ToArray();
				short[][] losses = Enumerable.Range(0, 8).Select(_ => Enumerable.Range(0, 28)
					.Select(kind => (short)(lostKinds.Contains(kind) && chance.Next(3) == 0 ? chance.Next(-2, 400) : 0)).ToArray()).ToArray();

				return new Scenario($"RenderPlayerComparisonChart({participantIdx})",
					game =>
					{
						for (int playerIdx = 0; playerIdx < 8; playerIdx++)
							losses[playerIdx].CopyTo(game.World.Participants[playerIdx].MissingTroops, 0);
					},
					game => new AdvisorReportsReference(game).RenderPlayerComparisonChart(participantIdx),
					game => game.Briefings.RenderPlayerComparisonChart(participantIdx),
					Register.Kept,
					game => participantIdx == 0 ? "player 0" : "player in the columns");
			}, output, ["player 0", "player in the columns"]);
		}

		[Fact]
		public void DisplayInterfaceTutorialMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				ushort horizBegin = (ushort)chance.Next(80);
				ushort vertBegin = (ushort)chance.Next(0, 38);
				int centreVert = vertBegin + 6;
				bool special = false, hut = false;

				for (int heading = 1; heading <= 8; heading++)
				{
					int horizLoc = (engine.MapOps.Chart.DirectionHoriz(heading) + horizBegin + 7 + MapRules.Width) % MapRules.Width;
					int vertLoc = engine.MapOps.Chart.DirectionVert(heading) + centreVert;

					special |= MapRules.HasSpecialResource(horizLoc, vertLoc, engine.World.ChanceRoot);
					hut |= engine.MapOps.Chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain &&
						MapRules.IsHutPlace(horizLoc, vertLoc, engine.World.ChanceRoot);
				}

				return new Scenario($"DisplayInterfaceTutorial(), view at {horizBegin}, {vertBegin}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd4cc, horizBegin);
						game.Processor.EmitGlobalWord(0xd75e, vertBegin);
					},
					game => new MainIntroReference(game).DisplayInterfaceTutorial(),
					game => game.Prologue.DisplayInterfaceTutorial(),
					Register.Kept,
					game => (special ? "special" : "") + (hut ? " hut place" : ""));
			}, output, ["special", " hut place", ""]);
		}
	}
}
