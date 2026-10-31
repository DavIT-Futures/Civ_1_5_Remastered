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

public sealed class BatchEightyFourDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitDestinationsMatchOriginalCode()
	{
		// Where the units without a destination went, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckDestinations(bay, ways);

		foreach (string way in new[] { "enemy city", "own city", "random land" })
			Assert.True(ways.Contains(way), $"No unit chose '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort RoleOf(GameEngine game, byte kind)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.TroopGroup));
	}

	private void CheckDestinations(int bay, HashSet<string> ways)
	{
		Check(bay, 96, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// Half of the time a transport (role 5) of a computer player with a stack (its units on board), else an attacker
			// (role 3, it comes here too; ships in a city half of the time), transports outside cities, as the save has it
			bool transport = chance.Next(2) == 0;
			bool docked = chance.Next(2) == 0;
			bool InCity((short Player, short Troop) unit) => engine.World.Towns.Any(town => town.ConditionMarker != 0xff &&
				(byte)town.Place.X == UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz) && (byte)town.Place.Y == UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert));
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
					(transport ? RoleOf(engine, kind) is 5 && UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff
						: RoleOf(engine, kind) is 3) && InCity(unit) == (!transport && docked))
				.ToList();

			if (candidates.Count == 0)
				return null;

			var (participantIdx, troopIdx) = candidates[chance.Next(candidates.Count)];

			List<short> members = [];
			for (short member = (sbyte)UnitByte(engine, participantIdx, troopIdx, UnitMemory.FollowingInStack); member >= 0 && member != troopIdx && members.Count < 128;
				member = (sbyte)UnitByte(engine, participantIdx, member, UnitMemory.FollowingInStack))
			{
				members.Add(member);
			}

			// The first unit on board stays awake (the load wants to land), the others are fortified at random (other roles)
			bool[] fortified = members.Select((_, idx) => idx > 0 && chance.Next(2) == 0).ToArray();
			// A third of the time settlers and another land unit on board (both roles in 0x1e)
			int partner = members.Count >= 2 && chance.Next(3) == 0 ? chance.Next(1, 13) : -1;
			// A quarter of the time a unit of another player next to it (0x4c not zero)
			int neighbour = chance.Next(4) == 0 ? chance.Next(1, 9) : -1;
			short stranger = (short)((participantIdx + chance.Next(1, 7)) % 8);
			bool trireme = transport && chance.Next(4) == 0;
			byte steps = (byte)chance.Next(0, 7);
			short round = (short)chance.Next(0, 400);
			short standing = (short)chance.Next(0, 11);
			// A third of the time (always for a ship in a city) at peace with everybody: no enemy city, the own cities are next
			bool peace = (!transport && docked) || chance.Next(3) == 0;
			ushort[] relations = Enumerable.Range(0, 8).Select(_ => (ushort)(peace ? 2 : new[] { 0, 2, 0x102, 0x100, 1 }[chance.Next(5)])).ToArray();
			// Strong continents: the transports do not land next to them (the role of the load does not fit approach 0)
			short[] strength = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(16, 32)).ToArray();
			short[] approach = Enumerable.Range(0, 16).Select(_ => (short)new[] { 0, 0, 0, 1, 2, 5 }[chance.Next(6)]).ToArray();
			short[] townTally = Enumerable.Range(0, 16).Select(_ => (short)(chance.Next(3) == 0 ? 0 : chance.Next(1, 5))).ToArray();
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) with {members.Count} more, trireme {trireme}, steps {steps}, round {round}, standing {standing}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					for (int memberIdx = 0; memberIdx < members.Count; memberIdx++)
					{
						ushort status = (ushort)(UnitMemory.Address(participantIdx, members[memberIdx]) + UnitMemory.Status);
						byte current = game.Processor.PullUInt8(0x3b01, status);
						game.Processor.EmitUInt8(0x3b01, status, (byte)(fortified[memberIdx] ? current | 8 : current & ~8));
					}

					if (partner >= 0)
					{
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, members[0]) + UnitMemory.Kind), 0);
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, members[1]) + UnitMemory.Kind), (byte)partner);
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, members[1]) + UnitMemory.Status),
							(byte)(game.Processor.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, members[1]) + UnitMemory.Status)) & ~8));
					}

					ushort unit = UnitMemory.Address(participantIdx, troopIdx);

					if (neighbour >= 0)
					{
						IMapPlanes chart = game.MapOps.Chart;
						int besideHoriz = (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) + chart.DirectionHoriz(neighbour) + 80) % 80;
						int besideVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)) + chart.DirectionVert(neighbour);

						if (besideVert >= 0 && besideVert < 50 && (chart.OwnerBits(besideHoriz, besideVert) & MapPlanes.UnitsPresent) == 0)
							chart.AssignOwnerBits(besideHoriz, besideVert, MapPlanes.UnitsPresent | stranger);
					}

					// (the land units on board of a sailing ship, not a trireme: a trireme goes nowhere)
					if (partner >= 0 && !trireme)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0x11);

					if (trireme)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0x10);

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.World.RoundTally = round;
					game.World.Participants[person].Standing = standing;

					for (int other = 0; other < 8; other++)
					{
						if (other != participantIdx)
							game.World.Participants[participantIdx].Relations[other] = relations[other];
					}

					for (int continentIdx = 0; continentIdx < 16; continentIdx++)
					{
						game.World.Participants[participantIdx].Landmasses[continentIdx].Assault = strength[continentIdx];
						game.World.Participants[participantIdx].Landmasses[continentIdx].Approach = approach[continentIdx];
						game.World.Participants[participantIdx].Landmasses[continentIdx].TownTally = townTally[continentIdx];
					}
				},
				game => new ComputerPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// Where the transport went: a city of another player, an own city, a land tile without cities, or nowhere
				game =>
				{
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					byte travelHoriz = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz));
					byte travelVert = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert));
					var town = game.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && (byte)city.Place.X == travelHoriz && (byte)city.Place.Y == travelVert);
					string way = travelHoriz == 0xff ? "none"
						: town == null ? "random land"
						: town.ParticipantIdx == participantIdx ? "own city"
						: "enemy city";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
