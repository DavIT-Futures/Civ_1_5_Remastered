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
		public void RleRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = RlePack(content, 4);
			Assert.Equal(content, RleUnpack(packed));
		}

		[Theory]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(4)]
		[InlineData(10)]
		public void RleRoundTripForEveryThreshold(int floorTally)
		{
			byte[] content = ScreenLike(64, 64);
			Assert.Equal(content, RleUnpack(RlePack(content, floorTally)));
		}

		[Fact]
		public void RleWritesRunAsValueMarkerCount()
		{
			byte[] packed = RlePack(Enumerable.Repeat((byte)7, 10).ToArray(), 4);
			Assert.Equal(new byte[] { 7, 0x90, 10 }, packed);
		}

		[Fact]
		public void RleKeepsShortRunsLiteral()
		{
			Assert.Equal(new byte[] { 7, 7, 7, 8 }, RlePack(new byte[] { 7, 7, 7, 8 }, 4));
		}

		[Fact]
		public void RleEscapesMarkerByte()
		{
			Assert.Equal(new byte[] { 1, 0x90, 0, 2 }, RlePack(new byte[] { 1, 0x90, 2 }, 4));
		}

		[Fact]
		public void RleSplitsRunsLongerThan255()
		{
			byte[] packed = RlePack(Enumerable.Repeat((byte)3, 300).ToArray(), 4);
			Assert.Equal(new byte[] { 3, 0x90, 255, 3, 0x90, 45 }, packed);
		}

		[Fact]
		public void RleUnpackStopsAtDanglingMarker()
		{
			Assert.Equal(new byte[] { 4 }, RleUnpack(new byte[] { 4, 0x90 }));
		}

		[Theory]
		[MemberData(nameof(Samples))]
		public void LzwRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = LzwPack(content, 9, 11);
			Assert.Equal(11, packed[0]);
			Assert.Equal(content, LzwUnpack(packed.AsSpan(1).ToArray(), 9, 11));
		}

		[Theory]
		[InlineData(9, 10)]
		[InlineData(9, 12)]
		public void LzwRoundTripForOtherPeaks(int begin, int peak)
		{
			byte[] content = RandomBytes(20000, 3);
			byte[] packed = LzwPack(content, begin, peak);
			Assert.Equal(content, LzwUnpack(packed.AsSpan(1).ToArray(), begin, peak));
		}

		[Fact]
		public void LzwShrinksRepetitiveData()
		{
			byte[] content = Enumerable.Repeat((byte)'A', 10000).ToArray();
			Assert.True(LzwPack(content, 9, 11).Length < content.Length / 10);
		}

		[Theory]
		[MemberData(nameof(Samples))]
		public void PicturePipelineRoundTrip(string name, byte[] content)
		{
			Assert.NotNull(name);
			byte[] packed = LzwPack(RlePack(content, 4), 9, 11);
			Assert.Equal(content, RleUnpack(LzwUnpack(packed.AsSpan(1).ToArray(), 9, 11)));
		}

		[Theory]
		[InlineData("", 0x00000000u)]
		[InlineData("a", 0xe8b7be43u)]
		[InlineData("abc", 0x352441c2u)]
		[InlineData("123456789", 0xcbf43926u)]
		[InlineData("The quick brown fox jumps over the lazy dog", 0x414fa339u)]
		public void Crc32MatchesKnownValues(string text, uint expected)
		{
			byte[] content = Encoding.ASCII.GetBytes(text);
			Assert.Equal(expected, CRC32.ObtainCRC32(content));
		}

		[Fact]
		public void Crc32NoticesOneBitChange()
		{
			byte[] content = RandomBytes(1000, 4);
			uint before = CRC32.ObtainCRC32(content);
			content[500] ^= 1;
			Assert.NotEqual(before, CRC32.ObtainCRC32(content));
		}

		internal static byte[] RlePack(byte[] content, int floorTally)
		{
			MemoryStream sink = new MemoryStream();
			RLE.Pack(sink, new MemoryStream(content), floorTally);
			return sink.ToArray();
		}

		internal static byte[] RleUnpack(byte[] content)
		{
			MemoryStream sink = new MemoryStream();
			RLE.Unpack(sink, new MemoryStream(content));
			return sink.ToArray();
		}

		internal static byte[] LzwPack(byte[] content, int begin, int peak)
		{
			MemoryStream sink = new MemoryStream();
			LZW.Pack(sink, new MemoryStream(content), begin, peak);
			return sink.ToArray();
		}

		internal static byte[] LzwUnpack(byte[] content, int begin, int peak)
		{
			MemoryStream sink = new MemoryStream();
			LZW.Unpack(sink, new MemoryStream(content), begin, peak);
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

			for (int idx = 0; idx < content.Length; idx++)
			{
				int row = idx / width;
				content[idx] = noise.Next(20) == 0 ? (byte)noise.Next(256) : (byte)(row / 8);
			}

			return content;
		}
	}
}
