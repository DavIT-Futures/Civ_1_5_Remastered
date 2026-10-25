using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The message buffer of the game at 0xba06 of the data segment, where the texts of the messages, dialogs and menus are
	/// built (and, now and then, a buffer of bytes of a file). B6.1: every use of the buffer goes through this class, and its
	/// 0x300 bytes (0xba06 - 0xbd05, 0xbd06 is the next variable) live in a C# array (Attach): the code that still gets the
	/// address (dialogs, menus, painters, files) reads and writes the array through the memory (CPUMemory.AttachOverlay).
	/// The texts are built with the same functions of the C runtime as before, so the bytes and AX stay as they were; the
	/// snapshots of the tests write the array back at its address (MovedData).
	/// </summary>
	public sealed class TextBuffer
	{
		/// <summary>The address of the buffer in the data segment</summary>
		public const ushort Address = 0xba06;

		/// <summary>The size of the buffer in bytes</summary>
		public const int Size = 0x300;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		public TextBuffer(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The text of the buffer is the text (strcpy); returns the address, like the C runtime (also in AX)</summary>
		public ushort Set(string text) => this.game.Libc.CopyText(Address, text);

		/// <summary>The text of the buffer is the text at the address</summary>
		public ushort Set(ushort textAddr) => this.game.Libc.CopyText(Address, textAddr);

		/// <summary>The text goes to the end of the text of the buffer (strcat)</summary>
		public ushort Append(string text) => this.game.Libc.AppendText(Address, text);

		/// <summary>The text at the address goes to the end of the text of the buffer</summary>
		public ushort Append(ushort textAddr) => this.game.Libc.AppendText(Address, textAddr);

		/// <summary>The buffer holds the empty text (its first byte 0)</summary>
		public void Clear() => this.game.Processor.EmitGlobalByte(Address, 0);

		/// <summary>The length of the text of the buffer (strlen, also in AX)</summary>
		public ushort Length => this.game.Libc.TextLength(Address);

		/// <summary>A byte of the buffer</summary>
		public byte this[int index]
		{
			get => this.game.Processor.PullGlobalByte((ushort)(Address + index));
			set => this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
		}

		/// <summary>The bytes of the buffer, all of them (also after the end of the text, like the memory of the original)</summary>
		public byte[] Bytes => (byte[])this.store.Clone();

		/// <summary>The bytes of the buffer are these again (a checkpoint of the tests puts the game back)</summary>
		public void Restore(byte[] content) => System.Array.Copy(content, this.store, Size);

		/// <summary>
		/// The store moves from the memory into the array (after the machine is prepared): the current bytes are taken over,
		/// every access of the memory to the buffer goes to the array from now on
		/// </summary>
		public void Attach() => this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);

		/// <summary>The text of the buffer</summary>
		public override string ToString() => this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address));
	}
}
