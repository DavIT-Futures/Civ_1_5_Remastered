using System;

namespace Civilization1Remaster.Core.Shared.State
{
	public sealed class TextBuffer
	{
		public const ushort Address = 0xba06;

		public const int Size = 0x300;

		public const ushort StackTextAddress = 0xe8c0;

		private readonly IGameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public TextBuffer(IGameEngine game)
		{
			this.game = game;
		}

		public ushort Length
		{
			get
			{
				int length = End();

				if (length < 0)
				{
					return this.game.Runtime.TextLength(Address);
				}

				return (ushort)length;
			}
		}

		public byte[] Bytes => (byte[])this.store.Clone();

		public byte this[int index]
		{
			get
			{
				return this.attached && index >= 0 && index < Size ? this.store[index] : this.game.Processor.PullGlobalByte((ushort)(Address + index));
			}

			set
			{
				if (this.attached && index >= 0 && index < Size)
				{
					this.store[index] = value;
				}
				else
				{
					this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
				}
			}
		}

		public ushort Set(string text) => Write(0, text);

		public ushort Set(ushort textAddress) => Write(0, TextAt(textAddress));

		public ushort Append(string text) => Write(End(), text);

		public ushort Append(ushort textAddress) => Append(TextAt(textAddress));

		public ushort Uppercase() => Set(ToString().ToUpper());

		public void Clear() => this[0] = 0;

		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, Address), this.store);
			this.attached = true;
		}

		public override string ToString() => TextFrom(0);

		public string TextFrom(int index)
		{
			int end = this.attached && index >= 0 && index < Size ? Array.IndexOf(this.store, (byte)0, index) : -1;

			if (end < 0)
			{
				return this.game.Processor.PullText(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, (ushort)(Address + index)));
			}

			char[] letters = new char[end - index];

			for (int position = index; position < end; position++)
			{
				letters[position - index] = (char)this.store[position];
			}

			return new string(letters);
		}

		private int End() => this.attached ? Array.IndexOf(this.store, (byte)0) : -1;

		private string TextAt(ushort textAddress) => this.game.Processor.PullText(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, textAddress));

		private ushort Write(int place, string text)
		{
			if (!this.attached || place < 0 || place + text.Length >= Size)
			{
				return place == 0 ? this.game.Runtime.CopyText(Address, text) : this.game.Runtime.AppendText(Address, text);
			}

			for (int index = 0; index < text.Length; index++)
			{
				this.store[place + index] = (byte)text[index];
			}

			this.store[place + text.Length] = 0;
			return Address;
		}
	}
}
