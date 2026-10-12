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
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		public void ArrangeTextRow(short arg1)
		{
			this.processor.Journal.OpenScope($"ArrangeTextRow({arg1})");

			// function body
			this.processor.EnterFrame(0x4);

			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L005d;

		L0012:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x5e);
			if (this.processor.FlagSet.IsNotEqual) goto L002b;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x20);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba07), 0xa);
			this.processor.EmitLocalWord(0x2, 0xffff);

		L002b:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0037;
			this.processor.EmitLocalWord(0x2, 0xffff);

		L0037:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			
			if (this.processor.PullLocalSignedWord(0x2) <= arg1)
				goto L005a;

			if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)) != 0x20)
				goto L005a;

			if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba07)) == 0x20)
				goto L005a;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0xa);
			this.processor.EmitLocalWord(0x2, 0x0);

		L005a:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L005d:
			this.owner.Libc.TextLength(0xba06);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsGreater) goto L0012;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0084;

			this.owner.Libc.AppendText(0xba06, "\n");

		L0084:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("ArrangeTextRow");
		}

		/// <summary>
		/// Draws a text in a block of maximum width
		/// </summary>
		/// <param name="maxWidth"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public int RenderTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainTextHeight(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)));

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L00b5;

		L00b2:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L00b5:
			this.owner.Libc.TextLength(0xba06);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			if (this.processor.FlagSet.IsGreater) goto L00c9;
			goto L017b;

		L00c9:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06));

			this.owner.VideoCard.ObtainCharWidth(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)), this.processor.RegAcc.LowByte);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L00fc;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L00fc;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x5e);
			if (this.processor.FlagSet.IsNotEqual) goto L0101;

		L00fc:
			this.processor.RegAcc.Value16 = this.processor.RegBase.Value16;
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L0101:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L011b;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x5e);
			if (this.processor.FlagSet.IsEqual) goto L011b;

			if (peakBreadth * 8 >= this.processor.PullLocalSignedWord(0x4))
				goto L00b2;

		L011b:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(0xba06 + this.processor.PullLocalWord(0xc)),
				horizLoc, vertLoc, style);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), this.processor.RegAcc.LowByte);

			vertLoc += this.processor.PullLocalSignedWord(0xa);

			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.Value16 = this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x5e);
			if (this.processor.FlagSet.IsNotEqual) goto L016b;
			this.processor.IncrementLocalWord(0xc);

		L016b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			if (vertLoc <= 192)
				goto L00b2;

		L017b:
			if (this.processor.PullLocalSignedWord(0x4) <= 0)
				goto L01a6;
			
			if (vertLoc > 192)
				goto L01a6;

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(0xba06 + this.processor.PullLocalWord(0xc)),
				horizLoc, vertLoc, style);

			vertLoc += this.processor.PullLocalSignedWord(0xa);

		L01a6:
			this.processor.RegAcc.Value16 = (ushort)((short)vertLoc);
			this.processor.LeaveFrame();
			// Far return
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
		/// ?
		/// </summary>
		public void ReflowText()
		{
			this.processor.Journal.OpenScope("ReflowText()");

			// function body
			this.processor.EnterFrame(0x204);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x204, 0x0);

		L0480:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.RegSource.Value16 <<= 1;
			this.owner.Libc.FindSubtext(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x30ae)));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L04e0;

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x202), this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x30b8)));

			this.owner.Libc.TextLength(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x30ae)));

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.owner.Libc.AppendText(0xba06, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x202));

		L04e0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0480;

			this.processor.IncrementLocalWord(0x204);

			this.processor.CompareWord(this.processor.PullLocalWord(0x204), 0x5);
			if (this.processor.FlagSet.IsLess) goto L0480;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
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
