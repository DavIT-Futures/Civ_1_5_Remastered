namespace Civilization1Remaster.Core.Engine
{
	public sealed class HueCycles
	{
		public const ushort Segment = 0x35cf;

		public const int Size = 0x344;

		private readonly CPU processor;

		public HueCycles(CPU processor)
		{
			this.processor = processor;
		}

		public byte[] Content { get; } = new byte[Size];

		public ushort Record(int cycleIdx) => Word((ushort)((cycleIdx << 1) + 2));

		public byte Byte(ushort place) => place < Size ? this.Content[place] : this.processor.Storage.PullUInt8(Segment, place);

		public void SetByte(ushort place, byte value)
		{
			if (place < Size)
				this.Content[place] = value;
			else
				this.processor.Storage.EmitUInt8(Segment, place, value);
		}

		public ushort Word(ushort place) => (ushort)(Byte(place) | (Byte((ushort)(place + 1)) << 8));

		public void SetWord(ushort place, ushort value)
		{
			SetByte(place, (byte)value);
			SetByte((ushort)(place + 1), (byte)(value >> 8));
		}
	}
}
