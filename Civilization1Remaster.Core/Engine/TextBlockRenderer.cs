using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2f4d.
	/// </summary>
	public class TextBlockRenderer
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public TextBlockRenderer(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
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
		/// ?
		/// </summary>
		/// <param name="filenamePtr"></param>
		/// <param name="stringPtr"></param>
		/// <returns></returns>
		public ushort ImportTextFromFile(ushort docnameAddr, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"ImportTextFromFile(0x{docnameAddr:x4}, 0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x110);
			this.processor.PreserveSource();
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x10e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x102, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			goto L01d9;

		L01c5:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = charsAddr;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x102, this.processor.SumWord(this.processor.PullLocalWord(0x102), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x106);

		L01d9:
			this.owner.Libc.TextLength(charsAddr);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x106));
			if (this.processor.FlagSet.IsGreater) goto L01c5;
			this.processor.EmitLocalByte(0x101, 0x0);

		L01ef:
			this.owner.Libc.OpenStream(docnameAddr, "rb");

			this.processor.EmitLocalWord(0x110, this.processor.RegAcc.Value16);

			this.owner.Libc.SeekStream((short)this.processor.PullLocalWord(0x110),
				(((int)((short)this.processor.PullLocalWord(0x102)) >> 5) +
					this.processor.PullLocalWord(0x102)) << 1,
				0);

			this.owner.Libc.ReadStream((ushort)(this.processor.RegFrame.Value16 - 0x104), 2, 1, 
				(short)this.processor.PullLocalWord(0x110));
			
			this.owner.Libc.CloseStream((short)this.processor.PullLocalWord(0x110));
			
			this.owner.Libc.OpenStream(docnameAddr, "rt");

			this.processor.EmitLocalWord(0x110, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x1ff);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L02bc;
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x3030);
			if (this.processor.FlagSet.IsEqual) goto L02bc;

			this.owner.Libc.SeekStream((short)this.processor.PullLocalWord(0x110),
				this.processor.PullLocalWord(0x104), 0);

			this.owner.Libc.ScanStream((short)this.processor.PullLocalWord(0x110),
				"%[^\n]\n", (ushort)(this.processor.RegFrame.Value16 - 0x100));

			this.owner.Libc.CompareTextNoCase((ushort)(this.processor.RegFrame.Value16 - 0x100), charsAddr);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L02b6;
			goto L0392;

		L02b6:
			this.processor.EmitLocalWord(0x10e, 0x1);

		L02bc:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x10a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0323;

			this.owner.Libc.SeekStream((short)this.processor.PullLocalWord(0x110),
				0x200, (short)this.processor.RegAcc.Value16);

		L02df:
			this.owner.Libc.TellStream((short)this.processor.PullLocalWord(0x110));

			this.processor.EmitLocalWord(0x104, this.processor.RegAcc.Value16);

			this.owner.Libc.ScanStream((short)this.processor.PullLocalWord(0x110),
				"%[^\n]\n", (ushort)(this.processor.RegFrame.Value16 - 0x100));

			this.processor.EmitLocalWord(0x10a, this.processor.RegAcc.Value16);

			this.owner.Libc.CompareTextNoCase((ushort)(this.processor.RegFrame.Value16 - 0x100), charsAddr);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0323;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L02df;

		L0323:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L032d;
			goto L03af;

		L032d:
			this.owner.Libc.ScanStream((short)this.processor.PullLocalWord(0x110),
				"%[^\n]\n", (ushort)(this.processor.RegFrame.Value16 - 0x100));

			this.processor.CompareByte(this.processor.PullLocalByte(0x100), 0x2a);
			if (this.processor.FlagSet.IsEqual) goto L032d;

		L0349:
			this.owner.Libc.AppendText(0xba06, (ushort)(this.processor.RegFrame.Value16 - 0x100));

			this.owner.Libc.AppendText(0xba06, " ");

			this.owner.Libc.ScanStream((short)this.processor.PullLocalWord(0x110),
				"%[^\n]\n", (ushort)(this.processor.RegFrame.Value16 - 0x100));

			this.processor.IncrementLocalWord(0x10c);
			this.processor.CompareByte(this.processor.PullLocalByte(0x100), 0x2a);
			if (this.processor.FlagSet.IsNotEqual) goto L0349;

			// Let's try to trim the dialog text, and fix the bug with unecessary lines in dialogs
			string feed = this.processor.PullText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, 0xba06));
			feed = feed.TrimEnd(new char[] {' ', '^'});
			this.processor.EmitText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, 0xba06), feed, feed.Length);

			this.processor.EmitLocalWord(0x108, 0x1);
			goto L03b5;

		L0392:
			this.processor.EmitLocalWord(0x102, this.processor.IncrementWord(this.processor.PullLocalWord(0x102)));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x102);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x102, this.processor.RegAcc.Value16);
			
			this.owner.Libc.CloseStream((short)this.processor.PullLocalWord(0x110));
			goto L01ef;

		L03af:
			this.processor.EmitLocalWord(0x108, 0x0);

		L03b5:
			this.owner.Libc.CloseStream((short)this.processor.PullLocalWord(0x110));

			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L043a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L043a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10a), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L043a;

			this.owner.Libc.OpenStream(docnameAddr, "r+b");

			this.processor.EmitLocalWord(0x110, this.processor.RegAcc.Value16);

			this.owner.Libc.SeekStream((short)this.processor.PullLocalWord(0x110),
				((int)((short)this.processor.PullLocalWord(0x102)) >> 5 +
					this.processor.PullLocalWord(0x102)) << 1,
				0);

			this.owner.Libc.WriteStream((ushort)(this.processor.RegFrame.Value16 - 0x104), 2, 1,
				(short)this.processor.PullLocalWord(0x110));
			
			this.owner.Libc.CloseStream((short)this.processor.PullLocalWord(0x110));

		L043a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0447;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10c);
			goto L044a;

		L0447:
			this.processor.RegAcc.Value16 = 0xffff;

		L044a:
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
