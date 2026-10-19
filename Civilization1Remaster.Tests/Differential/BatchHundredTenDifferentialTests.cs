using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchHundredTenDifferentialTests(ITestOutputHelper output)
{
	// The keys of the human turn that are neither orders nor moves, each with the answers to its screen or dialog (the
	// revolution comes from the GAME menu, which waits for one more key after the choice)
	private static readonly int[][] Keys =
	{
		new[] { 0x3b00 }, new[] { 0x3c00 }, new[] { 0x3d00 }, new[] { 0x3e00 }, new[] { 0x3f00 }, new[] { 0x4000 },
		new[] { 0x4100 }, new[] { 0x4200 }, new[] { 0x4300 }, new[] { 0x4400 },
		new[] { 0x4838 }, new[] { 0x4939 }, new[] { 0x4d36 }, new[] { 0x5133 }, new[] { 0x5032 }, new[] { 0x4f31 }, new[] { 0x4b34 }, new[] { 0x4737 },
		new[] { 0x2000 }, new[] { 0x0d }, new[] { 0x2f }, new[] { 0x3f }, new[] { 0x53, 0x1b },
		new[] { 0x2200, 0x5000, 0x5000, 0x5000, 0x5000, 0x5000, 0x5000, 0x0d, 0x31, 0x0d }, new[] { 0x2200, 0x5000, 0x5000, 0x5000, 0x5000, 0x5000, 0x5000, 0x0d, 0x31, 0x5000, 0x0d },
		new[] { 0x3200 },
		new[] { 0x3d, 0x0d }, new[] { 0x2b, 0x5000, 0x0d }, new[] { 0x3d, 0x1b },
		new[] { 0x2d, 0x0d }, new[] { 0x5f, 0x5000, 0x0d }, new[] { 0x2d, 0x1b },
		new[] { 0x74 }, new[] { 0x1300 }, new[] { 0x2f00 }, new[] { 0x2300 },
		new[] { 0x1000, 0x0d }, new[] { 0x1000, 0x5000, 0x0d },
		new[] { 0x09 }, new[] { 0x09, 0x1b }, new[] { 0x09, 0x09 }, new[] { 0x1b },
		new[] { 0x2200, 0x1b }, new[] { 0x1800, 0x0d }, new[] { 0x1e00, 0x1b }, new[] { 0x1100, 0x0d }, new[] { 0x2e00, 0x0d }, new[] { 0x2200, 0x0d },
	};

	private static readonly string[] Screens =
	{
		"DisplayWhichArmy", "DisplayCityStatusReport", "RenderUnitCountsChart", "DisplayScreen", "DisplayMilitaryReport",
		"DisplayAttitudeReport", "DisplayTradeReport", "DisplayScienceReport", "DisplayCivilizationOverview",
		"DisplayWondersOfTheWorld", "DisplayCivilizationGraph", "DisplayReplay", "DisplayTopCities", "DisplayGameFigures",
		"DisplayFinalScore", "DisplayWorldMap", "DisplayNewCityLocation", "DisplayStoreGameDialog", "DisplayNewspaper",
		"PrepareMouse", "AwaitForKeyPressOrMouseClick", "ManageMenuBar",
	};

	[Fact]
	public void HandlePlayerTurnKeysMatchOriginalCode()
	{
		// The first unit of the human (or the end of the turn) gets a key of the reports, the view, the rates, the
		// revolution, the save, the sound, the quit, the marked tile or the menu bar
		HashSet<string> ways = [];
		int counter = 0;

		foreach (int bay in new[] { 5, 9, 13 })
			CheckKeys(bay, ways, () => counter++);

		foreach (string way in Screens.Concat(new[] { "tax", "luxuries", "quit", "sound", "debug", "drawn", "marked" }))
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private void CheckKeys(int bay, HashSet<string> ways, Func<int> next)
	{
		Check(bay, 34, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			int turn = next();
			int[] keys = Keys[turn % Keys.Length];
			bool everything = turn / Keys.Length % 2 == 0;
			// Sometimes no unit of the human moves (0xdf60: no new moves), so Tab comes at the end of the turn
			bool unitless = keys[0] == 0x09 && chance.Next(2) == 0;
			// The option "End of Turn" (4) mostly on: without it the turn of the human ends at once once a unit took a key
			bool endOfTurn = unitless || chance.Next(3) != 0;
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			short levy = 0, research = 0, markers = 0;
			ushort debug = 0;
			byte[] records = [];

			return new Scenario(
				$"HandlePlayerTurn({person}), keys {string.Join(" ", keys.Select(key => key.ToString("x")))}, everything {everything}, unitless {unitless}, end of turn {endOfTurn}",
				game =>
				{
					// No quit left from an earlier turn
					game.Processor.EmitUInt16(0x3b01, 0xd806, (ushort)(everything ? 1 : 0));
					game.Processor.EmitUInt16(0x3b01, 0xdc48, 0);
					game.World.SessionPrefMarkers = (short)(endOfTurn ? game.World.SessionPrefMarkers | 4 : game.World.SessionPrefMarkers & ~4);
					game.Processor.EmitUInt16(0x3b01, 0xdf60, (ushort)(unitless ? 1 : 0));

					for (int troop = 0; unitless && troop < 128; troop++)
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.LeftoverSteps), 0);

					// No key left from the end of an earlier turn ('1' goes to AwaitForKey, Escape to the panel of a settler
					// or else to nothing; without units there is no panel)
					lock (game.VideoCard.Tokens)
						game.VideoCard.Tokens.Clear();

					levy = game.World.Participants[person].LevyRatio;
					research = game.World.Participants[person].ResearchLevyRatio;
					markers = game.World.SessionPrefMarkers;
					debug = game.Processor.PullUInt16(0x3b01, 0xd806);
					records = Enumerable.Range(0, 0x3a * 0x10).Select(at => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(0x1512 + at))).ToArray();
					HumanTurnScript.Install(game, (unitless ? new[] { 0x31 } : new[] { 0x31, 0x1b }).Concat(keys).Select(key => new HumanTurnScript.Input(key)));
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the key did, from the screens it showed and the state it changed
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = Screens.Where(name => Delta(name) > 0).ToList();

					if (game.World.Participants[person].LevyRatio != levy)
						met.Add("tax");

					if (game.World.Participants[person].LevyRatio == levy && game.World.Participants[person].ResearchLevyRatio != research)
						met.Add("luxuries");

					if (game.Processor.PullUInt16(0x3b01, 0xdc48) != 0)
						met.Add("quit");

					if (((game.World.SessionPrefMarkers ^ markers) & 0x10) != 0)
						met.Add("sound");

					if (game.Processor.PullUInt16(0x3b01, 0xd806) != debug)
						met.Add("debug");

					if (Enumerable.Range(0, records.Length).Any(at => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(0x1512 + at)) != records[at]))
						met.Add("drawn");

					if (keys[0] == 0x09 && Delta("RepaintMapTile") > 0)
						met.Add("marked");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x80), 0x80)]);
		}, output, [], null);
	}
}
