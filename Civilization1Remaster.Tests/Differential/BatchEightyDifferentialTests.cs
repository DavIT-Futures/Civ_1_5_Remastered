using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchEightyDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitCityDefendersMatchOriginalCode()
	{
		// What the defenders did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 3, 9, 13 })
			CheckDefenders(bay, ways);

		foreach (string way in new[] { "alone", "better than the leader", "calls for defenders", "fortified leader", "goes on" })
			Assert.True(ways.Contains(way), $"No defender did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckDefenders(int bay, HashSet<string> ways)
	{
		Check(bay, 30, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			short[] players = Enumerable.Range(1, 7)
				.Where(idx => idx != person && engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == idx))
				.Select(idx => (short)idx).ToArray();
			short participantIdx = players[chance.Next(players.Length)];
			List<short> units = Enumerable.Range(0, 128).Where(idx => UnitByte(engine, participantIdx, idx, UnitMemory.Kind) != 0xff).Select(idx => (short)idx).ToList();
			var towns = engine.World.Towns.Select((town, idx) => (town, idx)).Where(pairing => pairing.town.ConditionMarker != 0xff && pairing.town.ParticipantIdx == participantIdx).ToList();

			if (units.Count < 4 || towns.Count == 0)
				return null;

			// The defenders (role 2) of the unit types, by their defence
			byte[] defenders = Enumerable.Range(0, 28)
				.Where(kind => engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.TroopGroup)) == 2)
				.Select(kind => (byte)kind).ToArray();
			int stack = chance.Next(1, 5);
			short[] members = units.OrderBy(_ => chance.Next()).Take(stack).ToArray();
			byte[] kinds = members.Select(_ => defenders[chance.Next(defenders.Length)]).ToArray();
			var (town, townIdx) = towns[chance.Next(towns.Count)];
			sbyte size = (sbyte)chance.Next(1, 16);
			bool palace = chance.Next(3) == 0;
			short round = (short)chance.Next(400);
			short troopIdx = members[0];
			byte[] statuses = members.Select(_ => (byte)(chance.Next(8) & 5)).ToArray();
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);

			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) in city {townIdx}, stack {stack}, size {size}, palace {palace}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					var place = game.World.Towns[townIdx];
					place.TrueExtent = size;
					place.StructureMarkers0 = (ushort)(palace ? place.StructureMarkers0 | 1 : place.StructureMarkers0 & ~1);
					game.World.RoundTally = round;

					for (int memberIdx = 0; memberIdx < members.Length; memberIdx++)
					{
						ushort unit = UnitMemory.Address(participantIdx, members[memberIdx]);
						void Set(int offset, byte amount) => game.Processor.EmitUInt8(0x3b01, (ushort)(unit + offset), amount);

						game.MapOps.RemoveUnitFromTile(participantIdx, members[memberIdx], game.Processor.PullInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)),
							game.Processor.PullInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)));
						Set(UnitMemory.Kind, kinds[memberIdx]);
						Set(UnitMemory.Status, statuses[memberIdx]);
						Set(UnitMemory.Horiz, (byte)place.Place.X);
						Set(UnitMemory.Vert, (byte)place.Place.Y);
						Set(UnitMemory.LeftoverSteps, 3);
						Set(UnitMemory.TravelHoriz, 0xff);
						Set(UnitMemory.TravelVert, 0xff);
						// A ring of the stack (alone: 0xff)
						Set(UnitMemory.FollowingInStack, members.Length == 1 ? (byte)0xff : (byte)members[(memberIdx + 1) % members.Length]);
						game.MapOps.PlaceUnitOnTile(participantIdx, members[memberIdx], place.Place.X, place.Place.Y);
					}
				},
				game => new ComputerPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// What the defender did, from the answer, the calls and the status bit 3 (fortified)
				game =>
				{
					ushort answer = game.Processor.RegAcc.Value16;
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					string way;

					if (answer == 0x66 && members.Length == 1)
						way = "alone";
					else if (answer == 0x66 && Delta("TallyUnitsWithFlagInStack") == 0)
						way = "better than the leader";
					else if (Delta("SendUnitToStrategicLocation") > 0 && answer == 0x66)
						way = "calls for defenders";
					else if (answer == 0x66 && Delta("TallyUnitsByDomainInStack") > 0)
						way = "fortified leader";
					else
						way = "goes on";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
