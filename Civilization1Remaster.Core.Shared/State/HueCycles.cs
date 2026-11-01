namespace Civilization1Remaster.Core.Shared.State
{
	public sealed class HueCycles
	{
		public const ushort Segment = 0x35cf;

		public const int Size = 0x344;

		private readonly CentralProcessor processor;

		public HueCycles(CentralProcessor processor)
		{
			this.processor = processor;
		}

		public byte[] Content { get; } = new byte[Size];

		public ushort Record(int cycleIndex) => Word((ushort)((cycleIndex << 1) + 2));

		public byte Byte(ushort place) => place < Size ? this.Content[place] : this.processor.Storage.PullByte(Segment, place);

		public void SetByte(ushort place, byte value)
		{
			if (place < Size)
			{
				this.Content[place] = value;
			}
			else
			{
				this.processor.Storage.EmitByte(Segment, place, value);
			}
		}

		public ushort Word(ushort place) => (ushort)(Byte(place) | (Byte((ushort)(place + 1)) << 8));

		public void SetWord(ushort place, ushort value)
		{
			SetByte(place, (byte)value);
			SetByte((ushort)(place + 1), (byte)(value >> 8));
		}
	}
}
