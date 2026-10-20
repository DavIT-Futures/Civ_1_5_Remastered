using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2f4d.
	/// </summary>
	public class TextBlockRenderer : GameModule
	{
		public TextBlockRenderer(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Breaks the text at 0xba06 into lines of the width (in characters): a space after the width becomes a line break
		/// (unless a space follows), "^" becomes a space with a line break after it (over the next character); a line break
		/// is added at the end of a text that does not end with one
		/// </summary>
		public void ArrangeTextRow(short breadth)
		{
			this.processor.Journal.OpenScope($"ArrangeTextRow({breadth})");

			short column = 0;

			for (ushort place = 0xba06; (short)this.owner.Libc.TextLength(0xba06) > (short)(place - 0xba06); place++)
			{
				if (this.processor.PullGlobalByte(place) == '^')
				{
					this.processor.EmitGlobalByte(place, 0x20);
					this.processor.EmitGlobalByte((ushort)(place + 1), 0xa);
					column = -1;
				}

				if (this.processor.PullGlobalByte(place) == 0xa)
				{
					column = -1;
				}

				column++;

				if (column > breadth && this.processor.PullGlobalByte(place) == 0x20 && this.processor.PullGlobalByte((ushort)(place + 1)) != 0x20)
				{
					this.processor.EmitGlobalByte(place, 0xa);
					column = 0;
				}
			}

			if (column > 0)
			{
				this.owner.Libc.AppendText(0xba06, "\n");
			}

			this.processor.Journal.CloseScope("ArrangeTextRow");
		}

		/// <summary>
		/// Draws the text at 0xba06 in lines of at most peakBreadth * 8 pixels, from vertLoc down while the lines start at most at
		/// row 192: a line ends at the last space, line break or "^" before it gets too wide (or at a line break or "^", which also
		/// skips the character after a "^"). Returns the row below the last drawn line.
		/// </summary>
		public int RenderTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style})");

			ushort lastBreak = 0;
			ushort lineStart = 0;
			short lineBreadth = 0;
			ushort lineHeight = this.owner.VideoCard.ObtainTextHeight(this.processor.PullGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)));

			for (ushort place = 0; (short)this.owner.Libc.TextLength(0xba06) > (short)place; place++)
			{
				byte chr = this.processor.PullGlobalByte((ushort)(0xba06 + place));

				lineBreadth += (short)this.owner.VideoCard.ObtainCharWidth(this.processor.PullGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)), chr);

				if (chr == 0x20 || chr == 0xa || chr == 0x5e)
				{
					lastBreak = place;
				}

				if (chr != 0xa && chr != 0x5e && peakBreadth * 8 >= lineBreadth)
					continue;

				// The line ends at the last break: draw it alone
				ushort breakAddr = (ushort)(0xba06 + lastBreak);
				byte broken = this.processor.PullGlobalByte(breakAddr);

				this.processor.EmitGlobalByte(breakAddr, 0);
				this.owner.TextPainter.RenderTextToScreen0((ushort)(0xba06 + lineStart), horizLoc, vertLoc, style);
				this.processor.EmitGlobalByte(breakAddr, broken);

				vertLoc += (short)lineHeight;
				lineBreadth = 0;
				lineStart = (ushort)(lastBreak + 1);

				if (this.processor.PullGlobalByte((ushort)(0xba06 + place)) == 0x5e)
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
				this.owner.TextPainter.RenderTextToScreen0((ushort)(0xba06 + lineStart), horizLoc, vertLoc, style);

				vertLoc += (short)lineHeight;
			}

			this.processor.RegAcc.Value16 = (ushort)vertLoc;
			this.processor.Journal.CloseScope("RenderTextChunk");

			return vertLoc;
		}

		/// <summary>
		/// Appends the text of the topic charsAddr from the text file (lines after the "*TOPIC" line up to the next "*" line,
		/// joined with spaces, without trailing spaces and '^') to 0xba06. The file starts with an index of 256 words, by a
		/// hash of the topic (the low byte of the sum of character * position): a word above 0x1ff (not "00") is the offset of the
		/// topic line, a wrong topic tries the next hash. Without an index entry, the topic is searched from 0x200 and its offset
		/// written into the index.
		/// </summary>
		/// <returns>The number of lines of the text, 0xffff when the topic is missing, also in AX</returns>
		/// <remarks>
		/// The index is written at ((hash >> (5 + hash)) * 2, not at the place it is read from ((hash >> 5) + hash) * 2: an
		/// operator precedence mistake of the translation, kept (see MIGRATION-BACKLOG.md).
		/// </remarks>
		public ushort ImportTextFromFile(ushort docnameAddr, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"ImportTextFromFile(0x{docnameAddr:x4}, 0x{charsAddr:x4})");

			// The line and the index entry are in the stack frame, like in the original; the final values of the locals stay there
			this.processor.EnterFrame(0x110);
			this.processor.PreserveSource();

			ushort line = (ushort)(this.processor.RegFrame.Value16 - 0x100);
			ushort entry = (ushort)(this.processor.RegFrame.Value16 - 0x104);

			ushort hash = 0;
			ushort charIdx = 0;

			while (true)
			{
				if ((short)this.owner.Libc.TextLength(charsAddr) <= (short)charIdx)
					break;

				hash += (ushort)((sbyte)this.processor.PullGlobalByte((ushort)(charIdx + charsAddr)) * (short)charIdx);
				charIdx++;
			}

			// The high byte of the hash is cleared (it is written as a byte in the original)
			hash &= 0xff;

			bool indexed = false;
			ushort handle;

			while (true)
			{
				this.owner.Libc.OpenStream(docnameAddr, "rb");

				handle = this.processor.RegAcc.Value16;

				this.owner.Libc.SeekStream((short)handle, (((int)(short)hash >> 5) + hash) << 1, 0);

				this.owner.Libc.ReadStream(entry, 2, 1, (short)handle);

				this.owner.Libc.CloseStream((short)handle);

				this.owner.Libc.OpenStream(docnameAddr, "rt");

				handle = this.processor.RegAcc.Value16;

				ushort offset = this.processor.PullGlobalWord(entry);

				if (offset <= 0x1ff || offset == 0x3030)
					break;

				this.owner.Libc.SeekStream((short)handle, offset, 0);

				this.owner.Libc.ScanStream((short)handle, "%[^\n]\n", line);

				this.owner.Libc.CompareTextNoCase(line, charsAddr);

				if (this.processor.RegAcc.Value16 == 0)
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
					this.owner.Libc.TellStream((short)handle);

					this.processor.EmitGlobalWord(entry, this.processor.RegAcc.Value16);

					this.owner.Libc.ScanStream((short)handle, "%[^\n]\n", line);

					scanned = this.processor.RegAcc.Value16;

					this.owner.Libc.CompareTextNoCase(line, charsAddr);
				}
				while (this.processor.RegAcc.Value16 != 0 && scanned != 0xffff);
			}

			bool found = scanned != 0xffff;

			if (found)
			{
				do
				{
					this.owner.Libc.ScanStream((short)handle, "%[^\n]\n", line);
				}
				while (this.processor.PullGlobalByte(line) == '*');

				do
				{
					this.owner.Libc.AppendText(0xba06, line);

					this.owner.Libc.AppendText(0xba06, " ");

					this.owner.Libc.ScanStream((short)handle, "%[^\n]\n", line);

					lines++;
				}
				while (this.processor.PullGlobalByte(line) != '*');

				// Let's try to trim the dialog text, and fix the bug with unecessary lines in dialogs
				string feed = this.processor.PullText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, 0xba06));
				feed = feed.TrimEnd(new char[] { ' ', '^' });
				this.processor.EmitText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, 0xba06), feed, feed.Length);
			}

			this.owner.Libc.CloseStream((short)handle);

			if (!indexed && found)
			{
				this.owner.Libc.OpenStream(docnameAddr, "r+b");

				handle = this.processor.RegAcc.Value16;

				this.owner.Libc.SeekStream((short)handle, ((int)(short)hash >> (5 + hash)) << 1, 0);

				this.owner.Libc.WriteStream(entry, 2, 1, (short)handle);

				this.owner.Libc.CloseStream((short)handle);
			}

			this.processor.EmitLocalWord(0x102, hash);
			this.processor.EmitLocalWord(0x106, charIdx);
			this.processor.EmitLocalWord(0x108, (ushort)(found ? 1 : 0));
			this.processor.EmitLocalWord(0x10a, scanned);
			this.processor.EmitLocalWord(0x10c, lines);
			this.processor.EmitLocalWord(0x10e, (ushort)(indexed ? 1 : 0));
			this.processor.EmitLocalWord(0x110, handle);

			this.processor.RegAcc.Value16 = found ? lines : (ushort)0xffff;

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportTextFromFile");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayTextDialog(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayTextDialog(0x{charsAddr:x4})");

			// function body
			ImportTextFromFile(0x30a5, charsAddr);

			ReflowText();

			ArrangeTextRow(0x50);

			// Far return
			this.processor.Journal.CloseScope("DisplayTextDialog");
		}

		/// <summary>
		/// Replaces in the text at 0xba06 every occurrence of the 5 codes (texts pointed to by 0x30ae + 2 * code) by their
		/// texts (0x30b8 + 2 * code), one code after the other. The rest of the text waits in a buffer in the stack frame,
		/// like in the original.
		/// </summary>
		public void ReflowText()
		{
			this.processor.Journal.OpenScope("ReflowText()");

			this.processor.EnterFrame(0x204);

			ushort rest = (ushort)(this.processor.RegFrame.Value16 - 0x202);

			for (int codeIdx = 0; codeIdx < 5; codeIdx++)
			{
				ushort code = this.processor.PullGlobalWord((ushort)((codeIdx << 1) + 0x30ae));
				ushort replacement = this.processor.PullGlobalWord((ushort)((codeIdx << 1) + 0x30b8));

				while (true)
				{
					this.owner.Libc.FindSubtext(0xba06, code);

					ushort found = this.processor.RegAcc.Value16;

					if (found == 0)
						break;

					this.owner.Libc.CopyText(rest, found);
					this.processor.EmitGlobalByte(found, 0);
					this.owner.Libc.AppendText(0xba06, replacement);
					this.owner.Libc.AppendText(0xba06, (ushort)(rest + this.owner.Libc.TextLength(code)));
				}
			}

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ReflowText");
		}

		/// <summary>
		/// Shortens the text until it fits the width: the last character goes, and the one before becomes a dot (unless the
		/// character two before the end is a space)
		/// </summary>
		public void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth)
		{
			this.processor.Journal.OpenScope($"ShrinkTextToWidth(0x{charsAddr:x4}, {peakBreadth})");

			while ((short)this.owner.TextPainter.ObtainTextWidth(charsAddr) > (short)peakBreadth)
			{
				if (this.processor.PullGlobalByte((ushort)(charsAddr + this.owner.Libc.TextLength(charsAddr) - 3)) != 0x20)
				{
					this.processor.EmitGlobalByte((ushort)(charsAddr + this.owner.Libc.TextLength(charsAddr) - 2), 0x2e);
				}

				this.processor.EmitGlobalByte((ushort)(charsAddr + this.owner.Libc.TextLength(charsAddr) - 1), 0x0);
			}

			this.processor.Journal.CloseScope("ShrinkTextToWidth");
		}
	}
}
