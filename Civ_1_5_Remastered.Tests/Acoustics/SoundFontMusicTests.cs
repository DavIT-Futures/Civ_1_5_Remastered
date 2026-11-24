using System.IO;
using System.Linq;
using Civ_1_5_Remastered.Acoustics.Drivers;
using Civ_1_5_Remastered.Acoustics.Output;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Acoustics
{
	public class SoundFontMusicTests
	{
		private static readonly string AcousticsFolder =
			Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Civ_1_5_Remastered.Acoustics");

		[Theory]
		[InlineData("soundfont")]
		[InlineData(" SoundFont ")]
		[InlineData("sf")]
		[InlineData("generaluser")]
		public void SoundFontOptionUsesTheBankWithAdLibEffects(string option)
		{
			Assert.True(SoundDriverHost.PlaysSoundFont(option));
			Assert.False(SoundDriverHost.PlaysMidiFiles(option));
			Assert.Equal(new[] { "ASOUND2.CVL", "ASOUND3.CVL", "ASOUND.CVL" }, SoundDriverHost.DriverFileNames(option));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("midifiles")]
		[InlineData("adlib")]
		[InlineData("roland")]
		public void OtherOptionsDoNotUseTheSoundFont(string? option)
		{
			Assert.False(SoundDriverHost.PlaysSoundFont(option));
		}

		[Fact]
		public void BankAndItsLicenseShipWithTheGame()
		{
			string bank = Path.Combine(AcousticsFolder, "Assets", "SoundFonts", SoundFontMusic.BankFileName);

			Assert.True(File.Exists(bank), "GeneralUser GS SoundFont must be in Assets/SoundFonts");
			Assert.True(File.Exists(Path.Combine(AcousticsFolder, "Assets", "SoundFonts", "GeneralUser-GS-LICENSE.txt")));

			byte[] heading = new byte[12];
			using (FileStream stream = File.OpenRead(bank))
			{
				Assert.Equal(heading.Length, stream.Read(heading, 0, heading.Length));
			}

			Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(heading, 0, 4));
			Assert.Equal("sfbk", System.Text.Encoding.ASCII.GetString(heading, 8, 4));
		}

		[Fact]
		public void OpeningSongPlaysInStereoAndStops()
		{
			string bank = Path.Combine(AcousticsFolder, "Assets", "SoundFonts", SoundFontMusic.BankFileName);
			string song = Path.Combine(AcousticsFolder, "Assets", "Midi", "opening_raw.mid");
			if (!File.Exists(bank) || !File.Exists(song))
			{
				return;
			}

			SoundFontMusic music = new SoundFontMusic(bank, 44100);
			short[] effects = new short[4410];
			short[] stereo = new short[effects.Length * 2];

			music.Play(song);
			Assert.True(music.IsPlaying);

			bool heard = false;
			for (int block = 0; block < 60 && !heard; block++)
			{
				music.Blend(effects, stereo, effects.Length);
				heard = stereo.Any(sample => sample != 0);
			}

			Assert.True(heard, "The opening song must make sound through the SoundFont");

			music.Halt();
			Assert.False(music.IsPlaying);

			music.Blend(effects, stereo, effects.Length);
			Assert.All(stereo, sample => Assert.Equal(0, sample));
		}

		[Fact]
		public void EffectsPassThroughWhileNoSongPlays()
		{
			string bank = Path.Combine(AcousticsFolder, "Assets", "SoundFonts", SoundFontMusic.BankFileName);
			if (!File.Exists(bank))
			{
				return;
			}

			SoundFontMusic music = new SoundFontMusic(bank, 44100);
			short[] effects = { 1000, -2000, short.MaxValue };
			short[] stereo = new short[effects.Length * 2];

			music.Blend(effects, stereo, effects.Length);

			Assert.Equal(new short[] { 1000, 1000, -2000, -2000, short.MaxValue, short.MaxValue }, stereo);
		}

		[Fact]
		public void AudioOutputInSoundFontModePlaysEverySoundSafely()
		{
			AudioOutput output = new AudioOutput(() => ".") { DriverPreference = "soundfont" };

			ushort[] sounds = { 3, 4, 5, 18, 19, 32, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 1, 0 };
			foreach (ushort sound in sounds)
			{
				output.PerformSound(sound, 0);
				output.ReadSongPosition();
			}

			output.PerformSound(100, 3);
			output.PerformSound(101, 3);
			output.PerformChopping();
			output.Halt();
		}
	}
}
