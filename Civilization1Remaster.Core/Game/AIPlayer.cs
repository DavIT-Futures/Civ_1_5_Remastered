using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x25fb.
	/// </summary>
	public class AIPlayer
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public AIPlayer(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void OperateAITurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"OperateAITurn({participantIdx})");

			// function body
			this.processor.EnterFrame(0x68);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x54, 0x0);

		L0011:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x54)].TownTally = 0;
			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x54)].Guard = 0;
			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x54)].Assault = 0;
			this.processor.IncrementLocalWord(0x54);
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x10);
			if (this.processor.FlagSet.IsLess) goto L0011;

			this.processor.EmitLocalWord(0x50, 0);
			goto L005e;

		L003b:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));

		L003e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0xd);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L005b;
			this.processor.ScaleIndexToBase(0xd, this.processor.PullLocalWord(0x50));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x52));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438), 0x0);
			goto L003b;

		L005b:
			this.processor.EmitLocalWord(0x50, this.processor.IncrementWord(this.processor.PullLocalWord(0x50)));

		L005e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L006b;
			this.processor.EmitLocalWord(0x52, 0x0);
			goto L003e;

		L006b:
			this.owner.World.Participants[participantIdx].MartialMight = 0;
			this.owner.World.Participants[participantIdx].ColonistTally = 0;
			this.owner.World.Participants[participantIdx].TroopTally = 0;
			this.processor.EmitLocalWord(0x54, 0);
			goto L01b7;

		L0084:
			this.owner.World.Participants[participantIdx].TroopTally++;

		L008d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x113e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1140)));
			this.owner.World.Participants[participantIdx].MartialMight += (short)this.processor.RegAcc.Value16;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xef));

			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L00db;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x10));

		L00db:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0148;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x64, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Assault +=
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1140));
			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Guard += 
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x113e));

			goto L016c;

		L0148:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L016c;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x10));

		L016c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));

			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148));
			this.processor.RegAcc.LowByte = 0x2;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x2);
			this.processor.RegBase.Value16 = 0xd;
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, this.processor.RegBase.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x2);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), this.processor.RegCount.LowByte));

		L01b4:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L01b7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01f0;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x64, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L01b4;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x19);
			if (this.processor.FlagSet.IsEqual) goto L01b4;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L01e4;
			goto L0084;

		L01e4:
			this.owner.World.Participants[participantIdx].ColonistTally++;
			goto L008d;

		L01f0:
			this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[participantIdx].TroopTally / 8, 3, 99);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].GrandTownExtent = 0;
			this.owner.World.Participants[participantIdx].TownTally = 0;
			this.processor.EmitLocalWord(0x54, 0);

		L0228:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.processor.PullLocalWord(0x54)] = 0;

			this.processor.IncrementLocalWord(0x54);
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L0228;
			this.processor.EmitLocalWord(0x54, 0x0);
			goto L03ed;

		L024c:
			this.processor.RegAcc.Value16 = 0x5;

		L024f:
			SendUnitToStrategicLocation(participantIdx,
				(short)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.X,
				(short)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.Y,
				1, (short)this.processor.RegAcc.Value16);

			goto L0397;

		L026d:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.World.Participants[participantIdx].TownTally++;
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x54));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.Y);

			this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].TownTally++;

			this.owner.World.Participants[participantIdx].GrandTownExtent +=
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].TrueExtent;

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x54)].PresentMakingIdx < 0)
				goto L02c8;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.ScaleIndex(0x38, (ushort)participantIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.owner.World.Towns[this.processor.PullLocalWord(0x54)].PresentMakingIdx]++;

		L02c8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x54));
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.X);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x52, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xd, this.processor.PullLocalWord(0x50));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x52));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438), this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x1));
			this.processor.EmitLocalWord(0x4c, 0x0);

		L0304:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x54));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Unfamiliar[this.processor.PullLocalWord(0x4c)], 0xff);
			if (this.processor.FlagSet.IsEqual) goto L038b;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Unfamiliar[this.processor.PullLocalWord(0x4c)];
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3f);
			this.processor.RegCount.Value16 = 0x22;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x64, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140)));
			this.owner.World.Participants[participantIdx].MartialMight = (short)this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.Y);

			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Unfamiliar[this.processor.PullLocalWord(0x4c)];
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3f);
			this.processor.RegCount.Value16 = 0x22;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x66, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x68, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x66);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));

			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Assault = 
				(short)this.processor.RegAcc.Value16;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x66);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e));

			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Guard +=
				(short)this.processor.RegAcc.Value16;

		L038b:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0397;
			goto L0304;

		L0397:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x54));

			this.owner.MapOps.ObtainUnitAtTile(
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.Y);

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L03ea;
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Unfamiliar[0], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L03ea;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L03cc;
			this.processor.RegAcc.Value16 = 0x4;
			goto L03cf;

		L03cc:
			this.processor.RegAcc.Value16 = 0x2;

		L03cf:
			SendUnitToStrategicLocation(participantIdx,
				(short)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.X,
				(short)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.Y,
				2, (short)this.processor.RegAcc.Value16);

		L03ea:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L03ed:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x80);
			if (this.processor.FlagSet.IsLess) goto L03f7;
			goto L049d;

		L03f7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x54));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x54)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L03ea;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x54));
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].ParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L041d;
			goto L026d;

		L041d:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc8);
			if (this.processor.FlagSet.IsGreater) goto L0458;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x54)].Place.Y];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0458;
			this.processor.CompareWord((ushort)this.owner.World.PersonParticipantIdx, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0458;
			goto L0397;

		L0458:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x54));

			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x54)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			
			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.Towns[this.processor.PullLocalWord(0x54)].ParticipantIdx] & 0x102) == 2)
				goto L0397;

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x54);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L048d;
			goto L0397;

		L048d:
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x54)].StructureMarkers0, 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L0497;
			goto L024c;

		L0497:
			this.processor.RegAcc.Value16 = 0x3;
			goto L024f;

		L049d:
			this.owner.World.Participants[participantIdx].SoilTally = 0;
			this.processor.EmitLocalWord(0x50, 0x0);
			goto L04dd;

		L04af:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));

		L04b2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0xd);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L04da;
			this.processor.ScaleIndexToBase(0xd, this.processor.PullLocalWord(0x50));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x52));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04af;
			this.owner.World.Participants[participantIdx].SoilTally++;
			goto L04af;

		L04da:
			this.processor.EmitLocalWord(0x50, this.processor.IncrementWord(this.processor.PullLocalWord(0x50)));

		L04dd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L04ea;
			this.processor.EmitLocalWord(0x52, 0x0);
			goto L04b2;

		L04ea:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.owner.World.Participants[participantIdx].Landmasses[0].Approach = 0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe6b6), 0x0);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x2a6e, this.processor.BitAndWord(this.processor.PullGlobalWord(0x2a6e), this.processor.RegAcc.Value16));
			this.processor.EmitGlobalWord(0x6ed4, 0x0);
			this.processor.EmitLocalWord(0x58, 0x1);

		L0516:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x58)] & 3) == 1)
			{
				this.processor.EmitGlobalWord(0x6ed4, 0x1);
			}
		
			this.processor.IncrementLocalWord(0x58);
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0516;

			this.processor.EmitLocalWord(0x48, 0xffff);
			this.processor.EmitLocalWord(0x58, 0x1);
			goto L07cc;

		L0548:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L054b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Assault == 0)
				goto L05a1;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L05a1;

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x54)] & 2) == 0)
				goto L05a1;
			
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x68, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Guard;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16,
				(ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Assault);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Guard);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05a1;

			this.processor.IncrementLocalWord(0x2);

		L05a1:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Assault;
			this.processor.EmitLocalWord(0x4e, this.processor.SumWord(this.processor.PullLocalWord(0x4e), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].TownTally;
			this.processor.EmitLocalWord(0x5c, this.processor.SumWord(this.processor.PullLocalWord(0x5c), this.processor.RegAcc.Value16));

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Assault == 0)
				goto L05e2;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x54)] & 2) != 0)
			{
				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0x2;
			}

			this.processor.EmitLocalWord(0x62, this.processor.BitOrWord(this.processor.PullLocalWord(0x62), this.processor.RegAcc.Value16));

		L05e2:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L05e5:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x8);
			if (this.processor.FlagSet.IsLess) goto L05ee;
			goto L068d;

		L05ee:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Assault == 0)
				goto L054b;

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Guard;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16,
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Guard);
			if (this.processor.FlagSet.IsLess) goto L062d;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].TownTally != 0) goto L062d;
			goto L054b;

		L062d:
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x54)] & 3) == 1 ||
				(this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x54)] & 0x100) != 0)
				goto L064f;
			goto L054b;

		L064f:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x68, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Assault <
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Guard)
				goto L0687;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x54)].Landmasses[this.processor.PullLocalWord(0x58)].Guard <
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Assault)
				goto L0687;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].TownTally != 0)
				goto L0548;

		L0687:
			this.processor.EmitLocalWord(0x5e, this.processor.IncrementWord(this.processor.PullLocalWord(0x5e)));
			goto L054b;

		L068d:
			if (this.owner.World.Participants[0].Landmasses[this.processor.PullLocalWord(0x58)].TownTally != 0)
			{
				this.processor.IncrementLocalWord(0x5e);
			}

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Assault;

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdcfe)));
			if (this.processor.FlagSet.IsGreater) goto L06ef;
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].TownTally;

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5c));
			this.processor.RegCount.Value16 = 0x6;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Landmasses[this.processor.PullLocalWord(0x58)].ConstructionSpotTally);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0707;

		L06ef:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 7);
			if (this.processor.RegAcc.Value16 != 0)
			{
				this.processor.RegAcc.Value16 = 0x5;
				goto L0709;
			}

		L0707:
			this.processor.RegAcc.Value16 = 0;

		L0709:
			this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach = 
				(short)this.processor.RegAcc.Value16;
			
			if (this.processor.PullLocalWord(0x5e) != 0)
			{
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach = 1;
			}

			if (this.processor.PullLocalWord(0x2) != 0)
			{
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach = 2;
			}

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Assault == 0 &&
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].TownTally == 0 &&
				this.processor.PullLocalWord(0x62) != 1)
			{
				this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach = 1;
			}

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].TownTally;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L076d;
			this.processor.EmitLocalWord(0x48, this.processor.RegSource.Value16);

		L076d:
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach !=
				this.processor.PullLocalSignedWord(0x60))
			{
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
				if (this.processor.FlagSet.IsNotEqual) goto L079d;

				RefreshCityProduction(participantIdx, this.processor.PullLocalWord(0x58));
			}

		L079d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Assault != 0 &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x58)].TownTally > 1)
			{
				this.processor.RegAcc.Value16 = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
				this.processor.EmitGlobalWord(0x2a6e, this.processor.BitOrWord(this.processor.PullGlobalWord(0x2a6e), this.processor.RegAcc.Value16));
			}

			this.processor.IncrementLocalWord(0x58);

		L07cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0802;
			this.processor.EmitLocalWord(0x5e, 0x0);
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x62, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x5c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = 
				(ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x58)].Approach;
			this.processor.EmitLocalWord(0x60, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, 0x1);
			goto L05e5;

		L0802:
			if (participantIdx == 0x0)
				goto L0c97;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L081c;
			goto L0c97;

		L081c:
			if (this.owner.World.Participants[participantIdx].ColonistTally != 0 ||			
				this.owner.World.Participants[participantIdx].TownTally != 0 ||
				participantIdx == this.owner.World.PersonParticipantIdx) goto L0845;

			this.owner.NewGameWizard.EliminateCivilization(participantIdx, 0);

		L0845:
			this.processor.RegAcc.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L08c1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xe3c2), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L08b2;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0876;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xdb42), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0876;

			this.owner.Politics.SwitchGovernment(participantIdx, 4);
			
			goto L08c1;

		L0876:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2b);
			if (this.processor.RegAcc.Value16 == 0 ||
				this.owner.World.Participants[participantIdx].TownTally <= 10)
				goto L089a;

			this.owner.Politics.SwitchGovernment(participantIdx, 3);

			goto L08c1;

		L089a:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L08c1;

			this.owner.Politics.SwitchGovernment(participantIdx, 2);

			goto L08c1;

		L08b2:
			this.owner.Politics.SwitchGovernment(participantIdx, 1);

		L08c1:
			this.processor.EmitLocalWord(0x58, 0x0);

		L08c6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x44), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x58);
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), 0x20);
			if (this.processor.FlagSet.IsLess) goto L08c6;
			this.processor.EmitLocalWord(0x54, 0x0);
			goto L09f8;

		L08e2:
			this.processor.EmitLocalWord(0x4a, 0x270f);
			this.processor.EmitLocalWord(0x62, 0xffff);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x52, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullLocalSignedWord(0x52));

			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x54);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0xf);
			if (this.processor.FlagSet.IsEqual) goto L0929;
			goto L0a2c;

		L0929:
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1136)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0980;

			if (this.owner.World.Participants[participantIdx].RegimeKind > 1)
				goto L0966;

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Approach != 5)
				goto L0a2c;

		L0966:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0980;
			goto L0a2c;

		L0980:
			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullLocalSignedWord(0x52));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0998;
			goto L0a2c;

		L0998:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullLocalSignedWord(0x52));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L09c1;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0a2c;

		L09c1:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullLocalSignedWord(0x52));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0a2c;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x5a)].Approach == 2)
				goto L0a2c;

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x54));

		L09f5:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L09f8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0a02;
			goto L0c97;

		L0a02:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L09f5;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L09f5;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x19);
			if (this.processor.FlagSet.IsEqual) goto L0a2a;
			goto L08e2;

		L0a2a:
			goto L09f5;

		L0a2c:
			this.processor.EmitLocalWord(0x58, 0x0);
			goto L0ad2;

		L0a34:
			this.processor.RegAcc.Value16 = 0x4;

		L0a37:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x46));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb2be));
			goto L0a7a;

		L0a43:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x58));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe598)),
				this.processor.PullLocalSignedWord(0x52),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe798)));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x44));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb2be));

		L0a7a:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.IncrementWord(this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xc);
			if (this.processor.FlagSet.IsNotEqual) goto L0a9f;
			goto L0c0e;

		L0a9f:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb2be));
			this.processor.EmitLocalByte(0x68, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x2);
			if (this.processor.FlagSet.IsLess) goto L0acf;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x46));
			if (this.processor.FlagSet.IsLess) goto L0ac0;
			goto L0be3;

		L0ac0:
			this.processor.RegSource.Value16 = this.processor.RegBase.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x44)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0acf;
			goto L0be3;

		L0acf:
			this.processor.EmitLocalWord(0x58, this.processor.IncrementWord(this.processor.PullLocalWord(0x58)));

		L0ad2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), 0x20);
			if (this.processor.FlagSet.IsLess) goto L0adb;
			goto L0c4d;

		L0adb:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x58));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe2c2)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0acf;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe2c2));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0b28;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe598)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe798)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5a));
			if (this.processor.FlagSet.IsEqual) goto L0b6c;

		L0b28:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x54));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x68, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L0acf;
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xe2c2));
			this.processor.EmitLocalByte(0x66, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x68));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x66);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b6c;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0b6c;
			goto L0acf;

		L0b6c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d7);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0xf);
			if (this.processor.FlagSet.IsEqual) goto L0b88;
			goto L0a43;

		L0b88:
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x58));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xb2be)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0b9c;
			goto L0acf;

		L0b9c:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x58);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.RegCount.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0bae;
			goto L0acf;

		L0bae:
			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x50),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe598)),
				this.processor.PullLocalSignedWord(0x52),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe798)));

			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0bdd;
			goto L0a34;

		L0bdd:
			this.processor.RegAcc.Value16 = 0x2;
			goto L0a37;

		L0be3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0c0e;
			goto L0acf;

		L0c0e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0c19;
			goto L0acf;

		L0c19:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb2be));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c3e;
			goto L0acf;

		L0c3e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x58, 0x62);
			goto L0acf;

		L0c4d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x62), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0c56;
			goto L09f5;

		L0c56:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x54));
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x62));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe598));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0xe798));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x10));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xf0));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x62);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegFrame.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 - 0x44), this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 - 0x44))));
			goto L09f5;

		L0c97:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("OperateAITurn");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort ShiftAIUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ShiftAIUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();

			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ccf;

			ManageBarbarianUnit(participantIdx, troopIdx);

			goto L2fd0;

		L0ccf:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L0cf3;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0cf3;

		L0ced:
			this.processor.RegAcc.Value16 = 0x68;
			goto L2fd0;

		L0cf3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xe);
			if (this.processor.FlagSet.IsEqual) goto L0d0d;
			goto L0f13;

		L0d0d:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0d17;
			goto L0f13;

		L0d17:
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a));
			this.processor.HalveAccumulator();
			this.processor.RegCount.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ced;
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x4, 0xffff);
			this.processor.EmitLocalWord(0x40, 0x0);

		L0d47:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L0db5;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)((sbyte)participantIdx);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0db5;
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0db5;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.CopyLocalWord(0x40, 0x4);

		L0db5:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0d47;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0dc8;
			goto L0f13;

		L0dc8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L0dd1;
			goto L0f13;

		L0dd1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0e16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x4));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			goto L0f13;

		L0e16:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x34, 0xffff);
			this.processor.EmitLocalWord(0x40, 0x0);
			goto L0e70;

		L0e27:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x40));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			
			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e92;

		L0e6d:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L0e70:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ee2;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x40));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0e6d;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0e27;
			goto L0e6d;

		L0e92:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x40));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e6d;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.CopyLocalWord(0x40, 0x34);
			goto L0e6d;

		L0ee2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0eeb;
			goto L16b3;

		L0eeb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L0f13:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2c, 0x0);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x30, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.EmitLocalWord(0x2a, this.processor.RegAcc.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7), 0xff);

			this.owner.Helpers.LocateNearestUnit(participantIdx, troopIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x8);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7), this.processor.RegAcc.LowByte);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148));
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].Approach;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)((sbyte)participantIdx);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x19);
			if (this.processor.FlagSet.IsEqual) goto L0ff1;
			goto L12a3;

		L0ff1:
			this.processor.EmitLocalWord(0x28, 0xffff);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L10ff;

		L0ffe:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L100c;
			this.processor.EmitLocalWord(0x3a, this.processor.SubtractWord(this.processor.PullLocalWord(0x3a), 0x2));
			goto L1010;

		L100c:
			this.processor.EmitLocalWord(0x3a, this.processor.SubtractWord(this.processor.PullLocalWord(0x3a), 0x63));

		L1010:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1013:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L106e;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1010;
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			
			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x20)] & 0x82) != 0x80)
				goto L0ffe;

			this.processor.IncrementLocalWord(0x3a);
			goto L1010;

		L106e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x2;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x3a, this.processor.SumWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16));

		L1083:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L10fc;
			this.processor.EmitLocalWord(0x3e, 0x0);
			this.processor.EmitLocalWord(0x22, 0x0);
			goto L109a;

		L1097:
			this.processor.EmitLocalWord(0x22, this.processor.IncrementWord(this.processor.PullLocalWord(0x22)));

		L109a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L10ea;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x22));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x22)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1097;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x22)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L1097;
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			
			this.owner.Helpers.ObtainDistance(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x22)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y,
				this.owner.World.Towns[this.processor.PullLocalWord(0x22)].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x10);
			if (this.processor.FlagSet.IsGreater) goto L1097;
			this.processor.EmitLocalWord(0x3e, 0x1);

		L10ea:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10fc;
			this.processor.CopyLocalWord(0x3a, 0x28);
			this.processor.CopyLocalWord(0xc, 0x34);

		L10fc:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L10ff:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L1109;
			goto L11ad;

		L1109:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L1123;
			goto L1083;

		L1123:
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].StructureMarkers1, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L112e;
			goto L1083;

		L112e:
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)this.owner.World.Participants[this.processor.RegAcc.Value16].MartialMight;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].MartialMight;
			this.processor.EmitLocalWord(0x5e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x5e));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsLess) goto L1176;
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.RegSource.Value16 >> 1] & 8) != 0)
				goto L1176;

			this.processor.ScaleIndexToBase(0x38, this.processor.PullLocalWord(0x3a));

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x3a)].LiveTroops[25] != 0)
				goto L1083;

			this.processor.CompareWord(this.processor.PullLocalWord(0x5e), this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsLess) goto L1176;
			goto L1083;

		L1176:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x3a)] & 0x82) != 0x80)
				goto L1083;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent, this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsGreater) goto L11a0;
			goto L1083;

		L11a0:
			this.processor.EmitLocalWord(0x3a, 0x0);
			this.processor.EmitLocalWord(0x40, 0x0);
			goto L1013;

		L11ad:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L11b6;
			goto L16b3;

		L11b6:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[25] <= 1)
				goto L16b3;

			this.processor.CopyLocalWord(0x34, 0xc);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L1200;

		L11fd:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1200:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1209;
			goto L16b3;

		L1209:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L11fd;

			this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);

			this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = -2;
			}
		
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegAcc.LowByte = this.processor.BitXorByte(this.processor.RegAcc.LowByte, 0x4);
			goto L2fd0;

		L12a3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L1307;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L12c5;

		L12c0:
			this.processor.RegAcc.Value16 = 0;
			goto L2fd0;

		L12c5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L12f9;

		L12d7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			goto L12c0;

		L12f9:
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

		L1307:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1321;

			DisableStrategicLocation(participantIdx, 0,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				0);

		L1321:
			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x30) + 0x50,
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1341;
			goto L1434;

		L1341:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x3);
			if (this.processor.FlagSet.IsLess) goto L134a;
			goto L1434;

		L134a:
			this.processor.EmitLocalWord(0x2c, 0x0);
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L1359;

		L1356:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1359:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L13d9;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x26) + 0x50,
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L13ac;
			this.processor.CopyLocalWord(0x10, 0x28);
			this.processor.CopyLocalWord(0x40, 0x2c);

		L13ac:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.owner.MapOps.ContainsHut(
				this.processor.RegAcc.Value16,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1356;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1356;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			goto L2fd0;

		L13d9:
			if (this.owner.World.RoundTally != 0) goto L13f1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd76a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L13f1;
			this.processor.EmitLocalWord(0x28, 0xf);
			this.processor.EmitLocalWord(0x2c, 0x0);

		L13f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1434;
			this.processor.RegAcc.Value16 = 0xf;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x28));
			if (this.processor.FlagSet.IsGreater) goto L1434;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1434;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L141d;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1417;
			goto L2fcd;

		L1417:
			this.processor.RegAcc.Value16 = 0x62;
			goto L2fd0;

		L141d:
			AttachStrategicLocation(participantIdx, 
				this.processor.PullLocalWord(0x30),
				this.processor.PullLocalWord(0x36),
				0, 2);

		L1434:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L14ac;
			if (this.owner.World.HardnessTier == 0) goto L14ac;

			if (this.processor.PullLocalWord(0x34) < 128)
			{
				this.processor.RegSource.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			}
			else
			{
				this.processor.RegSource.Value16 = 0xffff;
			}
			this.processor.CompareWord((ushort)this.owner.World.PersonParticipantIdx, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L14ac;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L14ac;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L14ac;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L14ac;
			
			this.processor.RegDest.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].FoundScienceTally >= this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally)
				goto L14ac;

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x30) + 0x50,
				this.processor.PullLocalSignedWord(0x36) + 0x32);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9);
			if (this.processor.FlagSet.IsLess) goto L14ac;
			this.processor.RegAcc.Value16 = 0xe;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsGreater) goto L14ac;
			goto L1417;

		L14ac:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L14c1;
			goto L166c;

		L14c1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L14ca;
			goto L168f;

		L14ca:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L14e7;

		L14e1:
			this.processor.RegAcc.Value16 = 0x66;
			goto L2fd0;

		L14e7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x81d7);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, 0x1a);

			this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx);

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1545;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x3a));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x20));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140)), this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLess) goto L14e1;

		L1545:
			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L155f;
			this.processor.RegAcc.Value16 = 0x3;
			goto L1562;

		L155f:
			this.processor.RegAcc.Value16 = 0x4;

		L1562:
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x34)].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1579;
			this.processor.DecrementLocalWord(0x6);
			goto L1582;

		L1579:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1582;
			this.processor.IncrementLocalWord(0x6);

		L1582:
			this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, troopIdx, 2);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsLessOrEqual) goto L15c5;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				2, 2);

		L15c5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsLess) goto L15e1;
			goto L14e1;

		L15e1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d4);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x8));

			this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx);

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)troopIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L1645;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 4);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1645;
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.LowByte);
			goto L14e1;

		L1645:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d4);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L166c;
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0xf2));

		L166c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L167b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L167b;
			goto L1f44;

		L167b:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			goto L1738;

		L168f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L166c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L166c;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				2, 4);

		L16b3:
			this.processor.RegAcc.Value16 = 0x20;
			goto L2fd0;

		L16b9:
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L16c4;
			goto L1752;

		L16c4:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L16f8;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1148));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x1e, this.processor.BitOrWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1732;
			goto L172f;

		L16f8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L1732;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x2c);
			this.processor.IncrementLocalWord(0x2c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1732;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x20));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xf7));

		L172f:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L1732:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x20));

		L1738:
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1752;
			goto L16b9;

		L1752:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L175d;
			this.processor.RegAcc.Value16 = 0x1;
			goto L1760;

		L175d:
			this.processor.RegAcc.Value16 = 0x3;

		L1760:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x54));
			if (this.processor.FlagSet.IsGreater) goto L176e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L176e;
			goto L198e;

		L176e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1777;
			goto L198e;

		L1777:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L17d4;

		L1783:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].Assault >= 16)
				goto L1800;

		L17d1:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L17d4:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L17de;
			goto L18d1;

		L17de:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L17d1;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L17d1;
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1783;
			goto L17d1;

		L1800:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));

			this.owner.MapOps.ObtainUnitAtTile(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1846;

			this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 5);

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1846;
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x10));

		L1846:
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach == 5)
				goto L185f;

			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x10));
			goto L1880;

		L185f:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Guard;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x12, this.processor.SubtractWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));

		L1880:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L188b;
			goto L17d1;

		L188b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			goto L17d1;

		L18d1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L18dc;
			this.processor.RegAcc.Value16 = 0x4;
			goto L18df;

		L18dc:
			this.processor.RegAcc.Value16 = 0x2;

		L18df:
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x12));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18f0;
			goto L1f44;

		L18f0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1922;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				0, this.processor.PullLocalSignedWord(0x12));

		L1922:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1955;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				2, this.processor.PullLocalSignedWord(0x12));

		L1955:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L195e;
			goto L1f44;

		L195e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				1, this.processor.PullLocalSignedWord(0x12));

			goto L1f44;

		L198e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1997;
			goto L1c37;

		L1997:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L1a68;

		L199f:
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach != 1)
				goto L19ca;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				3, 5);

		L19ca:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x75;
			goto L2fd0;

		L19ea:
			this.processor.TestBitsByte((byte)(this.owner.World.RoundTally & 0xff), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1a06;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L1a06:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1a20;
			goto L1c37;

		L1a20:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual)
				goto L1c37;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1a62;
			goto L1c22;

		L1a62:
			goto L1c37;

		L1a65:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1a68:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L1a06;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1a65;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1a65;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1ad2;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1a65;

		L1ad2:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1b00;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.processor.RegAcc.Value16, 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L1b00;
			goto L1a65;

		L1b00:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L1b3d;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L1b3d;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0xf);
			if (this.processor.FlagSet.IsEqual) goto L1b3d;
			goto L1a65;

		L1b3d:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, 0x0);
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Assault >= 16)
				goto L1b6c;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach != 5)
				goto L1b87;

		L1b6c:
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, this.processor.PullLocalByte(0x1e));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L1b8c;

		L1b87:
			this.processor.EmitLocalWord(0x3c, 0x1);

		L1b8c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1bc1;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4a));
			if (this.processor.FlagSet.IsNotEqual) goto L1bc1;
			this.processor.EmitLocalWord(0x3c, 0x1);

		L1bc1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1bca;
			goto L19ea;

		L1bca:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			if (this.owner.World.Landmasses[this.processor.PullLocalWord(0x4a)].ConstructionSpotTally < 5)
				goto L19ea;

			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L1be2;
			goto L19ea;

		L1be2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x30);
			if (this.processor.FlagSet.IsLess) goto L1beb;
			goto L19ea;

		L1beb:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L1c05;
			goto L199f;

		L1c05:
			MayAIEnterTile(participantIdx, 
				this.processor.PullLocalWord(0x30),
				this.processor.PullLocalWord(0x36));
			
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreater) goto L1c1f;
			goto L199f;

		L1c1f:
			goto L2fd0;

		L1c22:
			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e),
				3, 3);

		L1c37:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c46;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1c46;
			goto L1f44;

		L1c46:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1c60;
			goto L1d49;

		L1c60:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1e), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1c6f;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1c6f;
			goto L1d49;

		L1c6f:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L1c89;
			goto L1d49;

		L1c89:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			goto L1d3f;

		L1c9a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ParticipantIdx;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ConditionMarker == 0xff)
				goto L1d3b;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1d3b;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x3a)] & 0x102) == 2)
				goto L1d3b;

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing < 7)
				goto L1ce8;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1d3b;

		L1ce8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1d3b;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1d3b:
			this.processor.EmitLocalWord(0x40, this.processor.SumWord(this.processor.PullLocalWord(0x40), 0x8));

		L1d3f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1d49;
			goto L1c9a;

		L1d49:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1d63;
			goto L1e78;

		L1d63:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L1d6d;
			goto L1e78;

		L1d6d:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x10, 0xffff);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1d90;
			this.processor.CopyLocalWord(0x1a, 0x10);

		L1d90:
			this.processor.EmitLocalWord(0x40, 0x0);

		L1d95:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ParticipantIdx;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ConditionMarker == 0xff)
				goto L1e6b;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1dba;
			goto L1e6b;

		L1dba:
			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1ddf;
			goto L1e6b;

		L1ddf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1e6b;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach == 5)
				goto L1e6b;

			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, this.processor.PullLocalByte(0x1e));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1e35;
			this.processor.EmitLocalWord(0x12, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x12), 0x1));

		L1e35:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e6b;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x40));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1e6b:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e78;
			goto L1d95;

		L1e78:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1e92;
			goto L1f44;

		L1e92:
			this.processor.EmitLocalWord(0x40, 0x2);
			goto L1e9c;

		L1e99:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1e9c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x18);
			if (this.processor.FlagSet.IsLess) goto L1ea5;
			goto L1f44;

		L1ea5:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(9));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.Routing.TunePosition(
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)) *
					this.processor.PullLocalSignedWord(0x40)) +
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x40));
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1e99;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e99;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1e99;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].TownTally == 0) goto L1f36;
			goto L1e99;

		L1f36:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1f44:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fb2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fb2;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x34));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L1fb2;
			
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 2) != 0)
				goto L1fb2;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L1fb2;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L1fb2;
			this.processor.RegAcc.Value16 = 0x50;
			goto L2fd0;

		L1fb2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1fbb;
			goto L2520;

		L1fbb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1fc4;
			goto L241e;

		L1fc4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x2);
			if (this.processor.FlagSet.IsLess) goto L1fdf;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				2, 2);

		L1fdf:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = 0x5;
			}
			else
			{
				this.processor.RegSource.Value16 = 0x4;
			}
		
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x42));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x44a)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2014;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L2014;
			goto L213b;

		L2014:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L201d;
			goto L213b;

		L201d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2026;
			goto L213b;

		L2026:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L203b;
			goto L213b;

		L203b:
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2060;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L2060;

			this.owner.MapOps.ContainsSpecialResource(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2060;
			goto L213b;

		L2060:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L206a;
			goto L213b;

		L206a:
			this.owner.TurnLogic.ObtainTileMoveCost(this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L20c0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L20db;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x6);
			if (this.processor.FlagSet.IsEqual) goto L20f6;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L20f6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L20f6;

		L20a5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L20ba:
			this.processor.RegAcc.Value16 = 0x72;
			goto L2fd0;

		L20c0:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x69;
			goto L2fd0;

		L20db:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x6d;
			goto L2fd0;

		L20f6:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, 0x18);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x18);
			if (this.processor.FlagSet.IsEqual) goto L213b;
			
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L213b;

			this.owner.CityOps.MayCityWorkTile(this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36), 1);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L2131;
			this.processor.RegAcc.Value16 = 0x1;
			goto L2134;

		L2131:
			this.processor.RegAcc.Value16 = 0x2;

		L2134:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreater) goto L213b;
			goto L20a5;

		L213b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L2153;
			this.processor.RegAcc.Value16 = 0x70;
			goto L2fd0;

		L2153:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L219c;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1514));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Landmasses[this.processor.PullLocalWord(0x1a)].ConstructionSpotTally);
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegData.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L219c;
			goto L12c0;

		L219c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L21a5;
			goto L230b;

		L21a5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L21ba;
			goto L230b;

		L21ba:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L21c4;
			goto L230b;

		L21c4:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L22a6;

		L21cc:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegBase.Value16 = 0x5;
			}
			else
			{
				this.processor.RegBase.Value16 = 0x4;
			}
		
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x44a)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L220a;
			goto L22a3;

		L220a:
			this.owner.TurnLogic.ObtainTileMoveCost(this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L221f;
			goto L22a3;

		L221f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2257;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L2257;

			this.owner.MapOps.ContainsSpecialResource(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L22a3;

		L2257:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));

			this.owner.Helpers.MergeCoordinates(
				(short)(this.processor.PullLocalSignedWord(0x26) -
					this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.X),
				(short)(this.processor.PullLocalSignedWord(0x2e) -
					this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.Y));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L22a3;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			goto L12c0;

		L22a3:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L22a6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L230b;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.RoundTally);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L22e9;
			goto L21cc;

		L22e9:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L22a3;

			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x20), 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2309;
			goto L21cc;

		L2309:
			goto L22a3;

		L230b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L2320;
			goto L241e;

		L2320:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L233c;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L233c;

			goto L241e;

		L233c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			if (this.processor.PullLocalWord(0x34) < 128)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L2351;
			goto L241e;

		L2351:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L2369;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2360;
			goto L20ba;

		L2360:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2369;
			goto L20ba;

		L2369:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x40, 0x1);

		L2379:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + this.processor.PullLocalSignedWord(0x30),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L23cf;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x40);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x10, this.processor.BitOrWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x54);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) * 2 + this.processor.PullLocalSignedWord(0x30),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) * 2 + this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L23cf;
			this.processor.EmitLocalWord(0x3a, 0x1);

		L23cf:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2379;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x10, this.processor.BitAndWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2400;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L23f1;
			goto L20ba;

		L23f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2400;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L2400;
			goto L20ba;

		L2400:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L241e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L241e;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x42));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L241e;
			goto L20ba;

		L241e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2438;
			goto L12c0;

		L2438:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(20));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xd, this.processor.PullLocalWord(0x26));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x2e));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L2499;
			goto L2520;

		L2499:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882));
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4));
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L250b;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x291)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L250b;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1a));
			if (this.processor.FlagSet.IsNotEqual) goto L250b;
			goto L12c0;

		L250b:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L2520:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2558;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L2540;
			goto L12c0;

		L2540:
			this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2558;
			goto L12c0;

		L2558:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2585;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L2585;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L2585;
			goto L14e1;

		L2585:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			goto L14e1;

		L2594:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25f2;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Landmasses[this.processor.PullLocalWord(0x1a)].ConstructionSpotTally;
			this.processor.DivideAccumulatorByPowerOfTwo(0x3);
			this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L25f2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25d6;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L25d6;
			goto L1417;

		L25d6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L25f2;
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L25f2;
			goto L12d7;

		L25f2:
			this.processor.EmitLocalWord(0x28, 0xfc19);
			this.processor.EmitLocalWord(0x2c, 0x0);
			this.processor.EmitLocalWord(0x5c, 0x0);

			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L262d;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), 0xff);
			goto L2632;

		L262d:
			this.processor.EmitLocalWord(0x5c, 0x1);

		L2632:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2643;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2643;
			this.processor.EmitLocalWord(0x56, 0x1);

		L2643:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L267b;

		L264a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2653;
			goto L2857;

		L2653:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L265e;
			goto L2857;

		L265e:
			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2678;
			goto L2857;

		L2678:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L267b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2684;
			goto L2f94;

		L2684:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2678;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x58, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x38, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L26ff;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L26ff;
			goto L2678;

		L26ff:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L277d;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x24));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L277d;
			this.processor.CopyLocalWord(0x24, 0x20);

		L2733:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2771;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2771;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L2733;

		L2771:
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L277d;
			this.processor.CopyLocalWord(0x20, 0x24);

		L277d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d7);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L27cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L27cd;

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L27cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			goto L2678;

		L27cd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2803;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2803;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2803;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2803;
			goto L2678;

		L2803:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L2825;
			goto L264a;

		L2825:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L282e;
			goto L264a;

		L282e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2837;
			goto L2678;

		L2837:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2842;
			goto L2678;

		L2842:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x16);
			if (this.processor.FlagSet.IsNotEqual) goto L284b;
			goto L2678;

		L284b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2854;
			goto L264a;

		L2854:
			goto L2678;

		L2857:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2886;
			this.processor.EmitLocalWord(0x10, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L286b;
			goto L29ed;

		L286b:
			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2883;
			goto L29ed;

		L2883:
			goto L2678;

		L2886:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L28eb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L28eb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L28ce;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L28c6:
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegCount.Value16);
			goto L29bf;

		L28ce:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			goto L28c6;

		L28eb:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(5));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2903;
			goto L29ab;

		L2903:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L290e;
			goto L29ab;

		L290e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2947;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2947:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2975;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2975:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L29bf;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 3);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			goto L29bc;

		L29ab:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L29bc:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L29bf:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L29ed;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L29ed:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc));
			this.processor.EmitLocalByte(0x5e, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2a30;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x40));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2a26;
			this.processor.RegAcc.Value16 = 0x8;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xa));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L2a26:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2a30:
			this.processor.EmitLocalWord(0x1c, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2a3e;
			goto L2d85;

		L2a3e:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2a49;
			goto L2d64;

		L2a49:
			this.processor.EmitLocalWord(0x1c, 0x1);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2a57;
			goto L2678;

		L2a57:
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.RegAcc.Value16].Relations[this.processor.PullLocalWord(0x58)] & 2) != 0)
				goto L2c6a;

			this.owner.UnitOps.LocateBestGuardianInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24));

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x58));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac8;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xe);
			if (this.processor.FlagSet.IsEqual) goto L2ac8;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac8;
			goto L2678;

		L2ac8:
			this.owner.Combat.SettleBattle(participantIdx, troopIdx,
				this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				0);
			
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 0);

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2b3b;
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

		L2b3b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b50;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b50;
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

		L2b50:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b8f;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2b8f;
			this.processor.EmitLocalWord(0x5a, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x5a), 0x1));

		L2b8f:
			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2b9a;
			this.processor.RegAcc.Value16 = 0x6;
			goto L2b9d;

		L2b9a:
			this.processor.RegAcc.Value16 = 0xc;

		L2b9d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5a));
			if (this.processor.FlagSet.IsGreater) goto L2baf;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			goto L2dbd;

		L2baf:
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), 0x3e7));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bbd;
			goto L2dbd;

		L2bbd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bc6;
			goto L2dbd;

		L2bc6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2be8;
			goto L2dbd;

		L2be8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x58));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x28);
			if (this.processor.FlagSet.IsNotEqual) goto L2c02;
			goto L2dbd;

		L2c02:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x38));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f)), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2c14;
			goto L2dbd;

		L2c14:
			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L2c2e;
			goto L2dbd;

		L2c2e:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2c41;
			goto L2dbd;

		L2c41:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c56;
			goto L2dbd;

		L2c56:
			this.owner.Diplomats.BuyUnit(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				participantIdx);
			
			goto L2dbd;

		L2c6a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c73;
			goto L2678;

		L2c73:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c7c;
			goto L2678;

		L2c7c:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2c9e;
			goto L2678;

		L2c9e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndex(0x600, this.processor.PullLocalWord(0x58));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L2cb8;
			goto L2678;

		L2cb8:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 3) != 1)
				goto L2678;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x58)].Relations[this.owner.World.PersonParticipantIdx] & 3) == 1)
				goto L2678;

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally == 0)
				goto L2678;

			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L2d15;
			goto L2678;

		L2d15:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(8));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2d3b;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L2678;

		L2d3b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d50;
			goto L2678;

		L2d50:
			this.owner.Diplomats.BuyUnit(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				participantIdx);
			
			goto L2678;

		L2d64:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			goto L2dbd;

		L2d85:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2da4;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2da4;
			this.processor.EmitLocalWord(0x10, 0x3e7);

		L2da4:
			this.owner.MapOps.ContainsHut(
				this.processor.PullLocalWord(0x38),
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2dbd;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x14));

		L2dbd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x5c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2dc6;
			goto L2f15;

		L2dc6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegCount.Value16 = 0xd;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2e47;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2e47;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2e47;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

		L2e47:
			this.processor.EmitLocalWord(0x44, 0x1);

		L2e4c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2e));
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2e81;
			goto L2f09;

		L2e81:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x16)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			if (this.processor.FlagSet.IsNotEqual) goto L2ed1;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2ecd;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2ed1;

		L2ecd:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x2));

		L2ed1:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2ee6;
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), 0x2));

		L2ee6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f09;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x290));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2f09:
			this.processor.EmitLocalWord(0x44, this.processor.IncrementWord(this.processor.PullLocalWord(0x44)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L2f15;
			goto L2e4c;

		L2f15:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2f74;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2f74;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3);

			this.owner.Helpers.VerifyValueSpan((short)this.processor.RegAcc.Value16, 1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 1);

			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x10));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L2f74:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L2f7f;
			goto L2678;

		L2f7f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x40, 0x2c);
			this.processor.CopyLocalWord(0x1c, 0x52);
			goto L2678;

		L2f94:
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2fb6;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2fb6;
			this.processor.EmitLocalWord(0x2c, 0x0);

		L2fb6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);

		L2fcd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2c);

		L2fd0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftAIUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Sets up the strategic locations of the player again: its 32 entries at 0xe2c2 + 32 * player become empty (0xff, with
		/// 0 at 0xb2be + ...), then a unit is sent to each of its 16 locations that is in use with a state up to 2
		/// </summary>
		public void ReimposeStrategicLocations(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ReimposeStrategicLocations({participantIdx})");

			for (int entryIdx = 0; entryIdx < 0x20; entryIdx++)
			{
				ushort entry = (ushort)((participantIdx << 5) + entryIdx);

				this.processor.EmitGlobalByte((ushort)(entry + 0xe2c2), 0xff);
				this.processor.EmitGlobalByte((ushort)(entry + 0xb2be), 0x0);
			}

			for (int siteIdx = 0; siteIdx < 0x10; siteIdx++)
			{
				StrategicLocation site = this.owner.World.Participants[participantIdx].TacticalSites[siteIdx];

				if (site.Live != -1 && site.Live <= 2)
				{
					SendUnitToStrategicLocation(participantIdx, (short)site.Place.X, (short)site.Place.Y, site.Live, site.Strategy);
				}
			}

			this.processor.Journal.CloseScope("ReimposeStrategicLocations");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="active"></param>
		/// <param name="policy"></param>
		public void SendUnitToStrategicLocation(short participantIdx, short horizLoc, short vertLoc, short live, short strategy)
		{
			this.processor.Journal.OpenScope($"SendUnitToStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");
			
			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L305f;

		L305c:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L305f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x20);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L309a;
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x4));

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe598)) != horizLoc)
				goto L305c;

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe798)) != vertLoc)
				goto L305c;

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe2c2)) != live)
				goto L305c;

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb2be)) > strategy)
				goto L305c;

			goto L31ce;

		L309a:
			if (participantIdx == this.processor.PullGlobalSignedWord(0x6b90))
				goto L30a5;

			goto L3170;

		L30a5:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L30b0;

			goto L3170;

		L30b0:
			if (live == 3 || live == 4)
				goto L30bf;

			goto L3170;

		L30bf:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L30c9;

		L30c6:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L30c9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x80);
			if (this.processor.FlagSet.IsLess) goto L30d3;
			goto L3170;

		L30d3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L30c6;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L30c6;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)live;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L30c6;

			this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L30c6;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				horizLoc,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				vertLoc);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3158;
			goto L30c6;

		L3158:
			this.processor.RegAcc.LowByte = (byte)((sbyte)horizLoc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)vertLoc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x10));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xf0));

		L3170:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L317a;

		L3177:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L317a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x20);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L31ce;
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe2c2)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L319b;

			if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb2be)) >= strategy) goto L3177;

		L319b:
			FreeStrategicLocation(participantIdx, this.processor.PullLocalWord(0x4));

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.LowByte = (byte)((sbyte)horizLoc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe598), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)vertLoc);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe798), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)live);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe2c2), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)strategy);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb2be), this.processor.RegAcc.LowByte);

		L31ce:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SendUnitToStrategicLocation");
		}

		/// <summary>
		/// Makes room at the place of the list of the player (32 entries a player in the byte arrays at 0xb2be, 0xe2c2, 0xe598
		/// and 0xe798): the used entries from the place up to 30 move one place on
		/// </summary>
		public void FreeStrategicLocation(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"FreeStrategicLocation({participantIdx}, {arg2})");

			for (int idx = 30; idx >= (short)arg2; idx--)
			{
				int entry = (participantIdx << 5) + idx;

				if (this.processor.PullGlobalByte((ushort)(0xb2be + entry)) == 0)
					continue;

				MoveListEntryOn(0xe598, entry);
				MoveListEntryOn(0xe798, entry);
				MoveListEntryOn(0xe2c2, entry);
				MoveListEntryOn(0xb2be, entry);
			}

			this.processor.Journal.CloseScope("FreeStrategicLocation");
		}

		private void MoveListEntryOn(ushort origin, int entry)
		{
			this.processor.EmitGlobalByte((ushort)(origin + entry + 1), this.processor.PullGlobalByte((ushort)(origin + entry)));
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void WipeStrategicLocations(short participantIdx)
		{
			for (int idx = 0; idx < 16; idx++)
			{
				this.owner.World.Participants[participantIdx].TacticalSites[idx].Live = -1;
				this.owner.World.Participants[participantIdx].TacticalSites[idx].Strategy = 0;
			}
		}

		/// <summary>
		/// Notes a strategic location of the player (the 16 places are sorted by strategy, highest first): nothing when it is
		/// already there with the same state and at least the strategy; otherwise it goes in before the first free place or
		/// the first place with a lower strategy (AffixStrategicLocation moves the rest down); no room, no change
		/// </summary>
		public void AttachStrategicLocation(short participantIdx, ushort horizLoc, ushort vertLoc, ushort live, ushort strategy)
		{
			this.processor.Journal.OpenScope($"AttachStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			bool known = sites.Take(0x10).Any(site => (ushort)(sbyte)site.Place.X == horizLoc && (ushort)(sbyte)site.Place.Y == vertLoc &&
				(ushort)site.Live == live && (sbyte)site.Strategy <= (short)strategy);

			if (!known)
			{
				for (ushort place = 0; place < 0x10; place++)
				{
					if (sites[place].Live == -1 || (sbyte)sites[place].Strategy < (short)strategy)
					{
						AffixStrategicLocation(participantIdx, place);

						sites[place].Place.X = (sbyte)horizLoc;
						sites[place].Place.Y = (sbyte)vertLoc;
						sites[place].Live = (sbyte)live;
						sites[place].Strategy = (byte)strategy;
						break;
					}
				}
			}

			this.processor.Journal.CloseScope("AttachStrategicLocation");
		}

		/// <summary>
		/// Makes room at the place of the strategic locations of the player: the used ones from the place up to 14 move one place on
		/// </summary>
		public void AffixStrategicLocation(short participantIdx, ushort place)
		{
			this.processor.Journal.OpenScope($"AffixStrategicLocation({participantIdx}, {place})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 14; idx >= (short)place; idx--)
			{
				if (sites[idx].Strategy == 0)
					continue;

				sites[idx + 1].Place.X = sites[idx].Place.X;
				sites[idx + 1].Place.Y = sites[idx].Place.Y;
				sites[idx + 1].Live = sites[idx].Live;
				sites[idx + 1].Strategy = sites[idx].Strategy;
			}

			this.processor.Journal.CloseScope("AffixStrategicLocation");
		}

		/// <summary>
		/// The strategic locations of the player of the kind (Live = arg2) not farther than arg5 from the position are given up
		/// (Live = -1)
		/// </summary>
		public void DisableStrategicLocation(short participantIdx, ushort arg2, int horizLoc, int vertLoc, ushort arg5)
		{
			this.processor.Journal.OpenScope($"DisableStrategicLocation({participantIdx}, {arg2}, {horizLoc}, {vertLoc}, {arg5})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 0; idx < 16; idx++)
			{
				if ((ushort)sites[idx].Live != arg2)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, sites[idx].Place.X, vertLoc, sites[idx].Place.Y);

				if (distance <= (short)arg5)
				{
					sites[idx].Live = -1;
				}
			}

			this.processor.Journal.CloseScope("DisableStrategicLocation");
		}

		/// <summary>
		/// The cities of the player choose their production again (PickNewCityProduction): all of them, or with arg2 not 0xffff
		/// only the cities on that continent
		/// </summary>
		public void RefreshCityProduction(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"RefreshCityProduction({participantIdx}, {arg2})");

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if ((byte)town.ParticipantIdx != (ushort)participantIdx || town.ConditionMarker == 0xff)
					continue;

				if (arg2 != 0xffff && this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) != arg2)
					continue;

				PickNewCityProduction(townIdx);
			}

			this.processor.Journal.CloseScope("RefreshCityProduction");
		}

		/// <summary>
		/// The city chooses its production again (ScienceAndAdvisors.PickCityProduction); the count of the units of each type
		/// its player is making (TroopsInsideMaking) follows the change
		/// </summary>
		public void PickNewCityProduction(short townIdx)
		{
			this.processor.Journal.OpenScope($"PickNewCityProduction({townIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			City town = this.owner.World.Towns[townIdx];
			ushort participantIdx = (byte)town.ParticipantIdx;

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]--;
			}

			this.owner.Advisors.PickCityProduction((short)participantIdx, townIdx);

			town.PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]++;
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("PickNewCityProduction");
		}

		/// <summary>
		/// The direction (0 - 8, 0 is the tile itself) of the ocean tile at or around (horizLoc, vertLoc), without units
		/// unless it is the tile itself, with the most empty land tiles around it (with relations bit 2 towards the human
		/// player, land of the human player, map layer 2 at column + 80, does not count); -1 without such an ocean tile
		/// </summary>
		public ushort MayAIEnterTile(short participantIdx, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"MayAIEnterTile({participantIdx}, {horizLoc}, {vertLoc})");

			short bestHeading = -1;
			short bestShore = -1;
			MapLayers chart = this.owner.MapOps.Chart;

			for (short heading = 0; heading <= 8; heading++)
			{
				short oceanHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
				short oceanVert = (short)(chart.DirectionVert(heading) + vertLoc);

				if (this.owner.MapOps.ObtainTerrainType(oceanHoriz, oceanVert) != MapRules.OceanTerrain)
					continue;

				if (heading != 0 && this.owner.MapOps.ObtainTileOwner(oceanHoriz, oceanVert) != 0xffff)
					continue;

				short shore = 0;

				for (int around = 1; around <= 8; around++)
				{
					short landHoriz = (short)(chart.DirectionHoriz(around) + oceanHoriz);
					short landVert = (short)(chart.DirectionVert(around) + oceanVert);

					if (this.owner.MapOps.ObtainTerrainType(landHoriz, landVert) == MapRules.OceanTerrain ||
						this.owner.MapOps.ObtainTileOwner(landHoriz, landVert) != 0xffff)
						continue;

					shore++;

					if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0 &&
						this.owner.VideoCard.ObtainDot(2, landHoriz + 0x50, landVert) == (ushort)this.owner.World.PersonParticipantIdx)
					{
						shore--;
					}
				}

				if (shore > bestShore)
				{
					bestShore = shore;
					bestHeading = heading;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)bestHeading;
			this.processor.Journal.CloseScope("MayAIEnterTile");

			return (ushort)bestHeading;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort ManageBarbarianUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ManageBarbarianUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x54);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalByte(0x54, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x2);
			if (this.processor.FlagSet.IsBelow) goto L3654;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x30);
			if (this.processor.FlagSet.IsBelow) goto L3665;

		L3654:
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			
			goto L3e96;

		L3665:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x26, 0x0);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x2a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x30, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L36be;
			goto L395c;

		L36be:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L36d9;

		L36c5:
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

		L36d3:
			this.processor.RegAcc.Value16 = 0x20;
			goto L3e96;

		L36d9:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x38, 0x1);
			goto L370c;

		L36f1:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L3709;

		L3703:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x38);
			goto L3e96;

		L3709:
			this.processor.EmitLocalWord(0x38, this.processor.IncrementWord(this.processor.PullLocalWord(0x38)));

		L370c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L3715;
			goto L38a2;

		L3715:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x38);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x2a));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L36f1;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L36f1;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L36f1;

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3775;
			goto L36f1;

		L3775:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L377e;
			goto L36f1;

		L377e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x30);
			if (this.processor.FlagSet.IsLess) goto L3787;
			goto L36f1;

		L3787:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x20),
				this.processor.PullLocalWord(0x28)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L37ac;
			goto L386f;

		L37ac:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L386f;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L37ed;
			goto L386f;

		L37ed:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.EmitGlobalWord(0x2f9e, 0x3);

			this.owner.Libc.CopyText(0xba06, "Barbarian raiding party\nlands near ");

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			this.owner.Conquest.ObtainCityLabel((short)this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "!\nCitizens are alarmed.\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 32);

		L386f:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x20),
				this.processor.PullLocalWord(0x28)];
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.Value16 = 0x75;
			goto L3e96;

		L38a2:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L38bc;
			goto L3957;

		L38bc:
			this.processor.EmitLocalWord(0x22, 0x0);
			this.processor.EmitLocalWord(0xc, 0x0);

		L38c6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L391c;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x2a),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegDest.Value16 = this.processor.RegCount.Value16;

			this.owner.Conquest.ComputeCityPlunder(this.processor.PullLocalSignedWord(0xc));
			
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x32);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegDest.Value16);
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L391c;
			this.processor.CopyLocalWord(0x34, 0x22);
			this.processor.CopyLocalWord(0xc, 0x2e);

		L391c:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L38c6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L392f;
			goto L36c5;

		L392f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2e));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.Y);

		L3953:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L3957:
			this.processor.RegAcc.Value16 = 0;
			goto L3e96;

		L395c:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L39a0;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L399a;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L39a0;

		L399a:
			this.processor.RegAcc.Value16 = 0x66;
			goto L3e96;

		L39a0:
			this.processor.RegAcc.Value16 = 0xfffe;
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L39f1;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2e));

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L39e7;
			goto L36c5;

		L39e7:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L39f1;
			goto L36c5;

		L39f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L3a60;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2e));

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3a60;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].ParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L3a60;
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.X);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2a));
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);

		L3a60:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2e));
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].ParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3a86;

			if (this.owner.World.Participants[this.processor.RegSource.Value16].TownTally >= 2 ||
				this.owner.World.Participants[this.processor.RegSource.Value16].Gold >= 100) goto L3a8f;

		L3a86:
			this.processor.RegAcc.Value16 = 0xfffe;
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);

		L3a8f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0xfffe);
			if (this.processor.FlagSet.IsEqual) goto L3acf;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x2e));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x2e)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			
			this.owner.Routing.CarryOutGoto(participantIdx, troopIdx);
			
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);

		L3acf:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), 0xff);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L3af0;
			goto L3b9b;

		L3af0:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L3b25;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L3b25;
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L3b25;
			goto L36d3;

		L3b25:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L3bac;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			this.owner.Helpers.LocateNearestUnit(participantIdx, troopIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L3b8a;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x1c));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L3b8a;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d5));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d6));
			goto L3953;

		L3b8a:
			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x34));
			if (this.processor.FlagSet.IsGreater) goto L3b9b;
			goto L36c5;

		L3b9b:
			this.processor.EmitLocalWord(0x26, 0x0);
			this.processor.EmitLocalWord(0x22, 0xfc19);
			this.processor.EmitLocalWord(0x38, 0x1);
			goto L3bda;

		L3bac:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3b9b;
			goto L36c5;

		L3bb5:
			this.processor.RegAcc.Value16 = 0;

		L3bb7:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x54, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L3bd0;
			this.processor.RegAcc.Value16 = 0x1;
			goto L3bd2;

		L3bd0:
			this.processor.RegAcc.Value16 = 0;

		L3bd2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x54));
			if (this.processor.FlagSet.IsEqual) goto L3c34;

		L3bd7:
			this.processor.EmitLocalWord(0x38, this.processor.IncrementWord(this.processor.PullLocalWord(0x38)));

		L3bda:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0x9);
			if (this.processor.FlagSet.IsLess) goto L3be3;
			goto L3e4a;

		L3be3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x38);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) +
					this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3bb5;
			this.processor.RegAcc.Value16 = 0x1;
			goto L3bb7;

		L3c34:
			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L3bd7;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L3cc7;
			this.processor.EmitLocalWord(0x10, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L3c8c;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3c86;
			this.processor.RegAcc.Value16 = 0x63;
			goto L3c89;

		L3c86:
			this.processor.RegAcc.Value16 = 0xff9d;

		L3c89:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L3c8c:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			goto L3e30;

		L3cc7:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3d05;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L3d05;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3d02;
			goto L3bd7;

		L3d02:
			goto L3703;

		L3d05:
			this.processor.RegSource.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(6));

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L3d2a;
			this.processor.RegAcc.Value16 = 0x6;
			goto L3d2c;

		L3d2a:
			this.processor.RegAcc.Value16 = 0;

		L3d2c:
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L3d45;
			this.processor.RegAcc.Value16 = 0;
			goto L3d48;

		L3d45:
			this.processor.RegAcc.Value16 = 0x4;

		L3d48:
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L3dab;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3d79;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L3d73;
			goto L3bd7;

		L3d73:
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), 0x14));
			goto L3dab;

		L3d79:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3d8f;
			goto L3bd7;

		L3d8f:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x1e));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L3dab;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x63));

		L3dab:
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3dee;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3dee;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L3dee;

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x28));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L3dee;
			goto L3bd7;

		L3dee:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x38));
			if (this.processor.FlagSet.IsNotEqual) goto L3e0c;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x6));

		L3e0c:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x38);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3e1e;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x2));

		L3e1e:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x38);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3e30;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x2));

		L3e30:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L3e3b;
			goto L3bd7;

		L3e3b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x38, 0x26);
			goto L3bd7;

		L3e4a:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2a),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3e7c;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L3e7c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x63);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3e7c;
			goto L36d3;

		L3e7c:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);

		L3e96:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ManageBarbarianUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Sends to the city the loaded transports at sea of the players 1 - 7 (not the human player, and only those without
		/// relations bit 2 towards the human player) that are closer to it than 3 turns of moves
		/// </summary>
		public void PostCityDefenders(short townIdx)
		{
			this.processor.Journal.OpenScope($"PostCityDefenders({townIdx})");

			short townHoriz = (short)this.owner.World.Towns[townIdx].Place.X;
			short townVert = (short)this.owner.World.Towns[townIdx].Place.Y;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx ||
					(this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
					continue;

				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx) ||
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ||
						this.troops.Following(participantIdx, troopIdx) == -1)
						continue;

					sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
					sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain)
						continue;

					int distance = this.owner.Helpers.ObtainDistance(horizLoc, townHoriz, vertLoc, townVert);

					if ((short)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)) > distance)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)townHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)townVert);
					}
				}
			}

			this.processor.Journal.CloseScope("PostCityDefenders");
		}
	}
}
