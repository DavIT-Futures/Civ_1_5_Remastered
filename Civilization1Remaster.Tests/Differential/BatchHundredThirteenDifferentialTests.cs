using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchHundredThirteenDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandleCityMatchesOriginalCode()
	{
		// A city only counted (-1), in its turn (0) or on the city screen (1, the human's cities, left with Escape); the
		// materials sometimes out of range; the advances that make citizens content taken away or given; the palaces of the
		// owner sometimes taken away
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckCities(bay, ways);

		foreach (string way in new[] { "own palace", "palace near", "no palace near", "shared tiles", "content advances", "materials reset", "computer turn", "city lost", "grew", "held", "settler starved" })
			Assert.True(ways.Contains(way), $"No city met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private void CheckCities(int bay, HashSet<string> ways)
	{
		Check(bay, 40, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			var towns = Enumerable.Range(0, 128).Where(town => engine.World.Towns[town].ConditionMarker != 0xff).ToList();

			if (towns.Count == 0)
				return null;

			short townIdx = (short)towns[chance.Next(towns.Count)];
			bool human = engine.World.Towns[townIdx].ParticipantIdx == person;
			short marker = (short)(human && chance.Next(4) == 0 ? 1 : chance.Next(2) - 1);
			short materials = (short)new[] { -1, -5, 0, 1000, 1500 }[chance.Next(5)];
			bool badMaterials = chance.Next(3) == 0;
			// The advances that make citizens content: 0 kept, 1 taken away, 2 all given
			int advances = chance.Next(3);
			bool noPalaces = chance.Next(4) == 0;
			// Another city moved next to this one (its own tile among the tiles around); the level and the leader of a later
			// game; the flag 1 of the city
			bool close = chance.Next(4) == 0;
			int closeIdx = chance.Next(1000);
			(int Horiz, int Vert) offset = (chance.Next(-2, 3), chance.Next(1, 3));
			short hardness = (short)chance.Next(5);
			bool leader = chance.Next(2) == 0;
			bool flag = chance.Next(2) == 0;
			// The food: 0 kept, 1 below zero, 2 enough to grow (sometimes at size 10 or 11), with a granary, an aqueduct and the
			// bit 4 of DiagMarkers by chance
			// (3 exactly the food to grow, 4 one less)
			int food = chance.Next(5);
			sbyte size = (sbyte)(chance.Next(3) != 0 ? chance.Next(10, 12) : 0);
			bool unrest = chance.Next(2) == 0;
			bool granary = chance.Next(2) == 0;
			bool aqueduct = chance.Next(2) == 0;
			bool diag = chance.Next(2) == 0;
			sbyte sizeBefore = 0;
			bool alive = true;
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);

			return new Scenario(
				$"HandleCity({townIdx}, {marker}), materials {(badMaterials ? materials : "kept")}, advances {advances}, no palaces {noPalaces}, close {close}, level {hardness}, leader {leader}, flag {flag}, food {food}, size {size}, granary {granary}, aqueduct {aqueduct}, diag {diag}, flag 0x80 {unrest}",
				game =>
				{
					HumanTurnScript.Install(game, [new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);

					if (badMaterials)
						game.World.Towns[townIdx].MaterialsTally = materials;

					short owner = game.World.Towns[townIdx].ParticipantIdx;
					ushort[] known = game.World.Participants[owner].FoundScienceMarkers;

					foreach (int advance in new[] { 0x23, 0x29, 0x3a, 0x3c })
					{
						if (advances == 1)
							known[advance >> 4] &= (ushort)~(1 << (advance & 0xf));
						else if (advances == 2)
							known[advance >> 4] |= (ushort)(1 << (advance & 0xf));
					}

					if (close)
					{
						var others = towns.Where(other => other != townIdx).ToList();

						if (others.Count > 0)
						{
							var other = game.World.Towns[others[closeIdx % others.Count]];
							var place = game.World.Towns[townIdx].Place;
							other.Place = new System.Drawing.Point((place.X + offset.Horiz + 80) % 80, place.Y + offset.Vert);
						}
					}

					game.World.HardnessTier = hardness;
					game.World.Participants[person].Standing = (short)(leader ? 7 : game.World.Participants[person].Standing == 7 ? 6 : game.World.Participants[person].Standing);
					game.World.Towns[townIdx].ConditionMarker = (byte)(flag ? game.World.Towns[townIdx].ConditionMarker | 1 : game.World.Towns[townIdx].ConditionMarker & 0xfe);

					var city = game.World.Towns[townIdx];

					if (size != 0)
						city.TrueExtent = size;

					if (food == 1)
						city.GrainTally = -1;
					else if (food == 2)
						city.GrainTally = 900;
					else if (food >= 3)
					{
						// The food box: 10 for the human, else 16 - 2 x the level (2 less for the leader of a later game)
						int box = city.ParticipantIdx == person ? 10 : 16 - 2 * hardness -
							(game.World.Season >= 0 && game.World.Participants[person].Standing == 7 && hardness > 1 ? 2 : 0);
						city.GrainTally = (short)((city.TrueExtent + 1) * box - (food - 3));
					}

					city.ConditionMarker = (byte)(unrest ? city.ConditionMarker | 0x80 : city.ConditionMarker & 0x7f);

					city.StructureMarkers0 = (ushort)(granary ? city.StructureMarkers0 | 4 : city.StructureMarkers0 & ~4);
					city.StructureMarkers0 = (ushort)(aqueduct ? city.StructureMarkers0 | 0x100 : city.StructureMarkers0 & ~0x100);
					game.World.DiagMarkers = (short)(diag ? game.World.DiagMarkers | 4 : game.World.DiagMarkers & ~4);
					sizeBefore = city.TrueExtent;
					alive = true;

					// The owner without any palace
					foreach (var town in game.World.Towns.Where(town => noPalaces && town.ConditionMarker != 0xff && town.ParticipantIdx == owner))
						town.StructureMarkers0 &= 0xfffe;
				},
				game => new CityManagementReference(game).HandleCity(townIdx, marker),
				game => game.CityOps.HandleCity(townIdx, marker),
				Register.Kept,
				// What the start of HandleCity found, from the globals and the dead frame
				game =>
				{
					List<string> met = [];
					ushort palace = game.Processor.PullGlobalWord(0x6b30);
					met.Add(palace == 0 ? "own palace" : palace < 0x20 ? "palace near" : "no palace near");

					if (Enumerable.Range(0, 25).Any(at => at != 12 && game.Processor.Storage.PullUInt16(game.Processor.SegStack.Value16, (ushort)(frame - 0xa6 + 2 * at)) == 1))
						met.Add("shared tiles");

					if (game.Processor.PullGlobalWord(0xb882) != 0)
						met.Add("content advances");

					if (badMaterials && (materials < 0 || materials > 999) && game.World.Towns[townIdx].MaterialsTally < 999)
						met.Add("materials reset");

					if (!human && marker == 0)
						met.Add("computer turn");

					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);

					if (Delta("FinalizeProduction") > 0)
						met.Add("city lost");
					else if (marker == 0 && game.World.Towns[townIdx].TrueExtent > sizeBefore)
						met.Add("grew");
					else if (marker == 0 && food == 2 && game.World.Towns[townIdx].TrueExtent == sizeBefore)
						met.Add("held");

					if (food == 1 && marker == 0 && Delta("EliminateUnit") > 0)
						met.Add("settler starved");

					foreach (string way in met)
						ways.Add(way);

					return string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x180), 0x180)]);
		}, output, [], null);
	}
}
