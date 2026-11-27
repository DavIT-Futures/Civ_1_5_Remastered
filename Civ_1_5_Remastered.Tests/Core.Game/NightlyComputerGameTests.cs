using System.Diagnostics;
using System.Text;
using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;
using Xunit.Abstractions;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// Nightly check of the computer players (opt-in, CIV_NIGHTLY=1, run by .github/workflows/nightly-ai.yml): plays
	/// about 200 rounds of several saved games with every civilization computer controlled and fails on an exception,
	/// a round that hangs, a unit or city off the map, or a broken unit record. The cities of every civilization are
	/// written each round to the test output and to a CSV file (CIV_NIGHTLY_REPORT, a folder).
	/// </summary>
	public class NightlyComputerGameTests
	{
		private const int MapColumns = 80;
		private const int MapRows = 50;

		private readonly ITestOutputHelper output;

		public NightlyComputerGameTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		/// <summary>
		/// The games played each night: EUROPE (a new game on the Europe map, see ComputerExpansionSimulation.NewEuropeGame),
		/// CIVIL10 (18 small islands, round 11), CIVIL12 (one large continent, round 14) and CIVIL15 (15 landmasses,
		/// round 49, wars). CIV_NIGHTLY_SAVES overrides the list.
		/// </summary>
		public static TheoryData<string> Saves()
		{
			TheoryData<string> saves = new TheoryData<string>();
			string list = Environment.GetEnvironmentVariable("CIV_NIGHTLY_SAVES") ?? "EUROPE,CIVIL10,CIVIL12,CIVIL15";

			foreach (string save in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				saves.Add(save);
			}

			return saves;
		}

		[Theory]
		[MemberData(nameof(Saves))]
		public async Task ComputerPlaysRoundsWithoutFailing(string save)
		{
			if (GameFiles.Folder == null || Environment.GetEnvironmentVariable("CIV_NIGHTLY") == null)
			{
				return;
			}

			int rounds = int.Parse(Environment.GetEnvironmentVariable("CIV_NIGHTLY_ROUNDS") ?? "200");
			TimeSpan roundLimit = TimeSpan.FromSeconds(int.Parse(Environment.GetEnvironmentVariable("CIV_NIGHTLY_ROUND_SECONDS") ?? "120"));
			string? reportFolder = Environment.GetEnvironmentVariable("CIV_NIGHTLY_REPORT");
			StringBuilder csv = new StringBuilder("round,year_round,barbarians,civ1,civ2,civ3,civ4,civ5,civ6,civ7,total,units,ms\n");
			List<string> problems = new List<string>();

			using ComputerExpansionSimulation simulation = new ComputerExpansionSimulation(save + ".SVE");
			Assert.True(simulation.Loaded, $"{save}: the save did not load");

			this.output.WriteLine($"{save}: round {simulation.Engine.World.RoundTally} level {simulation.Engine.World.HardnessTier} cities {string.Join(",", simulation.CitiesPerCivilization())}");
			Stopwatch total = Stopwatch.StartNew();

			try
			{
				for (int round = 1; round <= rounds; round++)
				{
					Stopwatch watch = Stopwatch.StartNew();
					Task play = Task.Run(simulation.PlayRound);

					try
					{
						await play.WaitAsync(roundLimit);
					}
					catch (TimeoutException)
					{
						Assert.Fail($"{save}: round {round} (game round {simulation.Engine.World.RoundTally}) hung for more than {roundLimit.TotalSeconds} s");
					}

					int[] cities = simulation.CitiesPerCivilization();
					int units = CheckUnits(simulation, save, round, problems);
					CheckCities(simulation, save, round, problems);

					csv.Append($"{round},{simulation.Engine.World.RoundTally},{string.Join(",", cities)},{cities.Sum()},{units},{watch.ElapsedMilliseconds}\n");
					this.output.WriteLine($"round {round,3} (game {simulation.Engine.World.RoundTally}) cities {string.Join(",", cities)} total {cities.Sum()} units {units} ({watch.ElapsedMilliseconds} ms)");

					Assert.True(problems.Count == 0, $"{save}: round {round}\n{string.Join("\n", problems.Take(20))}");
				}
			}
			finally
			{
				this.output.WriteLine($"{save}: {total.Elapsed.TotalSeconds:0} s, siege orders none/advance/hold/assault/raid {string.Join(",", ComputerExpansionSimulation.SiegeOrders)}");

				if (reportFolder != null)
				{
					Directory.CreateDirectory(reportFolder);
					File.WriteAllText(Path.Combine(reportFolder, save + ".csv"), csv.ToString());
				}
			}
		}

		/// <summary>Every unit stands on the map and has a known kind; returns the number of units.</summary>
		private static int CheckUnits(ComputerExpansionSimulation simulation, string save, int round, List<string> problems)
		{
			UnitMemory troops = new UnitMemory(simulation.Engine.Processor);
			int count = 0;

			for (short participant = 0; participant < 8; participant++)
			{
				for (short troop = 0; troop < UnitMemory.TroopTally; troop++)
				{
					sbyte kind = troops.TroopKind(participant, troop);

					if (kind == -1)
					{
						continue;
					}

					count++;
					int column = troops.PullSigned(participant, troop, UnitMemory.Horizontal);
					int row = troops.PullSigned(participant, troop, UnitMemory.Vertical);

					if (kind < 0 || kind >= Civ_1_5_Remastered.GameModel.Rules.UnitKinds.Count)
					{
						problems.Add($"{save} round {round}: civ {participant} unit {troop} has kind {kind}");
					}

					if (column < 0 || column >= MapColumns || row < 0 || row >= MapRows)
					{
						problems.Add($"{save} round {round}: civ {participant} unit {troop} (kind {kind}) is off the map at {column},{row}");
					}
				}
			}

			return count;
		}

		private static void CheckCities(ComputerExpansionSimulation simulation, string save, int round, List<string> problems)
		{
			foreach (City town in simulation.Engine.World.Towns)
			{
				if (town.ConditionMarker == 0xff)
				{
					continue;
				}

				if (town.Place.X < 0 || town.Place.X >= MapColumns || town.Place.Y < 0 || town.Place.Y >= MapRows)
				{
					problems.Add($"{save} round {round}: a city of civ {town.ParticipantIndex} is off the map at {town.Place.X},{town.Place.Y}");
				}

				if (town.ParticipantIndex < 0 || town.ParticipantIndex >= 8)
				{
					problems.Add($"{save} round {round}: a city at {town.Place.X},{town.Place.Y} belongs to civ {town.ParticipantIndex}");
				}
			}
		}
	}
}
