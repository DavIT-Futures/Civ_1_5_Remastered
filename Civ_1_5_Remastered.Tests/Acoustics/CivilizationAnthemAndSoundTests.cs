using Civ_1_5_Remastered.Acoustics.Output;
using Civ_1_5_Remastered.Core.Shared.Tables;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Acoustics
{
	public class CivilizationAnthemAndSoundTests
	{
		[Fact]
		public void PolishAnthemIdentifierIsOneHundredAndMapsToPolandRawMidi()
		{
			Assert.Equal("poland_raw.mid", GeneralMidiPlayer.SongFileName(100));
		}

		[Fact]
		public void JapaneseAnthemIdentifierIsOneHundredAndOneAndMapsToJapanRawMidi()
		{
			Assert.Equal("japan_raw.mid", GeneralMidiPlayer.SongFileName(101));
		}

		[Fact]
		public void WarSoundIsDistinctFromCustomAnthemsAndNotMidiSong()
		{
			const ushort warSoundCode = 0x21; // 33 decimal
			Assert.NotEqual(100, warSoundCode);
			Assert.NotEqual(101, warSoundCode);
			Assert.Null(GeneralMidiPlayer.SongFileName(warSoundCode));
		}

		[Fact]
		public void LeaderWordReturnsIdentifierOneHundredForPolandAnthemAndShortTune()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal(100, tables.LeaderWord(16, LeaderTable.Tune));
			Assert.Equal(100, tables.LeaderWord(16, LeaderTable.ShortTune));
			Assert.Equal(100, tables.LeaderWordAt(16, LeaderTable.Tune));
			Assert.Equal(100, tables.LeaderWordAt(16, LeaderTable.ShortTune));
			Assert.Equal(100, tables.LeaderWordAt(16 * LeaderTable.Stride, LeaderTable.Tune));
			Assert.Equal(100, tables.LeaderWordAt(16 * LeaderTable.Stride, LeaderTable.ShortTune));
		}

		[Fact]
		public void LeaderWordReturnsIdentifierOneHundredAndOneForJapaneseAnthemAndShortTune()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal(101, tables.LeaderWord(17, LeaderTable.Tune));
			Assert.Equal(101, tables.LeaderWord(17, LeaderTable.ShortTune));
			Assert.Equal(101, tables.LeaderWordAt(17, LeaderTable.Tune));
			Assert.Equal(101, tables.LeaderWordAt(17, LeaderTable.ShortTune));
			Assert.Equal(101, tables.LeaderWordAt(17 * LeaderTable.Stride, LeaderTable.Tune));
			Assert.Equal(101, tables.LeaderWordAt(17 * LeaderTable.Stride, LeaderTable.ShortTune));
		}

		[Fact]
		public void CoreRawMidiSongFileNamesAreMappedCorrectly()
		{
			Assert.Equal("poland_raw.mid", GeneralMidiPlayer.SongFileName(100));
			Assert.Equal("japan_raw.mid", GeneralMidiPlayer.SongFileName(101));
			Assert.Equal("indian_raw.mid", GeneralMidiPlayer.SongFileName(17));
			Assert.Equal("german_raw.mid", GeneralMidiPlayer.SongFileName(18));
			Assert.Equal("victory_raw.mid", GeneralMidiPlayer.SongFileName(34));
			Assert.Equal("defeat_raw.mid", GeneralMidiPlayer.SongFileName(35));
			Assert.Null(GeneralMidiPlayer.SongFileName(33)); // 0x21 war sound
			Assert.Null(GeneralMidiPlayer.SongFileName(0));
			Assert.Null(GeneralMidiPlayer.SongFileName(1));
		}

		[Fact]
		public void AudioOutputHandlesPolishAndJapaneseAnthemsSafely()
		{
			AudioOutput output = new AudioOutput(() => ".");

			// Calling PerformSound with Polish anthem (100) should execute without throwing
			output.PerformSound(100, 3);

			// Calling PerformSound with Japanese anthem (101) should execute without throwing
			output.PerformSound(101, 3);

			// Calling PerformSound with war sound (0x21) should execute without throwing
			output.PerformSound(0x21, 0);

			// Calling PerformSound with stop (1) should execute cleanly
			output.PerformSound(1, 0);

			// Calling PerformSound with silence (0) should execute cleanly
			output.PerformSound(0, 0);
		}

		[Fact]
		public void AudioOutputInGeneralMidiMusicModePlaysEverySoundSafely()
		{
			AudioOutput output = new AudioOutput(() => ".") { DriverPreference = "midifiles" };

			// Every sound the game asks for: the songs (3, 4, the anthems, 0x21 to 0x23) and the effects (0x24 to 0x2c).
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
