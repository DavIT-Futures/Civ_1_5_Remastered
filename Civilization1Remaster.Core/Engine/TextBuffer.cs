using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The message buffer of the game at 0xba06 of the data segment, where the texts of the messages, dialogs and menus are
	/// built (and, now and then, a buffer of bytes of a file). B6.1: every use of the buffer goes through this class. For now
	/// it keeps the text in the memory, through the same functions of the C runtime as before, so the bytes and AX stay as
	/// they were; then the store can move into C# (with an entry in MovedData for the snapshots).
	/// </summary>
	public sealed class TextBuffer
	{
		/// <summary>The address of the buffer in the data segment</summary>
		public const ushort Address = 0xba06;

		private readonly GameEngine game;

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

		/// <summary>The text of the buffer</summary>
		public override string ToString() => this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address));
	}
}
