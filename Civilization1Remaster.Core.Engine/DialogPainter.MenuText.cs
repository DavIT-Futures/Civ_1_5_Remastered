namespace Civilization1Remaster.Core.Engine
{
	public partial class DialogPainter
	{
		private sealed class MenuText
		{
			private readonly IGameEngine? game;
			private readonly ushort address;
			private readonly int bufferIndex;
			private readonly byte[]? own;

			public MenuText(IGameEngine game, ushort address)
			{
				this.game = game;
				this.address = address;
				int offset = address - TextBuffer.Address;
				this.bufferIndex = offset >= 0 && offset < TextBuffer.Size ? offset : -1;
			}

			public MenuText(string text)
			{
				this.own = new byte[text.Length + 1];

				for (int index = 0; index < text.Length; index++)
				{
					this.own[index] = (byte)text[index];
				}
			}

			public ushort Length => (ushort)From(0).Length;

			public byte this[int index]
			{
				get
				{
					if (this.own != null)
					{
						return index >= 0 && index < this.own.Length ? this.own[index] : (byte)0;
					}

					if (this.bufferIndex >= 0)
					{
						return this.game!.Words[this.bufferIndex + index];
					}

					return this.game!.Processor.PullByte(this.game.Processor.SegmentData.Word, (ushort)(this.address + index));
				}

				set
				{
					if (this.own != null)
					{
						if (index >= 0 && index < this.own.Length)
						{
							this.own[index] = value;
						}
					}
					else if (this.bufferIndex >= 0)
					{
						this.game!.Words[this.bufferIndex + index] = value;
					}
					else
					{
						this.game!.Processor.EmitByte(this.game.Processor.SegmentData.Word, (ushort)(this.address + index), value);
					}
				}
			}

			public string From(int index)
			{
				if (this.own != null)
				{
					int end = index;

					while (end >= 0 && end < this.own.Length && this.own[end] != 0)
					{
						end++;
					}

					return index < 0 || index >= this.own.Length ? string.Empty : System.Text.Encoding.Latin1.GetString(this.own, index, end - index);
				}

				return this.bufferIndex >= 0 ? this.game!.Words.TextFrom(this.bufferIndex + index) : this.game!.Processor.PullText(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, (ushort)(this.address + index)));
			}
		}
	}
}
