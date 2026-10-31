using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	partial class DialogDrawing
	{
		/// <summary>
		/// The text of a menu by the index of its bytes: in the message buffer its C# array (Words), elsewhere the data
		/// segment (a byte outside the buffer is the one of the memory, like at the address of the original), or the bytes of
		/// a C# string (a text the original kept on the stack; past its end 0)
		/// </summary>
		private sealed class MenuText
		{
			private readonly GameEngine? game;
			private readonly ushort address;
			private readonly int bufferIndex;
			private readonly byte[]? own;

			public MenuText(GameEngine game, ushort address)
			{
				this.game = game;
				this.address = address;
				int offset = address - TextBuffer.Address;
				this.bufferIndex = offset >= 0 && offset < TextBuffer.Size ? offset : -1;
			}

			public MenuText(string text)
			{
				this.own = new byte[text.Length + 1];

				for (int idx = 0; idx < text.Length; idx++)
					this.own[idx] = (byte)text[idx];
			}

			public byte this[int index]
			{
				get => this.own != null ? (index >= 0 && index < this.own.Length ? this.own[index] : (byte)0) :
					this.bufferIndex >= 0 ? this.game!.Words[this.bufferIndex + index] :
					this.game!.Processor.PullUInt8(this.game.Processor.SegData.Value16, (ushort)(this.address + index));
				set
				{
					if (this.own != null)
					{
						if (index >= 0 && index < this.own.Length)
							this.own[index] = value;
					}
					else if (this.bufferIndex >= 0)
						this.game!.Words[this.bufferIndex + index] = value;
					else
						this.game!.Processor.EmitUInt8(this.game.Processor.SegData.Value16, (ushort)(this.address + index), value);
				}
			}

			/// <summary>The text from the byte at the index on (to its 0 byte)</summary>
			public string From(int index)
			{
				if (this.own != null)
				{
					int end = index;

					while (end >= 0 && end < this.own.Length && this.own[end] != 0)
						end++;

					return index < 0 || index >= this.own.Length ? "" : System.Text.Encoding.Latin1.GetString(this.own, index, end - index);
				}

				return this.bufferIndex >= 0 ? this.game!.Words.TextFrom(this.bufferIndex + index) :
					this.game!.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, (ushort)(this.address + index)));
			}

			/// <summary>The length of the text (strlen)</summary>
			public ushort Length => (ushort)From(0).Length;
		}
	}
}
