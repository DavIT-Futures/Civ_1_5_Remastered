using System.Drawing;
using Civilization1Remaster.Core;
using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Imaging;

namespace Civilization1Remaster.Tests.Core
{
	/// <summary>
	/// Loads every saved game of CIVILIZATION/ (copied to a scratch folder) with the real SaveGameStore, saves it again and
	/// compares the files. This covers the whole saved game format: the order and the sizes of all the fields.
	/// </summary>
	public class SaveGameRoundTripTests
	{
		private sealed class SilentHost : IGameHost
		{
			public List<string> Messages { get; } = new List<string>();
			public Point PointerSite => Point.Empty;
			public PointerButtons PointerClicks => PointerButtons.None;
			public void OnDisplayCountChanged() { }
			public void ShowMessage(string message, string caption) => this.Messages.Add($"{caption}: {message}");
			public HostDialogResult ShowRetryCancel(string message, string caption) => HostDialogResult.Cancel;
		}

		public static IEnumerable<object[]> SavedGames() =>
			GameFiles.Find("*.SVE").Select(route => new object[] { Path.GetFileName(route) }).DefaultIfEmpty(new object[] { "" });

		[Theory]
		[MemberData(nameof(SavedGames))]
		public async Task SavedGameIsWrittenBackByteForByte(string name)
		{
			if (name.Length == 0)
				return;

			string folder = Path.GetDirectoryName(GameFiles.ScratchFile("x"))!;
			string work = Path.Combine(folder, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(work);
			string stem = Path.GetFileNameWithoutExtension(name);

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP" })
			{
				string original = Path.Combine(GameFiles.Folder!, stem + extension);
				if (File.Exists(original))
					File.Copy(original, Path.Combine(work, stem + extension));
			}

			bool loaded = false, saved = false;
			SilentHost host = new SilentHost();

			// a failed load would show a message in the emulated game and wait for a key, so the test runs with a time limit
			Task run = Task.Run(() =>
			{
				GameEngine engine = new GameEngine(host);
				// the start-up checks the files of the game in their folder (read only), the saves go to the scratch copy
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
				for (int x = 0; x < picture.Extent.Width; x++)
					pixels[y * picture.Extent.Width + x] = picture.ObtainDot(x, y);

			return pixels;
		}
	}
}
