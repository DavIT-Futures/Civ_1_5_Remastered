using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Video
{
	public class CivRectangle
	{
		private readonly CPU processor;
		private readonly uint layoutAddr;

		public CivRectangle(CPU processor, uint layoutRef)
		{
			this.processor = processor;
			this.layoutAddr = layoutRef;
		}

		public ushort DisplayIdx
		{
			get { return this.processor.Storage.PullUInt16(this.layoutAddr); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr, value); }
		}

		public short Horiz
		{
			get { return (short)this.processor.Storage.PullUInt16(this.layoutAddr + 2); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 2, (ushort)value); }
		}

		public short Vert
		{
			get { return (short)this.processor.Storage.PullUInt16(this.layoutAddr + 4); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 4, (ushort)value); }
		}

		public short Breadth
		{
			get { return (short)this.processor.Storage.PullUInt16(this.layoutAddr + 6); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 6, (ushort)value); }
		}

		public short Altitude
		{
			get { return (short)this.processor.Storage.PullUInt16(this.layoutAddr + 8); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 8, (ushort)value); }
		}

		public ushort FlagSet
		{
			get { return this.processor.Storage.PullUInt16(this.layoutAddr + 0xa); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 0xa, value); }
		}

		public byte ForeHue
		{
			get { return this.processor.Storage.PullUInt8(this.layoutAddr + 0xc); }
			set { this.processor.Storage.EmitUInt8(this.layoutAddr + 0xc, value); }
		}

		public byte DotStyle
		{
			get { return this.processor.Storage.PullUInt8(this.layoutAddr + 0xd); }
			set { this.processor.Storage.EmitUInt8(this.layoutAddr + 0xd, value); }
		}

		public byte RearHue
		{
			get { return this.processor.Storage.PullUInt8(this.layoutAddr + 0xe); }
			set { this.processor.Storage.EmitUInt8(this.layoutAddr + 0xe, value); }
		}

		/// <summary>The word at 0xc: ForeHue and DotStyle</summary>
		public ushort ForeHues
		{
			get { return this.processor.Storage.PullUInt16(this.layoutAddr + 0xc); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 0xc, value); }
		}

		/// <summary>The word at 0xe: RearHue and the byte after it</summary>
		public ushort RearHues
		{
			get { return this.processor.Storage.PullUInt16(this.layoutAddr + 0xe); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 0xe, value); }
		}

		public ushort TypefaceIdx
		{
			get { return this.processor.Storage.PullUInt16(this.layoutAddr + 0x10); }
			set { this.processor.Storage.EmitUInt16(this.layoutAddr + 0x10, value); }
		}
	}
}
