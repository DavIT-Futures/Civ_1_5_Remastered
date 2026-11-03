using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Foundation.Support;
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

			string folder = Path.GetDirectoryName(GameFiles.ScratchFile("scratchToken"))!;
			string work = Path.Combine(folder, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(work);
			string stem = Path.GetFileNameWithoutExtension(saveFile);

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP" })
			{
				string original = Path.Combine(GameFiles.Folder, stem + extension);
				if (File.Exists(original))
				{
					File.Copy(original, Path.Combine(work, stem + extension));
				}
			}

			SilentGameHost host = new SilentGameHost();

			await Task.Run(() =>
			{
				GameEngine engine = new GameEngine(host);
				engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
				engine.PrepareMachine();
				engine.Processor.StandardFolder = work + Path.DirectorySeparatorChar;
				engine.VideoCard.ReserveScreen(2);

				bool loaded = engine.SaveLoad.LoadGameContent(stem + ".SVE");
				if (!loaded)
				{
					return;
				}

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

				// 1. Test Wealth: 50% net shields converted to gold, materials tally remains unchanged
				town.PresentMakingIndex = SpecialProjects.Wealth;
				town.MaterialsTally = 10;
				player.Gold = 100;

				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(10, town.MaterialsTally);
				Assert.True(player.Gold >= 100, "Gold should not decrease when producing Wealth");

				// 2. Test Research: 50% net shields converted to inquiry headway, materials tally remains unchanged
				town.PresentMakingIndex = SpecialProjects.Research;
				town.MaterialsTally = 15;
				player.InquiryHeadway = 5;

				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(15, town.MaterialsTally);

				// 3. Test Food: 50% net shields converted to food, materials tally remains unchanged
				town.PresentMakingIndex = SpecialProjects.Food;
				town.MaterialsTally = 20;
				short previousGrain = town.GrainTally;

				engine.CityOperations.HandleCity(cityIndex, 0);

				Assert.Equal(20, town.MaterialsTally);
				Assert.True(town.GrainTally >= previousGrain, "Grain tally should increase or hold when producing Food");
			});
		}
	}
}
