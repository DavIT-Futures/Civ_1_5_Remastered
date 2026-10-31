using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchHundredElevenDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnEndOfMoveMatchesOriginalCode()
	{
		// The first unit of the human ends its move (Space): a plane on land, in a city of the human or on a carrier, with
		// some fuel left; a trireme on open sea or by the land
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckEndOfMove(bay, ways);

		foreach (string way in new[] { "refueled", "burned", "plane lost", "trireme lost", "trireme kept", "waited" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static void Place(GameEngine game, short participantIdx, int troopIdx, int horizLoc, int vertLoc)
	{
		ushort unit = UnitMemory.Address(participantIdx, troopIdx);
		game.MapOps.RemoveUnitFromTile(participantIdx, (short)troopIdx, UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz), UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert));
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
		game.MapOps.Chart.AssignOwnerBits(horizLoc, vertLoc, MapPlanes.UnitsPresent | participantIdx);
	}

	private void CheckEndOfMove(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0 a plane on land, 1 in a city, 2 on a carrier, 3 a trireme on open sea, 4 by the land, 5 a land unit with 1 - 3
			// moves left that waits ('w': it comes again in the second pass)
			int place = chance.Next(6);
			byte kind = (byte)(place < 3 ? new[] { 0xe, 0xf }[chance.Next(2)] : place < 5 ? 0x10 : 2);
			byte steps = (byte)(1 + chance.Next(3));
			byte fuel = (byte)chance.Next(3);
			int pick = chance.Next(1000);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			int moverIdx = -1;

			return new Scenario(
				$"HandlePlayerTurn({person}), place {place}, kind {kind}, fuel {fuel}, steps {steps}",
				game =>
				{
					IMapPlanes chart = game.MapOps.Chart;
					moverIdx = -1;
					game.Processor.EmitUInt16(0x3b01, 0xdc48, 0);
					// (0xdf60 = 1: the turn gives no new moves, so the waiting unit keeps its steps)
					game.Processor.EmitUInt16(0x3b01, 0xdf60, (ushort)(place == 5 ? 1 : 0));

					lock (game.VideoCard.Tokens)
						game.VideoCard.Tokens.Clear();

					var units = Enumerable.Range(0, 128).Where(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0).ToList();
					bool Around(int horizLoc, int vertLoc, Func<int, bool> terrain) => Enumerable.Range(1, 8)
						.Any(direction => terrain(chart.Terrain((horizLoc + chart.DirectionHoriz(direction) + 80) % 80, vertLoc + chart.DirectionVert(direction))));
					bool Free(int horizLoc, int vertLoc) => (chart.OwnerBits(horizLoc, vertLoc) & MapPlanes.UnitsPresent) == 0;
					var tiles = Enumerable.Range(0, 80 * 46).Select(at => (Horiz: at % 80, Vert: 2 + at / 80));
					var spots = place switch
					{
						0 or 5 => tiles.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) != MapRules.OceanTerrain && Free(tile.Horiz, tile.Vert) &&
							(chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0),
						1 => game.World.Towns.Where(town => town.ParticipantIdx == person && town.ConditionMarker != 0xff).Select(town => (town.Place.X, town.Place.Y)),
						3 => tiles.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) == MapRules.OceanTerrain && Free(tile.Horiz, tile.Vert) &&
							!Around(tile.Horiz, tile.Vert, terrain => terrain != MapRules.OceanTerrain)),
						_ => tiles.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) == MapRules.OceanTerrain && Free(tile.Horiz, tile.Vert) &&
							Around(tile.Horiz, tile.Vert, terrain => terrain != MapRules.OceanTerrain)),
					};
					var spotList = spots.Take(300).ToList();

					if (units.Count < 2 || spotList.Count == 0)
					{
						HumanTurnScript.Install(game, []);
						return;
					}

					var spot = spotList[pick % spotList.Count];
					int mover = units[0];
					moverIdx = mover;
					Place(game, person, mover, spot.Item1, spot.Item2);
					ushort unit = UnitMemory.Address(person, mover);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), kind);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), fuel);

					if (place == 5)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack), (byte)mover);

					if (place == 2)
					{
						// A carrier of the human under the plane, the two in one stack
						int carrier = units[1];
						Place(game, person, carrier, spot.Item1, spot.Item2);
						ushort carried = UnitMemory.Address(person, carrier);
						game.Processor.EmitUInt8(0x3b01, (ushort)(carried + UnitMemory.Kind), 0x17);
						game.Processor.EmitUInt8(0x3b01, (ushort)(carried + UnitMemory.Status), 1);
						game.Processor.EmitUInt8(0x3b01, (ushort)(carried + UnitMemory.TravelHoriz), 0xff);
						game.Processor.EmitUInt8(0x3b01, (ushort)(carried + UnitMemory.FollowingInStack), (byte)mover);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack), (byte)carrier);
					}

					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, spot.Item1 - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, spot.Item2 - 5));
					HumanTurnScript.Install(game, new[] { 0x31, 0x1b, place == 5 ? 0x77 : 0x20 }.Select(key => new HumanTurnScript.Input(key)));
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What became of the mover, from its slot
				game =>
				{
					if (moverIdx < 0)
						return "no place";

					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					bool alive = UnitByte(game, person, moverIdx, UnitMemory.Kind) == kind;
					byte rounds = UnitByte(game, person, moverIdx, UnitMemory.UnusualSteps);
					string met;

					if (kind == 2)
						met = Delta("RenderUnitInfo") > 1 ? "waited" : "not waited";
					else if (kind == 0x10)
						met = alive ? "trireme kept" : "trireme lost";
					else if (!alive)
						met = "plane lost";
					else if (rounds >= fuel)
						met = "refueled";
					else
						met = rounds == fuel - 1 ? "burned" : "plane other";

					ways.Add(met);
					return met;
				});
		}, output, [], null);
	}
}
