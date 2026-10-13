using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The nuclear explosion, the Save Game dialog, the Game menu and the turn of all cities against their original code</summary>
	public class BatchTwentyNineDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderExplosionMatchesOriginalCode()
		{
			ushort? diskPath = null;

			Check(3, 16, (chance, pair) =>
			{
				// The file name of disk 1 as loaded; a scenario without the disk points it to a missing file
				diskPath ??= pair.Original.Engine.Processor.PullGlobalWord(0x1ad0);
				(int horizLoc, int vertLoc) = RandomTile(pair.Original.Engine, chance);
				bool near = chance.Next(2) == 0;
				ushort viewLeft = (ushort)(near ? (horizLoc - chance.Next(1, 14) + 80) % 80 : chance.Next(0, 80));
				ushort viewTop = (ushort)(near ? Math.Max(0, vertLoc - chance.Next(1, 11)) : chance.Next(0, 40));
				bool missing = chance.Next(5) == 0;

				return new Scenario($"RenderExplosion({horizLoc}, {vertLoc}), view {viewLeft} {viewTop}, disk missing {missing}",
					game =>
					{
						game.Libc.CopyText(0xbb80, "NOFILE.XYZ");
						game.Processor.EmitGlobalWord(0x1ad0, missing ? (ushort)0xbb80 : diskPath.Value);
						game.Processor.EmitGlobalWord(0xd4cc, viewLeft);
						game.Processor.EmitGlobalWord(0xd75e, viewTop);
					},
					game => new DiplomatActionsReference(game).RenderExplosion((short)horizLoc, (short)vertLoc),
					game => game.Diplomats.RenderExplosion((short)horizLoc, (short)vertLoc),
					Register.Kept,
					game =>
					{
						if (missing)
							return "no disk";

						int left = (((horizLoc - viewLeft + 80) % 80) << 4) + 0x42;
						int top = ((vertLoc - viewTop) << 4) - 6;
						return left >= 80 && left <= 276 && top >= 8 && top <= 0x9c ? "on the map" : "moved to the map";
					});
			}, output, ["no disk", "on the map", "moved to the map"]);
		}

		/// <summary>Enter takes the drive and the slot the list starts at (0xe168), Escape leaves the drive or the list</summary>
		private static readonly int[] SaveKeys = { 0x0d, 0x0d, 0x0d, 0x0d, 0x1b, 0x0d, 0x0d, 0x1b };

		[Fact]
		public void DisplayStoreGameDialogMatchesOriginalCode()
		{
			Check(3, 14, (chance, pair) =>
			{
				ushort slot = chance.Next(2) == 0 ? (ushort)0xffff : (ushort)chance.Next(10);
				ushort last = (ushort)chance.Next(-3, 13);
				int saves = Calls(pair.Rewritten.Engine, pair, "StoreGame");
				int lists = Calls(pair.Rewritten.Engine, pair, "ImportGame");

				return new Scenario($"DisplayStoreGameDialog({slot}), last slot {(short)last}",
					game => game.Processor.EmitGlobalWord(0xe168, last),
					game =>
					{
						new GameLoadAndSaveReference(game).DisplayStoreGameDialog(slot);
						ShowSaves(game);
					},
					game =>
					{
						game.SaveLoad.DisplayStoreGameDialog(slot);
						ShowSaves(game);
					},
					Register.Kept,
					game => Calls(game, pair, "StoreGame") > saves ? (slot == 0xffff ? "saved, chosen" : "saved, given") :
						Calls(game, pair, "ImportGame") > lists ? "list left" : "no drive");
			}, output, ["saved, chosen", "saved, given", "list left", "no drive"], SaveKeys);
		}

		/// <summary>The length and a hash of all 10 save files go into the data segment</summary>
		private static void ShowSaves(GameEngine game)
		{
			for (int slot = 0; slot < 10; slot++)
				BatchTwelveDifferentialTests.ShowSavedFiles(game, $"civil{slot}.sve", (ushort)(0xbc00 + (slot * 18)));
		}

		/// <summary>Enter takes the line the menu starts at (0x2f9a, set by the scenario) and the options, Escape leaves them</summary>
		private static readonly int[] MenuKeys = { 0x0d, 0x0d, 0x0d, 0x1b, 0x0d, 0x1b };

		[Fact]
		public void DisplayGameMenuMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				bool replay = chance.Next(3) == 0;
				short markers = (short)((chance.Next(0x10000) & ~0x100) | (replay ? 0x100 : 0));
				ushort line = (ushort)(replay && chance.Next(2) == 0 ? 10 : chance.Next(10));
				short rounds = (short)(chance.Next(4) == 0 ? 0 : chance.Next(1, 400));
				int replays = Calls(pair.Rewritten.Engine, pair, "DisplayReplay");

				return new Scenario($"DisplayGameMenu(), line {line}, replay {replay}, turn {rounds}",
					game =>
					{
						game.World.StarshipMarkers = markers;
						game.World.RoundTally = rounds;
						game.Processor.EmitGlobalWord(0x2f9a, line);
						game.Processor.EmitGlobalWord(0xd4ca, 0);
						game.Processor.EmitGlobalWord(0xdc48, 0);
						game.Processor.EmitGlobalWord(0xb276, 0);
						game.SaveLoad.FetchBay = 0xffff;
					},
					game => new GameMenusReference(game).DisplayGameMenu(),
					game => game.MenuSystem.DisplayGameMenu(),
					Register.Kept,
					game => MenuBranch(game, pair, replays));
			}, output, ["tax", "luxuries", "find city", "options", "save", "load", "revolution", "retire", "quit", "replay", "nothing"], MenuKeys);
		}

		private static string MenuBranch(GameEngine game, DifferentialPair pair, int replays)
		{
			ushort command = game.Processor.PullGlobalWord(0xd4ca);
			ushort leave = game.Processor.PullGlobalWord(0xdc48);

			if (Calls(game, pair, "DisplayReplay") > replays)
				return "replay";

			if (command == 0 && game.Processor.PullText(CPU.AsLinearAddress(game.Processor.SegData.Value16, 0xba06)).StartsWith("Options:"))
				return "options";

			return command switch
			{
				0x3d => "tax",
				0x2d => "luxuries",
				0x3f => "find city",
				0x53 => "save",
				0xfffe => "revolution",
				0x1000 => leave == 2 ? "retire" : leave == 1 ? "quit" : "load",
				_ => "nothing",
			};
		}

		[Fact]
		public void HandleAllCitiesMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)(chance.Next(4) == 0 ? engine.World.PersonParticipantIdx : chance.Next(1, 8));
				short diagnostics = (short)(chance.Next(4) == 0 ? 0 : 4);
				short rounds = (short)chance.Next(0, 400);
				short gold = (short)(chance.Next(3) == 0 ? chance.Next(0, 100) : chance.Next(0, 1000));
				// Taxes and science of 7 together: more luxuries only with a city in disorder
				short science = (short)chance.Next(0, 8);
				short taxes = (short)(chance.Next(2) == 0 ? 7 - science : chance.Next(0, 11 - science));
				ushort lastTechnology = (ushort)(chance.Next(6) == 0 ? 0x8000 : 0);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx == participantIdx).ToArray();
				sbyte[] sizes = towns.Select(_ => (sbyte)(chance.Next(3) == 0 ? chance.Next(1, 6) : chance.Next(6, 20))).ToArray();
				int shown = Calls(pair.Rewritten.Engine, pair, "RepaintMainScreen");

				return new Scenario($"HandleAllCities({participantIdx}), diagnostics {diagnostics}, turn {rounds}, gold {gold}, science {science}, taxes {taxes}",
					game =>
					{
						Player participant = game.World.Participants[participantIdx];
						game.World.DiagMarkers = (short)((game.World.DiagMarkers & ~4) | diagnostics);
						game.World.RoundTally = rounds;
						participant.Gold = gold;
						participant.ResearchLevyRatio = science;
						participant.LevyRatio = taxes;
						participant.FoundScienceMarkers[3] = (ushort)((participant.FoundScienceMarkers[3] & 0x7fff) | lastTechnology);

						for (int idx = 0; idx < towns.Length; idx++)
							game.World.Towns[towns[idx]].TrueExtent = sizes[idx];
					},
					game => new ScienceAndAdvisorsReference(game).HandleAllCities(participantIdx),
					game => game.Advisors.HandleAllCities(participantIdx),
					Register.Kept,
					game => participantIdx == game.World.PersonParticipantIdx ? (Calls(game, pair, "RepaintMainScreen") > shown ? "human, city shown" : "human") :
						diagnostics == 0 ? "no taxes" :
						lastTechnology != 0 ? "taxes, no science" :
						game.World.Participants[participantIdx].LevyRatio + game.World.Participants[participantIdx].ResearchLevyRatio == 6 && science + taxes == 7 ? "taxes, luxuries 4" : "taxes");
			}, output, ["human, city shown", "no taxes", "taxes", "taxes, no science", "taxes, luxuries 4"]);
		}
	}
}
