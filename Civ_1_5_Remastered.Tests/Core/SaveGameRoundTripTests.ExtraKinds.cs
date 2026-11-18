using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core
{
	/// <summary>
	/// Units of an extra kind (Pikemen) are written to the original save format as their stand-in (a Phalanx), so the
	/// original game reads them; the extras file brings the real kind back.
	/// </summary>
	public partial class SaveGameRoundTripTests
	{
		[Fact]
		public async Task PikemenComeBackFromTheExtrasFile()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			string work = string.Empty;
			(short Participant, short Troop)? pikemen = null;
			short townIndex = -1;
			short[] alive = new short[2];

			// The first saved game in which the player has a city and a unit (the autosaves change as the game is played).
			foreach (string candidate in GameFiles.Find("*.SVE"))
			{
				work = WorkFolderWith(Path.GetFileNameWithoutExtension(candidate));
				string stem = Path.GetFileNameWithoutExtension(candidate);

				bool fitted = await Task.Run(() => MakePikemenAndSave(work, stem));

				if (fitted)
				{
					break;
				}
			}

			if (pikemen == null)
			{
				return;
			}

			bool MakePikemenAndSave(string folder, string stem)
			{
				GameEngine engine = LoadedEngine(folder, stem + ".SVE");
				short person = engine.World.PersonParticipantIndex;
				UnitMemory troops = new UnitMemory(engine.Processor);
				Player player = engine.World.Participants[person];

				short troopIndex = Enumerable.Range(0, UnitMemory.TroopTally).Select(index => (short)index).FirstOrDefault(index => troops.Exists(person, index), (short)-1);
				townIndex = (short)Array.FindIndex(engine.World.Towns, town => town.ConditionMarker != 0xff && town.ParticipantIndex == person);

				if (troopIndex < 0 || townIndex < 0)
				{
					return false;
				}

				int formerKind = troops.TroopKind(person, troopIndex);
				player.LiveTroops[formerKind]--;
				player.LiveTroops[UnitKinds.Pikemen]++;
				troops.Assign(person, troopIndex, UnitMemory.Kind, UnitKinds.Pikemen);
				pikemen = (person, troopIndex);

				engine.World.Towns[townIndex].PresentMakingIndex = UnitKinds.Pikemen;
				alive[0] = player.LiveTroops[UnitKinds.Phalanx];
				alive[1] = player.LiveTroops[UnitKinds.Pikemen];

				Assert.True(engine.SaveLoad.SaveGameContent("PIKEMEN.SVE"));

				// Writing leaves the game as it was.
				Assert.Equal(UnitKinds.Pikemen, troops.TroopKind(person, troopIndex));
				Assert.Equal(UnitKinds.Pikemen, engine.World.Towns[townIndex].PresentMakingIndex);
				Assert.Equal(alive[1], player.LiveTroops[UnitKinds.Pikemen]);

				return true;
			}

			(short Participant, short Troop) spot = pikemen!.Value;
			File.Copy(Path.Combine(work, "PIKEMEN.SVE"), Path.Combine(work, "ORIGINAL.SVE"));
			File.Copy(Path.Combine(work, "PIKEMEN.MAP"), Path.Combine(work, "ORIGINAL.MAP"));

			await Task.Run(() =>
			{
				// Without the extras file (as the original game reads it): a Phalanx, counted with the Phalanxes.
				GameEngine original = LoadedEngine(work, "ORIGINAL.SVE");
				UnitMemory troops = new UnitMemory(original.Processor);
				Player player = original.World.Participants[spot.Participant];

				Assert.Equal(UnitKinds.Phalanx, troops.TroopKind(spot.Participant, spot.Troop));
				Assert.Equal(UnitKinds.Phalanx, original.World.Towns[townIndex].PresentMakingIndex);
				Assert.Equal(alive[0] + alive[1], player.LiveTroops[UnitKinds.Phalanx]);
				Assert.Equal(0, player.LiveTroops[UnitKinds.Pikemen]);

				// With the extras file: Pikemen again, and a second save writes the same game.
				GameEngine remastered = LoadedEngine(work, "PIKEMEN.SVE");
				troops = new UnitMemory(remastered.Processor);
				player = remastered.World.Participants[spot.Participant];

				Assert.Equal(UnitKinds.Pikemen, troops.TroopKind(spot.Participant, spot.Troop));
				Assert.Equal(UnitKinds.Pikemen, remastered.World.Towns[townIndex].PresentMakingIndex);
				Assert.Equal(alive[0], player.LiveTroops[UnitKinds.Phalanx]);
				Assert.Equal(alive[1], player.LiveTroops[UnitKinds.Pikemen]);

				Assert.True(remastered.SaveLoad.SaveGameContent("AGAIN.SVE"));
			}).WaitAsync(TimeSpan.FromSeconds(30));

			Assert.Equal(File.ReadAllBytes(Path.Combine(work, "PIKEMEN.SVE")), File.ReadAllBytes(Path.Combine(work, "AGAIN.SVE")));
			Assert.Equal(File.ReadAllBytes(Path.Combine(work, "PIKEMEN.EXT")), File.ReadAllBytes(Path.Combine(work, "AGAIN.EXT")));
		}

		[Fact]
		public async Task AStockadeComesBackFromTheExtrasFile()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			string work = string.Empty;
			short townIndex = -1;

			// The first saved game with two cities: one has a Stockade, the other is making one.
			foreach (string candidate in GameFiles.Find("*.SVE"))
			{
				string stem = Path.GetFileNameWithoutExtension(candidate);
				work = WorkFolderWith(stem);

				bool fitted = await Task.Run(() =>
				{
					GameEngine engine = LoadedEngine(work, stem + ".SVE");
					int[] towns = Enumerable.Range(0, engine.World.Towns.Length).Where(index => engine.World.Towns[index].ConditionMarker != 0xff).Take(2).ToArray();

					if (towns.Length < 2)
					{
						return false;
					}

					townIndex = (short)towns[0];
					engine.World.Towns[towns[0]].Add(CityStructure.Stockade);
					engine.World.Towns[towns[1]].PresentMakingIndex = -BuildingKinds.Stockade;

					Assert.True(engine.SaveLoad.SaveGameContent("STOCKADE.SVE"));
					Assert.True(engine.World.Towns[towns[0]].Has(CityStructure.Stockade));
					Assert.Equal(-BuildingKinds.Stockade, engine.World.Towns[towns[1]].PresentMakingIndex);

					return true;
				}).WaitAsync(TimeSpan.FromSeconds(30));

				if (fitted)
				{
					break;
				}
			}

			if (townIndex < 0)
			{
				return;
			}

			File.Copy(Path.Combine(work, "STOCKADE.SVE"), Path.Combine(work, "ORIGINAL.SVE"));
			File.Copy(Path.Combine(work, "STOCKADE.MAP"), Path.Combine(work, "ORIGINAL.MAP"));

			await Task.Run(() =>
			{
				// Without the extras file (as the original game reads it): no Stockade, City Walls in production.
				GameEngine original = LoadedEngine(work, "ORIGINAL.SVE");
				City[] towns = original.World.Towns.Where(town => town.ConditionMarker != 0xff).Take(2).ToArray();

				Assert.False(towns[0].Has(CityStructure.Stockade));
				Assert.Equal(-BuildingKinds.CityWalls, towns[1].PresentMakingIndex);

				// With the extras file: the Stockade and its production again, and a second save writes the same game.
				GameEngine remastered = LoadedEngine(work, "STOCKADE.SVE");
				towns = remastered.World.Towns.Where(town => town.ConditionMarker != 0xff).Take(2).ToArray();

				Assert.True(towns[0].Has(CityStructure.Stockade));
				Assert.Equal(-BuildingKinds.Stockade, towns[1].PresentMakingIndex);

				Assert.True(remastered.SaveLoad.SaveGameContent("AGAIN.SVE"));
			}).WaitAsync(TimeSpan.FromSeconds(30));

			Assert.Equal(File.ReadAllBytes(Path.Combine(work, "STOCKADE.SVE")), File.ReadAllBytes(Path.Combine(work, "AGAIN.SVE")));
			Assert.Equal(File.ReadAllBytes(Path.Combine(work, "STOCKADE.EXT")), File.ReadAllBytes(Path.Combine(work, "AGAIN.EXT")));
		}

		private static string WorkFolderWith(string stem)
		{
			string folder = Path.GetDirectoryName(GameFiles.ScratchFile("x"))!;
			string work = Path.Combine(folder, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(work);

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP", ".EXT" })
			{
				string original = Path.Combine(GameFiles.Folder!, stem + extension);

				if (File.Exists(original))
				{
					File.Copy(original, Path.Combine(work, stem + extension));
				}
			}

			return work;
		}

		private static GameEngine LoadedEngine(string work, string name)
		{
			GameEngine engine = new GameEngine(new SilentHost());
			engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
			engine.PrepareMachine();
			engine.Processor.StandardFolder = work + Path.DirectorySeparatorChar;
			engine.VideoCard.ReserveScreen(2);

			Assert.True(engine.SaveLoad.LoadGameContent(name), $"{name} did not load");

			return engine;
		}
	}
}
