using Civilization1Remaster.Foundation.Binary;

namespace Civilization1Remaster.Tests.Foundation
{
	public class LittleEndianStreamTests
	{
		private static MemoryStream Source(params byte[] content) => new MemoryStream(content);

		[Fact]
		public void ReadsLittleEndian()
		{
			MemoryStream source = Source(0xfe, 0x34, 0x12, 0x78, 0x56, 0x34, 0x12, 0xff, 0xff, 0x00, 0x80);

			Assert.Equal(0xfe, source.PullUInt8());
			Assert.Equal(0x1234, source.PullUInt16());
			Assert.Equal(0x12345678u, source.PullUInt32());
			Assert.Equal(-1, source.PullInt16OrZero());
			Assert.Equal(-32768, source.PullInt16OrZero());
		}

		[Fact]
		public void LenientReadsGiveZeroAtEndAndConsumeWhatWasThere()
		{
			MemoryStream source = Source(0x34);

			Assert.Equal(0, source.PullUInt16OrZero());
			Assert.Equal(1, source.Position);
			Assert.Equal(0, source.PullUInt8OrZero());
			Assert.Equal(0, source.PullInt16OrZero());
		}

		[Fact]
		public void LenientReadsMatchStrictReadsWhenDataIsThere()
		{
			MemoryStream source = Source(0x01, 0x02, 0x03, 0x04, 0x05);

			Assert.Equal(0x01, source.PullUInt8OrZero());
			Assert.Equal(0x0302, source.PullUInt16OrZero());
			Assert.Equal(0x0504, source.PullInt16OrZero());
		}

		[Theory]
		[InlineData(0, 1)]
		[InlineData(1, 2)]
		[InlineData(3, 4)]
		public void StrictReadsThrowTheOriginalException(int available, int needed)
		{
			MemoryStream source = Source(new byte[available]);

			Exception problem = Assert.Throws<Exception>(() =>
			{
				switch (needed)
				{
					case 1: source.PullUInt8(); break;
					case 2: source.PullUInt16(); break;
					default: source.PullUInt32(); break;
				}
			});

			Assert.Equal(typeof(Exception), problem.GetType());
			Assert.Equal(LittleEndianStream.UnexpectedEnd, problem.Message);
			Assert.Equal("Unexpected end of stream", problem.Message);
			Assert.Equal(available, source.Position);
		}

		[Fact]
		public void TryPullReportsTheEnd()
		{
			MemoryStream source = Source(0xcd, 0xab, 0x01);

			Assert.True(source.TryPullUInt16(out ushort first));
			Assert.Equal(0xabcd, first);
			Assert.False(source.TryPullUInt16(out ushort second));
			Assert.Equal(0, second);
			Assert.Equal(3, source.Position);
		}

		[Fact]
		public void ReadsAcrossShortReads()
		{
			// a stream that returns one byte per Read call
			Assert.Equal(0x12345678u, new TrickleStream(new byte[] { 0x78, 0x56, 0x34, 0x12 }).PullUInt32());
		}

		[Fact]
		public void WritesLittleEndian()
		{
			MemoryStream sink = new MemoryStream();
			sink.EmitUInt16(0xbeef);
			sink.EmitInt16(-2);
			sink.EmitInt16(0x0102);

			Assert.Equal(new byte[] { 0xef, 0xbe, 0xfe, 0xff, 0x02, 0x01 }, sink.ToArray());
		}

		[Fact]
		public void WriteThenReadRoundTrip()
		{
			MemoryStream sink = new MemoryStream();

			for (int value = short.MinValue; value <= short.MaxValue; value += 97)
				sink.EmitInt16((short)value);

			sink.Position = 0;

			for (int value = short.MinValue; value <= short.MaxValue; value += 97)
				Assert.Equal((short)value, sink.PullInt16OrZero());
		}

		private sealed class TrickleStream : MemoryStream
		{
			public TrickleStream(byte[] content) : base(content) { }

			public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(1, buffer.Length)));

			public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(1, count));
		}
	}
}
