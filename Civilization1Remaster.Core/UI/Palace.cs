using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class Palace
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public Palace(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayPalace()
		{
			this.processor.Journal.OpenScope("DisplayPalace()");

			// function body
			this.processor.EnterFrame(0x110);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x4ade);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x100), 0xba06);

			this.owner.Startup.QueryForDisk(2, 1);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);

			ImportPalaceGraphics(0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.Helpers.HaltHueCycling();

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);

			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x151a)), 3);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4ae6, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Dialogs.DisplayMenu((ushort)(this.processor.RegFrame.Value16 - 0x100), 20, 16, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0x14, 0x10, 0x118, 0x2c,
				this.processor.PullGlobalWord(0xaa),
				0x14, 0x10);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0x4af0, 40, 16, 1);

			this.processor.EmitLocalWord(0x102, 0x0);
			this.processor.EmitLocalWord(0x108, 0x1);

		L0148:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01bc;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91c)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L016a;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd920)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L016a;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0x0);
			if (this.processor.FlagSet.IsLess) goto L01bc;

		L016a:
			this.processor.RegAcc.Value16 = 0x30;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x24);
			this.processor.EmitLocalWord(0x104, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x108), 10));

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				this.processor.PullLocalSignedWord(0x104),
				144,
				14);

			this.processor.EmitLocalWord(0x102, 0x1);

		L01bc:
			this.processor.EmitLocalWord(0x108, this.processor.IncrementWord(this.processor.PullLocalWord(0x108)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0148;
			this.processor.EmitLocalWord(0x108, 0x0);

		L01cd:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd930)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0217;

			this.owner.Libc.CopyText(0xba06, "A");

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x108);
			this.processor.EmitGlobalByte(0xba06, this.processor.SumByte(this.processor.PullGlobalByte(0xba06), this.processor.RegAcc.LowByte));

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				this.processor.PullLocalSignedWord(0x108) * 120 + 40,
				160,
				14);

			this.processor.EmitLocalWord(0x102, 0x1);

		L0217:
			this.processor.EmitLocalWord(0x108, this.processor.IncrementWord(this.processor.PullLocalWord(0x108)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x3);
			if (this.processor.FlagSet.IsLess) goto L01cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x102), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L022c;
			goto L0792;

		L022c:
			this.owner.Startup.DisplayMouseCursor();

		L0231:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0246;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0231;

		L0246:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L02a0;
			this.processor.CompareWord(this.owner.GlobDb3e, 0x9a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0292;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x30;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);

		L0264:
			this.processor.EmitLocalWord(0x10e, this.processor.RegAcc.Value16);

		L0268:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91e)), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0231;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x9);
			if (this.processor.FlagSet.IsLess) goto L0289;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91e)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0231;

		L0289:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x9);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L030e;
			goto L02e6;

		L0292:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6b;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x9);
			goto L0264;

		L02a0:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x10e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x38);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02b5;
			this.processor.EmitLocalWord(0x10e, this.processor.SubtractWord(this.processor.PullLocalWord(0x10e), 0x30));
			goto L02ba;

		L02b5:
			this.processor.EmitLocalWord(0x10e, this.processor.SubtractWord(this.processor.PullLocalWord(0x10e), 0x58));

		L02ba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x1);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02c4;
			goto L0231;

		L02c4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0xb);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0268;
			goto L0231;

		L02ce:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd920)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0307;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0307;
			this.processor.IncrementLocalWord(0x10e);

		L02e6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x4);
			if (this.processor.FlagSet.IsLess) goto L02ce;
			goto L0307;

		L02ef:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91c)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L030e;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L030e;
			this.processor.DecrementLocalWord(0x10e);

		L0307:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x4);
			if (this.processor.FlagSet.IsGreater) goto L02ef;

		L030e:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16,
					(ushort)((this.processor.PullLocalWord(0x10e) << 1) + 0xd91e)) + 1,
				1, 4);

			this.processor.EmitLocalWord(0x110, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x9);
			if (this.processor.FlagSet.IsLess) goto L035d;
			goto L0747;

		L035d:
			this.processor.EmitLocalWord(0x108, 0x1);

		L0363:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25f), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25e), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x108);
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0363;

			RenderPalaceSection(this.processor.PullLocalWord(0x10e), 0);
			
			this.processor.EmitLocalWord(0x108, 0x1);

		L0393:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25e));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb25e), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25f));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb25f), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x108);
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0393;
			this.processor.EmitGlobalWord(0x6828, 0xffff);

			ImportPalaceCastle(
				(ushort)(1 << this.processor.PullLocalByte(0x110)),
				0);

			this.processor.EmitLocalWord(0x106, 0x5c);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 16, 2, 288, 102, 9);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x4b13, 160, 3, 15);

			this.processor.EmitLocalWord(0x108, 0x1);
			goto L0587;

		L0426:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104), 10,
				80, this.processor.PullLocalSignedWord(0x106), 1);

		L0429:
			this.processor.EmitLocalWord(0x104, this.processor.SumWord(this.processor.PullLocalWord(0x104), 0x10));
			this.processor.EmitLocalWord(0x10a, 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0478;
			this.processor.EmitLocalWord(0x10a, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91c)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0472;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0478;

		L0472:
			this.processor.EmitLocalWord(0x10a, 0x0);

		L0478:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L049f;
			this.processor.EmitLocalWord(0x10a, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd920)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0499;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L049f;

		L0499:
			this.processor.EmitLocalWord(0x10a, 0x3);

		L049f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L04c1;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L04af;
			goto L05af;

		L04af:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L04b7;
			goto L0609;

		L04b7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L04bf;
			goto L062d;

		L04bf:
			goto L053e;

		L04c1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0502;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104) + this.processor.PullGlobalSignedWord(0x4b32),
				this.processor.PullInt8(this.processor.SegData.Value16,
					(ushort)((0xf * this.processor.PullLocalWord(0x110)) +
					(5 * this.processor.PullLocalWord(0x108)) + 0x4b32)) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0x108) << 2) + 0xb23e)));

		L0502:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104),
				this.processor.PullInt8(this.processor.SegData.Value16,
				(ushort)((0xf * this.processor.PullLocalWord(0x110)) +
					(0x5 * this.processor.PullLocalWord(0x108)) + 0x4b34)) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0x108) << 2) + 0xb23c)));

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b32);
			this.processor.EmitLocalWord(0x104, this.processor.SumWord(this.processor.PullLocalWord(0x104), this.processor.RegAcc.Value16));

		L053e:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x108), 10));

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				this.processor.PullLocalSignedWord(0x108) * 100 - 44,
				12,
				15);

			this.processor.IncrementLocalWord(0x108);

		L0587:
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0591;
			goto L06ae;

		L0591:
			this.processor.RegAcc.Value16 = 0x64;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x104, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05a9;
			goto L0426;

		L05a9:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104), 10,
				80, this.processor.PullLocalSignedWord(0x106), 3);

			goto L0429;

		L05af:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104),
				this.processor.PullInt8(this.processor.SegData.Value16,
					(ushort)(((0x5 * this.processor.PullLocalWord(0x108)) +
					(0xf * this.processor.PullLocalWord(0x110))) + 0x4b32)) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0x108) << 2) + 0xb23c)));

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			this.processor.EmitLocalWord(0x104, this.processor.SumWord(this.processor.PullLocalWord(0x104), this.processor.RegAcc.Value16));
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb23e)));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4b33));

		L05f3:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			this.processor.StackWord(this.processor.PullLocalWord(0x104));

		L05fa:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.UnstackWord(),
				(short)this.processor.UnstackWord(),
				this.processor.UnstackWord());
			goto L053e;

		L0609:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb23c)));
			this.processor.ScaleIndexToBase(0xf, this.processor.PullLocalWord(0x110));
			this.processor.ScaleIndexToSource(0x5, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x4b31));
			goto L05f3;

		L062d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10e), 0x7);
			if (this.processor.FlagSet.IsEqual) goto L0670;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x104),
				this.processor.PullInt8(this.processor.SegData.Value16,
					(ushort)((0xf * this.processor.PullLocalWord(0x110)) +
					(0x5 * this.processor.PullLocalWord(0x108)) + 0x4b32)) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0x108) << 2) + 0xb23c)));

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			this.processor.EmitLocalWord(0x104, this.processor.SumWord(this.processor.PullLocalWord(0x104), this.processor.RegAcc.Value16));

		L0670:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb23e)));
			this.processor.ScaleIndexToBase(0xf, this.processor.PullLocalWord(0x110));
			this.processor.ScaleIndexToSource(0x5, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x4b35));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L06a1;
			this.processor.RegAcc.Value16 = 0x3;
			goto L06a4;

		L06a1:
			this.processor.RegAcc.Value16 = 0x2;

		L06a4:
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x104);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.StackWord(this.processor.RegCount.Value16);
			goto L05fa;

		L06ae:
			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x6828), 0x4b27);

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Startup.DisplayMouseCursor();

		L06c8:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L06dd;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L06c8;

		L06dd:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L06f5;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6b;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x10a, this.processor.RegAcc.Value16);
			goto L070a;

		L06f5:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x31);
			this.processor.EmitLocalWord(0x10a, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L06c8;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L06c8;

		L070a:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);

			ImportPalaceGraphics(0);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.RegBase.Value16,
				0, 0);

		L0747:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x110);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222), this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);

			ImportPalaceGraphics(0);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x14);

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

		L0792:
			this.owner.Services.DisplayScreen(0);
			
			this.owner.Services.PerformSound(1, 0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07dc;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

		L07dc:
			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayPalace");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="flag"></param>
		public void ImportPalaceGraphics(ushort marker)
		{
			this.processor.Journal.OpenScope($"ImportPalaceGraphics({marker})");

			// function body
			this.processor.EnterFrame(0x2e);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(2, 1);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.EmitGlobalWord(0x6828, 0xffff);
			this.processor.EmitLocalWord(0x14, 0x0);
			this.processor.EmitLocalWord(0x22, 0x0);

		L081d:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91e));
			this.processor.CompareWord(this.processor.RegSource.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0835;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x14, this.processor.BitOrWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16));

		L0835:
			this.processor.EmitLocalWord(0x22, this.processor.IncrementWord(this.processor.PullLocalWord(0x22)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x9);
			if (this.processor.FlagSet.IsLess) goto L081d;
			this.processor.EmitLocalWord(0x22, 0x1);

		L0843:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25f), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb25e), this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0869;

			RenderPalaceSection(this.processor.PullLocalWord(0x22), 1);

		L0869:
			this.processor.EmitLocalWord(0x22, this.processor.IncrementWord(this.processor.PullLocalWord(0x22)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0843;

			ImportPalaceCastle(
				this.processor.PullLocalWord(0x14),
				1);

			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0897;
			
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4b81, 0);

			goto L089a;

		L0897:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4b89, 0);

		L089a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd932), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L08e0;

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "cbacks0.pic");

			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0xd932);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x30);
			this.processor.EmitLocalByte(0xa, this.processor.RegAcc.LowByte);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x87, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

		L08e0:
			this.processor.EmitLocalWord(0x1c, 0x10);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd922), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L08f0;
			this.processor.EmitLocalWord(0x1c, this.processor.SubtractWord(this.processor.PullLocalWord(0x1c), 0x18));

		L08f0:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb22a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L08fa;
			this.processor.IncrementLocalWord(0x1c);

		L08fa:
			this.processor.EmitLocalWord(0x20, 0x24);
			this.processor.EmitLocalWord(0x22, 0x8);
			goto L090c;

		L0906:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

		L090c:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.DecrementLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91e)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0906;
			this.processor.EmitLocalWord(0x22, 0x1);
			goto L0b05;

		L0923:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0945;

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				(ushort)(this.processor.PullLocalWord(0x1c) + this.processor.PullGlobalWord(0x4b32)),
				this.processor.PullLocalWord(0x20),
				1);

		L0945:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0957;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L096d;

		L0957:
			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x20),
				0);

		L096d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b32);
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L097f;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));

		L097f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			goto L0b02;

		L0988:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			if (this.processor.FlagSet.IsNotEqual) goto L09c6;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L09c6;

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x12),
				(ushort)(this.processor.PullLocalWord(0x1c) +
					this.processor.PullGlobalWord(0x4b2e) - 17),
				(ushort)(this.processor.PullLocalWord(0x20) - 2),
				0);

		L09c6:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			if (this.processor.FlagSet.IsNotEqual) goto L0a05;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0a05;
			
			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x18),
				(ushort)(this.processor.PullLocalWord(0x1c) +
					this.processor.PullGlobalWord(0x4b2e) - 17),
				(ushort)(this.processor.PullLocalWord(0x20) - 2),
				1);

		L0a05:
			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x20),
				0);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x20),
				1);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			goto L0af9;

		L0a3e:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb22a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0a48;
			this.processor.DecrementLocalWord(0x1c);

		L0a48:
			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				(ushort)(this.processor.PullLocalWord(0x1c) - 2),
				this.processor.PullLocalWord(0x20),
				0);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2c);
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb22a), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0a71;
			goto L0b02;

		L0a71:
			this.processor.EmitLocalWord(0x1c, this.processor.DecrementWord(this.processor.PullLocalWord(0x1c)));
			goto L0b02;

		L0a77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0a99;

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x20),
				0);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b2e);
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));

		L0a99:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0ac9;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91c)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0ac9;

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				(ushort)(this.processor.PullLocalWord(0x1c) - 3),
				(ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91c)) -
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)) +
					this.processor.PullLocalWord(0x20) + 1),
				1);

			goto L0ae9;

		L0ac9:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);			

			RenderPalacePart(
				this.processor.PullLocalWord(0x2e),
				this.processor.PullLocalWord(0x22),
				(ushort)(this.processor.PullLocalWord(0x1c) -
					((this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)) == 1) ? 3 : 2)),
				this.processor.PullLocalWord(0x20),
				1);

		L0ae9:			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x4b34);

		L0af9:
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16));
			goto L0b02;

		L0afe:
			this.processor.EmitLocalWord(0x1c, this.processor.SumWord(this.processor.PullLocalWord(0x1c), 0x30));

		L0b02:
			this.processor.EmitLocalWord(0x22, this.processor.IncrementWord(this.processor.PullLocalWord(0x22)));

		L0b05:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b87;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0afe;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x26, 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b40;
			this.processor.EmitLocalWord(0x26, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91c)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0b3b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0b40;

		L0b3b:
			this.processor.EmitLocalWord(0x26, 0x0);

		L0b40:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0b62;
			this.processor.EmitLocalWord(0x26, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd920)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0b5d;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L0b62;

		L0b5d:
			this.processor.EmitLocalWord(0x26, 0x3);

		L0b62:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0b6c;
			goto L0923;

		L0b6c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0b74;
			goto L0988;

		L0b74:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0b7c;
			goto L0a3e;

		L0b7c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0b84;
			goto L0a77;

		L0b84:
			goto L0b02;

		L0b87:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0bc0;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0bc0;

			RenderPalacePart(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)),
				this.processor.PullLocalWord(0x12),
				(ushort)((this.processor.PullLocalWord(0x12) * 48) - 36),
				(ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd920)) -
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e)) +
					this.processor.PullLocalWord(0x20)),
				0);

		L0bc0:
			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x6828), 0x4b9f);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd930), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0be1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd934), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0be1;
			goto L0c94;

		L0be1:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "cbrush0.pic");

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd930), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c36;
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0xd930);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x2e);
			this.processor.EmitLocalByte(0xa, this.processor.RegAcc.LowByte);

			this.owner.ImageToolkit.ImportIcon((ushort)(this.processor.RegFrame.Value16 - 0x10));

			this.processor.EmitLocalWord(0x2a, this.processor.RegAcc.Value16);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				0,
				((this.processor.PullGlobalSignedWord(0xd930) <= 1) ? 105 : 94),
				this.processor.PullLocalWord(0x2a));

		L0c36:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd934), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c77;
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0xd934);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x2f);
			this.processor.EmitLocalByte(0xa, this.processor.RegAcc.LowByte);

			this.owner.ImageToolkit.ImportIcon((ushort)(this.processor.RegFrame.Value16 - 0x10));

			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				184,
				((this.processor.PullGlobalSignedWord(0xd934) <= 1) ? 105 : 94),
				this.processor.PullLocalWord(0x2c));

		L0c77:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd930), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c88;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2a);
			goto L0c8b;

		L0c88:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2c);

		L0c8b:
			this.owner.Helpers.ReleaseResource(this.processor.RegAcc.Value16, 0);

		L0c94:
			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0cf9;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0cc1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeba), 0x7);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0cc1;

			this.owner.Services.FloodColors(5, 0, 0, 0);

		L0cc1:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4bb0, 1);

			this.owner.Startup.QueryForDisk(0, 1);

		L0cf9:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportPalaceGraphics");
		}

		/// <summary>
		/// The look of the palace section arg1 (0 - 7, levels in the words at 0xd91c...) by its neighbours: the style (0xb25e
		/// + 2 * section) and the shape (0xb25f + 2 * section). Section 4 is the middle; a left section (0 - 3) without a
		/// built neighbour to the left (or section 1) is the left end, a right section (5 - 7) without one to the right (or
		/// section 7) the right end.
		/// </summary>
		public void RenderPalaceSection(ushort arg1, ushort marker)
		{
			this.processor.Journal.OpenScope($"RenderPalaceSection({arg1}, {marker})");

			short section = (short)arg1;
			ushort levels = (ushort)(arg1 << 1);
			ushort style = (ushort)(levels + 0xb25e);
			ushort shape = (ushort)(levels + 0xb25f);
			ushort level = this.processor.PullGlobalWord((ushort)(levels + 0xd91e));

			// 0: left end, 1: inner section, 2: middle, 3: right end
			int place = 2;

			if (section < 4)
			{
				place = 1;

				if (this.processor.PullGlobalWord((ushort)(levels + 0xd91c)) == 0xffff || arg1 == 1)
				{
					place = 0;
				}
			}

			if (section > 4)
			{
				place = 1;

				if (this.processor.PullGlobalWord((ushort)(levels + 0xd920)) == 0xffff || arg1 == 7)
				{
					place = 3;
				}
			}

			switch (place)
			{
				case 0:
					this.processor.EmitGlobalByte(style, 0x3);

					if (arg1 == 3 || marker == 0)
					{
						this.processor.EmitGlobalByte(shape, (byte)(level != 0 ? 1 : 2));
					}
					break;

				case 1:
					this.processor.EmitGlobalByte(style, (byte)(LeftNeighbourHigher(levels) || arg1 == 5 ||
						this.processor.PullGlobalWord((ushort)(levels + 0xd91a)) == 0xffff ? 1 : 2));
					this.processor.EmitGlobalByte(shape, 0x2);
					break;

				case 2:
					this.processor.EmitGlobalByte(style, 0x0);
					break;

				case 3:
					if (arg1 == 5 || marker == 0)
					{
						this.processor.EmitGlobalByte(style, (byte)(LeftNeighbourHigher(levels) || arg1 == 5 ? 1 : 2));
					}

					this.processor.EmitGlobalByte(shape, 0x4);
					break;
			}

			// Far return
			this.processor.Journal.CloseScope("RenderPalaceSection");
		}

		/// <summary>The section to the left (levels at 0xd91c) is higher than the section (0xd91e)</summary>
		private bool LeftNeighbourHigher(ushort levels)
		{
			return (short)this.processor.PullGlobalWord((ushort)(levels + 0xd91c)) > (short)this.processor.PullGlobalWord((ushort)(levels + 0xd91e));
		}

		/// <summary>
		/// One part of the palace: picture (table at 0xb23c, by the part arg2 and its style marker) at column arg3; the row comes
		/// from the height table at 0x4b36 (records of 15 bytes by arg1, by the place of the part) plus arg4 and arg1
		/// </summary>
		public void RenderPalacePart(ushort arg1, ushort arg2, ushort arg3, ushort arg4, ushort marker)
		{
			this.processor.Journal.OpenScope($"RenderPalacePart({arg1}, {arg2}, {arg3}, {arg4}, {marker})");

			ushort partPlace = this.processor.PullGlobalWord((ushort)((arg2 << 1) + 0xb222));
			sbyte styleShift = (sbyte)this.processor.PullGlobalByte((ushort)(marker + (arg2 << 1) + 0xb25e));
			ushort heightIdx = (ushort)(styleShift + (5 * partPlace));
			sbyte height = (sbyte)this.processor.PullGlobalByte((ushort)((0xf * arg1) + heightIdx + 0x4b36));
			ushort vertLoc = (ushort)(height + arg4 + arg1);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
				(short)arg3,
				(short)vertLoc,
				this.processor.PullGlobalWord((ushort)((marker << 1) + (arg2 << 2) + 0xb23c)));

			this.processor.Journal.CloseScope("RenderPalacePart");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void ImportPalaceCastle(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ImportPalaceCastle({arg1}, {arg2})");

			// function body
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x1a, 0x0);
			goto L104e;

		L0ea4:
			this.owner.VideoCard.ScreenToPicture(1,
				this.processor.PullLocalWord(0x16),
				(ushort)(this.processor.PullLocalWord(0x18) + 1),
				0x34, 0x63);

		L0eb4:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb23c), this.processor.RegAcc.Value16);

		L0ed0:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb25e)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0ef6;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6828), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0ef6;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb23c));
			this.processor.EmitGlobalWord(0x6828, this.processor.RegAcc.Value16);

		L0ef6:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));

		L0ef9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x2);
			if (this.processor.FlagSet.IsLess) goto L0f02;
			goto L0fdf;

		L0f02:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb25e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0ea4;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0f29;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0f40;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0f55;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0f99;
			goto L0ed0;

		L0f29:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x16) + 0x35),
				(ushort)(this.processor.PullLocalWord(0x18) + 1),
				0x18, 0x63);

		L0f3c:
			goto L0eb4;

		L0f40:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x16) + 0x4e),
				(ushort)(this.processor.PullLocalWord(0x18) + 1),
				0x18, 0x63);
			goto L0f3c;

		L0f55:
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f67;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0f73;

		L0f67:
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0f88;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0f88;

		L0f73:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x16) + 0x67),
				(ushort)(this.processor.PullLocalWord(0x18) + 1),
				0x1c, 0x63);
			goto L0f3c;

		L0f88:
			this.owner.VideoCard.ScreenToPicture(1, 0xa0, 0x65, 0x23, 0x63);
			goto L0f3c;

		L0f99:
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0fab;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0fb7;

		L0fab:
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0fcd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0fcd;

		L0fb7:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x16) + 0x84),
				(ushort)(this.processor.PullLocalWord(0x18) + 1),
				0x1b, 0x63);
			goto L0f3c;

		L0fcd:
			this.owner.VideoCard.ScreenToPicture(1, 0xc4, 0x65, 0x23, 0x63);
			goto L0f3c;

		L0fdf:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));

		L0fe2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L104b;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd91e)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ffc;
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0fdf;

		L0ffc:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb222));
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = 0xa0;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x2);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1043;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.DecrementWord(this.processor.RegSource.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = 0xa0;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x2);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

		L1043:
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0ef9;

		L104b:
			this.processor.EmitLocalWord(0x1a, this.processor.IncrementWord(this.processor.PullLocalWord(0x1a)));

		L104e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1091;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x1a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, arg1);
			if (this.processor.FlagSet.IsEqual) goto L104b;

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "castle0.pic");

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1a);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x30);
			this.processor.EmitLocalByte(0xa, this.processor.RegAcc.LowByte);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.processor.EmitLocalWord(0x12, 0x1);
			goto L0fe2;

		L1091:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportPalaceCastle");
		}
	}
}
