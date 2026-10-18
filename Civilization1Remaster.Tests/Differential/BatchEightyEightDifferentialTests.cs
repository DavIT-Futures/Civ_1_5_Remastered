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

public sealed class BatchEightyEightDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitTileScoresMatchOriginalCode()
	{
		// How the units scored the tiles around them (a fight, a purchase, a free tile, an own unit), over all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckTiles(bay, ways);

		foreach (string way in new[] { "battle", "guardian", "bought", "free tile", "foreign city", "hut", "own unit", "heading", "militia", "city battle", "air battle" })
			Assert.True(ways.Contains(way), $"No unit met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort KindField(GameEngine game, byte kind, int field)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + field));
	}

	/// <summary>Moves a lone unit of the player (any player but the excluded ones when -1) to the tile; false when none</summary>
	private static bool MoveLoneUnit(GameEngine game, short participantIdx, short[] excluded, short exceptTroop, int horizLoc, int vertLoc, int pick, int domain)
	{
		MapLayers chart = game.MapOps.Chart;

		if (vertLoc <= 0 || vertLoc >= 49 || (chart.OwnerBits(horizLoc, vertLoc) & MapLayers.UnitsPresent) != 0)
			return false;

		var lone = Enumerable.Range(0, 8).Where(player => participantIdx >= 0 ? player == participantIdx : !excluded.Contains((short)player))
			.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
			.Where(unit => unit.Troop != exceptTroop && UnitByte(game, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
				UnitByte(game, unit.Player, unit.Troop, UnitMemory.FollowingInStack) == 0xff &&
				KindField(game, kind, UnitKindTable.LandGroup) == domain)
			.ToList();

		if (lone.Count == 0)
			return false;

		var (player, troop) = lone[pick % lone.Count];
		ushort unit = UnitMemory.Address(player, troop);
		int oldHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
		int oldVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));
		game.MapOps.DiscardUnitFromTile(player, troop, oldHoriz, oldVert);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
		chart.AssignOwnerBits(horizLoc, vertLoc, MapLayers.UnitsPresent | player);
		return true;
	}

	private static bool AnyUnitAt(GameEngine game, int horizLoc, int vertLoc)
	{
		return Enumerable.Range(0, 8).Any(player => Enumerable.Range(0, 128).Any(troop => UnitByte(game, player, troop, UnitMemory.Kind) != 0xff &&
			UnitByte(game, player, troop, UnitMemory.Horiz) == horizLoc && UnitByte(game, player, troop, UnitMemory.Vert) == vertLoc));
	}

	/// <summary>The tile gets the terrain of another tile (its colour on the map layer)</summary>
	private static void CopyTerrain(GameEngine game, int horizLoc, int vertLoc, int terrain)
	{
		MapLayers chart = game.MapOps.Chart;

		for (int at = 0; at < 80 * 50; at++)
		{
			if (chart.Terrain(at / 50, at % 50) == terrain)
			{
				game.VideoCard.AssignDot(2, horizLoc, vertLoc, (byte)game.VideoCard.ObtainDot(2, at / 50, at % 50), 0);
				return;
			}
		}
	}

	private void CheckTiles(int bay, HashSet<string> ways)
	{
		// The city of the last scenario goes away before the next one (a unit there would fight in a city that is not there)
		(int Horiz, int Vert, int Bits, int Low)? lastCity = null;

		Check(bay, 160, (chance, pair) =>
		{
			var undo = lastCity;
			lastCity = null;

			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// The battles of the earlier scenarios may leave the city mark of a city that is gone: the mark goes away
			var orphans = Enumerable.Range(0, 80 * 50).Select(at => (Horiz: at / 50, Vert: at % 50))
				.Where(tile => (engine.MapOps.Chart.Improvements(tile.Horiz, tile.Vert) & 1) != 0 &&
					!engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.Place.X == tile.Horiz && town.Place.Y == tile.Vert))
				.ToList();
			// Settlers (0), land attackers (1), defenders (2), attacking ships (3) or planes (4) of a computer player
			int wanted = new[] { 0, 1, 2, 2, 2, 3, 3, 4 }[chance.Next(8)];
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff &&
					KindField(engine, kind, UnitKindTable.LandGroup) == (wanted == 3 ? UnitKindTable.SeaUnits : UnitKindTable.LandUnits) &&
					(wanted == 4 ? KindField(engine, kind, UnitKindTable.TroopGroup) is 1 or 2 : KindField(engine, kind, UnitKindTable.TroopGroup) == wanted))
				.ToList();
			// The defenders mostly in a stack (a lone one next to a foreign unit fortifies)
			var stacked = candidates.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff &&
				(engine.MapOps.Chart.Improvements(UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Horiz), UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Vert)) & 1) == 0).ToList();

			if ((wanted is 1 or 2) && stacked.Count > 0 && chance.Next(4) != 0)
				candidates = stacked;

			// No planes in the saves: a unit of a stack becomes a bomber (with fuel: a fighter without a destination never gets that far)
			if (wanted == 4)
				candidates = stacked;

			if (candidates.Count == 0)
				return null;

			var (participantIdx, troopIdx) = candidates[chance.Next(candidates.Count)];
			// What stands next to the unit: 0 nothing, 1 a unit of another player, 2 an own unit, 3 a city of another player
			// (no units), 4 a hut (the unit, when alone on land, moves next to one), 5 a city of another player at war with units
			// (the stack of the unit moves next to it), 6 a plane of another player at war
			int neighbour = wanted == 4 && chance.Next(4) != 0 ? 6 : chance.Next(6);
			byte fuel = (byte)chance.Next(1, 3);
			byte strangerPlane = (byte)(chance.Next(2) == 0 ? 0xe : 0xf);
			int heading = chance.Next(1, 9);
			int neighbourPick = chance.Next(1000);
			byte strangerStatus = (byte)new[] { 0, 8, 0x20, 0x28, 0x28 }[chance.Next(5)];
			bool strongTerrain = chance.Next(2) == 0;
			short cityOwner = (short)chance.Next(1, 8);
			// A militia (as a defender) under approach 1 counts as an attacker
			bool militia = wanted == 2 && chance.Next(2) == 0;
			short approach = (short)(militia && chance.Next(4) != 0 ? 1 : new[] { 0, 1, 2, 5 }[chance.Next(4)]);
			// A purchase made likely for a militia next to a stranger: 1 at peace with it, 2 at war (on a strong terrain)
			int buyMode = militia && neighbour == 1 ? chance.Next(3) : 0;
			ushort[] relations = Enumerable.Range(0, 8 * 8).Select(_ => (ushort)chance.Next(0, 4)).ToArray();
			short[] humanCities = Enumerable.Range(0, 16).Select(_ => (short)chance.Next(0, 3)).ToArray();
			byte shown = (byte)(chance.Next(2) == 0 ? 0 : chance.Next(1, 0x100));
			byte unitHeading = (byte)(chance.Next(3) == 0 ? 0xff : chance.Next(1, 9));
			byte steps = (byte)chance.Next(0, 6);
			sbyte[] seen = Enumerable.Range(0, 11 * 11).Select(_ => (sbyte)(chance.Next(2) == 0 ? 0 : chance.Next(0, 0x80))).ToArray();
			byte[] pattern = Enumerable.Range(0, 20 * 13).Select(_ => (byte)chance.Next(4)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			// A terrain with a defence of 4 or more for the tile of the stranger
			int strong = Enumerable.Range(0, 11).FirstOrDefault(terrain => (sbyte)engine.Processor.Storage.PullUInt8(0x3b01, (ushort)(terrain * 0x13 + 0x28f)) >= 4, -1);
			var huts = Enumerable.Range(0, 80 * 50).Select(at => (Horiz: at / 50, Vert: at % 50))
				.Where(tile => tile.Vert >= 3 && tile.Vert <= 46 && tile.Horiz >= 1 && tile.Horiz < 79 &&
					MapRules.IsHutPlace(tile.Horiz, tile.Vert, engine.World.ChanceRoot) &&
					engine.MapOps.Chart.Terrain(tile.Horiz, tile.Vert) != MapRules.OceanTerrain &&
					engine.MapOps.Chart.Terrain(tile.Horiz - 1, tile.Vert) != MapRules.OceanTerrain &&
					(engine.MapOps.Chart.OwnerBits(tile.Horiz - 1, tile.Vert) & MapLayers.UnitsPresent) == 0 &&
					(engine.MapOps.Chart.Improvements(tile.Horiz, tile.Vert) & 1) == 0)
				.ToList();
			var hut = huts.Count > 0 ? huts[neighbourPick % huts.Count] : (Horiz: -1, Vert: -1);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) role {wanted}, neighbour {neighbour} at {heading}, militia {militia}, approach {approach}",
				game =>
				{
					if (undo is var (undoHoriz, undoVert, undoBits, undoLow))
					{
						game.MapOps.Chart.AssignOwnerBits(undoHoriz, undoVert, undoBits);
						game.VideoCard.AssignDot(2, undoHoriz, undoVert + 100, (byte)undoLow, 0);
					}

					foreach (var orphan in orphans)
						game.MapOps.Chart.ClearImprovements(1, orphan.Horiz, orphan.Vert);

					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					foreach (int local in new[] { 0x10, 0x1c, 0x5a, 0x5c, 0x40 })
						game.Processor.EmitUInt16(0x3b01, (ushort)(frame - local), 0x1234);

					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0x5e), 0x1234);
					MapLayers chart = game.MapOps.Chart;
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					short strangerPlayer = -1;
					bool alone = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack)) == 0xff;

					if (neighbour == 5 && !alone && wanted is 1 or 2)
					{
						// The stack goes next to a city of another player with units, at war with it
						var towns = game.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx != participantIdx &&
							(chart.OwnerBits(town.Place.X, town.Place.Y) & MapLayers.UnitsPresent) != 0).ToList();
						var spots = towns.SelectMany(town => Enumerable.Range(1, 8).Select(direction => (Town: town,
								Horiz: (town.Place.X + chart.DirectionHoriz(direction) + 80) % 80, Vert: town.Place.Y + chart.DirectionVert(direction))))
							.Where(spot => spot.Vert > 0 && spot.Vert < 49 && chart.Terrain(spot.Horiz, spot.Vert) != MapRules.OceanTerrain &&
								(chart.OwnerBits(spot.Horiz, spot.Vert) & MapLayers.UnitsPresent) == 0 && (chart.Improvements(spot.Horiz, spot.Vert) & 1) == 0)
							.ToList();

						if (spots.Count > 0)
						{
							var spot = spots[neighbourPick % spots.Count];
							int oldHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
							int oldVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));
							int member = troopIdx;

							do
							{
								ushort memberUnit = UnitMemory.Address(participantIdx, member);
								game.Processor.EmitUInt8(0x3b01, (ushort)(memberUnit + UnitMemory.Horiz), (byte)spot.Horiz);
								game.Processor.EmitUInt8(0x3b01, (ushort)(memberUnit + UnitMemory.Vert), (byte)spot.Vert);
								member = game.Processor.PullUInt8(0x3b01, (ushort)(memberUnit + UnitMemory.FollowingInStack));
							}
							while (member != troopIdx && member != 0xff);

							chart.AssignOwnerBits(oldHoriz, oldVert, chart.OwnerBits(oldHoriz, oldVert) & 7);
							chart.AssignOwnerBits(spot.Horiz, spot.Vert, MapLayers.UnitsPresent | participantIdx);
							strangerPlayer = spot.Town.ParticipantIdx;
						}
					}

					if (wanted == 4)
					{
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0xf);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), fuel);
					}

					if (neighbour == 4 && alone && wanted is 0 or 1 or 2 && hut.Horiz >= 0)
					{
						int oldHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
						int oldVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));
						game.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, oldHoriz, oldVert);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)(hut.Horiz - 1));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)hut.Vert);
						chart.AssignOwnerBits(hut.Horiz - 1, hut.Vert, MapLayers.UnitsPresent | participantIdx);
						game.World.ChartSight[hut.Horiz, hut.Vert] &= ~1;
					}

					int unitHoriz = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
					int unitVert = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

					for (int idx = 0; idx < 11 * 11; idx++)
					{
						int seenVert = unitVert + idx / 11 - 5;

						if (seenVert >= 0 && seenVert < 50)
							game.World.ChartSight[(unitHoriz + idx % 11 - 5 + 80) % 80, seenVert] = seen[idx];
					}

					if (neighbour == 4)
						game.World.ChartSight[(unitHoriz + 1) % 80, unitVert] &= ~1;

					int domain = wanted == 3 ? UnitKindTable.SeaUnits : UnitKindTable.LandUnits;
					int besideHoriz = 0, besideVert = -1;
					bool fits = false;

					// The first free tile of the right kind from the drawn direction on
					for (int turn = 0; turn < 8 && !fits; turn++)
					{
						int direction = (heading - 1 + turn) % 8 + 1;
						besideHoriz = (unitHoriz + chart.DirectionHoriz(direction) + 80) % 80;
						besideVert = unitVert + chart.DirectionVert(direction);
						fits = besideVert > 0 && besideVert < 49 && (chart.OwnerBits(besideHoriz, besideVert) & MapLayers.UnitsPresent) == 0 &&
							(chart.Terrain(besideHoriz, besideVert) == MapRules.OceanTerrain) == (wanted == 3);
					}

					if (neighbour is 1 or 6 && fits)
					{
						if (MoveLoneUnit(game, -1, buyMode != 0 ? [participantIdx, person] : [participantIdx], -1, besideHoriz, besideVert, neighbourPick, domain))
						{
							short other = (short)(chart.OwnerBits(besideHoriz, besideVert) & 7);
							strangerPlayer = other;

							for (int troop = 0; troop < 128; troop++)
							{
								ushort strangerUnit = UnitMemory.Address(other, troop);

								if (game.Processor.PullUInt8(0x3b01, (ushort)(strangerUnit + UnitMemory.Horiz)) == besideHoriz &&
									game.Processor.PullUInt8(0x3b01, (ushort)(strangerUnit + UnitMemory.Vert)) == besideVert &&
									game.Processor.PullUInt8(0x3b01, (ushort)(strangerUnit + UnitMemory.Kind)) != 0xff)
								{
									game.Processor.EmitUInt8(0x3b01, (ushort)(strangerUnit + UnitMemory.Status), buyMode switch { 1 => 8, 2 => 0x28, _ => strangerStatus });

									// (a plane of another player for a plane)
									if (neighbour == 6)
										game.Processor.EmitUInt8(0x3b01, (ushort)(strangerUnit + UnitMemory.Kind), strangerPlane);
								}
							}

							if ((strongTerrain || buyMode == 2) && strong >= 0 && wanted != 3)
								CopyTerrain(game, besideHoriz, besideVert, strong);
						}
					}
					else if (neighbour == 2 && fits)
					{
						MoveLoneUnit(game, participantIdx, [], troopIdx, besideHoriz, besideVert, neighbourPick, domain);
					}
					else if (neighbour == 3 && besideVert > 0 && besideVert < 49 && chart.Terrain(besideHoriz, besideVert) != MapRules.OceanTerrain &&
						(chart.OwnerBits(besideHoriz, besideVert) & MapLayers.UnitsPresent) == 0 && !AnyUnitAt(game, besideHoriz, besideVert))
					{
						// (no unit of any player may stand there: the original fights it with the owner of the tile)
						lastCity = (besideHoriz, besideVert, chart.OwnerBits(besideHoriz, besideVert), chart.LowImprovements(besideHoriz, besideVert));
						chart.AssignOwnerBits(besideHoriz, besideVert, cityOwner);
						game.VideoCard.AssignDot(2, besideHoriz, besideVert + 100, (byte)(chart.LowImprovements(besideHoriz, besideVert) | 1), 0);
					}

					if (militia)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 1);

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHeading), unitHeading);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.ShownToParticipants), shown);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), steps);

					for (int idx = 0; idx < 20 * 13; idx++)
						game.Processor.EmitUInt8(0x3772, (ushort)(0x2438 + idx), pattern[idx]);

					for (int player = 0; player < 8; player++)
					{
						for (int other = 0; other < 8; other++)
							game.World.Participants[player].Relations[other] = (ushort)((game.World.Participants[player].Relations[other] & ~3) | relations[player * 8 + other]);
					}

					if (neighbour is 5 or 6 && strangerPlayer >= 0)
					{
						game.World.Participants[participantIdx].Relations[strangerPlayer] &= 0xfffd;
						game.World.Participants[strangerPlayer].Relations[participantIdx] &= 0xfffd;
					}

					if (buyMode != 0 && strangerPlayer >= 0 && strangerPlayer != person)
					{
						ushort[] own = game.World.Participants[participantIdx].Relations;
						own[strangerPlayer] = (ushort)((own[strangerPlayer] & ~3) | (buyMode == 1 ? 3 : 1));
						own[person] = (ushort)((own[person] & ~3) | 1);
						ushort[] theirs = game.World.Participants[strangerPlayer].Relations;
						theirs[person] = (ushort)((theirs[person] & ~3) | (relations[0] == 1 ? 0 : relations[0]));
					}

					for (int continentIdx = 0; continentIdx < 16; continentIdx++)
					{
						game.World.Participants[participantIdx].Landmasses[continentIdx].Approach = approach;
						game.World.Participants[person].Landmasses[continentIdx].TownTally = buyMode == 1 ? (short)(humanCities[continentIdx] + 1) : humanCities[continentIdx];
					}
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Bot.ShiftAIUnit(participantIdx, troopIdx),
				Register.Kept | Register.Acc,
				// What the look met, from the calls and the locals left in the frame (the last tile scored)
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					ushort Local(int local) => game.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - local));
					List<string> met = [];

					if (Delta("SettleBattle") > 0)
						met.Add("battle");

					if (Delta("LocateBestGuardianInStack") > 0)
						met.Add("guardian");

					if (Delta("BuyUnit") > 0)
						met.Add("bought");

					if (Delta("ContainsHut") > 0)
						met.Add("free tile");

					if (neighbour == 3 && Delta("ContainsHut") > 0 && Local(0x40) != 0x1234 && Local(0x1c) == 0)
						met.Add("foreign city");

					if (neighbour == 5 && Delta("SettleBattle") > 0)
						met.Add("city battle");

					if (neighbour == 6 && Delta("SettleBattle") > 0)
						met.Add("air battle");

					if (neighbour == 4 && Delta("ContainsHut") > 0)
						met.Add("hut");

					if (neighbour == 2 && Local(0x1c) is 0 or 1 && Delta("ContainsHut") > 0)
						met.Add("own unit");

					if ((game.Processor.Storage.PullUInt8(0x3b01, (ushort)(frame - 0x5e)) is >= 1 and <= 8))
						met.Add("heading");

					if (militia && Local(0x56) == 1 && Local(0x1c) is 0 or 1)
						met.Add("militia");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
