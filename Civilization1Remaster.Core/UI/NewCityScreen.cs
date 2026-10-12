using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 23.
	/// </summary>
	public class NewCityScreen
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public NewCityScreen(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort DisplayCityLabelPrompt(short townIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCityLabelPrompt({townIdx})");

			// function body
			this.processor.EnterFrame(0x4);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

		L0010:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 80, 80, 160, 32, 15);

			this.owner.Dialogs.RenderBox(80, 80, 160, 32, 11);

			this.owner.TextPainter.RenderTextToScreen0(0x539e, 88, 82, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			
			this.owner.Conquest.ObtainCityLabel(townIdx);
			
			PullTextInput(0x58, 0x60, 0xc);
			
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.Libc.TextLength(0xba06);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0096;
			goto L0010;

		L0096:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L00c5;

			uint settlementTitleIdent = this.owner.World.Towns[townIdx].LabelIdx;
			char[] townLabel = this.owner.World.TownLabels[settlementTitleIdent].ToCharArray();

			for (int idx = 0; idx < 0xd; idx++)
			{
				townLabel[idx] = (char)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0xba06 + idx));
			}
			this.owner.World.TownLabels[settlementTitleIdent] = new string(townLabel);

		L00c5:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayCityLabelPrompt");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Asks the human player for a name in a box with the question at 0x53ab: the answer starts as the text that
		/// 0x19a2 + 2 * player points to, and is written back there
		/// </summary>
		public void DisplayFoundCityMessage()
		{
			this.processor.Journal.OpenScope("DisplayFoundCityMessage()");

			ushort nameSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 158, 88, 160, 32, 15);

			this.owner.Dialogs.RenderBox(158, 88, 160, 32, 11);

			this.owner.TextPainter.RenderTextToScreen0(0x53ab, 166, 90, 0);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord(nameSlot));

			PullTextInput(0xa6, 0x68, 0xd);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(nameSlot), 0xba06);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayFoundCityMessage");
		}

		/// <summary>
		/// Asks the human player for the name of their civilization in a box with the question at 0x53b8: the plural is typed
		/// (the text that 0x1992 + 2 * player points to); without its final "s" and cut to 10 characters it is also the name
		/// (0x1982 + 2 * player)
		/// </summary>
		public void DisplayCityFoundedMessage()
		{
			this.processor.Journal.OpenScope("DisplayCityFoundedMessage()");

			ushort nameSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1982);
			ushort pluralSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1992);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 158, 88, 160, 32, 15);

			this.owner.Dialogs.RenderBox(158, 88, 160, 32, 11);

			this.owner.TextPainter.RenderTextToScreen0(0x53b8, 166, 90, 0);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord(pluralSlot));

			PullTextInput(0xa6, 0x68, 0xb);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(pluralSlot), 0xba06);

			// The last character (the one before the text when it is empty)
			ushort lastAddr = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06) - 1);

			if (this.processor.PullGlobalByte(lastAddr) == 's')
			{
				this.processor.EmitGlobalByte(lastAddr, 0);
			}

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(nameSlot), 0xba06);
			this.processor.EmitGlobalByte((ushort)(this.processor.PullGlobalWord(nameSlot) + 0xa), 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayCityFoundedMessage");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayNewCityLocation()
		{
			this.processor.Journal.OpenScope("DisplayNewCityLocation()");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			DisplayLabelInputBox(0x53ce);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L02cd;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02cd;

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X - 7,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y - 6);

			goto L030e;

		L02cd:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 64, 78, 224, 10, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x53f3, 82, 80, 0);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

		L030e:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayNewCityLocation");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <returns></returns>
		public ushort DisplayLabelInputBox(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayLabelInputBox(0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 64, 78, 224, 24, 15);

			this.owner.Dialogs.RenderBox(64, 78, 224, 24, 0);

			this.owner.TextPainter.RenderTextToScreen0(charsAddr, 66, 80, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			PullTextInput(0x42, 0x59, 0x10);
			
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L03bc;

		L0390:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));

		L0393:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03b9;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x12));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x12)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0390;
			this.processor.RegAcc.LowByte = this.owner.World.Towns[this.processor.PullLocalWord(0x12)].LabelIdx;
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			if (this.processor.FlagSet.IsNotEqual) goto L0390;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			goto L040f;

		L03b9:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));

		L03bc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x100);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L040c;

			int settlementTitleIdent = this.processor.PullLocalWord(0x14);
			ushort charsDisplacement = (ushort)(this.processor.RegFrame.Value16 - 0x10);

			for (int idx = 0; idx < 13; idx++)
			{
				this.processor.Storage.EmitUInt8(this.processor.SegStack.Value16, (ushort)(charsDisplacement + idx), this.owner.World.TownLabels[settlementTitleIdent][idx]);
			}

			this.owner.Libc.TextLength(0xba06);

			this.owner.Libc.CompareTextPrefixNoCase(0xba06, (ushort)(this.processor.RegFrame.Value16 - 0x10), this.processor.RegAcc.Value16);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L03b9;
			this.processor.EmitLocalWord(0x12, 0x0);
			goto L0393;

		L040c:
			this.processor.RegAcc.Value16 = 0xffff;

		L040f:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayLabelInputBox");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth)
		{
			this.processor.Journal.OpenScope($"PullTextInput({horizLoc}, {vertLoc}, {breadth})");

			// function body
			this.processor.EnterFrame(0x48);
			this.processor.PreserveSource();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, (7 * breadth), 11, 15);

			this.owner.Dialogs.RenderBox(horizLoc, vertLoc - 1, breadth * 8 + 8, 13, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xe), 0xf);
			this.processor.EmitGlobalWord(0x68cc, 0x0);
			this.processor.EmitLocalWord(0x42, 0x1);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			goto L048e;

		L0483:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x20);
			this.processor.IncrementLocalWord(0x44);

		L048e:
			this.processor.RegAcc.Value16 = breadth;
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0483;
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

			RenderTextInputField(horizLoc, vertLoc, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x46, 0x1);
			goto L0525;

		L04b2:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L052e;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				this.processor.PullGlobalSignedWord(0x68d0),
				vertLoc + 1,
				this.processor.PullGlobalSignedWord(0x68ce), 10, 15, 11);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				this.processor.PullGlobalSignedWord(0x68d0),
				vertLoc + 1,
				this.processor.PullGlobalSignedWord(0x68ce), 10, 11, 15);

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0520;

			this.owner.TextPainter.AwaitTime(10);

		L0520:
			this.owner.Startup.ObtainMouseState();

		L0525:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L04b2;

		L052e:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L053c;
			this.processor.EmitLocalWord(0x48, 0xd);
			goto L0544;

		L053c:
			this.owner.Libc.ReadKey();

			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);

		L0544:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0555;

			this.owner.Libc.ReadKey();

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x80);
			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);

		L0555:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L0565;

		L055b:
			this.processor.RegAcc.Value16 = breadth;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			goto L069d;

		L0565:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x1b);
			if (this.processor.FlagSet.IsNotEqual) goto L0572;
			this.processor.EmitLocalWord(0x46, 0x0);
			goto L055b;

		L0572:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xcb);
			if (this.processor.FlagSet.IsNotEqual) goto L0584;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x68cc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0584;
			this.processor.EmitGlobalWord(0x68cc, this.processor.DecrementWord(this.processor.PullGlobalWord(0x68cc)));

		L0584:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xcd);
			if (this.processor.FlagSet.IsNotEqual) goto L0599;

			this.processor.RegAcc.Value16 = breadth;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x68cc));
			if (this.processor.FlagSet.IsLessOrEqual) goto L0599;
			this.processor.EmitGlobalWord(0x68cc, this.processor.IncrementWord(this.processor.PullGlobalWord(0x68cc)));

		L0599:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xd2);
			if (this.processor.FlagSet.IsNotEqual) goto L05d7;

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xba06);
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x40), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xba07);
			this.owner.Libc.CopyText(this.processor.RegAcc.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x40));

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x20);
			this.processor.RegBase.Value16 = breadth;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L05d7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xd3);
			if (this.processor.FlagSet.IsNotEqual) goto L060a;

			this.owner.Libc.CopyText((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06),
				(ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba07));

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x20);
			this.processor.RegBase.Value16 = breadth;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba05), 0x20);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L060a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L063e;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x68cc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L063e;

			this.owner.Libc.CopyText((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba05),
				(ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06));

			this.processor.RegBase.Value16 = breadth;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba05), 0x20);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);
			this.processor.EmitGlobalWord(0x68cc, this.processor.DecrementWord(this.processor.PullGlobalWord(0x68cc)));

		L063e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0682;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x20);
			if (this.processor.FlagSet.IsLess) goto L0682;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L066b;
			this.processor.EmitLocalWord(0x44, 0x0);
			goto L0663;

		L0658:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x20);
			this.processor.IncrementLocalWord(0x44);

		L0663:
			this.processor.RegAcc.Value16 = breadth;
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0658;

		L066b:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x48);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.Value16 = breadth;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0682;
			this.processor.EmitGlobalWord(0x68cc, this.processor.IncrementWord(this.processor.PullGlobalWord(0x68cc)));

		L0682:
			this.processor.EmitLocalWord(0x42, 0x0);

			RenderTextInputField(horizLoc, vertLoc, breadth);
			
			goto L0525;

		L069a:
			this.processor.EmitLocalWord(0x44, this.processor.DecrementWord(this.processor.PullLocalWord(0x44)));

		L069d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L06b4;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L06b4;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);
			goto L069a;

		L06b4:
			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("PullTextInput");

			return this.processor.RegAcc.Value16;
		}
		
		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		public void RenderTextInputField(short horizLoc, short vertLoc, ushort breadth)
		{
			this.processor.Journal.OpenScope($"RenderTextInputField({horizLoc}, {vertLoc}, {breadth})");

			// function body
			this.processor.EnterFrame(0x4);
			horizLoc += 2;
			vertLoc += 2;
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L0777;

		L06d7:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, 8, 8, 15);

			this.owner.Libc.TextLength(0xba06);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			if (this.processor.FlagSet.IsLessOrEqual) goto L0774;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba07));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba07), 0x0);

			this.owner.TextPainter.RenderText((ushort)(0xba06 + this.processor.RegBase.Value16), horizLoc, vertLoc, 0);
			
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba07), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x68cc);
			this.processor.CompareWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0759;
			this.processor.RegAcc.Value16 = (ushort)horizLoc;
			this.processor.EmitGlobalWord(0x68d0, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06));

			this.owner.VideoCard.ObtainCharWidth(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)), this.processor.RegAcc.LowByte);

			this.processor.EmitGlobalWord(0x68ce, this.processor.RegAcc.Value16);

		L0759:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06));
			
			this.owner.VideoCard.ObtainCharWidth(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)), this.processor.RegAcc.LowByte);

			horizLoc += (short)this.processor.RegAcc.Value16;

		L0774:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0777:
			this.processor.RegAcc.Value16 = breadth;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0782;
			goto L06d7;

		L0782:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RenderTextInputField");
		}
	}
}
