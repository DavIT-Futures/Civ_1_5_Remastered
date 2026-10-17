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

public sealed class BatchEightyFiveDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitSettlersMatchOriginalCode()
	{
		// What the settlers (and the slow units next to human cities) did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckSettlers(bay, ways);

		foreach (string way in new[] { "pillage", "irrigation", "mine", "road or irrigation", "fortress", "new destination" })
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

	private static void Learn(GameEngine game, short participantIdx, int scienceIdx, bool known)
	{
		ushort[] markers = game.World.Participants[participantIdx].FoundScienceMarkers;
		markers[scienceIdx >> 4] = (ushort)(known ? markers[scienceIdx >> 4] | (1 << (scienceIdx & 0xf)) : markers[scienceIdx >> 4] & ~(1 << (scienceIdx & 0xf)));
	}

	/// <summary>A lone land unit of another computer player moves to the free land tile</summary>
	private static void PlaceStranger(GameEngine game, short participantIdx, int horizLoc, int vertLoc, int pick)
	{
		MapLayers chart = game.MapOps.Chart;
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

	private void CheckSettlers(int bay, HashSet<string> ways)
	{
		Check(bay, 96, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// Settlers of a computer player alone on their tile, or (a quarter of the time) a slow land unit moved next to a
			// human city
			bool slow = chance.Next(4) == 0;
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
					UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff &&
					KindField(engine, kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					(slow ? KindField(engine, kind, UnitKindTable.ShiftTally) < 2 : KindField(engine, kind, UnitKindTable.TroopGroup) == 0))
				.ToList();
			var humanTowns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == person).ToList();

			if (candidates.Count == 0 || (slow && humanTowns.Count == 0))
				return null;

			var (participantIdx, troopIdx) = candidates[chance.Next(candidates.Count)];
			var ownTowns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx).ToList();
			// The place: as the save has it, or a land tile 0 - 2 tiles from a city (human for the slow units, else own)
			var towns = slow ? humanTowns : ownTowns;
			var town = towns.Count > 0 && (slow || chance.Next(3) != 0) ? towns[chance.Next(towns.Count)] : null;
			int offsetHoriz = chance.Next(-2, 3);
			int offsetVert = chance.Next(-2, 3);
			// The tile and the tiles around: improvements (bits 1 irrigation, 2 mine, 3 road, 6 fortress)
			int[] improvements = Enumerable.Range(0, 25).Select(_ => new[] { 0, 0, 2, 4, 8, 8, 10, 0x40 }[chance.Next(8)]).ToArray();
			bool clearing = chance.Next(2) == 0;
			bool diag = chance.Next(4) != 0;
			bool[] sciences = Enumerable.Range(0, 3).Select(_ => chance.Next(2) == 0).ToArray();
			bool travelling = chance.Next(4) == 0;
			// The marks of the wandering pattern (segment 0x3772, 0x2438: 20 x 13 bytes), the sizes of the cities, and a
			// lone unit of another player moved next to the unit
			byte[] pattern = chance.Next(2) == 0 ? Enumerable.Range(0, 20 * 13).Select(_ => (byte)chance.Next(4)).ToArray() : null;
			byte[] extents = Enumerable.Range(0, 128).Select(_ => (byte)chance.Next(1, 6)).ToArray();
			int stranger = chance.Next(3) == 0 ? chance.Next(1, 9) : -1;
			int strangerPick = chance.Next(1000);
			short round = (short)chance.Next(0, 400);
			short chief = (short)chance.Next(0, 14);
			bool atPeace = chance.Next(2) == 0;
			short approach = (short)new[] { 0, 1, 2, 5 }[chance.Next(4)];
			short[] cities = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 5)).ToArray();
			short[] sites = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 12)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);
			byte travelBefore = 0;

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) slow {slow}, near {(town == null ? "-" : $"{town.Place.X},{town.Place.Y}")} +{offsetHoriz},{offsetVert}, round {round}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);
					MapLayers chart = game.MapOps.Chart;
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);

					if (town != null)
					{
						int horizLoc = (town.Place.X + offsetHoriz + 80) % 80;
						int vertLoc = town.Place.Y + offsetVert;

						if (vertLoc > 0 && vertLoc < 49 && chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain &&
							(chart.OwnerBits(horizLoc, vertLoc) & MapLayers.UnitsPresent) == 0)
						{
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
							chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | participantIdx);
						}
					}

					int unitHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
					int unitVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

					if (stranger >= 0)
						PlaceStranger(game, participantIdx, (unitHoriz + chart.DirectionHoriz(stranger) + 80) % 80, unitVert + chart.DirectionVert(stranger), strangerPick);

					// The improvements of the 5 x 5 tiles around (not the cities)
					if (clearing)
					{
						for (int idx = 0; idx < 25; idx++)
						{
							int horizLoc = (unitHoriz + idx % 5 - 2 + 80) % 80;
							int vertLoc = unitVert + idx / 5 - 2;

							if (vertLoc < 0 || vertLoc >= 50 || (game.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
								continue;

							game.MapOps.DiscardTileImprovements(0xfe, horizLoc, vertLoc);

							if (improvements[idx] != 0)
								game.MapOps.AssignTileImprovements((ushort)improvements[idx], horizLoc, vertLoc);
						}
					}

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), travelling ? (byte)((unitHoriz + 3) % 80) : (byte)0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), (byte)unitVert);
					travelBefore = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz));
					game.World.DiagMarkers = (short)(diag ? game.World.DiagMarkers | 2 : game.World.DiagMarkers & ~2);
					game.World.RoundTally = round;

					if (pattern != null)
					{
						for (int idx = 0; idx < pattern.Length; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(0x2438 + idx), pattern[idx]);
					}

					for (int townIdx = 0; townIdx < 128; townIdx++)
					{
						if (game.World.Towns[townIdx].ConditionMarker != 0xff)
							game.World.Towns[townIdx].TrueExtent = (sbyte)extents[townIdx];
					}
					game.World.Participants[participantIdx].ChiefVisuals = chief;
					Learn(game, participantIdx, 5, sciences[0]);
					Learn(game, participantIdx, 0x13, sciences[1]);
					Learn(game, participantIdx, 0x31, sciences[2]);
					game.World.Participants[person].Relations[participantIdx] =
						(ushort)(atPeace ? game.World.Participants[person].Relations[participantIdx] | 2 : game.World.Participants[person].Relations[participantIdx] & ~2);

					for (int continentIdx = 0; continentIdx < 16; continentIdx++)
					{
						game.World.Participants[participantIdx].Landmasses[continentIdx].Approach = approach;
						game.World.Participants[participantIdx].Landmasses[continentIdx].TownTally = cities[continentIdx];
						game.World.Landmasses[continentIdx].ConstructionSpotTally = sites[continentIdx];
					}
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Bot.ShiftAIUnit(participantIdx, troopIdx),
				Register.Kept | Register.Acc,
				// What the unit did: by the order it gives, or a new destination
				game =>
				{
					ushort result = game.Processor.RegAcc.Value16;
					byte travelAfter = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.TravelHoriz));
					string way = result switch
					{
						0x50 => "pillage",
						0x69 => "irrigation",
						0x6d => "mine",
						0x72 => "road or irrigation",
						0x70 => "fortress",
						0 when travelAfter != 0xff && travelAfter != travelBefore => "new destination",
						_ => $"other {result:x}",
					};

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
