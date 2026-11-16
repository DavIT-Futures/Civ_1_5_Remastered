using System.Reflection;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// With the world revealed (Alt+D) the units of the other civilizations are drawn as they move, but what they see
	/// is not added to the player's small map: once the world is hidden again, the land they explored stays unknown.
	/// </summary>
	public class RevealedWorldTests
	{
		private const int KnownPlane = 0xf0;

		[Fact]
		public async Task OtherUnitsExploreNothingForThePlayerInARevealedWorld()
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
				short other = (short)(person == 1 ? 2 : 1);
				MethodInfo ring = engine.UnitOperations.GetType().GetMethod("RepaintRingTile", BindingFlags.Instance | BindingFlags.NonPublic)!;

				const int Column = 40;
				const int Row = 25;

				engine.Globals.MapView.Revealed = 0xffff;

				engine.Services.AssignDot(2, Column + KnownPlane, Row, 0);
				ring.Invoke(engine.UnitOperations, new object[] { other, Column, Row });
				Assert.Equal(0, engine.VideoCard.ObtainDot(2, Column + KnownPlane, Row));

				// The player's own units still explore.
				ring.Invoke(engine.UnitOperations, new object[] { person, Column, Row });
				Assert.NotEqual(0, engine.VideoCard.ObtainDot(2, Column + KnownPlane, Row));
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
