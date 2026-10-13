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
		/// Barbarians, from turn 30 * (5 - level): every 8th turn, while nobody knows technology 58, a landing ship (0x11, from
		/// turn 300 0x12) on an ocean tile nobody owns of a sea of at least 16 tiles with 1 - 3 units (3, with technology 56: 7)
		/// and a leader (0x1a), all on board (flag 1). Then a random city of the human player is attacked with a chance of its
		/// plunder %: up to 0x63 tries for a free land tile nobody owns, at least 6 from its nearest city and on its continent
		/// (the nearest city becomes the target; the occupation test asks for the player in the never written local 0x2 of the
		/// frame, like in the original); a human city the player sees gets the travellers' report, then size / 2 (2 - 99) units
		/// (4 / 9 or 6 / 10 with technology 35) and a leader land there, seen like the tile. The final values of the locals
		/// stay in the stack frame, like in the original.
		/// </summary>
		public void DisplayTravellersReport()
		{
			this.processor.Journal.OpenScope("DisplayTravellersReport()");

			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			if ((ushort)((ushort)(5 - (ushort)this.owner.World.HardnessTier) * 0x1e) <= this.owner.World.RoundTally)
			{
				if ((((byte)(this.owner.World.RoundTally & 0xff) + 1) & 7) == 0 && (this.owner.World.ScienceLeadingFoundVia[58] & 7) == 0)
				{
					LandBarbariansFromSea();
				}

				RaidHumanCity();
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTravellersReport");
		}

		/// <summary>The landing ship of the barbarians with its units (see DisplayTravellersReport)</summary>
		private void LandBarbariansFromSea()
		{
			short horizLoc, vertLoc;

			do
			{
				horizLoc = (short)this.owner.Libc.Dice.DrawInt(80);
				this.processor.EmitLocalWord(0xe, (ushort)horizLoc);

				vertLoc = (short)(this.owner.Libc.Dice.DrawInt(44) + 3);
				this.processor.EmitLocalWord(0x12, (ushort)vertLoc);
			}
			while (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != 0xa ||
				this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff ||
				(short)this.owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 0x10);

			short units = (short)this.owner.Helpers.VerifyValueSpan((this.owner.World.RoundTally / 150) + 1, 1, 3);
			this.processor.EmitLocalWord(0x16, (ushort)units);

			this.owner.UnitOps.ConstructUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizLoc, vertLoc);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIdx == units)
					kind = 0x1a;

				this.processor.EmitLocalWord(0x4, (ushort)kind);

				this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc);

				ushort troopIdx = this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0x1c, troopIdx);

				ushort marker = (ushort)((ushort)(0xc * troopIdx) + 0x81d4);
				this.processor.EmitGlobalByte(marker, (byte)(this.processor.PullGlobalByte(marker) | 1));
			}

			this.processor.EmitLocalWord(0x14, (ushort)unitIdx);
		}

		/// <summary>The barbarians landing near a city of the human player (see DisplayTravellersReport)</summary>
		private void RaidHumanCity()
		{
			ushort townIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);
			this.processor.EmitLocalWord(0x14, townIdx);

			if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff ||
				this.owner.World.Towns[townIdx].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return;

			this.owner.Conquest.ComputeCityPlunder((short)townIdx);

			ushort plunder = this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x16, plunder);

			if ((short)this.owner.Libc.Dice.DrawInt(100) >= (short)plunder)
				return;

			ushort tries = 0;
			short horizLoc, vertLoc;

			while (true)
			{
				int shift = this.owner.Libc.Dice.DrawInt(13);
				horizLoc = (short)((short)this.owner.World.Towns[townIdx].Place.X + shift - 6);
				this.processor.EmitLocalWord(0xe, (ushort)horizLoc);

				shift = this.owner.Libc.Dice.DrawInt(13);
				vertLoc = (short)((short)this.owner.World.Towns[townIdx].Place.Y + shift - 6);
				this.processor.EmitLocalWord(0x12, (ushort)vertLoc);

				this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);

				townIdx = this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0x14, townIdx);

				this.owner.Helpers.ObtainDistance(horizLoc, this.owner.World.Towns[townIdx].Place.X, vertLoc, this.owner.World.Towns[townIdx].Place.Y);

				short distance = (short)this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0xc, (ushort)distance);

				tries++;
				this.processor.EmitLocalWord(0x1c, tries);

				if ((short)tries >= 0x63)
					return;

				if (distance < 6)
					continue;

				if (this.owner.UnitOps.VerifyTileOccupation(this.processor.PullLocalSignedWord(0x2), horizLoc, vertLoc) != 0)
					continue;

				if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa)
					continue;

				if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
					continue;

				ushort continent = this.owner.MapOps.ObtainContinentID(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc) == continent)
					break;
			}

			if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
				((sbyte)this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (byte)this.owner.World.PersonParticipantIdx)) != 0)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
					this.owner.World.Towns[townIdx].Place.X - 8,
					this.owner.World.Towns[townIdx].Place.Y - 6);

				this.owner.Libc.CopyText(0xba06, "Travellers report:\n");

				if ((this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x1d26);
				}
				else if ((this.processor.PullUInt8(this.processor.SegExtra.Value16, 0x4f4) & 7) != 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x1d3f);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, 0x1d53);
				}

				this.owner.Conquest.ObtainCityLabel((short)townIdx);

				this.owner.Libc.AppendText(0xba06, "!\n");

				this.processor.EmitGlobalWord(0x2f9e, 0x2);

				DisplayDialog(0xba06, 100, 32);
			}

			short units = (short)this.owner.Helpers.VerifyValueSpan(this.owner.Libc.Dice.DrawInt(this.owner.World.Towns[townIdx].TrueExtent / 2), 2, 99);
			this.processor.EmitLocalWord(0x16, (ushort)units);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIdx & 4) != 0 ? (short)9 : (short)4) :
					((unitIdx & 4) != 0 ? (short)0xa : (short)6);

				if (unitIdx == units)
					kind = 0x1a;

				this.processor.EmitLocalWord(0x4, (ushort)kind);

				this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc);

				ushort sight = (ushort)((ushort)(0xc * this.processor.RegAcc.Value16) + 0x81dd);
				this.processor.EmitGlobalByte(sight, (byte)(this.processor.PullGlobalByte(sight) | (byte)this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc]));

				if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				{
					this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
				}
			}

			this.processor.EmitLocalWord(0x18, (ushort)unitIdx);
		}

		/// <summary>
		/// The power of every player 1 - 7 (gold / 32 + 8 * citizens + technologies + the price of all units, 0 for a destroyed
		/// civilization): before turn 600 it goes (/ 8, at most 255) into the power graph (segment 0x3772) and the players' peace
		/// treaties into the peace graph (TrucePlotContent), both at 8 * (turn / 4 % 150) + player. The players are ranked by
		/// power (Standing 7 - 1). A human player first in power, with more than 4 cities, no spaceship part (unit 25) and after
		/// turn 200 is feared (0x8078): the others with more than one city make peace, or with a treaty get 0x100.
		/// </summary>
		public void RefreshPeaceGraphContent()
		{
			this.processor.Journal.OpenScope("RefreshPeaceGraphContent()");

			// The powers are in the stack frame, like in the original, and the final values of the locals stay there: when no
			// power is above -1, the ranking takes the player of the rank before (or the one left in the frame)
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveIndexRegisters();

			ushort powers = (ushort)(this.processor.RegFrame.Value16 - 0x16);
			short strongest = 0;
			ushort treaties = 0;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				Player participant = this.owner.World.Participants[participantIdx];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(UnitKindTable.Field(this.processor, (sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIdx) & this.owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				this.processor.EmitGlobalWord((ushort)(powers + (participantIdx << 1)), power);

				if (participantIdx != this.owner.World.PersonParticipantIdx && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.owner.World.RoundTally < 600)
				{
					// The power graph is in the segment 0x3772 (ES is left there, like in the original)
					this.processor.SegExtra.Value16 = 0x3772;
					this.processor.EmitUInt8(0x3772, GraphPlace(participantIdx), (byte)this.owner.Helpers.VerifyValueSpan((short)power / 8, 0, 255));
				}

				treaties = 0;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if ((participant.Relations[otherIdx] & 3) == 1)
					{
						treaties |= (ushort)(1 << otherIdx);
					}
				}

				if (this.owner.World.RoundTally < 600)
				{
					this.owner.World.TrucePlotContent[GraphPlace(participantIdx)] = (byte)treaties;
				}
			}

			ushort best = this.processor.PullLocalWord(0x1a);

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short power = (short)this.processor.PullGlobalWord((ushort)(powers + (participantIdx << 1)));

					if (strongest < power)
					{
						best = (ushort)participantIdx;
						strongest = power;
					}
				}

				this.owner.World.Participants[best].Standing = (short)(8 - rank);
				this.processor.EmitGlobalWord((ushort)(powers + (best << 1)), 0xffff);
			}

			this.processor.EmitLocalWord(0x2, 0x8);
			this.processor.EmitLocalWord(0x4, (ushort)strongest);
			this.processor.EmitLocalWord(0x6, 0x8);
			this.processor.EmitLocalWord(0x18, treaties);
			this.processor.EmitLocalWord(0x1a, best);

			this.processor.EmitGlobalWord(0x8078, 0x0);

			Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.owner.World.RoundTally > 200)
			{
				this.processor.EmitGlobalWord(0x8078, 0x1);

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					if (participantIdx == this.owner.World.PersonParticipantIdx || this.owner.World.Participants[participantIdx].TownTally <= 1)
						continue;

					if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
					{
						this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 1;
					}
					else
					{
						this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x100;
					}
				}
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RefreshPeaceGraphContent");
		}

		/// <summary>The place of the player in the power and peace graphs: 8 * (turn / 4 % 150) + player</summary>
		private ushort GraphPlace(short participantIdx)
		{
			return (ushort)(((DivideByPowerOfTwo(this.owner.World.RoundTally, 2) % 150) << 3) + participantIdx);
		}

		/// <summary>Signed division toward zero by 2^shift of the original (-32768 gives a positive quotient)</summary>
		private static short DivideByPowerOfTwo(short amount, int shift)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> shift);

			return amount < 0 ? (short)-quotient : quotient;
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
		/// The small palace of a player with cities, on a line at (horizLoc, vertLoc): the human player's from its palace
		/// (levels 0xd91e, styles 0xb222), an AI player's built from castleTier + 3 parts (3 - 36) by a generator seeded with
		/// player * ChanceRoot (levels up to 3 and parts / 5, a section only next to a higher one, its style player % 3). Each built
		/// section is drawn from the pictures at 0xd21c by its style, level and place (ends and middle).
		/// </summary>
		public void RenderPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc})");

			// The levels and the styles of the sections are in the stack frame, like in the original: the left neighbour of
			// section 0 is the frame word 0x34 (never written), the right neighbour of section 8 the style of section 0; the
			// final values of the locals stay there
			this.processor.EnterFrame(0x36);
			this.processor.PreserveIndexRegisters();

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), horizLoc - 5, vertLoc + 13, horizLoc + 68, vertLoc + 13, 2);

			if (this.owner.World.Participants[participantIdx].TownTally != 0)
			{
				ushort levels = (ushort)(this.processor.RegFrame.Value16 - 0x32);
				ushort styles = (ushort)(this.processor.RegFrame.Value16 - 0x20);

				short parts = (short)this.owner.Helpers.VerifyValueSpan(castleTier + 3, 3, 36);
				short highest = (short)(parts / 5);

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					SetSectionWord(levels, sectionIdx, sectionIdx is >= 3 and <= 5 ? (ushort)0 : (ushort)0xffff);
				}

				ushort seed = (ushort)(participantIdx * this.owner.World.ChanceRoot);
				short built = 0;
				(ushort attempts, ushort fits, ushort level, ushort chosen) last = (0, 0, 0, 0);
				bool generated = false;

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
					{
						SetSectionWord(levels, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xd91e)));
						SetSectionWord(styles, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xb222)));
					}

					built = 9;
				}
				else
				{
					for (built = 0; built < parts; built++)
					{
						ushort attempts = 0;
						bool fits;
						short chosen;
						short level;

						while (true)
						{
							fits = true;
							seed = (ushort)((seed * 5) + 1);
							chosen = (short)((byte)seed % 9);
							level = (short)(SectionWord(levels, chosen) + 1);

							if (chosen < 4 && ((short)SectionWord(levels, chosen + 1) < level || SectionWord(levels, 7 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (chosen > 4 && ((short)SectionWord(levels, chosen - 1) < level || SectionWord(levels, 9 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (level > highest || level > 3)
							{
								fits = false;
							}

							if (fits || ++attempts >= 0x3e7)
								break;
						}

						SetSectionWord(levels, chosen, (ushort)level);
						SetSectionWord(styles, chosen, (ushort)(participantIdx % 3));
						last = (attempts, (ushort)(fits ? 1 : 0), (ushort)level, (ushort)chosen);
						generated = true;
					}
				}

				// The frame word 0xc: the last level generated, then the shape of each drawn section
				ushort? lastShape = generated ? last.level : null;

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					short sectionLevel = (short)SectionWord(levels, sectionIdx);

					if (sectionLevel <= 0)
						continue;

					// 0: left end, 1: left, 2: middle, 3: right, 4: right end
					ushort shape = 2;

					if (sectionIdx < 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx - 1) == 0xffff || sectionIdx == 1 ? 0 : 1);
					}

					if (sectionIdx > 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx + 1) == 0xffff || sectionIdx == 7 ? 4 : 3);
					}

					lastShape = shape;

					int height = this.owner.Helpers.VerifyValueSpan(sectionLevel - 1, 0, 3);

					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), (7 * sectionIdx) + horizLoc, vertLoc - 1,
						this.processor.PullGlobalWord((ushort)((0x28 * SectionWord(styles, sectionIdx)) + ((((short)height * 5) + shape) << 1) + 0xd21c)));
				}

				this.processor.EmitLocalWord(0x36, (ushort)parts);
				this.processor.EmitLocalWord(0x6, (ushort)highest);
				this.processor.EmitLocalWord(0x4, seed);
				this.processor.EmitLocalWord(0x8, 0x9);

				if (lastShape != null)
				{
					this.processor.EmitLocalWord(0xc, lastShape.Value);
				}

				if (generated)
				{
					this.processor.EmitLocalWord(0x2, last.attempts);
					this.processor.EmitLocalWord(0xa, last.fits);
					this.processor.EmitLocalWord(0xe, last.chosen);
				}
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderPalace");
		}

		/// <summary>A word of a table of the sections in the stack frame (the index may be -1 or 9, like in the original)</summary>
		private ushort SectionWord(ushort table, int sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)(table + (sectionIdx << 1)));
		}

		private void SetSectionWord(ushort table, int sectionIdx, ushort amount)
		{
			this.processor.EmitGlobalWord((ushort)(table + (sectionIdx << 1)), amount);
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
		/// The end of a turn: the pollution level SmogImpactTier moves one step towards 2 * DirtyCellTally - 4 * WorldHeatingTally
		/// (0 - 99; the newspaper warns at 12 with more than 6 polluted tiles, above 16 global warming comes). Then up to level + 1
		/// tries for a wonder 1 - 21 built by a rival: on the easiest level only a wonder the human player could build, on the
		/// two easiest the one a human city builds becomes 0x11 (the Manhattan Project with technology 0x2e, also in the first try
		/// when the rival has 0x2e and the human player not, unless built), not stronger rivals than him for 0x11 once he has
		/// 0x2e; a city of size above cost / 10 of a player at peace with the barbarians and with the wonder's technology builds
		/// it with up to a third of the treasury (announced unless 0xb278 is 1), the human cities building it get a new
		/// production. The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		public void HandleEndOfTurnEvents()
		{
			this.processor.Journal.OpenScope("HandleEndOfTurnEvents()");

			this.processor.EnterFrame(0xe);
			this.processor.PreserveIndexRegisters();

			short pull = (short)(((ushort)this.owner.World.DirtyCellTally << 1) - ((ushort)this.owner.World.WorldHeatingTally << 2) - (ushort)this.owner.World.SmogImpactTier);

			this.owner.Helpers.VerifyValueSpan((short)(Math.Sign(pull) + this.owner.World.SmogImpactTier), 0, 99);

			this.owner.World.SmogImpactTier = (short)this.processor.RegAcc.Value16;

			if (this.processor.RegAcc.Value16 == 0xc && this.owner.World.DirtyCellTally > 6)
			{
				this.owner.Libc.CopyText(0xba06, "Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			if (this.owner.World.SmogImpactTier > 16)
			{
				this.owner.WorldSetup.EnactGlobalWarming(this.owner.World.WorldHeatingTally);

				this.owner.World.WorldHeatingTally++;
				this.owner.World.SmogImpactTier = 0;
			}

			short attempt;

			for (attempt = 0; attempt <= this.owner.World.HardnessTier; attempt++)
			{
				this.processor.EmitLocalWord(0xa, (ushort)attempt);

				if (TryRivalWonder(attempt))
					break;
			}

			this.processor.EmitLocalWord(0xa, (ushort)attempt);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleEndOfTurnEvents");
		}

		/// <summary>One try for a wonder built by a rival (see HandleEndOfTurnEvents), true when one was built</summary>
		private bool TryRivalWonder(short attempt)
		{
			short person = this.owner.World.PersonParticipantIdx;
			ushort wonderIdx = (ushort)(this.owner.Libc.Dice.DrawInt(21) + 1);
			this.processor.EmitLocalWord(0x8, wonderIdx);

			if (this.owner.World.HardnessTier == 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, (short)this.processor.PullGlobalWord((ushort)((ushort)(0x1e * wonderIdx) + 0xbb6))) == 0)
				return false;

			if (this.owner.World.HardnessTier < 2)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (HumanCityBuilds(townIdx, wonderIdx))
						wonderIdx = 0x11;
				}

				this.processor.EmitLocalWord(0x2, 0x80);
				this.processor.EmitLocalWord(0x8, wonderIdx);
			}

			ushort cityIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);
			this.processor.EmitLocalWord(0x2, cityIdx);

			if (this.owner.World.Towns[cityIdx].ConditionMarker == 0xff)
				return false;

			ushort rivalIdx = (ushort)this.owner.World.Towns[cityIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0xc, rivalIdx);

			if ((this.owner.World.Participants[rivalIdx].Relations[0] & 4) != 0)
				return false;

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) == 0)
				return false;

			if (attempt == 0 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) != 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.owner.World.MarvelTownIdx[17] == -1)
			{
				wonderIdx = 0x11;
				this.processor.EmitLocalWord(0x8, wonderIdx);
			}

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.owner.World.Participants[rivalIdx].MartialMight > this.owner.World.Participants[person].MartialMight)
				return false;

			if (rivalIdx == 0xffff || rivalIdx == (ushort)person)
				return false;

			if (this.owner.World.MarvelTownIdx[wonderIdx] != -1)
				return false;

			ushort wonder = (ushort)(0x1e * wonderIdx);
			this.processor.EmitLocalWord(0xe, (ushort)(0x1c * cityIdx));

			if (this.owner.World.Towns[cityIdx].TrueExtent <= CPU.SignedQuotient((short)this.processor.PullGlobalWord((ushort)(wonder + 0xe82)), 10))
				return false;

			if (this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, (short)this.processor.PullGlobalWord((ushort)(wonder + 0xe86))) == 0)
				return false;

			this.owner.World.MarvelTownIdx[wonderIdx] = (short)cityIdx;
			this.owner.World.Towns[cityIdx].MaterialsTally = 0;

			this.owner.UnitOps.AttachReplayContent(10, (byte)rivalIdx, (byte)wonderIdx);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.Conquest.ObtainCityLabel((short)cityIdx);

			this.owner.Libc.AppendText(0xba06, " (");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((rivalIdx << 1) + 0x1982)));

			this.owner.Libc.AppendText(0xba06, ")\nbuilds ");

			this.owner.Libc.AppendText(0xba06, (ushort)(wonder + 0xe6a));

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[rivalIdx].Gold / 3, 0,
				(10 * (short)this.processor.PullGlobalWord((ushort)(wonder + 0xe82))) - this.owner.World.Towns[cityIdx].MaterialsTally);

			this.owner.World.Participants[rivalIdx].Gold -= (short)this.processor.RegAcc.Value16;

			if (this.processor.PullGlobalWord(0xb278) != 1)
			{
				this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)((ushort)(0x3a * this.owner.World.Participants[rivalIdx].ChiefVisuals) + 0x1518)), 0);

				this.processor.EmitGlobalWord(0x2f9e, (this.owner.World.Participants[person].Relations[rivalIdx] & 0x40) == 0 ? (ushort)2 : (ushort)5);

				DisplayDialog(0xba06, 100, 80);

				this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)(wonderIdx + 0x18), 1);

				RepaintMainScreen();

				this.owner.Services.PerformSound(1, 0);
			}

			if (wonderIdx == 0xe)
			{
				this.owner.Advisors.UncoverTechnology((short)rivalIdx, 0);

				this.owner.Advisors.UncoverTechnology((short)rivalIdx, 0);
			}

			for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (HumanCityBuilds(townIdx, wonderIdx))
					this.owner.Advisors.RenewCityScreen((short)townIdx);
			}

			this.processor.EmitLocalWord(0x2, 0x80);

			return true;
		}

		/// <summary>Whether the city is a city of the human player building the wonder</summary>
		private bool HumanCityBuilds(ushort townIdx, ushort wonderIdx)
		{
			return this.owner.World.Towns[townIdx].ConditionMarker != 0xff &&
				this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
				this.owner.World.Towns[townIdx].PresentMakingIdx == (short)-(wonderIdx + 0x18);
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
