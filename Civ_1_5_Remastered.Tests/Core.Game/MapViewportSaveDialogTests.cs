using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>The save game dialog draws a 320 x 200 screen; closing it must bring back the enlarged map unchanged.</summary>
	public class MapViewportSaveDialogTests
	{
		[Fact]
		public async Task MapLayoutReturnsAfterTheSaveDialog()
		{
			if (GameFiles.Folder == null || !File.Exists(Path.Combine(GameFiles.Folder, "CIVIL0.SVE")))
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation("CIVIL0.SVE");
				game.Engine.World.ParticipantMarkers = (short)(1 << game.Engine.World.PersonParticipantIndex);
				game.Engine.MapViewport.Request(30, 24);
				game.Engine.TurnHooks.RepaintMainScreen();

				byte[] before = (byte[])game.Engine.VideoCard.Displays[0].PictureStorage.Clone();

				// The dialog is dismissed by the simulated Esc key.
				game.Engine.SaveLoad.ShowStoreGameDialog(0);

				Assert.True(game.Engine.VideoCard.Wide);
				Assert.Equal(before, game.Engine.VideoCard.Displays[0].PictureStorage);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
