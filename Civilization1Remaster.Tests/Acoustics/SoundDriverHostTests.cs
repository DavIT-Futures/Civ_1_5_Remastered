using Civilization1Remaster.Acoustics.Drivers;

namespace Civilization1Remaster.Tests.Acoustics
{
	public class SoundDriverHostTests
	{
		[Theory]
		[InlineData("speaker", "ISOUND.CVL")]
		[InlineData("adlib", "ASOUND.CVL")]
		[InlineData(" Roland ", "RSOUND.CVL")]
		[InlineData("midi", "RSOUND.CVL")]
		public void EachOptionPicksItsDriver(string option, string expected)
		{
			Assert.Equal(new[] { expected }, SoundDriverHost.DriverFileNames(option));
		}

		[Fact]
		public void NoOptionTriesEveryDriverAndNoneTriesNothing()
		{
			Assert.Equal(new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" }, SoundDriverHost.DriverFileNames(null));
			Assert.Empty(SoundDriverHost.DriverFileNames("none"));
		}
	}
}
