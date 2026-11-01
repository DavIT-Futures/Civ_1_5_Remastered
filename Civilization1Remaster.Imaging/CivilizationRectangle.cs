namespace Civilization1Remaster.Imaging
{
	public class CivilizationRectangle
	{
		private readonly CentralProcessor processor;
		private readonly uint layoutAddress;

		public CivilizationRectangle(CentralProcessor processor, uint layoutAddress)
		{
			this.processor = processor;
			this.layoutAddress = layoutAddress;
		}

		public ushort DisplayIndex
		{
			get { return this.processor.Storage.PullWord(this.layoutAddress); }
			set { this.processor.Storage.EmitWord(this.layoutAddress, value); }
		}

		public short Horizontal
		{
			get { return (short)this.processor.Storage.PullWord(this.layoutAddress + 2); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 2, (ushort)value); }
		}

		public short Vertical
		{
			get { return (short)this.processor.Storage.PullWord(this.layoutAddress + 4); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 4, (ushort)value); }
		}

		public short Breadth
		{
			get { return (short)this.processor.Storage.PullWord(this.layoutAddress + 6); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 6, (ushort)value); }
		}

		public short Altitude
		{
			get { return (short)this.processor.Storage.PullWord(this.layoutAddress + 8); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 8, (ushort)value); }
		}

		public ushort FlagSet
		{
			get { return this.processor.Storage.PullWord(this.layoutAddress + 0xa); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 0xa, value); }
		}

		public byte ForegroundHue
		{
			get { return this.processor.Storage.PullByte(this.layoutAddress + 0xc); }
			set { this.processor.Storage.EmitByte(this.layoutAddress + 0xc, value); }
		}

		public byte DotStyle
		{
			get { return this.processor.Storage.PullByte(this.layoutAddress + 0xd); }
			set { this.processor.Storage.EmitByte(this.layoutAddress + 0xd, value); }
		}

		public byte RearHue
		{
			get { return this.processor.Storage.PullByte(this.layoutAddress + 0xe); }
			set { this.processor.Storage.EmitByte(this.layoutAddress + 0xe, value); }
		}

		public ushort ForegroundHues
		{
			get { return this.processor.Storage.PullWord(this.layoutAddress + 0xc); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 0xc, value); }
		}

		public ushort RearHues
		{
			get { return this.processor.Storage.PullWord(this.layoutAddress + 0xe); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 0xe, value); }
		}

		public ushort TypefaceIndex
		{
			get { return this.processor.Storage.PullWord(this.layoutAddress + 0x10); }
			set { this.processor.Storage.EmitWord(this.layoutAddress + 0x10, value); }
		}
	}
}
