namespace Civilization1Remaster.Core.Engine
{
	public class TextBlockPainter : GameComponent, ITextBlockPainter
	{
		private const ushort MacroCodesTableAddress = 0x30ae;
		private const ushort MacroReplacementsTableAddress = 0x30b8;

		public TextBlockPainter(GameEngine owner)
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

		public int DrawTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style})");

			ushort lastBreak = 0;
			ushort lineStart = 0;
			short lineBreadth = 0;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(DrawingBox.TypefaceIdx);

			for (ushort place = 0; (short)this.Owner.Words.Length > (short)place; place++)
			{
				byte chr = this.Owner.Words[place];

				lineBreadth += (short)this.Owner.VideoCard.ObtainCharWidth(DrawingBox.TypefaceIdx, chr);

				if (chr == 0x20 || chr == 0xa || chr == 0x5e)
				{
					lastBreak = place;
				}

				if (chr != 0xa && chr != 0x5e && peakBreadth * 8 >= lineBreadth)
				{
					continue;
				}

				byte broken = this.Owner.Words[lastBreak];

				this.Owner.Words[lastBreak] = 0;
				this.Owner.TextPainter.DrawTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);
				this.Owner.Words[lastBreak] = broken;

				vertLoc += (short)lineHeight;
				lineBreadth = 0;
				lineStart = (ushort)(lastBreak + 1);

				if (this.Owner.Words[place] == 0x5e)
				{
					lineStart++;
				}

				place = lastBreak;

				if (vertLoc > 192)
				{
					break;
				}
			}

			if (lineBreadth > 0 && vertLoc <= 192)
			{
				this.Owner.TextPainter.DrawTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);

				vertLoc += (short)lineHeight;
			}

			return vertLoc;
		}

		public ushort LoadTextFromFile(GameText docname, GameText topic) =>
			LoadTextFromFile(docname, topic.Read(this.Processor), topic.Address);

		public ushort LoadTextFromFile(GameText docname, string topic) => LoadTextFromFile(docname, topic, TextBuffer.StackTextAddress);

		public void ShowTextDialog(GameText topic)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayTextDialog(0x{topic.Address:x4})");

			LoadTextFromFile(GameTexts.FileKingTxt, topic);

			ReflowText();

			ArrangeTextRow(0x50);
		}

		public void ReflowText()
		{
			using var _ = this.Processor.Journal.Scope("ReflowText()");

			for (int codeIdx = 0; codeIdx < 5; codeIdx++)
			{
				ushort code = this.Processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroCodesTableAddress));
				ushort replacement = this.Processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroReplacementsTableAddress));

				while (true)
				{
					int matchOffset = this.Owner.Libc.FindSubtext(TextBuffer.Address, code);

					if (matchOffset < 0)
					{
						break;
					}

					ushort found = (ushort)(TextBuffer.Address + matchOffset);

					ushort codeLen = this.Owner.Libc.TextLength(code);
					string rest = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, (ushort)(found + codeLen)));
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
			int len = letters.Length;

			while ((short)this.Owner.TextPainter.MeasureTextWidth(new string(letters, 0, len)) > (short)peakBreadth && len > 0)
			{
				if (len < 3 || letters[len - 3] != ' ')
				{
					if (len >= 2)
					{
						letters[len - 2] = '.';
					}
				}

				len--;
			}

			return new string(letters, 0, len);
		}

		public void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth)
		{
			using var _ = this.Processor.Journal.Scope($"ShrinkTextToWidth(0x{charsAddr:x4}, {peakBreadth})");

			while ((short)this.Owner.TextPainter.MeasureTextWidth(charsAddr) > (short)peakBreadth)
			{
				int len = this.Owner.Libc.TextLength(charsAddr);

				if (this.Processor.PullGlobalByte((ushort)(charsAddr + len - 3)) != 0x20)
				{
					this.Processor.EmitGlobalByte((ushort)(charsAddr + len - 2), 0x2e);
				}

				this.Processor.EmitGlobalByte((ushort)(charsAddr + len - 1), 0x0);
			}
		}

		private ushort LoadTextFromFile(GameText docname, string topic, ushort topicAddr)
		{
			using var _ = this.Processor.Journal.Scope($"ImportTextFromFile(0x{docname.Address:x4}, 0x{topicAddr:x4})");

			string line = string.Empty;
			ushort entry = 0;

			ushort hash = 0;

			for (int charIdx = 0; charIdx < topic.Length; charIdx++)
			{
				hash += (ushort)((sbyte)(byte)topic[charIdx] * (short)charIdx);
			}

			hash &= 0xff;

			bool indexed = false;
			ushort handle;

			while (true)
			{
				handle = (ushort)this.Owner.Libc.OpenStream(docname.Read(this.Processor), "rb");

				this.Owner.Libc.SeekStream((short)handle, (((int)(short)hash >> 5) + hash) << 1, 0);

				entry = this.Owner.Libc.ReadStreamWord((short)handle, entry);

				this.Owner.Libc.CloseStream((short)handle);

				handle = (ushort)this.Owner.Libc.OpenStream(docname.Read(this.Processor), "rt");

				ushort offset = entry;

				if (offset <= 0x1ff || offset == 0x3030)
				{
					break;
				}

				this.Owner.Libc.SeekStream((short)handle, offset, 0);

				this.Owner.Libc.ScanStreamLine((short)handle, out line);

				if ((ushort)this.Owner.Libc.CompareTextNoCase(line, topic) == 0)
				{
					indexed = true;
					break;
				}

				hash = (ushort)((hash + 1) & 0xff);

				this.Owner.Libc.CloseStream((short)handle);
			}

			ushort scanned = 0;
			ushort lines = 0;

			if (!indexed)
			{
				this.Owner.Libc.SeekStream((short)handle, 0x200, 0);

				do
				{
					int streamPos = this.Owner.Libc.TellStream((short)handle);

					entry = (ushort)streamPos;

					scanned = (ushort)this.Owner.Libc.ScanStreamLine((short)handle, out line);
				}
				while ((ushort)this.Owner.Libc.CompareTextNoCase(line, topic) != 0 && scanned != 0xffff);
			}

			bool found = scanned != 0xffff;

			if (found)
			{
				do
				{
					this.Owner.Libc.ScanStreamLine((short)handle, out line);
				}
				while (line.StartsWith('*'));

				do
				{
					this.Owner.Words.Append(line);

					this.Owner.Words.Append(" ");

					this.Owner.Libc.ScanStreamLine((short)handle, out line);

					lines++;
				}
				while (!line.StartsWith('*'));

				this.Owner.Words.Set(this.Owner.Words.ToString().TrimEnd(new char[] { ' ', '^' }));
			}

			this.Owner.Libc.CloseStream((short)handle);

			if (!indexed && found)
			{
				handle = (ushort)this.Owner.Libc.OpenStream(docname.Read(this.Processor), "r+b");

				this.Owner.Libc.SeekStream((short)handle, ((int)(short)hash >> (5 + hash)) << 1, 0);

				this.Owner.Libc.WriteStreamWord(entry, (short)handle);

				this.Owner.Libc.CloseStream((short)handle);
			}

			ushort result = found ? lines : (ushort)0xffff;

			return result;
		}
	}
}
