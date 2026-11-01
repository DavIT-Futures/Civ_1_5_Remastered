using Civilization1Remaster.Foundation.Binary;

namespace Civilization1Remaster.Tests.Foundation
{
	public partial class LittleEndianStreamTests
	{
		[Fact]
		public void ReadsLittleEndian()
		{
			MemoryStream source = Source(0xfe, 0x34, 0x12, 0xff, 0xff, 0x00, 0x80);

			Assert.Equal(0xfe, source.PullByteOrZero());
			Assert.Equal(0x1234, source.PullWordOrZero());
			Assert.Equal(-1, source.PullSignedWordOrZero());
			Assert.Equal(-32768, source.PullSignedWordOrZero());
		}

		[Fact]
		public void LenientReadsGiveZeroAtEndAndConsumeWhatWasThere()
		{
			MemoryStream source = Source(0x34);

			Assert.Equal(0, source.PullWordOrZero());
			Assert.Equal(1, source.Position);
			Assert.Equal(0, source.PullByteOrZero());
			Assert.Equal(0, source.PullSignedWordOrZero());
		}

		[Fact]
		public void TryPullReportsTheEnd()
		{
			MemoryStream source = Source(0xcd, 0xab, 0x01);

			Assert.True(source.TryPullWord(out ushort first));
			Assert.Equal(0xabcd, first);
			Assert.False(source.TryPullWord(out ushort second));
			Assert.Equal(0, second);
			Assert.Equal(3, source.Position);
		}

		[Fact]
		public void ReadsAcrossShortReads()
		{
			Assert.Equal(0x1234, new TrickleStream(new byte[] { 0x34, 0x12 }).PullWordOrZero());
		}

		[Fact]
		public void WritesLittleEndian()
		{
			MemoryStream sink = new MemoryStream();
			sink.EmitWord(0xbeef);
			sink.EmitSignedWord(-2);
			sink.EmitSignedWord(0x0102);

			Assert.Equal(new byte[] { 0xef, 0xbe, 0xfe, 0xff, 0x02, 0x01 }, sink.ToArray());
		}

		[Fact]
		public void WriteThenReadRoundTrip()
		{
			MemoryStream sink = new MemoryStream();

			for (int value = short.MinValue; value <= short.MaxValue; value += 97)
			{
				sink.EmitSignedWord((short)value);
			}

			sink.Position = 0;

			for (int value = short.MinValue; value <= short.MaxValue; value += 97)
			{
				Assert.Equal((short)value, sink.PullSignedWordOrZero());
			}
		}

		private static MemoryStream Source(params byte[] content) => new MemoryStream(content);
	}
}
