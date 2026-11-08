using System.Reflection;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Captures the city panorama of a new building over the map (opt-in tool: CIV_PREVIEW=folder, CIV_PREVIEW_VIEW=30x24).</summary>
	public class CityPanoramaPreview
	{
		[Fact]
		public async Task Render()
		{
			string? output = Environment.GetEnvironmentVariable("CIV_PREVIEW");
			if (GameFiles.Folder == null || output == null)
			{
				return;
			}

			string[] size = (Environment.GetEnvironmentVariable("CIV_PREVIEW_VIEW") ?? "15x12").Split('x');

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation("CIVIL0.SVE");
				short person = game.Engine.World.PersonParticipantIndex;
				game.Engine.World.ParticipantMarkers = (short)(1 << person);
				game.Engine.Globals.Screen.VideoGraphicsMode = 0;
				game.Engine.MapViewport.Request(int.Parse(size[0]), int.Parse(size[1]));
				game.Engine.TurnHooks.RepaintMainScreen();
				short town = (short)Array.FindIndex(game.Engine.World.Towns, candidate => candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == person);
				game.Feeding = false;
				Thread scene = new Thread(() => game.Engine.CityScene.ShowCityScreen(town, 3)) { IsBackground = true };
				scene.Start();
				MethodInfo save = typeof(MapScreenPreview).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Static)!;

				for (int shot = 0; shot < 4; shot++)
				{
					Thread.Sleep(700);
					lock (game.Engine.VideoCard.VideoLatch)
					{
						save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"pano_{size[0]}_{shot}.png") });
					}
				}

				game.Feeding = true;
				scene.Join(5000);
				game.Engine.TurnHooks.RepaintMainScreen();
				save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"pano_{size[0]}_after.png") });
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
