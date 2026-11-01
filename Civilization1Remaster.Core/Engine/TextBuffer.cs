using System;

namespace Civilization1Remaster.Core.Engine
{
	public sealed class TextBuffer
	{
		public const ushort Address = 0xba06;

		public const int Size = 0x300;

		public const ushort StackTextAddress = 0xe8c0;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public TextBuffer(GameEngine game)
		{
			this.game = game;
		}

		public ushort Length
		{
			get
			{
				int length = End();

				if (length < 0)
					return this.game.Libc.TextLength(Address);

				return (ushort)length;
			}
		}

		public byte[] Bytes => (byte[])this.store.Clone();

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

		public ushort Set(string text) => Write(0, text);

		public ushort Set(ushort textAddr) => Write(0, TextAt(textAddr));

		public ushort Append(string text) => Write(End(), text);

		public ushort Append(ushort textAddr) => Append(TextAt(textAddr));

		public ushort Uppercase() => Set(ToString().ToUpper());

		public void Clear() => this[0] = 0;

		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		public override string ToString() => TextFrom(0);

		public string TextFrom(int index)
		{
			int end = this.attached && index >= 0 && index < Size ? Array.IndexOf(this.store, (byte)0, index) : -1;

			if (end < 0)
				return this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, (ushort)(Address + index)));

			char[] letters = new char[end - index];

			for (int idx = index; idx < end; idx++)
				letters[idx - index] = (char)this.store[idx];

			return new string(letters);
		}

		private int End() => this.attached ? Array.IndexOf(this.store, (byte)0) : -1;

		private string TextAt(ushort textAddr) => this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr));

		private ushort Write(int place, string text)
		{
			if (!this.attached || place < 0 || place + text.Length >= Size)
				return place == 0 ? this.game.Libc.CopyText(Address, text) : this.game.Libc.AppendText(Address, text);

			for (int idx = 0; idx < text.Length; idx++)
				this.store[place + idx] = (byte)text[idx];

			this.store[place + text.Length] = 0;
			return Address;
		}
	}
}
