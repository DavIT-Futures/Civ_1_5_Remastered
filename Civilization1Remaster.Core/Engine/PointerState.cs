namespace Civilization1Remaster.Core.Engine
{
	public sealed class PointerState
	{
		public const ushort PlaceAddress = 0x586e;

		public const ushort ShiftAddress = 0x587c;

		public const ushort CursorAddress = 0x5402;

		public byte[] Place { get; } = new byte[8];

		public byte[] Shift { get; } = new byte[2];

		public byte[] Cursor { get; } = new byte[2];

		public ushort Horiz
		{
			get => Word(this.Place, 0);
			set => SetWord(this.Place, 0, value);
		}

		public ushort Vert
		{
			get => Word(this.Place, 2);
			set => SetWord(this.Place, 2, value);
		}

		public ushort Buttons
		{
			get => Word(this.Place, 4);
			set => SetWord(this.Place, 4, value);
		}

		public ushort Clicks
		{
			get => Word(this.Place, 6);
			set => SetWord(this.Place, 6, value);
		}

		public byte ColumnShift
		{
			get => this.Shift[0];
			set => this.Shift[0] = value;
		}

		public byte DriverActive
		{
			get => this.Shift[1];
			set => this.Shift[1] = value;
		}

		public byte RefreshLatch
		{
			get => this.Cursor[0];
			set => this.Cursor[0] = value;
		}

		public byte PointerShown
		{
			get => this.Cursor[1];
			set => this.Cursor[1] = value;
		}

		private static ushort Word(byte[] bytes, int place) => (ushort)(bytes[place] | (bytes[place + 1] << 8));

		private static void SetWord(byte[] bytes, int place, ushort value)
		{
			bytes[place] = (byte)value;
			bytes[place + 1] = (byte)(value >> 8);
		}
	}
}
