using System.Text;
using Civilization1Remaster.Foundation.Squeeze;

namespace Civilization1Remaster.Tests.Foundation
{
	public class CompressionTests
	{
		public static IEnumerable<object[]> Samples()
		{
			yield return new object[] { "empty", Array.Empty<byte>() };
			yield return new object[] { "one byte", new byte[] { 7 } };
			yield return new object[] { "marker byte", new byte[] { 0x90 } };
			yield return new object[] { "marker run", Enumerable.Repeat((byte)0x90, 40).ToArray() };
			yield return new object[] { "marker mixed", new byte[] { 1, 0x90, 0x90, 2, 0x90, 0, 0x90, 0x90, 0x90, 0x90, 0x90, 3 } };
			yield return new object[] { "short runs", new byte[] { 1, 1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4 } };
			yield return new object[] { "long run", Enumerable.Repeat((byte)5, 1000).ToArray() };
			yield return new object[] { "run of 255", Enumerable.Repeat((byte)9, 255).ToArray() };
			yield return new object[] { "run of 256", Enumerable.Repeat((byte)9, 256).ToArray() };
			yield return new object[] { "text", Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("TOBEORNOTTOBEORTOBEORNOT#", 40))) };
			yield return new object[] { "all values", Enumerable.Range(0, 256).Select(value => (byte)value).ToArray() };
			yield return new object[] { "random 4k", RandomBytes(4096, 1) };
			yield return new object[] { "random 64k", RandomBytes(65536, 2) };
			yield return new object[] { "screen-like 64k", ScreenLike(320, 200) };
		}

		[Theory]
		[MemberData(nameof(Samples))]
		public void RunLengthRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = RunLengthPack(content, 4);
			Assert.Equal(content, RunLengthUnpack(packed));
		}

		[Theory]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(4)]
		[InlineData(10)]
		public void RunLengthRoundTripForEveryThreshold(int floorTally)
		{
			byte[] content = ScreenLike(64, 64);
			Assert.Equal(content, RunLengthUnpack(RunLengthPack(content, floorTally)));
		}

		[Fact]
		public void RunLengthWritesRunAsValueMarkerCount()
		{
			byte[] packed = RunLengthPack(Enumerable.Repeat((byte)7, 10).ToArray(), 4);
			Assert.Equal(new byte[] { 7, 0x90, 10 }, packed);
		}

		[Fact]
		public void RunLengthKeepsShortRunsLiteral()
		{
			Assert.Equal(new byte[] { 7, 7, 7, 8 }, RunLengthPack(new byte[] { 7, 7, 7, 8 }, 4));
		}

		[Fact]
		public void RunLengthEscapesMarkerByte()
		{
			Assert.Equal(new byte[] { 1, 0x90, 0, 2 }, RunLengthPack(new byte[] { 1, 0x90, 2 }, 4));
		}

		[Fact]
		public void RunLengthSplitsRunsLongerThanOneByte()
		{
			byte[] packed = RunLengthPack(Enumerable.Repeat((byte)3, 300).ToArray(), 4);
			Assert.Equal(new byte[] { 3, 0x90, 255, 3, 0x90, 45 }, packed);
		}

		[Fact]
		public void RunLengthUnpackStopsAtDanglingMarker()
		{
			Assert.Equal(new byte[] { 4 }, RunLengthUnpack(new byte[] { 4, 0x90 }));
		}

		[Theory]
		[MemberData(nameof(Samples))]
		public void DictionaryCompressionRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = DictionaryCompressionPack(content, 9, 11);
			Assert.Equal(11, packed[0]);
			Assert.Equal(content, DictionaryCompressionUnpack(packed.AsSpan(1).ToArray(), 9, 11));
		}

		[Theory]
		[InlineData(9, 10)]
		[InlineData(9, 12)]
		public void DictionaryCompressionRoundTripForOtherPeaks(int begin, int peak)
		{
			byte[] content = RandomBytes(20000, 3);
			byte[] packed = DictionaryCompressionPack(content, begin, peak);
			Assert.Equal(content, DictionaryCompressionUnpack(packed.AsSpan(1).ToArray(), begin, peak));
		}

		[Fact]
		public void DictionaryCompressionShrinksRepetitiveData()
		{
			byte[] content = Enumerable.Repeat((byte)'A', 10000).ToArray();
			Assert.True(DictionaryCompressionPack(content, 9, 11).Length < content.Length / 10);
		}

		[Theory]
		[MemberData(nameof(Samples))]
		public void PicturePipelineRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = DictionaryCompressionPack(RunLengthPack(content, 4), 9, 11);
			Assert.Equal(content, RunLengthUnpack(DictionaryCompressionUnpack(packed.AsSpan(1).ToArray(), 9, 11)));
		}

		[Theory]
		[InlineData("", 0x00000000u)]
		[InlineData("a", 0xe8b7be43u)]
		[InlineData("abc", 0x352441c2u)]
		[InlineData("123456789", 0xcbf43926u)]
		[InlineData("The quick brown fox jumps over the lazy dog", 0x414fa339u)]
		public void ChecksumMatchesKnownValues(string text, uint expected)
		{
			byte[] content = Encoding.ASCII.GetBytes(text);
			Assert.Equal(expected, CyclicRedundancyCheck.ObtainChecksum(content));
		}

		[Fact]
		public void ChecksumNoticesOneBitChange()
		{
			byte[] content = RandomBytes(1000, 4);
			uint before = CyclicRedundancyCheck.ObtainChecksum(content);
			content[500] ^= 1;
			Assert.NotEqual(before, CyclicRedundancyCheck.ObtainChecksum(content));
		}

		internal static byte[] RunLengthPack(byte[] content, int floorTally)
		{
			MemoryStream sink = new MemoryStream();
			RunLength.Pack(sink, new MemoryStream(content), floorTally);
			return sink.ToArray();
		}

		internal static byte[] RunLengthUnpack(byte[] content)
		{
			MemoryStream sink = new MemoryStream();
			RunLength.Unpack(sink, new MemoryStream(content));
			return sink.ToArray();
		}

		internal static byte[] DictionaryCompressionPack(byte[] content, int begin, int peak)
		{
			MemoryStream sink = new MemoryStream();
			DictionaryCompression.Pack(sink, new MemoryStream(content), begin, peak);
			return sink.ToArray();
		}

		internal static byte[] DictionaryCompressionUnpack(byte[] content, int begin, int peak)
		{
			MemoryStream sink = new MemoryStream();
			DictionaryCompression.Unpack(sink, new MemoryStream(content), begin, peak);
			return sink.ToArray();
		}

		internal static byte[] RandomBytes(int length, int seed)
		{
			byte[] content = new byte[length];
			new Random(seed).NextBytes(content);
			return content;
		}

		internal static byte[] ScreenLike(int width, int height)
		{
			Random noise = new Random(width * height);
			byte[] content = new byte[width * height];

			for (int index = 0; index < content.Length; index++)
			{
				int row = index / width;
				content[index] = noise.Next(20) == 0 ? (byte)noise.Next(256) : (byte)(row / 8);
			}

			return content;
		}
	}
}
