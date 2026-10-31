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

public sealed class BatchHundredOneDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnAttacksMatchOriginalCode()
	{
		// The attacks of the first unit of the human: with fewer than 3 moves (confirmed or cancelled), on a foreign unit or
		// city (walls, size, the easiest level), a bomber; the attacker may be lost, the city may shrink or be destroyed
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckAttacks(bay, ways);

		foreach (string way in new[] { "partial", "cancelled", "battle", "attacker lost", "city shrank", "city destroyed" })
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

	private void CheckAttacks(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			bool city = chance.Next(2) == 0;
			byte steps = (byte)new[] { 1, 2, 3, 6 }[chance.Next(4)];
			byte kind = (byte)new[] { 2, 3, 7, 11, 0xf }[chance.Next(5)];
			bool walls = chance.Next(3) == 0;
			sbyte size = (sbyte)chance.Next(1, 4);
			short hardness = (short)chance.Next(0, 2);
			int pick = chance.Next(1000);
			int[] answers = new[] { new[] { 0x0d }, new[] { 0x5000, 0x0d }, new[] { 0x31 } }[chance.Next(3)];
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			List<HumanTurnScript.Input> inputs = [];
			int moverIdx = -1;
			int townIdx = -1;

			return new Scenario(
				$"HandlePlayerTurn({person}), city {city}, steps {steps}, kind {kind:x}, walls {walls}, size {size}, hardness {hardness}",
				game =>
				{
					IMapPlanes chart = game.MapOps.Chart;
					inputs.Clear();
					townIdx = -1;
					game.World.HardnessTier = hardness;
					int mover = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);
					moverIdx = mover;
					var lone = Enumerable.Range(1, 7).Where(player => player != person)
						.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: troop)))
						.Where(other => UnitByte(game, other.Player, other.Troop, UnitMemory.Kind) is byte unitKind && unitKind != 0xff && unitKind < 0xe &&
							UnitByte(game, other.Player, other.Troop, UnitMemory.FollowingInStack) == 0xff)
						.ToList();

					if (mover < 0 || lone.Count == 0)
					{
						HumanTurnScript.Install(game, inputs);
						return;
					}

					// The target: a foreign city (with a defender moved in) or a foreign unit on land
					var targets = city
						? Enumerable.Range(0, 128).Where(idx => game.World.Towns[idx].ConditionMarker != 0xff && game.World.Towns[idx].ParticipantIdx != person)
							.Select(idx => (Horiz: (int)game.World.Towns[idx].Place.X, Vert: (int)game.World.Towns[idx].Place.Y, Town: idx)).ToList()
						: Enumerable.Range(0, 80 * 48).Select(at => (Horiz: at % 80, Vert: 1 + at / 80, Town: -1))
							.Where(tile => chart.Terrain(tile.Horiz, tile.Vert) != MapRules.OceanTerrain && (chart.OwnerBits(tile.Horiz, tile.Vert) & MapPlanes.UnitsPresent) == 0 &&
								(chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0).Take(300).ToList();
					var spots = targets.SelectMany(target => Enumerable.Range(1, 8).Select(direction => (Target: target, Direction: direction,
							From: (Horiz: (target.Horiz - chart.DirectionHoriz(direction) + 80) % 80, Vert: target.Vert - chart.DirectionVert(direction)))))
						.Where(spot => spot.From.Vert > 0 && spot.From.Vert < 49 && chart.Terrain(spot.From.Horiz, spot.From.Vert) != MapRules.OceanTerrain &&
							(chart.OwnerBits(spot.From.Horiz, spot.From.Vert) & MapPlanes.UnitsPresent) == 0 && (chart.Improvements(spot.From.Horiz, spot.From.Vert) & 1) == 0)
						.ToList();

					if (spots.Count == 0)
					{
						HumanTurnScript.Install(game, inputs);
						return;
					}

					var spot = spots[pick % spots.Count];
					Place(game, person, mover, spot.From.Horiz, spot.From.Vert);
					ushort unit = UnitMemory.Address(person, mover);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), kind);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), 2);
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, spot.From.Horiz - 7));
					game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, spot.From.Vert - 5));

					var (player, troop) = lone[(pick / 3) % lone.Count];

					if (spot.Target.Town >= 0)
					{
						var town = game.World.Towns[spot.Target.Town];
						townIdx = spot.Target.Town;
						player = town.ParticipantIdx;
						var defenders = Enumerable.Range(0, 128).Where(idx => UnitByte(game, player, idx, UnitMemory.Kind) is byte unitKind && unitKind != 0xff && unitKind < 0xe &&
							UnitByte(game, player, idx, UnitMemory.FollowingInStack) == 0xff).ToList();

						if (defenders.Count == 0)
						{
							HumanTurnScript.Install(game, inputs);
							return;
						}

						troop = defenders[(pick / 5) % defenders.Count];
						town.TrueExtent = size;
						town.StructureMarkers0 = (ushort)(walls ? town.StructureMarkers0 | 0x80 : town.StructureMarkers0 & ~0x80);
					}

					if ((chart.OwnerBits(spot.Target.Horiz, spot.Target.Vert) & MapPlanes.UnitsPresent) == 0)
						Place(game, player, troop, spot.Target.Horiz, spot.Target.Vert);

					inputs.Add(new HumanTurnScript.Input(0x31));
					inputs.Add(new HumanTurnScript.Input(Arrows[spot.Direction]));
					inputs.AddRange(answers.Select(key => new HumanTurnScript.Input(key)));
					HumanTurnScript.Install(game, inputs);

					// (0xdf60 = 1: the turn gives no new moves, so the unit keeps these)
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);
					game.Processor.EmitUInt16(0x3b01, 0xdf60, 1);
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the attack did, from the calls, the attacker and the city
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (inputs.Count == 0)
						return "no place";

					if (Delta("DisplayDialog") > 0 && Delta("SettleBattle") == 0)
						met.Add("cancelled");

					if (Delta("DisplayDialog") > 0)
						met.Add("partial");

					if (Delta("SettleBattle") > 0)
						met.Add("battle");

					if (Delta("SettleBattle") > 0 && UnitByte(game, person, moverIdx, UnitMemory.Kind) == 0xff)
						met.Add("attacker lost");

					if (townIdx >= 0 && game.World.Towns[townIdx].ConditionMarker != 0xff && game.World.Towns[townIdx].TrueExtent < size)
						met.Add("city shrank");

					if (Delta("FinalizeProduction") > 0)
						met.Add("city destroyed");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
