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
		/// Draws the palace on screen 1 (the parts of sections 0 - 8 in the words at 0xd91e, 0xffff not built): the looks of
		/// sections 1 - 7, the castle, the background (0x4b81 for the palace screen, 0x4b89), the garden "cbacksN.pic" (N =
		/// 0xd932), the sections from x 0x10 (8 further left without section 2, one with 0xb22a = 1): the left ones with their
		/// wings, the middle one, the right ones with their towers, each joined to a neighbour of the same style (0xb222), and
		/// the bushes "cbrushN.pic" left (0xd930) and right (0xd934). For the palace screen (marker) screen 1 is shown with the
		/// palette 0x4bb0. The file name is in the stack frame, like in the original, and the rightmost built section (0x18)
		/// and the last left section (0x12) are read from the frame even when this call didn't set them, like in the original.
		/// </summary>
		public void ImportPalaceGraphics(ushort marker)
		{
			this.processor.Journal.OpenScope($"ImportPalaceGraphics({marker})");

			this.processor.EnterFrame(0x2e);
			this.processor.PreserveSource();

			ushort pictureName = (ushort)(this.processor.RegFrame.Value16 - 0x10);

			this.owner.Startup.QueryForDisk(2, 1);

			this.processor.EmitGlobalWord(this.processor.PullGlobalWord(0xaa), 1);
			this.processor.EmitGlobalWord(0x6828, 0xffff);

			ushort built = 0;

			for (ushort sectionIdx = 0; sectionIdx < 9; sectionIdx++)
			{
				ushort part = PalacePart(sectionIdx);

				if (part != 0xffff)
					built |= (ushort)(1 << (byte)part);
			}

			this.processor.EmitLocalWord(0x14, built);

			for (ushort sectionIdx = 1; sectionIdx < 8; sectionIdx++)
			{
				this.processor.EmitGlobalByte((ushort)((sectionIdx << 1) + 0xb25f), 0xff);
				this.processor.EmitGlobalByte((ushort)((sectionIdx << 1) + 0xb25e), 0xff);

				if (PalacePart(sectionIdx) != 0xffff)
					RenderPalaceSection(sectionIdx, 1);
			}

			ImportPalaceCastle(built, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, marker != 0 ? (ushort)0x4b81 : (ushort)0x4b89, 0);

			if ((short)this.processor.PullGlobalWord(0xd932) > 0)
			{
				this.owner.Libc.CopyText(pictureName, "cbacks0.pic");

				this.processor.EmitGlobalByte((ushort)(pictureName + 6), (byte)(this.processor.PullGlobalByte(0xd932) + 0x30));

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x87, pictureName, 0);
			}

			ushort horizLoc = 0x10;

			if (this.processor.PullGlobalWord(0xd922) == 0xffff)
				horizLoc -= 0x18;

			if (this.processor.PullGlobalWord(0xb22a) == 1)
				horizLoc++;

			const ushort vertLoc = 0x24;

			// The rightmost built section (0x18 is written only when section 8 isn't built)
			short rightmost = 8;

			while (PalacePart((ushort)rightmost) == 0xffff)
			{
				rightmost--;
				this.processor.EmitLocalWord(0x18, (ushort)rightmost);
			}

			ushort section;

			for (section = 1; section < 8; section++)
			{
				ushort part = PalacePart(section);

				if (part == 0xffff)
				{
					horizLoc += 0x30;
					continue;
				}

				this.processor.EmitLocalWord(0x2e, part);

				ushort look = 2;

				if (section < 4)
					look = PalacePart((ushort)(section - 1)) == 0xffff || section == 1 ? (ushort)0 : (ushort)1;

				if (section > 4)
					look = PalacePart((ushort)(section + 1)) == 0xffff || section == 7 ? (ushort)3 : (ushort)1;

				this.processor.EmitLocalWord(0x26, look);

				switch (look)
				{
					case 0:
						// A left end with its wing
						if (section == 3)
							RenderPalacePart(part, section, (ushort)(horizLoc + this.processor.PullGlobalWord(0x4b32)), vertLoc, 1);

						if (PalaceStyle(section) != 1 || section == 3)
							RenderPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += this.processor.PullGlobalWord(0x4b32);

						if (section == 3)
							horizLoc += this.processor.PullGlobalWord(0x4b2e);

						this.processor.EmitLocalWord(0x12, section);
						break;

					case 1:
						// A section between two, joined to the left end before it and to the rightmost one after it
						ushort leftEnd = this.processor.PullLocalWord(0x12);

						if ((ushort)(leftEnd + 1) == section && PalaceStyle(section) == PalaceStyle(leftEnd))
							RenderPalacePart(part, leftEnd, (ushort)(horizLoc + this.processor.PullGlobalWord(0x4b2e) - 17), vertLoc - 2, 0);

						ushort rightEnd = this.processor.PullLocalWord(0x18);

						if ((ushort)(rightEnd - 1) == section && PalaceStyle(section) == PalaceStyle(rightEnd))
							RenderPalacePart(part, rightEnd, (ushort)(horizLoc + this.processor.PullGlobalWord(0x4b2e) - 17), vertLoc - 2, 1);

						RenderPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += this.processor.PullGlobalWord(0x4b2e);

						RenderPalacePart(part, section, horizLoc, vertLoc, 1);

						horizLoc += this.processor.PullGlobalWord(0x4b2e);
						break;

					case 2:
						// The middle
						if (this.processor.PullGlobalWord(0xb22a) == 1)
							horizLoc--;

						RenderPalacePart(part, section, (ushort)(horizLoc - 2), vertLoc, 0);

						horizLoc += this.processor.PullGlobalWord(0x4b2c);

						if (this.processor.PullGlobalWord(0xb22a) == 1)
							horizLoc--;
						break;

					case 3:
						// A right end with its tower
						if (section == 5)
						{
							RenderPalacePart(part, section, horizLoc, vertLoc, 0);

							horizLoc += this.processor.PullGlobalWord(0x4b2e);
						}

						ushort level = PalacePart((ushort)(section - 1));

						if (PalaceStyle(section) == 1 && (short)level > (short)PalacePart(section))
						{
							RenderPalacePart(part, section, (ushort)(horizLoc - 3), (ushort)(level - PalacePart(section) + vertLoc + 1), 1);
						}
						else
						{
							RenderPalacePart(part, section, (ushort)(horizLoc - (PalaceStyle(section) == 1 ? 3 : 2)), vertLoc, 1);
						}

						horizLoc += this.processor.PullGlobalWord(0x4b34);
						break;
				}
			}

			this.processor.EmitLocalWord(0x1c, horizLoc);
			this.processor.EmitLocalWord(0x20, vertLoc);
			this.processor.EmitLocalWord(0x22, section);

			// The wing of the last left end, unless it is section 3
			ushort lastLeft = this.processor.PullLocalWord(0x12);

			if (PalaceStyle(lastLeft) == 1 && lastLeft != 3)
			{
				RenderPalacePart(PalacePart(lastLeft), lastLeft, (ushort)((lastLeft * 48) - 36),
					(ushort)(PalacePart((ushort)(lastLeft + 1)) - PalacePart(lastLeft) + vertLoc), 0);
			}

			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x6828), 0x4b9f);

			if (this.processor.PullGlobalWord(0xd930) != 0 || this.processor.PullGlobalWord(0xd934) != 0)
			{
				this.owner.Libc.CopyText(pictureName, "cbrush0.pic");

				if (this.processor.PullGlobalWord(0xd930) != 0)
				{
					RenderPalaceBush(pictureName, 0xd930, 0x2e, 0x2a, 0);
				}

				if (this.processor.PullGlobalWord(0xd934) != 0)
				{
					RenderPalaceBush(pictureName, 0xd934, 0x2f, 0x2c, 184);
				}

				this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(this.processor.PullGlobalWord(0xd930) != 0 ? 0x2a : 0x2c), 0);
			}

			if (marker != 0)
			{
				this.processor.EmitGlobalWord(this.processor.PullGlobalWord(0xaa), 0);

				if (this.processor.PullGlobalWord(0xd762) != 0 && (short)this.processor.PullGlobalWord(0xdeba) > 7)
				{
					this.owner.Services.FloodColors(5, 0, 0, 0);
				}

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4bb0, 1);

				this.owner.Startup.QueryForDisk(0, 1);
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportPalaceGraphics");
		}

		/// <summary>The part built in the palace section (0xd91e + 2 * section), 0xffff for none</summary>
		private ushort PalacePart(ushort sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xd91e));
		}

		/// <summary>The style of the palace section (0xb222 + 2 * section)</summary>
		private ushort PalaceStyle(ushort sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xb222));
		}

		/// <summary>A bush of the palace garden: cbrush picture 2 * size + first from the word sizeAddr, at x horizLoc</summary>
		private void RenderPalaceBush(ushort pictureName, ushort sizeAddr, byte first, int pictureLocal, int horizLoc)
		{
			this.processor.EmitGlobalByte((ushort)(pictureName + 6), (byte)((this.processor.PullGlobalByte(sizeAddr) << 1) + first));

			this.owner.ImageToolkit.ImportIcon(pictureName);

			this.processor.EmitLocalWord(pictureLocal, this.processor.RegAcc.Value16);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), horizLoc,
				(short)this.processor.PullGlobalWord(sizeAddr) <= 1 ? 105 : 94, this.processor.PullLocalWord(pictureLocal));
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
		/// Cuts the pictures of the palace sections 1 - 7 from the castle pictures castle0 - castle4.pic (each one in the mask
		/// arg1): with arg2 the sections of that level (0xd91e) from the place of the section (0xb222: bit 0 the column, bit 1 the
		/// row), without arg2 all sections from the place of the section before. Each section has two parts (styles at 0xb25e:
		/// 0 the whole wall, 1 and 2 left and right pieces, 3 and 4 the ends, cut from the middle picture for section 2 or a
		/// section at the place 1), the picture goes to 0xb23c + 4 * section + 2 * part, the first one also to 0x6828 when unset.
		/// </summary>
		public void ImportPalaceCastle(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ImportPalaceCastle({arg1}, {arg2})");

			// The file name is in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveSource();

			ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			ushort level;

			for (level = 0; level < 5; level++)
			{
				if (((1 << level) & arg1) == 0)
					continue;

				this.owner.Libc.CopyText(docname, "castle0.pic");
				this.processor.EmitLocalByte(0xa, (byte)(level + 0x30));

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docname, 0);

				ushort section;

				for (section = 1; section < 8; section++)
				{
					if (this.processor.PullGlobalWord((ushort)((section << 1) + 0xd91e)) != level && arg2 != 0)
						continue;

					ushort place = this.processor.PullGlobalWord((ushort)((section << 1) + 0xb222));

					if (arg2 == 0)
					{
						place = (ushort)(section - 1);
					}

					ushort horizLoc = (ushort)((place & 1) * 0xa0);
					ushort vertLoc = (ushort)((place & 2) * 0x32);

					for (ushort partIdx = 0; partIdx < 2; partIdx++)
					{
						ushort picture = (ushort)((section << 2) + (partIdx << 1) + 0xb23c);
						sbyte style = (sbyte)this.processor.PullGlobalByte((ushort)((section << 1) + 0xb25e + partIdx));
						bool middle = arg2 != 0 ? this.processor.PullGlobalWord((ushort)((section << 1) + 0xb222)) == 1 : section == 2;

						switch (style)
						{
							case 0:
								this.owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 1), 0x34, 0x63);
								break;

							case 1:
								this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x35), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 2:
								this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x4e), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 3:
								if (middle)
									this.owner.VideoCard.ScreenToPicture(1, 0xa0, 0x65, 0x23, 0x63);
								else
									this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x67), (ushort)(vertLoc + 1), 0x1c, 0x63);
								break;

							case 4:
								if (middle)
									this.owner.VideoCard.ScreenToPicture(1, 0xc4, 0x65, 0x23, 0x63);
								else
									this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x84), (ushort)(vertLoc + 1), 0x1b, 0x63);
								break;
						}

						if (style is >= 0 and <= 4)
						{
							this.processor.EmitGlobalWord(picture, this.processor.RegAcc.Value16);
						}

						if (style != -1 && this.processor.PullGlobalWord(0x6828) == 0xffff)
						{
							this.processor.EmitGlobalWord(0x6828, this.processor.PullGlobalWord(picture));
						}
					}

					this.processor.EmitLocalWord(0x14, 0x2);
					this.processor.EmitLocalWord(0x16, horizLoc);
					this.processor.EmitLocalWord(0x18, vertLoc);
				}

				this.processor.EmitLocalWord(0x12, section);
			}

			this.processor.EmitLocalWord(0x1a, level);

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ImportPalaceCastle");
		}
	}
}
