using System.Diagnostics;
using Civ_1_5_Remastered.Tests.Solution;
using Xunit.Abstractions;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	public class ComputerExpansionMeasurementTests
	{
		private readonly ITestOutputHelper output;

		public ComputerExpansionMeasurementTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public async Task ProbeSimulation()
		{
			if (GameFiles.Folder == null || Environment.GetEnvironmentVariable("CIV_SIMULATE") == null)
			{
				return;
			}

			string save = Environment.GetEnvironmentVariable("CIV_SIMULATE_SAVE") ?? "CIVIL0.SVE";
			int rounds = int.Parse(Environment.GetEnvironmentVariable("CIV_SIMULATE_ROUNDS") ?? "3");
			ComputerExpansionSimulation.Expansion = Environment.GetEnvironmentVariable("CIV_SIMULATE_ORIGINAL") == null;

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation simulation = new ComputerExpansionSimulation(save);
				Assert.True(simulation.Loaded);
				this.output.WriteLine($"round {simulation.Engine.World.RoundTally} level {simulation.Engine.World.HardnessTier} cities {string.Join(",", simulation.CitiesPerCivilization())}");
				Stopwatch watch = Stopwatch.StartNew();

				for (int round = 0; round < rounds; round++)
				{
					simulation.PlayRound();
					if ((round + 1) % 10 != 0 && round + 1 != rounds)
					{
						continue;
					}

					this.output.WriteLine($"round {simulation.Engine.World.RoundTally} cities {string.Join(",", simulation.CitiesPerCivilization())} ({watch.ElapsedMilliseconds} ms)");

					if (Environment.GetEnvironmentVariable("CIV_SIMULATE_DIAGNOSE") != null)
					{
						this.output.WriteLine(simulation.Diagnose());
					}
				}
			}).WaitAsync(TimeSpan.FromMinutes(30));

			ComputerExpansionSimulation.Expansion = true;
		}
	}
}
