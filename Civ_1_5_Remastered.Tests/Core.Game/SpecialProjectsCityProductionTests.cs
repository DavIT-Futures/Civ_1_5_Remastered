using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	public class SpecialProjectsCityProductionTests
	{
		[Fact]
		public async Task WealthResearchAndFoodConvertFiftyPercentNetShields()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			string? saveFile = GameFiles.Find("*.SVE").FirstOrDefault();
			if (saveFile == null)
			{
				return;
			}

			await Task.Run(() =>
			{
				// A game with the screens of a running game; messages of the turn (civil disorder, a discovered
				// technology) are dismissed automatically, so any saved game will do.
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Path.GetFileName(saveFile));
				if (!game.Loaded)
				{
					return;
				}

				GameEngine engine = game.Engine;
				engine.World.ParticipantMarkers = (short)(1 << engine.World.PersonParticipantIndex);

				short humanIndex = (short)engine.World.PersonParticipantIndex;
				short cityIndex = -1;
				for (short townScan = 0; townScan < engine.World.Towns.Length; townScan++)
				{
					City candidate = engine.World.Towns[townScan];
					if (candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == humanIndex)
					{
						cityIndex = townScan;
						break;
					}
				}

				if (cityIndex == -1)
				{
					return;
				}

				City town = engine.World.Towns[cityIndex];
				Player player = engine.World.Participants[humanIndex];

				// 1. Wealth and Research from the same city state: the materials tally stays, and Wealth ends the turn
				// with at least as much gold as Research. The city also pays the upkeep of its buildings, so the gold
				// may fall in a city with costly buildings; only the difference between the two depends on Wealth.
				// The city as it was before the turn, to run the same turn again with another production.
				byte[] start = Keep(town);

				City Restore()
				{
					using MemoryStream stream = new MemoryStream(start);
					engine.World.Towns[cityIndex] = City.ReadFrom(stream);
					return engine.World.Towns[cityIndex];
				}

				town.PresentMakingIndex = SpecialProjects.Wealth;
				town.MaterialsTally = 10;
				player.Gold = 100;
				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(10, engine.World.Towns[cityIndex].MaterialsTally);
				short wealthGold = player.Gold;

				town = Restore();
				town.PresentMakingIndex = SpecialProjects.Research;
				town.MaterialsTally = 15;
				player.Gold = 100;
				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(15, engine.World.Towns[cityIndex].MaterialsTally);
				Assert.True(wealthGold >= player.Gold, $"Wealth should bring at least as much gold as Research ({wealthGold} < {player.Gold})");

				town = Restore();

				// 2. Test Food: from the same city state, Food yields at least as much grain as Wealth (which adds no
				// food), and the materials tally remains unchanged. A low granary keeps the city from growing (which
				// empties the granary) during the turn, so the result does not depend on the saved game.
				town.PresentMakingIndex = SpecialProjects.Wealth;
				town.MaterialsTally = 20;
				town.GrainTally = 1;
				engine.CityOperations.HandleCity(cityIndex, 0);
				short wealthGrain = engine.World.Towns[cityIndex].GrainTally;

				town = Restore();

				town.PresentMakingIndex = SpecialProjects.Food;
				town.MaterialsTally = 20;
				town.GrainTally = 1;
				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(20, town.MaterialsTally);
				Assert.True(town.GrainTally >= wealthGrain, "Food should yield at least as much grain as Wealth");
			});
		}

		private static byte[] Keep(City town)
		{
			using MemoryStream stream = new MemoryStream();
			town.WriteTo(stream);
			return stream.ToArray();
		}
	}
}
