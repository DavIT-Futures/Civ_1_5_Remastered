using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class StartGameMenu
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public StartGameMenu(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ConfigureFreshGame()
		{
			this.processor.Journal.OpenScope("ConfigureFreshGame()");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x7);

			this.owner.Libc.CopyText(0xba06, "Difficulty Level...\n Chieftain (easiest)\n Warlord\n Prince\n King\n Emperor (toughest)\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 160, 35, 1);

			this.owner.World.HardnessTier = (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L009b;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2fa2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L009b;

			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3c, 0x87);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L009b;

			this.owner.Helpers.VerifyValueSpan(((short)this.owner.GlobDb3e - 12) / 35, 0, 4);

			this.owner.World.HardnessTier = (short)this.processor.RegAcc.Value16;

		L009b:
			if (owner.World.HardnessTier != -1) goto L00a8;
			this.owner.World.HardnessTier = 0;

		L00a8:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 150, 200, 0);

			this.processor.TestBitsByte((byte)(this.owner.World.HardnessTier & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x50, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					0x14, 0x64);
			}
			else
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x15, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					0x14, 0x64);
			}

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0x96, 0, 0xa0, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0x96, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Libc.CopyText(0xba06, "Level of Competition...\n ");

			this.processor.EmitLocalWord(0xa, 0x7);

		L0143:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xa), 10));

			this.owner.Libc.AppendText(0xba06, " Civilizations\n ");

			this.processor.DecrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L0143;

		L017c:
			this.owner.Dialogs.DisplayMenu(0xba06, 160, 35, 1);

			owner.World.BotRivalTally = (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L017c;

			this.owner.Helpers.VerifyValueSpan(6 - this.owner.World.BotRivalTally, 2, 6);

			owner.World.BotRivalTally = (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0xff;
			this.processor.RegCount.LowByte = 0x6;
			this.processor.RegCount.LowByte = this.processor.SubtractByte(this.processor.RegCount.LowByte, (byte)(this.owner.World.BotRivalTally & 0xff));
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.owner.World.LiveRealms = (short)this.processor.RegAcc.Value16;

			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.BotRivalTally;
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			goto L01e1;			

		L01d1:
			this.processor.EmitLocalWord(0xa, this.processor.DecrementWord(this.processor.PullLocalWord(0xa)));

		L01e1:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsLess) goto L021d;

			this.processor.TestBitsByte((byte)(this.owner.World.HardnessTier & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x50, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					(ushort)((this.processor.PullLocalWord(0xa) << 1) + 0x14),
					(ushort)((0x3 * this.processor.PullLocalWord(0xa)) + 0x64));
			}
			else
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x15, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					(ushort)((this.processor.PullLocalWord(0xa) << 1) + 0x14),
					(ushort)((0x3 * this.processor.PullLocalWord(0xa)) + 0x64));

			}
			goto L01d1;

		L021d:
			this.owner.World.Season = -4000;
			this.owner.World.DiagMarkers = 0xf;
			this.processor.EmitLocalWord(0xa, 0x0);

		L022e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.LowByte = 0xff;
			this.owner.World.Towns[this.processor.PullLocalWord(0xa)].LabelIdx = this.processor.RegAcc.LowByte;
			this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ConditionMarker = this.processor.RegAcc.LowByte;
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x80);
			if (this.processor.FlagSet.IsLess) goto L022e;

			for (int idx = 0; idx < 256; idx++)
			{
				this.owner.World.TownPlaces[idx].X = -1;
			}

			this.processor.EmitLocalWord(0xa, 0x0);

		L026b:
			this.owner.World.ScienceLeadingFoundVia[this.processor.PullLocalWord(0xa)] = 0;
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x48);
			if (this.processor.FlagSet.IsLess) goto L026b;
			this.processor.EmitLocalWord(0xa, 0x0);

		L0289:
			this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0xa)] = -1;

			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0289;
			this.processor.EmitLocalWord(0xa, 0x0);

		L02a2:
			this.owner.World.Participants[this.processor.PullLocalWord(0xa)].ChiefVisuals = -1;

			WipeSpaceshipContent(this.processor.PullLocalSignedWord(0xa));

			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLess) goto L02a2;

			this.processor.EmitLocalWord(0x6, 0x0);
			goto L02ea;

		L02c7:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L02ca:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02e7;

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0x8)] = 0;
			goto L02c7;

		L02e7:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L02ea:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02f7;
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L02ca;

		L02f7:
			this.owner.World.SessionPrefMarkers = 0xfa;
			this.processor.CompareByte(this.processor.PullGlobalByte(0x1a30), 0x4e);
			if (this.processor.FlagSet.IsNotEqual) goto L0309;
			this.owner.World.SessionPrefMarkers &= 0x7fef;

		L0309:
			this.owner.Libc.CopyText(0xba06, "Pick your tribe...\n ");

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L0358;

		L032f:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1502);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n ");

			this.processor.IncrementLocalWord(0xa);

		L0358:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.BotRivalTally;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L032f;
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L0392;

		L0369:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x16d2);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n ");

			this.processor.IncrementLocalWord(0xa);

		L0392:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.BotRivalTally;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0369;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0x96, 0, 0xa0, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0x96, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Dialogs.DisplayMenu(0xba06, 160, 35, 1);

			owner.World.PersonParticipantIdx = (short)this.processor.RegAcc.Value16;

			if (this.owner.World.PersonParticipantIdx <= this.owner.World.BotRivalTally)
			{
				owner.World.PersonParticipantIdx++;
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.BotRivalTally);
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
				owner.World.PersonParticipantIdx = (short)this.processor.RegAcc.Value16;
			}

			if (this.owner.World.HardnessTier == 0)
			{
				this.owner.World.SessionPrefMarkers |= 1;
			}

			// Another indexing error. Value this.oParent.GameState.HumanPlayerID is equal
			// to nationality selected and not actual ID, but later in code it is forced to 0-7 by value & 7,
			// so we will use this value instead
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx & 7].PresentInquiryIdx = -1;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(50));

			owner.World.FollowingCollectionRound += (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0xa, 0x0);

		L0421:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e), 0xffff);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222), 0x0);
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xc);
			if (this.processor.FlagSet.IsLess) goto L0421;
			this.processor.EmitLocalWord(0xa, 0x3);

		L0440:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd92a), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x5);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0440;

			if (this.owner.World.PersonParticipantIdx == 0)
				goto L0476;

			if (this.owner.World.PersonParticipantIdx <= 7)
				goto L046a;

			this.processor.RegAcc.Value16 = 0x1;
			goto L046c;

		L046a:
			this.processor.RegAcc.Value16 = 0;

		L046c:
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.owner.World.PersonParticipantIdx &= 7;
			goto L048b;

		L0476:
			this.processor.EmitLocalWord(0xc, 0xffff);
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.owner.World.BotRivalTally));
			this.processor.RegAcc.Value16++;
			this.owner.World.PersonParticipantIdx = (short)this.processor.RegAcc.Value16;

		L048b:
			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982));
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L04c2;

			this.owner.CityNaming.DisplayCityFoundedMessage();

			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals = (short)this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14e2);
			this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)), 
				this.processor.RegAcc.Value16);
			goto L0569;

		L04c2:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			}
		
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals = (short)this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14e2);
			this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)),
				this.processor.RegAcc.Value16);

			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1502);
			this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
				this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14f2);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareByte(this.processor.PullGlobalByte(0xba06), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L055b;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, "s");

		L055b:
			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)), 0xba06);

		L0569:
			this.owner.TextPainter.RenderCenteredTextToScreen0(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((ushort)(this.owner.World.PersonParticipantIdx << 1) + 0x1992)),
				46, 92, 15);

			this.processor.EmitLocalWord(0x4, 0x0);
			goto L05b6;

		L0596:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));

		L0599:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05b3;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0xa)].TroopsRuined[this.processor.PullLocalWord(0xe)] = 0;
			goto L0596;

		L05b3:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L05b6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05cd;
			
			ConstructCivilization(this.processor.PullLocalSignedWord(0x4));
			
			this.processor.EmitLocalWord(0xe, 0x0);
			goto L0599;

		L05cd:
			CharacterizeStartLocation();

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.owner.World.ParticipantMarkers = (short)this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x5;
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ResearchLevyRatio = (short)this.processor.RegAcc.Value16;

			if (this.owner.World.HardnessTier != 0) goto L05fe;
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold = 50;

		L05fe:
			owner.World.ParticipantIdentMarkers = 0;
			this.processor.EmitLocalWord(0xa, 0x0);

		L0609:
			if (this.owner.World.Participants[this.processor.PullLocalWord(0xa)].ChiefVisuals >= 8)
			{
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xa);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				owner.World.ParticipantIdentMarkers |= (short)this.processor.RegAcc.Value16;
			}
		
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0609;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 150, 0, 170, 200, 0);

			this.owner.CityNaming.DisplayFoundCityMessage();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x35c3);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0xa, 0x0);

		L06b6:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xa));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0708;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ", ");

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0708;

			this.owner.Libc.AppendText(0xba06, "\n");

		L0708:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x48);
			if (this.processor.FlagSet.IsLess) goto L06b6;

			this.owner.Libc.AppendText(0xba06, "and Roads.\n");

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			
			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x151a)), 3);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.owner.UnitOps.RenderBusyIndicator();
			
			this.processor.TestBitsByte((byte)(this.owner.World.HardnessTier & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x50, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					0x86, 0x14);
			}
			else
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0x15, (ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f,
					this.processor.PullGlobalWord(0xaa),
					0x86, 0x14);
			}

			this.owner.BlockText.RenderTextChunk(99, 88, 81, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ConfigureFreshGame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <returns></returns>
		public ushort ConstructCivilization(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ConstructCivilization({participantIdx})");

			// function body
			this.processor.EnterFrame(0x34);
			this.processor.PreserveIndexRegisters();

			if (participantIdx != this.owner.World.PersonParticipantIdx || this.owner.World.RoundTally == 0)
			{
				this.owner.World.Participants[participantIdx].StarshipInhabitants = 0;
				this.owner.World.Participants[participantIdx].RunningSagaStanding = 0;
				this.owner.World.Participants[participantIdx].FoundScienceTally = 0;
				this.owner.World.Participants[participantIdx].InquiryHeadway = 0;
				this.owner.World.Participants[participantIdx].Gold = 0;
				this.owner.World.Participants[participantIdx].RegimeKind = 1;
				this.owner.World.Participants[participantIdx].LevyRatio = 4;
				this.owner.World.Participants[participantIdx].ResearchLevyRatio = 4;

				for (int idx = 0; idx < 16; idx++)
				{
					this.processor.RegSource.Value16 = (ushort)idx;
					this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
					this.owner.World.Participants[participantIdx].Landmasses[idx].Approach = 0;
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32), 0x0);

					for (int jdx = 1; jdx < 8; jdx++)
					{
						this.processor.RegSource.Value16 = (ushort)idx;
						this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
						this.processor.RegBase.Value16 = (ushort)jdx;
						this.processor.RegCount.LowByte = 0x5;
						this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

						this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[jdx].Landmasses[idx].TownTally;

						this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32),
							this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32)), this.processor.RegAcc.Value16));
					}
				}

				this.owner.World.Participants[participantIdx].TownTally = 0;
				this.owner.World.Participants[participantIdx].TroopTally = 0;
				this.owner.World.Participants[participantIdx].SoilTally = 1;
				this.owner.World.Participants[participantIdx].ColonistTally = 1;
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = -1;

				for (int idx = 0; idx < this.owner.World.Participants[participantIdx].ScienceObtainedSince.Length; idx++)
				{
					this.owner.World.Participants[participantIdx].ScienceObtainedSince[idx] = -1;
				}

				this.processor.EmitLocalWord(0xc, 0x0);

			L08a3:
				this.processor.RegAcc.Value16 = 0x600;
				this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
				this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0xc));
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7), 0xff);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 0x0);
				this.processor.IncrementLocalWord(0xc);
				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
				if (this.processor.FlagSet.IsLess) goto L08a3;
				this.processor.RegSource.Value16 = (ushort)participantIdx;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd20c), 0);
				this.owner.World.Participants[participantIdx].Tally = 0;

				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsNotEqual) goto L08fc;

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19e8), 240, 0, 80, 50, 0);

			L08fc:
				this.owner.Bot.WipeStrategicLocations(participantIdx);
				
				this.processor.EmitLocalWord(0xc, 0x0);

			L090c:
				this.processor.RegSource.Value16 = (ushort)participantIdx;
				this.processor.RegCount.LowByte = 0x4;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

				this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
				
				this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0xc)] = 0;

				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
				this.processor.RegCount.LowByte = 0x4;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

				this.processor.RegBase.Value16 = (ushort)participantIdx;
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

				this.owner.World.Participants[this.processor.PullLocalWord(0xc)].Relations[participantIdx] = 0;

				this.processor.IncrementLocalWord(0xc);

				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
				if (this.processor.FlagSet.IsLess) goto L090c;

				this.processor.EmitLocalWord(0xc, 0);

			L0938:
				this.processor.RegAcc.Value16 = 0xa;
				this.processor.ScaleAccumulatorToSource((ushort)participantIdx);

				this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

				this.owner.World.Participants[participantIdx].FoundScienceMarkers[this.processor.PullLocalWord(0xc)] = 0;

				this.processor.IncrementLocalWord(0xc);
				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x5);
				if (this.processor.FlagSet.IsLess) goto L0938;

				if (this.owner.World.RoundTally == 0) goto L09c3;
				this.processor.EmitLocalWord(0xc, 0x0);

			L0960:
				this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
					this.processor.PullLocalSignedWord(0xc));
				this.processor.TestAccumulator();
				if (this.processor.FlagSet.IsEqual)
					goto L09ba;

				this.processor.TestBitsByte((byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullLocalWord(0xc)], 0x7);
				if (this.processor.FlagSet.IsEqual) goto L09ba;
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullLocalWord(0xc)];
				this.processor.RegAcc.HighByte = 0;
				this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
				this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.PersonParticipantIdx);
				if (this.processor.FlagSet.IsEqual) goto L09ba;

				this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

				this.processor.TestAccumulator();
				if (this.processor.FlagSet.IsNotEqual) goto L09ba;

				this.owner.Advisors.EarnTechnology(participantIdx,
					this.processor.PullLocalSignedWord(0xc),
					participantIdx);

			L09ba:
				this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x48);
				if (this.processor.FlagSet.IsLess) goto L0960;

				L09c3:
				this.processor.CompareWord((ushort)participantIdx, 0x0);
				if (this.processor.FlagSet.IsNotEqual) goto L09cc;
				goto L0e35;

			L09cc:
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)(participantIdx & 0xff);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
				if (this.processor.FlagSet.IsNotEqual) goto L09dd;
				goto L0e35;

			L09dd:
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsEqual) goto L0a59;
				if (this.owner.World.RoundTally != 0) goto L0a4f;

			L09ec:
				this.owner.World.Participants[participantIdx].ChiefVisuals = (short)this.owner.Libc.Dice.DrawInt(16);
				this.processor.EmitLocalWord(0xe, 0x1);

				if ((this.owner.World.Participants[participantIdx].ChiefVisuals & 7) != participantIdx)
				{
					this.processor.EmitLocalWord(0xe, 0x0);
				}

				this.processor.EmitLocalWord(0xc, 0x0);

			L0a1e:
				if (this.processor.PullLocalSignedWord(0xc) != participantIdx &&
					this.owner.World.Participants[this.processor.PullLocalWord(0xc)].ChiefVisuals ==
					this.owner.World.Participants[participantIdx].ChiefVisuals)
				{
					this.processor.EmitLocalWord(0xe, 0x0);
				}

				this.processor.IncrementLocalWord(0xc);
				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
				if (this.processor.FlagSet.IsLess) goto L0a1e;
				this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x0);
				if (this.processor.FlagSet.IsEqual) goto L09ec;
				goto L0a59;

			L0a4f:
				this.owner.World.Participants[participantIdx].ChiefVisuals ^= 8;

			L0a59:
				this.processor.EmitLocalWord(0x34, 0x0);

			L0a5e:
				this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(64));

				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
				this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

				this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(34));
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
				this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

				this.owner.Helpers.LocateNearestCity(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
				this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
				if (this.processor.FlagSet.IsNotEqual) goto L0a9b;
				this.processor.RegAcc.Value16 = 0x1e;
				goto L0ac7;

			L0a9b:
				this.owner.Helpers.ObtainDistance(
					this.processor.PullLocalSignedWord(0x8),
					this.owner.World.Towns[this.processor.PullLocalWord(0x6)].Place.X,
					this.processor.PullLocalSignedWord(0xa),
					this.owner.World.Towns[this.processor.PullLocalWord(0x6)].Place.Y);

			L0ac7:
				this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
				if (owner.World.RoundTally != 0) goto L0b16;
				this.processor.EmitLocalWord(0x2, 0x1);
				goto L0b0e;

			L0ad8:
				this.processor.RegAcc.Value16 = 0x600;
				this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));

				this.owner.Helpers.ObtainDistance(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
					this.processor.PullLocalSignedWord(0xa),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

				this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
				this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b0b;
				this.processor.CopyLocalWord(0x12, 0x4);

			L0b0b:
				this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

			L0b0e:
				this.processor.RegAcc.Value16 = (ushort)participantIdx;
				this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsLess) goto L0ad8;

				L0b16:
				this.owner.VideoCard.ObtainDot(2,
					this.processor.PullLocalSignedWord(0x8) + 0x50,
					this.processor.PullLocalSignedWord(0xa));

				this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
				this.processor.IncrementLocalWord(0x34);
				this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0x7d0);
				if (this.processor.FlagSet.IsLessOrEqual) goto L0b4e;
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsEqual) goto L0b44;
				goto L0c1b;

			L0b44:
				if (owner.World.RoundTally == 0) goto L0b4e;
				goto L0c1b;

			L0b4e:
				this.owner.MapOps.ObtainTerrainType(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
				if (this.processor.FlagSet.IsNotEqual) goto L0b64;
				goto L0a5e;

			L0b64:
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
				this.processor.DivideAccumulatorByPowerOfTwo(0x5);
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xc);
				this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
				this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10));
				if (this.processor.FlagSet.IsLessOrEqual) goto L0b82;
				goto L0a5e;

			L0b82:
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
				this.processor.DivideAccumulatorByPowerOfTwo(0x6);
				this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
				this.processor.RegAcc.Value16 = 0xa;
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
				this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ba2;
				goto L0a5e;

			L0ba2:
				this.owner.MapOps.ObtainContinentID(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

				this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
				this.processor.DivideAccumulatorByPowerOfTwo(0x4);
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x20);
				this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);

				this.processor.CompareWord((ushort)this.owner.World.Landmasses[this.processor.RegBase.Value16].ConstructionSpotTally, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsLess)
					goto L0a5e;

				if (this.owner.World.Season <= 0)
					goto L0bf7;

				this.owner.MapOps.ObtainContinentID(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
				this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x32)), 0x0);
				if (this.processor.FlagSet.IsEqual) goto L0bf7;
				goto L0a5e;

			L0bf7:
				this.owner.MapOps.ObtainTerrainType(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.owner.MapOps.ContainsHut(
					this.processor.RegAcc.Value16,
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.processor.TestAccumulator();
				if (this.processor.FlagSet.IsEqual) goto L0c1b;
				goto L0a5e;

			L0c1b:
				if (this.owner.World.RoundTally != 0) goto L0c49;
				this.processor.CompareWord(this.processor.PullGlobalWord(0xd76a), 0x0);
				if (this.processor.FlagSet.IsEqual) goto L0c49;
				this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].ChiefVisuals;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x35da));
				this.processor.SignExtendByte(this.processor.RegAcc);
				this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x35db));
				this.processor.SignExtendByte(this.processor.RegAcc);
				this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
				this.processor.EmitLocalWord(0x34, 0x0);

			L0c49:
				if (this.owner.World.RoundTally == 0) goto L0c8e;
				this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals, 0x8);
				if (this.processor.FlagSet.IsLess) goto L0c6f;
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)(participantIdx & 0xff);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantIdentMarkers);
				if (this.processor.FlagSet.IsEqual) goto L0c6f;
				this.processor.EmitLocalWord(0x34, 0x7d0);

			L0c6f:
				this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals, 0x8);
				if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c8e;
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)(participantIdx & 0xff);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantIdentMarkers);
				if (this.processor.FlagSet.IsNotEqual) goto L0c8e;
				this.processor.EmitLocalWord(0x34, 0x7d0);

			L0c8e:
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsEqual) goto L0cba;
				this.processor.RegSource.Value16 = (ushort)participantIdx;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.RegAcc.Value16 = 0x3a;
				this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
				this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516));
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x3);
				this.owner.World.Participants[participantIdx].ResearchLevyRatio = (short)this.processor.RegAcc.Value16;
				this.processor.RegAcc.Value16 = 0x9;
				this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx].ResearchLevyRatio;
				this.owner.World.Participants[participantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;

			L0cba:
				this.processor.RegAcc.Value16 = 0x32;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x15e);
				this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
				if (this.processor.RegAcc.Value16 < this.owner.World.RoundTally) goto L0cd3;
				if (this.processor.PullLocalWord(0x34) < 0x7d0) goto L0d12;

			L0cd3:
				if (participantIdx == this.owner.World.PersonParticipantIdx) goto L0d12;

				this.owner.World.Participants[participantIdx].ChiefVisuals ^= 8;
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)(participantIdx & 0xff);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
				owner.World.LiveRealms &= (short)this.processor.RegAcc.Value16;
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, 0x1);

				if (this.processor.RegAcc.Value16 == (ushort)this.owner.World.LiveRealms)
				{
					this.processor.EmitGlobalWord(0xdc48, 1);
					this.processor.EmitGlobalWord(0xb884, 1);
				}

				this.processor.RegAcc.Value16 = 0;
				goto L0e66;

			L0d12:
				this.processor.RegSource.Value16 = (ushort)participantIdx;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.RegAcc.Value16 = 0x3a;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
					(ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14e2);
				this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)),
					this.processor.RegAcc.Value16);

				this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsNotEqual) goto L0d3f;
				this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982));
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x0);
				if (this.processor.FlagSet.IsNotEqual) goto L0dae;

				L0d3f:
				this.processor.RegSource.Value16 = (ushort)participantIdx;
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.RegAcc.Value16 = 0x3a;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
					(ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1502);
				this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
					this.processor.RegAcc.Value16);

				this.processor.RegAcc.Value16 = 0x3a;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
					(ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
				this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14f2);
				this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

				this.processor.CompareByte(this.processor.PullGlobalByte(0xba06), 0x0);
				if (this.processor.FlagSet.IsNotEqual) goto L0d99;

				this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

				this.owner.Libc.AppendText(0xba06, "s");

			L0d99:
				this.processor.RegBase.Value16 = (ushort)participantIdx;
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
				this.owner.Libc.CopyText(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)), 0xba06);

			L0dae:
				this.owner.MapOps.MapAssignCityOwner(participantIdx,
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));

				this.owner.UnitOps.ConstructUnit(
					participantIdx,
					0,
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullLocalSignedWord(0xa));
				
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
				this.owner.World.Participants[participantIdx].HorizBegin = (short)this.processor.RegAcc.Value16;

				this.processor.EmitLocalWord(0xc, 0x0);

			L0de4:
				this.processor.RegAcc.Value16 = 0xc;
				this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
				if (this.processor.FlagSet.IsEqual) goto L0e20;

				this.owner.Helpers.ObtainDistance(
					this.processor.PullLocalSignedWord(0x8),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
					this.processor.PullLocalSignedWord(0xa),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

				this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
				if (this.processor.FlagSet.IsGreater) goto L0e20;
				
				this.owner.UnitOps.EliminateUnit(0, this.processor.PullLocalSignedWord(0xc));

			L0e20:
				this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
				this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
				if (this.processor.FlagSet.IsLess) goto L0de4;

				this.owner.World.Participants[participantIdx].FoundScienceTally = 1;

			L0e35:
				for (int idx = 0; idx < 28; idx++)
				{
					this.owner.World.Participants[participantIdx].MissingTroops[idx] = 0;
					this.owner.World.Participants[participantIdx].LiveTroops[idx] = 0;
					this.owner.World.Participants[participantIdx].TroopsInsideMaking[idx] = 0;
				}
				this.processor.RegAcc.Value16 = 0x1;
			}

		L0e66:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("ConstructCivilization");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// When the player (not 0, not the human player) has no city left: tells that the civilization was destroyed by
		/// destroyerIdx (dialog style 0x2f9e: 3 when by the human player, else 5 with relations bit 0x40 of the human player
		/// towards it, else 2), takes away its spaceship, units and everything it saw, notes it in the replay and makes a new
		/// civilization in its place
		/// </summary>
		public void EliminateCivilization(short participantIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"EliminateCivilization({participantIdx}, {participantIdx1})");

			if (participantIdx != 0 && participantIdx != this.owner.World.PersonParticipantIdx && !OwnsCity(participantIdx))
			{
				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));
				this.owner.Libc.AppendText(0xba06, "\ncivilization\ndestroyed\nby ");
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx1 << 1) + 0x1992)));
				this.owner.Libc.AppendText(0xba06, "!\n");

				short humanIdx = this.owner.World.PersonParticipantIdx;
				this.processor.EmitGlobalWord(0x2f9e, (ushort)((this.owner.World.Participants[humanIdx].Relations[participantIdx] & 0x40) != 0 ? 5 : 2));

				if (participantIdx1 == humanIdx)
				{
					this.processor.EmitGlobalWord(0x2f9e, 3);
				}

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.owner.Conquest.EliminateSpaceship(participantIdx);

				this.owner.UnitOps.AttachReplayContent(13, (byte)participantIdx, (byte)participantIdx1);

				UnitMemory troops = new UnitMemory(this.processor);

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (troops.Exists(participantIdx, troopIdx))
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}

				sbyte unseen = (sbyte)~(1 << participantIdx);

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					for (int vertLoc = 0; vertLoc < 50; vertLoc++)
					{
						this.owner.World.ChartSight[horizLoc, vertLoc] &= unseen;
					}
				}

				ConstructCivilization(participantIdx);
			}

			this.processor.Journal.CloseScope("EliminateCivilization");
		}

		/// <summary>True when one of the 128 cities belongs to the player</summary>
		private bool OwnsCity(short participantIdx)
		{
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker != 0xff && this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					return true;
			}

			return false;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <returns></returns>
		public ushort CharacterizeStartLocation()
		{
			this.processor.Journal.OpenScope("CharacterizeStartLocation()");

			// function body
			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x26, 0x0);

		L0ff3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x46), 0x0);
			this.processor.IncrementLocalWord(0x26);
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0x10);
			if (this.processor.FlagSet.IsLess) goto L0ff3;
			this.processor.EmitLocalWord(0x26, 0x0);
			this.processor.EmitLocalWord(0x4, 0x1);

		L1010:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1050;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegFrame.Value16);
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, 0x5a);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegFrame.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 - 0x46), this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 - 0x46))));

		L1050:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1010;
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x1);
			goto L1206;

		L1069:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1084;

			this.owner.Libc.AppendText(0xba06, "moderate");

			this.processor.IncrementLocalWord(0x6);
			goto L10b5;

		L1084:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x3e7);
			if (this.processor.FlagSet.IsEqual) goto L10a1;

			this.owner.Libc.AppendText(0xba06, "far away");

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x2));
			goto L10b5;

		L10a1:
			this.owner.Libc.AppendText(0xba06, "isolated");

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x4));

		L10b5:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);

		L10c3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, this.processor.PullLocalWord(0x4));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x26));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1101;
			this.processor.IncrementLocalWord(0x20);

		L1101:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L110a;
			this.processor.IncrementLocalWord(0x2);

		L110a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L1113;
			this.processor.IncrementLocalWord(0x1e);

		L1113:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L10c3;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1127;
			this.processor.RegAcc.Value16 = 0x3653;
			goto L112a;

		L1127:
			this.processor.RegAcc.Value16 = 0x365c;

		L112a:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1153;

			this.owner.Libc.AppendText(0xba06, "river valley on a ");

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x2));
			goto L117e;

		L1153:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3);
			if (this.processor.FlagSet.IsLess) goto L116e;

			this.owner.Libc.AppendText(0xba06, "grasslands on a ");

			this.processor.IncrementLocalWord(0x6);
			goto L117e;

		L116e:
			this.owner.Libc.AppendText(0xba06, "plains on a ");

		L117e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));

			this.owner.MapOps.ObtainLandMassSize(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L11da;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L11b4;
			goto L1317;

		L11b4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L11bc;
			goto L131d;

		L11bc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L11c4;
			goto L1333;

		L11c4:
			this.owner.Libc.AppendText(0xba06, "large continent.");

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x2));
			goto L11ea;

		L11da:
			this.owner.Libc.AppendText(0xba06, 0x3695);

		L11ea:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L11f8;
			this.processor.CopyLocalWord(0x6, 0x24);

		L11f8:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), this.processor.RegAcc.Value16);

		L1203:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L1206:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L120f;
			goto L1338;

		L120f:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x26));
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1203;
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0xa, 0x3e7);
			this.processor.EmitLocalWord(0x48, 0x1);

		L1241:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L12c7;
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x48));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L12c7;
			this.processor.ScaleIndex(0x600, this.processor.PullLocalWord(0x4));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x26));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x5e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5e));
			if (this.processor.FlagSet.IsNotEqual) goto L12c7;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L12c7;
			this.processor.CopyLocalWord(0x8, 0xa);

		L12c7:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L12d3;
			goto L1241;

		L12d3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xa);
			if (this.processor.FlagSet.IsLess) goto L12eb;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x3e7);
			if (this.processor.FlagSet.IsEqual) goto L12eb;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0xc, this.processor.BitOrWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16));

		L12eb:
			this.owner.Libc.CopyText(0xba06, "Nearest civilization: ");

			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xa);
			if (this.processor.FlagSet.IsLess) goto L1304;
			goto L1069;

		L1304:
			this.owner.Libc.AppendText(0xba06, "close");
			goto L10b5;

		L1317:
			this.owner.Libc.AppendText(0xba06, 0x36a3);

			goto L11ea;

		L131d:
			this.owner.Libc.AppendText(0xba06, 0x36b1);

		L1320:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			goto L11ea;

		L1333:
			this.owner.Libc.AppendText(0xba06, 0x36c2);

			goto L1320;

		L1338:
			this.processor.EmitLocalWord(0x5c, 0xffff);
			this.processor.EmitLocalWord(0x4, 0x1);
			goto L13ca;

		L1345:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L135b;

			this.owner.Libc.AppendText(0xba06, "+");

		L135b:
			this.owner.Advisors.UncoverTechnology(this.processor.PullLocalSignedWord(0x4), 0);

			this.processor.RegCount.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x48);

		L1381:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c)));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x48));
			if (this.processor.FlagSet.IsGreater) goto L1345;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L13c7;

			this.owner.Libc.AppendText(0xba06, "NONE");

			this.processor.CompareWord(this.processor.PullLocalWord(0x5c), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L13c1;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			if (this.processor.FlagSet.IsEqual) goto L13c7;

		L13c1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x5c, this.processor.RegAcc.Value16);

		L13c7:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L13ca:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L144c;
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x4));
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x26));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L13c7;
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Skills: ");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegFrame.Value16);
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, 0x1c);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16));
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1444;
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16), 0x3));
			
			this.owner.UnitOps.ConstructUnit(
				this.processor.PullLocalSignedWord(0x4),
				0,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.owner.Libc.AppendText(0xba06, "Settlers+");

		L1444:
			this.processor.EmitLocalWord(0x48, 0x0);
			goto L1381;

		L144c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5c);
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("CharacterizeStartLocation");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayTribeSelection()
		{
			this.processor.Journal.OpenScope("DisplayTribeSelection()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.QueryForDisk(7, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x36ff, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0xa0, 0x32, 0xa0, 0x96,
				this.processor.PullGlobalWord(0x19e8),
				0xa0, 0x32);

			this.processor.EmitLocalWord(0x4, 0x0);

		L14b1:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((0x2a * this.processor.PullLocalWord(0x4)) + 0xa0),
				0x8e, 0x29, 0x3a);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdf62), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x3);
			if (this.processor.FlagSet.IsLess) goto L14b1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L14ef;
			goto L1637;

		L14ef:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3709, 0);

			this.processor.EmitLocalWord(0x4, 0x0);
			goto L156c;

		L1506:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb884));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb886), this.processor.RegAcc.Value16);

		L151c:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L151f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x10);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1569;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1532;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1538;

		L1532:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1506;

		L1538:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x6) << 4),
				(ushort)(this.processor.PullLocalWord(0x4) << 4),
				0x10, 0x10);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb886), this.processor.RegAcc.Value16);
			goto L151c;

		L1569:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L156c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1579;
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L151f;

		L1579:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L15ec;

		L1580:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x4) << 4) - ((this.processor.PullLocalWord(0x6) & 1) << 3)) + 8),
				0xb8, 8, 8);

		L159f:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xd294), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x6);

		L15bf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L15e9;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1580;

			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x6) & 1) << 3) + (this.processor.PullLocalWord(0x4) << 4)),
				0xb0, 8, 8);
			goto L159f;

		L15e9:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L15ec:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L15f9;
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L15bf;

		L15f9:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L1603;

		L1600:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L1603:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x4);
			if (this.processor.FlagSet.IsLess) goto L160c;
			goto L16b3;

		L160c:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((this.processor.PullLocalWord(0x4) << 4) + 0x80),
				0xb0, 0x10, 0x10);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd2d4), this.processor.RegAcc.Value16);
			goto L1600;

		L1637:
			this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3714, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19fc),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitLocalWord(0x4, 0x0);

		L166c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.owner.VideoCard.ScreenToPicture(3, this.processor.RegSource.Value16, 0, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb886), this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(3, this.processor.RegSource.Value16, 0x10, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb888), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xc);
			if (this.processor.FlagSet.IsLess) goto L166c;

		L16b3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L16f5;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3720, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x372a, 0xbdee);

			this.processor.EmitLocalWord(0x4, 0x0);

		L16df:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.RegBase.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbdf4));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6b34), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x30);
			if (this.processor.FlagSet.IsLess) goto L16df;

		L16f5:
			this.processor.EmitLocalWord(0x4, 0x0);

		L16fa:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			
			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 0x30, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb27a), this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(1, (ushort)(this.processor.RegSource.Value16 + 0x80), 0x60, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb29a), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L16fa;
			
			this.owner.VideoCard.ScreenToPicture(1, 0x30, 0x20, 0x10, 0x10);

			this.processor.EmitGlobalWord(0xb2ba, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x1);

		L1768:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			
			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 0x40, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x6dfe), this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 0x50, 0x10, 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x6e1e), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x10);
			if (this.processor.FlagSet.IsLess) goto L1768;
			this.processor.EmitLocalWord(0x4, 0x0);

		L17b6:
			this.processor.RegAcc.Value16 = 0x7;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xa0);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 0, 7, 15);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xd21c), this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 16, 7, 15);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xd244), this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, 0x20, 7, 15);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xd26c), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x14);
			if (this.processor.FlagSet.IsLess) goto L17b6;
			this.processor.EmitLocalWord(0x4, 0x0);

		L182b:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((this.processor.PullLocalWord(0x4) << 4) + 0x50),
				0x80, 0x10, 0x10);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7eec), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x4);
			if (this.processor.FlagSet.IsLess) goto L182b;
			this.processor.EmitLocalWord(0x4, 0x0);

		L1862:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x4) << 4),
				0x20, 16, 16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ce), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x7);
			if (this.processor.FlagSet.IsLess) goto L1862;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6e92);
			this.processor.EmitGlobalWord(0xd4dc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x0);

		L189c:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x4) & 3) << 3) + 0x81),
				(ushort)(((this.processor.PullLocalWord(0x4) & 4) << 1) + 0x21),
				7, 7);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4de), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L189c;
			this.processor.EmitLocalWord(0x4, 0x0);

		L18de:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(this.processor.PullLocalWord(0x4) << 3),
				0x80, 8, 16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6e96), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x9);
			if (this.processor.FlagSet.IsLess) goto L18de;
			this.processor.EmitLocalWord(0x4, 0x0);

		L1915:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((this.processor.PullLocalWord(0x4) << 4) + 1),
				0x71, 15, 15);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ee), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x10);
			if (this.processor.FlagSet.IsLess) goto L1915;
			this.processor.EmitLocalWord(0x4, 0x0);

		L194a:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((this.processor.PullLocalWord(0x4) << 4) + 1),
				0x61, 15, 15);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd50e), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L194a;
			
			this.owner.VideoCard.ScreenToPicture(1, 0x99, 0x29, 7, 7);

			this.processor.EmitGlobalWord(0xb880, this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.ScreenToPicture(1, 0xb0, 0x80, 0x14, 9);

			this.processor.EmitGlobalWord(0xdf0c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L1a0b;

		L19bc:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L19bf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1a08;
			
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x6) % 0x14) << 4) + 1),
				(ushort)(((this.processor.PullLocalWord(0x6) / 0x14) << 4) + 0x81),
				15, 15);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x6));
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd54e), this.processor.RegAcc.Value16);
			goto L19bc;

		L1a08:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L1a0b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1a14;
			goto L1aeb;

		L1a14:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0xa0, 0x140, 0x20,
				this.processor.PullGlobalWord(0x19d4),
				0, 0x80);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L1a69;
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 15, 11);

		L1a69:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1a98;
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 7, 3);

		L1a98:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 <<= 1;
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 10,
				(byte)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 2,
				(byte)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1956)));

			this.processor.EmitLocalWord(0x6, 0x0);
			goto L19bf;

		L1aeb:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTribeSelection");
		}

		/// <summary>
		/// Loads the government picture of the human player, "govtNa.pic": N is the government / 2 (3 for government 3), "m"
		/// instead of "a" with technology 20; the leader part of it goes into the picture 0x19e8. The file name is made in a
		/// buffer in the stack frame, like in the original.
		/// </summary>
		public void ImportGovernmentGraphics()
		{
			this.processor.Journal.OpenScope("ImportGovernmentGraphics()");

			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(2, 1);

			ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			this.owner.Libc.CopyText(docname, "govt0a.pic");

			ushort regime = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)(((short)regime / 2) + 0x30));

			if (regime == 3)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 4), 0x33);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docname, 0);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0xa0, 0x3c,
				this.processor.PullGlobalWord(0x19e8), 0xa0, 0x8c);

			this.owner.Startup.QueryForDisk(0, 1);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ImportGovernmentGraphics");
		}

		/// <summary>
		/// After a revolution of the human player (not to government 0): the newspaper "... government changed to ...!", then
		/// the advisor screen with the title 0x375a and the 4 advisors (pictures from 0x19e8, names at 0x2fac without their
		/// last character, every second one lower) until a key or click
		/// </summary>
		public void DisplayRevolutionScreen()
		{
			this.processor.Journal.OpenScope("DisplayRevolutionScreen()");

			short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;

			if (regime != 0)
			{
				this.owner.Advisors.RenderAdvisorScenery();

				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1982)));
				this.owner.Libc.AppendText(0xba06, " government\nchanged to ");
				this.owner.Libc.AppendText(0xba06,
					this.processor.PullGlobalWord((ushort)((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind << 1) + 0x1966)));
				this.owner.Libc.AppendText(0xba06, "!\n");

				this.owner.Paper.DisplayNewspaper(-2);

				this.owner.Startup.ConcealMouseCursor();

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 100, 320, 100, 15);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x6);

				this.owner.TextPainter.RenderCenteredTextToScreen0(0x375a, 160, 102, 0);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x2);

				for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
				{
					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), (ushort)((0x28 * advisorIdx) + 0xa0), 0x8c, 0x28, 0x3c,
						this.processor.PullGlobalWord(0xaa), (ushort)((0x50 * advisorIdx) + 0x14), 0x76);

					this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((advisorIdx << 1) + 0x2fac)));
					this.processor.EmitGlobalByte((ushort)(this.owner.Libc.TextLength(0xba06) + 0xba05), 0x0);

					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, (80 * advisorIdx) + 40, (advisorIdx & 1) != 0 ? 186 : 180, 0);
				}

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

				this.owner.Startup.DisplayMouseCursor();

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.Advisors.RecoverAdvisorScenery();
			}

			this.processor.Journal.CloseScope("DisplayRevolutionScreen");
		}

		/// <summary>
		/// The player has no spaceship: no arrival, every place of the spaceship layout (15 rows of 12, segment 0x3772) empty
		/// </summary>
		public void WipeSpaceshipContent(short participantIdx)
		{
			this.processor.Journal.OpenScope($"WipeSpaceshipContent({participantIdx})");

			this.owner.World.Participants[participantIdx].StarshipArrivalSeason = -1;

			this.processor.SegExtra.Value16 = 0x3772;

			for (int row = 0; row < 0xf; row++)
			{
				for (int column = 0; column < 0xc; column++)
				{
					this.processor.EmitUInt8(this.processor.SegExtra.Value16,
						(ushort)((0xb4 * participantIdx) + (0xc * row) + column + 0x16e0), 0xff);
				}
			}

			this.processor.Journal.CloseScope("WipeSpaceshipContent");
		}
	}
}
