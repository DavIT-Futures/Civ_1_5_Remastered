using Civ_1_5_Remastered.Tests.Core.Game;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.UI
{
	/// <summary>
	/// The War Advisor (F12) on a saved game: the summary, the details of the first civilization (1) and its
	/// conditions page (C) each draw a screen of their own, and Esc leaves the advisor.
	/// </summary>
	public class WarAdvisorScreenTests
	{
		[Fact]
		public async Task TheWarAdvisorShowsItsPagesAndCloses()
		{
			if (GameFiles.Folder == null || GameFiles.Find("*.SVE").FirstOrDefault() is not string save)
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Path.GetFileName(save));

				if (!game.Loaded)
				{
					return;
				}

				var engine = game.Engine;
				short person = engine.World.PersonParticipantIndex;
				engine.World.ParticipantMarkers = (short)(1 << person);
				engine.TurnHooks.RepaintMainScreen();

				// The keys, one each time the advisor waits: the details of the first rival, its conditions, back to
				// the summary, and out. Each page is kept as the advisor shows it.
				int[] keys = ['1', 'C', 0x0d, 0x1b];
				List<byte[]> pages = new List<byte[]>();

				lock (engine.VideoCard.Tokens)
				{
					engine.VideoCard.Tokens.Clear();
				}

				game.Waiting = step =>
				{
					pages.Add((byte[])engine.VideoCard.Displays[0].PictureStorage.Clone());
					game.StepKey = keys[Math.Min(step, keys.Length - 1)];
				};

				engine.Briefings.ShowWarReport(person);

				game.Waiting = null;

				// The summary, then (with a rival known) the details and the conditions page.
				Assert.NotEmpty(pages);
				Assert.Contains(pages[0], dot => dot != pages[0][0]);

				if (pages.Count >= 3)
				{
					Assert.False(pages[1].SequenceEqual(pages[0]), "the details look like the summary");
					Assert.False(pages[2].SequenceEqual(pages[1]), "the conditions look like the details");
				}
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
