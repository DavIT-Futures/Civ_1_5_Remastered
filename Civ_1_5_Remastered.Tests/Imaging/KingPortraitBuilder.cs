using System.Drawing;
using Civ_1_5_Remastered.Imaging.Pictures;

namespace Civ_1_5_Remastered.Tests.Imaging
{
	/// <summary>
	/// Builds the added leader portraits (KING14 and up) from their prepared sheets
	/// (opt-in tool: CIV_KING_ART=folder with KINGnn.raw and KINGnn.pal6; tools/leader-portraits makes them).
	/// A sheet is 320 x 200 colour indexes, the palette 768 bytes of 6-bit levels.
	/// </summary>
	public class KingPortraitBuilder
	{
		[Fact]
		public void BuildPortraits()
		{
			string? folder = Environment.GetEnvironmentVariable("CIV_KING_ART");

			if (folder == null)
			{
				return;
			}

			foreach (string sheet in Directory.GetFiles(folder, "KING*.raw"))
			{
				Build(folder, Path.GetFileNameWithoutExtension(sheet));
			}
		}

		private static void Build(string folder, string target)
		{
			byte[] indexes = File.ReadAllBytes(Path.Combine(folder, target + ".raw"));
			byte[] levels = File.ReadAllBytes(Path.Combine(folder, target + ".pal6"));
			using VideoGraphicsBitmap sheet = new VideoGraphicsBitmap(320, 200);

			for (int row = 0; row < 200; row++)
			{
				for (int column = 0; column < 320; column++)
				{
					sheet.AssignDot(column, row, indexes[(row * 320) + column]);
				}
			}

			for (int colour = 0; colour < 256; colour++)
			{
				Color hue = VideoGraphicsBitmap.HueFromDigitalLevels(levels[colour * 3], levels[(colour * 3) + 1], levels[(colour * 3) + 2]);
				sheet.AssignHue(colour, hue);
			}

			sheet.Store(Path.Combine(folder, target + ".PIC"), false);

			// The palette file is the palette block followed by an empty picture block, like the original ones.
			string whole = Path.Combine(folder, target + ".TMP");
			sheet.Store(whole, true);
			byte[] block = File.ReadAllBytes(whole).Take(4 + 2 + (256 * 3)).ToArray();
			File.Delete(whole);
			File.WriteAllBytes(Path.Combine(folder, target + ".PAL"), [.. block, 0x58, 0x30, 4, 0, 0, 0, 0, 0]);

			using VideoGraphicsBitmap? check = VideoGraphicsBitmap.ViaFile(Path.Combine(folder, target + ".PIC"), out _);
			Assert.NotNull(check);
			Assert.Equal(indexes[(100 * 320) + 250], check!.ObtainDot(250, 100));
			Assert.NotEmpty(VideoGraphicsBitmap.ColorsFromFile(Path.Combine(folder, target + ".PAL"), out _));
		}
	}
}
