using Civilization1Remaster.Core;
using Civilization1Remaster.Imaging;

namespace Civilization1Remaster.Tests.Core
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

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP" })
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
			Assert.Equal(Pixels(Path.Combine(work, stem + ".MAP")), Pixels(Path.Combine(work, "ROUNDTRP.MAP")));
		}

		private static byte[] Pixels(string route)
		{
			VGABitmap picture = VGABitmap.ViaFile(route, out _)!;
			byte[] pixels = new byte[picture.Extent.Width * picture.Extent.Height];

			for (int y = 0; y < picture.Extent.Height; y++)
			{
				for (int x = 0; x < picture.Extent.Width; x++)
				{
					pixels[(y * picture.Extent.Width) + x] = picture.ObtainDot(x, y);
				}
			}

			return pixels;
		}
	}
}
