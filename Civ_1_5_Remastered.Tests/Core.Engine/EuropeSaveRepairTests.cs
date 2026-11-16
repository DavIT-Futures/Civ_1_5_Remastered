using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Imaging.Pictures;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Engine
{
	/// <summary>
	/// A game saved on the first map of Europe is repaired when it is loaded: the river tiles added to link the
	/// diagonal rivers appear, and the ice of the south pole over the Sahara turns back into its land.
	/// </summary>
	public class EuropeSaveRepairTests
	{
		private const byte River = 9;
		private const byte Ocean = 1;
		private const byte Tundra = 7;
		private const byte Arctic = 15;

		[Fact]
		public async Task EuropeSavesHaveEveryRiverAndNoSouthPoleOnceLoaded()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			using VideoGraphicsBitmap europe = VideoGraphicsBitmap.ViaFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Pictures", "EUROPE.PIC"), out _)!;

			foreach (string save in GameFiles.Find("*.SVE"))
			{
				await Task.Run(() => CheckSave(Path.GetFileNameWithoutExtension(save), europe)).WaitAsync(TimeSpan.FromMinutes(1));
			}
		}

		private static void CheckSave(string stem, VideoGraphicsBitmap europe)
		{
			string work = Path.Combine(Path.GetDirectoryName(GameFiles.ScratchFile("repair"))!, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(work);

			foreach (string extension in new[] { ".SVE", ".MAP", ".HP", ".EXT" })
			{
				string original = Path.Combine(GameFiles.Folder!, stem + extension);

				if (File.Exists(original))
				{
					File.Copy(original, Path.Combine(work, stem + extension));
				}
			}

			GameEngine engine = new GameEngine(new SilentGameHost());
			engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
			engine.PrepareMachine();
			engine.Processor.StandardFolder = work + Path.DirectorySeparatorChar;
			engine.VideoCard.ReserveScreen(2);

			if (!engine.SaveLoad.LoadGameContent(stem + ".SVE") || !EuropeMap.Played(engine.World.Extras.World))
			{
				return;
			}

			for (int row = 0; row < 50; row++)
			{
				for (int column = 0; column < 80; column++)
				{
					byte drawn = europe.ObtainDot(column, row);
					byte played = (byte)engine.VideoCard.ObtainDot(2, column, row);

					if (drawn == River && played != Ocean)
					{
						Assert.True(played == River, $"{stem}: the river at {column},{row} is missing");
					}

					if (row >= 48 && drawn != Ocean && drawn != played)
					{
						Assert.False(played == Arctic || played == Tundra, $"{stem}: ice of the south pole at {column},{row}");
					}
				}
			}
		}
	}
}
