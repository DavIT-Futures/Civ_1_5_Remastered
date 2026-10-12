using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 18.
	/// </summary>
	public class SpaceshipScreen
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public SpaceshipScreen(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// The space background: a black screen with the picture 0x4bc4, its 10 parts cut out (rectangles of 13 words at 0x4be2,
		/// the pictures go to 0x682a), 20 stars at random places (0x683e, 0x6852, 0x6866), the colour cycles 1 - 5 set up and,
		/// when 0xd762 is set, started
		/// </summary>
		public void RenderSpaceScenery()
		{
			this.processor.Journal.OpenScope("RenderSpaceScenery()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(1, 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4bc4, 1);

			for (int partIdx = 0; partIdx < 0xa; partIdx++)
			{
				ushort part = (ushort)(0x1a * partIdx);

				ushort picture = this.owner.VideoCard.ScreenToPicture(1, this.processor.PullGlobalWord((ushort)(part + 0x4be2)),
					this.processor.PullGlobalWord((ushort)(part + 0x4be4)), this.processor.PullGlobalWord((ushort)(part + 0x4be6)),
					this.processor.PullGlobalWord((ushort)(part + 0x4be8)));

				this.processor.EmitGlobalWord((ushort)((partIdx << 1) + 0x682a), picture);
			}

			for (int starIdx = 0; starIdx < 0x14; starIdx++)
			{
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x683e), (byte)this.owner.Libc.Dice.DrawInt(240));
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x6852), (byte)(this.owner.Libc.Dice.DrawInt(192) + 8));
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x6866), 0x0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Services.AssignHueCycleSpan(1, 30, 208, 223);
			this.owner.Services.AssignHueCycleSpan(2, 11, 192, 207);
			this.owner.Services.AssignHueCycleSpan(3, 20, 224, 239);
			this.owner.Services.AssignHueCycleSpan(4, 8, 144, 159);
			this.owner.Services.AssignHueCycleSpan(5, 150, 128, 129);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.owner.Services.ActivateHueCycle(cycleIdx);
				}
			}

			this.processor.Journal.CloseScope("RenderSpaceScenery");
		}

		/// <summary>
		/// Leaves the spaceship screen: frees its picture (0x682a), stops the colour cycles 1 - 5 when 0xd762 is set, loads the
		/// palette of the human player's government, "backNa.pal" (N is the government / 2, "m" instead of "a" with technology
		/// 20), and the palette 0x4cea, clears the screen and draws the map screen again. The file name is made in a buffer in
		/// the stack frame, like in the original.
		/// </summary>
		public void ImportSpaceScenery()
		{
			this.processor.Journal.OpenScope("ImportSpaceScenery()");

			this.processor.EnterFrame(0x12);

			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x682a), 0x4cda);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.owner.Services.DeactivateHueCycle(cycleIdx);
				}
			}

			this.owner.Startup.QueryForDisk(0, 1);

			ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			this.owner.Libc.CopyText(docname, "back0a.pal");

			short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)((regime / 2) + 0x30));

			if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, 0xc1d6);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4cea, 0xc5be);

			this.owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.VideoCard.AssignHuesFromHueStruct(0xc5be);

			this.owner.Helpers.SetupHueCycling();

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ImportSpaceScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort RenderSpaceship(short participantIdx, short arg2, short arg3)
		{
			this.processor.Journal.OpenScope($"RenderSpaceship({participantIdx}, {arg2}, {arg3})");

			// function body
			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0275;
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L027a;

		L0275:
			this.processor.RegAcc.Value16 = 0x1;
			goto L027c;

		L027a:
			this.processor.RegAcc.Value16 = 0;

		L027c:
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0286;
			goto L0325;

		L0286:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " SpaceShip: X.S.S. ");

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb9ff), this.processor.RegAcc.LowByte);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 1, 0);

		L0325:
			this.processor.EmitLocalWord(0xe, 0x0);

		L032a:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdea2), 0x0);
			this.processor.IncrementLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xa);
			if (this.processor.FlagSet.IsLess) goto L032a;
			this.processor.EmitLocalWord(0xe, 0x0);

		L0343:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xb4, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0xa));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x10));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0389;
			goto L04b0;

		L0389:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L03b7;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xa) * 16) - 16,
				(this.processor.PullLocalSignedWord(0x10) * 16) + 8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegAcc.Value16 << 1 + 0x682a)));

		L03b7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03c8;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L03c8;
			this.processor.RegAcc.Value16 = 0x1;
			goto L03cb;

		L03c8:
			this.processor.RegAcc.Value16 = 0x2;

		L03cb:
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03d9;
			this.processor.RegAcc.Value16 = 0x1;
			goto L03dc;

		L03d9:
			this.processor.RegAcc.Value16 = 0x2;

		L03dc:
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03ea;
			this.processor.RegAcc.Value16 = 0x1;
			goto L03ec;

		L03ea:
			this.processor.RegAcc.Value16 = 0;

		L03ec:
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x18, 0x0);
			goto L045a;

		L03f6:
			this.processor.EmitLocalWord(0x1c, this.processor.IncrementWord(this.processor.PullLocalWord(0x1c)));

		L03f9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0457;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x18));
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x1c));
			
			ObtainSpaceshipPartValue(participantIdx, (ushort)(this.processor.RegSource.Value16 + 0x1), this.processor.RegDest.Value16);
			
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));

			ObtainSpaceshipPartValue(participantIdx, (ushort)(this.processor.RegSource.Value16 - 0x1), this.processor.RegDest.Value16);
			
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));
			
			ObtainSpaceshipPartValue(participantIdx, this.processor.RegSource.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1));
			
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));
			
			ObtainSpaceshipPartValue(participantIdx, this.processor.RegSource.Value16, (ushort)(this.processor.RegDest.Value16 - 0x1));
			
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));

			goto L03f6;

		L0457:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));

		L045a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0469;
			this.processor.EmitLocalWord(0x1c, 0x0);
			goto L03f9;

		L0469:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L047a;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdea2), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdea2))));
			goto L04c9;

		L047a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04c9;

			this.owner.Dialogs.RenderBox(
				(this.processor.PullLocalWord(0xa) << 4) - 16,
				(this.processor.PullLocalWord(0x10) << 4) + 8,
				(this.processor.PullLocalWord(0x1a) << 4) - 1,
				(this.processor.PullLocalWord(0x1e) << 4) - 1,
				12);

			goto L04c9;

		L04b0:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L04c9;
			this.processor.EmitLocalWord(0x12, 0x0);

		L04c9:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L04e5;
			goto L0343;

		L04e5:
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0xe, 0x0);

		L04ef:
			this.processor.RegAcc.Value16 = 0x1a;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4be0));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdea2)));
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xa);
			if (this.processor.FlagSet.IsLess) goto L04ef;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x50);
			if (this.processor.FlagSet.IsLess) goto L0534;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0534;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				152, 88,
				this.processor.PullGlobalWord(0x6834));

		L0534:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0592;

			this.processor.RegSource.Value16 = (ushort)((participantIdx << 1) + 0x1946);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 240, 8, 80, 192,
				this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				0, 8, 0, 199, this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				0, 199, 240, 199, this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16));

		L0592:
			this.processor.EmitLocalWord(0xc, 0x64);
			this.processor.EmitLocalWord(0x10, 0x10);

			this.owner.TextPainter.RenderTextToScreen0(0x4d08, 242, 16, 0);

			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0xdeae), 10));

			this.owner.Libc.AppendText(0xba06, "0,000");

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0609;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L0609:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

			this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0xdeae), 1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x64, this.processor.PullGlobalWord(0xdeb0));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Support: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x14), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x64);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L067b;
			this.processor.RegAcc.Value16 = 0x4;
			goto L067d;

		L067b:
			this.processor.RegAcc.Value16 = 0;

		L067d:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 
				242, 
				this.processor.PullLocalSignedWord(0x10), 
				this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06b9;

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x14), 0, 100);

			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xc));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x64;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

		L06b9:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0xdeae) + this.processor.PullGlobalSignedWord(0xdeb0), 
				1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc8, this.processor.PullGlobalWord(0xdeb2));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Energy: ");

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x14), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x14), 0, 100);

			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xc));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x64;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0764;
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L074e;
			this.processor.RegAcc.Value16 = 0x4;
			goto L0750;

		L074e:
			this.processor.RegAcc.Value16 = 0;

		L0750:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 
				242,
				this.processor.PullLocalSignedWord(0x10), 
				this.processor.RegAcc.Value16);

		L0764:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x10));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0784;
			
			this.owner.TextPainter.RenderTextToScreen0(0x4d31, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L0784:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x6) / 10), 10));

			this.owner.Libc.AppendText(0xba06, ",");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x6) / 10), 10));

			this.owner.Libc.AppendText(0xba06, "00 tons");

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0817;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L0817:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

			this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0xdea8), 1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x64, this.processor.PullGlobalWord(0xdeaa));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Fuel: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x14), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08a5;
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x64);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L088f;
			this.processor.RegAcc.Value16 = 0x4;
			goto L0891;

		L088f:
			this.processor.RegAcc.Value16 = 0;

		L0891:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242,
				this.processor.PullLocalSignedWord(0x10), this.processor.RegAcc.Value16);

		L08a5:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08c5;

			this.owner.TextPainter.RenderTextToScreen0(0x4d4a, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L08c5:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

			this.owner.Helpers.VerifyValueSpan(
				10 * this.processor.PullGlobalSignedWord(0xdea8), 
				0, 10 * this.processor.PullGlobalSignedWord(0xdeaa));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.ScaleIndex(0x32, this.processor.PullLocalWord(0x6));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x96);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0904;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x96);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.SubtractWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16));

		L0904:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x14) / 10), 10));

			this.owner.Libc.AppendText(0xba06, ".");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x14) / 10), 10));

			this.owner.Libc.AppendText(0xba06, " Years");

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0993;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L0993:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x10));

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xc), 0, 100);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09c8;

			this.owner.TextPainter.RenderTextToScreen0(0x4d60, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L09c8:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

			this.owner.Libc.CopyText(0xba06, "--- ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xc), 10));

			this.owner.Libc.AppendText(0xba06, "% ---");

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0a28;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, this.processor.PullLocalSignedWord(0x10), 0);

		L0a28:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x10));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeae), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0a38;
			arg3 = 0;

		L0a38:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0a7e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0a4f;
			goto L0ada;

		L0a4f:
			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)((this.owner.World.Participants[participantIdx].StarshipArrivalSeason < owner.World.Season) ? 0x4d7c : 0x4d84),
				242,
				this.processor.PullLocalSignedWord(0x10),
				0);

			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x10));
			goto L0ada;

		L0a7e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Season);
			this.owner.World.Participants[participantIdx].StarshipArrivalSeason = (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0ada;
			
			if (arg2 >= 0) goto L0ada;
			
			if (arg3 == 0) goto L0ada;

			this.owner.TextPainter.RenderTextToScreen0(0x4d8e, 256, 190, 0);

			this.owner.Dialogs.RenderBox(242, 188, 76, 9, 0);

		L0ada:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ae5;

			this.owner.TurnLogic.AwaitForKey();

		L0ae5:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0af3;
			goto L0bfc;

		L0af3:
			this.owner.Startup.ConcealMouseCursor();

			RenderStarField();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0b14;
			this.processor.RegAcc.Value16 = 0x2;
			goto L0b17;

		L0b14:
			this.processor.RegAcc.Value16 = 0x4;

		L0b17:
			this.owner.TextPainter.AwaitTime((short)this.processor.RegAcc.Value16);

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0b3a;

			arg2--;
			if (arg2 == 0) goto L0b3a;

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0af3;

		L0b3a:
			if (arg2 == 0) goto L0b4f;

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b4f;

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

		L0b4f:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b5a;
			goto L0bfc;

		L0b5a:
			if (arg3 != 0) goto L0b63;
			goto L0bfc;

		L0b63:
			if (arg2 < 0) goto L0b6c;
			goto L0bfc;

		L0b6c:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0b83;
			this.processor.CompareWord(this.owner.GlobDb3c, 0xf2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0b83;
			this.processor.CompareWord(this.owner.GlobDb3e, 0xbe);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b89;

		L0b83:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x6c);
			if (this.processor.FlagSet.IsNotEqual) goto L0bfc;

		L0b89:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L0bfc;

			this.owner.Libc.CopyText(0xba06, "Confirm spaceship launch.\n(");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xc), 10));

			this.owner.Libc.AppendText(0xba06, "% chance of success)\n No Launch\n Launch CONFIRMED!\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 100);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0bfc;

			FireSpaceship(this.owner.World.PersonParticipantIdx);

		L0bfc:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeae);
			this.owner.World.Participants[participantIdx].StarshipInhabitants = (short)this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("RenderSpaceship");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Is the place of the spaceship of the player taken by a part: the byte of the spaceship layout (segment 0x3772,
		/// 0x16e0 + 0xb4 * player + 0xc * row + column) is not empty (0xff) and below 3
		/// </summary>
		/// <returns>1 or 0, also in AX</returns>
		public ushort ObtainSpaceshipPartValue(short participantIdx, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ObtainSpaceshipPartValue({participantIdx}, {arg2}, {arg3})");

			this.processor.SegExtra.Value16 = 0x3772;

			sbyte part = (sbyte)this.processor.PullUInt8(this.processor.SegExtra.Value16,
				(ushort)((0xb4 * participantIdx) + (0xc * arg2) + arg3 + 0x16e0));

			ushort taken = (ushort)(part != -1 && part < 3 ? 1 : 0);

			this.processor.RegAcc.Value16 = taken;

			this.processor.Journal.CloseScope("ObtainSpaceshipPartValue");

			return taken;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenderStarField()
		{
			this.processor.Journal.OpenScope("RenderStarField()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L0cff;

		L0c57:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e), this.processor.SumByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e)), 0x2));
			this.processor.EmitLocalWord(0x6, 0x7);

		L0c64:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e)), 0xf0);
			if (this.processor.FlagSet.IsBelow) goto L0c8d;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e), 0x1);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(192));

			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x8);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6852), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6866), 0x0);

		L0c8d:
			this.owner.VideoCard.ObtainDot(0, 
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(0x683e + this.processor.RegBase.Value16)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(0x6852 + this.processor.RegBase.Value16)));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0cf4;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6866), 0x1);

			this.owner.Services.AssignDot(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6852)),
				this.processor.PullLocalWord(0x6));

			goto L0cfc;

		L0cd0:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e), this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e))));
			this.processor.EmitLocalWord(0x6, 0x3);
			goto L0c64;

		L0cde:
			this.processor.TestBitsByte(this.processor.PullGlobalByte(0x687a), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0cec;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e), this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e))));

		L0cec:
			this.processor.EmitLocalWord(0x6, 0x8);
			goto L0c64;

		L0cf4:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6866), 0x0);

		L0cfc:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L0cff:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d47;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6866)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0d2a;

			this.owner.Services.AssignDot(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x683e)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6852)),
				0);

		L0d2a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0d35;
			goto L0c57;

		L0d35:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0cd0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0cde;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0cde;
			goto L0c64;

		L0d47:
			this.processor.EmitGlobalWord(0x687a, this.processor.IncrementWord(this.processor.PullGlobalWord(0x687a)));
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderStarField");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="param2"></param>
		/// <returns></returns>
		public ushort ComputeSpaceshipSuccess(short participantIdx, short arg2)
		{
			this.processor.Journal.OpenScope($"ComputeSpaceshipSuccess({participantIdx}, {arg2})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = 0;
			this.owner.World.BotStarshipVictoryRatio = (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x1);
			this.processor.EmitLocalWord(0x2, 0x3e7);
			this.processor.CompareWord((ushort)arg2, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0d9b;
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].StarshipArrivalSeason;
			this.processor.CompareWord(this.processor.RegSource.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0da0;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Season);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0da0;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipArrivalSeason;
			this.processor.CompareWord(this.processor.RegSource.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsLess) goto L0d9b;
			this.processor.CompareWord(this.processor.RegDest.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0da0;

		L0d9b:
			this.processor.EmitLocalWord(0x6, 0x21);

		L0da0:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x6));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0e1a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsNotEqual) goto L0df7;
			this.processor.ScaleIndex(0x3, (ushort)arg2);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLess) goto L0df7;
			this.processor.CopyLocalWord(0x6, 0x2);

		L0df7:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x6));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)arg2);
			if (this.processor.FlagSet.IsNotEqual) goto L0e1a;
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L0e68;

		L0e1a:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0e68;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e41;
			goto L0da0;

		L0e41:
			this.processor.CompareWord((ushort)arg2, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0e4a;
			goto L0da0;

		L0e4a:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase((ushort)arg2);
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xe68)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0e68;

			goto L0da0;

		L0e68:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x6));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0e89;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsNotEqual) goto L0e89;
			this.processor.RegAcc.Value16 = 0x2;
			goto L0f7d;

		L0e89:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0e95;

		L0e8f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			goto L0f7d;

		L0e95:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x6));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0eb6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsEqual) goto L0eb6;

			this.processor.RegAcc.Value16 = 0x1;
			goto L0f7d;

		L0eb6:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " build space ship.\n");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 96, 15);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceScenery();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.ScaleIndexToSource(0x3, this.processor.PullLocalWord(0x6));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0), this.processor.RegCount.LowByte);
			
			RenderSpaceship(participantIdx, -1, 0);

			this.owner.World.BotStarshipVictoryRatio = (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.owner.World.StarshipMarkers |= (short)this.processor.RegAcc.Value16;

			this.owner.TurnLogic.AwaitForKey();

			ImportSpaceScenery();

			goto L0e8f;

		L0f7d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("ComputeSpaceshipSuccess");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="param2"></param>
		public void AttachSpaceshipPart(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"AttachSpaceshipPart({participantIdx}, {arg2})");

			// function body
			this.processor.EnterFrame(0x16);
			this.processor.PreserveSource();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			RenderSpaceScenery();

			RenderSpaceship(participantIdx, 15, 0);

			this.owner.TurnLogic.AwaitForKey();

		L0fc0:
			this.processor.EmitLocalWord(0x16, 0x2);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(arg2, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1000;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x687c);
			this.processor.EmitGlobalWord(0xb276, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0fde;
			goto L12c2;

		L0fde:
			this.owner.TurnHooks.DisplayDialog(0x4df9, 100, 80);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1000;
			goto L12de;

		L1000:
			this.processor.CompareWord(arg2, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1036;

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x687e);
			this.processor.EmitGlobalWord(0xb276, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1014;
			goto L12c2;

		L1014:
			this.owner.TurnHooks.DisplayDialog(0x4e12, 100, 80);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L1036;
			goto L12de;

		L1036:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1044;
			goto L1169;

		L1044:
			this.processor.EmitLocalWord(0x14, 0x63);
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L1078;

		L1055:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1075;
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L1075;
			this.processor.CopyLocalWord(0xc, 0x14);

		L1075:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L1078:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x21);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L10c3;
			this.processor.ScaleIndexToSource(0x3, this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1055;
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2)), 0x3);
			if (this.processor.FlagSet.IsLess) goto L1075;
			this.processor.CopyLocalWord(0xc, 0x6);
			goto L1075;

		L10c3:
			this.processor.EmitLocalWord(0x2, 0x63);
			this.processor.EmitLocalWord(0x8, 0xffff);
			this.processor.EmitLocalWord(0xc, 0x21);

		L10d2:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1117;
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2)), 0x3);
			if (this.processor.FlagSet.IsLess) goto L1137;
			this.processor.CopyLocalWord(0xc, 0x8);
			goto L1137;

		L1117:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1137;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L1137;
			this.processor.CopyLocalWord(0xc, 0x2);

		L1137:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L10d2;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0x14));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1164;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);

		L115f:
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			goto L11cf;

		L1164:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			goto L115f;

		L1169:
			this.processor.EmitLocalWord(0xc, 0x0);

		L116e:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L11b6;
			this.processor.ScaleIndexToBase(0x3, this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x16));
			if (this.processor.FlagSet.IsEqual) goto L11cf;

		L11b6:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L116e;

		L11cf:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L120c;
			this.processor.ScaleIndexToSource(0x3, this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x375a; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xb4, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x16e0), this.processor.RegCount.LowByte);
			goto L1278;

		L120c:
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1278;
			this.processor.RegBase.Value16 = arg2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x687a), this.processor.BitOrWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x687a)), this.processor.RegAcc.Value16));

			this.owner.Libc.CopyText(0xba06, "No further ");

			this.processor.ScaleIndex(0x3, arg2);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x16));
			this.processor.RegCount.Value16 = 0x1a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4bd0);

			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " required.\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 120, 80);

			goto L0fc0;

		L1278:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L12c2;
			this.processor.EmitLocalWord(0x4, 0x1);

		L128e:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.World.Participants[this.processor.PullLocalWord(0x4)].TouchParticipantTimeleft = 0;
			}

			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L128e;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.owner.World.StarshipMarkers |= (short)this.processor.RegAcc.Value16;

		L12c2:
			this.owner.Startup.DisplayMouseCursor();

			RenderSpaceship(participantIdx, -1, 1);
			
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio = (short)this.processor.RegAcc.Value16;

		L12de:
			this.owner.Startup.ConcealMouseCursor();

			ImportSpaceScenery();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("AttachSpaceshipPart");
		}

		/// <summary>
		/// When some player 1 - 7 has a spaceship (StarshipMarkers bits 9 - 15), asks which nation's spaceship to show (the
		/// names at 0x1982 + 2 * player; 0xb276 masks the ones without a spaceship) and shows it
		/// </summary>
		public void ChooseNationToView()
		{
			this.processor.Journal.OpenScope("ChooseNationToView()");

			if ((this.owner.World.StarshipMarkers & 0xfe00) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "Which nation?\n ");

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

					this.owner.Libc.AppendText(0xba06, "\n ");
				}

				this.processor.EmitGlobalWord(0xb276, (ushort)((ushort)(this.owner.World.StarshipMarkers >> 9) ^ 0xff));

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				short chosenIdx = (short)(this.processor.RegAcc.Value16 + 1);

				if (chosenIdx > 0)
				{
					RenderSpaceScenery();

					RenderSpaceship(chosenIdx, -1, 1);

					ImportSpaceScenery();
				}
			}

			this.processor.Journal.CloseScope("ChooseNationToView");
		}

		/// <summary>
		/// The player launches its spaceship (StarshipMarkers bit player): a black screen with "... LAUNCH space ship!" and the
		/// estimated arrival, the launch year is noted, then the spaceship is shown (on the space background, which is left
		/// again afterwards, for another player than the human)
		/// </summary>
		public void FireSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"FireSpaceship({participantIdx})");

			this.owner.World.StarshipMarkers |= (short)(1 << participantIdx);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)));
			this.owner.Libc.AppendText(0xba06, " LAUNCH space ship!");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 92, 15);

			this.owner.Libc.CopyText(0xba06, "Estimate arrival in ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].StarshipArrivalSeason, 10));
			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 100, 15);

			this.owner.World.Participants[participantIdx].StarshipLiftoffSeason = this.owner.World.Season;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				RenderSpaceScenery();
			}

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceship(participantIdx, -1, 0);

			this.owner.Startup.DisplayMouseCursor();

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				ImportSpaceScenery();
			}

			this.processor.Journal.CloseScope("FireSpaceship");
		}
	}
}
