using System;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyFourDifferentialTests(ITestOutputHelper output)
{
	/// <summary>The blocks of the map (20 x 13 bytes at 0x3772:0x2438) that OperateAITurn marks</summary>
	private static readonly (ushort Segment, ushort Begin, int Length)[] Blocks = [(0x3772, 0x2438, 0x104)];

	/// <summary>A byte of a unit, read without counting it for the timer of the tests (it must not change the game)</summary>
	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	[Fact]
	public void OperateAITurnMatchesOriginalCode()
	{
		// The orders a survey gave, over the scenarios of all the saves
		System.Collections.Generic.HashSet<string> orders = [];

		foreach (int bay in new[] { 1, 6, 11 })
			CheckOperateAITurn(bay, orders);

		foreach (string order in new[] { "government", "unit given up", "production", "targets", "units sent" })
			Assert.True(orders.Contains(order), $"No scenario gave the order '{order}' (given: {string.Join(", ", orders)})");
	}

	private void CheckOperateAITurn(int bay, System.Collections.Generic.HashSet<string> orders)
	{
		const int ScenarioTally = 24;
		int step = 0;

		Check(bay, ScenarioTally, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			// The first scenarios: the barbarians and a marked player, the last one: a ruined player
			int mode = step < 2 ? step++ : step++ == ScenarioTally - 1 ? 9 : chance.Next(2, 9);
			short[] alive = Enumerable.Range(1, 7).Where(idx => engine.World.Participants[idx].TownTally > 0).Select(idx => (short)idx).ToArray();
			short[] others = alive.Where(idx => idx != engine.World.PersonParticipantIdx).ToArray();
			short participantIdx = mode == 0 ? (short)0 : mode is 8 or 9 ? others[chance.Next(others.Length)] : alive[chance.Next(alive.Length)];
			short round = (short)chance.Next(400);
			int liveTowns = engine.World.Towns.Count(town => town.ConditionMarker != 0xff);

			// Mode 8: a computer player with exactly 10 or 11 cities that knows the technology 0x2b in the turn of its government
			int townsWanted = mode == 8 && liveTowns > 11 ? 10 + chance.Next(2) : -1;
			if (townsWanted > 0)
				round = (short)(round & ~7 | (8 - participantIdx) & 7);

			// Mode 7: a bomber goes to the strategic locations 29 and 31 (one near, one far), in a turn that allows the odd locations
			if (mode == 7)
				round = (short)(round & ~2 | ((31 & 1) ^ 1) << 1);

			short bomberHoriz = (short)chance.Next(80);
			short bomberVert = (short)chance.Next(2, 48);
			short hardness = (short)chance.Next(5);
			ushort anarchy = (ushort)(chance.Next(5) == 0 ? 1 : 0);
			ushort communism = (ushort)(chance.Next(3) == 0 ? 0xffff : 0);
			bool marked = mode == 1 || (mode == 2 && bay == 1);
			// At the end a player loses its cities (to another player) and its units: the civilization is eliminated
			bool ruined = mode == 9 && participantIdx != engine.World.PersonParticipantIdx;
			short heir = (short)(participantIdx == 1 ? 2 : 1);
			bool war = chance.Next(2) == 0;

			int governments = Calls(engine, pair, "SwitchGovernment");
			int eliminations = Calls(engine, pair, "EliminateCivilization");
			int givenUp = Calls(engine, pair, "EliminateUnit");
			int refreshed = Calls(engine, pair, "RefreshCityProduction");
			int targets = Calls(engine, pair, "SendUnitToStrategicLocation");
			int occupations = Calls(engine, pair, "VerifyTileOccupation");
			byte[] destinations = Enumerable.Range(0, 128)
				.SelectMany(troopIdx => new[] { UnitByte(engine, participantIdx, troopIdx, 6), UnitByte(engine, participantIdx, troopIdx, 7) }).ToArray();

			return new Scenario(
				$"OperateAITurn({participantIdx}), round {round}, level {hardness}, e3c2 {anarchy}, db42 {communism:x}, marked {marked}, ruined {ruined}, war {war}",
				game =>
				{
					game.World.RoundTally = round;
					game.World.HardnessTier = hardness;
					game.Processor.EmitGlobalWord(0xe3c2, anarchy);
					game.Processor.EmitGlobalWord(0xdb42, communism);
					game.World.ParticipantMarkers = (short)(marked ? game.World.ParticipantMarkers | (1 << participantIdx) : game.World.ParticipantMarkers & ~(1 << participantIdx));

					for (int otherIdx = 1; otherIdx < 8; otherIdx++)
					{
						if (otherIdx != participantIdx)
						{
							ushort relation = game.World.Participants[participantIdx].Relations[otherIdx];
							game.World.Participants[participantIdx].Relations[otherIdx] = (ushort)(war ? (relation & ~2) | 1 : relation);
						}
					}

					// Mode 6: the units have orders (status bits 2, 3) and the strategic locations priority 2 or 3
					if (mode == 6)
					{
						for (int troopIdx = 0; troopIdx < 128; troopIdx++)
						{
							ushort status = (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Status);
							game.Processor.EmitUInt8(0x3b01, status, (byte)(game.Processor.PullUInt8(0x3b01, status) | (troopIdx % 3 == 0 ? 4 : 8)));
						}

						for (int locationIdx = 0; locationIdx < 32; locationIdx++)
							game.Processor.EmitUInt8(0x3b01, (ushort)(0xb2be + (participantIdx << 5) + locationIdx), (byte)(2 + locationIdx % 2));
					}

					if (mode == 7)
					{
						int troopIdx = Enumerable.Range(0, 128).First(idx => game.Processor.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, idx) + UnitMemory.Kind)) == 0xff);
						ushort bomber = UnitMemory.Address(participantIdx, troopIdx);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Status), 0);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Horiz), (byte)bomberHoriz);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Vert), (byte)bomberVert);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Kind), 0xf);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.FollowingInStack), 0xff);
						byte role = (byte)UnitKindTable.Field(game.Processor, 0xf, UnitKindTable.TroopGroup);
						int range = (short)UnitKindTable.Field(game.Processor, 0xf, UnitKindTable.ShiftTally);

						// Location 29 within the range (doubled, priority 1), location 31 beyond it (quadrupled, priority 7):
						// the far one weighs as much as the near one (the near one is taken), any other factor changes the choice
						int near = Math.Max(1, range - 2);
						int far = Enumerable.Range(range + 1, 40).FirstOrDefault(distance => 4 * distance / 8 == 2 * near / 2, range + 1);

						foreach ((int locationIdx, int distance, byte locationRole, byte priority) in new[] { (29, near, role, (byte)1), (31, far, (byte)3, (byte)7) })
						{
							int at = (participantIdx << 5) + locationIdx;
							game.Processor.EmitUInt8(0x3b01, (ushort)(0xe2c2 + at), locationRole);
							game.Processor.EmitUInt8(0x3b01, (ushort)(0xe598 + at), (byte)((bomberHoriz + distance) % 80));
							game.Processor.EmitUInt8(0x3b01, (ushort)(0xe798 + at), (byte)bomberVert);
							game.Processor.EmitUInt8(0x3b01, (ushort)(0xb2be + at), priority);
						}
					}

					if (townsWanted > 0)
					{
						var towns = game.World.Towns.Where(town => town.ConditionMarker != 0xff).ToList();
						var own = towns.Where(town => town.ParticipantIdx == participantIdx).ToList();

						foreach (var town in towns.Where(town => town.ParticipantIdx != participantIdx).Take(Math.Max(0, townsWanted - own.Count)))
							town.ParticipantIdx = (sbyte)participantIdx;
						foreach (var town in own.Skip(townsWanted))
							town.ParticipantIdx = (sbyte)heir;

						ushort[] markers = game.World.Participants[participantIdx].FoundScienceMarkers;
						markers[0x2b >> 4] |= (ushort)(1 << (0x2b & 0xf));
						markers[0x2c >> 4] = (ushort)(markers[0x2c >> 4] & ~(1 << (0x2c & 0xf)));
						game.Processor.EmitGlobalWord(0xe3c2, 0);
					}

					if (ruined)
					{
						foreach (var town in game.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx))
							town.ParticipantIdx = (sbyte)heir;

						for (int troopIdx = 0; troopIdx < 128; troopIdx++)
							game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Kind), 0xff);
					}
				},
				game => new ComputerPlayerReference(game).OperateAITurn(participantIdx),
				game => game.Bot.OperateAITurn(participantIdx),
				Register.Kept,
				// What the survey did: nothing for the barbarians or a marked player beyond the counting, else the elimination of
				// the civilization, a new government, an obsolete unit given up, a unit sent to a strategic location
				game =>
				{
					if (Calls(game, pair, "EliminateCivilization") > eliminations)
						return "civilization eliminated";
					if (Calls(game, pair, "VerifyTileOccupation") == occupations && participantIdx != 0)
						return "did not count";

					if (Calls(game, pair, "SwitchGovernment") > governments)
						orders.Add("government");
					if (Calls(game, pair, "EliminateUnit") > givenUp)
						orders.Add("unit given up");
					if (Calls(game, pair, "RefreshCityProduction") > refreshed)
						orders.Add("production");
					if (Calls(game, pair, "SendUnitToStrategicLocation") > targets)
						orders.Add("targets");

					byte[] after = Enumerable.Range(0, 128)
						.SelectMany(troopIdx => new[] { UnitByte(game, participantIdx, troopIdx, 6), UnitByte(game, participantIdx, troopIdx, 7) }).ToArray();

					if (!after.SequenceEqual(destinations))
						orders.Add("units sent");

					return participantIdx == 0 ? "barbarians" : marked ? "marked" : "orders";
				},
				Blocks);
		}, output, ["barbarians", "marked", "orders", "civilization eliminated"], null);
	}
}
