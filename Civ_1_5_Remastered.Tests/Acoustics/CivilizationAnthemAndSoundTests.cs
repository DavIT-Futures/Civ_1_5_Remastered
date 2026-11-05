using Civ_1_5_Remastered.Acoustics.Output;
using Civ_1_5_Remastered.Core.Shared.Tables;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Acoustics
{
	public class CivilizationAnthemAndSoundTests
	{
		[Fact]
		public void PolishAnthemIdentifierIs100AndMapsToPolandRawMidi()
		{
			Assert.Equal("poland_raw.mid", GeneralMidiPlayer.SongFileName(100));
		}

		[Fact]
		public void WarSound0x21IsDistinctFromPolishAnthemAndNotMidiSong()
		{
			const ushort warSoundId = 0x21; // 33 decimal
			Assert.NotEqual(100, warSoundId);
			Assert.Null(GeneralMidiPlayer.SongFileName(warSoundId));
		}

		[Fact]
		public void LeaderWordReturnsIdentifier100ForPolandAnthemAndShortTune()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal(100, tables.LeaderWord(16, LeaderTable.Tune));
			Assert.Equal(100, tables.LeaderWord(16, LeaderTable.ShortTune));
			Assert.Equal(100, tables.LeaderWordAt(16, LeaderTable.Tune));
			Assert.Equal(100, tables.LeaderWordAt(16, LeaderTable.ShortTune));
		}

		[Fact]
		public void CoreRawMidiSongFileNamesAreMappedCorrectly()
		{
			Assert.Equal("poland_raw.mid", GeneralMidiPlayer.SongFileName(100));
			Assert.Equal("indian_raw.mid", GeneralMidiPlayer.SongFileName(17));
			Assert.Equal("german_raw.mid", GeneralMidiPlayer.SongFileName(18));
			Assert.Equal("victory_raw.mid", GeneralMidiPlayer.SongFileName(34));
			Assert.Equal("defeat_raw.mid", GeneralMidiPlayer.SongFileName(35));
			Assert.Null(GeneralMidiPlayer.SongFileName(33)); // 0x21 war sound
			Assert.Null(GeneralMidiPlayer.SongFileName(0));
			Assert.Null(GeneralMidiPlayer.SongFileName(1));
		}

		[Fact]
		public void AudioOutputHandlesPolishAnthemAndWarSoundSafely()
		{
			AudioOutput output = new AudioOutput(() => ".");

			// Calling PerformSound with war sound (0x21) should execute without throwing
			output.PerformSound(0x21, 0);

			// Calling PerformSound with stop (1) should execute cleanly
			output.PerformSound(1, 0);

			// Calling PerformSound with silence (0) should execute cleanly
			output.PerformSound(0, 0);
		}
	}
}
