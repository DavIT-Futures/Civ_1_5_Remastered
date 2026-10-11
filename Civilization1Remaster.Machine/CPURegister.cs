namespace Civilization1Remaster.Emulation
{
	public class CPURegister
	{
		private uint quantity = 0;

		public CPURegister()
		{ }

		public CPURegister(uint amount)
		{
			this.quantity = amount;
		}

		public byte LowByte
		{
			get
			{
				return (byte)(this.quantity & 0xff);
			}
			set
			{
				this.quantity = (uint)((this.quantity & 0xffffff00) | value);
			}
		}

		public byte HighByte
		{
			get
			{
				return (byte)((this.quantity & 0xff00) >> 8);
			}
			set
			{
				this.quantity = (uint)((this.quantity & 0xffff00ff) | ((uint)value << 8));
			}
		}

		public ushort Value16
		{
			get
			{
				return (ushort)(this.quantity & 0xffff);
			}
			set
			{
				this.quantity = (uint)((this.quantity & 0xffff0000) | (uint)value);
			}
		}

		public ushort UpperWord
		{
			get
			{
				return (ushort)((this.quantity & 0xffff0000) >> 16);
			}
			set
			{
				this.quantity = (uint)((this.quantity & 0xffff) | ((uint)value << 16));
			}
		}

		public uint Value32
		{
			get
			{
				return this.quantity;
			}
			set
			{
				this.quantity = value;
			}
		}

		public override string ToString()
		{
			return $"0x{this.UpperWord:x4}:0x{this.Value16:x4}";
		}
	}
}
