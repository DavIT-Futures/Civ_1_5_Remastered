using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>The small map of a revealed world shows the whole world from its first column, wherever the view is.</summary>
	public class SmallMapTests
	{
		[Fact]
		public async Task TheSmallMapOfARevealedWorldDoesNotScroll()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			string? save = GameFiles.Find("*.SVE").FirstOrDefault();

			if (save == null)
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

				// Far to the east: before, the small map scrolled with the view and showed what lay past the map.
				engine.Globals.MapView.Revealed = 0xffff;
				engine.MapOperations.FocusMapView(person, 50, 30);

				int alike = 0;
				int compared = 0;

				for (int row = 1; row < 49; row++)
				{
					for (int column = 1; column < 79; column++)
					{
						int shown = engine.VideoCard.Displays[0].ObtainDot(column, row + 8);

						// The small map is the world of the map planes (screen 2), one dot a tile; the white frame of the window
						// and the dots of the cities lie over it.
						if (shown == 15)
						{
							continue;
						}

						compared++;
						alike += shown == engine.VideoCard.Displays[2].ObtainDot(column, row) ? 1 : 0;
					}
				}

				Assert.True(alike > compared * 9 / 10, $"the small map matches the world in {alike} of {compared} dots");
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
