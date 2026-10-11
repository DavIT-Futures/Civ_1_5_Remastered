using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1866.
	/// </summary>
	public class UnitManagement
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public UnitManagement(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		public void ConstructUnitsFromCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"ConstructUnitsFromCityList({townIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);

			if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.EmitGlobalWord(0x20f4, 0x1);
				this.processor.EmitLocalWord(0x4, 0x0);

			L002f:
				this.processor.RegAcc.Value16 = 0x1c;
				this.processor.ScaleAccumulatorToSource((ushort)townIdx);
				this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0x4)], 0xff);
				if (this.processor.FlagSet.IsEqual) goto L00a4;

				ConstructUnit(
					this.owner.World.Towns[townIdx].ParticipantIdx,
					(short)(this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0x4)] & 0x3f),
					this.owner.World.Towns[townIdx].Place.X,
					this.owner.World.Towns[townIdx].Place.Y);
				
				this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
				this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
				if (this.processor.FlagSet.IsEqual) goto L00a4;
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
				this.processor.RegCount.Value16 = 0x600;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
				this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
				this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x2));
				this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
				this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x81d4);
				this.processor.EmitLocalWord(0x6, this.processor.RegCount.Value16);
				this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x8));
				this.processor.TestBitsByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0x4)], 0x40);
				if (this.processor.FlagSet.IsEqual) goto L0094;
				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x20));

			L0094:
				this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0x4)] = -1;

			L00a4:
				this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
				this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x2);
				if (this.processor.FlagSet.IsLess) goto L002f;
				this.processor.EmitGlobalWord(0x20f4, 0x0);
				this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
				this.owner.World.Towns[townIdx].ConditionMarker |= 4;
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ConstructUnitsFromCityList");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		public void KeepUnitsInCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"KeepUnitsInCityList({townIdx})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70d8);
				this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
				this.processor.RegAcc.Value16 = 0;
				this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalWord(0x70d8, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalWord(0x20f4, 0x1);
				this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
				goto L014b;

			L0102:
				this.processor.RegAcc.Value16 = 0x1c;
				this.processor.ScaleAccumulatorToSource((ushort)townIdx);
				this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[1], 0xff);
				if (this.processor.FlagSet.IsEqual) goto L0116;
				goto L01bd;

			L0116:
				this.processor.RegAcc.Value16 = 0x600;
				this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x2));
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x6));
				this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7));
				this.owner.World.Towns[townIdx].Unfamiliar[1] = (sbyte)this.processor.RegAcc.LowByte;
				this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0x20);
				if (this.processor.FlagSet.IsEqual) goto L0136;
				this.owner.World.Towns[townIdx].Unfamiliar[1] |= 0x40;

			L0136:
				EliminateUnit(this.processor.PullLocalSignedWord(0x2),
					this.processor.PullLocalSignedWord(0x6));

			L0143:
				this.processor.EmitLocalWord(0x4, 0x1);

			L0148:
				this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

			L014b:
				this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x80);
				if (this.processor.FlagSet.IsGreaterOrEqual) goto L01bd;
				this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x2));
				this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x6));
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
				if (this.processor.FlagSet.IsEqual) goto L0148;
				this.processor.ScaleIndex(0x1c, (ushort)townIdx);
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.X);
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)), this.processor.RegAcc.LowByte);
				if (this.processor.FlagSet.IsNotEqual) goto L0148;
				this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.Y);
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)), this.processor.RegAcc.LowByte);
				if (this.processor.FlagSet.IsNotEqual) goto L0148;
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df));
				this.processor.SignExtendByte(this.processor.RegAcc);
				this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
				if (this.processor.FlagSet.IsNotEqual) goto L0148;
				this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x8);
				if (this.processor.FlagSet.IsEqual) goto L0148;
				this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
				if (this.processor.FlagSet.IsEqual) goto L0143;
				this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[0], 0xff);
				if (this.processor.FlagSet.IsEqual) goto L01a6;
				goto L0102;

			L01a6:
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
				this.owner.World.Towns[townIdx].Unfamiliar[0] = (sbyte)this.processor.RegAcc.LowByte;
				this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x20);
				if (this.processor.FlagSet.IsEqual) goto L0136;
				this.owner.World.Towns[townIdx].Unfamiliar[0] |= 0x40;
				goto L0136;

			L01bd:
				this.processor.EmitGlobalWord(0x20f4, 0x0);
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
				this.processor.EmitGlobalWord(0x70d8, this.processor.RegAcc.Value16);
				this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
				this.owner.World.Towns[townIdx].ConditionMarker &= 0xfb;
			}
		
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("KeepUnitsInCityList");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="flag"></param>
		public void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"ShiftUnit({horizLoc}, {vertLoc}, {participantIdx}, {troopIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x16);
			this.processor.PreserveIndexRegisters();

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L02af;

			this.processor.EmitLocalWord(0xa, 0x0);

		L0205:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x9);
			if (this.processor.FlagSet.IsLess) goto L0266;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L02a3;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0266;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L02a3;

		L0266:
			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L02a3;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
				this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;

			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

		L02a3:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x19);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02af;
			goto L0205;

		L02af:
			this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L0381;

		L02c8:
			this.processor.RegAcc.Value16 = 0x1;

		L02cb:
			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullGlobalSignedWord(0xd7f0),
				(short)horizLoc, (short)vertLoc, 2, (short)this.processor.RegAcc.Value16);

		L02e2:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0329;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0329;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L0329;

			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				3, 2);

		L0329:
			if (marker == 0)
				goto L037e;
			
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L033e;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L037e;

		L033e:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0x2;
			}
		
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				this.processor.RegAcc.Value16);

		L037e:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L0381:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x9);
			if (this.processor.FlagSet.IsLess) goto L038a;
			goto L06cc;

		L038a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L037e;

			if (participantIdx != 0)
			{
				this.processor.RegAcc.LowByte = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
				this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
					this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;
			}

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L03f9;
			goto L04bd;

		L03f9:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			if ((short)this.processor.RegAcc.Value16 == participantIdx)
				goto L04bd;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			ConstructUnitsFromCityList((short)this.processor.RegAcc.Value16);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x8));
				this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x8)].TrueExtent;
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;
			}

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04bd;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x14);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x14)] & 2) != 0)
				goto L04bd;

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				1, 4);

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				2, 2);

		L04bd:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L04d6;
			goto L0329;

		L04d6:
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L0329;

			this.processor.TestBitsByte(this.processor.PullLocalByte(0xe), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0503;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L0503:
			VisitEachUnitInStack(this.processor.PullGlobalSignedWord(0xd7f0), this.processor.PullLocalSignedWord(0x2), 0x14a2);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0532;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0548;

		L0532:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L0548:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0580;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0580;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0595;

		L0580:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L0595:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L05b7;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L05b7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05d9;
			goto L02e2;

		L05d9:
			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L05ef;
			goto L02e2;

		L05ef:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0605;
			goto L02e2;

		L0605:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0628;
			goto L02e2;

		L0628:
			this.owner.Politics.BargainPeaceTreaty(participantIdx, this.processor.PullGlobalSignedWord(0xd7f0), horizLoc, vertLoc);
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L06a3;

			this.processor.EmitLocalWord(0x6, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L068b;
			this.processor.ScaleIndex(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L068b;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L0686;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L068b;

		L0686:
			this.processor.EmitLocalWord(0x6, 0x3);

		L068b:
			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				1, this.processor.PullLocalSignedWord(0x6));

		L06a3:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L06bd;
			goto L02e2;

		L06bd:
			if (participantIdx == 0)
				goto L02c8;

			this.processor.RegAcc.Value16 = 0x2;
			goto L02cb;

		L06cc:
			this.processor.EmitLocalWord(0xa, 0x9);
			goto L08bc;

		L06d4:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				2);

		L06ea:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L06f3;
			goto L0857;

		L06f3:			
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L0857;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)), 0x16);
			if (this.processor.FlagSet.IsNotEqual) goto L0718;
			goto L0857;

		L0718:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x22, this.processor.PullLocalWord(0x4));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0743;
			goto L07f0;

		L0743:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0760;

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0760;
			goto L07f0;

		L0760:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfe));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xe), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L079d;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L079d:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L07b3;
			goto L0857;

		L07b3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07bd;
			goto L0857;

		L07bd:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L0857;

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullGlobalSignedWord(0xd7f0),
				(short)horizLoc, (short)vertLoc, 3, 2);

			goto L0857;

		L07f0:
			if (this.processor.PullGlobalSignedWord(0xd7f0) != this.owner.World.PersonParticipantIdx)
				goto L0857;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0857;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[horizLoc, vertLoc];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0857;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

		L0857:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			if ((short)this.processor.RegAcc.Value16 != this.owner.World.PersonParticipantIdx)
				goto L08b9;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[horizLoc, vertLoc];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

		L08b9:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L08bc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x19);
			if (this.processor.FlagSet.IsLess) goto L08c5;
			goto L0a42;

		L08c5:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L093b;
			goto L06ea;

		L093b:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0958;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0958;
			goto L06ea;

		L0958:
			if (participantIdx != 0)
			{
				this.processor.RegAcc.LowByte = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
				this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
					this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L09f9;

			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L09f9;

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x16);
			if (this.processor.FlagSet.IsEqual) goto L09f9;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L09b2;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L09b2:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L09f9;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09f9;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L09f9;

			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				3, 2);

		L09f9:
			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0a02;
			goto L06ea;

		L0a02:
			if (participantIdx != this.owner.World.PersonParticipantIdx &&
				this.processor.PullGlobalSignedWord(0xd806) == 0)
				goto L06ea;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0a3c;
			goto L06d4;

		L0a3c:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				1);

			goto L06ea;

		L0a42:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L0ad0;

			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ad0;

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xa, 0x9);

		L0a66:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0ac7;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ac7;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

		L0ac7:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x31);
			if (this.processor.FlagSet.IsLess) goto L0a66;

		L0ad0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftUnit");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		public void ChoreographUnitMove(short participantIdx, short troopIdx, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitMove({participantIdx}, {troopIdx}, {arg3}, {arg4})");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.CompareWord(arg3, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0b2d;

			this.owner.Routing.TunePosition(
				(short)arg3 - this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = arg4;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x50);
			if (this.processor.FlagSet.IsLess) goto L0b28;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b28;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L0b28;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0b2d;

		L0b28:
			this.processor.EmitLocalWord(0x6, 0xffff);

		L0b2d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70ea);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b36;
			this.processor.RegAcc.Value16 = 0;

		L0b36:
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70ea));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Routing.TunePosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) -
					this.processor.PullGlobalSignedWord(0x6ed6));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			if (troopIdx < 127)
			{
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			}
			else
			{
				this.processor.RegAcc.LowByte = 0xff;
			}
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xa));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

		L0b75:
			this.owner.Startup.ConcealMouseCursor();

			if (troopIdx >= 128)
				goto L0bc9;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.RenderMapTile(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0be1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0be1;

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4) + 8,
				15);

			goto L0be1;

		L0bc9:
			this.owner.TextPainter.RenderTextToScreen0(0x20f6, 4, 124, 7);

		L0be1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0bfb;

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8),
				15, 15, 15);

		L0bfb:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0c0e;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0c11;

		L0c0e:
			this.processor.RegAcc.Value16 = 0xa;

		L0c11:
			this.owner.TextPainter.AwaitTime((short)this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			if (troopIdx >= 0x80)
				goto L0c86;

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);

			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0c9d;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c9d;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0x2;
			}

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4) + 8,
				this.processor.RegAcc.Value16);

			goto L0c9d;

		L0c86:
			this.owner.TextPainter.RenderTextToScreen0(0x2102, 4, 124, 0);

		L0c9d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0cb9;

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x8),
				15, 15, 0);

		L0cb9:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0ccc;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0ccf;

		L0ccc:
			this.processor.RegAcc.Value16 = 0xa;

		L0ccf:
			this.owner.TextPainter.AwaitTime((short)this.processor.RegAcc.Value16);

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0cf0;
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0cf0;
			goto L0b75;

		L0cf0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChoreographUnitMove");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ConstructUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ConstructUnit({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L0d06;

		L0d03:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0d06:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x7f);
			if (this.processor.FlagSet.IsLess) goto L0d0f;
			goto L0ec8;

		L0d0f:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0d03;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), 0xff);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de), 0xff);

			this.owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);

			this.owner.MapOps.SituateUnitOnTile(participantIdx, this.processor.PullLocalSignedWord(0x2), horizLoc, vertLoc);

			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitLocalByte(0x4, this.processor.RegAcc.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)this.processor.RegCount.LowByte;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 0x0);
			this.processor.RegAcc.LowByte = (byte)horizLoc;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)vertLoc;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)troopIdx;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7), this.processor.RegAcc.LowByte);

			// A new unit is fully healthy
			this.owner.World.Vitals.Wipe(participantIdx, this.processor.PullLocalSignedWord(0x2));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), 0xff);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

			this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x1c;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			// !!!
			if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df)) < 128)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df))].ParticipantIdx;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0dd9;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df), 0xff);

		L0dd9:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d9);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0df4;
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.DecrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16)));

		L0df4:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.RegBase.Value16 = (ushort)troopIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].LiveTroops[troopIdx]++;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x20f4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0e7d;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0e3d;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0e3d;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x2));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0e64;

		L0e3d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x2));

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

		L0e64:
			ShiftUnit(
				horizLoc,
				vertLoc,
				participantIdx,
				this.processor.PullLocalSignedWord(0x2),
				1);

		L0e7d:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0ec3;
			if (this.owner.World.RoundTally == 0) goto L0ec3;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x20f4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ec3;

			if (this.owner.World.Participants[participantIdx].TroopTally != 0) goto L0eab;

			this.owner.HelpSystem.DisplayHelpPopup(0x210e);

		L0eab:
			if (this.owner.World.Participants[participantIdx].TroopTally != 1) goto L0ec3;

			this.owner.HelpSystem.DisplayHelpPopup(0x211a);

		L0ec3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			goto L0f0b;

		L0ec8:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0f08;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd760), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0f08;
			this.processor.EmitGlobalWord(0x2f9e, 0x3);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x2126);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

			this.processor.EmitGlobalWord(0xd760, 0x1);

		L0f08:
			this.processor.RegAcc.Value16 = 0xffff;

		L0f0b:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("ConstructUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void EliminateUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70d8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f46;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);

			this.owner.World.Participants[participantIdx].MissingTroops[this.processor.RegAcc.Value16]++;

		L0f46:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f9a;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0f9a;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0f9a;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1610);

		L0f9a:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x17);
			if (this.processor.FlagSet.IsNotEqual) goto L0fe6;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0fe6;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0fe6;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1676);

		L0fe6:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToBase(0x38, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)) != -1)
				this.owner.World.Participants[participantIdx].LiveTroops[this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7))]--;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7), 0xff);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.PullGlobalWord(0x20f4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1085;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3936), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1085;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L105e;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L105e;

			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1085;

		L105e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

		L1085:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("EliminateUnit");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort LocateStackLeader(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateStackLeader({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L10ad;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			goto L111c;

		L10ad:
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitGlobalWord(0x6530, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6534, 0x0);
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L10c3;

		L10c0:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L10c3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1119;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L10c0;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d5)), this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L10c0;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d6)), this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L10c0;

			VisitEachUnitInStack(participantIdx, this.processor.PullLocalSignedWord(0x2), 0x1169);

		L1119:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6530);

		L111c:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("LocateStackLeader");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort LocateBestGuardianInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianInStack({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L1142;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			goto L1166;

		L1142:
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitGlobalWord(0x6530, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6534, 0xffff);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1169);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6530);

		L1166:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();

			// Far return
			this.processor.Journal.CloseScope("LocateBestGuardianInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void LocateBestGuardianStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianStep({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L11b7;
			this.processor.ScaleIndexToBase(0x22, this.processor.PullLocalWord(0x2));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L11b7;
			goto L124b;

		L11b7:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1202;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L11e2;
			this.processor.RegAcc.Value16 = 0x3;
			goto L11e5;

		L11e2:
			this.processor.RegAcc.Value16 = 0x2;

		L11e5:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1140)));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x6));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.RegCount.LowByte = 0x3;
			goto L1210;

		L1202:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));
			this.processor.RegCount.LowByte = 0x4;

		L1210:
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L1237;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L1237:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6534);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L124b;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitGlobalWord(0x6534, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitGlobalWord(0x6530, this.processor.RegAcc.Value16);

		L124b:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateBestGuardianStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort TallyUnitsByDomainInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainInStack({participantIdx}, {troopIdx}, {arg3})");

			// function body
			this.processor.EnterFrame();
			this.processor.EmitGlobalWord(0x6532, 0x0);
			this.processor.RegAcc.Value16 = arg3;
			this.processor.EmitGlobalWord(0x6536, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitGlobalWord(0x6538, this.processor.RegAcc.Value16);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1280);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6532);
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();

			// Far return
			this.processor.Journal.CloseScope("TallyUnitsByDomainInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void TallyUnitsByDomainStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainStep({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6536);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L12a5;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L12c7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1298;
			goto L132a;

		L1298:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L12e5;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L1303;
			goto L132e;

		L12a5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));

		L12c1:
			this.processor.EmitGlobalWord(0x6532, this.processor.SumWord(this.processor.PullGlobalWord(0x6532), this.processor.RegAcc.Value16));
			goto L132e;

		L12c7:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));
			goto L12c1;

		L12e5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e));
			goto L12c1;

		L1303:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6538);
			this.processor.CompareWord((ushort)troopIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L132e;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L132e;

			L132a:
			this.processor.EmitGlobalWord(0x6532, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6532)));

		L132e:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("TallyUnitsByDomainStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort TallyUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeInStack({participantIdx}, {troopIdx}, {arg3})");

			// function body			
			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x135a);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			// Far return
			this.processor.Journal.CloseScope("TallyUnitsOfTypeInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void TallyUnitsOfTypeStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeStep({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x653a));
			if (this.processor.FlagSet.IsNotEqual) goto L137d;
			this.processor.EmitGlobalWord(0x653c, this.processor.IncrementWord(this.processor.PullGlobalWord(0x653c)));

		L137d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("TallyUnitsOfTypeStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort TallyUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagInStack({participantIdx}, {troopIdx}, {arg3})");

			// function body
			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13a9);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			// Far return
			this.processor.Journal.CloseScope("TallyUnitsWithFlagInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void TallyUnitsWithFlagStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagStep({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653a);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L13d2;
			this.processor.EmitGlobalWord(0x653c, this.processor.IncrementWord(this.processor.PullGlobalWord(0x653c)));

		L13d2:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("TallyUnitsWithFlagStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort TallySpecialUnitsInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsInStack({participantIdx}, {troopIdx})");

			// function body
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13f8);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			// Far return
			this.processor.Journal.CloseScope("TallySpecialUnitsInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void TallySpecialUnitsStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsStep({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1420;
			this.processor.EmitGlobalWord(0x653c, this.processor.DecrementWord(this.processor.PullGlobalWord(0x653c)));

		L1420:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1148)), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L1447;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1146));
			this.processor.EmitGlobalWord(0x653c, this.processor.SumWord(this.processor.PullGlobalWord(0x653c), this.processor.RegAcc.Value16));

		L1447:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("TallySpecialUnitsStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="fnPtr"></param>
		/// <exception cref="Exception"></exception>
		public void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr)
		{
			this.processor.Journal.OpenScope($"VisitEachUnitInStack({participantIdx}, {troopIdx}, 0x{funcAddr:x4})");

			// function body
			short priorTroopIdx = troopIdx;

			for (int idx = 0; idx < 10; idx++)
			{
				this.processor.ScaleIndexToBase(0xc, (ushort)priorTroopIdx);
				this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

				short freshTroopIdx = this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));

				switch (funcAddr)
				{
					case 0xf10:
						EliminateUnit(participantIdx, priorTroopIdx);
						break;

					case 0x1169:
						LocateBestGuardianStep(participantIdx, priorTroopIdx);
						break;

					case 0x1280:
						TallyUnitsByDomainStep(participantIdx, priorTroopIdx);
						break;

					case 0x135a:
						TallyUnitsOfTypeStep(participantIdx, priorTroopIdx);
						break;

					case 0x13a9:
						TallyUnitsWithFlagStep(participantIdx, priorTroopIdx);
						break;

					case 0x13f8:
						TallySpecialUnitsStep(participantIdx, priorTroopIdx);
						break;

					case 0x14a2:
						VerifyUnitTerrainFlag(participantIdx, priorTroopIdx);
						break;

					case 0x14f6:
						WipeUnitOrders(participantIdx, priorTroopIdx);
						break;

					case 0x1560:
						RestoreUnitOrders(participantIdx, priorTroopIdx);
						break;

					case 0x1593:
						RestoreUnitStatus(participantIdx, priorTroopIdx);
						break;

					case 0x1610:
						EliminateLandUnit(participantIdx, priorTroopIdx);
						break;

					case 0x1643:
						EliminateNonLandUnit(participantIdx, priorTroopIdx);
						break;

					case 0x1676:
						EliminateSeaUnit(participantIdx, priorTroopIdx);
						break;

					default:
						throw new Exception("Unknown function address");
				}

				priorTroopIdx = freshTroopIdx;

				if (priorTroopIdx == -1 || priorTroopIdx == troopIdx)
					break;
			}

			// Far return
			this.processor.Journal.CloseScope("VisitEachUnitInStack");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void VerifyUnitTerrainFlag(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VerifyUnitTerrainFlag({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L14de;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L14f3;

			L14de:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xfe));

		L14f3:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("VerifyUnitTerrainFlag");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void WipeUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"WipeUnitOrders({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1542;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfe));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.RegDest.Value16 = this.processor.BitOrWord(this.processor.RegDest.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1542;
			this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
			this.processor.RegAcc.LowByte = this.processor.DecrementByte(this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L1542:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RestoreIndexRegisters();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("WipeUnitOrders");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void RestoreUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitOrders({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1590;

			RestoreUnitStatus(participantIdx, troopIdx);

		L1590:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RestoreUnitOrders");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void RestoreUnitStatus(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitStatus({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsEqual) goto L15b3;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);

		L15b3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xcb);
			if (this.processor.FlagSet.IsEqual) goto L15dd;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L15dd:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L160d;

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);

		L160d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RestoreUnitStatus");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void EliminateLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateLandUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1640;

			EliminateUnit(participantIdx, troopIdx);

		L1640:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("EliminateLandUnit");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void EliminateNonLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateNonLandUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1673;
			
			EliminateUnit(participantIdx, troopIdx);

		L1673:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("EliminateNonLandUnit");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void EliminateSeaUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSeaUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L16a6;

			EliminateUnit(participantIdx, troopIdx);

		L16a6:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("EliminateSeaUnit");
		}

		/// <summary>
		/// ??? To be checked - Too many parameters calling CenterMapView
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"PanMapToPosition({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x2);

			this.processor.EmitLocalWord(0x2, 0x0);

			if (horizLoc < 16 && this.processor.PullGlobalSignedWord(0xd4cc) > 64)
			{
				horizLoc += 80;
			}

			if (horizLoc < this.processor.PullGlobalSignedWord(0xd4cc) + 2 ||
				horizLoc > this.processor.PullGlobalSignedWord(0xd4cc) + 13)
			{
				this.processor.EmitLocalWord(0x2, 0x1);
			}

			if (vertLoc < this.processor.PullGlobalSignedWord(0xd75e) + 2 ||
				vertLoc > this.processor.PullGlobalSignedWord(0xd75e) + 9)
			{
				this.processor.EmitLocalWord(0x2, 0x1);
			}
		
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1721;

			this.owner.Routing.TunePosition(horizLoc - 8);

			this.owner.MapOps.FocusMapView(participantIdx, (short)this.processor.RegAcc.Value16, vertLoc - 6);

		L1721:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("PanMapToPosition");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"MayOccupyTile({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame();

			this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L173e;
			this.processor.RegAcc.Value16 = 0;
			goto L174e;

		L173e:
			VerifyTileOccupation(participantIdx, horizLoc, vertLoc);

		L174e:
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("MayOccupyTile");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public ushort VerifyTileOccupation(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyTileOccupation({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x10);
			this.processor.PreserveIndexRegisters();

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, 0x1);
			goto L1795;

		L1770:
			this.processor.RegAcc.Value16 = 0;

		L1772:
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L178c;
			this.processor.RegAcc.Value16 = 0x1;
			goto L178e;

		L178c:
			this.processor.RegAcc.Value16 = 0;

		L178e:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L17cb;

		L1792:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L1795:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L179e;
			goto L18c8;

		L179e:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1770;
			this.processor.RegAcc.Value16 = 0x1;
			goto L1772;

		L17cb:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1792;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1792;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L17f4;
			goto L18c3;

		L17f4:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x8));
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x4));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81dd);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1856;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitLocalByte(0x10, this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), this.processor.RegAcc.LowByte));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa)] |= (sbyte)this.processor.RegCount.LowByte;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

		L1856:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L18c3;

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].TrueExtent;
			this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;
			
			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa)] |= (sbyte)this.processor.RegCount.LowByte;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

		L18c3:
			this.processor.RegAcc.Value16 = 0x1;
			goto L18ca;

		L18c8:
			this.processor.RegAcc.Value16 = 0;

		L18ca:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("VerifyTileOccupation");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsAdjacentForeignUnit({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x8, 0x1);
			goto L18e1;

		L18de:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L18e1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x9);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L192a;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x6));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L18de;
			
			if (this.processor.PullLocalSignedWord(0x4) == participantIdx)
				goto L18de;

			this.processor.RegAcc.Value16 = 0x1;
			goto L192c;

		L192a:
			this.processor.RegAcc.Value16 = 0;

		L192c:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ContainsAdjacentForeignUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void VisitHut(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VisitHut({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveIndexRegisters();

			if (participantIdx == 0)
				goto L1d4f;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1964;
			goto L1d4f;

		L1964:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				// !!! Added if city doesn't exist at that location
				(this.processor.PullLocalSignedWord(0xc) >= 0 && this.processor.PullLocalSignedWord(0xc) < 128) ?
					this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X : -1,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				// !!! Added if city doesn't exist at that location
				(this.processor.PullLocalSignedWord(0xc) >= 0 && this.processor.PullLocalSignedWord(0xc) < 128) ?
					this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y : -1);

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L19e7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L19cc;
			goto L1a7f;

		L19cc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L19d4;
			goto L1a8e;

		L19d4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L19dc;
			goto L1b88;

		L19dc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L19e4;
			goto L1b9a;

		L19e4:
			goto L1d4f;

		L19e7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) + 80,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1a20;
			goto L1b9a;

		L1a20:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1a8e;

			this.owner.Libc.CopyText(0xba06, "You have discovered\nan advanced tribe.\n");

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L1a51;

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L1a51:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			
			this.owner.Founding.EstablishCity(participantIdx,
				0,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				1);

			goto L1d30;

		L1a7f:
			if (this.owner.World.RoundTally == 0)
				goto L1a8e;

			if (this.owner.World.Season <= 1000)
				goto L1ac7;

			L1a8e:
			this.owner.Libc.CopyText(0xba06, "You have discovered\nvaluable metal deposits\nworth 50$\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.World.Participants[participantIdx].Gold += 50;
			goto L1d4f;

		L1ac7:
			this.owner.Libc.CopyText(0xba06, "You have discovered\nscrolls of ancient wisdom.\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			this.processor.EmitLocalWord(0x2, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(72));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

		L1b07:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x12));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1b63;
			
			this.processor.ScaleIndexToSource(0x16, this.processor.PullLocalWord(0x12));
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1b63;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1b63;
			
			this.owner.Advisors.EarnTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x12),
				0);
			
			this.processor.EmitLocalWord(0x12, 0xffff);
			goto L1b72;

		L1b63:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x48;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x12, this.processor.RegData.Value16);

		L1b72:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsLess) goto L1b7f;
			goto L1d4f;

		L1b7f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1b07;
			goto L1d4f;

		L1b88:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1b9a;
			
			if (this.owner.World.Participants[participantIdx].TownTally != 0) goto L1bff;

		L1b9a:
			this.owner.Libc.CopyText(0xba06, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			goto L1d28;

		L1bff:
			this.owner.Libc.CopyText(0xba06, "You have unleashed\na horde of barbarians!\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.processor.EmitLocalWord(0x10, 0x1);
			goto L1c91;

		L1c32:
			this.processor.RegAcc.Value16 = 0x6;

		L1c35:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			ConstructUnit(
				0,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));
			
			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0x6),
					this.processor.PullLocalSignedWord(0xa));
			}

		L1c71:
			this.owner.Helpers.VerifyValueSpan(
				4 - this.owner.World.Participants[participantIdx].TownTally,	1, 4);

			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L1c91:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1c9a;
			goto L1d4f;

		L1c9a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c71;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1c71;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1d00;
			goto L1c71;

		L1d00:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L1d1f;
			goto L1c32;

		L1d1f:
			this.processor.RegAcc.Value16 = 0x3;
			goto L1c35;

		L1d28:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			ConstructUnit(
				participantIdx,
				(short)((this.owner.Libc.Dice.DrawInt(2) != 0) ? 3 : 6),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

		L1d30:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			}

		L1d4f:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("VisitHut");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		public void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitCombat({participantIdx}, {troopIdx}, {arg3})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1d8b;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.RenderMapTile(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			goto L1f63;

		L1d8b:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			this.owner.MapOps.RenderMapTile(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1dbd;
			goto L1f5e;

		L1dbd:
			this.owner.TurnLogic.VerifyPositionOnScreen(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1dd8;
			goto L1f5e;

		L1dd8:
			this.processor.RegAcc.Value16 = arg3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.owner.TurnLogic.VerifyPositionOnScreen(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882)) + this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4)) + this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1e09;
			goto L1f5e;

		L1e09:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1e61;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1e40;
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1e61;

		L1e40:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)));

		L1e61:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0x50, 0, 0xf0, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0x50, 0);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Routing.TunePosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) -
					this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);

		L1ecb:
			this.processor.RegSource.Value16 = (ushort)(arg3 << 1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) *
						this.processor.PullLocalSignedWord(0x8)) +
					this.processor.PullLocalSignedWord(0x2) + 1,
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) *
						this.processor.PullLocalSignedWord(0x8)) +
					this.processor.PullLocalSignedWord(0x4) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
							(ushort)(((0x600 * participantIdx) + (0xc * troopIdx)) + 0x81d7)) + (participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

			this.owner.TextPainter.AwaitTime(1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x2));

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.RegDest.Value16,
				this.processor.PullLocalWord(0xc),
				0x10, 0x10,
				this.processor.PullGlobalWord(0xaa),
				this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x10);
			if (this.processor.FlagSet.IsGreater) goto L1f5e;
			goto L1ecb;

		L1f5e:
			this.owner.Startup.DisplayMouseCursor();

		L1f63:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChoreographUnitCombat");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void DisplayBattleScreen(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayBattleScreen({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x28);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			LocateStackLeader(participantIdx, troopIdx);

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, 0x0);

		L1f9a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1a), this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x1e));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0xc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fd3;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1fd3;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1f9a;

		L1fd3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x60);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

		L1fe2:
			this.owner.Dialogs.FloodBoxWithDoubleShadow(100, this.processor.PullLocalSignedWord(0x22),
				120, (this.processor.PullLocalWord(0x28) << 4) + 5, 3);

			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x5);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

		L2018:
			this.owner.MapOps.RenderUnitSprite(participantIdx,
				this.processor.PullLocalSignedWord(0x1e),
				106, this.processor.PullLocalSignedWord(0x20));

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x1e));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L206a;

			this.owner.Libc.AppendText(0xba06, " (V)");

		L206a:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, this.processor.PullLocalSignedWord(0x20), 15);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x1e));

			this.owner.Conquest.ObtainCityLabel(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df)));
			
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, this.processor.PullLocalSignedWord(0x20) + 8, 14);

			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.SumWord(this.processor.PullLocalWord(0x20), 0x10));
			this.processor.IncrementLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0xc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L20e4;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L20e4;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L20e4;
			goto L2018;

		L20e4:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2110;

		L20f5:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2104;
			goto L21b7;

		L2104:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L20f5;
			goto L21b7;

		L2110:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				105,
				(this.processor.PullLocalSignedWord(0x2) * 16) +
					this.processor.PullLocalSignedWord(0x22) + 4,
				112, 17, 3, 11);

			this.processor.CopyLocalWord(0x2, 0x1c);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsEqual) goto L2158;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsEqual) goto L21a0;
			goto L2161;

		L2158:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2161;
			this.processor.DecrementLocalWord(0x2);

		L2161:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				105,
				(this.processor.PullLocalSignedWord(0x1c) * 16) +
					this.processor.PullLocalSignedWord(0x22) + 4,
				112, 17, 11, 3);

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xd);
			if (this.processor.FlagSet.IsEqual) goto L219b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L21ae;

		L219b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			goto L21ef;

		L21a0:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			if (this.processor.FlagSet.IsLessOrEqual) goto L2161;
			this.processor.IncrementLocalWord(0x2);
			goto L2161;

		L21ae:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L21b7;
			goto L2110;

		L21b7:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L21c6;
			goto L2256;

		L21c6:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x64);
			if (this.processor.FlagSet.IsGreater) goto L21d0;
			goto L2256;

		L21d0:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xdc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2256;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x5);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);

		L21ef:
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L2256;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2256;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1a));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsEqual) goto L2233;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L2233:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2256;
			goto L1fe2;

		L2256:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayBattleScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void LocateNearestFriendlyCity(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateNearestFriendlyCity({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x16);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x6, 0x3e7);
			this.processor.EmitLocalWord(0xc, 0xffff);
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L2330;

		L2287:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegCount.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x28);
			if (this.processor.FlagSet.IsLessOrEqual) goto L22c9;
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xa));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L22c9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2306;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);

		L2306:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L232d;
			this.processor.CopyLocalWord(0x4, 0x6);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x2, 0xc);

		L232d:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L2330:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2355;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L232d;

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ParticipantIdx != participantIdx)
				goto L232d;
			goto L2287;

		L2355:
			this.processor.EmitLocalWord(0x8, 0x0);

		L235a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x8));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x17);
			if (this.processor.FlagSet.IsEqual) goto L2374;
			goto L2416;

		L2374:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x28);
			if (this.processor.FlagSet.IsLessOrEqual) goto L23b6;
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xa));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L23b6:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L23f5;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);

		L23f5:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2416;
			this.processor.CopyLocalWord(0x4, 0x6);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

		L2416:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2423;
			goto L235a;

		L2423:
			this.processor.EmitLocalWord(0x10, 0x0);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Helpers.ObtainDistanceFromOffsets(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) - this.processor.PullLocalSignedWord(0x14),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) - this.processor.PullLocalSignedWord(0x16));

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x12, 0x1);

		L245c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L24a7;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L24f9;

		L24a7:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L24cc;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L24f9;

		L24cc:
			this.owner.Helpers.ObtainDistanceFromOffsets(
				this.processor.PullLocalSignedWord(0xa) - this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0xe) - this.processor.PullLocalSignedWord(0x16));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L24f9;
			this.processor.CopyLocalWord(0x4, 0x6);
			this.processor.CopyLocalWord(0x12, 0x10);

		L24f9:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L2505;
			goto L245c;

		L2505:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateNearestFriendlyCity");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1})");

			// function body
			if (this.owner.World.RerunContentSpan + 3 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2})");

			// function body
			if (this.owner.World.RerunContentSpan + 4 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		/// <param name="data3"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3})");

			// function body
			if (this.owner.World.RerunContentSpan + 5 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content3;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		/// <param name="data3"></param>
		/// <param name="data4"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3}, {content4})");

			// function body
			if (this.owner.World.RerunContentSpan + 6 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content3;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content4;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenderBusyIndicator()
		{
			this.processor.Journal.OpenScope("RenderBusyIndicator()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xc0);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x78);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				this.processor.PullLocalWord(0x4),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 8),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x18),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, 0xc0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x10),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, 0xc0);

			this.processor.EmitLocalWord(0x2, 0x1);

		L26cc:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x18),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, (ushort)(this.processor.PullLocalWord(0x2) << 3));

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 8),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, (ushort)(this.processor.PullLocalWord(0x2) << 3));

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x18);
			if (this.processor.FlagSet.IsLess) goto L26cc;
			this.processor.EmitLocalWord(0x2, 0x1);

		L272a:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				this.processor.PullLocalWord(0x4),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(this.processor.PullLocalWord(0x2) << 3), 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x10),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(this.processor.PullLocalWord(0x2) << 3), 0xc0);

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x27);
			if (this.processor.FlagSet.IsLess) goto L272a;
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderBusyIndicator");
		}
	}
}
