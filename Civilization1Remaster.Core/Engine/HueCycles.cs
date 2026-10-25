using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The colour cycles of the driver (B6.5) at 0x35cf:0000 - 0x0343, kept in C#: a word at 0 (0xffff), the addresses of
	/// the 8 cycle records at 2 - 0x11 and 0xffff after them, then the records, 0x66 bytes each from 0x14 (step 0, position
	/// 2, length of the colours 4, first colour 6, delay 7, timer 8, the colours twice from 9). The code of the cycles reads
	/// and writes here; the memory reaches the bytes through an overlay (CPUMemory.AttachOverlay). An offset past the block
	/// (a broken record address) goes to the memory, like in the original.
	/// </summary>
	public sealed class HueCycles
	{
		/// <summary>The segment of the cycles</summary>
		public const ushort Segment = 0x35cf;

		/// <summary>The size of the block in bytes</summary>
		public const int Size = 0x344;

		private readonly CPU processor;

		public HueCycles(CPU processor)
		{
			this.processor = processor;
		}

		/// <summary>The bytes of the block</summary>
		public byte[] Content { get; } = new byte[Size];

		/// <summary>The address of the record of the cycle 0 - 7 (the table at 2)</summary>
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
