using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Loading a game during play re-cuts the unit and terrain sprites; on an enlarged map they must stay the same.</summary>
	public class MapViewportLoadTests
	{
		[Fact]
		public async Task SpritesSurviveLoadingDuringPlayOnAnEnlargedMap()
		{
			if (GameFiles.Folder == null || !File.Exists(Path.Combine(GameFiles.Folder, "CIVIL0.SVE")))
			{
				return;
			}

			List<byte[]> original = await Task.Run(() => SpritesAfterLoad(15, 12)).WaitAsync(TimeSpan.FromMinutes(2));
			List<byte[]> enlarged = await Task.Run(() => SpritesAfterLoad(30, 24)).WaitAsync(TimeSpan.FromMinutes(2));

			Assert.Equal(original.Count, enlarged.Count);

			for (int index = 0; index < original.Count; index++)
			{
				Assert.True(original[index].SequenceEqual(enlarged[index]), $"sprite {index} differs");
			}
		}

		private static List<byte[]> SpritesAfterLoad(int columns, int rows)
		{
			using ComputerExpansionSimulation game = new ComputerExpansionSimulation("CIVIL0.SVE");
			game.Engine.World.ParticipantMarkers = (short)(1 << game.Engine.World.PersonParticipantIndex);
			game.Engine.MapViewport.Request(columns, rows);
			game.Engine.TurnHooks.RepaintMainScreen();

			// Loading a game during play clears the screen and cuts the sprites again (GameLauncher.LoadGameDuringPlay),
			// in the VGA mode from TER257.PIC loaded into the back screen.
			game.Engine.Services.FloodBox(game.Engine.Globals.Screen.Box, 0, 0, 320, 200, 7);
			game.Engine.Globals.Screen.VideoGraphicsMode = 1;
			game.Engine.NewGameWizard.ShowTribeSelection();

			List<byte[]> sprites = new List<byte[]>();

			for (int index = 0; index < 0xa0; index++)
			{
				ushort handle = game.Engine.Processor.PullGlobalWord((ushort)(0xb886 + (index << 1)));

				if (game.Engine.VideoCard.Pictures.TryGetValue(handle, out var picture))
				{
					sprites.Add((byte[])picture.PictureStorage.Clone());
				}
				else
				{
					sprites.Add(Array.Empty<byte>());
				}
			}

			Assert.Equal((columns * 16) + 80, game.Engine.VideoCard.Displays[0].Extent.Width);

			return sprites;
		}
	}
}
