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

public sealed class BatchNinetyEightDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnZoneOfControlMatchesOriginalCode()
	{
		// The tile of a move and the zone of control: a foreign unit next to the first unit of the human that moves, the
		// arrows towards the tiles next to it (stopped, with the help option) or away
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckZones(bay, ways);

		foreach (string way in new[] { "stopped with help", "moved" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static readonly int[] Arrows = { 0, 0x4800, 0x4900, 0x4d00, 0x5100, 0x5000, 0x4f00, 0x4b00, 0x4700 };

	private void CheckZones(int bay, HashSet<string> ways)
	{
		Check(bay, 16, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// The foreign unit next to the unit (direction), the arrows (often to the tiles next to both), the help option
			int side = chance.Next(1, 9);
			List<HumanTurnScript.Input> inputs = Enumerable.Range(0, chance.Next(3, 10))
				.Select(_ => new HumanTurnScript.Input(chance.Next(3) == 0 ? 0x20 : Arrows[chance.Next(3) == 0 ? chance.Next(1, 9) : (side + new[] { 1, 7, 2, 6 }[chance.Next(4)] - 1) % 8 + 1]))
				.ToList();
			bool help = chance.Next(2) == 0;
			int strangerPick = chance.Next(1000);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			var positions = Enumerable.Range(0, 128).Select(troop => (UnitByte(engine, person, troop, UnitMemory.Horiz), UnitByte(engine, person, troop, UnitMemory.Vert))).ToArray();

			return new Scenario(
				$"HandlePlayerTurn({person}), side {side}, keys {string.Join(" ", inputs.Select(input => input.Key.ToString("x")))}, help {help}",
				game =>
				{
					HumanTurnScript.Install(game, inputs);
					game.World.SessionPrefMarkers = (short)((game.World.SessionPrefMarkers & ~1) | (help ? 1 : 0));
					IMapPlanes chart = game.MapOps.Chart;

					// The first unit of the human that moves, a land unit on land outside a city
					int first = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);

					if (first < 0)
						return;

					int horizLoc = UnitByte(game, person, first, UnitMemory.Horiz);
					int vertLoc = UnitByte(game, person, first, UnitMemory.Vert);
					// (the view on the unit: off the view it would wait for the second pass)
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, horizLoc - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, vertLoc - 5));
					int strangerHoriz = (horizLoc + chart.DirectionHoriz(side) + 80) % 80;
					int strangerVert = vertLoc + chart.DirectionVert(side);

					if (strangerVert <= 0 || strangerVert >= 49 || chart.Terrain(strangerHoriz, strangerVert) == MapRules.OceanTerrain ||
						(chart.OwnerBits(strangerHoriz, strangerVert) & MapPlanes.UnitsPresent) != 0 || (chart.Improvements(strangerHoriz, strangerVert) & 1) != 0)
					{
						return;
					}

					var lone = Enumerable.Range(1, 7).Where(player => player != person)
						.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
						.Where(unit => UnitByte(game, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff && kind < 0x10 &&
							UnitByte(game, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff)
						.ToList();

					if (lone.Count == 0)
						return;

					var (player, troop) = lone[strangerPick % lone.Count];
					game.MapOps.DiscardUnitFromTile(player, troop, UnitByte(game, player, troop, UnitMemory.Horiz), UnitByte(game, player, troop, UnitMemory.Vert));
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Horiz), (byte)strangerHoriz);
					game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Vert), (byte)strangerVert);
					chart.AssignOwnerBits(strangerHoriz, strangerVert, MapPlanes.UnitsPresent | player);
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the moves met, from the calls and the units
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					// (the beep has no entry in the journal)
					if (help && Delta("DisplayHelpForHuman") > 0)
						met.Add("stopped with help");

					if (Enumerable.Range(0, 128).Any(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Horiz), UnitByte(game, person, troop, UnitMemory.Vert)) != positions[troop]))
						met.Add("moved");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
