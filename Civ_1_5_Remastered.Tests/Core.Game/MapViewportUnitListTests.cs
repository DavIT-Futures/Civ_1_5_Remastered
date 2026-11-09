using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Clicking a fortified unit on an enlarged map shows the list of units centred and wakes the chosen unit.</summary>
	public class MapViewportUnitListTests
	{
		[Fact]
		public async Task UnitListIsCentredAndWakesTheUnit()
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

				// Without a mouse the list is answered with keys: Enter picks the first unit.
				game.Engine.Globals.Mouse.Present = 0;
				UnitMemory troops = new UnitMemory(game.Engine.Processor);
				short troop = (short)Enumerable.Range(0, UnitMemory.TroopTally).First(index => troops.Exists(person, index));

				troops.Assign(person, troop, UnitMemory.Status, (byte)UnitStatus.Fortified);
				troops.Assign(person, troop, UnitMemory.LeftoverSteps, 3);

				game.Engine.MapViewport.Request(30, 24);
				game.Engine.TurnHooks.RepaintMainScreen();

				byte[] before = (byte[])game.Engine.VideoCard.Displays[0].PictureStorage.Clone();
				byte[]? shown = null;

				lock (game.Engine.VideoCard.Tokens)
				{
					game.Engine.VideoCard.Tokens.Clear();
				}

				game.Waiting = step =>
				{
					shown ??= (byte[])game.Engine.VideoCard.Displays[0].PictureStorage.Clone();
				};

				game.Engine.UnitOperations.ShowBattleScreen(person, troop);
				game.Waiting = null;

				Assert.NotNull(shown);

				// Everything the list changed lies in the centred 320 x 200 area (origin 120, 96 on a 560 x 392 screen).
				int pitch = game.Engine.VideoCard.Displays[0].Pitch;
				int changed = 0;

				for (int index = 0; index < before.Length; index++)
				{
					if (before[index] == shown![index])
					{
						continue;
					}

					int column = index % pitch;
					int row = index / pitch;
					Assert.InRange(column, 120, 120 + 319);
					Assert.InRange(row, 96, 96 + 199);
					changed++;
				}

				Assert.True(changed > 0);
				Assert.Equal(0, troops.Pull(person, troop, UnitMemory.Status) & (int)UnitStatus.Fortified);
				Assert.Equal(3, troops.Pull(person, troop, UnitMemory.LeftoverSteps));
				Assert.True(game.Engine.VideoCard.Wide);
				Assert.Equal(before, game.Engine.VideoCard.Displays[0].PictureStorage);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
