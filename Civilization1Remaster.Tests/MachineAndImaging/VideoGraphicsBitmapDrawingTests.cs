using System.Drawing;
using Civilization1Remaster.Foundation.Squeeze;
using Civilization1Remaster.Imaging;
using Civilization1Remaster.Imaging.Typefaces;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class VideoGraphicsBitmapDrawingTests
	{
		private const uint SceneDigest = 1619667550u;

		private static readonly Dictionary<int, CivilizationFont> Fonts = LoadFonts();

		[Theory]
		[InlineData(PixelWriteMode.Normal, 0x0c)]
		[InlineData(PixelWriteMode.And, 0x0a & 0x0c)]
		[InlineData(PixelWriteMode.Or, 0x0a | 0x0c)]
		[InlineData(PixelWriteMode.Xor, 0x0a ^ 0x0c)]
		public void DotWriteModesCombineWithTheOldColour(PixelWriteMode style, int expected)
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(4, 4);
			picture.AssignDot(1, 1, 0x0a);
			picture.AssignDot(1, 1, 0x0c, style);

			Assert.Equal(expected, picture.ObtainDot(1, 1));
		}

		[Fact]
		public void DotsOutsideThePictureAreIgnored()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(4, 4);
			picture.AssignDot(-1, 0, 5);
			picture.AssignDot(4, 0, 5);
			picture.AssignDot(0, 4, 5, PixelWriteMode.Xor);

			Assert.All(Pixels(picture), dot => Assert.Equal(0, dot));
		}

		[Fact]
		public void FloodBoxFillsExactlyTheBoxClippedToThePicture()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(10, 8);
			picture.FloodBox(new Rectangle(7, 5, 6, 6), 3, PixelWriteMode.Normal);

			for (int vertical = 0; vertical < 8; vertical++)
			{
				for (int horizontal = 0; horizontal < 10; horizontal++)
				{
					Assert.Equal(horizontal >= 7 && vertical >= 5 ? 3 : 0, picture.ObtainDot(horizontal, vertical));
				}
			}
		}

		[Fact]
		public void HorizontalAndVerticalLinesIncludeBothEnds()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(10, 10);
			picture.RenderLine(2, 3, 7, 3, 1, PixelWriteMode.Normal);
			picture.RenderLine(5, 1, 5, 8, 2, PixelWriteMode.Normal);

			Assert.Equal(new byte[] { 0, 0, 1, 1, 1, 2, 1, 1, 0, 0 }, Enumerable.Range(0, 10).Select(horizontal => picture.ObtainDot(horizontal, 3)).ToArray());
			Assert.Equal(new byte[] { 0, 2, 2, 2, 2, 2, 2, 2, 2, 0 }, Enumerable.Range(0, 10).Select(vertical => picture.ObtainDot(5, vertical)).ToArray());
		}

		[Fact]
		public void SubstituteHueSwapsOnlyInsideTheBox()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(6, 1);
			picture.FloodBox(new Rectangle(0, 0, 6, 1), 4, PixelWriteMode.Normal);
			picture.AssignDot(3, 0, 7);
			picture.SubstituteHue(new Rectangle(2, 0, 3, 1), 4, 9);

			Assert.Equal(new byte[] { 4, 4, 9, 7, 9, 4 }, Pixels(picture));
		}

		[Fact]
		public void RenderImageCopiesAndClips()
		{
			VideoGraphicsBitmap source = new VideoGraphicsBitmap(3, 3);
			source.FloodBox(new Rectangle(0, 0, 3, 3), 6, PixelWriteMode.Normal);
			source.AssignDot(1, 1, 0);
			VideoGraphicsBitmap target = new VideoGraphicsBitmap(4, 4);
			target.FloodBox(new Rectangle(0, 0, 4, 4), 2, PixelWriteMode.Normal);

			target.RenderImage(2, 2, source, false);

			Assert.Equal(new byte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 6, 6, 2, 2, 6, 0 }, Pixels(target));
		}

		[Fact]
		public void TextUsesTheFontShapes()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(40, 12);
			CivilizationFont font = Fonts[1];
			picture.RenderText("A", font, new Rectangle(0, 0, 40, 12), 15, 0, PixelWriteMode.Normal);
			CivilizationFontCharacter shape = font.Letters['A'];

			for (int vertical = 0; vertical < shape.Altitude; vertical++)
			{
				for (int horizontal = 0; horizontal < shape.Breadth; horizontal++)
				{
					Assert.Equal(shape.Picture[vertical][horizontal] != 0, picture.ObtainDot(horizontal, vertical) == 15);
				}
			}
		}

		[Fact]
		public void SceneOfAllPrimitivesIsUnchanged()
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(320, 200);
			Random random = new Random(1991);
			PixelWriteMode[] styles = Enum.GetValues<PixelWriteMode>();

			for (int index = 0; index < 400; index++)
			{
				PixelWriteMode style = styles[index % styles.Length];
				byte hue = (byte)random.Next(256);
				int firstHorizontal = random.Next(-20, 340), firstVertical = random.Next(-20, 220), secondHorizontal = random.Next(-20, 340), secondVertical = random.Next(-20, 220);

				switch (index % 4)
				{
					case 0: picture.AssignDot(firstHorizontal, firstVertical, hue, style); break;
					case 1: picture.RenderLine(firstHorizontal, firstVertical, secondHorizontal, secondVertical, hue, style); break;
					case 2: picture.FloodBox(new Rectangle(firstHorizontal, firstVertical, random.Next(40), random.Next(40)), hue, style); break;
					default: picture.SubstituteHue(new Rectangle(firstHorizontal, firstVertical, random.Next(80), random.Next(80)), hue, (byte)random.Next(256)); break;
				}
			}

			foreach (var (number, font) in Fonts.OrderBy(entry => entry.Key))
			{
				string text = "The Civilization 4000 BC ?!,. abcxyz ABCXYZ";
				picture.RenderText(text, font, new Rectangle(number * 3, number * 20, 320 - (number * 3), 20), (byte)(number + 1), 0,
					number % 2 == 0 ? PixelWriteMode.Normal : PixelWriteMode.Xor);
			}

			Assert.Equal(SceneDigest, CyclicRedundancyCheck.ObtainChecksum(Pixels(picture)));
		}

		[Fact]
		public void OriginalPicturesSurviveStoreAndLoad()
		{
			foreach (string route in GameFiles.Find("*.PIC"))
			{
				VideoGraphicsBitmap original = VideoGraphicsBitmap.ViaFile(route, out _)!;
				string copy = GameFiles.ScratchFile(Path.GetFileName(route));
				original.Store(copy, false);

				VideoGraphicsBitmap reloaded = VideoGraphicsBitmap.ViaFile(copy, out _)!;

				Assert.Equal(original.Extent, reloaded.Extent);
				Assert.True(Pixels(original).AsSpan().SequenceEqual(Pixels(reloaded)), Path.GetFileName(route));
				File.Delete(copy);
			}
		}

		private static Dictionary<int, CivilizationFont> LoadFonts()
		{
			return CivilizationFonts.Unpack(GameFiles.FontsMarkup());
		}

		private static byte[] Pixels(VideoGraphicsBitmap picture)
		{
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
