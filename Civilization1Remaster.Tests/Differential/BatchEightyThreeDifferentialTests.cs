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

public sealed class BatchEightyThreeDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitLandingsMatchOriginalCode()
	{
		// What the loaded transports did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckLandings(bay, ways);

		foreach (string way in new[] { "landing", "unloaded", "forgot destination" })
			Assert.True(ways.Contains(way), $"No transport did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort RoleOf(GameEngine game, byte kind)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.TroopGroup));
	}

	private static int Wrap(int horizLoc)
	{
		return (horizLoc + 80) % 80;
	}

	/// <summary>
	/// Moves the stack to the sea near a pole (when there is such a place next to land), then changes the free land tiles
	/// around it: a human player's tile with improvements, or two lone land units of the player in a stack of their own
	/// </summary>
	private static void ArrangeSurroundings(GameEngine game, short participantIdx, short troopIdx, List<short> members, int pole, int[] around)
	{
		MapLayers chart = game.MapOps.Chart;
		short person = game.World.PersonParticipantIdx;
		int horizLoc = UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz);
		int vertLoc = UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert);

		if (pole >= 0)
		{
			bool Sea(int horiz, int vert) => vert >= 0 && vert < 50 && chart.Terrain(Wrap(horiz), vert) == MapRules.OceanTerrain;

			if (pole % 2 == 0)
			{
				// The maps have no land in the rows 2 and 0x2f: one tile there becomes land like the polar tile next to it
				foreach ((int polar, int edge, int sea) in new[] { (1, 2, 3), (0x30, 0x2f, 0x2e) })
				{
					var columns = Enumerable.Range(0, 80).Where(horiz => !Sea(horiz, polar) && Sea(horiz, edge) && Sea(horiz, sea)).ToList();

					if (columns.Count == 0)
						continue;

					int column = columns[pole / 2 % columns.Count];
					game.VideoCard.AssignDot(2, column, edge, (byte)game.VideoCard.ObtainDot(2, column, polar), 0);
					game.VideoCard.AssignDot(2, column, edge + 50, (byte)game.VideoCard.ObtainDot(2, column, polar + 50), 0);
				}
			}

			// Half of the time the sea next to the last land row before a pole, else an inlet (at most one sea tile around)
			var places = pole % 2 == 0
				? (from vert in new[] { 2, 3, 0x2e, 0x2f }
					from horiz in Enumerable.Range(0, 80)
					where Sea(horiz, vert) && (chart.OwnerBits(horiz, vert) & MapLayers.UnitsPresent) == 0 &&
						Enumerable.Range(1, 8).Any(heading => vert + chart.DirectionVert(heading) is int landVert && (landVert == 2 || landVert == 0x2f) &&
							!Sea(horiz + chart.DirectionHoriz(heading), landVert))
					select (Horiz: horiz, Vert: vert)).ToList()
				: (from vert in Enumerable.Range(3, 44)
					from horiz in Enumerable.Range(0, 80)
					where Sea(horiz, vert) && (chart.OwnerBits(horiz, vert) & MapLayers.UnitsPresent) == 0 &&
						Enumerable.Range(1, 8).Count(heading => Sea(horiz + chart.DirectionHoriz(heading), vert + chart.DirectionVert(heading))) <= 1
					select (Horiz: horiz, Vert: vert)).ToList();

			if (places.Count > 0)
			{
				(horizLoc, vertLoc) = places[pole % places.Count];

				foreach (short member in members.Prepend(troopIdx))
				{
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Horiz), (byte)horizLoc);
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Vert), (byte)vertLoc);
				}

				chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | participantIdx);
			}
		}

		// Lone land units of the player, away from the stack
		Queue<short> lone = new Queue<short>(Enumerable.Range(0, 128).Select(troop => (short)troop)
			.Where(troop => troop != troopIdx && !members.Contains(troop) && UnitByte(game, participantIdx, troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
				UnitByte(game, participantIdx, troop, UnitMemory.FollowingInStack) == 0xff &&
				game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.LandGroup)) == UnitKindTable.LandUnits));

		for (int heading = 1; heading <= 8; heading++)
		{
			int landHoriz = Wrap(horizLoc + chart.DirectionHoriz(heading));
			int landVert = vertLoc + chart.DirectionVert(heading);

			if (landVert < 0 || landVert >= 50 || chart.Terrain(landHoriz, landVert) == MapRules.OceanTerrain ||
				(chart.OwnerBits(landHoriz, landVert) & MapLayers.UnitsPresent) != 0 || around[heading] == 0)
			{
				continue;
			}

			if (around[heading] < 16)
			{
				if (person == participantIdx)
					continue;

				chart.AssignOwnerBits(landHoriz, landVert, person);
				game.MapOps.DiscardTileImprovements(0xff, landHoriz, landVert);
				game.MapOps.AssignTileImprovements((ushort)around[heading], landHoriz, landVert);
			}
			else if (lone.Count >= 2)
			{
				short first = lone.Dequeue();
				short second = lone.Dequeue();

				foreach (short troop in new[] { first, second })
				{
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troop) + UnitMemory.Horiz), (byte)landHoriz);
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troop) + UnitMemory.Vert), (byte)landVert);
				}

				game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, first) + UnitMemory.FollowingInStack), (byte)second);
				game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, second) + UnitMemory.FollowingInStack), (byte)first);
				chart.AssignOwnerBits(landHoriz, landVert, MapLayers.UnitsPresent | participantIdx);
			}
		}
	}

	private void CheckLandings(int bay, HashSet<string> ways)
	{
		Check(bay, 48, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// A transport (role 5) of a computer player with a stack (its land units on board), as the save has it
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff && RoleOf(engine, kind) is 5 &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff)
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

			byte steps = (byte)chance.Next(0, 7);
			// The destination: none, a city of the saved game, or the place of the transport itself
			var towns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff).ToList();
			int destination = chance.Next(3);
			var town = towns.Count > 0 ? towns[chance.Next(towns.Count)] : null;
			byte travelHoriz = destination == 0 || town == null ? (byte)0xff : destination == 1 ? (byte)town.Place.X : UnitByte(engine, participantIdx, troopIdx, UnitMemory.Horiz);
			byte travelVert = destination == 0 || town == null ? (byte)0 : destination == 1 ? (byte)town.Place.Y : UnitByte(engine, participantIdx, troopIdx, UnitMemory.Vert);
			short round = (short)(chance.Next(0, 100) * 4 + (chance.Next(2) == 0 ? 0 : chance.Next(1, 4)));
			bool peace = chance.Next(2) == 0;
			short[] strength = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 32)).ToArray();
			short[] approach = Enumerable.Range(0, 16).Select(_ => (short)new[] { 0, 1, 2, 5 }[chance.Next(4)]).ToArray();
			short[] sites = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(3, 8)).ToArray();
			// The stack moved to the sea near a pole or into an inlet (-1: stays), and what the land tiles around get: nothing, a human player's
			// tile with improvements (the bits in 1 - 15), or two own land units (16)
			int pole = chance.Next(2) == 0 ? chance.Next(1000) : -1;
			int[] around = Enumerable.Range(0, 9).Select(_ => chance.Next(3) switch { 0 => 0, 1 => new[] { 1, 2, 8, 9 }[chance.Next(4)], _ => 16 }).ToArray();
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) with {members.Count} more, steps {steps}, travel {travelHoriz}/{travelVert}, round {round}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					// The units on board are not fortified: they want to land
					foreach (short member in members)
					{
						ushort status = (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Status);
						game.Processor.EmitUInt8(0x3b01, status, (byte)(game.Processor.PullUInt8(0x3b01, status) & ~8));
					}

					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), travelHoriz);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), travelVert);
					game.World.RoundTally = round;

					if (person != participantIdx)
					{
						game.World.Participants[participantIdx].Relations[person] =
							(ushort)(peace ? game.World.Participants[participantIdx].Relations[person] | 2 : game.World.Participants[participantIdx].Relations[person] & ~2);
					}

					for (int continentIdx = 0; continentIdx < 16; continentIdx++)
					{
						game.World.Participants[participantIdx].Landmasses[continentIdx].Assault = strength[continentIdx];
						game.World.Participants[participantIdx].Landmasses[continentIdx].Approach = approach[continentIdx];
						game.World.Landmasses[continentIdx].ConstructionSpotTally = sites[continentIdx];
					}

					ArrangeSurroundings(game, participantIdx, troopIdx, members, pole, around);
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// What the transport did: entered a landing tile, unloaded its units, forgot its destination (then the code after chooses
				// another one), or else
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					ushort result = game.Processor.RegAcc.Value16;
					ushort entered = pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x3a));
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					byte travelAfter = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz));
					byte stepsAfter = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps));
					string way = Delta("MayAIEnterTile") > 0 && result == entered && (short)result > 0 && result != 0x75 ? "landing"
						: result == 0x75 && stepsAfter == 0 && travelAfter == 0xff ? "unloaded"
						: travelHoriz != 0xff && travelAfter != travelHoriz && result != 0x75 ? "forgot destination"
						: "other";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
