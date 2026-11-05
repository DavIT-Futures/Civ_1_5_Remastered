using System;
using System.IO;
using Civ_1_5_Remastered.Acoustics.Drivers;
using Civ_1_5_Remastered.Acoustics.Output;
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
		public void FastProcessorAndCircuitFixOptionsPickUpdatedDrivers()
		{
			string[] expected = new[] { "ASOUND2.CVL", "ASOUND3.CVL", "ASOUND.CVL" };
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("asound2"));
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("asound3"));
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("adlib2"));
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("adlib3"));
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("fast486"));
			Assert.Equal(expected, SoundDriverHost.DriverFileNames("ym262"));
		}

		[Fact]
		public void NoSoundOptionYieldsNoLegacyDriver()
		{
			Assert.Empty(SoundDriverHost.DriverFileNames("none"));
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

		[Fact]
		public void FastProcessorDriverProducesAudioWaveform()
		{
			string repositoryFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..");
			string driverPath = Path.Combine(repositoryFolder, "Civ_1_5_Remastered.Acoustics", "Assets", "Drivers", "ASOUND2.CVL");
			if (!File.Exists(driverPath))
			{
				return;
			}

			byte[] document = File.ReadAllBytes(driverPath);
			SoundDriverHost host = new SoundDriverHost(document, 44100, "ASOUND2.CVL");

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

		[Fact]
		public void SongFileNameResolvesAllExpectedTracks()
		{
			Assert.Equal("opening_raw.mid", GeneralMidiPlayer.SongFileName(3));
			Assert.Equal("evolution_raw.mid", GeneralMidiPlayer.SongFileName(4));
			Assert.Equal("americans_raw.mid", GeneralMidiPlayer.SongFileName(5));
			Assert.Equal("aztecs_raw.mid", GeneralMidiPlayer.SongFileName(6));
			Assert.Equal("egyptians_raw.mid", GeneralMidiPlayer.SongFileName(7));
			Assert.Equal("zulus_raw.mid", GeneralMidiPlayer.SongFileName(8));
			Assert.Equal("french_raw.mid", GeneralMidiPlayer.SongFileName(9));
			Assert.Equal("romans_raw.mid", GeneralMidiPlayer.SongFileName(10));
			Assert.Equal("russians_raw.mid", GeneralMidiPlayer.SongFileName(11));
			Assert.Equal("greeks_raw.mid", GeneralMidiPlayer.SongFileName(12));
			Assert.Equal("english_raw.mid", GeneralMidiPlayer.SongFileName(13));
			Assert.Equal("babylonians_raw.mid", GeneralMidiPlayer.SongFileName(14));
			Assert.Equal("chinese_raw.mid", GeneralMidiPlayer.SongFileName(15));
			Assert.Equal("mongols_raw.mid", GeneralMidiPlayer.SongFileName(16));
			Assert.Equal("indian_raw.mid", GeneralMidiPlayer.SongFileName(17));
			Assert.Equal("german_raw.mid", GeneralMidiPlayer.SongFileName(18));
			Assert.Equal("victory_raw.mid", GeneralMidiPlayer.SongFileName(34));
			Assert.Equal("defeat_raw.mid", GeneralMidiPlayer.SongFileName(35));
			Assert.Null(GeneralMidiPlayer.SongFileName(33)); // 33 (0x21) is original engine war sound
			Assert.Equal("poland_raw.mid", GeneralMidiPlayer.SongFileName(100));
			Assert.Equal("japan_raw.mid", GeneralMidiPlayer.SongFileName(101));
			Assert.Null(GeneralMidiPlayer.SongFileName(0));
			Assert.Null(GeneralMidiPlayer.SongFileName(1));
		}

		[Fact]
		public void AllRawMidiAssetsArePresentAndValid()
		{
			string repositoryFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..");
			string midiFolder = Path.Combine(repositoryFolder, "Civ_1_5_Remastered.Acoustics", "Assets", "Midi");

			Assert.True(Directory.Exists(midiFolder), "Midi assets directory must exist");
			string[] files = Directory.GetFiles(midiFolder, "*.mid");
			Assert.True(files.Length >= 18);

			foreach (string filePath in files)
			{
				byte[] header = new byte[4];
				using FileStream stream = File.OpenRead(filePath);
				int readCount = stream.Read(header, 0, 4);
				Assert.Equal(4, readCount);
				Assert.Equal((byte)'M', header[0]);
				Assert.Equal((byte)'T', header[1]);
				Assert.Equal((byte)'h', header[2]);
				Assert.Equal((byte)'d', header[3]);
			}
		}
	}
}
