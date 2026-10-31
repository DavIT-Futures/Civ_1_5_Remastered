namespace Civilization1Remaster.Machine
{
	public class CPUFlags
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
					this.amount ^= 0x1;
			}
		}

		public bool IsNoCarry
		{
			get
			{
				return (this.amount & 0x1) == 0;
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
					this.amount ^= 0x40;
			}
		}

		public bool IsEqual
		{
			get
			{
				return (this.amount & 0x40) != 0;
			}
			set
			{
				this.amount |= 0x40;
				if (!value)
					this.amount ^= 0x40;
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
					this.amount ^= 0x80;
			}
		}

		public bool IsBackward
		{
			get
			{
				return (this.amount & 0x400) != 0;
			}
			set
			{
				this.amount |= 0x400;
				if (!value)
					this.amount ^= 0x400;
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
					this.amount ^= 0x800;
			}
		}

		public bool IsAbove
		{
			get { return !this.IsCarry && !this.IsZero; }
		}

		public bool IsAboveOrEqual
		{
			get { return !this.IsCarry; }
		}

		public bool IsBelow
		{
			get { return this.IsCarry; }
		}

		public bool IsBelowOrEqual
		{
			get { return this.IsCarry || this.IsZero; }
		}

		public bool IsNonZero
		{
			get { return !this.IsZero; }
		}

		public bool IsNotEqual
		{
			get { return !this.IsZero; }
		}

		public bool IsGreater
		{
			get { return !((this.IsSigned ^ this.IsOverflow) | this.IsZero); }
		}

		public bool IsGreaterOrEqual
		{
			get { return !(this.IsSigned ^ this.IsOverflow); }
		}

		public bool IsLess
		{
			get { return this.IsSigned ^ this.IsOverflow; }
		}

		public bool IsLessOrEqual
		{
			get { return (this.IsSigned ^ this.IsOverflow) | this.IsZero; }
		}

		public bool IsNoOverflow
		{
			get { return !this.IsOverflow; }
		}

		public bool IsUnsigned
		{
			get { return !this.IsSigned; }
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
