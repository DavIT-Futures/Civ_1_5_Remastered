using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The message buffer of the game at 0xba06 of the data segment, where the texts of the messages, dialogs and menus are
	/// built (and, now and then, a buffer of bytes of a file). B6.1: every use of the buffer goes through this class, and its
	/// 0x300 bytes (0xba06 - 0xbd05, 0xbd06 is the next variable) live in a C# array (Attach): the code that still gets the
	/// address (dialogs, menus, painters, files) reads and writes the array through the memory (CPUMemory.AttachOverlay).
	/// B6.1 part 3: the texts are built on the array (strcpy / strcat / strlen in C#, with the same bytes and AX as the C
	/// runtime; a text that would run past the buffer still goes through the C runtime and the memory after it); the
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
		private bool attached;

		public TextBuffer(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The text of the buffer is the text (strcpy); returns the address, like the C runtime (also in AX)</summary>
		public ushort Set(string text) => Write(0, text);

		/// <summary>The text of the buffer is the text at the address (read whole before the copy, like the C runtime)</summary>
		public ushort Set(ushort textAddr) => Write(0, TextAt(textAddr));

		/// <summary>The text goes to the end of the text of the buffer (strcat)</summary>
		public ushort Append(string text) => Write(End(), text);

		/// <summary>The text at the address goes to the end of the text of the buffer</summary>
		public ushort Append(ushort textAddr) => Append(TextAt(textAddr));

		/// <summary>The buffer holds the empty text (its first byte 0)</summary>
		public void Clear() => this[0] = 0;

		/// <summary>The length of the text of the buffer (strlen, also in AX)</summary>
		public ushort Length
		{
			get
			{
				int length = End();

				if (length < 0)
					return this.game.Libc.TextLength(Address);

				this.game.Processor.RegAcc.Value16 = (ushort)length;
				return (ushort)length;
			}
		}

		/// <summary>A byte of the buffer (past its end: the memory after it)</summary>
		public byte this[int index]
		{
			get => this.attached && index >= 0 && index < Size ? this.store[index] : this.game.Processor.PullGlobalByte((ushort)(Address + index));
			set
			{
				if (this.attached && index >= 0 && index < Size)
					this.store[index] = value;
				else
					this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
			}
		}

		/// <summary>
		/// The end of the text (its 0 byte), -1 when the text runs past the buffer or the buffer is not attached yet (then the
		/// C runtime works on the memory, like before)
		/// </summary>
		private int End() => this.attached ? Array.IndexOf(this.store, (byte)0) : -1;

		/// <summary>The text at the address of the data segment</summary>
		private string TextAt(ushort textAddr) => this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr));

		/// <summary>
		/// The text from the place on, closed with 0, bytes as the low bytes of the characters (like CPU.EmitText); AX and the
		/// result are the address. A text that does not fit (or a buffer not attached yet) goes through the C runtime.
		/// </summary>
		private ushort Write(int place, string text)
		{
			if (!this.attached || place < 0 || place + text.Length >= Size)
				return place == 0 ? this.game.Libc.CopyText(Address, text) : this.game.Libc.AppendText(Address, text);

			for (int idx = 0; idx < text.Length; idx++)
				this.store[place + idx] = (byte)text[idx];

			this.store[place + text.Length] = 0;
			this.game.Processor.RegAcc.Value16 = Address;
			return Address;
		}

		/// <summary>The bytes of the buffer, all of them (also after the end of the text, like the memory of the original)</summary>
		public byte[] Bytes => (byte[])this.store.Clone();

		/// <summary>
		/// The store moves from the memory into the array (after the machine is prepared): the current bytes are taken over,
		/// every access of the memory to the buffer goes to the array from now on
		/// </summary>
		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		/// <summary>The text of the buffer</summary>
		public override string ToString() => this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address));
	}
}
