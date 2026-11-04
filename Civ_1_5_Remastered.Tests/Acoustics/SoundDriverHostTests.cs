using System.IO;
using Civ_1_5_Remastered.Acoustics.Drivers;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Acoustics
{
	public class SoundDriverHostTests
	{
		[Theory]
		[InlineData("speaker", "ISOUND.CVL")]
		[InlineData("ibm", "ISOUND.CVL")]
		[InlineData("adlib", "ASOUND.CVL")]
		[InlineData("soundblaster", "ASOUND.CVL")]
		[InlineData(" Roland ", "RSOUND.CVL")]
		[InlineData("midi", "RSOUND.CVL")]
		public void EachOptionPicksItsDriver(string option, string expected)
		{
			Assert.Equal(new[] { expected }, SoundDriverHost.DriverFileNames(option));
		}

		[Fact]
		public void TandyPicksTandyWithSpeakerFallback()
		{
			Assert.Equal(new[] { "TSOUND.CVL", "ISOUND.CVL" }, SoundDriverHost.DriverFileNames("tandy"));
		}

		[Fact]
		public void CustomPicksStandardDriverList()
		{
			Assert.Equal(new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" }, SoundDriverHost.DriverFileNames("custom"));
		}

		[Fact]
		public void NoOptionTriesEveryDriverAndNoneTriesNothing()
		{
			Assert.Equal(new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" }, SoundDriverHost.DriverFileNames(null));
			Assert.Empty(SoundDriverHost.DriverFileNames("none"));
		}

		[Fact]
		public void TandyDriverProducesAudioWaveform()
		{
			string gameDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "CIVILIZATION");
			string path = Path.Combine(gameDirectory, "TSOUND.CVL");
			if (!File.Exists(path))
			{
				return;
			}

			byte[] document = File.ReadAllBytes(path);
			SoundDriverHost host = new SoundDriverHost(document, 44100, "TSOUND.CVL");

			host.Prepare();
			host.Perform(3, 0);

			for (int beat = 0; beat < 200; beat++)
			{
				host.Beat();
			}

			short[] staging = new short[2048];
			host.Produce(staging, 0, staging.Length);

			Assert.Contains(staging, sample => sample != 0);
		}
	}
}
