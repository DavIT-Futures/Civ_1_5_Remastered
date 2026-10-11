using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

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
			if (troopIdx < 128)
			{
				this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
				this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			}

			int stepDir = MovementRules.DirectionTo(0, 0, (short)this.processor.PullLocalWord(0x1e), (short)this.processor.PullLocalWord(0x20));
			this.processor.RegAcc.Value16 = (ushort)stepDir;
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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort ChartRoute(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ChartRoute({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x3a);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			LocateRouteStep(
				this.processor.PullLocalSignedWord(0x36),
				this.processor.PullLocalSignedWord(0x3a),
				(ushort)((this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)) != 2) ? 0 : 1));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0674;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6590);
			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			goto L0699;

		L0674:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);
			goto L0a26;

		L0699:
			LocateRouteStep(
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				(ushort)((this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)) != 0x2) ? 0 : 1));

			this.owner.Libc.FillBytes(0xd816, 0, 0x104);

			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x6590);
			this.processor.EmitGlobalByte(0x6594, this.processor.RegAcc.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x6592);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694), this.processor.RegAcc.LowByte);
			this.processor.ScaleIndex(0xd, this.processor.PullGlobalWord(0x6590));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6592);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xd816), 0x1);
			this.processor.EmitLocalWord(0xc, 0x0);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0708;
			this.processor.RegAcc.Value16 = 0x1;
			goto L070a;

		L0708:
			this.processor.RegAcc.Value16 = 0;

		L070a:
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

		L070d:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6594));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x36);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0738;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2c), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0738;
			this.processor.EmitLocalWord(0xc, 0x1);
			goto L0815;

		L0738:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x14));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2c);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xd816));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L076a;
			this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x7f38));
			goto L0776;

		L076a:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdb44));

		L0776:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x1);

		L077f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10));
			if (this.processor.FlagSet.IsEqual) goto L07f8;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0x30, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x14);
			if (this.processor.FlagSet.IsNotEqual) goto L07a7;
			this.processor.EmitLocalWord(0x30, 0x0);

		L07a7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L07b2;
			this.processor.EmitLocalWord(0x30, 0x13);

		L07b2:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2c));
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0x30));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x34));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xd816);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07f8;
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.RegAcc.LowByte = this.processor.IncrementByte(this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x30);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6594), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x34);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6694), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

		L07f8:
			this.processor.EmitLocalWord(0x2e, this.processor.IncrementWord(this.processor.PullLocalWord(0x2e)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L0804;
			goto L077f;

		L0804:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0815;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0815;
			goto L070d;

		L0815:
			this.processor.EmitGlobalWord(0x6590, 0xffff);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0824;
			goto L09fc;

		L0824:
			this.processor.EmitLocalWord(0x8, 0x63);
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L085e;
			this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0x36));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x7f38));
			goto L086d;

		L085e:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x36));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdb44));

		L086d:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x1);
			goto L08d2;

		L0878:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L08cf;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullLocalSignedWord(0x30) * 4 + 1,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				this.processor.PullLocalSignedWord(0x34) * 4 + 1);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L08cf;
			this.processor.CopyLocalWord(0x2e, 0xe);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);

		L08cc:
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L08cf:
			this.processor.EmitLocalWord(0x2e, this.processor.IncrementWord(this.processor.PullLocalWord(0x2e)));

		L08d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L08db;
			goto L0981;

		L08db:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10));
			if (this.processor.FlagSet.IsEqual) goto L08cf;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x30, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0x14);
			if (this.processor.FlagSet.IsNotEqual) goto L090e;
			this.processor.EmitLocalWord(0x30, 0x0);

		L090e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0919;
			this.processor.EmitLocalWord(0x30, 0x13);

		L0919:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x30));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x34);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xd816));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L08cf;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L093b;
			goto L0878;

		L093b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x2e, 0xe);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullLocalSignedWord(0x30) * 4 + 1,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				this.processor.PullLocalSignedWord(0x34) * 4 + 1);

			goto L08cc;

		L0981:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L09fc;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);

			TunePosition(this.processor.PullGlobalSignedWord(0x6590));

			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullGlobalSignedWord(0x6590),
				this.processor.PullGlobalSignedWord(0x6592));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L09cc;
			this.processor.RegAcc.Value16 = 0x1;
			goto L09ce;

		L09cc:
			this.processor.RegAcc.Value16 = 0;

		L09ce:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x16));
			if (this.processor.FlagSet.IsEqual) goto L09fc;
			this.processor.EmitGlobalWord(0x6590, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6590)));

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullGlobalSignedWord(0x6590),
				this.processor.PullGlobalSignedWord(0x6592));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L09f1;
			this.processor.RegAcc.Value16 = 0x1;
			goto L09f3;

		L09f1:
			this.processor.RegAcc.Value16 = 0;

		L09f3:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x16));
			if (this.processor.FlagSet.IsEqual) goto L09fc;
			this.processor.EmitGlobalWord(0x6592, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6592)));

		L09fc:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6590), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0a23;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);

		L0a23:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);

		L0a26:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChartRoute");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		public ushort LocateRouteStep(int horizLoc, int vertLoc, ushort marker)
		{
			this.processor.Journal.OpenScope($"LocateRouteStep({horizLoc}, {vertLoc}, {marker})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = (ushort)((short)horizLoc);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)vertLoc);
			this.processor.AbsoluteAccumulator();
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.BitXorWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe, 0xffff);

			if (marker != 0)
			{
				this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0x12));
				this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);

				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x7f38)) != 0)
				{
					this.processor.EmitLocalWord(0xe, 0x0);
				}
			}
			else
			{
				this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0x12));
				this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
				
				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdb44)) != 0x0)
				{
					this.processor.EmitLocalWord(0xe, 0x0);
				}
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0a96;
			goto L0bf1;

		L0a96:
			this.processor.EmitLocalWord(0x6, 0x63);
			this.processor.EmitLocalWord(0x10, 0x1);
			goto L0ad8;

		L0aa2:
			this.processor.RegAcc.Value16 = 0;

		L0aa4:
			if (this.processor.RegAcc.Value16 != marker)
				goto L0b7f;

		L0aac:
			ComputeRouteCost(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x8),
				horizLoc, vertLoc, marker, 18);

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ad5;
			this.processor.CopyLocalWord(0x4, 0x6);
			this.processor.CopyLocalWord(0x10, 0xe);

		L0ad5:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));

		L0ad8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0ae1;
			goto L0bf1;

		L0ae1:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x12));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			if (marker == 0) goto L0b12;
			this.processor.ScaleIndex(0xd, this.processor.PullLocalWord(0xa));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x7f38)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b2a;

		L0b12:
			if (marker != 0) goto L0ad5;
			this.processor.ScaleIndexToSource(0xd, this.processor.PullLocalWord(0xa));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdb44)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ad5;

		L0b2a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 <<= 2;

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegDest.Value16 <<= 2;

			this.owner.Helpers.MergeCoordinates(
				horizLoc - (this.processor.PullLocalSignedWord(0xa) * 4) - 1,
				vertLoc - (this.processor.PullLocalSignedWord(0xc) * 4) - 1);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0b59;
			goto L0ad5;

		L0b59:
			// LEA
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegSource.Value16 + 0x1);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			// LEA
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegDest.Value16 + 0x1);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0b79;
			goto L0aa2;

		L0b79:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0aa4;

		L0b7f:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0b9a;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0b9c;

		L0b9a:
			this.processor.RegAcc.Value16 = 0;

		L0b9c:
			if (this.processor.RegAcc.Value16 == marker)
				goto L0aac;

			this.processor.IncrementLocalWord(0x8);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0bbf;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0bc1;

		L0bbf:
			this.processor.RegAcc.Value16 = 0;

		L0bc1:
			if (this.processor.RegAcc.Value16 == marker)
				goto L0aac;

			this.processor.DecrementLocalWord(0x2);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0be4;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0be6;

		L0be4:
			this.processor.RegAcc.Value16 = 0;

		L0be6:
			if (this.processor.RegAcc.Value16 == marker) goto L0aac;
			goto L0ad5;

		L0bf1:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0c15;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x12));
			this.processor.EmitGlobalWord(0x6590, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitGlobalWord(0x6592, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			goto L0c17;

		L0c15:
			this.processor.RegAcc.Value16 = 0;

		L0c17:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateRouteStep");

			return this.processor.RegAcc.Value16;
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
			int wrapped = MovementRules.WrapX(horizLoc);
			this.processor.RegAcc.Value16 = (ushort)((short)wrapped);
			return wrapped;
		}
	}
}
