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

public sealed class BatchHundredDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnMoveTileMatchesOriginalCode()
	{
		// The tile of a move of the first unit of the human: boarding an own ship, the sea without one, a diplomat bribing
		// a foreign unit, a plane as the target, an attack from a ship, a submarine against land, an attack
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckTiles(bay, ways);

		foreach (string way in new[] { "boarded", "sea refused", "bribe", "plane refused", "from ship refused", "submarine refused", "attack" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static readonly int[] Arrows = { 0, 0x4800, 0x4900, 0x4d00, 0x5100, 0x5000, 0x4f00, 0x4b00, 0x4700 };

	/// <summary>Moves the unit (alone) to the tile</summary>
	private static void Place(GameEngine game, short participantIdx, int troopIdx, int horizLoc, int vertLoc)
	{
		ushort unit = UnitMemory.Address(participantIdx, troopIdx);
		game.MapOps.DiscardUnitFromTile(participantIdx, (short)troopIdx, UnitByte(game, participantIdx, troopIdx, UnitMemory.Horiz), UnitByte(game, participantIdx, troopIdx, UnitMemory.Vert));
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
		game.MapOps.Chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | participantIdx);
	}

	private void CheckTiles(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0 boarding, 1 the sea without a ship, 2 a diplomat bribing, 3 a plane as the target, 4 from a ship, 5 a submarine,
			// 6 an attack
			int mode = chance.Next(7);
			bool fighter = chance.Next(3) == 0;
			int pick = chance.Next(1000);
			int[] answers = new[] { new[] { 0x0d }, new[] { 0x31 }, new[] { 0x5000, 0x0d } }[chance.Next(3)];
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			List<HumanTurnScript.Input> inputs = [];
			(int Horiz, int Vert) start = (-1, -1);
			int moverIdx = -1;

			return new Scenario(
				$"HandlePlayerTurn({person}), mode {mode}, fighter {fighter}, answers {string.Join(" ", answers.Select(key => key.ToString("x")))}",
				game =>
				{
					MapLayers chart = game.MapOps.Chart;
					inputs.Clear();
					int mover = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);
					moverIdx = mover;
					bool wantSea = mode is 1 or 0;
					bool onSea = mode is 4 or 5;
					// A place: the mover on land (or on the sea for 4 / 5), the target next to it on the sea (0 / 1) or on land
					var places = Enumerable.Range(0, 80 * 48).Select(at => (Horiz: at % 80, Vert: 1 + at / 80))
						.Where(tile => (chart.Terrain(tile.Horiz, tile.Vert) == MapRules.OceanTerrain) == onSea &&
							(chart.OwnerBits(tile.Horiz, tile.Vert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0)
						.SelectMany(tile => Enumerable.Range(1, 8).Select(direction => (Tile: tile, Direction: direction,
							Target: (Horiz: (tile.Horiz + chart.DirectionHoriz(direction) + 80) % 80, Vert: tile.Vert + chart.DirectionVert(direction)))))
						.Where(spot => spot.Target.Vert > 0 && spot.Target.Vert < 49 &&
							(chart.Terrain(spot.Target.Horiz, spot.Target.Vert) == MapRules.OceanTerrain) == wantSea &&
							(chart.OwnerBits(spot.Target.Horiz, spot.Target.Vert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spot.Target.Horiz, spot.Target.Vert) & 1) == 0)
						.Take(400)
						.ToList();

					if (mover < 0 || places.Count == 0)
					{
						HumanTurnScript.Install(game, inputs);
						return;
					}

					var spot = places[pick % places.Count];
					Place(game, person, mover, spot.Tile.Horiz, spot.Tile.Vert);
					ushort unit = UnitMemory.Address(person, mover);
					byte moverKind = mode switch { 2 => UnitKindTable.Diplomat, 3 => (byte)(fighter ? 0xe : 2), 5 => UnitKindTable.Submarine, _ => (byte)2 };
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), moverKind);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, spot.Tile.Horiz - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, spot.Tile.Vert - 5));
					start = spot.Tile;

					if (mode == 0)
					{
						// An own transport on the sea next to it
						int ship = Enumerable.Range(0, 128).FirstOrDefault(troop => troop != mover && UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
							UnitByte(game, person, troop, UnitMemory.FollowingInStack) == 0xff, -1);

						if (ship >= 0)
						{
							Place(game, person, ship, spot.Target.Horiz, spot.Target.Vert);
							game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(person, ship) + UnitMemory.Kind), 0x18);
						}
					}
					else if (mode >= 2)
					{
						// A unit of a computer player on the target (a bomber for 3)
						var lone = Enumerable.Range(1, 7).Where(player => player != person)
							.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: troop)))
							.Where(other => UnitByte(game, other.Player, other.Troop, UnitMemory.Kind) is byte kind && kind != 0xff && kind < 0x10 &&
								UnitByte(game, other.Player, other.Troop, UnitMemory.FollowingInStack) == 0xff)
							.ToList();

						if (lone.Count > 0)
						{
							var (player, troop) = lone[(pick / 3) % lone.Count];
							Place(game, player, troop, spot.Target.Horiz, spot.Target.Vert);

							if (mode == 3)
								game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Kind), 0xf);
						}
					}

					inputs.Add(new HumanTurnScript.Input(0x31));
					inputs.Add(new HumanTurnScript.Input(Arrows[spot.Direction]));
					inputs.AddRange(answers.Select(key => new HumanTurnScript.Input(key)));
					HumanTurnScript.Install(game, inputs);
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the move met, from the calls and the mover
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					bool stayed = moverIdx >= 0 && (UnitByte(game, person, moverIdx, UnitMemory.Horiz), UnitByte(game, person, moverIdx, UnitMemory.Vert)) == ((byte)start.Horiz, (byte)start.Vert);
					List<string> met = [];

					if (inputs.Count == 0)
						return "no place";

					if (mode == 0 && Delta("TallySpecialUnitsInStack") > 0)
						met.Add("boarded");

					if (mode == 1 && stayed)
						met.Add("sea refused");

					if (Delta("BuyUnit") > 0)
						met.Add("bribe");

					if (mode == 3 && !fighter && Delta("DisplayHelpForHuman") > 0)
						met.Add("plane refused");

					if (mode == 4 && Delta("DisplayHelpForHuman") > 0)
						met.Add("from ship refused");

					if (mode == 5 && stayed && Delta("SettleBattle") == 0)
						met.Add("submarine refused");

					if (Delta("SettleBattle") > 0)
						met.Add("attack");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x80), 0x80)]);
		}, output, [], null);
	}
}
