using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2459.
	/// </summary>
	public class TradeAndConquest
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public TradeAndConquest(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="cityID"></param>
		/// <param name="flag"></param>
		public void SeizeCity(short participantIdx, short townIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"SeizeCity({participantIdx}, {townIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveIndexRegisters();

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Participants[this.processor.PullLocalWord(0x12)].TouchParticipantTimeleft = -2;
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			}

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L00c5;
			
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L005c;
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L00c5;

		L005c:
			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L00c5;

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L00c5;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x12)].TownTally <= 4 ||
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing >=
				this.owner.World.Participants[this.processor.PullLocalWord(0x12)].Standing) goto L00c5;

			this.owner.Secession.DivideCivilization(this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L00c5;
			this.processor.RegAcc.LowByte = (byte)participantIdx;
			this.owner.World.Towns[townIdx].ParticipantIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x00ab); // stack management - push return offset
			this.owner.Secession.ObtainDistanceToCapital(this.processor.PullLocalSignedWord(0x12));
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment
			
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x2));
			this.owner.World.Towns[this.processor.PullLocalWord(0x2)].PresentMakingIdx = -1;

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x12);
			this.owner.World.Towns[townIdx].ParticipantIdx = (sbyte)this.processor.RegAcc.LowByte;

		L00c5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L00de;

			EliminateSpaceship(this.processor.PullLocalSignedWord(0x12));

		L00de:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual)
			{
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = -1;
			}

			ComputeCityPlunder(townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(short)this.processor.RegAcc.Value16,
				0,
				this.owner.World.Participants[this.processor.PullLocalWord(0x12)].Gold);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L016c;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);
			this.processor.ScaleIndexToBase(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L016c;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x28bc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L016c;

			this.owner.Advisors.FinalizeProduction(
				townIdx,
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0x8));
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);

			this.owner.World.Towns[townIdx].TrueExtent = 0;
			goto L01dc;

		L016c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xae;
			this.owner.World.Towns[townIdx].StructureMarkers0 &= 0xfbb6;

			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L01a8;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0xaaaa;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegData.Value16 = 0;
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegData.Value16;

		L01a8:
			if (marker == 0)
				goto L01bd;
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);

			if (this.owner.World.Towns[townIdx].TrueExtent <= 1)
				goto L01dc;

		L01bd:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.owner.World.Towns[townIdx].TrueExtent--;
			if (this.owner.World.Towns[townIdx].TrueExtent < 1)
			{
				this.owner.Advisors.FinalizeProduction(
					townIdx,
					this.processor.PullLocalWord(0x6),
					this.processor.PullLocalWord(0x8));
			}

		L01dc:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.owner.World.Participants[this.processor.PullLocalWord(0x12)].Gold -= (short)this.processor.RegAcc.Value16;

			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L01f7;
			this.owner.World.Participants[participantIdx].Gold += (short)this.processor.RegAcc.Value16;

		L01f7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0209;
			goto L028f;

		L0209:
			this.owner.UnitOps.AttachReplayContent(9, (byte)participantIdx, this.owner.World.Towns[townIdx].LabelIdx,
				this.processor.PullLocalByte(0x6),
				this.processor.PullLocalByte(0x8));

			this.owner.Bot.DisableStrategicLocation(participantIdx, 0,
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8),
				4);
			
			this.owner.World.Towns[townIdx].ConditionMarker &= 0x9b;
			this.processor.EmitLocalWord(0x10, 0x0);

		L024b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x6));

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0xa) + 0x50,
				this.processor.PullLocalSignedWord(0xc),
				(ushort)participantIdx);

			this.processor.IncrementLocalWord(0x10);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x14);
			if (this.processor.FlagSet.IsLess) goto L024b;

		L028f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)participantIdx;
			this.owner.World.Towns[townIdx].ParticipantIdx = (sbyte)this.processor.RegAcc.LowByte;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02ae;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02ae;
			goto L03f7;

		L02ae:
			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8));

			if (this.processor.PullLocalSignedWord(0x12) == this.owner.World.PersonParticipantIdx)
			{
				this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
					this.processor.PullLocalSignedWord(0x6),
					this.processor.PullLocalSignedWord(0x8));
			}
		
			this.owner.MapOps.RenderMapTile(this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " capture\n");

			ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ". ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x4), 10));

			this.owner.Libc.AppendText(0xba06, " gold\npieces plundered.\n");

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);

			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1518)), 0);

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L03b7;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.CityScene.DisplayCityScreen(townIdx, -2);
			
			this.owner.Startup.DisplayMouseCursor();

			this.owner.Startup.QueryForDisk(1, 1);

			this.owner.CityScene.RenderCitySceneryForPlayer(participantIdx);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			goto L03c2;

		L03b7:
			this.owner.Paper.DisplayNewspaper(townIdx);

		L03c2:
			this.owner.Services.PerformSound(1, 0);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L03da;
			this.processor.RegAcc.LowByte = 0;
			goto L03e6;

		L03da:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;

		L03e6:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ShownExtent = (sbyte)this.processor.RegCount.LowByte;
			goto L04bc;

		L03f7:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				goto L041a;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x12)] & 0x40) == 0)
				goto L04bc;

		L041a:
			this.processor.EmitGlobalWord(0x2f9e, 0x0);

			this.owner.Libc.CopyText(0xba06, "Spies report:\n");

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);

			if (this.owner.World.Towns[townIdx].TrueExtent != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x2893);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x28a1);
			}

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " city\nof ");

			ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L04bc:
			ChooseTechnologyToTake(participantIdx, this.processor.PullLocalSignedWord(0x12));

			this.owner.MapOps.MapAssignCityOwner(
				participantIdx,
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.EmitLocalWord(0x10, 0x0);

		L04df:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x12));
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x10));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0531;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L0531;
			
			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.X);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)), this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L0523;
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)), this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L0523;
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0531;

		L0523:
			this.owner.UnitOps.EliminateUnit(this.processor.PullLocalSignedWord(0x12),
				this.processor.PullLocalSignedWord(0x10));

		L0531:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x80);
			if (this.processor.FlagSet.IsLess) goto L04df;
			
			if (participantIdx == this.owner.World.PersonParticipantIdx &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind < 4)
			{
				this.owner.World.Towns[townIdx].ConditionMarker |= 1;
			}

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05da;

			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L0586;

			this.processor.ScaleIndexToBase(0x38, this.processor.PullLocalWord(0x12));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0x12)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L0586:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L05a6;

			this.owner.CityOps.HandleCity(townIdx, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			goto L05da;

		L05a6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.World.Towns[townIdx].PresentMakingIdx = 0;

			this.owner.Advisors.PickCityProduction(participantIdx, townIdx);
			
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;
			
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsLess) goto L05da;
			
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.ScaleIndex(0x38, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L05da:
			this.owner.NewGameWizard.EliminateCivilization(this.processor.PullLocalSignedWord(0x12), participantIdx);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SeizeCity");
		}

		/// <summary>
		/// The player loses its spaceship: when it was launched (StarshipMarkers bit 8 + player) a message "... spaceship" with
		/// the text 0x28ca (also built: bit player) or 0x28dd; both bits are cleared and the parts wiped
		/// </summary>
		public void EliminateSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSpaceship({participantIdx})");

			ushort built = (ushort)(1 << participantIdx);
			ushort launched = (ushort)(1 << (participantIdx + 8));

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " spaceship\n");

			if ((this.owner.World.StarshipMarkers & launched) != 0)
			{
				this.owner.Libc.AppendText(0xba06, (this.owner.World.StarshipMarkers & built) != 0 ? (ushort)0x28ca : (ushort)0x28dd);

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.World.StarshipMarkers &= (short)~built;
			this.owner.World.StarshipMarkers &= (short)~launched;

			this.owner.NewGameWizard.WipeSpaceshipContent(participantIdx);

			this.processor.Journal.CloseScope("EliminateSpaceship");
		}

		/// <summary>
		/// The gold taken with a city: the owner's gold * city size / (total size of the owner's cities + 1), divided first
		/// when the product would pass 32000. Signed word arithmetic like in the original (also its division errors).
		/// </summary>
		/// <returns>The gold, also in AX</returns>
		public ushort ComputeCityPlunder(short townIdx)
		{
			this.processor.Journal.OpenScope($"ComputeCityPlunder({townIdx})");

			short size = this.owner.World.Towns[townIdx].TrueExtent;
			Player victim = this.owner.World.Participants[(ushort)this.owner.World.Towns[townIdx].ParticipantIdx];
			short divisor = (short)(victim.GrandTownExtent + 1);

			short plunder = CPU.SignedQuotient(32000, size) > victim.Gold ?
				CPU.SignedQuotient((short)(victim.Gold * size), divisor) :
				(short)(CPU.SignedQuotient(victim.Gold, divisor) * size);

			this.processor.RegAcc.Value16 = (ushort)plunder;

			this.processor.Journal.CloseScope("ComputeCityPlunder");

			return (ushort)plunder;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		public void ChooseTechnologyToTake(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"ChooseTechnologyToTake({participantIdx1}, {participantIdx2})");

			// function body
			this.processor.EnterFrame(0x9a);
			this.processor.PreserveSource();
			this.processor.CompareWord((ushort)participantIdx1, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0703;
			goto L08c1;

		L0703:
			this.processor.CompareWord((ushort)participantIdx2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L070c;
			goto L08c1;

		L070c:
			this.processor.EmitLocalWord(0x2, 0x0);

			this.owner.Libc.CopyText(0xba06, "Select one...\n ");

			this.processor.EmitLocalWord(0x96, 0xffff);
			this.processor.EmitLocalWord(0x98, 0x47);

		L072d:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx1,
				this.processor.PullLocalSignedWord(0x98));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L07b9;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx2,
				this.processor.PullLocalSignedWord(0x98));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L07b9;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x98));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n ");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x98);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x94), this.processor.RegAcc.Value16);

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx1, this.processor.PullLocalWord(0x98));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x96));
			if (this.processor.FlagSet.IsLessOrEqual) goto L07b9;

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx1, this.processor.PullLocalWord(0x98));

			this.processor.EmitLocalWord(0x96, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x98, 0x4);

		L07b9:
			this.processor.EmitLocalWord(0x98, this.processor.DecrementWord(this.processor.PullLocalWord(0x98)));
			if (this.processor.FlagSet.IsSigned) goto L07c2;
			goto L072d;

		L07c2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07cb;
			goto L08c1;

		L07cb:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx1, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0823;

		L07d3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L07ed;

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 32);

			goto L07fe;

		L07ed:
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x07fe); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

		L07fe:
			this.processor.EmitLocalWord(0x9a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L07d3;

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Advisors.EarnTechnology(participantIdx1,
				this.processor.PullInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x94)),
				participantIdx2);
			
			goto L08c1;

		L0823:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx2, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L08a1;

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " take\n");

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L088d;

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			goto L08a1;

		L088d:
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x089e); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

		L08a1:
			this.owner.Advisors.EarnTechnology(participantIdx1,
				this.processor.PullLocalSignedWord(0x4),
				participantIdx2);
			
			this.owner.Bot.RefreshCityProduction(participantIdx1, 0xffff);

		L08c1:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChooseTechnologyToTake");
		}

		/// <summary>
		/// Appends the name of the city (13 characters of GameState.TownLabels) to the text at 0xba06, "NONE" for city -1
		/// </summary>
		public void ObtainCityLabel(short townIdx)
		{
			this.processor.Journal.OpenScope($"ObtainCityLabel({townIdx})");

			if (townIdx != -1)
			{
				byte townLabelIdx = this.owner.World.Towns[townIdx].LabelIdx;
				ushort charsDisplacement = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06));

				for (int idx = 0; idx < 0xd; idx++)
				{
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.owner.World.TownLabels[townLabelIdx][idx]);
				}
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, "NONE");
			}

			this.processor.Journal.CloseScope("ObtainCityLabel");
		}

		/// <summary>
		/// Waits for a key or a mouse click; a key is taken (with the second code of an extended key)
		/// </summary>
		public void AwaitForKeyPressOrMouseClick()
		{
			this.processor.Journal.OpenScope("AwaitForKeyPressOrMouseClick()");

			this.owner.TurnLogic.AwaitForKey();

			do
			{
				this.owner.Startup.ObtainMouseState();

				if (this.owner.GlobDb3a != 0)
					break;
			}
			while (this.owner.Libc.KeyAvailable() == 0);

			if (this.owner.GlobDb3a == 0 && this.owner.Libc.ReadKey() == 0)
			{
				this.owner.Libc.ReadKey();
			}

			this.processor.Journal.CloseScope("AwaitForKeyPressOrMouseClick");
		}

		/// <summary>
		/// A caravan enters a city: the revenue ((distance + 10) * (trade of both cities) / 24, halved on the same continent and
		/// for an own city, less a third each with the technologies 0x31 and 0x26 of the city's owner) goes to the gold and the
		/// science of the player (a message for the human player), the caravan is removed and the city becomes a trade route of
		/// its home city, in place of the route with the least trade (an empty slot first), unless that one has more.
		/// </summary>
		/// <remarks>The continent of the home city is looked up at (x, x), like in the original.</remarks>
		/// <returns>1, also in AX</returns>
		public ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx})");

			// DisplayDialog leaves SI in DI: both are given back to the caller as they were
			ushort source = this.processor.RegSource.Value16;
			ushort dest = this.processor.RegDest.Value16;

			UnitMemory troops = new UnitMemory(this.processor);
			ushort homeIdx = (ushort)troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			City town = this.owner.World.Towns[townIdx];
			City home = this.owner.World.Towns[homeIdx];

			int distance = this.owner.Helpers.ObtainDistance(town.Place.X, home.Place.X, town.Place.Y, home.Place.Y);
			short revenue = (short)((home.RootCommerce + town.RootCommerce) * (short)(distance + 0xa));
			revenue = (short)(revenue / 0x18);

			short homeHoriz = (short)home.Place.X;

			if (this.owner.MapOps.ObtainContinentID(homeHoriz, homeHoriz) == this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y))
			{
				revenue = (short)(revenue / 2);
			}

			if (town.ParticipantIdx == participantIdx)
			{
				revenue = (short)(revenue / 2);
			}

			if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x31) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x26) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			this.owner.World.Participants[participantIdx].Gold += revenue;
			this.owner.World.Participants[participantIdx].InquiryHeadway += revenue;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)(((troopIdx & 7) << 1) + 0x1972)));

				this.owner.Libc.AppendText(0xba06, " caravan from ");

				ObtainCityLabel((short)homeIdx);

				this.owner.Libc.AppendText(0xba06, "\narrives in ");

				ObtainCityLabel(townIdx);

				this.owner.Libc.AppendText(0xba06, "\nTrade route established\nRevenue: $");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(revenue, 10));

				this.owner.Libc.AppendText(0xba06, ".\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

				this.owner.TurnHooks.RenderStatusBar();
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			short trade = RouteTrade(townIdx, participantIdx);
			int replacedIdx = -1;

			for (int routeIdx = 0; routeIdx < 3; routeIdx++)
			{
				ushort routeTownIdx = (byte)home.CommerceTownIndices[routeIdx];

				if (routeTownIdx == (ushort)townIdx)
				{
					replacedIdx = -1;
					break;
				}

				short routeTrade = routeTownIdx == 0xff ? (short)-1 : RouteTrade((short)routeTownIdx, participantIdx);

				if (routeTrade < trade)
				{
					trade = routeTrade;
					replacedIdx = routeIdx;
				}
			}

			if (replacedIdx != -1)
			{
				home.CommerceTownIndices[replacedIdx] = (sbyte)townIdx;
			}

			this.processor.RegSource.Value16 = source;
			this.processor.RegDest.Value16 = dest;
			this.processor.RegAcc.Value16 = 1;

			// Far return
			this.processor.Journal.CloseScope("FoundTradeRoute");

			return 1;
		}

		/// <summary>The trade of the city as a trade route of the player: doubled for a foreign city</summary>
		private short RouteTrade(short townIdx, short participantIdx)
		{
			short trade = this.owner.World.Towns[townIdx].RootCommerce;

			return this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx ? trade : (short)(trade << 1);
		}
	}
}
