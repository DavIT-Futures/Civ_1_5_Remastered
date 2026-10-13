using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1238.
	/// </summary>
	public class TurnEvents
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public TurnEvents(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Fortified units heal one lost hit point every turn
		/// </summary>
		public void MendFortifiedUnits()
		{
			for (int participantIdx = 0; participantIdx < UnitHealth.ParticipantTally; participantIdx++)
			{
				for (int troopIdx = 0; troopIdx < UnitHealth.TroopTally; troopIdx++)
				{
					ushort troop = (ushort)(0x81d4 + 0x600 * participantIdx + 0xc * troopIdx);
					int troopKind = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(troop + 3));

					if (troopKind != 0xff && (this.processor.PullUInt8(this.processor.SegData.Value16, troop) & (int)UnitStatusEnum.Fortified) != 0)
					{
						this.owner.World.Vitals.MendOne(participantIdx, troopIdx, troopKind);
					}
				}
			}
		}

		/// <summary>
		/// Shows the text as a dialog (DisplayMenu) and puts back the screen as it was before (kept in the back screen 0x19d4);
		/// the answer of DisplayMenu stays in AX
		/// </summary>
		public void DisplayDialog(ushort charsAddr, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"DisplayDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200,
				this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Dialogs.DisplayMenu(charsAddr, horizLoc, vertLoc, 1);

			ushort answer = this.processor.RegAcc.Value16;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 320, 200,
				this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("DisplayDialog");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void HandleGameEnd()
		{
			this.processor.Journal.OpenScope("HandleGameEnd()");

			// function body
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.LaunchHueCycling();

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold;
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0xd760, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6b92, this.processor.RegAcc.Value16);
			this.owner.World.HighestScienceTally = (short)this.processor.RegAcc.Value16;
			this.processor.EmitGlobalWord(0xde20, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L00c2:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L00e7;
			this.processor.EmitGlobalWord(0xde20, this.processor.IncrementWord(this.processor.PullGlobalWord(0xde20)));
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x6)].FoundScienceTally;

			if (this.owner.World.HighestScienceTally < (short)this.processor.RegSource.Value16)
			{
				this.owner.World.HighestScienceTally = (short)this.processor.RegSource.Value16;
			}

		L00e7:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L00c2;
			if (this.owner.World.RoundTally != 0) goto L0147;
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitGlobalWord(0xd75e, this.processor.RegAcc.Value16);

			RepaintMainScreen();
			
			if (this.owner.World.HardnessTier != 0) goto L013b;

			this.owner.TurnLogic.RenderUnitInfo(this.owner.World.PersonParticipantIdx, 0);

			this.owner.Prologue.DisplayInterfaceTutorial();

			RepaintMainScreen();

		L013b:
			this.owner.Services.PerformSound(1, 0);

		L0147:
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L026a;

		L014f:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 2, 192, 6, 6, 8);

			this.processor.EmitLocalWord(0x16, 0x0);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0183;

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitLocalWord(0x16, 0x1);

		L0183:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdf60), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0195;
			
			this.owner.Advisors.HandleAllCities(this.processor.PullLocalSignedWord(0x6));
			
		L0195:
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Gold > 0x7530)
			{
				this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Gold = 0x7530;
			}

			this.owner.Politics.HandlePeaceTreaties(this.processor.PullLocalSignedWord(0x6));

			this.owner.Bot.OperateAITurn(this.processor.PullLocalSignedWord(0x6));

			this.owner.Bot.ReimposeStrategicLocations(this.processor.PullLocalSignedWord(0x6));

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 2, 192, 6, 6, 15);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0225;

			RenderStatusBar();

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].TownTally != 0 ||
				this.owner.World.Participants[this.processor.PullLocalWord(0x6)].ColonistTally != 0)
				goto L021a;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Prologue.DisplayTerrainTutorial();

			this.processor.EmitGlobalWord(0xdc48, 0xffff);

			this.owner.Startup.DisplayMouseCursor();

		L021a:
			if (this.owner.World.Season <= 0) goto L0225;
			owner.World.TruceRoundTally++;

		L0225:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0237;

			this.owner.TurnLogic.HandlePlayerTurn(this.processor.PullLocalSignedWord(0x6));

		L0237:
			this.processor.EmitGlobalWord(0xdf60, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0248;

			this.owner.Startup.ConcealMouseCursor();

		L0248:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0267;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb884), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0267;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0260;
			goto L089a;

		L0260:
			DisplayEndGameSequence();

			goto L0896;

		L0267:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L026a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02a0;
			this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Tally = 0;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L0267;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0294;
			goto L014f;

		L0294:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdf60), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L029e;
			goto L014f;

		L029e:
			goto L0267;

		L02a0:
			this.owner.Startup.DisplayMouseCursor();

			DisplayTravellersReport();

			HandleEndOfTurnEvents();

			if (this.owner.World.RoundTally <= 50) goto L0306;
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L02c9;

		L02bb:
			this.owner.Founding.HandleCityDisasters(this.processor.PullLocalSignedWord(0x6));

		L02c6:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L02c9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0306;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(128));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x6));
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ConditionMarker == 0xff ||
				this.owner.World.Participants[this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ParticipantIdx].TownTally <= 1)
				goto L02c6;

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x6)].TrueExtent >= 5) goto L02bb;
			goto L02c6;

		L0306:
			RefreshPeaceGraphContent();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L036d;
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TownTally <= 1 ||
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold >= 100) goto L036d;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1a));
			this.processor.RegCount.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold);
			if (this.processor.FlagSet.IsUnsigned) goto L036d;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L036d;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x1c2a);

			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			DisplayDialog(0xba06, 80, 80);

		L036d:
			owner.World.RoundTally++;
			if (this.owner.World.Season >= 1000) goto L0380;
			this.owner.World.Season += 20;
			goto L03b8;

		L0380:
			if (this.owner.World.Season >= 1500) goto L038f;
			this.owner.World.Season += 10;
			goto L03b8;

		L038f:
			if (this.owner.World.Season >= 1750) goto L039e;
			this.owner.World.Season += 5;
			goto L03b8;

		L039e:
			if (this.owner.World.Season >= 1850) goto L03b4;
			this.processor.TestBitsByte((byte)(this.owner.World.StarshipMarkers & 0xff), 0xfe);
			if (this.processor.FlagSet.IsNotEqual) goto L03b4;
			this.owner.World.Season += 2;
			goto L03b8;

		L03b4:
			this.owner.World.Season += 1;

		L03b8:
			if (owner.World.Season != 0) goto L03dc;
			this.processor.EmitLocalWord(0x6, 0x0);

		L03c4:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.World.Participants[this.processor.PullLocalWord(0x6)].InquiryHeadway <<= 1;
			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L03c4;
			this.owner.World.Season = 1;

		L03dc:
			if (this.owner.World.Season != 21) goto L03e9;
			this.owner.World.Season = 20;

		L03e9:
			// Autosave every turn. The original game saved only every 50 turns (when enabled in the game options)
			// into the saved games 4 - 9, which are now manual saves.
			MendFortifiedUnits();

			if (this.owner.World.PersonParticipantIdx != -1)
			{
				this.owner.SaveLoad.PerformBackup();
			}

			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L03f9;
			goto L055e;

		L03f9:
			this.processor.CompareWord((ushort)this.owner.World.PersonParticipantIdx, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0416;
			goto L055e;
			
		L0416:
			// The autosave every 50 turns was replaced by the autosave every turn above

			this.owner.World.HighestScienceTally = 0;
			this.processor.EmitLocalWord(0x6, 0x1);

		L0444:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x6)].FoundScienceTally;
			if (this.owner.World.HighestScienceTally < (short)this.processor.RegSource.Value16)
			{
				this.owner.World.HighestScienceTally = (short)this.processor.RegSource.Value16;
			}

			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0444;
			this.processor.EmitGlobalWord(0xd2de, 0x0);
			
			if (this.owner.World.Season < 0) goto L048f;

			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.HighestScienceTally - (this.owner.World.RoundTally / 9),
				0, 6);

			this.processor.EmitGlobalWord(0xd2de, this.processor.RegAcc.Value16);

		L048f:
			this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			this.owner.UnitOps.AttachReplayContent(11,
				(byte)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TownTally,
				(byte)((this.processor.PullLocalByte(0x18) & 0xff00) >> 8),
				(byte)(this.processor.PullLocalByte(0x18) & 0xff));

			this.processor.EmitLocalWord(0x6, 0x0);

		L04d5:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x14), 0x0);
			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x4);
			if (this.processor.FlagSet.IsLess) goto L04d5;
			this.processor.EmitLocalWord(0x6, 0x1);
			goto L050a;

		L04ef:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Standing;
			this.processor.HalveAccumulator();
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x14), 
				this.processor.BitOrWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x14)), this.processor.RegAcc.Value16));

		L0507:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L050a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0542;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L0507;
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Standing;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L04ef;
			this.processor.HalveAccumulator();
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x14), 
				this.processor.BitOrWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x14)), this.processor.RegAcc.Value16));
			goto L0507;

		L0542:
			this.owner.UnitOps.AttachReplayContent(12,
				this.processor.PullLocalByte(0xe),
				this.processor.PullLocalByte(0x10),
				this.processor.PullLocalByte(0x12),
				this.processor.PullLocalByte(0x14));

		L055e:
			if (this.owner.World.RoundTally < this.owner.World.FollowingCollectionRound) goto L0582;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(40));

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.RoundTally);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14);
			owner.World.FollowingCollectionRound = (short)this.processor.RegAcc.Value16;

			this.owner.Atlas.DisplayEpicChronicle();

		L0582:
			this.processor.EmitLocalWord(0x6, 0x1);

		L0587:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x6)];
			this.processor.CompareWord(this.processor.RegSource.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L05b0;

			this.processor.CompareWord(this.processor.RegSource.Value16, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L05b0;

			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.RegSource.Value16);
			this.owner.World.Participants[this.owner.World.Towns[this.processor.RegSource.Value16].ParticipantIdx].Tally += 25;

		L05b0:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0587;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x3484), 0xfffd);
			if (this.processor.FlagSet.IsNotEqual) goto L05c8;

			this.processor.EmitGlobalWord(0x3484, 0xfffe);
			goto L05e6;

		L05c8:
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.DirtyCellTally);
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Tally -= (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3484), 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L05e6;
			this.processor.EmitGlobalWord(0x3484, 0x0);

		L05e6:
			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.EmitLocalWord(0x6, 0x0);

		L05f0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x6));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ConditionMarker == 0xff ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L0616;
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x6)].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0616;
			this.processor.EmitLocalWord(0x8, 0x1);

		L0616:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x80);
			if (this.processor.FlagSet.IsLess) goto L05f0;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L066e;
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x80);
			if (this.processor.FlagSet.IsEqual) goto L066e;
			this.processor.TestBitsWord((ushort)this.owner.World.StarshipMarkers, 0x100);
			if (this.processor.FlagSet.IsNotEqual) goto L066e;

			ObtainPalaceScoreForLevel((short)(this.owner.World.CastleTier + 1));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, false);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsLess) goto L066e;

			this.owner.World.CastleTier++;
			if (this.owner.World.CastleTier > 37) goto L066e;
			
			this.owner.Throne.DisplayPalace();

		L066e:
			RenderStatusBar();

			this.processor.EmitGlobalWord(0x1cf6, 0xffff);
			this.processor.EmitLocalWord(0x6, 0x1);

		L067d:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L069f;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].StarshipArrivalSeason == this.owner.World.Season)
			{
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
				this.processor.EmitGlobalWord(0x1cf6, this.processor.RegAcc.Value16);
			}

		L069f:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L067d;
			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x820);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			if ((short)this.processor.RegAcc.Value16 != this.owner.World.Season) goto L0718;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.owner.Libc.AppendText(0xba06, "\nplans retirement\nin 20 years.\n");

			this.owner.Paper.DisplayNewspaper(-1);

		L0718:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1cf6), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L073b;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xb884), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L073b;

			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x834);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			if ((short)this.processor.RegAcc.Value16 == this.owner.World.Season) goto L073b;
			goto L089a;

		L073b:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Libc.CopyText(0xba06, "*** GAME OVER ***\n");

			this.processor.CompareWord(this.processor.PullGlobalWord(0xb884), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L077e;

			this.owner.Libc.CopyText(0xba06, "Your civilization\nhas conquered\nthe entire planet!\n");

			this.processor.EmitGlobalWord(0x1cf6, 0xffff);

			this.owner.Paper.DisplayNewspaper(-1);

			this.owner.Replayer.DisplayConquerorScreen();

		L077e:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1cf6), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L07c7;

			this.owner.Prologue.DisplayCivilizationScreen(this.processor.PullGlobalSignedWord(0x1cf6));

			RepaintMainScreen();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x1cf6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " spaceship\narrives at Alpha Centauri.\n");

			this.owner.Paper.DisplayNewspaper(-1);

		L07c7:
			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x834);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			if ((short)this.processor.RegAcc.Value16 != this.owner.World.Season) goto L0836;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.owner.Libc.AppendText(0xba06, " dynasty\nends after glorious\n6000 year reign!\n");

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);

			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x151a)), 3);

			this.owner.Paper.DisplayNewspaper(-1);
			
			this.owner.Services.PerformSound(1, 0);

		L0836:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.TestBitsWord((ushort)this.owner.World.StarshipMarkers, 0x100);
			if (this.processor.FlagSet.IsNotEqual) goto L084e;

			DisplayEndGameSequence();

		L084e:
			RepaintMainScreen();

			this.processor.EmitGlobalByte(0x4cd9, this.processor.BitOrByte(this.processor.PullGlobalByte(0x4cd9), 0x1));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb884), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0889;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x1cf0);
			
			DisplayDialog(0xba06, 80, 80);

			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdc48, this.processor.RegAcc.Value16);
			goto L088f;

		L0889:
			this.processor.EmitGlobalWord(0xdc48, 0x1);

		L088f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L089a;

		L0896:
			DisplayTitleScreen();

		L089a:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleGameEnd");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayEndGameSequence()
		{
			this.processor.Journal.OpenScope("DisplayEndGameSequence()");

			// function body
			this.owner.Atlas.DisplayCivilizationGraph();

			if (this.processor.PullGlobalWord(0xdc48) != 2)
			{
				this.owner.Replayer.DisplayReplay();
			}

			this.owner.Startup.ConcealMouseCursor();

			if ((this.owner.World.StarshipMarkers & 0x100) == 0)
			{
				this.owner.Legends.DisplayGameResultsScreen((ushort)((this.owner.World.CastleTier >> 1) - 1),
					this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true));

				this.owner.Legends.ImportHallOfFame();

				this.owner.Legends.AttachHallOfFameRecord();

				this.owner.Legends.StoreHallOfFame();
			}
		
			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("DisplayEndGameSequence");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayTitleScreen()
		{
			this.processor.Journal.OpenScope("DisplayTitleScreen()");

			// function body
			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(1, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1cf8, 0);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1d05, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			// Far return
			this.processor.Journal.CloseScope("DisplayTitleScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayTravellersReport()
		{
			this.processor.Journal.OpenScope("DisplayTravellersReport()");

			// function body
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			this.processor.RegAcc.Value16 = 0x5;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.HardnessTier);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			if (this.processor.RegAcc.Value16 <= this.owner.World.RoundTally) goto L099d;
			goto L0d9b;

		L099d:
			this.processor.RegAcc.LowByte = (byte)(owner.World.RoundTally & 0xff);
			this.processor.RegAcc.LowByte = this.processor.IncrementByte(this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsEqual) goto L09a9;
			goto L0aa6;

		L09a9:
			this.processor.TestBitsWord((ushort)this.owner.World.ScienceLeadingFoundVia[58], 0x7);
			if (this.processor.FlagSet.IsEqual) goto L09b8;
			goto L0aa6;

		L09b8:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(80));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(44));

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L09b8;

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L09b8;

			this.owner.MapOps.ObtainLandMassSize(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x10);
			if (this.processor.FlagSet.IsLess) goto L09b8;

			this.owner.Helpers.VerifyValueSpan(
				(this.owner.World.RoundTally / 150) + 1, 1, 3);

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.UnitOps.ConstructUnit(
				0,
				(short)((this.processor.RegAcc.Value16 < 3) ? 0x11 : 0x12),
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));
			
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0a8b;

		L0a52:
			this.processor.EmitLocalWord(0x4, 0x7);

		L0a57:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0a64;
			this.processor.EmitLocalWord(0x4, 0x1a);

		L0a64:
			this.owner.UnitOps.ConstructUnit(
				0,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));
			
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x1c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d4), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d4)), 0x1));
			this.processor.IncrementLocalWord(0x14);

		L0a8b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0aa6;
			this.processor.TestBitsWord((ushort)this.owner.World.ScienceLeadingFoundVia[56], 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L0a52;
			this.processor.EmitLocalWord(0x4, 0x3);
			goto L0a57;

		L0aa6:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(128));

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x14));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x14)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0ac7;
			goto L0d9b;

		L0ac7:
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x14)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L0d9b;

			this.owner.Conquest.ComputeCityPlunder(this.processor.PullLocalSignedWord(0x14));

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(100));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x16));
			if (this.processor.FlagSet.IsLess) goto L0af7;
			goto L0d9b;

		L0af7:
			this.processor.EmitLocalWord(0x1c, 0x0);

		L0afc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x14));

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(13));

			this.processor.RegCount.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.X);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, 0x6);
			this.processor.EmitLocalWord(0xe, this.processor.RegCount.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(13));

			this.processor.RegCount.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.Y);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, 0x6);
			this.processor.EmitLocalWord(0x12, this.processor.RegCount.Value16);

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x14));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0xe),
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.X,
				this.processor.PullLocalSignedWord(0x12),
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.Y);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x1c);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x63);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0bef;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6);
			if (this.processor.FlagSet.IsLess) goto L0afc;

			this.owner.UnitOps.VerifyTileOccupation(this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0b92;
			goto L0afc;

		L0b92:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0ba8;
			goto L0afc;

		L0ba8:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0bbc;
			goto L0afc;

		L0bbc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x14));

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0bef;
			goto L0afc;

		L0bef:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x63);
			if (this.processor.FlagSet.IsLess) goto L0bf8;
			goto L0d9b;

		L0bf8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x14));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x14)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L0cc5;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c33;
			goto L0cc5;

		L0c33:
			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.X - 8,
				this.owner.World.Towns[this.processor.PullLocalWord(0x14)].Place.Y - 6);

			this.owner.Libc.CopyText(0xba06, "Travellers report:\n");

			this.processor.TestBitsWord((ushort)this.owner.World.ScienceLeadingFoundVia[35], 0x7);
			if (this.processor.FlagSet.IsEqual) goto L0c74;

			this.owner.Libc.AppendText(0xba06, 0x1d26);

			goto L0c84;

		L0c74:
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, 0x4f4), 0x7);
			if (this.processor.FlagSet.IsEqual) goto L0c81;

			this.owner.Libc.AppendText(0xba06, 0x1d3f);

			goto L0c84;

		L0c81:
			this.owner.Libc.AppendText(0xba06, 0x1d53);

		L0c84:
			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x14));

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x2);

			DisplayDialog(0xba06, 100, 32);

		L0cc5:
			this.owner.Helpers.VerifyValueSpan(
				this.owner.Libc.Dice.DrawInt(
					this.owner.World.Towns[this.processor.PullLocalWord(0x14)].TrueExtent / 2),
				2, 99);

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x18, 0x0);
			goto L0d75;

		L0cfb:
			this.processor.RegAcc.Value16 = 0x4;
			goto L0d0e;

		L0d00:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x18), 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0d0b;
			this.processor.RegAcc.Value16 = 0xa;
			goto L0d0e;

		L0d0b:
			this.processor.RegAcc.Value16 = 0x6;

		L0d0e:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0d1e;
			this.processor.EmitLocalWord(0x4, 0x1a);

		L0d1e:
			this.owner.UnitOps.ConstructUnit(
				0,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));
			
			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x14));
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x14)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L0d72;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L0d72:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));

		L0d75:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0d9b;
			this.processor.TestBitsWord((ushort)this.owner.World.ScienceLeadingFoundVia[35], 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L0d8c;
			goto L0d00;

		L0d8c:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x18), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0d95;
			goto L0cfb;

		L0d95:
			this.processor.RegAcc.Value16 = 0x9;
			goto L0d0e;

		L0d9b:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTravellersReport");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RefreshPeaceGraphContent()
		{
			this.processor.Journal.OpenScope("RefreshPeaceGraphContent()");

			// function body
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.EmitLocalWord(0x2, 0x1);

		L0db3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x5);
			this.processor.RegData.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].GrandTownExtent;
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 += this.processor.RegData.Value16;
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].FoundScienceTally;
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x0);

		L0de0:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x6));
			this.processor.ScaleIndexToSource(0x38, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].LiveTroops[this.processor.PullLocalWord(0x6)]);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16)), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L0de0;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L0e26;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16), 0x0);

		L0e26:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0e3e;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e3e;
			this.processor.EmitLocalWord(0x4, this.processor.RegSource.Value16);

		L0e3e:
			if (this.owner.World.RoundTally >= 600) goto L0e99;

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + (this.processor.PullLocalWord(0x2) << 1) - 0x16)) / 8,
				0, 255);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x96;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegData.Value16;
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x2));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0), this.processor.RegBase.LowByte);

		L0e99:
			this.processor.EmitLocalWord(0x18, 0x0);
			this.processor.EmitLocalWord(0x6, 0x0);

		L0ea3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[this.processor.PullLocalWord(0x6)] & 3) == 1)
			{
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.EmitLocalWord(0x18, this.processor.BitOrWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16));
			}
		
			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0ea3;
			if (this.owner.World.RoundTally >= 600) goto L0f05;
			this.processor.RegBase.LowByte = this.processor.PullLocalByte(0x18);
			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x96;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegData.Value16;
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x2));
			this.owner.World.TrucePlotContent[this.processor.RegSource.Value16] = this.processor.RegBase.LowByte;

		L0f05:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f11;
			goto L0db3;

		L0f11:
			this.processor.EmitLocalWord(0x6, 0x1);

		L0f16:
			this.processor.EmitLocalWord(0x4, 0xffff);
			this.processor.EmitLocalWord(0x2, 0x1);

		L0f20:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f36;
			this.processor.CopyLocalWord(0x2, 0x1a);
			this.processor.EmitLocalWord(0x4, this.processor.RegSource.Value16);

		L0f36:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0f20;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x8;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			this.owner.World.Participants[this.processor.PullLocalWord(0x1a)].Standing = (short)this.processor.RegAcc.Value16;
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16), 0xffff);
			this.processor.IncrementLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0f16;
			this.processor.EmitGlobalWord(0x8078, 0x0);

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing != 7 ||
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TownTally <= 4) goto L0fe4;

			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.PersonParticipantIdx);
			
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25] != 0 ||
				owner.World.RoundTally <= 200) goto L0fe4;

			this.processor.EmitGlobalWord(0x8078, 0x1);
			this.processor.EmitLocalWord(0x6, 0x1);
			goto L0fb0;

		L0f9b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Relations[this.owner.World.PersonParticipantIdx] |= 1;

		L0fad:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L0fb0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual)
				goto L0fe4;

			this.processor.CompareWord(this.processor.PullLocalWord(0x6), (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsEqual)
				goto L0fad;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].TownTally <= 1)
				goto L0fad;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord((ushort)this.owner.World.PersonParticipantIdx, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L0f9b;

			this.owner.World.Participants[this.processor.PullLocalWord(0x6)].Relations[this.owner.World.PersonParticipantIdx] |= 0x100;
			goto L0fad;

		L0fe4:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RefreshPeaceGraphContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void WipeStatusLine()
		{
			this.processor.Journal.OpenScope("WipeStatusLine()");

			// function body
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 8, 0);

			this.owner.TextPainter.RenderTextToScreen0(0x1d6f, 8, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d75, 64, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d7d, 128, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d87, 192, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d8e, 240, 1, 15);

			// Far return
			this.processor.Journal.CloseScope("WipeStatusLine");
		}

		/// <summary>
		/// The status panel (not on the world map, 0xb278 = 1): peace bar, palace, free memory (with 0xd806), population, year,
		/// research bulb (progress * 4 / (cost + 1), 0 - 3), pollution sun, gold and the tax / luxury / science rates; then, while
		/// 0x1c28 is 0, tells when the population passes the next mark (0x1c24: tens below 100, then hundreds + 9). The stack
		/// frame, the saved SI, the return address pushed for ObtainFreeStorage and SI = the new mark before DisplayDialog stay
		/// like in the original (DisplayDialog leaves a value made from SI in DI).
		/// </summary>
		public void RenderStatusBar()
		{
			this.processor.Journal.OpenScope("RenderStatusBar()");

			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();

			if (this.processor.PullGlobalWord(0xb278) != 1)
			{
				Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

				FloodBoxWithShadow(0, 58, 80, 39);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 3, 60, 74, 11, 11);

				if (this.owner.World.TruceRoundTally > 1)
				{
					int peace = this.owner.Helpers.VerifyValueSpan(this.owner.World.TruceRoundTally * 2, 0, 60);

					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 3, 60, (short)peace, 2, 15);
				}

				RenderPalace(this.owner.World.PersonParticipantIdx, this.owner.World.CastleTier, 8, 58);

				if (this.processor.PullGlobalWord(0xd806) != 0)
				{
					this.processor.StackWord(0); // stack management - push return segment, ignored
					this.processor.StackWord(0x111e); // stack management - push return offset
					this.owner.VideoCard.ObtainFreeStorage();
					this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

					short storage = (short)this.processor.RegAcc.Value16;

					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(storage, 10));
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 59, 0);
				}

				this.processor.EmitGlobalByte(0xba06, 0);
				short population = (short)this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);
				this.owner.Libc.AppendText(0xba06, "#");

				if (population != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 73, 0);
				}

				this.processor.EmitGlobalByte(0xba06, 0);
				ObtainCurrentYearAsText();
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 81, 0);

				short cost = (short)(((this.owner.World.HardnessTier << 1) + this.processor.PullGlobalWord(0xd2de) + 6) *
					human.FoundScienceTally * (this.owner.World.Season < 0 ? 1 : 2));
				short progress = CPU.SignedQuotient((short)(human.InquiryHeadway << 2), (short)(cost + 1));
				int bulb = this.owner.Helpers.VerifyValueSpan(progress, 0, 3);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), (ushort)((bulb << 3) + 0xa0), 0x78, 8, 8,
					this.processor.PullGlobalWord(0xaa), 0x30, 0x50);

				if (this.owner.World.SmogImpactTier != 0)
				{
					int smog = this.owner.Helpers.VerifyValueSpan(this.owner.World.SmogImpactTier / 4, 0, 3);

					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), (ushort)((smog << 3) + 0xa0), 0x80, 8, 8,
						this.processor.PullGlobalWord(0xaa), 0x38, 0x50);
				}

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(human.Gold, 10));
				this.owner.Libc.AppendText(0xba06, "$");
				this.owner.Libc.AppendText(0xba06, " ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(-(human.LevyRatio + human.ResearchLevyRatio - 10)), 10));
				this.owner.Libc.AppendText(0xba06, ".");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(human.LevyRatio, 10));
				this.owner.Libc.AppendText(0xba06, ".");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(human.ResearchLevyRatio, 10));
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 89, 0);

				if (this.processor.PullGlobalWord(0x1c28) == 0)
				{
					AnnouncePopulationMark(population);
				}
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("RenderStatusBar");
		}

		/// <summary>
		/// The population mark of RenderStatusBar: when the population passes the next mark 0x1c24 (in tens below 100, then in
		/// hundreds with 9 added), the mark moves and, unless 0x3484 is below -1, a dialog tells "...exceeds N00,000 citizens"
		/// </summary>
		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)this.processor.PullGlobalWord(0x1c24);

			this.owner.Libc.CopyText(0xba06, "The population of the\nfertile ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1982)));
			this.owner.Libc.AppendText(0xba06, " empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(this.processor.PullGlobalWord(0x1c24) * 10) < population)
				{
					short mark = (short)(population / 10);
					this.processor.RegSource.Value16 = (ushort)mark;

					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(mark, 10));

					this.processor.EmitGlobalWord(0x1c24, (ushort)mark);
				}
			}
			else if ((short)((this.processor.PullGlobalWord(0x1c24) - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);
				this.processor.RegSource.Value16 = (ushort)hundreds;

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(hundreds, 10));
				this.owner.Libc.AppendText(0xba06, ",0");

				this.processor.EmitGlobalWord(0x1c24, (ushort)(hundreds + 9));
			}

			if (previousMark < (short)this.processor.PullGlobalWord(0x1c24) && (short)this.processor.PullGlobalWord(0x3484) >= -1)
			{
				this.owner.Libc.AppendText(0xba06, "00,000 citizens.\n");

				this.processor.EmitGlobalWord(0x2f9e, 4);

				DisplayDialog(0xba06, 100, 80);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="palaceLevel"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void RenderPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x36);
			this.processor.PreserveIndexRegisters();

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 
				horizLoc - 5, vertLoc + 13, horizLoc + 68, vertLoc + 13, 2);

			if (this.owner.World.Participants[participantIdx].TownTally != 0) goto L14dc;
			goto L16e1;

		L14dc:
			this.owner.Helpers.VerifyValueSpan(castleTier + 3, 3, 36);

			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x5;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);

		L1504:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32), 0xffff);
			this.processor.IncrementLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x9);
			if (this.processor.FlagSet.IsLess) goto L1504;
			this.processor.EmitLocalWord(0x8, 0x3);

		L151c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32), 0x0);
			this.processor.IncrementLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x5);
			if (this.processor.FlagSet.IsLessOrEqual) goto L151c;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.owner.World.ChanceRoot);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L1569;

			this.processor.EmitLocalWord(0x8, 0x0);
			goto L154b;

		L1548:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L154b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x9);
			if (this.processor.FlagSet.IsLess) goto L1554;
			goto L1630;

		L1554:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd91e));
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb222));
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			goto L1548;

		L1569:
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L1625;

		L1571:
			this.processor.EmitLocalWord(0x2, 0x0);

		L1576:
			this.processor.EmitLocalWord(0xa, 0x1);
			this.processor.ScaleIndex(0x5, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegData.Value16 = 0;
			this.processor.RegCount.Value16 = 0x9;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe, this.processor.RegData.Value16);
			this.processor.RegSource.Value16 = this.processor.RegData.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x32));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegData.Value16, 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L15bd;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x30)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L15b8;
			this.processor.RegDest.Value16 = 0x7;
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, this.processor.RegData.Value16);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x32)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L15bd;

		L15b8:
			this.processor.EmitLocalWord(0xa, 0x0);

		L15bd:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L15e3;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x34)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L15de;
			this.processor.RegSource.Value16 = 0x9;
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0xe));
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x32)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L15e3;

		L15de:
			this.processor.EmitLocalWord(0xa, 0x0);

		L15e3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L15f1;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L15f6;

		L15f1:
			this.processor.EmitLocalWord(0xa, 0x0);

		L15f6:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1609;
			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1609;
			goto L1576;

		L1609:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x32), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20), this.processor.RegData.Value16);
			this.processor.IncrementLocalWord(0x8);

		L1625:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x36);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1630;
			goto L1571;

		L1630:
			this.processor.EmitLocalWord(0x8, 0x0);

		L1635:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x32)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L1645;
			goto L16d5;

		L1645:
			this.processor.EmitLocalWord(0xc, 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1666;
			this.processor.EmitLocalWord(0xc, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x34)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1661;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1666;

		L1661:
			this.processor.EmitLocalWord(0xc, 0x0);

		L1666:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1687;
			this.processor.EmitLocalWord(0xc, 0x3);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x30)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1682;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1687;

		L1682:
			this.processor.EmitLocalWord(0xc, 0x4);

		L1687:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x32)) - 1, 0, 3);
			
			this.processor.RegCount.Value16 = 0x5;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x28;
			this.processor.ScaleAccumulatorToBase(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(7 * this.processor.PullLocalSignedWord(0x8)) + horizLoc,
				vertLoc - 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xd21c)));

		L16d5:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x9);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L16e1;
			goto L1635;

		L16e1:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderPalace");
		}

		/// <summary>
		/// Score needed for the palace level: the sum of level * (7 - difficulty) for the levels 0 up to the given one, halved,
		/// plus 1
		/// </summary>
		/// <returns>Score, also in AX</returns>
		public ushort ObtainPalaceScoreForLevel(short castleTier)
		{
			this.processor.Journal.OpenScope($"ObtainPalaceScoreForLevel({castleTier})");

			short sum = 0;

			for (short tier = 0; tier <= castleTier; tier++)
			{
				sum = (short)(sum + (tier * (short)(7 - this.owner.World.HardnessTier)));
			}

			ushort score = (ushort)((sum / 2) + 1);

			this.processor.RegAcc.Value16 = score;

			this.processor.Journal.CloseScope("ObtainPalaceScoreForLevel");

			return score;
		}

		/// <summary>
		/// Gets current year as a String, appends buffer at 0xba06
		/// </summary>
		public void ObtainCurrentYearAsText()
		{
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs(this.owner.World.Season), 10));

			this.owner.Libc.AppendText(0xba06, (ushort)((this.owner.World.Season >= 0) ? 0x1df4 : 0x1df0));
		}

		/// <summary>
		/// ?
		/// </summary>
		public void HandleEndOfTurnEvents()
		{
			this.processor.Journal.OpenScope("HandleEndOfTurnEvents()");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveIndexRegisters();

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.DirtyCellTally;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.WorldHeatingTally;
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.SmogImpactTier);
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.SmogImpactTier);
			
			this.owner.Helpers.VerifyValueSpan((short)this.processor.RegAcc.Value16, 0, 99);

			owner.World.SmogImpactTier = (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsNotEqual) goto L17ca;
			if (this.owner.World.DirtyCellTally <= 6) goto L17ca;

			this.owner.Libc.CopyText(0xba06, "Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

			this.owner.Paper.DisplayNewspaper(-1);

		L17ca:
			if (this.owner.World.SmogImpactTier <= 16) goto L17e7;
			
			this.owner.WorldSetup.EnactGlobalWarming(this.owner.World.WorldHeatingTally);

			this.owner.World.WorldHeatingTally++;
			this.owner.World.SmogImpactTier = 0;

		L17e7:
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L18a0;

		L17ef:
			this.processor.RegAcc.Value16 = 0x2;

		L17f2:
			this.processor.EmitGlobalWord(0x2f9e, this.processor.RegAcc.Value16);

			DisplayDialog(0xba06, 100, 80);

			this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)(this.processor.PullLocalWord(0x8) + 0x18), 1);
			
			RepaintMainScreen();

			this.owner.Services.PerformSound(1, 0);

		L182b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L184d;

			this.owner.Advisors.UncoverTechnology(this.processor.PullLocalSignedWord(0xc), 0);
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullLocalSignedWord(0xc), 0);

		L184d:
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L1857;

		L1854:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L1857:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsLess) goto L1861;
			goto L1b3e;

		L1861:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker == 0xff ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L1854;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x18);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1854;
			
			this.owner.Advisors.RenewCityScreen(this.processor.PullLocalSignedWord(0x2));
			
			goto L1854;

		L189d:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L18a0:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L18ab;
			goto L1b3e;

		L18ab:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(21));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			
			if (this.owner.World.HardnessTier != 0) goto L18de;

			this.processor.ScaleIndexToBase(0x1e, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb6)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L189d;

		L18de:
			if (this.owner.World.HardnessTier >= 2) goto L1924;
			this.processor.EmitLocalWord(0x2, 0x0);

		L18ea:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker == 0xff ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L191a;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x18);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L191a;
			this.processor.EmitLocalWord(0x8, 0x11);

		L191a:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsLess) goto L18ea;

		L1924:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(128));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L1945;
			goto L189d;

		L1945:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ParticipantIdx;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.processor.RegAcc.Value16].Relations[0] & 0x4) != 0)
				goto L189d;

			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x11);
			if (this.processor.FlagSet.IsNotEqual) goto L1981;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0xc), 0x2e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1981;

			goto L189d;

		L1981:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L19ba;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0xc), 0x2e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L19ba;
	
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x2e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L19ba;

			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[17], 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L19ba;
			this.processor.EmitLocalWord(0x8, 0x11);

		L19ba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x11);
			if (this.processor.FlagSet.IsNotEqual) goto L19ec;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x2e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L19ec;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0xc)].MartialMight <=
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight)
				goto L19ec;

			goto L189d;

		L19ec:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L19f5;
			goto L189d;

		L19f5:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1a00;
			goto L189d;

		L1a00:
			if (this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x8)] == -1)
				goto L1a11;
			goto L189d;

		L1a11:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe82));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreater) goto L1a3d;
			goto L189d;

		L1a3d:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe86)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1a53;

			goto L189d;

		L1a53:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x8)] = (short)this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.owner.World.Towns[this.processor.PullLocalWord(0x2)].MaterialsTally = 0;

			this.owner.UnitOps.AttachReplayContent(10,
				this.processor.PullLocalByte(0xc),
				this.processor.PullLocalByte(0x8));
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x2));

			this.owner.Libc.AppendText(0xba06, " (");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, ")\nbuilds ");

			this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xe6a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Participants[this.processor.PullLocalWord(0xc)].Gold / 3, 
				0,
				(10 * this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe82))) -
					this.owner.World.Towns[this.processor.PullLocalWord(0x2)].MaterialsTally);

			this.owner.World.Participants[this.processor.PullLocalWord(0xc)].Gold -= (short)this.processor.RegAcc.Value16;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsEqual)
				goto L182b;

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.processor.PullLocalWord(0xc)].ChiefVisuals);

			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1518)), 0);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0xc)] & 0x40) == 0)
				goto L17ef;

			this.processor.RegAcc.Value16 = 0x5;
			goto L17f2;

		L1b3e:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleEndOfTurnEvents");
		}

		/// <summary>
		/// Draws the main screen again: the map view, the box of the left column and the civilization panels, with the colour
		/// cycling stopped meanwhile (0x1c28 = 1 while drawing)
		/// </summary>
		public void RepaintMainScreen()
		{
			this.processor.Journal.OpenScope("RepaintMainScreen()");

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x1c28, 0x1);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			FloodBoxWithShadow(0, 97, 80, 103);

			this.owner.Startup.DisplayMouseCursor();

			DisplayCivilizationScenery();

			this.processor.EmitGlobalWord(0x1c28, 0x0);

			this.owner.Helpers.LaunchHueCycling();

			// Far return
			this.processor.Journal.CloseScope("RepaintMainScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithShadow(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			// function body
			this.owner.Helpers.FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.owner.Dialogs.RenderShadowBox(horizLoc, vertLoc, breadth - 1, altitude - 1, 15, 8);
		}

		/// <summary>
		/// When 0xd762 is set: loads the palette of the human player's government, "backNa.pal" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" with technology 20), and the palette 0x1e56, and puts colours 5 on. The file name is
		/// made in a buffer in the stack frame, like in the original.
		/// </summary>
		public void DisplayCivilizationScenery()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationScenery()");

			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Helpers.HaltHueCycling();

				ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
				this.owner.Libc.CopyText(docname, "back0a.pal");

				short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
				this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)((regime / 2) + 0x30));

				if (regime == 3)
				{
					this.processor.EmitGlobalByte((ushort)(docname + 4), 0x33);
				}
				else if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
				{
					this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
				}

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e56, 0xc5be);

				this.owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

				this.owner.Services.ImportColorsFromBuffer(5, 0xc5be);

				this.owner.Helpers.LaunchHueCycling();
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayCivilizationScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="score"></param>
		public short ObtainPalaceLevel(short rating)
		{
			// function body
			short castleTier = 0;

			do
			{
				castleTier += 5;
				rating -= castleTier;
			} while (rating > 0);

			castleTier /= 5;
			castleTier--;

			return castleTier;
		}
	}
}
