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
		/// Rates the start of every player 1 - 7 by his first unit (settlers): the nearest civilization on the same continent
		/// (close under 10, moderate under 20 +1, far away +2, isolated +4), river valley (more than one river tile +2) or
		/// grasslands (3 grassland tiles +1), and the continent size (/ 50: 2 and 3 +1, more +2). The players behind the best
		/// get compensation: more than 3 points behind another settlers (+3), then a technology per point (UncoverTechnology).
		/// Returns the first player with the best start, or the last of them not too close to another civilization. The
		/// descriptions in 0xba06 are never shown. The tables of the continents and the points are in the stack frame and SI / DI
		/// get the values of the original before the calls, like in the original.
		/// </summary>
		public ushort CharacterizeStartLocation()
		{
			this.processor.Journal.OpenScope("CharacterizeStartLocation()");

			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();

			ushort frame = this.processor.RegFrame.Value16;
			ushort settlersPerContinent = (ushort)(frame - 0x46);
			ushort continents = (ushort)(frame - 0x5a);
			ushort points = (ushort)(frame - 0x1c);

			this.processor.EmitLocalWord(0x26, 0);

			for (ushort continentIdx = 0; continentIdx < 0x10; continentIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(continentIdx << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(settlersPerContinent + (continentIdx << 1)), 0);
			}

			ushort participantIdx;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.RegDest.Value16 = (ushort)((participantIdx << 1) + frame - 0x5a);

				this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				ushort continent = this.processor.RegAcc.Value16;
				this.processor.EmitGlobalWord((ushort)(continents + (participantIdx << 1)), continent);

				ushort counter = (ushort)((continent << 1) + frame - 0x46);
				this.processor.EmitGlobalWord(counter, (ushort)(this.processor.PullGlobalWord(counter) + 1));
			}

			ushort scattered = 0;
			short best = 0;

			this.processor.EmitLocalWord(0xc, scattered);
			this.processor.EmitLocalWord(0x24, (ushort)best);

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.processor.EmitLocalWord(0x4, participantIdx);

				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.EmitLocalWord(0x22, (ushort)((0x1c * participantIdx) - 0x18));

				short nearest = NearestCivilization(participantIdx, troop);

				if (nearest >= 10 && nearest != 0x3e7)
				{
					scattered |= (ushort)(1 << (byte)participantIdx);
					this.processor.EmitLocalWord(0xc, scattered);
				}

				short start = RateStartLocation(participantIdx, troop, nearest);

				if (start > best)
				{
					best = start;
					this.processor.EmitLocalWord(0x24, (ushort)best);
				}

				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(points + (participantIdx << 1)), (ushort)start);
			}

			short chosen = -1;

			this.processor.EmitLocalWord(0x5c, 0xffff);

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.processor.EmitLocalWord(0x4, participantIdx);

				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.EmitLocalWord(0x22, (ushort)((0x1c * participantIdx) - 0x8));

				this.owner.Libc.CopyText(0xba06, "Skills: ");

				ushort playerPoints = (ushort)((participantIdx << 1) + frame - 0x1c);
				this.processor.RegDest.Value16 = playerPoints;

				if ((short)(best - this.processor.PullGlobalWord(playerPoints)) > 3)
				{
					this.processor.EmitGlobalWord(playerPoints, (ushort)(this.processor.PullGlobalWord(playerPoints) + 3));

					this.owner.UnitOps.ConstructUnit((short)participantIdx, 0, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)),
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

					this.owner.Libc.AppendText(0xba06, "Settlers+");
				}

				ushort given;

				for (given = 0; ; given++)
				{
					this.processor.EmitLocalWord(0x48, given);
					this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

					if ((short)(best - this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(points + (participantIdx << 1)))) <= (short)given)
						break;

					if (given != 0)
						this.owner.Libc.AppendText(0xba06, "+");

					this.owner.Advisors.UncoverTechnology((short)participantIdx, 0);

					this.owner.Libc.AppendText(0xba06, (ushort)((ushort)(this.processor.RegAcc.Value16 * 0x16) + 0x4da));
				}

				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

				if ((short)this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(points + (participantIdx << 1))) != best)
					continue;

				this.owner.Libc.AppendText(0xba06, "NONE");

				if (chosen == -1 || (scattered & (1 << (byte)participantIdx)) != 0)
				{
					chosen = (short)participantIdx;
					this.processor.EmitLocalWord(0x5c, (ushort)chosen);
				}
			}

			this.processor.EmitLocalWord(0x4, participantIdx);

			this.processor.RegAcc.Value16 = (ushort)chosen;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("CharacterizeStartLocation");

			return (ushort)chosen;
		}

		/// <summary>
		/// The distance from the player's first unit to the nearest first unit of another player on the same continent, 0x3e7
		/// for none (the locals 0x5e, 0x8, 0xa, 0x48 as in the original)
		/// </summary>
		private short NearestCivilization(ushort participantIdx, ushort troop)
		{
			short nearest = 0x3e7;
			ushort otherIdx;

			this.processor.EmitLocalWord(0x6, 0);
			this.processor.EmitLocalWord(0xa, (ushort)nearest);

			for (otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				this.processor.EmitLocalWord(0x48, otherIdx);

				if (otherIdx == participantIdx)
					continue;

				ushort other = (ushort)(0x600 * otherIdx);
				this.processor.RegSource.Value16 = other;

				if (this.processor.PullGlobalByte((ushort)(other + 0x81d7)) == 0xff)
					continue;

				this.processor.RegDest.Value16 = troop;

				this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d6)));

				ushort otherContinent = this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0x5e, otherContinent);

				if (this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6))) != otherContinent)
					continue;

				this.owner.Helpers.ObtainDistance(
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d5)),
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d6)));

				short distance = (short)this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0x8, (ushort)distance);

				if (distance < nearest)
				{
					nearest = distance;
					this.processor.EmitLocalWord(0xa, (ushort)nearest);
				}
			}

			this.processor.EmitLocalWord(0x48, otherIdx);

			return nearest;
		}

		/// <summary>The points of the player's start (see CharacterizeStartLocation); the description goes into 0xba06</summary>
		private short RateStartLocation(ushort participantIdx, ushort troop, short nearest)
		{
			short start = 0;

			this.owner.Libc.CopyText(0xba06, "Nearest civilization: ");

			if (nearest < 10)
			{
				this.owner.Libc.AppendText(0xba06, "close");
			}
			else if (nearest < 0x14)
			{
				this.owner.Libc.AppendText(0xba06, "moderate");
				start++;
			}
			else if (nearest != 0x3e7)
			{
				this.owner.Libc.AppendText(0xba06, "far away");
				start += 2;
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, "isolated");
				start += 4;
			}

			ushort ocean = 0, grassland = 0, river = 0;
			ushort neighbourIdx;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(neighbourIdx << 1);
				this.processor.RegDest.Value16 = troop;

				ushort terrain = this.owner.MapOps.ObtainTerrainType(
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)) + (short)this.processor.PullGlobalWord((ushort)((neighbourIdx << 1) + 0x1882)),
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)) + (short)this.processor.PullGlobalWord((ushort)((neighbourIdx << 1) + 0x18e4)));

				this.processor.EmitLocalWord(0x4a, terrain);

				if (terrain == 0xa)
					ocean++;

				if (terrain == 2)
					grassland++;

				if (terrain == 0xb)
					river++;
			}

			this.processor.EmitLocalWord(0x48, neighbourIdx);
			this.processor.EmitLocalWord(0x20, ocean);
			this.processor.EmitLocalWord(0x2, grassland);
			this.processor.EmitLocalWord(0x1e, river);

			this.owner.Libc.CopyText(0xba06, ocean != 0 ? (ushort)0x3653 : (ushort)0x365c);

			if ((short)river > 1)
			{
				this.owner.Libc.AppendText(0xba06, "river valley on a ");
				start += 2;
			}
			else if ((short)grassland >= 3)
			{
				this.owner.Libc.AppendText(0xba06, "grasslands on a ");
				start++;
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, "plains on a ");
			}

			this.processor.RegSource.Value16 = troop;

			this.owner.MapOps.ObtainLandMassSize((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			switch (CPU.SignedQuotient((short)this.processor.RegAcc.Value16, 0x32))
			{
				case 0:
					this.owner.Libc.AppendText(0xba06, 0x3695);
					break;

				case 1:
					this.owner.Libc.AppendText(0xba06, 0x36a3);
					break;

				case 2:
					this.owner.Libc.AppendText(0xba06, 0x36b1);
					start++;
					break;

				case 3:
					this.owner.Libc.AppendText(0xba06, 0x36c2);
					start++;
					break;

				default:
					this.owner.Libc.AppendText(0xba06, "large continent.");
					start += 2;
					break;
			}

			this.processor.EmitLocalWord(0x6, (ushort)start);

			return start;
		}

		/// <summary>
		/// Despite the name: cuts the sprites of the game out of the sprite sheets into picture tables (the order of the
		/// ScreenToPicture calls gives the picture numbers, like in the original). The three tribe portraits (0xdf62); with VGA
		/// colours (0xd762) the unit sprites of 0x3709 (10 rows of 16, 0xb886; rows 6 - 9, and all rows with 0x1a3e, only
		/// column 0 cut and copied to the others), the quarter tiles (0xd294) and 4 tiles (0xd2d4), else the EGA unit sprites
		/// of 0x3714 (12 in two rows); with VGA its palette part (0x30 bytes to 0x6b34). Then the terrain and map pieces of
		/// sheet 0x36ff: tiles, rivers, specials, city walls, numbers, cursors and the 8 coloured rows of city sprites (0xd54e),
		/// coloured per player with the colours 0x1946 / 0x1956.
		/// </summary>
		public void DisplayTribeSelection()
		{
			this.processor.Journal.OpenScope("DisplayTribeSelection()");

			this.owner.Startup.QueryForDisk(7, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x36ff, 0);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0xa0, 0x32, 0xa0, 0x96,
				this.processor.PullGlobalWord(0x19e8), 0xa0, 0x32);

			for (int idx = 0; idx < 3; idx++)
			{
				StorePicture(0xdf62, idx, 1, (ushort)((0x2a * idx) + 0xa0), 0x8e, 0x29, 0x3a);
			}

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				CutVgaUnitSprites();
			}
			else
			{
				CutEgaUnitSprites();
			}

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3720, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x372a, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.processor.EmitGlobalByte((ushort)(0x6b34 + idx), this.processor.PullGlobalByte((ushort)(0xbdf4 + idx)));
				}
			}

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xb27a, idx, 1, (ushort)(idx << 4), 0x30, 0x10, 0x10);
				StorePicture(0xb29a, idx, 1, (ushort)((idx << 4) + 0x80), 0x60, 0x10, 0x10);
			}

			StorePicture(0xb2ba, 0, 1, 0x30, 0x20, 0x10, 0x10);

			for (int idx = 1; idx < 0x10; idx++)
			{
				StorePicture(0x6dfe, idx, 1, (ushort)(idx << 4), 0x40, 0x10, 0x10);
				StorePicture(0x6e1e, idx, 1, (ushort)(idx << 4), 0x50, 0x10, 0x10);
			}

			for (int idx = 0; idx < 0x14; idx++)
			{
				ushort horizLoc = (ushort)((7 * idx) + 0xa0);

				StorePicture(0xd21c, idx, 1, horizLoc, 0, 7, 15);
				StorePicture(0xd244, idx, 1, horizLoc, 0x10, 7, 15);
				StorePicture(0xd26c, idx, 1, horizLoc, 0x20, 7, 15);
			}

			for (int idx = 0; idx < 4; idx++)
			{
				StorePicture(0x7eec, idx, 1, (ushort)((idx << 4) + 0x50), 0x80, 0x10, 0x10);
			}

			for (int idx = 0; idx < 7; idx++)
			{
				StorePicture(0xd4ce, idx, 1, (ushort)(idx << 4), 0x20, 0x10, 0x10);
			}

			this.processor.EmitGlobalWord(0xd4dc, this.processor.PullGlobalWord(0x6e92));

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xd4de, idx, 1, (ushort)(((idx & 3) << 3) + 0x81), (ushort)(((idx & 4) << 1) + 0x21), 7, 7);
			}

			for (int idx = 0; idx < 9; idx++)
			{
				StorePicture(0x6e96, idx, 1, (ushort)(idx << 3), 0x80, 8, 0x10);
			}

			for (int idx = 0; idx < 0x10; idx++)
			{
				StorePicture(0xd4ee, idx, 1, (ushort)((idx << 4) + 1), 0x71, 15, 15);
			}

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xd50e, idx, 1, (ushort)((idx << 4) + 1), 0x61, 15, 15);
			}

			StorePicture(0xb880, 0, 1, 0x99, 0x29, 7, 7);
			StorePicture(0xdf0c, 0, 1, 0xb0, 0x80, 0x14, 9);

			for (int playerIdx = 0; playerIdx < 8; playerIdx++)
			{
				CutPlayerCitySprites(playerIdx);
			}

			this.processor.Journal.CloseScope("DisplayTribeSelection");
		}

		/// <summary>
		/// Cuts a picture out of the screen into the table at tableAddr, entry idx
		/// </summary>
		private void StorePicture(ushort tableAddr, int idx, ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			ushort picture = this.owner.VideoCard.ScreenToPicture(displayIdx, horizLoc, vertLoc, breadth, altitude);

			this.processor.EmitGlobalWord((ushort)(tableAddr + (idx << 1)), picture);
		}

		/// <summary>
		/// The VGA unit sprites (sheet 0x3709, 10 rows of 16 at 0xb886, 0x20 bytes a row), the quarter tiles (8 of 4 at 0xd294)
		/// and 4 tiles (0xd2d4)
		/// </summary>
		private void CutVgaUnitSprites()
		{
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3709, 0);

			for (int row = 0; row < 0xa; row++)
			{
				for (int column = 0; column < 0x10; column++)
				{
					ushort entry = (ushort)(0xb886 + (row << 5) + (column << 1));

					// Rows 6 - 9 (and all rows with 0x1a3e) have one sprite, repeated in every column
					if ((row >= 6 || this.processor.PullGlobalWord(0x1a3e) != 0) && column != 0)
					{
						this.processor.EmitGlobalWord(entry, this.processor.PullGlobalWord((ushort)(entry - 2)));
					}
					else
					{
						this.processor.EmitGlobalWord(entry,
							this.owner.VideoCard.ScreenToPicture(1, (ushort)(column << 4), (ushort)(row << 4), 0x10, 0x10));
					}
				}
			}

			for (int tile = 0; tile < 8; tile++)
			{
				for (int quarter = 0; quarter < 4; quarter++)
				{
					// The upper quarters at row 0xb0, the lower ones at 0xb8 (swapped left and right)
					ushort horizLoc = quarter < 2
						? (ushort)(((quarter & 1) << 3) + (tile << 4))
						: (ushort)(((tile << 4) - ((quarter & 1) << 3)) + 8);
					ushort vertLoc = (ushort)(quarter < 2 ? 0xb0 : 0xb8);

					StorePicture((ushort)(0xd294 + (tile << 3)), quarter, 1, horizLoc, vertLoc, 8, 8);
				}
			}

			for (int idx = 0; idx < 4; idx++)
			{
				StorePicture(0xd2d4, idx, 1, (ushort)((idx << 4) + 0x80), 0xb0, 0x10, 0x10);
			}
		}

		/// <summary>
		/// The EGA unit sprites: sheet 0x3714 on screen 3 (copied to the back screen), 12 sprites in two rows (0xb886 and 0xb888,
		/// 0x20 bytes apart)
		/// </summary>
		private void CutEgaUnitSprites()
		{
			this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3714, 1);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19fc), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4), 0, 0);

			for (int idx = 0; idx < 0xc; idx++)
			{
				StorePicture(0xb886, idx << 4, 3, (ushort)(idx << 4), 0, 0x10, 0x10);
				StorePicture(0xb888, idx << 4, 3, (ushort)(idx << 4), 0x10, 0x10, 0x10);
			}
		}

		/// <summary>
		/// The city sprites of a player (0x1c of them at 0xd54e + 0x40 * player): the city row of the sheet copied to row 0x80 of
		/// the back screen, white (15) to 11 and grey (7) to 3 for those player colours, 10 and 2 to the colours of the player
		/// (0x1946, 0x1956)
		/// </summary>
		private void CutPlayerCitySprites(int playerIdx)
		{
			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0xa0, 0x140, 0x20,
				this.processor.PullGlobalWord(0x19d4), 0, 0x80);

			ushort colours = (ushort)(playerIdx << 1);

			if (this.processor.PullGlobalWord((ushort)(0x1946 + colours)) == 0xf)
			{
				this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 15, 11);
			}

			if (this.processor.PullGlobalWord((ushort)(0x1946 + colours)) == 0x7)
			{
				this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 7, 3);
			}

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 10,
				(byte)this.processor.PullGlobalWord((ushort)(0x1946 + colours)));

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0x19d4), 0, 128, 320, 32, 2,
				(byte)this.processor.PullGlobalWord((ushort)(0x1956 + colours)));

			for (int idx = 0; idx < 0x1c; idx++)
			{
				StorePicture(0xd54e, (playerIdx << 5) + idx, 1, (ushort)(((idx % 0x14) << 4) + 1), (ushort)(((idx / 0x14) << 4) + 0x81), 15, 15);
			}
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
