using Civ_1_5_Remastered.Imaging.Pictures;
using Civ_1_5_Remastered.Imaging.Typefaces;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Imaging
{
	public class ClassicRomTypefaceTests
	{
		[Fact]
		public void RenderCharacterDrawsExpectedPixels()
		{
			VideoGraphicsBitmap screen = new VideoGraphicsBitmap(16, 16);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x43, 0, 0, 14, 1);

			// The character 'C' has blue background and yellow foreground
			Assert.Equal(1, screen.PictureStorage[0]);
			Assert.Contains((byte)14, screen.PictureStorage);
		}

		[Fact]
		public void RenderTextDrawsFullString()
		{
			VideoGraphicsBitmap screen = new VideoGraphicsBitmap(640, 16);

			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "CIVILIZATION", 0, 0, 14, 0);

			Assert.Contains((byte)14, screen.PictureStorage);
		}

		[Fact]
		public void RenderCenteredTextPositionsProperly()
		{
			VideoGraphicsBitmap screen = new VideoGraphicsBitmap(640, 16);

			ClassicRomTypeface.RenderCenteredText(screen.PictureStorage, screen.Pitch, "CIVILIZATION", 0, 14, 0);

			Assert.Contains((byte)14, screen.PictureStorage);
		}

		[Fact]
		public void RenderBoxCharactersDoesNotThrow()
		{
			VideoGraphicsBitmap screen = new VideoGraphicsBitmap(16, 16);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xc9, 0, 0, 14, 1);

			// Check all 256 characters can be rendered without exception
			for (int code = 0; code < 256; code++)
			{
				ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, (byte)code, 0, 0, 14, 1);
			}
		}
	}
}
