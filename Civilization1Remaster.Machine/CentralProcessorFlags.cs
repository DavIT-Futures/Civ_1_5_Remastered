namespace Civilization1Remaster.Machine
{
	public class CentralProcessorFlags
	{
		private ushort amount = 0x2;

		public bool IsCarry
		{
			get
			{
				return (this.amount & 0x1) != 0;
			}

			set
			{
				this.amount |= 0x1;
				if (!value)
				{
					this.amount ^= 0x1;
				}
			}
		}

		public bool IsZero
		{
			get
			{
				return (this.amount & 0x40) != 0;
			}

			set
			{
				this.amount |= 0x40;
				if (!value)
				{
					this.amount ^= 0x40;
				}
			}
		}

		public bool IsSigned
		{
			get
			{
				return (this.amount & 0x80) != 0;
			}

			set
			{
				this.amount |= 0x80;
				if (!value)
				{
					this.amount ^= 0x80;
				}
			}
		}

		public bool IsOverflow
		{
			get
			{
				return (this.amount & 0x800) != 0;
			}

			set
			{
				this.amount |= 0x800;
				if (!value)
				{
					this.amount ^= 0x800;
				}
			}
		}

		public ushort Datum
		{
			get
			{
				return this.amount;
			}

			set
			{
				this.amount = value;
			}
		}
	}
}
