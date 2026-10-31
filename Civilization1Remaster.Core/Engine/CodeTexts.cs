using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The texts of the 5 codes of the text blocks ($US, $THEM, $BUCKS, $RPLC1, $RPLC2 at 0x3018 - 0x3030), which
	/// TextBlockPainter.ReflowText puts in place of the codes: 5 buffers of 0x16 bytes at 0x3037 - 0x30a4 of the data
	/// segment (their addresses at 0x30b8 - 0x30c0, CodeText1 - 5). B6.1: the block lives in a C# array (Attach), the code
	/// that still gets an address reads it through the memory (CPUMemory.AttachOverlay); the snapshots of the tests write
	/// it back at its address (MovedData). A text longer than its buffer runs into the next one (and past the block into
	/// the memory), like strcpy in the original.
	/// </summary>
	public sealed class CodeTexts
	{
		/// <summary>The address of the first buffer</summary>
		public const ushort Address = 0x3037;

		/// <summary>The size of one buffer</summary>
		public const int Stride = 0x16;

		/// <summary>The size of the block of the 5 buffers</summary>
		public const int Size = 5 * Stride;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public CodeTexts(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The text of the code (0 - 4) is the text (strcpy); returns the address of its buffer</summary>
		public ushort Set(int code, string text)
		{
			int place = code * Stride;

			for (int idx = 0; idx <= text.Length; idx++)
				Poke(place + idx, idx < text.Length ? (byte)text[idx] : (byte)0);

			return (ushort)(Address + place);
		}

		/// <summary>The text of the code is the text at the address (read whole before the copy, like the C runtime)</summary>
		public ushort Set(int code, ushort textAddr) =>
			Set(code, this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr)));

		/// <summary>
		/// The store moves from the memory into the array (after the machine is prepared): the current bytes are taken over,
		/// every access of the memory to the block goes to the array from now on
		/// </summary>
		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		/// <summary>A byte of the block (past it: the memory after it)</summary>
		private void Poke(int index, byte value)
		{
			if (this.attached && index >= 0 && index < Size)
				this.store[index] = value;
			else
				this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
		}
	}
}
