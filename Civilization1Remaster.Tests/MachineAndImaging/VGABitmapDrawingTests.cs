using System.Drawing;
using Civilization1Remaster.Foundation.Squeeze;
using Civilization1Remaster.Imaging;
using Civilization1Remaster.Imaging.Typefaces;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class VGABitmapDrawingTests
	{
		private const uint SceneDigest = 1619667550u;

		private static readonly Dictionary<int, CivFont> Fonts = LoadFonts();

		private static Dictionary<int, CivFont> LoadFonts()
		{
			return CivFonts.Unpack(GameFiles.FontsXml());
		}

		private static byte[] Pixels(VGABitmap picture)
		{
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

		[Theory]
		[InlineData(PixelWriteMode.Normal, 0x0c)]
		[InlineData(PixelWriteMode.And, 0x0a & 0x0c)]
		[InlineData(PixelWriteMode.Or, 0x0a | 0x0c)]
		[InlineData(PixelWriteMode.Xor, 0x0a ^ 0x0c)]
		public void DotWriteModesCombineWithTheOldColour(PixelWriteMode style, int expected)
		{
			VGABitmap picture = new VGABitmap(4, 4);
			picture.AssignDot(1, 1, 0x0a);
			picture.AssignDot(1, 1, 0x0c, style);

			Assert.Equal(expected, picture.ObtainDot(1, 1));
		}

		[Fact]
		public void DotsOutsideThePictureAreIgnored()
		{
			VGABitmap picture = new VGABitmap(4, 4);
			picture.AssignDot(-1, 0, 5);
			picture.AssignDot(4, 0, 5);
			picture.AssignDot(0, 4, 5, PixelWriteMode.Xor);

			Assert.All(Pixels(picture), dot => Assert.Equal(0, dot));
		}

		[Fact]
		public void FloodBoxFillsExactlyTheBoxClippedToThePicture()
		{
			VGABitmap picture = new VGABitmap(10, 8);
			picture.FloodBox(new Rectangle(7, 5, 6, 6), 3, PixelWriteMode.Normal);

			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 10; x++)
				{
					Assert.Equal(x >= 7 && y >= 5 ? 3 : 0, picture.ObtainDot(x, y));
				}
			}
		}

		[Fact]
		public void HorizontalAndVerticalLinesIncludeBothEnds()
		{
			VGABitmap picture = new VGABitmap(10, 10);
			picture.RenderLine(2, 3, 7, 3, 1, PixelWriteMode.Normal);
			picture.RenderLine(5, 1, 5, 8, 2, PixelWriteMode.Normal);

			Assert.Equal(new byte[] { 0, 0, 1, 1, 1, 2, 1, 1, 0, 0 }, Enumerable.Range(0, 10).Select(x => picture.ObtainDot(x, 3)).ToArray());
			Assert.Equal(new byte[] { 0, 2, 2, 2, 2, 2, 2, 2, 2, 0 }, Enumerable.Range(0, 10).Select(y => picture.ObtainDot(5, y)).ToArray());
		}

		[Fact]
		public void SubstituteHueSwapsOnlyInsideTheBox()
		{
			VGABitmap picture = new VGABitmap(6, 1);
			picture.FloodBox(new Rectangle(0, 0, 6, 1), 4, PixelWriteMode.Normal);
			picture.AssignDot(3, 0, 7);
			picture.SubstituteHue(new Rectangle(2, 0, 3, 1), 4, 9);

			Assert.Equal(new byte[] { 4, 4, 9, 7, 9, 4 }, Pixels(picture));
		}

		[Fact]
		public void RenderImageCopiesAndClips()
		{
			VGABitmap source = new VGABitmap(3, 3);
			source.FloodBox(new Rectangle(0, 0, 3, 3), 6, PixelWriteMode.Normal);
			source.AssignDot(1, 1, 0);
			VGABitmap target = new VGABitmap(4, 4);
			target.FloodBox(new Rectangle(0, 0, 4, 4), 2, PixelWriteMode.Normal);

			target.RenderImage(2, 2, source, false);

			Assert.Equal(new byte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 6, 6, 2, 2, 6, 0 }, Pixels(target));
		}

		[Fact]
		public void TextUsesTheFontShapes()
		{
			VGABitmap picture = new VGABitmap(40, 12);
			CivFont font = Fonts[1];
			picture.RenderText("A", font, new Rectangle(0, 0, 40, 12), 15, 0, PixelWriteMode.Normal);
			CivFontCharacter shape = font.Letters['A'];

			for (int y = 0; y < shape.Altitude; y++)
			{
				for (int x = 0; x < shape.Breadth; x++)
				{
					Assert.Equal(shape.Picture[y][x] != 0, picture.ObtainDot(x, y) == 15);
				}
			}
		}

		[Fact]
		public void SceneOfAllPrimitivesIsUnchanged()
		{
			VGABitmap picture = new VGABitmap(320, 200);
			Random random = new Random(1991);
			PixelWriteMode[] styles = Enum.GetValues<PixelWriteMode>();

			for (int idx = 0; idx < 400; idx++)
			{
				PixelWriteMode style = styles[idx % styles.Length];
				byte hue = (byte)random.Next(256);
				int x1 = random.Next(-20, 340), y1 = random.Next(-20, 220), x2 = random.Next(-20, 340), y2 = random.Next(-20, 220);

				switch (idx % 4)
				{
					case 0: picture.AssignDot(x1, y1, hue, style); break;
					case 1: picture.RenderLine(x1, y1, x2, y2, hue, style); break;
					case 2: picture.FloodBox(new Rectangle(x1, y1, random.Next(40), random.Next(40)), hue, style); break;
					default: picture.SubstituteHue(new Rectangle(x1, y1, random.Next(80), random.Next(80)), hue, (byte)random.Next(256)); break;
				}
			}

			foreach (var (number, font) in Fonts.OrderBy(entry => entry.Key))
			{
				string text = "The Civilization 4000 BC ?!,. abcxyz ABCXYZ";
				picture.RenderText(text, font, new Rectangle(number * 3, number * 20, 320 - (number * 3), 20), (byte)(number + 1), 0,
					number % 2 == 0 ? PixelWriteMode.Normal : PixelWriteMode.Xor);
			}

			Assert.Equal(SceneDigest, CRC32.ObtainCRC32(Pixels(picture)));
		}

		[Fact]
		public void OriginalPicturesSurviveStoreAndLoad()
		{
			foreach (string route in GameFiles.Find("*.PIC"))
			{
				VGABitmap original = VGABitmap.ViaFile(route, out _)!;
				string copy = GameFiles.ScratchFile(Path.GetFileName(route));
				original.Store(copy, false);

				VGABitmap reloaded = VGABitmap.ViaFile(copy, out _)!;

				Assert.Equal(original.Extent, reloaded.Extent);
				Assert.True(Pixels(original).AsSpan().SequenceEqual(Pixels(reloaded)), Path.GetFileName(route));
				File.Delete(copy);
			}
		}
	}
}
