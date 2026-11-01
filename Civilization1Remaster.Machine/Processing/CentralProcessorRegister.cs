namespace Civilization1Remaster.Machine.Processing
{
	public class CentralProcessorRegister
	{
		private uint quantity = 0;

		public CentralProcessorRegister()
		{
		}

		public CentralProcessorRegister(uint amount)
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

		public ushort Word
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

		public override string ToString()
		{
			return $"0x{this.UpperWord:x4}:0x{this.Word:x4}";
		}
	}
}
