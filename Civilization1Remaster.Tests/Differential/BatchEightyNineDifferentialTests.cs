using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchEightyNineDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitDirectionsMatchOriginalCode()
	{
		// Which tiles around the units were no target (diplomats in front of a stack, own stacks, land units at sea, ships
		// next to land), over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckDirections(bay, ways);

		foreach (string way in new[] { "looked", "diplomat", "own stack", "may occupy", "at sea", "ship by land", "submarine", "ship in city", "near own unit" })
			Assert.True(ways.Contains(way), $"No unit met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort KindField(GameEngine game, byte kind, int field)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + field));
	}

	private static int Group(GameEngine game, int participantIdx, int troopIdx, int field)
	{
		byte kind = UnitByte(game, participantIdx, troopIdx, UnitMemory.Kind);
		return kind == 0xff ? -1 : KindField(game, kind, field);
	}

	/// <summary>The members of the stack of the unit (its ring), the unit first</summary>
	private static List<int> Members(GameEngine game, int participantIdx, int troopIdx)
	{
		List<int> members = [troopIdx];
		int member = UnitByte(game, participantIdx, troopIdx, UnitMemory.FollowingInStack);

		while (member != 0xff && member != troopIdx && members.Count < 128)
		{
			members.Add(member);
			member = UnitByte(game, participantIdx, member, UnitMemory.FollowingInStack);
		}

		return members;
	}

	/// <summary>Moves the whole stack of the unit to the tile</summary>
	private static void MoveStack(GameEngine game, int participantIdx, int troopIdx, int horizLoc, int vertLoc)
	{
		MapLayers chart = game.MapOps.Chart;
		int oldHoriz = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
		int oldVert = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);

		foreach (int member in Members(game, participantIdx, troopIdx))
		{
			game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Horiz), (byte)horizLoc);
			game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Vert), (byte)vertLoc);
		}

		chart.AssignOwnerBits(oldHoriz, oldVert, chart.OwnerBits(oldHoriz, oldVert) & 7);
		chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | participantIdx);
	}

	/// <summary>Moves a lone land unit of a player other than the excluded ones to the tile; its player, or -1</summary>
	private static short MoveLoneStranger(GameEngine game, short excluded, int horizLoc, int vertLoc, int pick)
	{
		MapLayers chart = game.MapOps.Chart;
		var lone = Enumerable.Range(1, 7).Where(player => player != excluded)
			.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
			.Where(unit => Group(game, unit.Player, unit.Troop, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				UnitByte(game, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff)
			.ToList();

		if (lone.Count == 0)
			return -1;

		var (player, troop) = lone[pick % lone.Count];
		game.MapOps.DiscardUnitFromTile(player, troop, UnitByte(game, player, troop, UnitMemory.Horiz), UnitByte(game, player, troop, UnitMemory.Vert));
		game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Vert), (byte)vertLoc);
		chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | player);
		return player;
	}

	private void CheckDirections(int bay, HashSet<string> ways)
	{
		Check(bay, 128, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			MapLayers sight = engine.MapOps.Chart;
			short person = engine.World.PersonParticipantIdx;
			// The battles of the earlier scenarios may leave the city mark of a city that is gone: the mark goes away
			var orphans = Enumerable.Range(0, 80 * 50).Select(at => (Horiz: at / 50, Vert: at % 50))
				.Where(tile => (sight.Improvements(tile.Horiz, tile.Vert) & 1) != 0 &&
					!engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.Place.X == tile.Horiz && town.Place.Y == tile.Vert))
				.ToList();
			// 0 a stack next to a foreign stack with a diplomat in front (1: diplomats only), 2 a stack next to an own stack,
			// 3 a land unit at sea next to a foreign unit on land, 4 an attacking ship next to a foreign unit on land (5: a
			// submarine), 6 a ship in an own city; the ships are attackers or transports; 7 a lone unit with an own lone unit
			// next to it (it goes to a strategic location)
			int mode = new[] { 0, 1, 2, 3, 4, 4, 4, 5, 6, 7 }[chance.Next(10)];
			var units = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) != 0xff)
				.ToList();
			bool OnOcean((short Player, short Troop) unit) => sight.Terrain(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz),
				UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)) == MapRules.OceanTerrain;
			bool InCity((short Player, short Troop) unit) => (sight.Improvements(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz),
				UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)) & 1) != 0;
			bool NearFreeLand((short Player, short Troop) unit) => Enumerable.Range(1, 8).Any(direction =>
				(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz) + sight.DirectionHoriz(direction) + 80) % 80 is int h &&
				UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert) + sight.DirectionVert(direction) is int v && v > 0 && v < 49 &&
				sight.Terrain(h, v) != MapRules.OceanTerrain && (sight.OwnerBits(h, v) & MapLayers.UnitsPresent) == 0 && (sight.Improvements(h, v) & 1) == 0);
			bool NearFreeSea((short Player, short Troop) unit) => Enumerable.Range(1, 8).Any(direction =>
				(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz) + sight.DirectionHoriz(direction) + 80) % 80 is int h &&
				UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert) + sight.DirectionVert(direction) is int v && v > 0 && v < 49 &&
				sight.Terrain(h, v) == MapRules.OceanTerrain && (sight.OwnerBits(h, v) & MapLayers.UnitsPresent) == 0);
			var candidates = mode switch
			{
				// (stacks of attackers or defenders on land outside cities)
				0 or 1 or 2 => units.Where(unit => Group(engine, unit.Player, unit.Troop, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					Group(engine, unit.Player, unit.Troop, UnitKindTable.TroopGroup) is 1 or 2 &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff && !OnOcean(unit) && !InCity(unit)).ToList(),
				// (no land units at sea in the saves: a stack on the coast goes onto the sea next to it)
				3 => units.Where(unit => Group(engine, unit.Player, unit.Troop, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					Group(engine, unit.Player, unit.Troop, UnitKindTable.TroopGroup) is 1 or 2 && !OnOcean(unit) && !InCity(unit) &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff && NearFreeSea(unit)).ToList(),
				// (a lone attacker: it moves into an empty own city)
				7 => units.Where(unit => Group(engine, unit.Player, unit.Troop, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					Group(engine, unit.Player, unit.Troop, UnitKindTable.TroopGroup) == 1 &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff &&
					engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == unit.Player &&
						(sight.OwnerBits(town.Place.X, town.Place.Y) & MapLayers.UnitsPresent) == 0)).ToList(),
				// (ships next to a free land tile; for mode 6 the ship moves into a city)
				_ => units.Where(unit => Group(engine, unit.Player, unit.Troop, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
					Group(engine, unit.Player, unit.Troop, UnitKindTable.TroopGroup) is 3 or 5 && OnOcean(unit) && NearFreeLand(unit)).ToList(),
			};

			if (candidates.Count == 0)
				return null;

			var (participantIdx, troopIdx) = candidates[chance.Next(candidates.Count)];
			int pick = chance.Next(1000);
			int heading = chance.Next(1, 9);
			ushort[] relations = Enumerable.Range(0, 8).Select(_ => (ushort)chance.Next(0, 4)).ToArray();
			byte steps = (byte)chance.Next(0, 6);
			byte unitHeading = (byte)(chance.Next(3) == 0 ? 0xff : chance.Next(1, 9));
			byte[] pattern = Enumerable.Range(0, 20 * 13).Select(_ => (byte)chance.Next(4)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			// The stacks of other players (or own ones) and free land tiles next to them
			// (the first unit at its tile: the one ObtainUnitAtTile finds)
			var ownStack = Members(engine, participantIdx, troopIdx);
			var stacks = units.Where(unit => (mode == 2 ? unit.Player == participantIdx && !ownStack.Contains(unit.Troop) : unit.Player != participantIdx) &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff && !OnOcean(unit) && !InCity(unit) &&
					!units.Any(other => other.Player == unit.Player && other.Troop < unit.Troop &&
						UnitByte(engine, other.Player, other.Troop, UnitMemory.Horiz) == UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz) &&
						UnitByte(engine, other.Player, other.Troop, UnitMemory.Vert) == UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)))
				.ToList();
			var host = stacks.Count > 0 ? stacks[pick % stacks.Count] : (Player: (short)-1, Troop: (short)-1);
			bool allDiplomats = mode == 1;
			// An own coastal city for the ship of mode 6
			var harbours = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx &&
				Enumerable.Range(1, 8).Any(direction => town.Place.Y + sight.DirectionVert(direction) is int v && v > 0 && v < 49 &&
					sight.Terrain((town.Place.X + sight.DirectionHoriz(direction) + 80) % 80, v) == MapRules.OceanTerrain)).ToList();
			var harbour = harbours.Count > 0 ? harbours[pick % harbours.Count] : null;
			var emptyTowns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx &&
				(sight.OwnerBits(town.Place.X, town.Place.Y) & MapLayers.UnitsPresent) == 0).ToList();
			var emptyTown = emptyTowns.Count > 0 ? emptyTowns[pick % emptyTowns.Count] : null;
			bool submarine = mode == 5;

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) mode {mode}, host {host.Player}/{host.Troop}",
				game =>
				{
					MapLayers chart = game.MapOps.Chart;

					foreach (var orphan in orphans)
						chart.ClearImprovements(1, orphan.Horiz, orphan.Vert);

					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					foreach (int local in new[] { 0x20, 0x40, 0x5c })
						game.Processor.EmitUInt16(0x3b01, (ushort)(frame - local), 0x1234);

					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					short stranger = -1;

					if (mode <= 2 && host.Player >= 0)
					{
						// The stack goes next to the other stack
						int hostHoriz = UnitByte(game, host.Player, host.Troop, UnitMemory.Horiz);
						int hostVert = UnitByte(game, host.Player, host.Troop, UnitMemory.Vert);

						for (int turn = 0; turn < 8; turn++)
						{
							int direction = (heading - 1 + turn) % 8 + 1;
							int spotHoriz = (hostHoriz + chart.DirectionHoriz(direction) + 80) % 80;
							int spotVert = hostVert + chart.DirectionVert(direction);

							if (spotVert > 0 && spotVert < 49 && chart.Terrain(spotHoriz, spotVert) != MapRules.OceanTerrain &&
								(chart.OwnerBits(spotHoriz, spotVert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spotHoriz, spotVert) & 1) == 0)
							{
								MoveStack(game, participantIdx, troopIdx, spotHoriz, spotVert);
								stranger = host.Player;
								break;
							}
						}

						if (mode <= 1)
						{
							// A diplomat in front of the other stack (or only diplomats)
							var hostMembers = Members(game, host.Player, host.Troop);

							for (int idx = 0; idx < hostMembers.Count; idx++)
							{
								if (idx == 0 || allDiplomats)
									game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(host.Player, hostMembers[idx]) + UnitMemory.Kind), UnitKindTable.Diplomat);
							}
						}
					}

					if (mode == 6 && harbour != null)
					{
						game.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz),
							UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)harbour.Place.X);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)harbour.Place.Y);
						chart.AssignOwnerBits(harbour.Place.X, harbour.Place.Y, MapLayers.UnitsPresent | participantIdx);
					}

					if (mode == 3)
					{
						int unitHoriz = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
						int unitVert = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);

						for (int direction = 1; direction <= 8; direction++)
						{
							int seaHoriz = (unitHoriz + chart.DirectionHoriz(direction) + 80) % 80;
							int seaVert = unitVert + chart.DirectionVert(direction);

							if (seaVert > 0 && seaVert < 49 && chart.Terrain(seaHoriz, seaVert) == MapRules.OceanTerrain && (chart.OwnerBits(seaHoriz, seaVert) & MapLayers.UnitsPresent) == 0)
							{
								MoveStack(game, participantIdx, troopIdx, seaHoriz, seaVert);
								break;
							}
						}
					}

					if (mode == 7 && emptyTown != null)
					{
						game.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz),
							UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)emptyTown.Place.X);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)emptyTown.Place.Y);
						chart.AssignOwnerBits(emptyTown.Place.X, emptyTown.Place.Y, MapLayers.UnitsPresent | participantIdx);
					}

					if (mode == 7 && heading <= 6)
					{
						// An own lone land unit next to the unit (else the nearest is farther)
						int unitHoriz = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
						int unitVert = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);
						var mates = Enumerable.Range(0, 128).Where(troop => troop != troopIdx &&
							Group(game, participantIdx, troop, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
							UnitByte(game, participantIdx, troop, UnitMemory.FollowingInStack) == 0xff).ToList();

						for (int turn = 0; turn < 8 && mates.Count > 0; turn++)
						{
							int direction = (heading - 1 + turn) % 8 + 1;
							int spotHoriz = (unitHoriz + chart.DirectionHoriz(direction) + 80) % 80;
							int spotVert = unitVert + chart.DirectionVert(direction);

							if (spotVert > 0 && spotVert < 49 && chart.Terrain(spotHoriz, spotVert) != MapRules.OceanTerrain &&
								(chart.OwnerBits(spotHoriz, spotVert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spotHoriz, spotVert) & 1) == 0)
							{
								int mate = mates[pick % mates.Count];
								game.MapOps.DiscardUnitFromTile(participantIdx, (short)mate, UnitByte(game, participantIdx, mate, UnitMemory.Horiz), UnitByte(game, participantIdx, mate, UnitMemory.Vert));
								game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, mate) + UnitMemory.Horiz), (byte)spotHoriz);
								game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, mate) + UnitMemory.Vert), (byte)spotVert);
								chart.AssignOwnerBits(spotHoriz, spotVert, MapLayers.UnitsPresent | participantIdx);
								break;
							}
						}
					}

					if (mode is >= 3 and <= 6)
					{
						// A foreign land unit on the land next to the unit
						int unitHoriz = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
						int unitVert = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);

						for (int turn = 0; turn < 8; turn++)
						{
							int direction = (heading - 1 + turn) % 8 + 1;
							int spotHoriz = (unitHoriz + chart.DirectionHoriz(direction) + 80) % 80;
							int spotVert = unitVert + chart.DirectionVert(direction);

							if (spotVert > 0 && spotVert < 49 && chart.Terrain(spotHoriz, spotVert) != MapRules.OceanTerrain &&
								(chart.OwnerBits(spotHoriz, spotVert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spotHoriz, spotVert) & 1) == 0)
							{
								stranger = MoveLoneStranger(game, participantIdx, spotHoriz, spotVert, pick);
								break;
							}
						}
					}

					if (mode is >= 4 and <= 6)
					{
						// A foreign ship (its mark on the map) on the sea next to the ship: the ship does not go on to its destination
						int unitHoriz = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
						int unitVert = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);

						for (int turn = 0; turn < 8; turn++)
						{
							int direction = (heading + 3 + turn) % 8 + 1;
							int spotHoriz = (unitHoriz + chart.DirectionHoriz(direction) + 80) % 80;
							int spotVert = unitVert + chart.DirectionVert(direction);

							if (spotVert > 0 && spotVert < 49 && chart.Terrain(spotHoriz, spotVert) == MapRules.OceanTerrain &&
								(chart.OwnerBits(spotHoriz, spotVert) & MapLayers.UnitsPresent) == 0)
							{
								chart.AssignOwnerBits(spotHoriz, spotVert, MapLayers.UnitsPresent | ((participantIdx + 1 + pick % 6) % 8));
								break;
							}
						}
					}

					if (submarine)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), UnitKindTable.Submarine);

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHeading), unitHeading);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);

					for (int idx = 0; idx < 20 * 13; idx++)
						game.Processor.EmitUInt8(0x3772, (ushort)(0x2438 + idx), pattern[idx]);

					for (int other = 0; other < 8; other++)
					{
						ushort[] own = game.World.Participants[participantIdx].Relations;
						own[other] = (ushort)((own[other] & ~3) | relations[other]);
					}

					// (at war with the stranger, mostly)
					if (stranger >= 0 && relations[0] != 0)
					{
						game.World.Participants[participantIdx].Relations[stranger] &= 0xfffd;
						game.World.Participants[stranger].Relations[participantIdx] &= 0xfffd;
					}
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Bot.ShiftAIUnit(participantIdx, troopIdx),
				Register.Kept | Register.Acc,
				// What the look met, from the calls and the locals left in the frame
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					ushort Local(int local) => pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - local));
					bool looked = Local(0x40) == 9;
					List<string> met = [];

					if (looked)
						met.Add("looked");

					if (Local(0x20) != 0x1234)
						met.Add("diplomat");

					if (looked && mode == 2 && Delta("TallyUnitsByDomainInStack") > 0)
						met.Add("own stack");

					if (Delta("MayOccupyTile") > 0)
						met.Add("may occupy");

					if (looked && mode == 3 && Delta("ContainsHut") > 0)
						met.Add("at sea");

					if (mode == 4 && Delta("SettleBattle") > 0)
						met.Add("ship by land");

					if (looked && mode == 5)
						met.Add("submarine");


					if (mode == 7 && game.Processor.RegAcc.Value16 == 0x20 && Delta("SendUnitToStrategicLocation") > 0)
						met.Add("near own unit");

					if (looked && mode == 6)
						met.Add("ship in city");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
