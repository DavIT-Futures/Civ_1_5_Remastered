using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The state of the mouse driver kept in C# (B6.7, part 1): the place, the buttons and the clicks at 0x586e - 0x5875
	/// (written by the mouse interrupt, PlatformServices.PointerEvent), the shift of the column at 0x587c, the driver switch at
	/// 0x587d and the pointer flags at 0x5402 (refresh latch) / 0x5403 (pointer on the screen). The memory reaches the bytes through overlays.
	/// </summary>
	public sealed class PointerState
	{
		/// <summary>The address of the place, buttons and clicks</summary>
		public const ushort PlaceAddress = 0x586e;

		/// <summary>The address of the column shift and the driver switch</summary>
		public const ushort ShiftAddress = 0x587c;

		/// <summary>The address of the cursor flags</summary>
		public const ushort CursorAddress = 0x5402;

		/// <summary>The bytes at 0x586e: column, row, buttons, clicks (words)</summary>
		public byte[] Place { get; } = new byte[8];

		/// <summary>The bytes at 0x587c: column shift, driver switch</summary>
		public byte[] Shift { get; } = new byte[2];

		/// <summary>The bytes at 0x5402: refresh latch, pointer shown</summary>
		public byte[] Cursor { get; } = new byte[2];

		/// <summary>The column of the pointer (0x586e)</summary>
		public ushort Horiz
		{
			get => Word(this.Place, 0);
			set => SetWord(this.Place, 0, value);
		}

		/// <summary>The row of the pointer (0x5870)</summary>
		public ushort Vert
		{
			get => Word(this.Place, 2);
			set => SetWord(this.Place, 2, value);
		}

		/// <summary>The buttons held (0x5872): bit 0 left, bit 1 right</summary>
		public ushort Buttons
		{
			get => Word(this.Place, 4);
			set => SetWord(this.Place, 4, value);
		}

		/// <summary>The buttons pressed since the game last asked (0x5874)</summary>
		public ushort Clicks
		{
			get => Word(this.Place, 6);
			set => SetWord(this.Place, 6, value);
		}

		/// <summary>The shift of the column of the driver (0x587c)</summary>
		public byte ColumnShift
		{
			get => this.Shift[0];
			set => this.Shift[0] = value;
		}

		/// <summary>The driver switch (0x587d)</summary>
		public byte DriverActive
		{
			get => this.Shift[1];
			set => this.Shift[1] = value;
		}

		/// <summary>The latch of the pointer refresh (0x5402): 2 while the mouse interrupt renews the screen</summary>
		public byte RefreshLatch
		{
			get => this.Cursor[0];
			set => this.Cursor[0] = value;
		}

		/// <summary>The pointer is on the screen (0x5403): the mouse interrupt then renews the screen when it moves</summary>
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
