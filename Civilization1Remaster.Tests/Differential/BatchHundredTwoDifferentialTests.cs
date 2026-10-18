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

public sealed class BatchHundredTwoDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnMoveCostsMatchOriginalCode()
	{
		// The cost of a move of the first unit of the human: roads and railroads, the terrain (a die with fewer moves than
		// full), a ship to land, a nuclear weapon or a bomber into a foreign city
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckCosts(bay, ways);

		foreach (string way in new[] { "moved", "road", "free", "stopped by the terrain", "ship refused", "nuclear", "plane refused" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static readonly int[] Arrows = { 0, 0x4800, 0x4900, 0x4d00, 0x5100, 0x5000, 0x4f00, 0x4b00, 0x4700 };

	private static void Place(GameEngine game, short participantIdx, int troopIdx, int horizLoc, int vertLoc)
	{
		ushort unit = UnitMemory.Address(participantIdx, troopIdx);
		game.MapOps.DiscardUnitFromTile(participantIdx, (short)troopIdx, UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz), UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert));
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
		game.MapOps.Chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | participantIdx);
	}

	private void CheckCosts(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0 a land unit (roads and railroads on the two tiles), 1 a ship to land, 2 a nuclear weapon, 3 a bomber into a city
			int mode = new[] { 0, 0, 0, 1, 2, 3 }[chance.Next(6)];
			byte steps = (byte)new[] { 1, 2, 3, 4, 6, 9 }[chance.Next(6)];
			int[] roads = { new[] { 0, 8, 0x18, 0x18 }[chance.Next(4)], new[] { 0, 8, 8, 0x18 }[chance.Next(4)] };
			int pick = chance.Next(1000);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			List<HumanTurnScript.Input> inputs = [];
			int moverIdx = -1;
			(int Horiz, int Vert) start = (-1, -1);

			return new Scenario(
				$"HandlePlayerTurn({person}), mode {mode}, steps {steps}, roads {roads[0]:x}/{roads[1]:x}",
				game =>
				{
					MapLayers chart = game.MapOps.Chart;
					inputs.Clear();
					int mover = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);
					moverIdx = mover;
					bool fromSea = mode == 1;
					bool toCity = mode >= 2;
					var targets = toCity
						? Enumerable.Range(0, 128).Where(idx => game.World.Towns[idx].ConditionMarker != 0xff && game.World.Towns[idx].ParticipantIdx != person &&
								(mode == 2 || (chart.OwnerBits(game.World.Towns[idx].Place.X, game.World.Towns[idx].Place.Y) & MapLayers.UnitsPresent) == 0))
							.Select(idx => (Horiz: (int)game.World.Towns[idx].Place.X, Vert: (int)game.World.Towns[idx].Place.Y)).ToList()
						: Enumerable.Range(0, 80 * 48).Select(at => (Horiz: at % 80, Vert: 1 + at / 80))
							.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) != MapRules.OceanTerrain && (chart.OwnerBits(tile.Horiz, tile.Vert) & MapLayers.UnitsPresent) == 0 &&
								(chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0).Take(400).ToList();
					var spots = targets.SelectMany(target => Enumerable.Range(1, 8).Select(direction => (Target: target, Direction: direction,
							From: (Horiz: (target.Horiz - chart.DirectionHoriz(direction) + 80) % 80, Vert: target.Vert - chart.DirectionVert(direction)))))
						.Where(spot => spot.From.Vert > 0 && spot.From.Vert < 49 && (chart.Terrain(spot.From.Horiz, spot.From.Vert) == MapRules.OceanTerrain) == fromSea &&
							(chart.OwnerBits(spot.From.Horiz, spot.From.Vert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spot.From.Horiz, spot.From.Vert) & 1) == 0)
						.ToList();

					if (mover < 0 || spots.Count == 0)
					{
						HumanTurnScript.Install(game, inputs);
						return;
					}

					var spot = spots[pick % spots.Count];
					Place(game, person, mover, spot.From.Horiz, spot.From.Vert);
					ushort unit = UnitMemory.Address(person, mover);
					byte kind = mode switch { 1 => 0x18, 2 => 0x19, 3 => 0xf, _ => game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) };

					if (mode == 0 && game.Processor.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.LandGroup)) != UnitKindTable.LandUnits)
						kind = 2;

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), kind);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), 4);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);
					game.Processor.EmitUInt16(0x3b01, 0xdf60, 1);
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, spot.From.Horiz - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, spot.From.Vert - 5));
					start = spot.From;

					if (mode == 0)
					{
						game.VideoCard.AssignDot(2, spot.From.Horiz, spot.From.Vert + 100, (byte)((chart.LowImprovements(spot.From.Horiz, spot.From.Vert) & ~8) | (roads[0] & 8)), 0);
						game.VideoCard.AssignDot(2, spot.From.Horiz, spot.From.Vert + 150, (byte)((chart.HighImprovements(spot.From.Horiz, spot.From.Vert) & ~1) | (roads[0] >> 4)), 0);
						game.VideoCard.AssignDot(2, spot.Target.Horiz, spot.Target.Vert + 100, (byte)((chart.LowImprovements(spot.Target.Horiz, spot.Target.Vert) & ~8) | (roads[1] & 8)), 0);
					}

					inputs.Add(new HumanTurnScript.Input(0x31));
					inputs.Add(new HumanTurnScript.Input(Arrows[spot.Direction]));
					HumanTurnScript.Install(game, inputs);
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the move cost, from the mover and the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (inputs.Count == 0)
						return "no place";

					bool moved = moverIdx >= 0 && UnitByte(game, person, moverIdx, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, moverIdx, UnitMemory.Horiz), UnitByte(game, person, moverIdx, UnitMemory.Vert)) != ((byte)start.Horiz, (byte)start.Vert);
					byte left = moverIdx >= 0 ? UnitByte(game, person, moverIdx, UnitMemory.LeftoverSteps) : (byte)0;

					if (mode == 0 && moved)
						met.Add("moved");

					if (mode == 0 && moved && roads[0] != 0 && roads[1] != 0 && left == steps - 1)
						met.Add("road");

					// (the keys after the move leave the unit without moves: the free move shows only in its mutations)
					if (mode == 0 && moved && roads[0] == 0x18 && roads[1] != 0)
						met.Add("free");

					if (mode == 0 && !moved && steps < 3 && left == 0)
						met.Add("stopped by the terrain");

					if (mode == 1 && !moved)
						met.Add("ship refused");

					if (Delta("ExplodeNuclearWeapon") > 0)
						met.Add("nuclear");

					if (mode == 3 && Delta("DisplayHelpForHuman") > 0)
						met.Add("plane refused");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x80), 0x80)]);
		}, output, [], null);
	}
}
