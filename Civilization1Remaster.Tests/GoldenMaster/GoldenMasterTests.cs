using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Civilization1Remaster.Tests.GoldenMaster
{
	/// <summary>
	/// Golden master (characterization) test of one autosave: the emulated game plays a few turns without a window, and the
	/// state after every turn must match the baseline recorded from the code before the refactoring. See README.md here.
	/// </summary>
	/// <remarks>
	/// Every autosave has its own small test class, because xUnit runs the classes in parallel, but not the rows of a theory.
	/// </remarks>
	public abstract class GoldenMasterTests
	{
		/// <summary>Turns played from every autosave</summary>
		public const int TurnTally = 5;

		/// <summary>Seed of the C runtime random generator (instead of the clock)</summary>
		public const uint RandomSeed = 1991;

		/// <summary>Set to 1 to write the baselines from the current code instead of comparing with them</summary>
		public const string UpdateVariable = "CIV1_GOLDEN_MASTER_UPDATE";

		/// <summary>Written into the baseline header, for example the commit the baseline was made from</summary>
		public const string SourceVariable = "CIV1_GOLDEN_MASTER_SOURCE";

		/// <summary>Set to 1 to write the last called game function every 2 seconds into %TEMP%\Civ1GoldenMaster-AUTOnn.log</summary>
		public const string TraceVariable = "CIV1_GOLDEN_MASTER_TRACE";

		private readonly int bay;
		private readonly ITestOutputHelper output;

		protected GoldenMasterTests(int bay, ITestOutputHelper output)
		{
			this.bay = bay;
			this.output = output;
		}

		[Fact]
		public void PlayedTurnsMatchBaseline()
		{
			string? gameFolder = FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			string baselineRoute = Path.Combine(SourceFolder(), "Baselines", $"AUTO{this.bay:00}.txt");
			bool update = Environment.GetEnvironmentVariable(UpdateVariable)?.Trim() == "1";

			if (!update)
				Assert.True(File.Exists(baselineRoute), $"Baseline {baselineRoute} is missing, see GoldenMaster/README.md how to create it");

			GoldenMasterBaseline? baseline = update ? null : GoldenMasterBaseline.Read(baselineRoute);
			int turnTally = baseline == null ? TurnTally : baseline.Turns.Count - 1;
			List<GameSnapshot> turns = Play(gameFolder!, this.bay, turnTally, out string ending);

			if (baseline == null)
			{
				string source = Environment.GetEnvironmentVariable(SourceVariable) ?? "working copy";
				string header = $"Golden master of AUTO{this.bay:00}.SVE: {turns.Count - 1} turns, random seed {RandomSeed}, " +
					$"keys {string.Join(" ", ScriptedKeyboard.DefaultScript.Select(k => $"0x{k:x2}"))}\n" +
					$"Source: {source}\n{ending}";

				new GoldenMasterBaseline(header, turns).Write(baselineRoute);
				this.output.WriteLine($"Baseline written: {baselineRoute}");
				return;
			}

			string? divergence = Compare(baseline.Turns, turns, this.output);

			Assert.True(divergence == null, $"AUTO{this.bay:00} diverges from the baseline ({baseline.Header.Split('\n')[1].Trim()}), {divergence}");
		}

		public static List<GameSnapshot> Play(string gameFolder, int bay, int turnTally, out string ending)
		{
			List<GameSnapshot> turns = new List<GameSnapshot>();
			ending = $"Played {turnTally} turns";

			using HeadlessGame game = new HeadlessGame(gameFolder, Path.Combine(SourceFolder(), "Saves"));

			if (Environment.GetEnvironmentVariable(TraceVariable)?.Trim() == "1")
				game.StartProbe(Path.Combine(Path.GetTempPath(), $"Civ1GoldenMaster-AUTO{bay:00}.log"));

			game.LoadAutosave(bay, RandomSeed);
			turns.Add(game.Capture());

			for (int turnIdx = 1; turnIdx <= turnTally; turnIdx++)
			{
				game.PlayTurn();
				turns.Add(game.Capture());

				if (game.GameEndFlag != 0)
				{
					ending = $"The game ended in turn {turnIdx} (game end flag 0x{game.GameEndFlag:x4})";
					break;
				}
			}

			return turns;
		}

		/// <summary>
		/// Finds the first turn where a behaviour channel differs
		/// </summary>
		/// <returns>Description of the first difference, null when all turns match</returns>
		public static string? Compare(IReadOnlyList<GameSnapshot> expected, IReadOnlyList<GameSnapshot> actual, ITestOutputHelper? output)
		{
			for (int turnIdx = 0; turnIdx < Math.Max(expected.Count, actual.Count); turnIdx++)
			{
				if (turnIdx >= expected.Count || turnIdx >= actual.Count)
					return $"the baseline has {expected.Count - 1} turns, the run has {actual.Count - 1} (the game ended at a different turn)";

				List<string> behaviour = Differences(expected[turnIdx], actual[turnIdx], GameSnapshot.IsBehaviour).ToList();
				List<string> trace = Differences(expected[turnIdx], actual[turnIdx], key => !GameSnapshot.IsBehaviour(key)).ToList();

				if (behaviour.Count > 0)
				{
					StringBuilder text = new StringBuilder();
					text.AppendLine($"first divergence after {(turnIdx == 0 ? "loading" : $"turn {turnIdx}")} ({behaviour.Count} values, expected -> actual):");

					foreach (string line in behaviour.Take(40))
						text.AppendLine($"  {line}");

					if (trace.Count > 0)
					{
						text.AppendLine($"call counts that differ in that turn ({trace.Count}, they show where the paths split):");

						foreach (string line in trace.Take(25))
							text.AppendLine($"  {line}");
					}

					return text.ToString();
				}

				if (trace.Count > 0 && output != null)
				{
					output.WriteLine($"Turn {turnIdx}: same state, {trace.Count} call counts differ (informational):");

					foreach (string line in trace.Take(25))
						output.WriteLine($"  {line}");
				}
			}

			return null;
		}

		public static IEnumerable<string> Differences(GameSnapshot expected, GameSnapshot actual, Func<string, bool> filter)
		{
			foreach (string key in expected.Values.Keys.Union(actual.Values.Keys).Where(filter).OrderBy(k => k, StringComparer.Ordinal))
			{
				expected.Values.TryGetValue(key, out string? before);
				actual.Values.TryGetValue(key, out string? after);

				if (before != after)
					yield return $"{key}: {before ?? "(0)"} -> {after ?? "(0)"}";
			}
		}

		/// <summary>The GoldenMaster folder in the source tree (not in bin), so an update of the baselines lands in the repository</summary>
		public static string SourceFolder()
		{
			DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory);

			while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Civilization1Remaster.Tests.csproj")))
				folder = folder.Parent;

			if (folder == null)
				throw new DirectoryNotFoundException("The source folder of Civilization1Remaster.Tests was not found");

			return Path.Combine(folder.FullName, "GoldenMaster");
		}

		/// <summary>The folder of the original game files, "CIVILIZATION" next to the tests or in a parent folder</summary>
		public static string? FindGameFolder()
		{
			DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory);

			while (folder != null)
			{
				string route = Path.Combine(folder.FullName, "CIVILIZATION");

				if (File.Exists(Path.Combine(route, "CIV.EXE")))
					return route;

				folder = folder.Parent;
			}

			return null;
		}
	}

	/// <summary>
	/// The same game played twice in one process gives the same snapshots, also the call counts.
	/// Without this the baseline comparison would be meaningless.
	/// </summary>
	public class GoldenMasterDeterminismTests
	{
		[Fact]
		public void HeadlessGameIsDeterministic()
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			List<GameSnapshot> first = GoldenMasterTests.Play(gameFolder!, 7, 2, out _);
			List<GameSnapshot> second = GoldenMasterTests.Play(gameFolder!, 7, 2, out _);

			Assert.Equal(first.Count, second.Count);

			for (int turnIdx = 0; turnIdx < first.Count; turnIdx++)
			{
				List<string> differences = GoldenMasterTests.Differences(first[turnIdx], second[turnIdx], key => true).ToList();
				Assert.True(differences.Count == 0, $"Turn {turnIdx} differs between two runs:\n{string.Join("\n", differences.Take(30))}");
			}
		}
	}

	public class GoldenMasterAuto00(ITestOutputHelper output) : GoldenMasterTests(0, output);
	public class GoldenMasterAuto01(ITestOutputHelper output) : GoldenMasterTests(1, output);
	public class GoldenMasterAuto02(ITestOutputHelper output) : GoldenMasterTests(2, output);
	public class GoldenMasterAuto03(ITestOutputHelper output) : GoldenMasterTests(3, output);
	public class GoldenMasterAuto04(ITestOutputHelper output) : GoldenMasterTests(4, output);
	public class GoldenMasterAuto05(ITestOutputHelper output) : GoldenMasterTests(5, output);
	public class GoldenMasterAuto06(ITestOutputHelper output) : GoldenMasterTests(6, output);
	public class GoldenMasterAuto07(ITestOutputHelper output) : GoldenMasterTests(7, output);
	public class GoldenMasterAuto08(ITestOutputHelper output) : GoldenMasterTests(8, output);
	public class GoldenMasterAuto09(ITestOutputHelper output) : GoldenMasterTests(9, output);
	public class GoldenMasterAuto10(ITestOutputHelper output) : GoldenMasterTests(10, output);
	public class GoldenMasterAuto11(ITestOutputHelper output) : GoldenMasterTests(11, output);
	public class GoldenMasterAuto12(ITestOutputHelper output) : GoldenMasterTests(12, output);
	public class GoldenMasterAuto13(ITestOutputHelper output) : GoldenMasterTests(13, output);
	public class GoldenMasterAuto14(ITestOutputHelper output) : GoldenMasterTests(14, output);
}
