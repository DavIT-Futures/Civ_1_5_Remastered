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

public sealed class BatchEightySixDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitBeforeFightMatchesOriginalCode()
	{
		// What the settlers, defenders and attackers decided before the look around, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckDecisions(bay, ways);

		foreach (string way in new[] { "goes on", "fortifies", "builds city", "joins city", "looks around" })
			Assert.True(ways.Contains(way), $"No unit did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort KindField(GameEngine game, byte kind, int field)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + field));
	}

	/// <summary>A lone land unit of another computer player moves to the free land tile</summary>
	private static void PlaceStranger(GameEngine game, short participantIdx, int horizLoc, int vertLoc, int pick)
	{
		IMapLayers chart = game.MapOps.Chart;
		short person = game.World.PersonParticipantIdx;

		if (vertLoc <= 0 || vertLoc >= 49 || chart.Terrain(horizLoc, vertLoc) == MapRules.OceanTerrain || (chart.OwnerBits(horizLoc, vertLoc) & MapLayers.UnitsPresent) != 0)
			return;

		var lone = Enumerable.Range(1, 7).Where(player => player != participantIdx && player != person)
			.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
			.Where(unit => UnitByte(game, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
				UnitByte(game, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff &&
				KindField(game, kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			.ToList();

		if (lone.Count == 0)
			return;

		var (player, troop) = lone[pick % lone.Count];
		game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(player, troop) + UnitMemory.Vert), (byte)vertLoc);
		chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | player);
	}

	private void CheckDecisions(int bay, HashSet<string> ways)
	{
		Check(bay, 128, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// Settlers (role 0) or defenders (2) of a computer player on land, or its attacking ships (3: the land attackers
			// leave ShiftAIUnit earlier), as the save has it; the defenders half of the time in a stack or at sea
			int wanted = new[] { 0, 2, 3 }[chance.Next(3)];
			int place = chance.Next(4);
			IMapLayers sight = engine.MapOps.Chart;
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
					KindField(engine, kind, UnitKindTable.LandGroup) == (wanted == 3 ? UnitKindTable.SeaUnits : UnitKindTable.LandUnits) &&
					KindField(engine, kind, UnitKindTable.TroopGroup) == wanted)
				.ToList();
			var special = wanted != 2 ? candidates
				: place == 1 ? candidates.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff &&
					(sight.Improvements(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz), UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)) & 1) == 0).ToList()
				: place == 2 ? candidates.Where(unit => sight.Terrain(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz),
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)) == MapRules.OceanTerrain).ToList()
				: candidates;

			if (special.Count > 0)
				candidates = special;

			if (candidates.Count == 0)
				return null;

			var (participantIdx, troopIdx) = candidates[chance.Next(candidates.Count)];
			// A lone defender moved 3 tiles away from an own city (place 3)
			var ownTowns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx).ToList();
			var home = wanted == 2 && place == 3 && ownTowns.Count > 0 ? ownTowns[chance.Next(ownTowns.Count)] : null;
			int homeHoriz = chance.Next(2) == 0 ? 3 : chance.Next(-3, 4);
			int homeVert = homeHoriz is 3 or -3 ? chance.Next(-3, 4) : (chance.Next(2) == 0 ? 3 : -3);
			bool travelling = chance.Next(3) == 0;
			// The settlers do not wander: every mark of the pattern is set
			bool marked = chance.Next(4) != 0;
			// (always next to a stack of defenders outside a city)
			int stranger = chance.Next(3) == 0 || (wanted == 2 && place == 1) ? chance.Next(1, 9) : -1;
			int strangerPick = chance.Next(1000);
			bool militia = chance.Next(4) == 0;
			short approach = (short)new[] { 0, 1, 1, 2, 5 }[chance.Next(5)];
			sbyte[] extents = Enumerable.Range(0, 128).Select(_ => (sbyte)chance.Next(8, 12)).ToArray();
			short[] cities = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 6)).ToArray();
			short[] sites = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 40)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);
			byte travelBefore = 0;

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) role {wanted}, travelling {travelling}, stranger {stranger}, approach {approach}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);
					// (0x5c gets 0 or 1 only when the look starts)
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0x5c), 0x1234);
					IMapLayers chart = game.MapOps.Chart;
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					int unitHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
					int unitVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

					if (home != null)
					{
						int homeH = (home.Place.X + homeHoriz + 80) % 80;
						int homeV = home.Place.Y + homeVert;

						if (homeV > 0 && homeV < 49 && chart.Terrain(homeH, homeV) != MapRules.OceanTerrain && (chart.OwnerBits(homeH, homeV) & MapLayers.UnitsPresent) == 0 &&
							game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack)) == 0xff)
						{
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)homeH);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)homeV);
							chart.AssignOwnerBits(homeH, homeV, MapLayers.UnitsPresent | participantIdx);
							(unitHoriz, unitVert) = (homeH, homeV);
						}
					}

					if (wanted == 2 && place == 2 && chart.Terrain(unitHoriz, unitVert) != MapRules.OceanTerrain &&
						game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack)) == 0xff)
					{
						// No defender at sea in the save: a lone one goes to the sea next to it
						for (int heading = 1; heading <= 8; heading++)
						{
							int seaHoriz = (unitHoriz + chart.DirectionHoriz(heading) + 80) % 80;
							int seaVert = unitVert + chart.DirectionVert(heading);

							if (seaVert > 0 && seaVert < 49 && chart.Terrain(seaHoriz, seaVert) == MapRules.OceanTerrain && (chart.OwnerBits(seaHoriz, seaVert) & MapLayers.UnitsPresent) == 0)
							{
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)seaHoriz);
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)seaVert);
								chart.AssignOwnerBits(seaHoriz, seaVert, MapLayers.UnitsPresent | participantIdx);
								(unitHoriz, unitVert) = (seaHoriz, seaVert);
								break;
							}
						}
					}

					if (stranger >= 0 && wanted == 3)
					{
						// A ship of another player next to the attacking ship
						int besideHoriz = (unitHoriz + chart.DirectionHoriz(stranger) + 80) % 80;
						int besideVert = unitVert + chart.DirectionVert(stranger);

						if (besideVert >= 0 && besideVert < 50 && chart.Terrain(besideHoriz, besideVert) == MapRules.OceanTerrain &&
							(chart.OwnerBits(besideHoriz, besideVert) & MapLayers.UnitsPresent) == 0)
						{
							chart.AssignOwnerBits(besideHoriz, besideVert, MapLayers.UnitsPresent | ((participantIdx + 1 + strangerPick % 6) % 8));
						}
					}
					else if (stranger >= 0)
					{
						PlaceStranger(game, participantIdx, (unitHoriz + chart.DirectionHoriz(stranger) + 80) % 80, unitVert + chart.DirectionVert(stranger), strangerPick);
					}

					if (militia && wanted == 2)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 1);

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), travelling ? (byte)((unitHoriz + 3) % 80) : (byte)0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), (byte)unitVert);
					travelBefore = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz));

					if (marked)
					{
						for (int idx = 0; idx < 20 * 13; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(0x2438 + idx), 3);
					}

					for (int townIdx = 0; townIdx < 128; townIdx++)
					{
						if (game.World.Towns[townIdx].ConditionMarker != 0xff)
							game.World.Towns[townIdx].TrueExtent = extents[townIdx];
					}

					for (int continentIdx = 0; continentIdx < 16; continentIdx++)
					{
						game.World.Participants[participantIdx].Landmasses[continentIdx].Approach = approach;
						game.World.Participants[participantIdx].Landmasses[continentIdx].TownTally = cities[continentIdx];
						game.World.Landmasses[continentIdx].ConstructionSpotTally = sites[continentIdx];
					}
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// What the unit decided: goes on to its destination, fortifies, builds a city, goes to its small city, or looks
				// around for a fight (the look sets 0x5c)
				game =>
				{
					ushort result = game.Processor.RegAcc.Value16;
					byte travelAfter = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.TravelHoriz));
					// (the frame of the original: the rewritten code has none)
					bool looked = pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x5c)) is 0 or 1;
					string way = result == 0x66 ? "fortifies"
						: result == 0x62 ? "builds city"
						: result == 0 && travelBefore != 0xff && travelAfter == travelBefore ? "goes on"
						: result == 0 && wanted == 0 && travelBefore == 0xff && game.World.Towns.Any(town => town.ConditionMarker != 0xff &&
							town.ParticipantIdx == participantIdx && (byte)town.Place.X == travelAfter) ? "joins city"
						: looked ? "looks around"
						: "other";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
