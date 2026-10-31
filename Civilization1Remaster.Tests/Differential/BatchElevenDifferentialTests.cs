using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The strategic locations of the AI, the repaint of a map tile, the spaceship to view, the replay line, the government
	/// palette and the loss of a spaceship against their original code
	/// </summary>
	public class BatchElevenDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The segment 0x3772: the spaceship parts, layouts and pictures</summary>
		private static readonly (ushort, ushort, int)[] Spaceships = [(0x3772, 0, 0x8000)];

		[Fact]
		public void ReimposeStrategicLocationsMatchesOriginalCode()
		{
			Check(3, 100, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(1, 8);
				bool unused = chance.Next(4) == 0;
				var sites = Enumerable.Range(0, 16).Select(_ => unused || chance.Next(3) == 0 ?
					((sbyte)-1, 0, 0, (byte)0) : ((sbyte)chance.Next(-2, 6), chance.Next(80), chance.Next(1, 49), (byte)chance.Next(4))).ToArray();
				int sent = Calls(pair.Rewritten.Engine, pair, "SendUnitToStrategicLocation");

				return new Scenario($"ReimposeStrategicLocations({participantIdx}), none in use {unused}",
					game =>
					{
						for (int siteIdx = 0; siteIdx < sites.Length; siteIdx++)
						{
							var site = game.World.Participants[participantIdx].TacticalSites[siteIdx];
							(site.Live, int horizLoc, int vertLoc, site.Strategy) = sites[siteIdx];
							site.Place = new System.Drawing.Point(horizLoc, vertLoc);
						}
					},
					game => new ComputerPlayerReference(game).ReimposeStrategicLocations(participantIdx),
					game => game.Bot.ReimposeStrategicLocations(participantIdx),
					Register.Kept, game => Calls(game, pair, "SendUnitToStrategicLocation") > sent ? "units sent" : "none sent");
			}, output, ["units sent", "none sent"]);
		}

		[Fact]
		public void RepaintMapTileMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				(int horizLoc, int vertLoc) = RandomTile(engine, chance);
				UnitMemory troops = new UnitMemory(engine.Processor);

				// Mostly the tile of a unit, seen or not by the human
				if (chance.Next(4) != 0 && RandomUnit(engine, chance) is (short ownerIdx, short troopIdx))
				{
					(horizLoc, vertLoc) = (troops.Pull(ownerIdx, troopIdx, UnitMemory.Horiz), troops.Pull(ownerIdx, troopIdx, UnitMemory.Vert));
				}

				ushort hidden = (ushort)(chance.Next(5) == 0 ? 1 : 0);
				ushort allShown = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				byte flip = (byte)(1 << chance.Next(8));
				int drawn = Calls(pair.Rewritten.Engine, pair, "RepaintUnitTile");
				int units = Calls(pair.Rewritten.Engine, pair, "ObtainTileImprovements");

				return new Scenario($"RepaintMapTile({horizLoc}, {vertLoc}), hidden {hidden}, all shown {allShown}, seen bits ^ {flip}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xdcfc, hidden);
						game.Processor.EmitGlobalWord(0xd806, allShown);

						UnitMemory memory = new UnitMemory(game.Processor);

						for (int participantIdx = 0; participantIdx < 8; participantIdx++)
						{
							for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
							{
								if (memory.Exists(participantIdx, troopIdx) && memory.StandsAt(participantIdx, troopIdx, horizLoc, vertLoc))
								{
									memory.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
										(byte)(memory.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) ^ flip));
								}
							}
						}
					},
					game => new MapServiceReference(game).RepaintMapTile(horizLoc, vertLoc),
					game => game.MapOps.RepaintMapTile(horizLoc, vertLoc),
					Register.Kept,
					game => hidden != 0 ? "units hidden" : Calls(game, pair, "RepaintUnitTile") > drawn ? "unit drawn" :
						game.Processor.PullGlobalWord(0xd806) == 0 && game.Processor.PullGlobalWord(0xd20a) != game.World.PersonParticipantIdx &&
						Calls(game, pair, "ObtainTileImprovements") == units ? "unit not seen" : "no unit or city");
			}, output, ["units hidden", "unit drawn", "unit not seen", "no unit or city"]);
		}

		[Fact]
		public void ChooseNationToViewMatchesOriginalCode()
		{
			// Enter picks the default, the arrows another nation, Escape cancels
			int[] keys = { 0x0d, 0x1b, 0x0d, 0x5000, 0x0d, 0x1b, 0x5000, 0x5000, 0x0d, 0x1b, 0x1b, 0x4800, 0x0d, 0x1b };

			Check(3, 30, (chance, pair) =>
			{
				short markers = (short)(chance.Next(5) == 0 ? chance.Next(0x200) : chance.Next(0x10000));
				int shown = Calls(pair.Rewritten.Engine, pair, "RenderSpaceship");

				return new Scenario($"ChooseNationToView(), spaceships 0x{(ushort)markers:x4}",
					game => game.World.StarshipMarkers = markers,
					game => new SpaceshipBayReference(game).ChooseNationToView(),
					game => game.RocketScreen.ChooseNationToView(),
					Register.Kept,
					game => (markers & 0xfe00) == 0 ? "no spaceship" : Calls(game, pair, "RenderSpaceship") > shown ? "shown" : "cancelled",
					Spaceships);
			}, output, ["no spaceship", "shown", "cancelled"], keys);
		}

		[Fact]
		public void RenderReplayPreambleMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				ushort mode = (ushort)chance.Next(5);
				ushort eventIdx = (ushort)chance.Next(8);
				string line = new[] { "Rome founded.", "The Babylonians discover Bronze Working, and a very long line follows it here.", "", "x" }[chance.Next(4)];

				return new Scenario($"RenderReplayPreamble(), mode {mode}, player {eventIdx}, \"{line}\"",
					game =>
					{
						game.Processor.EmitGlobalWord(0x680a, mode);
						game.Processor.EmitGlobalWord(0x6810, eventIdx);
						game.Libc.CopyText(0xba06, line);

						if (mode == 3)
							game.Processor.EmitGlobalWord(0x680c, (ushort)game.Libc.OpenHandle("replay.txt", 0x8302, 0x80));
					},
					game =>
					{
						new ReplayViewerReference(game).DrawReplayPreamble();
						ShowReplayFile(game, mode);
					},
					game =>
					{
						game.Replayer.DrawReplayPreamble();
						ShowReplayFile(game, mode);
					},
					Register.Kept,
					game => mode == 4 ? "nothing" : mode == 3 ? "written" : eventIdx == game.World.PersonParticipantIdx ? "human's line" : "line");
			}, output, ["nothing", "written", "human's line", "line"]);
		}

		/// <summary>
		/// The snapshot does not see files: the replay file is closed and its content copied into the data segment
		/// (0xbc00: length, then the bytes)
		/// </summary>
		private static void ShowReplayFile(GameEngine game, ushort mode)
		{
			if (mode != 3)
				return;

			game.Libc.CloseHandle((short)game.Processor.PullGlobalWord(0x680c));

			byte[] content = File.ReadAllBytes(Path.Combine(game.Processor.StandardFolder, "replay.txt"));
			game.Processor.EmitGlobalWord(0xbc00, (ushort)content.Length);

			for (int idx = 0; idx < content.Length && idx < 0x100; idx++)
				game.Processor.EmitGlobalByte((ushort)(0xbc02 + idx), content[idx]);
		}

		[Fact]
		public void DisplayCivilizationSceneryMatchesOriginalCode()
		{
			Check(10, 60, (chance, pair) =>
			{
				ushort shown = (ushort)(chance.Next(4) == 0 ? 0 : 1);
				short regime = (short)chance.Next(6);
				bool flip = chance.Next(2) == 0;

				return new Scenario($"DisplayCivilizationScenery(), shown {shown}, government {regime}, technology 20 changed {flip}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd762, shown);
						game.World.Participants[game.World.PersonParticipantIdx].RegimeKind = regime;

						if (flip)
							game.World.Participants[game.World.PersonParticipantIdx].FoundScienceMarkers[1] ^= 0x10;
					},
					game => new WorldEventsReference(game).ShowCivilizationScenery(),
					game => game.TurnHooks.ShowCivilizationScenery(),
					Register.Kept,
					game => shown == 0 ? "not shown" : regime == 3 ? "government 3" :
						game.Advisors.PlayerKnowsTechnology(game.World.PersonParticipantIdx, 0x14) != 0 ? "with technology 20" : "without technology 20",
					[(0x35cf, 0, 0x200)]);
			}, output, ["not shown", "government 3", "with technology 20", "without technology 20"]);
		}

		[Fact]
		public void EliminateSpaceshipMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				short markers = (short)chance.Next(0x10000);

				return new Scenario($"EliminateSpaceship({participantIdx}), spaceships 0x{(ushort)markers:x4}",
					game => game.World.StarshipMarkers = markers,
					game => new CityCaptureReference(game).DestroySpaceship(participantIdx),
					game => game.Conquest.DestroySpaceship(participantIdx),
					Register.Kept,
					game => (markers & (1 << (participantIdx + 8))) == 0 ? "not launched" :
						(markers & (1 << participantIdx)) != 0 ? "launched and built" : "launched",
					Spaceships);
			}, output, ["not launched", "launched and built", "launched"]);
		}
	}
}
