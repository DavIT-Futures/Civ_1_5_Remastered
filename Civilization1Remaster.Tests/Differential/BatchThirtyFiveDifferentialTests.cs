using System;
using System.Drawing;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The revolt of a city, the city sites of a new map, the Hall of Fame and the epic chronicle against their original code</summary>
	public class BatchThirtyFiveDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The replay (segment 0x3725) up to the data segment</summary>
		private static readonly (ushort, ushort, int)[] Replay = [(0x3725, 0, 0x3dc0)];

		[Fact]
		public void ComputeInciteCostMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				short ownerIdx = engine.World.Towns[townIdx].ParticipantIdx;
				short participantIdx = (short)(chance.Next(3) == 0 ? -1 : chance.Next(0, 8));

				if (participantIdx == ownerIdx)
					return null;

				ushort known = (ushort)(chance.Next(2) == 0 ? 0x40 : 0);
				ushort disks = (ushort)chance.Next(1, 3);
				int seized = Calls(pair.Rewritten.Engine, pair, "SeizeCity");
				int dialogs = Calls(pair.Rewritten.Engine, pair, "DisplayDialog");

				return new Scenario($"ComputeInciteCost({townIdx}, {participantIdx}), owner {ownerIdx}, known 0x{known:x2}, disks {disks}",
					game =>
					{
						ushort[] relations = game.World.Participants[game.World.PersonParticipantIdx].Relations;
						relations[ownerIdx] = (ushort)((relations[ownerIdx] & ~0x40) | known);
						game.Processor.EmitGlobalWord(0xdeba, disks);
					},
					game => new DiplomatMissionsReference(game).ComputeInciteCost(townIdx, participantIdx),
					game => game.Diplomats.ComputeInciteCost(townIdx, participantIdx),
					Register.Kept,
					game => Calls(game, pair, "SeizeCity") == seized ? "no rival" :
						$"{(participantIdx == -1 ? "rival city" : participantIdx == 0 ? "barbarians" : "given player")}" +
						$"{(Calls(game, pair, "DisplayDialog") > dialogs ? ", announced" : "")}",
					Replay);
			}, output, ["no rival", "rival city, announced", "given player", "given player, announced", "barbarians, announced"], [0x0d]);
		}

		[Fact]
		public void SituateSpecialTerrainMatchesOriginalCode()
		{
			Check(3, 4, (chance, pair) =>
			{
				// The colours of the terrains 0 - 11 (the table 0x2ba6 of colour -> terrain)
				GameEngine engine = pair.Original.Engine;
				int[] colours = Enumerable.Range(0, 16).Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) < 12).ToArray();
				int[] map = Enumerable.Range(0, 80 * 50).Select(_ => colours[chance.Next(colours.Length)]).ToArray();
				int[] continents = Enumerable.Range(0, 80 * 50).Select(_ => chance.Next(16)).ToArray();
				byte[] specials = Enumerable.Range(0, 0x18 * 0x13).Select(_ => (byte)chance.Next(0, 4)).ToArray();

				return new Scenario($"SituateSpecialTerrain(), {colours.Length} terrain colours",
					game =>
					{
						// The screen of the box 0xaa, which the function reads (a map of terrain colours only, like during the
						// generation of a map; other colours would give terrains above 23)
						ushort display = game.Processor.PullGlobalWord(game.Processor.PullGlobalWord(0xaa));
						game.VideoCard.FloodBox(display, new Rectangle(0, 0, 320, 200), 0, 0);

						for (int spot = 0; spot < 80 * 50; spot++)
						{
							game.VideoCard.AssignDot(display, spot % 80, spot / 80, (byte)map[spot], 0);
							game.VideoCard.AssignDot(display, spot % 80, 50 + (spot / 80), (byte)continents[spot], 0);
						}

						// Random terrain values (food, shields, trade of the 24 terrains)
						for (int idx = 0; idx < 0x18; idx++)
						{
							for (int part = 0; part < 3; part++)
								game.Processor.EmitGlobalByte((ushort)(0x290 + (0x13 * idx) + part), specials[(idx * 0x13) + part]);
						}
					},
					game => new WorldBuilderReference(game).PlaceSpecialTerrain(),
					game => game.WorldSetup.PlaceSpecialTerrain(),
					Register.Kept, game => "sites");
			}, output, ["sites"]);
		}

		[Fact]
		public void DisplayHallOfFameMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				ushort arg1 = chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 6);
				byte[][] records = Enumerable.Range(0, 6).Select(_ =>
				{
					byte[] record = new byte[0x2e];
					short score = (short)(chance.Next(5) == 0 ? -chance.Next(0, 5) : chance.Next(1, 3000));
					short year = (short)chance.Next(-4000, 2100);
					short hardness = (short)chance.Next(0, 5);
					short population = (short)chance.Next(0, 30000);
					(record[0], record[1]) = ((byte)score, (byte)(score >> 8));
					(record[2], record[3]) = ((byte)hardness, (byte)(hardness >> 8));
					(record[6], record[7]) = ((byte)year, (byte)(year >> 8));
					(record[0xc], record[0xd]) = ((byte)population, (byte)(population >> 8));
					// Short names: a longer line overwrites the path of CIV.EXE after the text buffer, which differs between the games
					string leader = new string(Enumerable.Range(0, chance.Next(3, 10)).Select(_ => (char)('a' + chance.Next(26))).ToArray());
					string civilization = new string(Enumerable.Range(0, chance.Next(3, 10)).Select(_ => (char)('A' + chance.Next(26))).ToArray());

					for (int idx = 0; idx < leader.Length; idx++)
						record[0xe + idx] = (byte)leader[idx];

					for (int idx = 0; idx < civilization.Length; idx++)
						record[0x1e + idx] = (byte)civilization[idx];

					return record;
				}).ToArray();

				return new Scenario($"DisplayHallOfFame({(short)arg1})",
					game =>
					{
						for (int place = 0; place < 6; place++)
						{
							for (int idx = 0; idx < 0x2e; idx++)
								game.Processor.EmitUInt8(0x3772, (ushort)(0x37d2 + (place * 0x2e) + idx), records[place][idx]);
						}
					},
					game => new FameRecordsReference(game).ShowHallOfFame(arg1),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Legends.ShowHallOfFame(arg1)),
					Register.Acc | Register.Kept,
					game => arg1 == 0xffff ? "no hint" : arg1 == 5 ? "sixth place" : game.Processor.RegAcc.Value16 != 0 ? "C pressed" : "other key");
			}, output, ["no hint", "sixth place", "C pressed", "other key"], [0x63, 0x0d, 0x43, 0x78]);
		}

		[Fact]
		public void DisplayEpicChronicleMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short human = engine.World.PersonParticipantIdx;
				short hardness = (short)chance.Next(0, 5);
				ushort[] known = Enumerable.Range(0, 8).Select(_ => (ushort)(chance.Next(2) == 0 ? 0x40 : 0)).ToArray();
				short realms = (short)(chance.Next(3) == 0 ? (chance.Next(0x100) | (1 << human)) : 0xff);
				ushort sp = engine.Processor.RegStack.Value16;

				return new Scenario($"DisplayEpicChronicle(), level {hardness}, known {string.Join("", known.Select(bit => bit != 0 ? 1 : 0))}, live 0x{realms:x2}",
					game =>
					{
						game.World.HardnessTier = hardness;
						game.World.LiveRealms = realms;
						known.CopyTo(game.World.Participants[human].Relations, 0);
					},
					game => new WorldStatisticsReference(game).ShowEpicChronicle(),
					game => game.Atlas.ShowEpicChronicle(),
					Register.Kept,
					// The measure stays in the frame of the original under the stack (local 4)
					game => $"measure {pair.Original.Engine.Processor.PullGlobalWord((ushort)(sp - 2 - 4))}");
			}, output, ["measure 0", "measure 1", "measure 2", "measure 3", "measure 4"], [0x0d]);
		}
	}
}
