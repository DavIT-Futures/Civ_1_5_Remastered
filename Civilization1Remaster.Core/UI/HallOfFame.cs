using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HallOfFame
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public HallOfFame(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Empties the 6 entries of the Hall of Fame (records of 0x2e bytes at 0x3772:37d2, the first word -1)
		/// </summary>
		public void WipeHallOfFame()
		{
			this.processor.Journal.OpenScope("WipeHallOfFame()");

			this.processor.SegExtra.Value16 = 0x3772;

			for (int entryIdx = 0; entryIdx < 6; entryIdx++)
			{
				this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)((0x2e * entryIdx) + 0x37d2), 0xffff);
			}

			this.processor.Journal.CloseScope("WipeHallOfFame");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ImportHallOfFame()
		{
			this.processor.Journal.OpenScope("ImportHallOfFame()");

			// function body
			this.processor.EnterFrame(0x2);

			WipeHallOfFame();

			this.owner.Libc.OpenHandle("fame.dta", 0x8102, 0x80);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.Libc.ReadHandle((short)this.processor.PullLocalWord(0x2), 
				0xba06, 0x114);

			this.owner.Libc.MoveFarData(
				this.processor.SegData.Value16,
				0xba06,
				0x3772,
				0x37d2,
				0x114);

			this.owner.Libc.CloseHandle((short)this.processor.PullLocalWord(0x2));

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportHallOfFame");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void StoreHallOfFame()
		{
			this.processor.Journal.OpenScope("StoreHallOfFame()");

			// function body
			this.processor.EnterFrame(0x2);

			this.owner.Libc.OpenHandle("fame.dta", 0x8302, 0x80);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.Libc.MoveFarData(
				0x3772,
				0x37d2,
				this.processor.SegData.Value16,
				0xba06,
				0x114);

			this.owner.Libc.WriteHandle((short)this.processor.PullLocalWord(0x2),
				0xba06, 0x114);
			
			this.owner.Libc.CloseHandle((short)this.processor.PullLocalWord(0x2));

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("StoreHallOfFame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort DisplayHallOfFame(ushort arg1)
		{
			this.processor.Journal.OpenScope($"DisplayHallOfFame({arg1})");

			// function body
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33be, 160, 16, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33cb, 160, 24, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 80, 32, 240, 32, 14);

			this.processor.EmitLocalWord(0x6, 0x28);
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L0160;

		L0151:
			this.processor.CompareWord(arg1, 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L01d1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L01d1;

		L015d:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L0160:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0174;
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0151;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0151;

		L0174:
			this.processor.CompareWord(arg1, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L01ad;

			this.owner.TextPainter.RenderTextToScreen0(0x346a, 252, 189, 0);

			this.owner.Dialogs.RenderBox(236, 187, 60, 10, 11);

		L01ad:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(arg1, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L01be;
			goto L050b;

		L01be:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x63);
			if (this.processor.FlagSet.IsEqual) goto L01cb;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x43);
			if (this.processor.FlagSet.IsEqual) goto L01cb;
			goto L050b;

		L01cb:
			this.processor.RegAcc.Value16 = 0x1;
			goto L050d;

		L01d1:
			this.processor.EmitLocalWord(0x4, 0x8);
			this.processor.ScaleIndexToBase(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x37d2));
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c), this.processor.RegSource.Value16);
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0220;

			this.owner.Libc.CopyText(0xba06, " ---");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x6), 0);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x1a));
			goto L015d;

		L0220:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x8) + 1), 10));

			this.owner.Libc.AppendText(0xba06, ". ");

			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x8));

			this.owner.Libc.MoveFarData(
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37e0),
				this.processor.SegData.Value16,
				(ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)),
				16);

			this.owner.Libc.AppendText(0xba06, ", ");

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4));
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x33a2)));

			this.owner.Libc.AppendText(0xba06, " of the ");

			this.owner.Libc.MoveFarData(
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37f0),
				this.processor.SegData.Value16,
				(ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)),
				16);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " to ");

			this.processor.SegExtra.Value16 = 0x3772; // segment
										
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(Math.Abs((short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d8))), 10));

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d8)), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0343;
			this.processor.RegAcc.Value16 = 0x33f1;
			goto L0346;

		L0343:
			this.processor.RegAcc.Value16 = 0x33f6;

		L0346:
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.ObtainTextWidth(0xba06);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x140);
			if (this.processor.FlagSet.IsLess) goto L036f;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L036f:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6),
				0);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));

			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			
			this.owner.Helpers.PopulationToText(this.processor.PullInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37de)));

			this.owner.Libc.AppendText(0xba06, ", Score: ");

			this.processor.SegExtra.Value16 = 0x3772; // segment

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d2)), 10));

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0416;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L03fc;
			goto L04f3;

		L03fc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0404;
			goto L04f9;

		L0404:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L040c;
			goto L04ff;

		L040c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0414;
			goto L0505;

		L0414:
			goto L0426;

		L0416:
			this.processor.RegAcc.Value16 = 0x3412;

		L0419:
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L0426:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xc);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x6),
				7);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));

			this.owner.Libc.CopyText(0xba06, "    --- CIVILIZATION RATING: ");

			this.processor.ScaleIndex(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((((short)this.processor.PullUInt16(0x3772, 
					(ushort)(this.processor.RegDest.Value16 + 0x37d4)) + 1) *
					(short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegDest.Value16 + 0x37d2))) / 0x32), 10));

			this.owner.Libc.AppendText(0xba06, "% ---");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x6),
				12);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x2));
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L04c6;
			goto L015d;

		L04c6:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4) - 4,
				this.processor.PullLocalSignedWord(0x6) - 28,
				296, 26, 15, 14);

			goto L015d;

		L04f3:
			this.processor.RegAcc.Value16 = 0x341e;
			goto L0419;

		L04f9:
			this.processor.RegAcc.Value16 = 0x3429;
			goto L0419;

		L04ff:
			this.processor.RegAcc.Value16 = 0x3433;
			goto L0419;

		L0505:
			this.processor.RegAcc.Value16 = 0x343b;
			goto L0419;

		L050b:
			this.processor.RegAcc.Value16 = 0;

		L050d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayHallOfFame");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void AttachHallOfFameRecord()
		{
			this.processor.Journal.OpenScope("AttachHallOfFameRecord()");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x2, 0x0);

		L051f:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L061a;

		L0527:
			this.processor.RegAcc.Value16 = 0x2e;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x6));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x37d2);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x3772);
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x37a4);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, 0x3772);

			this.owner.Libc.MoveFarData(
				this.processor.PullLocalWord(0xc),
				this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x8),
				this.processor.PullLocalWord(0xa),
				0x2e);

			this.processor.DecrementLocalWord(0x6);

		L0562:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0527;
			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xb77e);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d2), this.processor.RegAcc.Value16);
			
			this.owner.Libc.MoveFarData(
				this.processor.SegData.Value16,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2)),
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37e0),
				0xe);

			this.owner.Libc.MoveFarData(
				this.processor.SegData.Value16,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1992)),
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37f0),
				0x10);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdc6a);
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d6), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd764);
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37da), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd766);
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37dc), this.processor.RegAcc.Value16);
			this.processor.EmitInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d8), owner.World.Season);
			
			this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37de), this.processor.RegAcc.Value16);

			DisplayHallOfFame(this.processor.PullLocalWord(0x4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0612;

			WipeHallOfFame();

			goto L051f;

		L0612:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			goto L0658;

		L0617:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L061a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0655;

			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x4));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d2)));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb77e));
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L064d;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0617;

		L064d:
			this.processor.EmitLocalWord(0x6, 0x5);
			goto L0562;

		L0655:
			this.processor.RegAcc.Value16 = 0xffff;

		L0658:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("AttachHallOfFameRecord");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void DisplayGameResultsScreen(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayGameResultsScreen({arg1}, {arg2})");

			// function body
			this.processor.EnterFrame(0xa);

			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06a6;
			this.processor.RegCount.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = arg2;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			arg1 = 0xffff;
			this.processor.EmitLocalWord(0x4, 0x1);

		L0686:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L069d;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			arg1 = this.processor.RegAcc.Value16;

		L069d:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x14);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0686;

		L06a6:
			this.processor.CompareWord(arg1, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L06af;
			goto L09a8;

		L06af:
			this.processor.CompareWord(arg1, 0x13);
			if (this.processor.FlagSet.IsLessOrEqual) goto L06ba;

			arg1 = 0x13;

		L06ba:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.PerformSound(0x22, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.owner.UnitOps.RenderBusyIndicator();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xc8, 0x8c, 0x28, 0x3c,
				this.processor.PullGlobalWord(0xaa),
				10, 10);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xf0, 0x8c, 0x28, 0x3c,
				this.processor.PullGlobalWord(0xaa),
				0x10e, 10);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x7);

			this.owner.Helpers.FloodBoxWithMotif(80, 4, 160, 27);

			this.owner.Dialogs.RenderShadowBox(80, 4, 159, 26, 11, 8);

			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0788;
			goto L0814;

		L0788:
			this.owner.Libc.MoveFarData(
				0x36d4,
				0x1f4,
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 6, 15);

			this.owner.Libc.MoveFarData(
				0x36d4,
				0x20d,
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 14, 15);

			this.owner.Libc.MoveFarData(
				0x36d4,
				(ushort)((0x13 - arg1) * 0x19),
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.processor.RegAcc.Value16 = 0x3471;
			goto L08cd;

		L0814:
			this.owner.Libc.MoveFarData(
				0x36d4,
				0x226,
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 6, 15);

			this.owner.Libc.MoveFarData(
				0x36d4,
				0x23f,
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			this.owner.Libc.AppendText(0xba06, "% exceeds even");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 14, 15);

			this.owner.Libc.MoveFarData(
				0x36d4,
				(ushort)((0x13 - arg1) * 0x19),
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.processor.RegAcc.Value16 = 0x3482;

		L08cd:
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 22, 15);

			this.processor.EmitLocalWord(0x4, 0x0);
			goto L091d;

		L08f9:
			this.processor.RegAcc.Value16 = 0x7;

		L08fc:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160,
				-((this.processor.PullLocalSignedWord(0x4) * 8) - 184), this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x4);

		L091d:
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0957;

			this.owner.Libc.MoveFarData(
				0x36d4,
				(ushort)((0x13 - this.processor.PullLocalWord(0x4)) * 0x19),
				this.processor.SegData.Value16,
				0xba06,
				0x20);

			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L08f9;
			this.processor.RegAcc.Value16 = 0;
			goto L08fc;

		L0957:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				80, 183 - (short)arg1 * 8,
				160, 8, 15, 14);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Services.PerformSound(1, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

		L09a8:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("DisplayGameResultsScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayTopCities()
		{
			this.processor.Journal.OpenScope("DisplayTopCities()");

			// function body
			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x14, 0x0);

		L09b9:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x5);
			if (this.processor.FlagSet.IsLess) goto L09b9;
			this.processor.EmitLocalWord(0x12, 0x0);
			goto L0a0c;

		L09d9:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x1e), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xe), this.processor.RegAcc.Value16);
			this.processor.DecrementLocalWord(0x16);

		L09ef:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L09d9;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);

		L0a09:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));

		L0a0c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a8e;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x12));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x12)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0a09;

			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x12), -1);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x12));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x12)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x14, 0x1);

		L0a4e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x14)], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0a60;
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0xa));

		L0a60:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0a4e;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0a73;

		L0a70:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));

		L0a73:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a09;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a70;
			this.processor.EmitLocalWord(0x16, 0x3);
			goto L09ef;

		L0a8e:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

			this.owner.TextPainter.RenderTextWithShadow(0x3486, 80, 12, 15);

			this.processor.EmitLocalWord(0x4, 0x20);
			this.processor.EmitLocalWord(0x14, 0x0);

		L0ad6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0ae4;
			goto L0c57;

		L0ae4:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10));
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x14) + 1), 10));

			this.owner.Libc.AppendText(0xba06, ". ");

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x12));

			this.owner.Libc.AppendText(0xba06, " (");

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x12));
			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x12)].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160,
				this.processor.PullLocalSignedWord(0x4) + 3,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((this.owner.World.Towns[this.processor.PullLocalWord(0x12)].ParticipantIdx) << 1) + 0x1946)));

			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x12), -1);

			this.processor.EmitLocalWord(0x2, 0x30);

			this.owner.CityOps.RenderCitizenIcons(this.processor.PullLocalSignedWord(0x12),
				48, (short)(this.processor.PullLocalWord(0x4) + 10),
				this.processor.PullGlobalWord(0xe8b8), 160);

			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Towns[this.processor.PullLocalWord(0x12)].TrueExtent * 8, 
				0, 160);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0x16, 0x1);

		L0bec:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x16)], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c1e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x125);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c1a;

			this.owner.CityOps.RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0x16) + 24),
				this.processor.PullLocalSignedWord(0x2),
				(short)(this.processor.PullLocalSignedWord(0x4) + 12));

		L0c1a:
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), 0x13));

		L0c1e:
			this.processor.EmitLocalWord(0x16, this.processor.IncrementWord(this.processor.PullLocalWord(0x16)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0bec;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x12)].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Dialogs.RenderBox(8, this.processor.PullLocalSignedWord(0x4),
				303, 25, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x20));

		L0c57:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c63;
			goto L0ad6;

		L0c63:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTopCities");
		}
	}
}
