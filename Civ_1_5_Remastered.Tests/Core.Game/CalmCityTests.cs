using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Assigning entertainers before the end of turn calms the player's cities on a real save.</summary>
	public class CalmCityTests
	{
		[Fact]
		public async Task EntertainersCalmRiotingCities()
		{
			if (GameFiles.Folder == null || !File.Exists(Path.Combine(GameFiles.Folder, "CIVIL0.SVE")))
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation("CIVIL0.SVE");
				Assert.True(game.Loaded);

				short person = game.Engine.World.PersonParticipantIndex;
				game.Engine.World.ParticipantMarkers = (short)(1 << person);
				Player player = game.Engine.World.Participants[person];

				// Without luxuries and with all trade spent on taxes, some cities riot.
				player.ResearchLevyRatio = 0;
				player.LevyRatio = 10;

				int riots = 0;

				for (short townIndex = 0; townIndex < game.Engine.World.Towns.Length; townIndex++)
				{
					City town = game.Engine.World.Towns[townIndex];

					if (town.ConditionMarker == 0xff || town.ParticipantIndex != person || (short)game.Engine.CityOperations.HandleCity(townIndex, -1) >= 0)
					{
						continue;
					}

					riots++;

					Assert.True(game.Engine.CityOperations.CalmCity(townIndex));
					Assert.True((short)game.Engine.CityOperations.HandleCity(townIndex, -1) >= 0);
				}

				Assert.True(riots > 0);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
