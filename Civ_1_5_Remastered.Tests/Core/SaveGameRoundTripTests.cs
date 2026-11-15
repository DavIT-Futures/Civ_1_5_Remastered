using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Imaging.Pictures;

namespace Civ_1_5_Remastered.Tests.Core
{
	public partial class SaveGameRoundTripTests
	{
		public static IEnumerable<object[]> SavedGames() =>
			GameFiles.Find("*.SVE").Select(route => new object[] { Path.GetFileName(route) }).DefaultIfEmpty(new object[] { string.Empty });

		[Theory]
		[MemberData(nameof(SavedGames))]
		public async Task SavedGameIsWrittenBackByteForByte(string name)
		{
			if (name.Length == 0)
			{
				return;
			}

			string folder = Path.GetDirectoryName(GameFiles.ScratchFile("x"))!;
			string work = Path.Combine(folder, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(work);
			string stem = Path.GetFileNameWithoutExtension(name);

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP", ".EXT" })
			{
				string original = Path.Combine(GameFiles.Folder!, stem + extension);
				if (File.Exists(original))
				{
					File.Copy(original, Path.Combine(work, stem + extension));
				}
			}

			bool loaded = false, saved = false;
			SilentHost host = new SilentHost();

			Task run = Task.Run(() =>
			{
				GameEngine engine = new GameEngine(host);
				engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
				engine.PrepareMachine();
				engine.Processor.StandardFolder = work + Path.DirectorySeparatorChar;
				engine.VideoCard.ReserveScreen(2);

				loaded = engine.SaveLoad.LoadGameContent(stem + ".SVE");
				saved = loaded && engine.SaveLoad.SaveGameContent("ROUNDTRP.SVE");
			});

			await run.WaitAsync(TimeSpan.FromSeconds(30));
			Assert.True(loaded, $"{name} did not load");
			Assert.True(saved, $"{name} was not saved");

			Assert.Equal(File.ReadAllBytes(Path.Combine(work, stem + ".SVE")), File.ReadAllBytes(Path.Combine(work, "ROUNDTRP.SVE")));

			byte[] before = Pixels(Path.Combine(work, stem + ".MAP"));
			byte[] after = Pixels(Path.Combine(work, "ROUNDTRP.MAP"));

			// A game on the first map of Europe is repaired when it is loaded (SaveGameStore.RepairEuropeMap):
			// a land tile of the terrain plane may come back as river, and the ice of the south pole as other land.
			const byte river = 9;
			const byte ocean = 1;
			const byte tundra = 7;
			const byte arctic = 15;

			for (int row = 0; row < 50; row++)
			{
				for (int column = 0; column < 80; column++)
				{
					int tile = (row * 320) + column;
					bool southPole = row >= 48 && (before[tile] == arctic || before[tile] == tundra);

					if (southPole && after[tile] == ocean)
					{
						// Ice over the sea is sea again, with the number of the sea above it (the plane 50 rows below).
						before[tile + (50 * 320)] = after[tile + (50 * 320)];
					}

					if ((after[tile] == river && before[tile] != ocean) || southPole)
					{
						before[tile] = after[tile];
					}
				}
			}

			Assert.Equal(before, after);
		}

		private static byte[] Pixels(string route)
		{
			VideoGraphicsBitmap picture = VideoGraphicsBitmap.ViaFile(route, out _)!;
			byte[] pixels = new byte[picture.Extent.Width * picture.Extent.Height];

			for (int vertical = 0; vertical < picture.Extent.Height; vertical++)
			{
				for (int horizontal = 0; horizontal < picture.Extent.Width; horizontal++)
				{
					pixels[(vertical * picture.Extent.Width) + horizontal] = picture.ObtainDot(horizontal, vertical);
				}
			}

			return pixels;
		}
	}
}
