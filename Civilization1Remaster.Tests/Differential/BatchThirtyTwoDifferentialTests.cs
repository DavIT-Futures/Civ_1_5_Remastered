using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The route step of the AI, the AI's peace treaties, the drive of the saved games and the castle pictures against their original code</summary>
	public class BatchThirtyTwoDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void LocateRouteStepMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				(int horizLoc, int vertLoc) = RandomTile(pair.Original.Engine, chance);
				ushort marker = (ushort)chance.Next(2);
				// The blocks of 4 x 4 tiles with a way (land 0xdb44, sea 0x7f38), 13 a column
				byte[] land = Enumerable.Range(0, 0x104).Select(_ => (byte)(chance.Next(3) == 0 ? 0 : 1)).ToArray();
				byte[] sea = Enumerable.Range(0, 0x104).Select(_ => (byte)(chance.Next(3) == 0 ? 0 : 1)).ToArray();
				bool own = (marker != 0 ? sea : land)[(vertLoc / 4) + (0xd * (horizLoc / 4))] != 0;

				return new Scenario($"LocateRouteStep({horizLoc}, {vertLoc}, {marker}), own block marked {own}",
					game =>
					{
						for (int idx = 0; idx < 0x104; idx++)
						{
							game.Processor.EmitGlobalByte((ushort)(0xdb44 + idx), land[idx]);
							game.Processor.EmitGlobalByte((ushort)(0x7f38 + idx), sea[idx]);
						}

						game.Processor.EmitGlobalWord(0x6590, 0xffff);
					},
					game => new RoutePlannerReference(game).FindRouteStep(horizLoc, vertLoc, marker),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Routing.FindRouteStep(horizLoc, vertLoc, marker)),
					Register.Acc | Register.Kept,
					game => own ? "own block" : game.Processor.RegAcc.Value16 != 0 ? $"neighbour, {(marker != 0 ? "sea" : "land")}" : "none");
			}, output, ["own block", "neighbour, sea", "neighbour, land", "none"]);
		}

		[Fact]
		public void AIWeighsTreatyMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				short human = pair.Original.Engine.World.PersonParticipantIdx;
				short[] others = Enumerable.Range(1, 7).Select(idx => (short)idx).Where(idx => idx != human).ToArray();
				short participantIdx1 = others[chance.Next(others.Length)];
				short participantIdx2 = others.Where(idx => idx != participantIdx1).ToArray()[chance.Next(others.Length - 1)];
				short diagnostics = (short)(chance.Next(8) == 0 ? 0 : 1);
				ushort[] relations = Enumerable.Range(0, 16).Select(_ => (ushort)chance.Next(0, 8)).ToArray();
				short[] might = Enumerable.Range(0, 3).Select(_ => (short)chance.Next(0, 100)).ToArray();
				short[] parts = { (short)(chance.Next(4) == 0 ? chance.Next(0, 3) : 0), (short)(chance.Next(4) == 0 ? chance.Next(0, 3) : 0) };
				// A few continents of each: cities and defence of the first one, defence and attack of the other one
				short[] continents = Enumerable.Range(0, 2 * 16).SelectMany(idx => chance.Next(4) != 0 ? new short[3] : idx < 16 ?
					new[] { (short)(chance.Next(3) == 0 ? 0 : chance.Next(1, 6)), (short)chance.Next(0, 30), (short)chance.Next(0, 20) } :
					new[] { (short)0, (short)chance.Next(0, 10), (short)chance.Next(0, 12) }).ToArray();

				return new Scenario($"AIWeighsTreaty({participantIdx1}, {participantIdx2}), diagnostics {diagnostics}, relations {string.Join("", relations)}, " +
					$"might {string.Join(" ", might)}, parts {string.Join(" ", parts)}",
					game =>
					{
						Player[] players = { game.World.Participants[participantIdx1], game.World.Participants[participantIdx2] };
						game.World.DiagMarkers = (short)((game.World.DiagMarkers & ~1) | diagnostics);
						game.World.Participants[human].MartialMight = might[2];

						for (int side = 0; side < 2; side++)
						{
							Array.Copy(relations, side * 8, players[side].Relations, 0, 8);
							players[side].MartialMight = might[side];
							players[side].LiveTroops[25] = parts[side];

							for (int continentIdx = 0; continentIdx < 16; continentIdx++)
							{
								int place = ((side * 16) + continentIdx) * 3;
								players[side].Landmasses[continentIdx].TownTally = continents[place];
								players[side].Landmasses[continentIdx].Guard = continents[place + 1];
								players[side].Landmasses[continentIdx].Assault = continents[place + 2];
							}
						}
					},
					game => new ForeignAffairsReference(game).AIWeighsTreaty(participantIdx1, participantIdx2),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Politics.AIWeighsTreaty(participantIdx1, participantIdx2)),
					Register.Acc | Register.Kept,
					game =>
					{
						if (diagnostics == 0)
							return "no diagnostics";

						bool humanPeace = ((relations[human] & 3) == 1 && might[1] < might[2] && (relations[8 + human] & 4) == 0) ||
							((relations[8 + human] & 3) == 1 && might[0] < might[2] && (relations[human] & 4) == 0);

						return humanPeace ? "peace with the human" : parts[0] < parts[1] ? "fewer parts" :
							game.Processor.RegAcc.Value16 != 0 ? "accepted" : "refused";
					});
			}, output, ["no diagnostics", "peace with the human", "fewer parts", "accepted", "refused"]);
		}

		/// <summary>Drive letters (A, B, c, x), Return and Escape</summary>
		private static readonly int[] DriveKeys = { 0x63, 0x0d, 0x61, 0x0d, 0x1b, 0x62, 0x78, 0x0d, 0x0d, 0x0d, 0x1b, 0x41, 0x0d };

		[Fact]
		public void QueryForStoreDriveMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				ushort drive = chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 6);
				ushort saved = (ushort)chance.Next(2);
				int menus = Calls(pair.Rewritten.Engine, pair, "DisplayMenu");

				return new Scenario($"QueryForStoreDrive(), drive {(short)drive}, saved files {saved}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x4384, drive);
						game.Processor.EmitGlobalWord(0x681a, saved);
					},
					game => new SaveGameStoreReference(game).AskForSaveDrive(),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.SaveLoad.AskForSaveDrive()),
					Register.Acc | Register.Kept,
					game => Calls(game, pair, "DisplayMenu") > menus ? "no disk" : game.Processor.RegAcc.Value16 == 0xffff ? "cancelled" : "drive");
			}, output, ["no disk", "cancelled", "drive"], DriveKeys);
		}

		/// <summary>The pictures (not in the snapshot) are drawn on the screen: the table of the sections at 0xb23c</summary>
		private static void ShowPictures(GameEngine game)
		{
			for (int idx = 0; idx < 16; idx++)
				game.VideoCard.DrawPictureToScreen(game.Processor.PullGlobalWord(0xaa), (idx & 3) * 80, (idx >> 2) * 50,
					game.Processor.PullGlobalWord((ushort)(0xb23c + (idx << 1))));
		}

		[Fact]
		public void ImportPalaceCastleMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort castles = (ushort)(chance.Next(8) == 0 ? 0 : chance.Next(4) == 0 ? 1 << chance.Next(5) : chance.Next(0, 32));
				ushort levelsOnly = (ushort)chance.Next(2);
				ushort[] levels = Enumerable.Range(0, 8).Select(_ => chance.Next(5) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 5)).ToArray();
				ushort[] places = Enumerable.Range(0, 8).Select(_ => (ushort)chance.Next(0, 4)).ToArray();
				byte[] styles = Enumerable.Range(0, 16).Select(_ => new byte[] { 0, 1, 2, 3, 4, 0xff, 5 }[chance.Next(7)]).ToArray();
				bool first = chance.Next(2) == 0;

				return new Scenario($"ImportPalaceCastle(0x{castles:x2}, {levelsOnly}), levels {string.Join(" ", levels.Select(level => (short)level))}, " +
					$"places {string.Join("", places)}, styles {string.Join(" ", styles)}, first picture unset {first}",
					game =>
					{
						for (int idx = 0; idx < 8; idx++)
						{
							game.Processor.EmitGlobalWord((ushort)(0xd91e + (idx << 1)), levels[idx]);
							game.Processor.EmitGlobalWord((ushort)(0xb222 + (idx << 1)), places[idx]);
						}

						for (int idx = 0; idx < 16; idx++)
							game.Processor.EmitGlobalByte((ushort)(0xb25e + idx), styles[idx]);

						if (first)
							game.Processor.EmitGlobalWord(0x6828, 0xffff);
					},
					game =>
					{
						new PalaceBuilderReference(game).LoadPalaceCastle(castles, levelsOnly);
						ShowPictures(game);
					},
					game =>
					{
						game.Throne.LoadPalaceCastle(castles, levelsOnly);
						ShowPictures(game);
					},
					Register.Kept,
					game => castles == 0 ? "no castle" : $"{(levelsOnly != 0 ? "levels" : "all sections")}{(first ? ", first picture" : "")}");
			}, output, ["no castle", "levels", "all sections", "levels, first picture", "all sections, first picture"]);
		}
	}
}
