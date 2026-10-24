using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyFiveDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void PickCityProductionMatchesOriginalCode()
	{
		// The ways the function answered, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 1, 11, 14 })
			CheckPickCityProduction(bay, ways);

		foreach (string way in new[] { "computer", "militia", "barbarians", "menu", "advisors", "builds itself", "civilopedia", "page cut", "more" })
			Assert.True(ways.Contains(way), $"No scenario answered by '{way}' (answered: {string.Join(", ", ways)})");
	}

	private void CheckPickCityProduction(int bay, HashSet<string> ways)
	{
		const int ScenarioTally = 20;
		int step = 0;

		Check(bay, ScenarioTally, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0 - 2: computer players (2: the barbarians), 3 - 8: the human player (menu, advisors, a city that builds by itself,
			// the Civilopedia, all technologies: the menu cut in pages, and "more..."), 9 (the last): a computer player without units
			int scenarioIdx = step++;
			int mode = scenarioIdx == ScenarioTally - 1 ? 9 : scenarioIdx <= 8 ? scenarioIdx : scenarioIdx <= 11 ? 10 : chance.Next(9);
			short[] computers = Enumerable.Range(1, 7)
				.Where(idx => idx != person && engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == idx))
				.Select(idx => (short)idx).ToArray();
			short participantIdx = mode switch
			{
				2 => 0,
				>= 3 and <= 8 => person,
				_ => computers[chance.Next(computers.Length)],
			};
			short[] towns = Enumerable.Range(0, 128)
				.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && (participantIdx == 0 || engine.World.Towns[idx].ParticipantIdx == participantIdx))
				.Select(idx => (short)idx).ToArray();
			// Mode 10 takes the largest city (much trade: a caravan scores low)
			short townIdx = mode == 10 ? towns.OrderByDescending(idx => engine.World.Towns[idx].TrueExtent).First() : towns[chance.Next(towns.Length)];
			// Mode 10 in turn: a caravan (free trade routes), the spaceship parts, the parts after the human player's launch
			bool tradeRoutes = scenarioIdx != 9;
			bool launched = scenarioIdx == 11;
			bool allBuilt = chance.Next(2) == 0;
			bool improvements = mode is 7 or 8 || chance.Next(4) != 0;
			ushort war = (ushort)chance.Next(2);
			ushort attack = (ushort)chance.Next(2);

			int[] keys = mode switch
			{
				4 => [0x0d, 0x0d, 0x0d],
				6 => [0x2300, 0x2300, 0x0d, 0x0d, 0x0d],
				8 => [.. Enumerable.Repeat(0x5000, 40), 0x0d, 0x0d],
				_ => [0x0d],
			};

			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			// The frame stays below the stack pointer with the last values of the locals (the scores, the best choices, the list)
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			int Delta(GameEngine game, string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);

			return new Scenario(
				$"PickCityProduction({participantIdx}, {townIdx}), mode {mode}, improvements {improvements}, war {war}, attack {attack}",
				game =>
				{
					short marker = (short)(1 << participantIdx);
					bool human = participantIdx == person;
					game.World.ParticipantMarkers = (short)(human ? game.World.ParticipantMarkers | marker : game.World.ParticipantMarkers & ~marker);
					game.World.DiagMarkers = (short)(improvements ? game.World.DiagMarkers | 4 : game.World.DiagMarkers & ~4);
					game.World.SessionPrefMarkers = (short)(mode == 4 ? game.World.SessionPrefMarkers | 1 : game.World.SessionPrefMarkers & ~1);
					// Without a mouse the menus read the keys (with one, a menu takes the pointer at once)
					game.Processor.EmitGlobalWord(0x1a3c, 0);
					game.Processor.EmitGlobalWord(0x6ed4, war);
					game.Processor.EmitGlobalWord(0x8078, attack);

					City town = game.World.Towns[townIdx];

					// Mode 10: every technology, every improvement built but the spaceship parts (after the Apollo Program), the
					// trade routes used (the parts are the best improvements) or free (a caravan); mode 5: sometimes nothing to build
					if (mode == 10)
					{
						ushort[] markers = game.World.Participants[participantIdx].FoundScienceMarkers;

						for (int wordIdx = 0; wordIdx < markers.Length; wordIdx++)
							markers[wordIdx] = 0xffff;

						town.StructureMarkers0 = 0xffff;
						town.StructureMarkers1 = 0xff1f;

						if (game.World.MarvelTownIdx[19] == -1)
							game.World.MarvelTownIdx[19] = townIdx;

						game.World.StarshipMarkers = (short)(launched ? 1 << (person + 8) : 0);

						for (int routeIdx = 0; routeIdx < 3; routeIdx++)
							town.CommerceTownIndices[routeIdx] = (sbyte)(tradeRoutes ? townIdx : -1);
					}

					if (mode == 5 && allBuilt)
					{
						town.StructureMarkers0 = 0xffff;
						town.StructureMarkers1 = 0xffff;
					}

					town.ConditionMarker = (byte)(mode == 5 ? town.ConditionMarker | 0x10 : town.ConditionMarker & ~0x10);

					if (mode is 7 or 8)
					{
						ushort[] markers = game.World.Participants[participantIdx].FoundScienceMarkers;

						for (int wordIdx = 0; wordIdx < markers.Length; wordIdx++)
							markers[wordIdx] = 0xffff;
					}

					if (mode == 9)
					{
						game.World.Participants[participantIdx].RegimeKind = 1;

						for (int troopIdx = 0; troopIdx < 128; troopIdx++)
							game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Kind), 0xff);

						foreach (City other in game.World.Towns)
							other.Unfamiliar[0] = other.Unfamiliar[1] = -1;
					}

					// The keys of the scenario, typed like the scripted keyboard does (a look for a key after a typed one finds
					// none), then Enter; a game that asks for too many keys is stuck
					lock (game.VideoCard.Tokens)
						game.VideoCard.Tokens.Clear();

					Queue<int> pending = new Queue<int>(keys);
					bool withhold = false;
					int typed = 0;

					game.Processor.InputStarved = waiting =>
					{
						game.Processor.Idle();

						if (!waiting && withhold)
						{
							withhold = false;
							return;
						}

						if (++typed > 300)
							throw new InvalidOperationException("The game asked for more than 300 keys");

						withhold = true;

						lock (game.VideoCard.Tokens)
							game.VideoCard.Tokens.Enqueue(pending.Count > 0 ? pending.Dequeue() : 0x0d);
					};
				},
				game => new ScienceAndAdvisorsReference(game).PickCityProduction(participantIdx, townIdx),
				game => game.Advisors.PickCityProduction(participantIdx, townIdx),
				Register.Kept | Register.Acc,
				// The way the function answered, from its calls and its result
				game =>
				{
					ushort result = game.Processor.RegAcc.Value16;
					string way;

					if (participantIdx == 0)
						way = "barbarians";
					else if (result == 1 && game.World.Participants[participantIdx].TroopTally == 0 && participantIdx != person)
						way = "militia";
					else if (participantIdx != person)
						way = "computer";
					else if (Delta(game, "DisplayCivilopediaRecord") > 0)
						way = "civilopedia";
					else if (Delta(game, "DisplayMenu") == 0)
						way = "builds itself";
					else if (Delta(game, "ImportTextFromFile") > 0)
						way = "advisors";
					else if (Delta(game, "ObtainCityLabel") >= 3)
						way = "more";
					else if (Delta(game, "ObtainCityLabel") == 2)
						way = "page cut";
					else
						way = "menu";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x14a), 0x14a)]);
		}, output, [], null);
	}
}
