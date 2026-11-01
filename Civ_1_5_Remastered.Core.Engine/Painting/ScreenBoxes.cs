namespace Civ_1_5_Remastered.Core.Engine.Painting
{
	public sealed class ScreenBoxes
	{
		public const int Size = 0x12;

		public static ushort[] Addresses { get; } = [0x98, 0x19c2, 0x19d6, 0x19ea];

		public byte[][] Content { get; } = [new byte[Size], new byte[Size], new byte[Size], new byte[Size]];
	}
}
