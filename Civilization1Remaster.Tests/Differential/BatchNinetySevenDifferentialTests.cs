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

public sealed class BatchNinetySevenDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnMovesMatchOriginalCode()
	{
		// The move keys of the human (arrows, keypad, 'c', the marked tile of 'g' moved with them) and a barbarian that
		// pillages a tile of the human on its way
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckMoves(bay, ways);

		foreach (string way in new[] { "moved", "center", "marked tile", "barbarian pillage", "barbarian" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckMoves(int bay, HashSet<string> ways)
	{
		Check(bay, 16, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			MapLayers sight = engine.MapOps.Chart;
			// A barbarian turn now and then: a barbarian unit on an irrigated tile next to a city of the human
			bool barbarian = chance.Next(3) == 0;
			int[] keys = { 0x4800, 0x4900, 0x4d00, 0x5100, 0x5000, 0x4f00, 0x4b00, 0x4700, 'c', 0x20, 0x0d, 0x31 };
			List<HumanTurnScript.Input> inputs = [];

			for (int idx = chance.Next(4, 14); idx > 0; idx--)
			{
				if (chance.Next(6) == 0)
				{
					// The marked tile moved with the arrows, then GoTo off
					inputs.Add(new HumanTurnScript.Input('g'));

					for (int step = chance.Next(1, 5); step > 0; step--)
						inputs.Add(new HumanTurnScript.Input(keys[chance.Next(8)]));

					inputs.Add(new HumanTurnScript.Input('g'));
				}
				else
				{
					inputs.Add(new HumanTurnScript.Input(keys[chance.Next(keys.Length)]));
				}
			}

			var towns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == person).ToList();
			var spots = towns.SelectMany(town => Enumerable.Range(1, 8).Select(direction =>
					(Horiz: (town.Place.X + sight.DirectionHoriz(direction) + 80) % 80, Vert: town.Place.Y + sight.DirectionVert(direction))))
				.Where(spot => spot.Vert > 0 && spot.Vert < 49 && sight.Terrain(spot.Horiz, spot.Vert) != MapRules.OceanTerrain &&
					(sight.OwnerBits(spot.Horiz, spot.Vert) & MapLayers.UnitsPresent) == 0 && (sight.Improvements(spot.Horiz, spot.Vert) & 1) == 0)
				.ToList();
			var spot = spots.Count > 0 ? spots[chance.Next(spots.Count)] : (Horiz: -1, Vert: -1);
			byte barbarianKind = (byte)new[] { 1, 2, 3, 6, 7, 0x1a }[chance.Next(6)];
			int improvement = new[] { 2, 4, 6, 8 }[chance.Next(4)];
			short participantIdx = barbarian && spot.Horiz >= 0 ? (short)0 : person;
			var positions = Enumerable.Range(0, 128).Select(troop => (UnitByte(engine, person, troop, UnitMemory.Horiz), UnitByte(engine, person, troop, UnitMemory.Vert))).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({participantIdx}), keys {string.Join(" ", inputs.Select(input => input.Key.ToString("x")))}, barbarian {barbarianKind} at {spot}",
				game =>
				{
					HumanTurnScript.Install(game, inputs);

					if (participantIdx == 0)
					{
						// A barbarian unit (the slot 0 of the barbarians) on the tile
						for (int troop = 0; troop < 128; troop++)
						{
							if (UnitByte(game, 0, troop, UnitMemory.Kind) != 0xff)
								game.MapOps.DiscardUnitFromTile(0, (short)troop, UnitByte(game, 0, troop, UnitMemory.Horiz), UnitByte(game, 0, troop, UnitMemory.Vert));

							game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(0, troop) + UnitMemory.Kind), 0xff);
						}

						ushort unit = UnitMemory.Address(0, 0);
						byte[] record = { 0, (byte)spot.Horiz, (byte)spot.Vert, barbarianKind, 3, 0, 0xff, 0xff, 0xff, 0, 0xff, 0xff };

						for (int offset = 0; offset < record.Length; offset++)
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + offset), record[offset]);

						game.MapOps.Chart.AssignOwnerBits(spot.Horiz, spot.Vert, MapLayers.UnitsPresent);
						game.VideoCard.AssignDot(2, spot.Horiz, spot.Vert + 100, (byte)(game.MapOps.Chart.LowImprovements(spot.Horiz, spot.Vert) | improvement), 0);
					}
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(participantIdx),
				game => game.TurnLogic.HandlePlayerTurn(participantIdx),
				Register.Kept,
				// What the moves did, from the units, the tile and the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (participantIdx == 0)
					{
						met.Add("barbarian");

						if ((game.MapOps.Chart.Improvements(spot.Horiz, spot.Vert) & improvement & 6) == 0 && (improvement & 6) != 0)
							met.Add("barbarian pillage");
					}
					else
					{
						if (Enumerable.Range(0, 128).Any(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
							(UnitByte(game, person, troop, UnitMemory.Horiz), UnitByte(game, person, troop, UnitMemory.Vert)) != positions[troop]))
							met.Add("moved");

						if (inputs.Any(input => input.Key == 'c') && Delta("FocusMapView") > 0)
							met.Add("center");

						if (inputs.Any(input => input.Key == 'g') && Delta("RepaintMapTile") > 0)
							met.Add("marked tile");
					}

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
