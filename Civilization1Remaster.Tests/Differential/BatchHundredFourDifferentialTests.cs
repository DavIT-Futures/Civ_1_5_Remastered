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

public sealed class BatchHundredFourDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnTerritoryMatchesOriginalCode()
	{
		// The first unit of the human moves onto an improved tile of the territory of a computer player (at peace or not):
		// the treaty may be broken or kept; the unit is shown to that player
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckTerritory(bay, ways);

		foreach (string way in new[] { "treaty asked", "kept", "entered", "shown" })
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
		game.MapOps.Chart.AssignOwnerBits(horizLoc, vertLoc, MapPlanes.UnitsPresent | participantIdx);
	}

	private void CheckTerritory(int bay, HashSet<string> ways)
	{
		Check(bay, 20, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			short owner = (short)Enumerable.Range(1, 7).Where(player => player != person).ElementAt(chance.Next(6));
			bool peace = chance.Next(3) != 0;
			byte improvement = (byte)new[] { 2, 4, 8, 0 }[chance.Next(4)];
			int[] answers = new[] { new[] { 0x0d }, new[] { 0x5000, 0x0d }, new[] { 0x31 } }[chance.Next(3)];
			int pick = chance.Next(1000);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			List<HumanTurnScript.Input> inputs = [];
			int moverIdx = -1;
			(int Horiz, int Vert) start = (-1, -1);

			return new Scenario(
				$"HandlePlayerTurn({person}), owner {owner}, peace {peace}, improvement {improvement}",
				game =>
				{
					IMapPlanes chart = game.MapOps.Chart;
					inputs.Clear();
					int mover = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);
					moverIdx = mover;
					var spots = Enumerable.Range(0, 80 * 48).Select(at => (Horiz: at % 80, Vert: 1 + at / 80))
						.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) != MapRules.OceanTerrain && (chart.OwnerBits(tile.Horiz, tile.Vert) & MapPlanes.UnitsPresent) == 0 &&
							(chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0)
						.Take(400)
						.SelectMany(tile => Enumerable.Range(1, 8).Select(direction => (From: tile, Direction: direction,
							Target: (Horiz: (tile.Horiz + chart.DirectionHoriz(direction) + 80) % 80, Vert: tile.Vert + chart.DirectionVert(direction)))))
						.Where(spot => spot.Target.Vert > 0 && spot.Target.Vert < 49 && chart.Terrain(spot.Target.Horiz, spot.Target.Vert) != MapRules.OceanTerrain &&
							(chart.OwnerBits(spot.Target.Horiz, spot.Target.Vert) & MapPlanes.UnitsPresent) == 0 && (chart.Improvements(spot.Target.Horiz, spot.Target.Vert) & 1) == 0)
						.ToList();

					if (mover < 0 || spots.Count == 0)
					{
						HumanTurnScript.Install(game, inputs);
						return;
					}

					var spot = spots[pick % spots.Count];
					Place(game, person, mover, spot.From.Horiz, spot.From.Vert);
					ushort unit = UnitMemory.Address(person, mover);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 2);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.ShownToParticipants), 0);
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, spot.From.Horiz - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, spot.From.Vert - 5));
					start = spot.From;

					// The territory and the improvement of the target, the relation with its player
					game.VideoCard.AssignDot(2, spot.Target.Horiz + 80, spot.Target.Vert, (byte)owner, 0);
					game.VideoCard.AssignDot(2, spot.Target.Horiz, spot.Target.Vert + 100, (byte)((chart.LowImprovements(spot.Target.Horiz, spot.Target.Vert) & 1) | improvement), 0);
					ushort[] relations = game.World.Participants[person].Relations;
					relations[owner] = (ushort)(peace ? relations[owner] | 3 : (relations[owner] | 1) & ~2);
					game.World.Participants[owner].Relations[person] = relations[owner];

					inputs.Add(new HumanTurnScript.Input(0x31));
					inputs.Add(new HumanTurnScript.Input(Arrows[spot.Direction]));
					inputs.AddRange(answers.Select(key => new HumanTurnScript.Input(key)));
					HumanTurnScript.Install(game, inputs);
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the move met, from the calls and the mover
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (inputs.Count == 0)
						return "no place";

					bool moved = UnitByte(game, person, moverIdx, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, moverIdx, UnitMemory.Horiz), UnitByte(game, person, moverIdx, UnitMemory.Vert)) != ((byte)start.Horiz, (byte)start.Vert);

					if (Delta("AffirmBreakTreaty") > 0)
						met.Add("treaty asked");

					if (Delta("AffirmBreakTreaty") > 0 && !moved)
						met.Add("kept");

					if (moved)
						met.Add("entered");

					// (the move recomputes who sees the unit afterwards: the bit shows only in the mutations)
					if (moved && improvement != 0)
						met.Add("shown");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
