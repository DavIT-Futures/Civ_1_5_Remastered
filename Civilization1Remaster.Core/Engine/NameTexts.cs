
namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The names of the players at 0x0111 - 0x0266 of the data segment, which the pointer tables of CIV.EXE (0x1982 the
	/// civilizations, 0x1992 their plurals, 0x19a2 the leaders, 0x19b2 the titles of the governments) point into; the game
	/// changes them (a new game, a civil war, the titles of a woman) and they go with the saved games. B6.1: the block lives
	/// in a C# array (Attach), the code that still gets an address reads it through the memory (CPUMemory.AttachOverlay).
	/// A name longer than its place runs into the next one (and past the block into the memory), like strcpy in the original.
	/// </summary>
	public sealed class NameTexts
	{
		/// <summary>The address of the block (the first civilization name)</summary>
		public const ushort Address = 0x0111;

		/// <summary>The size of the block in bytes (to the end of the last title, 0x0266)</summary>
		public const int Size = 0x156;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public NameTexts(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The name at the address (from a pointer table) is the text (strcpy); returns the address</summary>
		public ushort Set(ushort nameAddr, string text)
		{
			for (int idx = 0; idx <= text.Length; idx++)
				Poke(nameAddr - Address + idx, idx < text.Length ? (byte)text[idx] : (byte)0);

			return nameAddr;
		}

		/// <summary>The name at the address is the text at the other address (read whole before the copy, like the C runtime)</summary>
		public ushort Set(ushort nameAddr, ushort textAddr) =>
			Set(nameAddr, this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr)));

		/// <summary>
		/// The store moves from the memory into the array (after the machine is prepared): the current bytes are taken over,
		/// every access of the memory to the block goes to the array from now on
		/// </summary>
		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		/// <summary>A byte of the block (outside it: the memory)</summary>
		private void Poke(int index, byte value)
		{
			if (this.attached && index >= 0 && index < Size)
				this.store[index] = value;
			else
				this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
		}
	}
}
