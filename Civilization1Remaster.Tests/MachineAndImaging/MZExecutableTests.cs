using Civilization1Remaster.Machine;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class MZExecutableTests
	{
		private const int ContentLength = 100;

		/// <summary>
		/// A small executable: 3 paragraph header with one relocation, 100 bytes of code.
		/// </summary>
		internal static byte[] Synthetic()
		{
			MemoryStream sink = new MemoryStream();
			void Word(int value) { sink.WriteByte((byte)value); sink.WriteByte((byte)(value >> 8)); }

			Word(0x5a4d);           // signature
			Word(48 + ContentLength); // bytes in last page
			Word(1);                // pages
			Word(1);                // relocations
			Word(3);                // header paragraphs
			Word(0x10);             // min allocation
			Word(0xffff);           // max allocation
			Word(0x20);             // SS
			Word(0x400);            // SP
			Word(0);                // checksum
			Word(0x12);             // IP
			Word(0x01);             // CS
			Word(0x1e);             // relocation table
			Word(0);                // overlay
			Word(1);                // overlay info
			Word(4);                // relocation: offset
			Word(0);                // relocation: segment

			while (sink.Length < 48)
				sink.WriteByte(0);

			for (int idx = 0; idx < ContentLength; idx++)
				sink.WriteByte((byte)(idx * 3));

			return sink.ToArray();
		}

		[Fact]
		public void ReadsHeaderAndContent()
		{
			MZExecutable program = new MZExecutable(new MemoryStream(Synthetic()));

			Assert.Equal(0x5a4d, program.Stamp);
			Assert.Equal(0x10, program.LowestReservation);
			Assert.Equal(0xffff, program.HighestReservation);
			Assert.Equal(0x20, program.StartingStackseg);
			Assert.Equal(0x400, program.StartingStackptr);
			Assert.Equal(0x12, program.StartingInstrptr);
			Assert.Equal(0x01, program.StartingCodeseg);
			Assert.Equal(0, program.LayerOrdinal);
			Assert.Equal(1, program.LayerIdx);
			Assert.Equal(Enumerable.Range(0, ContentLength).Select(idx => (byte)(idx * 3)).ToArray(), program.Content);
			Assert.Single(program.Rebases);
			Assert.Equal(4u, program.Rebases[0].Displacement);
			Assert.Equal(0u, program.Rebases[0].Section);
			Assert.Empty(program.Layers);
		}

		[Fact]
		public void RelocationsAddTheLoadSegment()
		{
			MZExecutable program = new MZExecutable(new MemoryStream(Synthetic()));
			ushort before = (ushort)(program.Content[4] | (program.Content[5] << 8));

			program.EnactRelocations(0x1234);

			Assert.Equal((ushort)(before + 0x1234), (ushort)(program.Content[4] | (program.Content[5] << 8)));
			Assert.Equal(Synthetic()[48 + 6], program.Content[6]);
		}

		[Fact]
		public void WriteThenReadGivesTheSameProgram()
		{
			MZExecutable program = new MZExecutable(new MemoryStream(Synthetic()));
			MemoryStream sink = new MemoryStream();
			program.EmitToFile(sink);

			Assert.Equal(512, sink.Length);

			sink.Position = 0;
			MZExecutable copy = new MZExecutable(sink);

			AssertSame(program, copy);
		}

		[Fact]
		public void WrittenChecksumMakesWordSumZero()
		{
			MZExecutable program = new MZExecutable(new MemoryStream(Synthetic()));
			MemoryStream sink = new MemoryStream();
			program.EmitToFile(sink);
			byte[] written = sink.ToArray();
			int sum = 0;

			for (int idx = 0; idx < written.Length; idx += 2)
				sum += written[idx] | (written[idx + 1] << 8);

			Assert.Equal(0, sum & 0xffff);
		}

		[Fact]
		public void RejectsOtherFiles()
		{
			byte[] content = Synthetic();
			content[0] = (byte)'X';

			Exception problem = Assert.Throws<Exception>(() => new MZExecutable(new MemoryStream(content)));
			Assert.Equal("Not an MS-DOS executable file", problem.Message);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(10)]
		[InlineData(29)]
		public void TruncatedHeaderThrowsUnexpectedEnd(int length)
		{
			byte[] content = Synthetic().AsSpan(0, length).ToArray();

			Exception problem = Assert.Throws<Exception>(() => new MZExecutable(new MemoryStream(content)));
			Assert.Equal(typeof(Exception), problem.GetType());
			Assert.Equal("Unexpected end of stream", problem.Message);
		}

		[Fact]
		public void TruncatedRelocationTableThrowsUnexpectedEnd()
		{
			byte[] content = Synthetic().AsSpan(0, 0x20).ToArray();

			Exception problem = Assert.Throws<Exception>(() => new MZExecutable(new MemoryStream(content)));
			Assert.Equal("Unexpected end of stream", problem.Message);
		}

		[Fact]
		public void OriginalExecutablesLoadAndRoundTrip()
		{
			foreach (string route in GameFiles.Find("*.EXE"))
			{
				byte[] original = File.ReadAllBytes(route);
				MZExecutable program = new MZExecutable(new MemoryStream(original));
				int headerParagraphs = original[8] | (original[9] << 8);

				Assert.Equal(0x5a4d, program.Stamp);
				Assert.Equal(original.AsSpan(headerParagraphs * 16, program.Content.Length).ToArray(), program.Content);

				MemoryStream sink = new MemoryStream();
				program.EmitToFile(sink);
				sink.Position = 0;
				AssertSame(program, new MZExecutable(sink));
			}
		}

		private static void AssertSame(MZExecutable expected, MZExecutable actual)
		{
			Assert.Equal(expected.Stamp, actual.Stamp);
			Assert.Equal(expected.LowestReservation, actual.LowestReservation);
			Assert.Equal(expected.HighestReservation, actual.HighestReservation);
			Assert.Equal(expected.StartingStackseg, actual.StartingStackseg);
			Assert.Equal(expected.StartingStackptr, actual.StartingStackptr);
			Assert.Equal(expected.StartingInstrptr, actual.StartingInstrptr);
			Assert.Equal(expected.StartingCodeseg, actual.StartingCodeseg);
			Assert.Equal(expected.LayerOrdinal, actual.LayerOrdinal);
			Assert.Equal(expected.LayerIdx, actual.LayerIdx);
			Assert.Equal(expected.Content, actual.Content);
			Assert.Equal(expected.Rebases.Select(item => (item.Section, item.Displacement)), actual.Rebases.Select(item => (item.Section, item.Displacement)));
			Assert.Equal(expected.Layers.Count, actual.Layers.Count);

			for (int idx = 0; idx < expected.Layers.Count; idx++)
				AssertSame(expected.Layers[idx], actual.Layers[idx]);
		}
	}
}
