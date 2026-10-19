using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>One call of a function in both games: the setup, the original and the rewritten call, what is compared</summary>
	/// <param name="Branch">Names the branch of the function the scenario took, from the rewritten game after the call</param>
	public sealed record Scenario(string Name, Action<GameEngine> Prepare, Action<GameEngine> Original, Action<GameEngine> Rewritten,
		DifferentialPair.Register Registers, Func<GameEngine, string>? Branch = null, (ushort Segment, ushort Begin, int Length)[]? Watched = null);

	/// <summary>Runs the scenarios of a differential test one after the other on one pair of games</summary>
	public static class DifferentialRun
	{
		/// <param name="make">Makes the next scenario (from the state of the original game), or null to skip one</param>
		/// <param name="branches">Every one of them must be met by some scenario</param>
		/// <param name="fresh">Every scenario starts from the saved game (for scenarios that change the state)</param>
		/// <param name="runSeconds">The longest a scenario may run in one game before it counts as hanging</param>
		public static void Check(int bay, int scenarioTally, Func<Random, DifferentialPair, Scenario?> make, ITestOutputHelper output,
			string[] branches, int[]? script = null, bool fresh = false, int runSeconds = 120)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using DifferentialPair pair = new DifferentialPair(gameFolder!, bay, script: script) { RunSeconds = runSeconds };
			Dictionary<string, int> met = new Dictionary<string, int>(StringComparer.Ordinal);
			Random chance = new Random(bay);

			for (int scenarioIdx = 0; scenarioIdx < scenarioTally; scenarioIdx++)
			{
				// (fresh: every scenario starts from the state after loading)
				if (fresh)
					pair.Restore();

				Scenario? scenario = make(chance, pair);

				if (scenario == null)
					continue;

				pair.Prepare(scenario.Prepare);

				string? difference = pair.Run($"AUTO{bay:00} scenario {scenarioIdx}: {scenario.Name}", scenario.Original, scenario.Rewritten,
					scenario.Registers, scenario.Watched);

				Assert.True(difference == null, difference);

				string branch = scenario.Branch?.Invoke(pair.Rewritten.Engine) ?? "run";
				met[branch] = met.GetValueOrDefault(branch) + 1;
			}

			output.WriteLine(string.Join(", ", met.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}: {entry.Value}")));

			foreach (string branch in branches)
				Assert.True(met.ContainsKey(branch), $"No scenario met the branch '{branch}' (met: {string.Join(", ", met.Keys)})");
		}

		/// <summary>A random unit of the player, or of a random player when participantIdx is -1</summary>
		public static (short, short)? RandomUnit(GameEngine engine, Random chance, int participantIdx = -1, Func<short, short, bool>? where = null)
		{
			UnitMemory troops = new UnitMemory(engine.Processor);
			short player = (short)(participantIdx < 0 ? chance.Next(8) : participantIdx);
			List<short> present = Enumerable.Range(0, UnitMemory.TroopTally).Select(idx => (short)idx)
				.Where(idx => troops.Exists(player, idx) && (where == null || where(player, idx))).ToList();

			return present.Count == 0 ? null : (player, present[chance.Next(present.Count)]);
		}

		/// <summary>A random tile of the map (not the polar rows), only land when wanted</summary>
		public static (int, int) RandomTile(GameEngine engine, Random chance, bool landOnly = false)
		{
			while (true)
			{
				int horizLoc = chance.Next(MapRules.Width);
				int vertLoc = chance.Next(1, MapRules.Height - 1);

				if (!landOnly || engine.MapOps.Chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain)
					return (horizLoc, vertLoc);
			}
		}

		/// <summary>How many times the function was called in the game so far</summary>
		public static int Calls(GameEngine engine, DifferentialPair pair, string name)
		{
			HeadlessGame game = engine == pair.Original.Engine ? pair.Original : pair.Rewritten;

			return game.Calls.GetValueOrDefault(name);
		}
	}
}
