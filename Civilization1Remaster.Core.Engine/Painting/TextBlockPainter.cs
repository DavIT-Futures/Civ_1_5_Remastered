namespace Civilization1Remaster.Core.Engine.Painting
{
	public class TextBlockPainter : GameComponent, ITextBlockPainter
	{
		private const ushort MacroCodesTableAddress = 0x30ae;
		private const ushort MacroReplacementsTableAddress = 0x30b8;

		public TextBlockPainter(IGameEngine owner)
			: base(owner)
		{
		}

		public void ArrangeTextRow(short breadth)
		{
			using var _ = this.Processor.Journal.Scope($"ArrangeTextRow({breadth})");

			short column = 0;

			for (int index = 0; (short)this.Owner.Words.Length > (short)index; index++)
			{
				if (this.Owner.Words[index] == '^')
				{
					this.Owner.Words[index] = 0x20;
					this.Owner.Words[index + 1] = 0xa;
					column = -1;
				}

				if (this.Owner.Words[index] == 0xa)
				{
					column = -1;
				}

				column++;

				if (column > breadth && this.Owner.Words[index] == 0x20 && this.Owner.Words[index + 1] != 0x20)
				{
					this.Owner.Words[index] = 0xa;
					column = 0;
				}
			}

			if (column > 0)
			{
				this.Owner.Words.Append("\n");
			}
		}

		public int DrawTextChunk(int peakBreadth, int horizontalLocation, int verticalLocation, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderTextChunk({peakBreadth}, {horizontalLocation}, {verticalLocation}, {style})");

			ushort lastBreak = 0;
			ushort lineStart = 0;
			short lineBreadth = 0;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(DrawingBox.TypefaceIndex);

			for (ushort place = 0; (short)this.Owner.Words.Length > (short)place; place++)
			{
				byte character = this.Owner.Words[place];

				lineBreadth += (short)this.Owner.VideoCard.ObtainCharacterWidth(DrawingBox.TypefaceIndex, character);

				if (character == 0x20 || character == 0xa || character == 0x5e)
				{
					lastBreak = place;
				}

				if (character != 0xa && character != 0x5e && peakBreadth * 8 >= lineBreadth)
				{
					continue;
				}

				byte broken = this.Owner.Words[lastBreak];

				this.Owner.Words[lastBreak] = 0;
				this.Owner.TextPainter.DrawTextToPrimaryScreen((ushort)(TextBuffer.Address + lineStart), horizontalLocation, verticalLocation, style);
				this.Owner.Words[lastBreak] = broken;

				verticalLocation += (short)lineHeight;
				lineBreadth = 0;
				lineStart = (ushort)(lastBreak + 1);

				if (this.Owner.Words[place] == 0x5e)
				{
					lineStart++;
				}

				place = lastBreak;

				if (verticalLocation > 192)
				{
					break;
				}
			}

			if (lineBreadth > 0 && verticalLocation <= 192)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen((ushort)(TextBuffer.Address + lineStart), horizontalLocation, verticalLocation, style);

				verticalLocation += (short)lineHeight;
			}

			return verticalLocation;
		}

		public ushort LoadTextFromFile(GameText documentName, GameText topic) =>
			LoadTextFromFile(documentName, topic.Read(this.Processor), topic.Address);

		public ushort LoadTextFromFile(GameText documentName, string topic) => LoadTextFromFile(documentName, topic, TextBuffer.StackTextAddress);

		public void ShowTextDialog(GameText topic)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayTextDialog(0x{topic.Address:x4})");

			LoadTextFromFile(GameTexts.FileKingText, topic);

			ReflowText();

			ArrangeTextRow(0x50);
		}

		public void ReflowText()
		{
			using var _ = this.Processor.Journal.Scope("ReflowText()");

			for (int codeIndex = 0; codeIndex < 5; codeIndex++)
			{
				ushort code = this.Processor.PullGlobalWord((ushort)((codeIndex << 1) + MacroCodesTableAddress));
				ushort replacement = this.Processor.PullGlobalWord((ushort)((codeIndex << 1) + MacroReplacementsTableAddress));

				while (true)
				{
					int matchOffset = this.Owner.Runtime.FindSubtext(TextBuffer.Address, code);

					if (matchOffset < 0)
					{
						break;
					}

					ushort found = (ushort)(TextBuffer.Address + matchOffset);

					ushort codeLength = this.Owner.Runtime.TextLength(code);
					string rest = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, (ushort)(found + codeLength)));
					this.Owner.Words[found - TextBuffer.Address] = 0;
					this.Owner.Words.Append(replacement);
					this.Owner.Words.Append(rest);
				}
			}
		}

		public string ShrinkTextToWidth(string text, ushort peakBreadth)
		{
			using var _ = this.Processor.Journal.Scope($"ShrinkTextToWidth(0x{TextBuffer.StackTextAddress:x4}, {peakBreadth})");

			char[] letters = text.ToCharArray();
			int length = letters.Length;

			while ((short)this.Owner.TextPainter.MeasureTextWidth(new string(letters, 0, length)) > (short)peakBreadth && length > 0)
			{
				if (length < 3 || letters[length - 3] != ' ')
				{
					if (length >= 2)
					{
						letters[length - 2] = '.';
					}
				}

				length--;
			}

			return new string(letters, 0, length);
		}

		public void ShrinkTextToWidth(ushort charactersAddress, ushort peakBreadth)
		{
			using var _ = this.Processor.Journal.Scope($"ShrinkTextToWidth(0x{charactersAddress:x4}, {peakBreadth})");

			while ((short)this.Owner.TextPainter.MeasureTextWidth(charactersAddress) > (short)peakBreadth)
			{
				int length = this.Owner.Runtime.TextLength(charactersAddress);

				if (this.Processor.PullGlobalByte((ushort)(charactersAddress + length - 3)) != 0x20)
				{
					this.Processor.EmitGlobalByte((ushort)(charactersAddress + length - 2), 0x2e);
				}

				this.Processor.EmitGlobalByte((ushort)(charactersAddress + length - 1), 0x0);
			}
		}

		private ushort LoadTextFromFile(GameText documentName, string topic, ushort topicAddress)
		{
			using var _ = this.Processor.Journal.Scope($"ImportTextFromFile(0x{documentName.Address:x4}, 0x{topicAddress:x4})");

			string line = string.Empty;
			ushort entry = 0;

			ushort hash = 0;

			for (int characterIndex = 0; characterIndex < topic.Length; characterIndex++)
			{
				hash += (ushort)((sbyte)(byte)topic[characterIndex] * (short)characterIndex);
			}

			hash &= 0xff;

			bool indexed = false;
			ushort handle;

			while (true)
			{
				handle = (ushort)this.Owner.Runtime.OpenStream(documentName.Read(this.Processor), "rb");

				this.Owner.Runtime.SeekStream((short)handle, (((int)(short)hash >> 5) + hash) << 1, 0);

				entry = this.Owner.Runtime.ReadStreamWord((short)handle, entry);

				this.Owner.Runtime.CloseStream((short)handle);

				handle = (ushort)this.Owner.Runtime.OpenStream(documentName.Read(this.Processor), "rt");

				ushort offset = entry;

				if (offset <= 0x1ff || offset == 0x3030)
				{
					break;
				}

				this.Owner.Runtime.SeekStream((short)handle, offset, 0);

				this.Owner.Runtime.ScanStreamLine((short)handle, out line);

				if ((ushort)this.Owner.Runtime.CompareTextNoCase(line, topic) == 0)
				{
					indexed = true;
					break;
				}

				hash = (ushort)((hash + 1) & 0xff);

				this.Owner.Runtime.CloseStream((short)handle);
			}

			ushort scanned = 0;
			ushort lines = 0;

			if (!indexed)
			{
				this.Owner.Runtime.SeekStream((short)handle, 0x200, 0);

				do
				{
					int streamPosition = this.Owner.Runtime.TellStream((short)handle);

					entry = (ushort)streamPosition;

					scanned = (ushort)this.Owner.Runtime.ScanStreamLine((short)handle, out line);
				}
				while ((ushort)this.Owner.Runtime.CompareTextNoCase(line, topic) != 0 && scanned != 0xffff);
			}

			bool found = scanned != 0xffff;

			if (found)
			{
				do
				{
					this.Owner.Runtime.ScanStreamLine((short)handle, out line);
				}
				while (line.StartsWith('*'));

				do
				{
					this.Owner.Words.Append(line);

					this.Owner.Words.Append(" ");

					this.Owner.Runtime.ScanStreamLine((short)handle, out line);

					lines++;
				}
				while (!line.StartsWith('*'));

				this.Owner.Words.Set(this.Owner.Words.ToString().TrimEnd(new char[] { ' ', '^' }));
			}

			this.Owner.Runtime.CloseStream((short)handle);

			if (!indexed && found)
			{
				handle = (ushort)this.Owner.Runtime.OpenStream(documentName.Read(this.Processor), "r+b");

				this.Owner.Runtime.SeekStream((short)handle, ((int)(short)hash >> (5 + hash)) << 1, 0);

				this.Owner.Runtime.WriteStreamWord(entry, (short)handle);

				this.Owner.Runtime.CloseStream((short)handle);
			}

			ushort result = found ? lines : (ushort)0xffff;

			return result;
		}
	}
}
