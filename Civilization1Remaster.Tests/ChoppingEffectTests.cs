using Civilization1Remaster.Acoustics;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class ChoppingEffectTests
	{
		[Fact]
		public void MidiHitsBeginAndEnd()
		{
			List<(int, int, int)> dispatched = new List<(int, int, int)>();
			int beat = 0;

			while (ChoppingEffect.MidiTick(beat, (condition, content1, content2) => dispatched.Add((condition, content1, content2))))
				beat++;

			// Five strikes, each a wood block and a tom on the percussion channel, all of them released
			Assert.Equal(10, dispatched.Count(note => note.Item1 == 0x99 && note.Item3 > 0));
			Assert.Equal(10, dispatched.Count(note => note.Item1 == 0x89));
			Assert.InRange(beat, SoundDriverHost.BeatRatio, SoundDriverHost.BeatRatio * 3);
		}

		[Fact]
		public void GeneratedAudioPlaysHitsAndPauses()
		{
			short[] slices = ChoppingEffect.Synthesize(44100);

			Assert.InRange(slices.Length, 44100, 44100 * 3);
			Assert.Equal(16000, slices.Max(slice => Math.Abs((int)slice)));

			// Between the first two strikes the sound fades out
			Assert.True(slices.Skip(44100 * 30 / 100).Take(4410).All(slice => Math.Abs((int)slice) < 500));
		}
	}
}
