using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2f4d.
	/// </summary>
	public class TextBlockRenderer : GameModule
	{
		private const ushort MacroCodesTableAddress = 0x30ae;
		private const ushort MacroReplacementsTableAddress = 0x30b8;


		public TextBlockRenderer(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Breaks the text at TextBuffer.Address into lines of the width (in characters): a space after the width becomes a line break
		/// (unless a space follows), "^" becomes a space with a line break after it (over the next character); a line break
		/// is added at the end of a text that does not end with one
		/// </summary>
		public void ArrangeTextRow(short breadth)
		{
			this.processor.Journal.OpenScope($"ArrangeTextRow({breadth})");

			short column = 0;

			// The length is read again every round, like in the original: a '^' at the end overwrites the 0 after it with a
			// line feed, so the text goes on into what follows in the buffer
			for (int index = 0; (short)this.owner.Words.Length > (short)index; index++)
			{
				if (this.owner.Words[index] == '^')
				{
					this.owner.Words[index] = 0x20;
					this.owner.Words[index + 1] = 0xa;
					column = -1;
				}

				if (this.owner.Words[index] == 0xa)
				{
					column = -1;
				}

				column++;

				if (column > breadth && this.owner.Words[index] == 0x20 && this.owner.Words[index + 1] != 0x20)
				{
					this.owner.Words[index] = 0xa;
					column = 0;
				}
			}

			if (column > 0)
			{
				this.owner.Words.Append("\n");
			}

			this.processor.Journal.CloseScope("ArrangeTextRow");
		}

		/// <summary>
		/// Draws the text at TextBuffer.Address in lines of at most peakBreadth * 8 pixels, from vertLoc down while the lines start at most at
		/// row 192: a line ends at the last space, line break or "^" before it gets too wide (or at a line break or "^", which also
		/// skips the character after a "^"). Returns the row below the last drawn line.
		/// </summary>
		public int RenderTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style})");

			ushort lastBreak = 0;
			ushort lineStart = 0;
			short lineBreadth = 0;
			ushort lineHeight = this.owner.VideoCard.ObtainTextHeight(DrawingBox.TypefaceIdx);

			for (ushort place = 0; (short)this.owner.Words.Length > (short)place; place++)
			{
				byte chr = this.owner.Words[place];

				lineBreadth += (short)this.owner.VideoCard.ObtainCharWidth(DrawingBox.TypefaceIdx, chr);

				if (chr == 0x20 || chr == 0xa || chr == 0x5e)
				{
					lastBreak = place;
				}

				if (chr != 0xa && chr != 0x5e && peakBreadth * 8 >= lineBreadth)
					continue;

				// The line ends at the last break: draw it alone
				byte broken = this.owner.Words[lastBreak];

				this.owner.Words[lastBreak] = 0;
				this.owner.TextPainter.RenderTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);
				this.owner.Words[lastBreak] = broken;

				vertLoc += (short)lineHeight;
				lineBreadth = 0;
				lineStart = (ushort)(lastBreak + 1);

				if (this.owner.Words[place] == 0x5e)
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
				this.owner.TextPainter.RenderTextToScreen0((ushort)(TextBuffer.Address + lineStart), horizLoc, vertLoc, style);

				vertLoc += (short)lineHeight;
			}

			this.processor.Journal.CloseScope("RenderTextChunk");

			return vertLoc;
		}

		/// <summary>
		/// Appends the text of the topic charsAddr from the text file (lines after the "*TOPIC" line up to the next "*" line,
		/// joined with spaces, without trailing spaces and '^') to TextBuffer.Address. The file starts with an index of 256 words, by a
		/// hash of the topic (the low byte of the sum of character * position): a word above 0x1ff (not "00") is the offset of the
		/// topic line, a wrong topic tries the next hash. Without an index entry, the topic is searched from 0x200 and its offset
		/// written into the index.
		/// </summary>
		/// <returns>The number of lines of the text, 0xffff when the topic is missing, also in AX</returns>
		/// <remarks>
		/// The index is written at ((hash >> (5 + hash)) * 2, not at the place it is read from ((hash >> 5) + hash) * 2: an
		/// operator precedence mistake of the translation, kept (see MIGRATION-BACKLOG.md).
		/// </remarks>
		public ushort ImportTextFromFile(ushort docnameAddr, ushort charsAddr) =>
			ImportTextFromFile(docnameAddr, this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr)), charsAddr);

		/// <summary>ImportTextFromFile of the topic in a C# string</summary>
		public ushort ImportTextFromFile(ushort docnameAddr, string topic) => ImportTextFromFile(docnameAddr, topic, TextBuffer.StackTextAddress);

		private ushort ImportTextFromFile(ushort docnameAddr, string topic, ushort topicAddr)
		{
			this.processor.Journal.OpenScope($"ImportTextFromFile(0x{docnameAddr:x4}, 0x{topicAddr:x4})");

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
				handle = (ushort)this.owner.Libc.OpenStream(docnameAddr, "rb");

				this.owner.Libc.SeekStream((short)handle, (((int)(short)hash >> 5) + hash) << 1, 0);

				entry = this.owner.Libc.ReadStreamWord((short)handle, entry);

				this.owner.Libc.CloseStream((short)handle);

				handle = (ushort)this.owner.Libc.OpenStream(docnameAddr, "rt");

				ushort offset = entry;

				if (offset <= 0x1ff || offset == 0x3030)
					break;

				this.owner.Libc.SeekStream((short)handle, offset, 0);

				this.owner.Libc.ScanStreamLine((short)handle, out line);

				if ((ushort)this.owner.Libc.CompareTextNoCase(line, topic) == 0)
				{
					indexed = true;
					break;
				}

				hash = (ushort)((hash + 1) & 0xff);

				this.owner.Libc.CloseStream((short)handle);
			}

			ushort scanned = 0;
			ushort lines = 0;

			if (!indexed)
			{
				this.owner.Libc.SeekStream((short)handle, 0x200, 0);

				do
				{
					int streamPos = this.owner.Libc.TellStream((short)handle);

					entry = (ushort)streamPos;

					scanned = (ushort)this.owner.Libc.ScanStreamLine((short)handle, out line);
				}
				while ((ushort)this.owner.Libc.CompareTextNoCase(line, topic) != 0 && scanned != 0xffff);
			}

			bool found = scanned != 0xffff;

			if (found)
			{
				do
				{
					this.owner.Libc.ScanStreamLine((short)handle, out line);
				}
				while (line.StartsWith('*'));

				do
				{
					this.owner.Words.Append(line);

					this.owner.Words.Append(" ");

					this.owner.Libc.ScanStreamLine((short)handle, out line);

					lines++;
				}
				while (!line.StartsWith('*'));

				// Let's try to trim the dialog text, and fix the bug with unecessary lines in dialogs
				this.owner.Words.Set(this.owner.Words.ToString().TrimEnd(new char[] { ' ', '^' }));
			}

			this.owner.Libc.CloseStream((short)handle);

			if (!indexed && found)
			{
				handle = (ushort)this.owner.Libc.OpenStream(docnameAddr, "r+b");

				this.owner.Libc.SeekStream((short)handle, ((int)(short)hash >> (5 + hash)) << 1, 0);

				this.owner.Libc.WriteStreamWord(entry, (short)handle);

				this.owner.Libc.CloseStream((short)handle);
			}

			ushort result = found ? lines : (ushort)0xffff;

			this.processor.Journal.CloseScope("ImportTextFromFile");

			return result;
		}

		/// <summary>
		/// The text of the topic at the address from the text file 0x30a5 into the message buffer, its codes replaced, arranged
		/// in rows of 0x50 for a dialog
		/// </summary>
		public void DisplayTextDialog(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayTextDialog(0x{charsAddr:x4})");

			ImportTextFromFile(0x30a5, charsAddr);

			ReflowText();

			ArrangeTextRow(0x50);

			this.processor.Journal.CloseScope("DisplayTextDialog");
		}

		/// <summary>
		/// Replaces in the text at TextBuffer.Address every occurrence of the 5 codes (texts pointed to by 0x30ae + 2 * code) by their
		/// texts (0x30b8 + 2 * code), one code after the other.
		/// </summary>
		public void ReflowText()
		{
			this.processor.Journal.OpenScope("ReflowText()");

			for (int codeIdx = 0; codeIdx < 5; codeIdx++)
			{
				ushort code = this.processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroCodesTableAddress));
				ushort replacement = this.processor.PullGlobalWord((ushort)((codeIdx << 1) + MacroReplacementsTableAddress));

				while (true)
				{
					int matchOffset = this.owner.Libc.FindSubtext(TextBuffer.Address, code);

					if (matchOffset < 0)
						break;

					ushort found = (ushort)(TextBuffer.Address + matchOffset);

					ushort codeLen = this.owner.Libc.TextLength(code);
					string rest = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, (ushort)(found + codeLen)));
					this.owner.Words[found - TextBuffer.Address] = 0;
					this.owner.Words.Append(replacement);
					this.owner.Words.Append(rest);
				}
			}

			this.processor.Journal.CloseScope("ReflowText");
		}

		/// <summary>
		/// Shortens the text until it fits the width: the last character goes, and the one before becomes a dot (unless the
		/// character two before the end is a space)
		/// </summary>
		/// <summary>ShrinkTextToWidth of a text in a C# string (the original kept it on the stack): the shortened text</summary>
		public string ShrinkTextToWidth(string text, ushort peakBreadth)
		{
			this.processor.Journal.OpenScope($"ShrinkTextToWidth(0x{TextBuffer.StackTextAddress:x4}, {peakBreadth})");

			char[] letters = text.ToCharArray();
			int len = letters.Length;

			while ((short)this.owner.TextPainter.ObtainTextWidth(new string(letters, 0, len)) > (short)peakBreadth && len > 0)
			{
				if (len < 3 || letters[len - 3] != ' ')
				{
					if (len >= 2)
						letters[len - 2] = '.';
				}

				len--;
			}

			this.processor.Journal.CloseScope("ShrinkTextToWidth");

			return new string(letters, 0, len);
		}

		public void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth)
		{
			this.processor.Journal.OpenScope($"ShrinkTextToWidth(0x{charsAddr:x4}, {peakBreadth})");

			while ((short)this.owner.TextPainter.ObtainTextWidth(charsAddr) > (short)peakBreadth)
			{
				int len = this.owner.Libc.TextLength(charsAddr);

				if (this.processor.PullGlobalByte((ushort)(charsAddr + len - 3)) != 0x20)
				{
					this.processor.EmitGlobalByte((ushort)(charsAddr + len - 2), 0x2e);
				}

				this.processor.EmitGlobalByte((ushort)(charsAddr + len - 1), 0x0);
			}

			this.processor.Journal.CloseScope("ShrinkTextToWidth");
		}
	}
}
