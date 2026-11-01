using System.Drawing;
using Civilization1Remaster.Foundation.Squeeze;
using Civilization1Remaster.Imaging.Pictures;
using Civilization1Remaster.Tests.Foundation;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class VideoGraphicsBitmapTests
	{
		private const uint OriginalPicturesDigest = 2409423137u;

		[Theory]
		[InlineData(320, 200)]
		[InlineData(17, 5)]
		[InlineData(1, 1)]
		public void StoreWithoutPaletteThenLoadGivesSamePixels(int width, int height)
		{
			VideoGraphicsBitmap original = Painted(width, height);
			string route = GameFiles.ScratchFile("picture.pic");
			original.Store(route, false);

			VideoGraphicsBitmap? copy = VideoGraphicsBitmap.ViaFile(route, out byte[] swatch);

			Assert.NotNull(copy);
			Assert.Empty(swatch);
			Assert.Equal(new Size(width, height), copy!.Extent);
			Assert.Equal(Pixels(original), Pixels(copy));
			File.Delete(route);
		}

		[Fact]
		public void StoreWithPaletteThenLoadGivesSamePixelsAndPalette()
		{
			VideoGraphicsBitmap original = Painted(32, 8);
			original.AssignHue(5, VideoGraphicsBitmap.HueFromDigitalLevels(10, 20, 30));
			string route = GameFiles.ScratchFile("palette-store.pic");
			original.Store(route, true);

			VideoGraphicsBitmap? copy = VideoGraphicsBitmap.ViaFile(route, out _);

			Assert.NotNull(copy);
			Assert.Equal(Pixels(original), Pixels(copy!));
			Assert.Equal(original.Palette[5], copy!.Palette[5]);
		}

		[Fact]
		public void EveryConverterLevelSurvivesTheConversionToColourAndBack()
		{
			for (int level = 0; level < 64; level++)
			{
				Color hue = VideoGraphicsBitmap.HueFromDigitalLevels(level, 63 - level, level / 2);
				Color back = VideoGraphicsBitmap.HueToDigitalLevels(hue);

				Assert.Equal((level, 63 - level, level / 2), (back.R, back.G, back.B));
			}
		}

		[Fact]
		public void PitchIsRoundedUpToFour()
		{
			Assert.Equal(20, new VideoGraphicsBitmap(17, 5).Pitch);
			Assert.Equal(20 * 5, new VideoGraphicsBitmap(17, 5).PictureStorage.Length);
		}

		[Fact]
		public void LoadReadsPaletteBlock()
		{
			string route = GameFiles.ScratchFile("palette.pic");
			MemoryStream content = new MemoryStream();
			content.Write(new byte[] { 0x4d, 0x30, 8, 0, 1, 2, 63, 0, 0, 0, 63, 0 });
			AppendImageBlock(content, 4, 2, new byte[] { 1, 2, 2, 1, 0, 0, 1, 2 });
			File.WriteAllBytes(route, content.ToArray());

			VideoGraphicsBitmap? picture = VideoGraphicsBitmap.ViaFile(route, out byte[] swatch);

			Assert.NotNull(picture);
			Assert.Equal(12, swatch.Length);
			Assert.Equal(VideoGraphicsBitmap.HueFromDigitalLevels(63, 0, 0), picture!.Palette[1]);
			Assert.Equal(VideoGraphicsBitmap.HueFromDigitalLevels(0, 63, 0), picture.Palette[2]);
			Assert.Equal(new byte[] { 1, 2, 2, 1, 0, 0, 1, 2 }, Pixels(picture));
			File.Delete(route);
		}

		[Fact]
		public void TrailingOddByteEndsTheFile()
		{
			string route = GameFiles.ScratchFile("odd.pic");
			MemoryStream content = new MemoryStream();
			AppendImageBlock(content, 2, 1, new byte[] { 7, 8 });
			content.WriteByte(0x58);
			File.WriteAllBytes(route, content.ToArray());

			VideoGraphicsBitmap? picture = VideoGraphicsBitmap.ViaFile(route, out _);

			Assert.Equal(new byte[] { 7, 8 }, Pixels(picture!));
			File.Delete(route);
		}

		[Fact]
		public void TruncatedBlockThrows()
		{
			string route = GameFiles.ScratchFile("cut.pic");
			File.WriteAllBytes(route, new byte[] { 0x58, 0x30, 50, 0, 1, 2 });

			Exception problem = Assert.Throws<Exception>(() => VideoGraphicsBitmap.ViaFile(route, out _));
			Assert.StartsWith("Block type 0x3058 data missing", problem.Message);
			File.Delete(route);
		}

		[Fact]
		public void UnknownBlockThrows()
		{
			string route = GameFiles.ScratchFile("unknown.pic");
			File.WriteAllBytes(route, new byte[] { 0x11, 0x22, 0, 0 });

			Exception problem = Assert.Throws<Exception>(() => VideoGraphicsBitmap.ViaFile(route, out _));
			Assert.Equal("Undefined block type 0x2211", problem.Message);
			File.Delete(route);
		}

		[Fact]
		public void OriginalPicturesAndPalettesDecodeTheSame()
		{
			string[] pictures = GameFiles.Find("*.PIC");
			string[] palettes = GameFiles.Find("*.PAL");

			if (pictures.Length == 0)
			{
				return;
			}

			List<byte> decoded = new List<byte>();

			foreach (string route in pictures)
			{
				VideoGraphicsBitmap? picture = VideoGraphicsBitmap.ViaFile(route, out byte[] swatch);
				Assert.NotNull(picture);
				decoded.AddRange(BitConverter.GetBytes(picture!.Extent.Width));
				decoded.AddRange(BitConverter.GetBytes(picture.Extent.Height));
				decoded.AddRange(Pixels(picture));
				decoded.AddRange(swatch);

				foreach (Color hue in picture.Palette)
				{
					decoded.AddRange(BitConverter.GetBytes(hue.ToArgb()));
				}
			}

			foreach (string route in palettes)
			{
				foreach (var couple in VideoGraphicsBitmap.ColorsFromFile(route, out byte[] swatch))
				{
					decoded.AddRange(BitConverter.GetBytes(couple.Key));
					decoded.AddRange(BitConverter.GetBytes(couple.Value.ToArgb()));
				}
			}

			Assert.Equal(OriginalPicturesDigest, CyclicRedundancyCheck.ObtainChecksum(decoded.ToArray()));
		}

		private static VideoGraphicsBitmap Painted(int width, int height)
		{
			VideoGraphicsBitmap picture = new VideoGraphicsBitmap(width, height);
			byte[] pattern = CompressionTests.ScreenLike(width, height);

			for (int vertical = 0; vertical < height; vertical++)
			{
				for (int horizontal = 0; horizontal < width; horizontal++)
				{
					picture.AssignDot(horizontal, vertical, pattern[(vertical * width) + horizontal]);
				}
			}

			return picture;
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

		private static void AppendImageBlock(MemoryStream content, int width, int height, byte[] pixels)
		{
			byte[] packed = CompressionTests.DictionaryCompressionPack(CompressionTests.RunLengthPack(pixels, 4), 9, 11);
			int length = packed.Length + 4;

			content.Write(new byte[] { 0x58, 0x30, (byte)length, (byte)(length >> 8), (byte)width, (byte)(width >> 8), (byte)height, (byte)(height >> 8) });
			content.Write(packed);
		}
	}
}
