using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.Tests.Core.Game;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.UI
{
	/// <summary>
	/// The Civilopedia pages of the remaster's extra unit kinds and improvements: with the history option on, the history
	/// fills a first screen and a click leads to a second one with the short note and the strengths (or price and upkeep),
	/// as for the original entries.
	/// </summary>
	public class ExtraCivilopediaPageTests
	{
		private const short HistoryOption = 0x40;

		private const ushort Units = 2;
		private const ushort Improvements = 1;

		[Theory]
		[InlineData(UnitKinds.Pikemen, Units)]
		[InlineData(BuildingKinds.Stockade, Improvements)]
		public async Task TheHistoryComesFirstThenThePageWithTheStrengths(ushort entry, ushort part)
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
				engine.World.SessionPreferenceMarkers |= HistoryOption;
				List<byte[]> pages = new List<byte[]>();

				game.Feeding = false;
				game.StepKey = 0x0d;
				game.Waiting = step => pages.Add((byte[])engine.VideoCard.Displays[0].PictureStorage.Clone());

				lock (engine.VideoCard.Tokens)
				{
					engine.VideoCard.Tokens.Clear();
				}

				engine.Encyclopedia.ShowCivilopediaRecord(entry, part);
				game.Waiting = null;

				Assert.True(pages.Count >= 2, $"{pages.Count} screens instead of the history and the strengths");

				int breadth = engine.VideoCard.Displays[0].Extent.Width;
				byte[] history = pages[0];
				byte[] strengths = pages[1];

				// The second screen differs from the first below the picture, where the note and the strengths are, and
				// both leave the band above the frame empty: nothing runs off the bottom.
				Assert.False(RowsAlike(history, strengths, breadth, 80, 170), "the second screen looks like the first");
				Assert.True(SameColour(history, breadth, 172, 186), "the history runs to the bottom of the screen");
				Assert.True(SameColour(strengths, breadth, 172, 186), "the strengths run to the bottom of the screen");
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}

		private static bool RowsAlike(byte[] first, byte[] second, int breadth, int top, int bottom)
		{
			for (int row = top; row <= bottom; row++)
			{
				for (int column = 0; column < 320; column++)
				{
					if (first[(row * breadth) + column] != second[(row * breadth) + column])
					{
						return false;
					}
				}
			}

			return true;
		}

		private static bool SameColour(byte[] screen, int breadth, int top, int bottom)
		{
			byte colour = screen[(top * breadth) + 10];

			for (int row = top; row <= bottom; row++)
			{
				for (int column = 10; column < 310; column++)
				{
					if (screen[(row * breadth) + column] != colour)
					{
						return false;
					}
				}
			}

			return true;
		}
	}
}
