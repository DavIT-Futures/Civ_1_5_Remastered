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

public sealed class BatchNinetyFiveDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnRoadIrrigationMineMatchesOriginalCode()
	{
		// The work of the settlers: 'r' (road, railroad), 'i' (irrigation or another terrain), 'm' (mine or another terrain)
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckWork(bay, ways);

		foreach (string way in new[] { "road", "railroad", "irrigated", "mined", "terrain changed", "work", "refused", "help" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckWork(int bay, HashSet<string> ways)
	{
		Check(bay, 20, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			int[] keys = { 0x20, 'r', 'r', 'i', 'i', 'm', 'm', 0x0d };
			List<HumanTurnScript.Input> inputs = Enumerable.Range(0, chance.Next(4, 16))
				.Select(_ => new HumanTurnScript.Input(keys[chance.Next(keys.Length)])).ToList();
			// The tiles of the settlers (improvements: irrigation, mine, road, railroad), the counter of their work, the water
			// bit (0xd7fa), Railroad and Bridge Building
			int[] tiles = Enumerable.Range(0, 128).Select(_ => new[] { 0, 0, 2, 4, 8, 8, 0x18, 0x10, 0xa, 0xc }[chance.Next(10)]).ToArray();
			byte[] work = Enumerable.Range(0, 128).Select(_ => (byte)(chance.Next(2) == 0 ? chance.Next(3, 6) : chance.Next(0, 14))).ToArray();
			short water = (short)(chance.Next(3) == 0 ? 0 : 2);
			bool railroad = chance.Next(2) == 0;
			bool bridges = chance.Next(2) == 0;
			MapLayers sight = engine.MapOps.Chart;
			var settlers = Enumerable.Range(0, 128)
				.Select(troop => (Troop: troop, Horiz: (int)UnitByte(engine, person, troop, UnitMemory.Horiz), Vert: (int)UnitByte(engine, person, troop, UnitMemory.Vert)))
				.Where(unit => UnitByte(engine, person, unit.Troop, UnitMemory.Kind) == 0 && unit.Vert < 50)
				.ToList();
			// (the tiles of cities keep their improvements)
			bool[] towns = settlers.Select(unit => (sight.Improvements(unit.Horiz, unit.Vert) & 1) != 0).ToArray();
			int[] terrains = settlers.Select(unit => sight.Terrain(unit.Horiz, unit.Vert)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({person}), keys {string.Join(" ", inputs.Select(input => input.Key.ToString("x")))}, water {water}, railroad {railroad}, bridges {bridges}",
				game =>
				{
					HumanTurnScript.Install(game, inputs);
					MapLayers layers = game.MapOps.Chart;
					game.World.DiagMarkers = (short)((game.World.DiagMarkers & ~2) | water);
					ushort[] science = game.World.Participants[person].FoundScienceMarkers;

					foreach ((int tech, bool known) in new[] { (0x31, railroad), (0x13, bridges) })
					{
						if (known)
							science[tech >> 4] |= (ushort)(1 << (tech & 0xf));
						else
							science[tech >> 4] &= unchecked((ushort)~(1 << (tech & 0xf)));
					}

					for (int idx = 0; idx < settlers.Count; idx++)
					{
						var (troop, horizLoc, vertLoc) = settlers[idx];
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.UnusualSteps), work[troop]);

						if (towns[idx])
							continue;

						game.VideoCard.AssignDot(2, horizLoc, vertLoc + 100, (byte)(tiles[troop] & 0xe), 0);
						game.VideoCard.AssignDot(2, horizLoc, vertLoc + 150, (byte)((layers.HighImprovements(horizLoc, vertLoc) & ~1) | (tiles[troop] >> 4)), 0);
					}
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the work did, from the tiles of the settlers and the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					MapLayers after = game.MapOps.Chart;
					List<string> met = [];

					for (int idx = 0; idx < settlers.Count; idx++)
					{
						var (troop, horizLoc, vertLoc) = settlers[idx];

						if (towns[idx])
							continue;

						int gained = after.Improvements(horizLoc, vertLoc) & ~tiles[troop];

						if ((gained & 8) != 0)
							met.Add("road");

						if ((gained & 0x10) != 0)
							met.Add("railroad");

						if ((gained & 2) != 0)
							met.Add("irrigated");

						if ((gained & 4) != 0)
							met.Add("mined");

						if (after.Terrain(horizLoc, vertLoc) != terrains[idx])
							met.Add("terrain changed");
					}

					if (settlers.Any(unit => UnitByte(game, person, unit.Troop, UnitMemory.Kind) == 0 && (UnitByte(game, person, unit.Troop, UnitMemory.Status) & 0xc2) != 0))
						met.Add("work");

					if (Delta("PanMapToPosition") > 0)
						met.Add("refused");

					if (Delta("DisplayHelpForHuman") > 0)
						met.Add("help");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met.Distinct());
				});
		}, output, [], null);
	}
}
