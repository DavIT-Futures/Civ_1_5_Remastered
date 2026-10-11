using Civilization1Remaster.Emulation;

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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void EliminateSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSpaceship({participantIdx})");

			// function body
			this.processor.EnterFrame();

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " spaceship\n");

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L065e;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.StarshipMarkers & this.processor.RegAcc.Value16) != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x28ca);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x28dd);
			}

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L065e:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.owner.World.StarshipMarkers &= (short)this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.owner.World.StarshipMarkers &= (short)this.processor.RegAcc.Value16;

			this.owner.NewGameWizard.WipeSpaceshipContent(participantIdx);
			
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("EliminateSpaceship");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort ComputeCityPlunder(short townIdx)
		{
			this.processor.Journal.OpenScope($"ComputeCityPlunder({townIdx})");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x7d00;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Gold);
			if (this.processor.FlagSet.IsLessOrEqual) goto L06ca;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Gold;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].GrandTownExtent;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			goto L06ec;

		L06ca:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Gold;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].GrandTownExtent;
			this.processor.RegBase.Value16 = this.processor.IncrementWord(this.processor.RegBase.Value16);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);

		L06ec:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ComputeCityPlunder");

			return this.processor.RegAcc.Value16;
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
		/// Appends buffer at 0xba06 with city name
		/// </summary>
		/// <param name="cityID"></param>
		public void ObtainCityLabel(short townIdx)
		{
			this.processor.Journal.OpenScope($"ObtainCityLabel({townIdx})");

			// function body
			this.processor.EnterFrame();

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

			this.processor.RegFrame.Value16 = this.processor.UnstackWord();

			// Far return
			this.processor.Journal.CloseScope("ObtainCityLabel");
		}

		/// <summary>
		/// Waits for Key press or Mouse click
		/// </summary>
		public void AwaitForKeyPressOrMouseClick()
		{
			this.processor.Journal.OpenScope("AwaitForKeyPressOrMouseClick()");

			// function body
			this.owner.TurnLogic.AwaitForKey();

		L091d:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0932;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L091d;

		L0932:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0947;

			this.owner.Libc.ReadKey();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0947;

			this.owner.Libc.ReadKey();

		L0947:
			// Far return
			this.processor.Journal.CloseScope("AwaitForKeyPressOrMouseClick");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0x600, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x6));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x6)].Place.X,
				this.owner.World.Towns[townIdx].Place.Y,
				this.owner.World.Towns[this.processor.PullLocalWord(0x6)].Place.Y);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x6)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x2));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x18;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x6)].Place.X);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x12));
			if (this.processor.FlagSet.IsNotEqual) goto L09f4;
			this.processor.RegCount.Value16 = 0x2;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

		L09f4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L0a12;
			this.processor.RegCount.Value16 = 0x2;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

		L0a12:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 0x31);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0a3c;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.SubtractWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L0a3c:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 0x26);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0a66;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.SubtractWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L0a66:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.owner.World.Participants[participantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[participantIdx].InquiryHeadway += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0a81;
			goto L0b23;

		L0a81:
			this.processor.RegBase.Value16 = (ushort)troopIdx;
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, 0x7);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1972)));

			this.owner.Libc.AppendText(0xba06, " caravan from ");

			ObtainCityLabel(this.processor.PullLocalSignedWord(0x6));

			this.owner.Libc.AppendText(0xba06, "\narrives in ");

			ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "\nTrade route established\nRevenue: $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			this.owner.TurnHooks.RenderStatusBar();

		L0b23:
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0b4e;
			this.processor.EmitLocalWord(0x4, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x4), 0x1));

		L0b4e:
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L0b9c;

		L0b5a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0b68;
			this.processor.EmitLocalWord(0x8, 0xffff);
			goto L0b85;

		L0b68:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0b85;
			this.processor.EmitLocalWord(0x8, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x8), 0x1));

		L0b85:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b99;
			this.processor.CopyLocalWord(0x8, 0x4);
			this.processor.CopyLocalWord(0xa, 0xe);

		L0b99:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L0b9c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0bc3;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x6));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x6)].CommerceTownIndices[this.processor.PullLocalWord(0xa)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0b5a;

		L0bbe:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0bdd;

		L0bc3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0bbe;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x6));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegAcc.LowByte = (byte)townIdx;
			this.owner.World.Towns[this.processor.PullLocalWord(0x6)].CommerceTownIndices[this.processor.PullLocalWord(0xe)] = (sbyte)this.processor.RegAcc.LowByte;
			goto L0bbe;

		L0bdd:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("FoundTradeRoute");

			return this.processor.RegAcc.Value16;
		}
	}
}
