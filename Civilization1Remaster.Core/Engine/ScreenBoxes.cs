namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The four boxes of the screens (B6.5): a display index, a rectangle, flags, hues and a typeface (CivRectangle), 0x12
	/// bytes each, every one followed in the data segment by the word pointing at it (0xaa the drawing box, 0x19d4 the back
	/// screen, 0x19e8 the picture screen, 0x19fc the sprites). Kept in C# arrays: the drivers and CivRectangle reach them
	/// through the memory (CPUMemory.AttachOverlay), the snapshots of the tests write them back (MovedData).
	/// </summary>
	public sealed class ScreenBoxes
	{
		/// <summary>The size of a box in bytes</summary>
		public const int Size = 0x12;

		/// <summary>The addresses of the boxes in the data segment</summary>
		public static ushort[] Addresses { get; } = [0x98, 0x19c2, 0x19d6, 0x19ea];

		/// <summary>The bytes of each box, in the order of Addresses</summary>
		public byte[][] Content { get; } = [new byte[Size], new byte[Size], new byte[Size], new byte[Size]];
	}
}
