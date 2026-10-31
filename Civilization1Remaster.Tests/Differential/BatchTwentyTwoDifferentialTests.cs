using System;
using System.IO;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The military report, the map written into the replay file, the greeting of the king and the end of a civilization
	/// against their original code
	/// </summary>
	public class BatchTwentyTwoDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderUnitCountsChartMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				// At most 17 rows of units and 20 of losses: with more the rows go below the screen and both codes throw
				short participantIdx = (short)chance.Next(8);
				int[] kinds = Enumerable.Range(0, 28).OrderBy(_ => chance.Next()).Take(chance.Next(1, 18)).ToArray();
				int filled = chance.Next(3);
				short[] active = Enumerable.Range(0, 28).Select(kind => (short)(filled != 2 && kinds.Contains(kind) && chance.Next(2) == 0 ? chance.Next(-1, 60) : 0)).ToArray();
				short[] making = Enumerable.Range(0, 28).Select(kind => (short)(filled != 1 && kinds.Contains(kind) && chance.Next(2) == 0 ? chance.Next(-1, 9) : 0)).ToArray();
				int[] lostKinds = Enumerable.Range(0, 28).OrderBy(_ => chance.Next()).Take(chance.Next(0, 21)).ToArray();
				short[][] losses = Enumerable.Range(0, 8).Select(_ => Enumerable.Range(0, 28)
					.Select(kind => (short)(lostKinds.Contains(kind) && chance.Next(3) == 0 ? chance.Next(1, 40) : 0)).ToArray()).ToArray();

				return new Scenario($"RenderUnitCountsChart({participantIdx}), kinds {string.Join(" ", kinds)}",
					game =>
					{
						active.CopyTo(game.World.Participants[participantIdx].LiveTroops, 0);
						making.CopyTo(game.World.Participants[participantIdx].TroopsInsideMaking, 0);

						for (int playerIdx = 0; playerIdx < 8; playerIdx++)
							losses[playerIdx].CopyTo(game.World.Participants[playerIdx].MissingTroops, 0);
					},
					game => new AdvisorScreensReference(game).RenderUnitCountsChart(participantIdx),
					game => game.Briefings.RenderUnitCountsChart(participantIdx),
					Register.Kept,
					game => Enumerable.Range(0, 28).Any(kind => active[kind] > 0 && making[kind] > 0) ? "active and in production" :
						active.Any(count => count > 0) ? "active" : making.Any(count => count > 0) ? "in production" : "none");
			}, output, ["active and in production", "active", "in production"]);
		}

		[Fact]
		public void DumpReplayCitiesMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				// Name places moved to land tiles, some of them near the right edge (the name is cut at the end of the line)
				GameEngine engine = pair.Original.Engine;
				int[] moved = Enumerable.Range(0, 0x100).OrderBy(_ => chance.Next()).Take(chance.Next(0, 6)).ToArray();
				(int, int)[] places = moved.Select(_ =>
				{
					while (true)
					{
						(int horizLoc, int vertLoc) = RandomTile(engine, chance, true);

						if (chance.Next(2) == 0)
							horizLoc = MapRules.Width - 1 - chance.Next(12);

						if (engine.MapOps.Chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain)
							return (horizLoc, vertLoc);
					}
				}).ToArray();

				return new Scenario($"DumpReplayCities(), {moved.Length} name places moved",
					game =>
					{
						for (int idx = 0; idx < moved.Length; idx++)
							game.World.TownPlaces[moved[idx]] = new System.Drawing.Point(places[idx].Item1, places[idx].Item2);

						game.Processor.EmitGlobalWord(0x680c, (ushort)game.Libc.OpenHandle("replay.txt", 0x8302, 0x80));
					},
					game =>
					{
						new ReplayViewerReference(game).DumpReplayCities();
						HashReplayFile(game);
					},
					game =>
					{
						game.Replayer.DumpReplayCities();
						HashReplayFile(game);
					},
					Register.Kept,
					game => places.Any(place => place.Item1 >= 68) ? "name cut" : moved.Length > 0 ? "moved" : "as loaded");
			}, output, ["name cut", "moved", "as loaded"]);
		}

		/// <summary>The snapshot does not see files: the replay file is closed, its length and hash go into the data segment (0xbc00)</summary>
		private static void HashReplayFile(GameEngine game)
		{
			game.Libc.CloseHandle((short)game.Processor.PullGlobalWord(0x680c));

			byte[] content = File.ReadAllBytes(Path.Combine(game.Processor.StandardFolder, "replay.txt"));
			uint hash = GameSnapshot.Fnv(content);

			game.Processor.EmitGlobalWord(0xbc00, (ushort)content.Length);
			game.Processor.EmitGlobalWord(0xbc02, (ushort)hash);
			game.Processor.EmitGlobalWord(0xbc04, (ushort)(hash >> 16));
		}

		[Fact]
		public void ObtainKingSalutationMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				short regime = (short)chance.Next(6);
				bool friendly = chance.Next(2) == 0;
				short nuclear = (short)(chance.Next(3) == 0 ? chance.Next(1, 3) : 0);

				return new Scenario($"ObtainKingSalutation({participantIdx}), government {regime}, relation bit 8 {friendly}, nuclear {nuclear}",
					game =>
					{
						game.World.Participants[participantIdx].RegimeKind = regime;
						game.World.Participants[participantIdx].LiveTroops[25] = nuclear;

						ref ushort relation = ref game.World.Participants[participantIdx].Relations[game.World.PersonParticipantIdx];
						relation = (ushort)(friendly ? relation | 8 : relation & ~8);
					},
					game => new RulerAudienceReference(game).ObtainKingSalutation(participantIdx),
					game => game.Audience.ObtainKingSalutation(participantIdx),
					Register.Kept,
					game =>
					{
						string text = game.Processor.PullText(CPU.AsLinearAddress(game.Processor.SegData.Value16, 0xba06));
						string title = new[] { "exalted", "ruler", "wise", "tremble" }.First(text.Contains);

						return title + (text.Contains("NUCLEAR") ? ", nuclear" : "");
					});
			}, output, ["exalted", "ruler", "wise", "tremble", "exalted, nuclear"]);
		}

		[Fact]
		public void EliminateCivilizationMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short humanIdx = engine.World.PersonParticipantIdx;
				short participantIdx = (short)chance.Next(8);
				short destroyerIdx = (short)(chance.Next(3) == 0 ? humanIdx : chance.Next(8));
				bool razed = chance.Next(3) != 0;
				bool met = chance.Next(2) == 0;
				int[] towns = Enumerable.Range(0, 128).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff &&
					engine.World.Towns[idx].ParticipantIdx == participantIdx).ToArray();

				return new Scenario($"EliminateCivilization({participantIdx}, {destroyerIdx}), cities razed {razed}, relation bit 0x40 {met}",
					game =>
					{
						// The cities of the player taken away (only the marker, like an empty record)
						if (razed)
						{
							foreach (int townIdx in towns)
								game.World.Towns[townIdx].ConditionMarker = 0xff;
						}

						ref ushort relation = ref game.World.Participants[humanIdx].Relations[participantIdx];
						relation = (ushort)(met ? relation | 0x40 : relation & ~0x40);
					},
					game => new NewGameSetupReference(game).EliminateCivilization(participantIdx, destroyerIdx),
					game => game.NewGameWizard.EliminateCivilization(participantIdx, destroyerIdx),
					Register.Kept,
					game => participantIdx == 0 || participantIdx == humanIdx ? "kept" : !razed && towns.Length > 0 ? "has cities" :
						destroyerIdx == humanIdx ? "destroyed by the human player" : "destroyed");
			}, output, ["kept", "has cities", "destroyed by the human player", "destroyed"]);
		}
	}
}
