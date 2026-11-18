using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Runs a test on the first saved game that suits it: the autosaves change whenever the game is played.</summary>
	internal static class FittingSaves
	{
		/// <summary>Runs <paramref name="attempt"/> on the saved games in turn until one suits it (it returns true).</summary>
		public static async Task OnFirstFittingSave(TimeSpan patience, Func<ComputerExpansionSimulation, string, bool> attempt)
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			foreach (string save in GameFiles.Find("*.SVE").Select(Path.GetFileName).OfType<string>())
			{
				bool fitted = await Task.Run(() =>
				{
					using ComputerExpansionSimulation game = new ComputerExpansionSimulation(save);

					return game.Loaded && attempt(game, save);
				}).WaitAsync(patience);

				if (fitted)
				{
					return;
				}
			}
		}
	}
}
