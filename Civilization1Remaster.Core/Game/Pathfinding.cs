using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2e31.
	/// </summary>
	public class Pathfinding
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public Pathfinding(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort CarryOutGoto(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"CarryOutGoto({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x2a);
			this.processor.PreserveIndexRegisters();

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L00f9;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			if (troopIdx < 128)
			{
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
				this.processor.RegAcc.HighByte = 0;
				this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
				this.processor.RegCount.HighByte = 0;
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
			}
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);

			if (troopIdx < 128)
			{
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
				this.processor.RegAcc.HighByte = 0;
				this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
				this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
			}
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x1e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0064;
			goto L00f9;

		L0064:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x20));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0077;
			goto L00f9;

		L0077:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x1e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x28);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0094;
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.PullLocalWord(0x1e));
			goto L00a1;

		L0094:
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.PullLocalWord(0x1e));
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);

		L00a1:
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)Math.Sign((short)this.processor.PullLocalWord(0x20));
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

			if (troopIdx < 128)
			{
				this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);

				this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			}

			this.processor.EmitLocalWord(0x18, 0x1);
			goto L00d1;

		L00ce:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));

		L00d1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L00f4;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L00ce;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L00ce;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			goto L05e0;

		L00f4:
			this.processor.RegAcc.Value16 = 0;
			goto L05e0;

		L00f9:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L012b;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);
			goto L0208;

		L012b:
			this.processor.EmitLocalWord(0xe, 0x0);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsLess) goto L0178;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x49);
			if (this.processor.FlagSet.IsLessOrEqual) goto L01d8;

		L0178:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01d8;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);

			ShiftToward(participantIdx, troopIdx, 0x3e7);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L01d3;

		L01cd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			goto L05e0;

		L01d3:
			this.processor.EmitLocalWord(0xe, 0x1);

		L01d8:
			ChartRoute(participantIdx, troopIdx);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L01ef;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0208;

		L01ef:
			ShiftToward(participantIdx, troopIdx, 0x3e7);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L01cd;

		L0208:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x1e));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x20));

			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x1e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x28));
			if (this.processor.FlagSet.IsLessOrEqual) goto L026e;
			
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x1e));
			goto L0271;

		L026e:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x20));

		L0271:
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x26));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L02a2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L02a2;
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.Value16 = 0xffff;
			goto L05e0;

		L02a2:
			this.processor.EmitLocalWord(0x24, 0x270f);
			this.processor.EmitLocalWord(0x14, 0x0);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x18, 0x1);
			goto L0324;

		L02f7:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x4));

		L02fa:
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x26));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x28));
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L03b1;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0321;
			goto L03b1;

		L0321:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));

		L0324:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x9);
			if (this.processor.FlagSet.IsLess) goto L032d;
			goto L0571;

		L032d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);

			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x2));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x4));

			this.processor.EmitLocalWord(0x2a, this.processor.RegAcc.Value16);

			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x2));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2a));
			if (this.processor.FlagSet.IsGreater) goto L03ab;
			goto L02f7;

		L03ab:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x2));
			goto L02fa;

		L03b1:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L03e0;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0455;

		L03e0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L03eb;
			this.processor.RegAcc.Value16 = 0x1;
			goto L03ed;

		L03eb:
			this.processor.RegAcc.Value16 = 0;

		L03ed:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0415;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0417;

		L0415:
			this.processor.RegAcc.Value16 = 0;

		L0417:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0436;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0480;

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0480;

		L0436:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0480;

		L0455:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L046a;
			goto L0321;

		L046a:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L0480;
			goto L0321;

		L0480:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L049c;

			this.owner.MapOps.ObtainLandMassSize(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L049c;
			goto L0321;

		L049c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04b4;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L04eb;

		L04b4:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L04e3;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x1a));
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));
			goto L04e6;

		L04e3:
			this.processor.RegAcc.Value16 = 0x3;

		L04e6:
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			goto L04f0;

		L04eb:
			this.processor.EmitLocalWord(0x8, 0x1);

		L04f0:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x4));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0x2));

			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.SumWord(this.processor.PullLocalWord(0x8), this.processor.RegCount.Value16));
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc));
			this.processor.EmitLocalByte(0x2a, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0557;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x18));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L054f;
			this.processor.RegAcc.Value16 = 0x8;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L054f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.SumWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16));

		L0557:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0562;
			goto L0321;

		L0562:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x8, 0x24);
			goto L0321;

		L0571:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L059e;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.LowByte = this.processor.BitXorByte(this.processor.RegAcc.LowByte, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			if (this.processor.FlagSet.IsNotEqual) goto L059e;
			this.processor.RegAcc.LowByte = 0;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

		L059e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L05c6;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.EmitLocalWord(0x14, 0xffff);
			goto L01cd;

		L05c6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x14);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			goto L01cd;

		L05e0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("CarryOutGoto");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The next step of a unit's long way (0x6590 / 0x6592): from the block of 4 x 4 tiles of its goal (LocateRouteStep), a
		/// search in width over the linked blocks (land 0xdb44 or, for a ship, sea 0x7f38; steps in 0xd816, queue 0x6594 /
		/// 0x6694, columns wrap at 20) until the unit's block. Found: the neighbour of the unit's block with the fewest steps
		/// (the nearest to the goal among equal ones), its tile (1, 1) or the next one of the right kind (land or sea), else
		/// the goal itself. Returns 1 when found, 0 when not, and the goal row when the unit's block has no step (like the
		/// original's AX). The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		public ushort ChartRoute(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ChartRoute({participantIdx}, {troopIdx})");

			this.processor.EnterFrame(0x3a);
			this.processor.PreserveIndexRegisters();

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			ushort unitHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81d5));
			ushort unitVert = this.processor.PullGlobalByte((ushort)(troop + 0x81d6));

			this.processor.EmitLocalWord(0x36, unitHoriz);
			this.processor.EmitLocalWord(0x3a, unitVert);
			this.processor.EmitGlobalWord(0x6590, this.processor.PullGlobalByte((ushort)(troop + 0x81da)));
			this.processor.EmitGlobalWord(0x6592, this.processor.PullGlobalByte((ushort)(troop + 0x81db)));

			ushort sea = UnitMovesBySea(troop) ? (ushort)1 : (ushort)0;

			LocateRouteStep((short)unitHoriz, (short)unitVert, sea);

			ushort result;

			if (this.processor.RegAcc.Value16 == 0)
			{
				this.processor.EmitGlobalWord(0x6590, this.processor.PullGlobalByte((ushort)(troop + 0x81da)));
				this.processor.EmitGlobalWord(0x6592, this.processor.PullGlobalByte((ushort)(troop + 0x81db)));

				// The goal row is still in AX in the original
				result = this.processor.PullGlobalByte((ushort)(troop + 0x81db));
			}
			else
			{
				short unitColumn = (short)this.processor.PullGlobalWord(0x6590), unitRow = (short)this.processor.PullGlobalWord(0x6592);

				this.processor.EmitLocalWord(0x36, (ushort)unitColumn);
				this.processor.EmitLocalWord(0x3a, (ushort)unitRow);

				LocateRouteStep(this.processor.PullGlobalByte((ushort)(troop + 0x81da)), this.processor.PullGlobalByte((ushort)(troop + 0x81db)),
					UnitMovesBySea(troop) ? (ushort)1 : (ushort)0);

				result = SearchRouteBlocks(troop, unitColumn, unitRow) ? (ushort)1 : (ushort)0;
			}

			this.processor.RegAcc.Value16 = result;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChartRoute");

			return result;
		}

		/// <summary>Whether the unit (offset from 0x81d4) is of a sea kind (domain 2, 0x1138 + 0x22 * kind)</summary>
		private bool UnitMovesBySea(ushort troop)
		{
			return this.processor.PullGlobalWord((ushort)((ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7))) + 0x1138)) == 2;
		}

		/// <summary>The links of the block (the bits of directions 1 - 8): sea blocks 0x7f38 or land blocks 0xdb44</summary>
		private short RouteBlockLinks(bool sea, short column, short row)
		{
			return (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * column) + row + (sea ? 0x7f38 : 0xdb44)));
		}

		/// <summary>The column of the block in the direction, wrapping at 20</summary>
		private short RouteNeighbourColumn(short direction, short column)
		{
			short nextColumn = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + column);

			if (nextColumn == 0x14)
				nextColumn = 0;

			if (nextColumn == -1)
				nextColumn = 0x13;

			return nextColumn;
		}

		/// <summary>
		/// The search in width from the goal's block (0x6590 / 0x6592) to the unit's block and the step there (see ChartRoute);
		/// true when the unit's block was reached
		/// </summary>
		private bool SearchRouteBlocks(ushort troop, short unitColumn, short unitRow)
		{
			this.owner.Libc.FillBytes(0xd816, 0, 0x104);

			byte tail = 0, head = 0;

			this.processor.EmitGlobalByte((ushort)(tail + 0x6594), this.processor.PullGlobalByte(0x6590));
			this.processor.EmitGlobalByte((ushort)(tail + 0x6694), this.processor.PullGlobalByte(0x6592));
			tail++;
			this.processor.EmitGlobalByte((ushort)((ushort)(0xd * this.processor.PullGlobalWord(0x6590)) + this.processor.PullGlobalWord(0x6592) + 0xd816), 1);

			bool sea = UnitMovesBySea(troop);
			bool found = false;
			// The last values of the locals 0xa (steps, then the distance), 0x10, 0x2e, 0x30, 0x34 (unset: never written)
			bool stepsSet = false, linksSet = false, neighbourSet = false;
			short steps = 0, links = 0, nextColumn = 0, nextRow = 0;
			short column, row;

			this.processor.EmitLocalWord(0x16, sea ? (ushort)1 : (ushort)0);

			do
			{
				column = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6594));
				row = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6694));

				if (column == unitColumn && row == unitRow)
				{
					found = true;
					break;
				}

				steps = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * column) + row + 0xd816));
				stepsSet = true;
				head++;
				links = RouteBlockLinks(sea, column, row);
				linksSet = true;

				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
						continue;

					nextColumn = RouteNeighbourColumn(direction, column);
					nextRow = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + row);
					neighbourSet = true;

					ushort block = (ushort)((ushort)(0xd * nextColumn) + nextRow + 0xd816);

					if (this.processor.PullGlobalByte(block) != 0)
						continue;

					this.processor.EmitGlobalByte(block, (byte)(steps + 1));
					this.processor.EmitGlobalByte((ushort)(tail + 0x6594), (byte)nextColumn);
					this.processor.EmitGlobalByte((ushort)(tail + 0x6694), (byte)nextRow);
					tail++;
				}
			}
			while (head != tail);

			this.processor.EmitLocalWord(0x2, tail);
			this.processor.EmitLocalWord(0x4, head);
			this.processor.EmitLocalWord(0xc, found ? (ushort)1 : (ushort)0);
			this.processor.EmitLocalWord(0x14, (ushort)column);
			this.processor.EmitLocalWord(0x2c, (ushort)row);

			this.processor.EmitGlobalWord(0x6590, 0xffff);

			if (found)
			{
				short fewest = 0x63;
				short bestDirection = -1;

				links = RouteBlockLinks(UnitMovesBySea(troop), unitColumn, unitRow);
				linksSet = true;

				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
						continue;

					nextColumn = RouteNeighbourColumn(direction, unitColumn);
					nextRow = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + unitRow);
					neighbourSet = true;

					short blockSteps = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * nextColumn) + nextRow + 0xd816));
					this.processor.EmitLocalWord(0x6, (ushort)blockSteps);

					if (blockSteps == 0 || blockSteps > fewest)
						continue;

					this.owner.Helpers.ObtainDistance((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81da)), (nextColumn * 4) + 1,
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81db)), (nextRow * 4) + 1);

					short distance = (short)this.processor.RegAcc.Value16;

					if (blockSteps < fewest)
					{
						fewest = blockSteps;
						bestDirection = direction;
					}
					else
					{
						this.processor.EmitLocalWord(0x12, (ushort)distance);

						// Compared with 0xa: the last distance, or the steps of the search (unwritten: what is in the frame)
						if (distance >= (stepsSet ? steps : (short)this.processor.PullLocalWord(0xa)))
							continue;

						bestDirection = direction;
					}

					steps = distance;
					stepsSet = true;
				}

				this.processor.EmitLocalWord(0x8, (ushort)fewest);
				this.processor.EmitLocalWord(0xe, (ushort)bestDirection);
				this.processor.EmitLocalWord(0x2e, 9);

				if (bestDirection != -1)
					AimRouteStep(bestDirection, unitColumn, unitRow, sea);
			}
			else if (linksSet)
			{
				this.processor.EmitLocalWord(0x2e, 9);
			}

			if (stepsSet)
				this.processor.EmitLocalWord(0xa, (ushort)steps);

			if (linksSet)
				this.processor.EmitLocalWord(0x10, (ushort)links);

			if (neighbourSet)
			{
				this.processor.EmitLocalWord(0x30, (ushort)nextColumn);
				this.processor.EmitLocalWord(0x34, (ushort)nextRow);
			}

			if (this.processor.PullGlobalWord(0x6590) == 0xffff)
			{
				this.processor.EmitGlobalWord(0x6590, this.processor.PullGlobalByte((ushort)(troop + 0x81da)));
				this.processor.EmitGlobalWord(0x6592, this.processor.PullGlobalByte((ushort)(troop + 0x81db)));
			}

			return found;
		}

		/// <summary>
		/// The step into the neighbouring block: its tile (1, 1), else (2, 1), else (2, 2) when the tile isn't of the unit's kind
		/// (land or sea)
		/// </summary>
		private void AimRouteStep(short direction, short unitColumn, short unitRow, bool sea)
		{
			this.processor.EmitGlobalWord(0x6590, (ushort)((((short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + unitColumn) << 2) + 1));

			TunePosition((short)this.processor.PullGlobalWord(0x6590));

			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6592, (ushort)((((short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + unitRow) << 2) + 1));

			if ((this.owner.MapOps.ObtainTerrainType((short)this.processor.PullGlobalWord(0x6590), (short)this.processor.PullGlobalWord(0x6592)) == 0xa) == sea)
				return;

			this.processor.EmitGlobalWord(0x6590, (ushort)(this.processor.PullGlobalWord(0x6590) + 1));

			if ((this.owner.MapOps.ObtainTerrainType((short)this.processor.PullGlobalWord(0x6590), (short)this.processor.PullGlobalWord(0x6592)) == 0xa) == sea)
				return;

			this.processor.EmitGlobalWord(0x6592, (ushort)(this.processor.PullGlobalWord(0x6592) + 1));
		}

		/// <summary>
		/// The next block of 4 x 4 tiles on the way to the place (land blocks 0xdb44, marker 0, or sea blocks 0x7f38, marker 1;
		/// 13 bytes a column): the block of the place itself when it is marked, else the marked neighbouring block (directions
		/// 1 - 8) nearest to the place that has a corner tile of the right kind (ocean for the marker 1) with a route
		/// (ComputeRouteCost) to the place. The block goes to 0x6590 / 0x6592.
		/// </summary>
		/// <returns>1 when a block was found, else 0, also in AX</returns>
		public ushort LocateRouteStep(int horizLoc, int vertLoc, ushort marker)
		{
			this.processor.Journal.OpenScope($"LocateRouteStep({horizLoc}, {vertLoc}, {marker})");

			short blockHoriz = DivideByFour((short)horizLoc);
			short blockVert = DivideByFour((short)vertLoc);
			ushort blocks = marker != 0 ? (ushort)0x7f38 : (ushort)0xdb44;
			int found = -1;

			if (BlockMarked(blocks, blockHoriz, blockVert))
			{
				found = 0;
			}
			else
			{
				short nearest = 0x63;

				for (int directionIdx = 1; directionIdx <= 8; directionIdx++)
				{
					short nextHoriz = (short)(this.processor.PullGlobalWord((ushort)((directionIdx << 1) + 0x1882)) + blockHoriz);
					short nextVert = (short)(this.processor.PullGlobalWord((ushort)((directionIdx << 1) + 0x18e4)) + blockVert);

					if (!BlockMarked(blocks, nextHoriz, nextVert))
						continue;

					short distance = (short)this.owner.Helpers.MergeCoordinates(horizLoc - (nextHoriz * 4) - 1, vertLoc - (nextVert * 4) - 1);

					if (distance >= nearest)
						continue;

					// The corners of the block: (1, 1), (2, 1), (2, 2), (1, 2)
					short cornerHoriz = (short)((nextHoriz << 2) + 1);
					short cornerVert = (short)((nextVert << 2) + 1);

					if (!CornerMatches(cornerHoriz, cornerVert, marker) && !CornerMatches(++cornerHoriz, cornerVert, marker) &&
						!CornerMatches(cornerHoriz, ++cornerVert, marker) && !CornerMatches(--cornerHoriz, cornerVert, marker))
						continue;

					ComputeRouteCost(cornerHoriz, cornerVert, horizLoc, vertLoc, marker, 18);

					if (this.processor.RegAcc.Value16 != 0xffff)
					{
						nearest = distance;
						found = directionIdx;
					}
				}
			}

			ushort result = 0;

			if (found != -1)
			{
				this.processor.EmitGlobalWord(0x6590, (ushort)(this.processor.PullGlobalWord((ushort)((found << 1) + 0x1882)) + blockHoriz));
				this.processor.EmitGlobalWord(0x6592, (ushort)(this.processor.PullGlobalWord((ushort)((found << 1) + 0x18e4)) + blockVert));
				result = 1;
			}

			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("LocateRouteStep");

			return result;
		}

		/// <summary>The block is marked in the table of blocks (13 bytes a column)</summary>
		private bool BlockMarked(ushort blocks, short blockHoriz, short blockVert)
		{
			return this.processor.PullGlobalByte((ushort)(blockVert + (0xd * blockHoriz) + blocks)) != 0;
		}

		/// <summary>The tile is ocean for the marker 1, land for the marker 0</summary>
		private bool CornerMatches(short horizLoc, short vertLoc, ushort marker)
		{
			return (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa ? 1 : 0) == marker;
		}

		/// <summary>Signed division by 4 toward zero, like the original (-32768 gives a positive quotient)</summary>
		private static short DivideByFour(short amount)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> 2);

			return amount < 0 ? (short)-quotient : quotient;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort ShiftToward(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ShiftToward({participantIdx}, {troopIdx}, {arg3})");

			// function body
			this.processor.EnterFrame(0x4a);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)((short)participantIdx));
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)((short)troopIdx));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6794, 0x0);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6796);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6590), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c89;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6798);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6592), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c89;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x4a));
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb780)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c89;
			goto L0d8b;

		L0c89:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.EmitGlobalWord(0x6796, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.EmitGlobalWord(0x6798, this.processor.RegAcc.Value16);

			this.owner.Libc.FillBytes(0xb780, 0, 0x100);

			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x6590);
			this.processor.EmitGlobalByte(0x6594, this.processor.RegAcc.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x6592);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694), this.processor.RegAcc.LowByte);
			this.processor.RegDest.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xb780), 0x1);
			this.processor.EmitLocalWord(0x14, 0x0);
			this.processor.RegAcc.Value16 = arg3;
			this.processor.EmitGlobalWord(0x6794, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)((short)troopIdx));
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)((short)participantIdx));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0d0b;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0d0d;

		L0d0b:
			this.processor.RegAcc.Value16 = 0;

		L0d0d:
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113a)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0d1c;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0d1e;

		L0d1c:
			this.processor.RegAcc.Value16 = 0;

		L0d1e:
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L0d21:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6594));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x36));
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb780));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6794);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0d7c;

			TunePosition(this.processor.PullLocalSignedWord(0x1c));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x46));
			if (this.processor.FlagSet.IsNotEqual) goto L0da8;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0da8;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.EmitGlobalWord(0x6794, this.processor.RegAcc.Value16);

		L0d7c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0d8b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xe1);
			if (this.processor.FlagSet.IsLess) goto L0d21;

		L0d8b:
			this.processor.EmitLocalWord(0x16, 0xffff);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6794);
			this.processor.CompareWord(arg3, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0d9b;
			goto L10ed;

		L0d9b:
			this.processor.EmitLocalWord(0xa, 0x63);
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L0f29;

		L0da8:
			TunePosition(this.processor.PullLocalSignedWord(0x1c));

			this.owner.MapOps.ContainsRoad(
				(short)this.processor.RegAcc.Value16,
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L0dea;

		L0dc8:
			this.processor.RegAcc.Value16 = 0;

		L0dca:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1e));
			if (this.processor.FlagSet.IsNotEqual) goto L0dd2;
			goto L0e70;

		L0dd2:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0de7;
			goto L0e70;

		L0de7:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L0dea:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L0d7c;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1c));
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6590));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0de7;

			TunePosition(this.processor.PullLocalSignedWord(0x40));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6592));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0de7;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0de7;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0e6a;
			goto L0dc8;

		L0e6a:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0dca;

		L0e70:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0e8e;

			this.owner.MapOps.ContainsRoad(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0e8e;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			goto L0ead;

		L0e8e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0e9c;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x3);
			goto L0ead;

		L0e9c:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));

		L0ead:
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x44));
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb780));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x38, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0ed7;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0ed7;
			goto L0de7;

		L0ed7:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x44));
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb780), this.processor.RegAcc.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x40);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6594), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x44);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			goto L0de7;

		L0f0d:
			this.processor.EmitLocalWord(0x40, this.processor.SumWord(this.processor.PullLocalWord(0x40), 0x50));

		L0f11:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6590));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L0f61;

		L0f26:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L0f29:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0f32;
			goto L10e2;

		L0f32:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x46));
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6590));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x48);
			if (this.processor.FlagSet.IsLess) goto L0f11;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0f0d;
			this.processor.EmitLocalWord(0x40, this.processor.SubtractWord(this.processor.PullLocalWord(0x40), 0x50));
			goto L0f11;

		L0f61:
			TunePosition(this.processor.PullLocalSignedWord(0x40));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4a));
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6592));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f26;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0fa7;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0fa9;

		L0fa7:
			this.processor.RegAcc.Value16 = 0;

		L0fa9:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)((short)troopIdx));
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)((short)participantIdx));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0fd1;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0fd3;

		L0fd1:
			this.processor.RegAcc.Value16 = 0;

		L0fd3:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0fec;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0fec;
			goto L0f26;

		L0fec:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x20));
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x44));
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb780));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L100b;
			goto L0f26;

		L100b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L106e;
			this.processor.CopyLocalWord(0x6, 0xa);
			this.processor.CopyLocalWord(0x3c, 0x16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1050;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx,
				this.processor.PullLocalSignedWord(0x12), 2);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			goto L1055;

		L1050:
			this.processor.EmitLocalWord(0xe, 0x0);

		L1055:
			this.owner.Helpers.ObtainDistance(
				this.processor.PullGlobalSignedWord(0x6590),
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullGlobalSignedWord(0x6592),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0xe, this.processor.SumWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16));

		L106e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1079;
			goto L0f26;

		L1079:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L10aa;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx,
				this.processor.PullLocalSignedWord(0x12), 2);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			goto L10af;

		L10aa:
			this.processor.EmitLocalWord(0x1a, 0x0);

		L10af:
			this.owner.Helpers.ObtainDistance(
				this.processor.PullGlobalSignedWord(0x6590),
				this.processor.PullLocalSignedWord(0xc),
				this.processor.PullGlobalSignedWord(0x6592),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0x1a, this.processor.SumWord(this.processor.PullLocalWord(0x1a), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L10d3;
			goto L0f26;

		L10d3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x1a, 0xe);
			goto L0f26;

		L10e2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L10ed;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			goto L1116;

		L10ed:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1113;
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)((short)participantIdx));
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)((short)troopIdx));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);

		L1113:
			this.processor.RegAcc.Value16 = 0xffff;

		L1116:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftToward");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos1"></param>
		/// <param name="flag"></param>
		/// <param name="param6"></param>
		/// <returns></returns>
		public ushort ComputeRouteCost(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort marker, ushort arg6)
		{
			this.processor.Journal.OpenScope($"ComputeRouteCost({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, {marker}, {arg6})");

			// function body
			if (Math.Abs(horizLoc - horizSpot1) <= 7 && Math.Abs(vertLoc - vertSpot1) <= 7)
			{
				this.processor.EmitGlobalByte(0x87c9, (byte)((sbyte)horizLoc));
				this.processor.EmitGlobalByte(0x87ca, (byte)((sbyte)vertLoc));				
				this.processor.EmitGlobalByte(0x87cb, (byte)((marker == 0) ? 1 : 0x10));
				this.processor.EmitGlobalWord(0x6590, (ushort)((short)horizSpot1));
				this.processor.EmitGlobalWord(0x6592, (ushort)((short)vertSpot1));

				ShiftToward(0, 127, arg6);

				this.processor.EmitGlobalByte(0x87cb, 0xff);
				
				if (this.processor.RegAcc.Value16 == 0xffff)
				{
					this.processor.RegAcc.Value16 = 0xffff;
				}
				else
				{
					this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6794);
				}
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}

			// Far return
			this.processor.Journal.CloseScope("ComputeRouteCost");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Adjust X position
		/// </summary>
		/// <param name="xPos"></param>
		/// <returns></returns>
		public int TunePosition(int horizLoc)
		{
			int backAmountInt = horizLoc;

			// function body
			if (horizLoc < 0)
			{
				backAmountInt = horizLoc + 80;
			}
			else if (horizLoc >= 80)
			{
				backAmountInt = horizLoc - 80;
			}

			this.processor.RegAcc.Value16 = (ushort)((short)backAmountInt);

			return backAmountInt;
		}
	}
}
