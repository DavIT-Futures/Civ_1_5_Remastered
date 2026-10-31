using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyNineDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitDiplomatsAndExplorersMatchOriginalCode()
	{
		// What the units did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 0, 2, 7, 12 })
			CheckUnits(bay, ways);

		foreach (string way in new[] { "diplomat goes on", "diplomat to a city", "diplomat given up", "hut", "explores", "ambassador", "wanders", "seven unknown", "goes on" })
			Assert.True(ways.Contains(way), $"No unit did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckUnits(int bay, HashSet<string> ways)
	{
		int step = 0;

		Check(bay, 40, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0: a diplomat with a destination, 1: a diplomat in its city, 2: a diplomat elsewhere, 3: settlers next to a city of
			// the human player who is ahead, 4: a unit next to a hut, 5+: a unit of the player as it is
			// 5: settlers in the first turn (they wander), 6: settlers with exactly 7 unknown tiles around (not enough), 7: as it is
			int mode = step++ % 8;
			short[] players = Enumerable.Range(1, 7)
				.Where(idx => idx != person && engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == idx))
				.Select(idx => (short)idx).ToArray();
			short participantIdx = players[chance.Next(players.Length)];
			List<short> units = Enumerable.Range(0, 128).Where(idx => UnitByte(engine, participantIdx, idx, UnitMemory.Kind) != 0xff).Select(idx => (short)idx).ToList();

			if (units.Count == 0)
				return null;

			short troopIdx = units[chance.Next(units.Count)];
			var own = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx).ToList();
			var human = engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == person).ToList();
			var ownTown = own[chance.Next(own.Count)];
			var humanTown = human.Count > 0 ? human[chance.Next(human.Count)] : ownTown;
			// Mode 3: 2 - 5 tiles from the human player's city, with the known land just enough (14 - distance)
			int ambassadorDistance = 2 + (step / 8) % 4;
			int place = mode switch
			{
				1 => ownTown.Place.X << 8 | ownTown.Place.Y,
				3 => (humanTown.Place.X + ambassadorDistance) % 80 << 8 | humanTown.Place.Y,
				_ => chance.Next(80) << 8 | chance.Next(3, 47),
			};
			if (mode == 6)
			{
				// Far (9 tiles and more) from every city: the nearest city doesn't stop the exploring
				var towns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff).ToList();
				var far = Enumerable.Range(0, 80 * 50).Select(at => (Horiz: at / 50, Vert: at % 50))
					.Where(tile => tile.Vert >= 3 && tile.Vert <= 46 && towns.All(town =>
						Math.Max(Math.Min(Math.Abs(town.Place.X - tile.Horiz), 80 - Math.Abs(town.Place.X - tile.Horiz)), Math.Abs(town.Place.Y - tile.Vert)) >= 9))
					.ToList();

				if (far.Count == 0)
					return null;

				var tile = far[chance.Next(far.Count)];
				place = tile.Horiz << 8 | tile.Vert;
			}

			if (mode == 4)
			{
				// Next to (west of) a hut place on land
				var huts = Enumerable.Range(0, 80 * 50).Select(at => (Horiz: at / 50, Vert: at % 50))
					.Where(tile => tile.Vert >= 3 && tile.Vert <= 46 && tile.Horiz >= 1 &&
						Civilization1Remaster.Game.Rules.MapRules.IsHutPlace(tile.Horiz, tile.Vert, engine.World.ChanceRoot) &&
						engine.MapOps.Chart.Terrain(tile.Horiz, tile.Vert) != Civilization1Remaster.Game.Rules.MapRules.OceanTerrain)
					.ToList();
				var hut = huts[chance.Next(huts.Count)];
				place = (hut.Horiz - 1) << 8 | hut.Vert;
			}

			byte kind = mode switch { <= 2 => 0x1a, 3 or 5 or 6 => 0, 4 => (byte)chance.Next(1, 4), _ => 0xff };
			short hardness = (short)chance.Next(1, 5);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}), mode {mode}, place {place >> 8},{place & 0xff}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					ushort unit = UnitMemory.Address(participantIdx, troopIdx);
					void Set(int offset, byte amount) => game.Processor.EmitUInt8(0x3b01, (ushort)(unit + offset), amount);

					game.World.HardnessTier = hardness;

					if (mode <= 6)
					{
						Set(UnitMemory.Kind, kind);
						Set(UnitMemory.Status, 0);
						Set(UnitMemory.Horiz, (byte)(place >> 8));
						Set(UnitMemory.Vert, (byte)place);
						Set(UnitMemory.LeftoverSteps, 3);
						Set(UnitMemory.TravelHoriz, (byte)(mode == 0 ? 10 : 0xff));
						Set(UnitMemory.TravelVert, (byte)(mode == 0 ? 10 : 0xff));
					}

					// The layer of unknown tiles around (screen 2, columns from 0x50) and the one below it (rows from 0x32)
					int horiz = place >> 8;
					int vert = place & 0xff;

					if (mode == 3)
					{
						game.VideoCard.AssignDot(2, horiz + 0x50, vert, 0, 0);
						game.VideoCard.AssignDot(2, horiz + 0x50, vert + 0x32, (byte)(14 - ambassadorDistance), 0);

						// The human player is ahead in technology
						game.World.Participants[person].FoundScienceTally = (short)(game.World.Participants[participantIdx].FoundScienceTally + 5);
					}

					// Mode 5: the first turn (with 0xd76a the units wander); the other modes are not in it
					game.Processor.EmitGlobalWord(0xd76a, (ushort)(mode == 5 ? 1 : 0));

					if (mode == 5)
						game.World.RoundTally = 0;

					if (mode == 6)
					{
						for (int dx = -1; dx <= 1; dx++)
							for (int dy = -1; dy <= 1; dy++)
								game.VideoCard.AssignDot(2, (horiz + dx + 80) % 80 + 0x50, vert + dy, (byte)(dx == 0 && dy == 0 ? 7 : 0), 0);
					}

					if (mode == 4)
					{
						// Nobody has visited the hut east of the unit
						int hutHoriz = ((place >> 8) + 1) % 80;
						game.World.ChartSight[hutHoriz, place & 0xff] = (sbyte)(game.World.ChartSight[hutHoriz, place & 0xff] & ~1);
					}
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// What the move did, from the answer, the destination and the calls
				game =>
				{
					ushort answer = game.Processor.RegAcc.Value16;
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					string way;

					if (mode == 0 && answer == 0)
						way = "diplomat goes on";
					else if (mode == 1 && Delta("EliminateUnit") > 0)
						way = "diplomat given up";
					else if (mode == 2 && answer == 0 && UnitByte(game, participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
						way = "diplomat to a city";
					else if (mode == 4 && answer >= 1 && answer <= 8 && (short)pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x40)) == answer)
						way = "hut";
					else if (mode == 5 && answer == 0x62)
						way = "wanders";
					else if (mode == 6 && pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x28)) == 7 &&
						pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x4c)) == 0)
						way = "seven unknown";
					else if (mode == 3 && answer == 0x62 && pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x3a)) >= 9)
						way = "ambassador";
					else if ((answer == 0x62 || answer <= 8) && pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x28)) >= 8)
						way = "explores";
					else
						way = "goes on";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
