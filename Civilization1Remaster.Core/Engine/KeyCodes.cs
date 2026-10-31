namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The codes of the keys as the game reads them (ReadKey, ReadNavigationKey): a character in the low byte, or for a
	/// special key its scan code in the high byte (with the character of the keypad digit in the low byte for Shift and a
	/// keypad arrow)
	/// </summary>
	public static class KeyCodes
	{
		public const ushort Backspace = 0x08;
		public const ushort Tab = 0x09;
		public const ushort Enter = 0x0d;
		public const ushort Escape = 0x1b;
		public const ushort Space = 0x20;
		public const ushort F1 = 0x3b00;
		public const ushort F2 = 0x3c00;
		public const ushort F3 = 0x3d00;
		public const ushort F4 = 0x3e00;
		public const ushort F5 = 0x3f00;
		public const ushort F6 = 0x4000;
		public const ushort F7 = 0x4100;
		public const ushort F8 = 0x4200;
		public const ushort F9 = 0x4300;
		public const ushort F10 = 0x4400;
		public const ushort Home = 0x4700;
		public const ushort Up = 0x4800;
		public const ushort PageUp = 0x4900;
		public const ushort Left = 0x4b00;
		public const ushort Right = 0x4d00;
		public const ushort End = 0x4f00;
		public const ushort Down = 0x5000;
		public const ushort PageDown = 0x5100;

		/// <summary>Shift and a keypad arrow: the scan code and the keypad digit</summary>
		public const ushort ShiftHome = 0x4737;

		public const ushort ShiftUp = 0x4838;
		public const ushort ShiftPageUp = 0x4939;
		public const ushort ShiftLeft = 0x4b34;
		public const ushort ShiftRight = 0x4d36;
		public const ushort ShiftEnd = 0x4f31;
		public const ushort ShiftDown = 0x5032;
		public const ushort ShiftPageDown = 0x5133;
		public const ushort AltQ = 0x1000;
		public const ushort AltW = 0x1100;
		public const ushort AltR = 0x1300;
		public const ushort AltO = 0x1800;
		public const ushort AltA = 0x1e00;
		public const ushort AltD = 0x2000;
		public const ushort AltG = 0x2200;
		public const ushort AltH = 0x2300;
		public const ushort AltC = 0x2e00;
		public const ushort AltV = 0x2f00;
		public const ushort AltM = 0x3200;
	}
}
