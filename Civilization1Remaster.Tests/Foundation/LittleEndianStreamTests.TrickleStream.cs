namespace Civilization1Remaster.Tests.Foundation
{
	public partial class LittleEndianStreamTests
	{
		private sealed class TrickleStream : MemoryStream
		{
			public TrickleStream(byte[] content)
				: base(content)
			{
			}

			public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(1, buffer.Length)));

			public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(1, count));
		}
	}
}
