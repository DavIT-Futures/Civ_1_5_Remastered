using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// UnitManagement.VisitHut against its original code: units of every player (also the barbarians, the human player and
	/// planes) enter a hut at random land tiles, near cities and far from them, before and after the year 1000 and in the
	/// first turn, with many random seeds. Every outcome of the hut must be met.
	/// </summary>
	public abstract class VisitHutDifferentialTests
	{
		private const int ScenarioTally = 200;

		private readonly int bay;
		private readonly ITestOutputHelper output;

		protected VisitHutDifferentialTests(int bay, ITestOutputHelper output)
		{
			this.bay = bay;
			this.output = output;
		}

		[Fact]
		public void MatchesOriginalCode()
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using DifferentialPair pair = new DifferentialPair(gameFolder!, this.bay);
			Dictionary<string, int> outcomes = new Dictionary<string, int>(StringComparer.Ordinal);
			Random chance = new Random(this.bay);

			for (int scenarioIdx = 0; scenarioIdx < ScenarioTally; scenarioIdx++)
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);

				short participantIdx = (short)chance.Next(8);
				List<short> present = Enumerable.Range(0, UnitMemory.TroopTally).Select(idx => (short)idx)
					.Where(idx => troops.Exists(participantIdx, idx)).ToList();

				if (present.Count == 0)
					continue;

				short troopIdx = present[chance.Next(present.Count)];
				(int horizLoc, int vertLoc) = ChooseTile(engine, chance);
				short season = new short[] { -4000, -500, 1000, 1001, 1600 }[chance.Next(5)];
				short roundTally = new short[] { 0, 1, 120 }[chance.Next(3)];
				uint seed = (uint)chance.Next();
				byte hutKind = (byte)chance.Next(8);

				pair.Prepare(game =>
				{
					UnitMemory gameTroops = new UnitMemory(game.Processor);
					gameTroops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
					gameTroops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
					game.World.Season = season;
					game.World.RoundTally = roundTally;
					game.Processor.EmitGlobalByte(0xba06, 0);
					// The layer of the map picture that VisitHut reads to choose an advanced tribe (above 4)
					game.VideoCard.AssignDot(2, horizLoc + 80, vertLoc, hutKind, 0);
					game.Libc.SeedRandom(seed);
				});

				string scenario = $"AUTO{this.bay:00} scenario {scenarioIdx}: VisitHut({participantIdx}, {troopIdx}) at {horizLoc}, {vertLoc}, " +
					$"year {season}, turn {roundTally}, seed {seed}, hut layer {hutKind}";

				int foundings = pair.Rewritten.Calls.GetValueOrDefault("EstablishCity");

				string? difference = pair.Run(scenario,
					game => new UnitManagementReference(game).VisitHut(participantIdx, troopIdx),
					game => game.UnitOps.VisitHut(participantIdx, troopIdx),
					DifferentialPair.Register.Kept);

				Assert.True(difference == null, difference);

				// The new city overwrites the text, so an advanced tribe is told by the call of EstablishCity
				string outcome = pair.Rewritten.Calls.GetValueOrDefault("EstablishCity") > foundings ? "advanced tribe" : Outcome(pair.Rewritten.Engine);
				outcomes[outcome] = outcomes.GetValueOrDefault(outcome) + 1;
			}

			this.output.WriteLine(string.Join(", ", outcomes.OrderBy(o => o.Key).Select(o => $"{o.Key}: {o.Value}")));

			foreach (string outcome in new[] { "advanced tribe", "metal deposits", "scrolls", "mercenaries", "barbarians", "nothing" })
				Assert.True(outcomes.ContainsKey(outcome), $"No scenario met the outcome '{outcome}'");
		}

		/// <summary>A random land tile, in a third of the cases at most 3 tiles from a city</summary>
		private static (int, int) ChooseTile(GameEngine engine, Random chance)
		{
			List<City> towns = engine.World.Towns.Where(town => town.ConditionMarker != 0xff).ToList();

			while (true)
			{
				int horizLoc;
				int vertLoc;

				if (towns.Count != 0 && chance.Next(3) == 0)
				{
					City town = towns[chance.Next(towns.Count)];
					horizLoc = (town.Place.X + chance.Next(-3, 4) + MapRules.Width) % MapRules.Width;
					vertLoc = town.Place.Y + chance.Next(-3, 4);
				}
				else
				{
					horizLoc = chance.Next(MapRules.Width);
					vertLoc = chance.Next(1, MapRules.Height - 1);
				}

				if (vertLoc > 0 && vertLoc < MapRules.Height - 1 && engine.MapOps.Chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain)
					return (horizLoc, vertLoc);
			}
		}

		/// <summary>The outcome of the hut by its text at 0xba06</summary>
		private static string Outcome(GameEngine engine)
		{
			string text = engine.Processor.PullText(CPU.AsLinearAddress(engine.Processor.SegData.Value16, 0xba06));

			foreach (string outcome in new[] { "advanced tribe", "metal deposits", "scrolls", "mercenaries", "barbarians" })
			{
				if (text.Contains(outcome, StringComparison.Ordinal))
					return outcome;
			}

			return "nothing";
		}
	}

	public class VisitHutDifferentialAuto03(ITestOutputHelper output) : VisitHutDifferentialTests(3, output);
	public class VisitHutDifferentialAuto10(ITestOutputHelper output) : VisitHutDifferentialTests(10, output);
}
