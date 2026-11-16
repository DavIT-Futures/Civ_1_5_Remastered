using Civ_1_5_Remastered.Core.Engine.Session;
using Civ_1_5_Remastered.Tests.Core.Game;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Engine
{
	/// <summary>
	/// The autosave is made as the player ends the turn and loads like a game saved in the middle of the turn: the
	/// player's cities and the civilizations before the player do not play the turn a second time, and the units keep
	/// the moves they had. An autosave of an older version, made after the round, still gives the units their moves.
	/// </summary>
	public class AutosaveTests
	{
		private const ushort MiddleOfTheTurn = 1;
		private const ushort AfterTheRound = 2;

		[Fact]
		public async Task AnAutosaveReturnsToTheEndOfTheSameTurn()
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

				int bay = ((engine.World.RoundTally % SaveGameStore.BackupBayTally) + SaveGameStore.BackupBayTally) % SaveGameStore.BackupBayTally;
				ushort slot = (ushort)(SaveGameStore.BackupBayMarker | bay);

				engine.SaveLoad.PerformBackup();
				Assert.False(engine.World.Extras.AtTurnEnd);

				engine.SaveLoad.LoadGameFromSlot(slot);
				Assert.Equal(MiddleOfTheTurn, engine.Globals.Files.MidTurnSave);
				Assert.False(engine.World.Extras.AtTurnEnd);

				// An autosave of an older version has no marker.
				engine.SaveLoad.SaveGameContent($"AUTO{bay:00}.SVE");
				engine.SaveLoad.LoadGameFromSlot(slot);
				Assert.Equal(AfterTheRound, engine.Globals.Files.MidTurnSave);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
