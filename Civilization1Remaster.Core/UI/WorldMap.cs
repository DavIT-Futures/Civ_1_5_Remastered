using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class WorldMap
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public WorldMap(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="flag"></param>
		public void DisplayWorldMap(ushort marker)
		{
			this.processor.Journal.OpenScope($"DisplayWorldMap({marker})");

			// function body
			this.processor.EnterFrame(0x22);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa), 
				0, 0, 0x140, 0xc8, 
				this.processor.PullGlobalWord(0x19d4), 
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x3e7;
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x28;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L008d;
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			goto L017a;

		L008d:
			this.processor.EmitLocalWord(0xe, 0x0);

		L0092:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0xe));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L00d9;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L00b9;
			this.processor.EmitLocalWord(0x1a, this.processor.RegDest.Value16);

		L00b9:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe));
			this.processor.ScaleIndex(0x600, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsAboveOrEqual) goto L00d9;
			this.processor.EmitLocalWord(0x20, this.processor.RegSource.Value16);

		L00d9:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0092;
			this.processor.EmitLocalWord(0xe, 0x0);

		L00e8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0xe)].ParticipantIdx != this.owner.World.PersonParticipantIdx) goto L0123;
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xe)].Place.Y);
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L010b;
			this.processor.EmitLocalWord(0x1a, this.processor.RegDest.Value16);

		L010b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe));
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xe)].Place.Y);
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsAboveOrEqual) goto L0123;
			this.processor.EmitLocalWord(0x20, this.processor.RegSource.Value16);

		L0123:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x80);
			if (this.processor.FlagSet.IsLess) goto L00e8;
			this.processor.EmitLocalWord(0xe, 0x0);

		L0132:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), 0];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1e));
			if (this.processor.FlagSet.IsNotEqual) goto L0154;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), 1];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1e));
			if (this.processor.FlagSet.IsEqual) goto L015e;

		L0154:
			this.processor.EmitLocalWord(0x1a, 0x0);
			this.processor.EmitLocalWord(0x20, 0x31);

		L015e:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x50);
			if (this.processor.FlagSet.IsLess) goto L0132;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1a));
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x19);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

		L017a:
			this.processor.EmitLocalWord(0x10, 0x0);
			goto L0367;

		L0182:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L018d;
			
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x12),
				this.processor.PullLocalSignedWord(0x16),
				4, 4, 1);

			goto L01a8;

		L018d:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x12),
				this.processor.PullLocalSignedWord(0x16),
				4, 4, 2);

		L01a8:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L020f;

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ShownExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L01ef;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L01ef;
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ParticipantIdx == this.owner.World.PersonParticipantIdx) goto L01ef;
			goto L02aa;

		L01ef:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946));
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x12),
				this.processor.PullLocalSignedWord(0x16),
				4, 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			goto L02aa;

		L020f:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0228;
			goto L02aa;

		L0228:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0266;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0266;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullLocalWord(0x22));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02aa;

		L0266:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946));
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x12) + 1,
				this.processor.PullLocalSignedWord(0x16) + 1,
				3, 3, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x12),
				this.processor.PullLocalSignedWord(0x16),
				3, 3, 
				this.processor.PullLocalWord(0x22));

		L02aa:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L02ad:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x50);
			if (this.processor.FlagSet.IsLess) goto L02b6;
			goto L0364;

		L02b6:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xa),
				this.processor.PullLocalWord(0x10)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1e));
			if (this.processor.FlagSet.IsEqual) goto L02e6;

			this.owner.MapOps.VerifyMapLimits(0,
				this.processor.PullLocalSignedWord(0x10) + this.processor.PullLocalSignedWord(0x8));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L02ed;

		L02e6:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L02aa;

		L02ed:
			this.owner.Routing.TunePosition(
				this.processor.PullLocalSignedWord(0xa) +
					this.processor.PullLocalSignedWord(0x6));

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x10));

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			if (marker == 0)
				goto L0182;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)((this.processor.PullLocalWord(0x14) << 2) + 0xa0),
				(ushort)((((this.processor.PullLocalWord(0xa) + 
					this.processor.PullLocalWord(0x10)) & 1) << 2) + 0x6f),
				4, 4,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x12),
				this.processor.PullLocalWord(0x16));

			goto L01a8;

		L0364:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));

		L0367:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0375;
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L02ad;

		L0375:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L03a1;

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

		L03a1:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayWorldMap");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayGameFigures()
		{
			this.processor.Journal.OpenScope("DisplayGameFigures()");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 4);

			this.processor.EmitLocalWord(0x2, 0x1);

		L03df:
			this.processor.RegAcc.Value16 = 0x28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.TextPainter.RenderTextWithShadow(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1982)),
				this.processor.PullLocalSignedWord(0x2) * 40,
				8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1946)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.RegSource.Value16, 16,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((this.processor.PullLocalWord(0x2) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L03df;
			this.processor.EmitLocalWord(0x6, 0x1);
			goto L0502;

		L042a:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L042d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0436;
			goto L04ff;

		L0436:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Landmasses[this.processor.PullLocalWord(0x6)].Assault == 0)
				goto L042a;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Landmasses[this.processor.PullLocalWord(0x6)].Guard, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Landmasses[this.processor.PullLocalWord(0x6)].Assault, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Landmasses[this.processor.PullLocalWord(0x6)].TownTally, 10));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x2) * 40,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			goto L042a;

		L04ff:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L0502:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L055e;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0x6) << 1) + 0xdcfe) & 0xffff)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x4), 0);

			this.processor.EmitLocalWord(0x2, 0x1);
			goto L042d;

		L055e:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayGameFigures");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayCivilizationGraph()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationGraph()");

			// function body
			this.processor.EnterFrame(0x10);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 8);

			this.processor.EmitLocalWord(0x6, 0x32);

			this.owner.Dialogs.RenderBox(4, 9, 311, 183, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L0673;

		L05d2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.HalveAccumulator();
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.RegSource.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x4);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				(short)this.processor.RegDest.Value16, 9, (short)this.processor.RegDest.Value16, 192, 0);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x64;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L066f;

			this.owner.Replayer.ObtainReplayYear(this.processor.PullLocalWord(0xa));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(Math.Abs((short)this.processor.PullLocalWord(0x10)), 10));

			if (this.processor.PullLocalSignedWord(0x10) < 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x43d2);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x43d5);
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.processor.RegSource.Value16, 194, 15);

		L066f:
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x32));

		L0673:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0685;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x258);
			if (this.processor.FlagSet.IsGreater) goto L0685;
			goto L05d2;

		L0685:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			this.processor.EmitLocalWord(0x2, 0x1);
			goto L06d2;

		L0695:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));

		L0698:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			if (this.processor.FlagSet.IsLessOrEqual) goto L06cf;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x2));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsAboveOrEqual) goto L0695;
			this.processor.EmitLocalWord(0x6, this.processor.RegSource.Value16);
			goto L0695;

		L06cf:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L06d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L071b;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L06ef;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.BotRivalTally;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L06cf;

		L06ef:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
				8,
				(this.processor.PullLocalSignedWord(0x2) << 3) + 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.processor.EmitLocalWord(0xe, 0x0);
			goto L0698;

		L071b:
			this.owner.TextPainter.RenderTextWithShadow(0x43d8, 100, 2, 15);

			this.processor.EmitLocalWord(0x2, 0x1);
			goto L07bc;

		L073b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x2));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.LowByte = 0xb4;
			this.processor.MultiplyByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)));
			this.processor.RegData.Value16 = 0;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x6));
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa) + 4,
				192 - this.processor.PullLocalSignedWord(0xc),
				this.processor.PullLocalSignedWord(0x4) + 4,
				192 - this.processor.PullLocalSignedWord(0x8),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((this.processor.PullLocalWord(0x2) << 1) + 0x1946)));

			this.processor.CopyLocalWord(0x4, 0xa);
			this.processor.CopyLocalWord(0x8, 0xc);
			this.processor.IncrementLocalWord(0xe);

		L079c:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			if (this.processor.FlagSet.IsLessOrEqual) goto L07b9;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x96);
			if (this.processor.FlagSet.IsLess) goto L073b;

		L07b9:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L07bc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L07ea;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L07d9;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.BotRivalTally;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L07b9;

		L07d9:
			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.EmitLocalWord(0xc, 0x0);
			this.processor.EmitLocalWord(0xe, 0x0);
			goto L079c;

		L07ea:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilizationGraph");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayWondersOfTheWorld()
		{
			this.processor.Journal.OpenScope("DisplayWondersOfTheWorld()");

			// function body
			this.processor.EnterFrame(0xc);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitLocalWord(0x6, 0x3e7);
			this.processor.EmitLocalWord(0x8, 0x1);
			goto L0902;

		L082a:
			this.owner.Libc.AppendText(0xba06, 0x4413);

		L082d:
			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x4));

			this.owner.Libc.AppendText(0xba06, ". (");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, ")");
			goto L0891;

		L087c:
			this.owner.Libc.AppendText(0xba06, " (Destroyed)");

			this.processor.EmitLocalWord(0xc, 0x0);

		L0891:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Dialogs.RenderBox(8, this.processor.PullLocalSignedWord(0x6),
				303, 15,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.CityOps.RenderResourceIcon(this.processor.PullLocalSignedWord(0xa),
				16, (short)(this.processor.PullLocalSignedWord(0x6) + 3));

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 270);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, this.processor.PullLocalSignedWord(0x6) + 5, 15);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x18));

		L08ff:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L0902:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L090b;
			goto L09cf;

		L090b:
			if (this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x8)] == -1) goto L08ff;

			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xb4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0963;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x3e7);
			if (this.processor.FlagSet.IsEqual) goto L092a;

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

		L092a:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

			this.owner.TextPainter.RenderTextWithShadow(0x43f0, 100, 12, 15);

			this.processor.EmitLocalWord(0x6, 0x20);

		L0963:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x8)];
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ParticipantIdx;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x7);
			if (this.processor.FlagSet.IsGreater) goto L099a;

			this.owner.Libc.AppendText(0xba06, "The ");

		L099a:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xe6a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			if (this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x8)] != 128)
				goto L09c0;

			goto L087c;

		L09c0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x7);
			if (this.processor.FlagSet.IsLessOrEqual) goto L09c9;
			goto L082a;

		L09c9:
			this.owner.Libc.AppendText(0xba06, 0x440e);

			goto L082d;

		L09cf:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayWondersOfTheWorld");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayEpicChronicle()
		{
			this.processor.Journal.OpenScope("DisplayEpicChronicle()");

			// function body
			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(5));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4516)));

			this.owner.Libc.AppendText(0xba06, " completes his epic history:");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 0, 4, 15);

			this.owner.Libc.CopyText(0xba06, "'The ");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x452c)));

			this.owner.Libc.AppendText(0xba06, " Civilizations in the World'");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 12, 15);
			
			this.processor.EmitLocalWord(0xc, 0x1);

		L0ab9:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), 0x0);
			this.processor.IncrementLocalWord(0xc);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0ab9;
			this.processor.EmitLocalWord(0xc, 0x0);

		L0ad1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx == -1) goto L0b36;
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0b36;

			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0xc), -1);

			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0b17;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c)), this.processor.RegCount.Value16));

		L0b17:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0b36;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c)), this.processor.RegAcc.Value16));

		L0b36:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0ad1;
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L0b56;

		L0b47:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Gold;

		L0b50:
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), this.processor.RegAcc.Value16);

		L0b53:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0b56:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0bb0;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0b47;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0b6f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0b7a;
			goto L0b53;

		L0b6f:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].MartialMight;
			goto L0b50;

		L0b7a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), 0x0);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0b8e;

		L0b8b:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0b8e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b53;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0xc));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0b8b;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c))));
			goto L0b8b;

		L0bb0:
			this.processor.EmitLocalWord(0xa, 0x20);
			this.processor.EmitLocalWord(0x20, 0x0);
			this.processor.EmitLocalWord(0xc, 0x1);
			goto L0d29;

		L0bc2:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x2, 0x1e);

		L0bd3:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0bd6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0bf9;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0bd3;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L0bc2;
			goto L0bd3;

		L0bf9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0c02;
			goto L0d26;

		L0c02:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(this.processor.PullLocalWord(0x20) + 1), 10));

			this.owner.Libc.AppendText(0xba06, ". The ");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x451e)));

			this.owner.Libc.AppendText(0xba06, " Civilization of the ");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ".");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x1e)] & 0x40) != 0)
				goto L0ca1;

			this.processor.RegAcc.Value16 = (ushort)owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ca1;

			if (this.owner.World.HardnessTier != 0)
				goto L0d0f;

		L0ca1:
			this.owner.TextPainter.AwaitTime(60);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.TurnHooks.RenderPalace(this.processor.PullLocalSignedWord(0x1e),
				this.owner.TurnHooks.ObtainPalaceLevel(this.owner.World.Participants[this.processor.PullLocalWord(0x1e)].Tally),
				128, 
				(short)(this.processor.PullLocalSignedWord(0xa) - 13));

			this.owner.Dialogs.RenderBox(8, this.processor.PullLocalSignedWord(0xa),
				303, 15, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, this.processor.PullLocalSignedWord(0xa) + 5, 15);

			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x18));

		L0d0f:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x7;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x20));
			this.owner.World.Participants[this.processor.PullLocalWord(0x1e)].RunningSagaStanding += (short)this.processor.RegAcc.Value16;
			this.processor.IncrementLocalWord(0x20);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1c), 0xffff);

		L0d26:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0d29:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d4f;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L0d26;
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x1e, 0xffff);
			this.processor.EmitLocalWord(0x2, 0x1);
			goto L0bd6;

		L0d4f:
			this.owner.World.Participants[0].RunningSagaStanding++;

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayEpicChronicle");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCensus(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCensus({participantIdx})");

			// function body
			this.processor.EnterFrame(0xba);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " Demographics");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 15);

			this.processor.EmitLocalWord(0x56, 0x0);

		L0dde:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98), 0x1);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x40), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xb8), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x78), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x52), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x30), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x66), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x56);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0dde;
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			goto L0e4f;

		L0e18:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L0e1b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x30);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e4c;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalSignedWord(0x54));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0e18;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalSignedWord(0x54));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xb8), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xb8))));
			goto L0e18;

		L0e4c:
			this.processor.EmitLocalWord(0x42, this.processor.IncrementWord(this.processor.PullLocalWord(0x42)));

		L0e4f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e5c;
			this.processor.EmitLocalWord(0x54, 0x2);
			goto L0e1b;

		L0e5c:
			this.processor.EmitLocalWord(0x56, 0x0);
			goto L0f84;

		L0e64:
			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x56), -1);

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x56));
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x98), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x98)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x20), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x20)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe17a));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x78), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x78)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x40), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x40)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70dc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x30), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x30)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x66), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x66)), this.processor.RegAcc.Value16));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0x68, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x68, this.processor.SumWord(this.processor.PullLocalWord(0x68), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x68), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0eff;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x68);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x88), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x88)), this.processor.RegAcc.Value16));

		L0eff:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0f1f;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10)), this.processor.RegAcc.Value16));

		L0f1f:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L0f40;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10)), this.processor.RegAcc.Value16));

		L0f40:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L0f60;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52)), this.processor.RegAcc.Value16));

		L0f60:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x800);
			if (this.processor.FlagSet.IsEqual) goto L0f81;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52)), this.processor.RegAcc.Value16));

		L0f81:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L0f84:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0fa6;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x56));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx == -1) goto L0f81;
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0fa4;
			goto L0e64;

		L0fa4:
			goto L0f81;

		L0fa6:
			this.processor.EmitLocalWord(0x54, 0x18);

			this.owner.TextPainter.RenderTextWithShadow(0x4544, 8, 24, 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L0fc8:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1035;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1035:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0fc8;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4557);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4559, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1071:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Helpers.ObtainPlayerPopulation(
				this.processor.PullLocalSignedWord(0x56));

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L10a9;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L10a9:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1071;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4565);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x456a, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L10e5:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x78));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " million $");

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L114d;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L114d:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L10e5;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x457a);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x457d, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1189:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x66));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L11ec;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " Mtons");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L11ec:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1189;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4590);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4597, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1228:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xb8));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, ",000 sq.miles");

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1291;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1291:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1228;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x45b0);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45b5, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L12cd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x52)));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0x16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0x1e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0xd);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegSource.Value16 = (ushort)((this.processor.PullLocalWord(0x56) << 1) + 
				this.processor.RegFrame.Value16 - 0xa8);
			this.owner.Helpers.VerifyValueSpan(this.processor.PullInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0, 100);

			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L13c5;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L13c5:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L13d1;
			goto L12cd;

		L13d1:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x45c1);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45c3, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1404:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 10);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegCount.Value16 = 0x2;
				this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8));
				this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
				this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			}
		
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x5dc;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x14);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L14ad;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L14ad:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L14b9;
			goto L1404;

		L14b9:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), (short)(-this.processor.PullLocalSignedWord(0x54)), 0x45ce);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45d0, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L14ef:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.SubtractWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1564;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "0 tons/year");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1564:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L14ef;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), (short)(-this.processor.PullLocalSignedWord(0x54)), 0x45e7);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45ee, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L15a3:
			this.processor.RegSource.Value16 = (ushort)((this.processor.PullLocalWord(0x56) << 1) +
				this.processor.RegFrame.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)), 20, 99);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1616;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " years");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1616:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L15a3;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4606);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x460b, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1652:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x28;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x40)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1678;
			goto L1704;

		L1678:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)) / 10), 10));

			this.owner.Libc.AppendText(0xba06, ".");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)) / 10), 10));

			this.owner.Libc.AppendText(0xba06, " children");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1704:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x56);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1723;
			goto L1652;

		L1723:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4624);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4625, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1756:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].TroopTally);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L17c2;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " years");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L17c2:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1756;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x463e);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4643, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);
			goto L1874;

		L1800:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally);

		L1813:
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1871;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegStack.Value16,
					(ushort)((this.processor.RegFrame.Value16 + (this.processor.PullLocalWord(0x56) << 1) - 0xa8) & 0xffff)), 10));

			this.owner.Libc.AppendText(0xba06, "$ per capita");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1871:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L1874:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18a3;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x7530;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = (ushort)(this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally + 1);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78)));
			if (this.processor.FlagSet.IsGreater) goto L1892;
			goto L1800;

		L1892:
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			goto L1813;

		L18a3:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x465f);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4661, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L18d6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x30)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1933;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1933:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L18d6;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x466f);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCensus");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="yPos"></param>
		/// <param name="stringPtr"></param>
		public void RenderCensusLine(ushort arg1, short vertLoc, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"RenderCensusLine({arg1}, {vertLoc}, 0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x2, 0x1);
			this.processor.EmitLocalWord(0x4, 0xd8f1);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x1);

		L1985:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L19d9;
			
			if (vertLoc >= 0)
				goto L19a7;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, arg1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

		L19a7:
			this.processor.RegBase.Value16 = arg1;
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L19c1;
			this.processor.IncrementLocalWord(0x2);

		L19c1:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegDest.Value16 = arg1;
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L19d9;
			this.processor.EmitLocalWord(0x4, this.processor.RegSource.Value16);
			this.processor.CopyLocalWord(0x6, 0x8);

		L19d9:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1985;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			switch (this.processor.PullLocalWord(0x2))
			{
				case 1:
					this.owner.Libc.AppendText(0xba06, 0x4670);
					break;

				case 2:
					this.owner.Libc.AppendText(0xba06, 0x4673);
					break;

				case 3:
					this.owner.Libc.AppendText(0xba06, 0x4676);
					break;

				default:
					this.owner.Libc.AppendText(0xba06, 0x4679);
					break;
			}

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				4, Math.Abs(vertLoc) - 3, 316, Math.Abs(vertLoc) - 3, 9);

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 192, Math.Abs(vertLoc), 15);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1a97;
			goto L1b52;

		L1a97:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x8)] & 0x40) == 0)
				goto L1b52;

			this.owner.Libc.CopyText(0xba06, "(");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ": ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(Math.Abs((short)this.processor.PullLocalWord(0x4)), 10));

			this.owner.Libc.AppendText(0xba06, charsAddr);

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				-(this.owner.TextPainter.ObtainTextWidth(0xba06) - 318),
				Math.Abs(vertLoc),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

		L1b52:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCensusLine");
		}
	}
}
