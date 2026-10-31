using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2f4d.
	/// </summary>
	public class TextBlockRenderer : GameModule, ITextBlockRenderer
	{
		private const ushort MacroCodesTableAddress = 0x30ae;
		private const ushort MacroReplacementsTableAddress = 0x30b8;

		public TextBlockRenderer(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ArrangeTextRow(short breadth)
		{
			this.Processor.Journal.OpenScope($"ArrangeTextRow({breadth})");

			short column = 0;

			// The length is read again every round, like in the original: a '^' at the end overwrites the 0 after it with a
			// line feed, so the text goes on into what follows in the buffer
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

			this.Processor.Journal.CloseScope("ArrangeTextRow");
		}

		/// <inheritdoc/>
		public int RenderTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style)
		{
			this.Processor.Journal.OpenScope($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style})");

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
					continue;

				// The line ends at the last break: draw it alone
				byte broken = this.Owner.Words[lastBreak];

				this.Owner.Words[lastBreak] = 0;
				this.Owner.TextPainter.RenderTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);
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
					break;
			}

			// The rest (lineBreadth is 0 after a full page)
			if (lineBreadth > 0 && vertLoc <= 192)
			{
				this.Owner.TextPainter.RenderTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);

				vertLoc += (short)lineHeight;
			}

			this.Processor.Journal.CloseScope("RenderTextChunk");

			return vertLoc;
		}

		/// <inheritdoc/>
		public ushort ImportTextFromFile(GameText docname, GameText topic) =>
			ImportTextFromFile(docname, topic.Read(this.Processor), topic.Address);

		/// <inheritdoc/>
		public ushort ImportTextFromFile(GameText docname, string topic) => ImportTextFromFile(docname, topic, TextBuffer.StackTextAddress);

		/// <inheritdoc/>
		public void DisplayTextDialog(GameText topic)
		{
			this.Processor.Journal.OpenScope($"DisplayTextDialog(0x{topic.Address:x4})");

			ImportTextFromFile(GameTexts.FileKingTxt, topic);

			ReflowText();

			ArrangeTextRow(0x50);

			this.Processor.Journal.CloseScope("DisplayTextDialog");
		}

		/// <inheritdoc/>
		public void ReflowText()
		{
			this.Processor.Journal.OpenScope("ReflowText()");

			for (int codeIdx = 0; codeIdx < 5; codeIdx++)
			{
				ushort code = this.Processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroCodesTableAddress));
				ushort replacement = this.Processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroReplacementsTableAddress));

				while (true)
				{
					int matchOffset = this.Owner.Libc.FindSubtext(TextBuffer.Address, code);

					if (matchOffset < 0)
						break;

					ushort found = (ushort)(TextBuffer.Address + matchOffset);

					ushort codeLen = this.Owner.Libc.TextLength(code);
					string rest = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, (ushort)(found + codeLen)));
					this.Owner.Words[found - TextBuffer.Address] = 0;
					this.Owner.Words.Append(replacement);
					this.Owner.Words.Append(rest);
				}
			}

			this.Processor.Journal.CloseScope("ReflowText");
		}

		/// <inheritdoc/>
		public string ShrinkTextToWidth(string text, ushort peakBreadth)
		{
			this.Processor.Journal.OpenScope($"ShrinkTextToWidth(0x{TextBuffer.StackTextAddress:x4}, {peakBreadth})");

			char[] letters = text.ToCharArray();
			int len = letters.Length;

			while ((short)this.Owner.TextPainter.ObtainTextWidth(new string(letters, 0, len)) > (short)peakBreadth && len > 0)
			{
				if (len < 3 || letters[len - 3] != ' ')
				{
					if (len >= 2)
						letters[len - 2] = '.';
				}

				len--;
			}

			this.Processor.Journal.CloseScope("ShrinkTextToWidth");

			return new string(letters, 0, len);
		}

		public void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth)
		{
			this.Processor.Journal.OpenScope($"ShrinkTextToWidth(0x{charsAddr:x4}, {peakBreadth})");

			while ((short)this.Owner.TextPainter.ObtainTextWidth(charsAddr) > (short)peakBreadth)
			{
				int len = this.Owner.Libc.TextLength(charsAddr);

				if (this.Processor.PullGlobalByte((ushort)(charsAddr + len - 3)) != 0x20)
				{
					this.Processor.EmitGlobalByte((ushort)(charsAddr + len - 2), 0x2e);
				}

				this.Processor.EmitGlobalByte((ushort)(charsAddr + len - 1), 0x0);
			}

			this.Processor.Journal.CloseScope("ShrinkTextToWidth");
		}

		private ushort ImportTextFromFile(GameText docname, string topic, ushort topicAddr)
		{
			this.Processor.Journal.OpenScope($"ImportTextFromFile(0x{docname.Address:x4}, 0x{topicAddr:x4})");

			// The line and the index word (buffers of the file functions under SP in the original)
			string line = "";
			ushort entry = 0;

			ushort hash = 0;

			for (int charIdx = 0; charIdx < topic.Length; charIdx++)
				hash += (ushort)((sbyte)(byte)topic[charIdx] * (short)charIdx);

			// The high byte of the hash is cleared (it is written as a byte in the original)
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
					break;

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

				// Let's try to trim the dialog text, and fix the bug with unecessary lines in dialogs
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

			this.Processor.Journal.CloseScope("ImportTextFromFile");

			return result;
		}
	}
}
