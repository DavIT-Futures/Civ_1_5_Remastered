using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>A map size chosen in the View menu while the game waits for the player's order is shown at once.</summary>
	public class MapViewportSwitchTests
	{
		[Fact]
		public async Task ChosenSizeAppliesWhileWaitingForAnOrder()
		{
			if (GameFiles.Folder == null || !File.Exists(Path.Combine(GameFiles.Folder, "CIVIL0.SVE")))
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation("CIVIL0.SVE");
				short person = game.Engine.World.PersonParticipantIndex;
				game.Engine.World.ParticipantMarkers = (short)(1 << person);
				game.Engine.TurnHooks.RepaintMainScreen();
				Assert.Equal(320, game.Engine.VideoCard.Displays[0].Extent.Width);

				game.Engine.MapViewport.Request(30, 24);

				// The blinking cursor of the end of turn waits for a key; the menu choice is applied meanwhile.
				game.Engine.UnitOperations.ChoreographUnitMove(person, 0x80, 0xffff, 0);

				Assert.Equal(560, game.Engine.VideoCard.Displays[0].Extent.Width);
				Assert.Equal(392, game.Engine.VideoCard.Displays[0].Extent.Height);
				Assert.Equal(560, game.Engine.VideoCard.Displays[1].Extent.Width);
				Assert.True(game.Engine.VideoCard.Wide);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
