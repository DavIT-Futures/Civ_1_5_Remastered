using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class InGameHelp
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public InGameHelp(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpTopic(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpTopic(0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x18);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x18), "*");

			this.owner.Libc.UppercaseText(charsAddr);

			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x18), this.processor.RegAcc.Value16);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.ImportTextFromFile(0x34b2, (ushort)(this.processor.RegFrame.Value16 - 0x18));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L00a5;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 2);

			this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(this.processor.RegFrame.Value16 - 0x18), 160, 192, 0);

			this.owner.BlockText.RenderTextChunk(32, 32, 32, 15);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

		L00a5:
			this.processor.EmitGlobalWord(0x2f9c, 0x0);
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpTopic");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="param2"></param>
		public void DisplayTileInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayTileInfo({participantIdx}, {arg2})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(arg2, 0x80);
			if (this.processor.FlagSet.IsLess) goto L00c1;
			goto L02cd;

		L00c1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, arg2);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x13, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x291));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x290));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L0108;
			this.processor.RegAcc.Value16 = 0x1;
			goto L010a;

		L0108:
			this.processor.RegAcc.Value16 = 0;

		L010a:
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) + 80,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0135;
			this.processor.EmitLocalWord(0x2, 0x0);

		L0135:
			this.processor.EmitLocalWord(0x8, 0x1);

		L013a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, arg2);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(0x81d5 + this.processor.RegDest.Value16)) +
					this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(0x1882 + this.processor.RegSource.Value16)) + 80,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(0x81d6 + this.processor.RegDest.Value16)) +
					this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			if (this.processor.FlagSet.IsLessOrEqual) goto L017e;
			this.processor.EmitLocalWord(0x2, 0x0);

		L017e:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L013a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L01c3;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, arg2);

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) - 7,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) - 6);

			DisplayHelpPopup(0x34bb);

			goto L02cd;

		L01c3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, arg2);

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L01f2;
			goto L02cd;

		L01f2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0207;
			goto L02cd;

		L0207:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0211;
			goto L02cd;

		L0211:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x4));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L025f;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xa), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L025f;

			DisplayHelpPopup(0x34c6);

		L025f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0298;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, arg2);

			this.owner.TurnLogic.VerifyCoastalTile(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0298;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xa), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L0298;

			DisplayHelpPopup(0x34ce);

			goto L02cd;

		L0298:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L02cd;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xa), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L02cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xb);
			if (this.processor.FlagSet.IsEqual) goto L02cd;
			this.processor.ScaleIndexToBase(0x38, (ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[0] >= 2)
				goto L02cd;

			DisplayHelpPopup(0x34d8);

		L02cd:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTileInfo");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpPopup(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpPopup(0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveSource();

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L02e4;
			goto L03a5;

		L02e4:
			if (this.owner.World.Season < 0) goto L02ee;
			goto L03a5;

		L02ee:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.ImportTextFromFile(0x34de, charsAddr);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.BlockText.ReflowText();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.ScaleIndexToSource(0x6, this.processor.PullLocalWord(0x2));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xa);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 56, 16, 208, (short)this.processor.RegSource.Value16, 2);

			this.owner.Dialogs.RenderBox(56, 16, 208, (short)this.processor.RegSource.Value16, 10);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x34e7, 160, 19, 0);

			this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

		L03a5:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayHelpPopup");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpPopupForHuman(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpPopupForHuman(0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveSource();
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b90), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L03bd;
			goto L0474;

		L03bd:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.ImportTextFromFile(0x3501, charsAddr);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.BlockText.ReflowText();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.ScaleIndexToSource(0x6, this.processor.PullLocalWord(0x2));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xa);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 56, 16, 208, (short)this.processor.RegSource.Value16, 4);

			this.owner.Dialogs.RenderBox(56, 16, 208, (short)this.processor.RegSource.Value16, 12);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x350b, 160, 19, 0);

			this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

		L0474:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpPopupForHuman");
		}
	}
}
