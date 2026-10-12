using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2aea.
	/// </summary>
	public class MapManagement
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly MapLayers chart;
		private readonly UnitMemory troops;

		public MapManagement(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.chart = new MapLayers(owner);
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>The map data (terrain, continents, improvements, tile owners)</summary>
		public MapLayers Chart
		{
			get { return this.chart; }
		}

		/// <summary>
		/// Returns the value also in AX, like the original function: the translated callers read it from there.
		/// </summary>
		private ushort ReturnAcc(int amount)
		{
			this.processor.RegAcc.Value16 = (ushort)amount;

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void FocusMapView(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"FocusMapView({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.EmitGlobalWord(0x1ae0, 0x0);

			this.owner.TurnHooks.WipeStatusLine();

			this.owner.TurnHooks.RenderStatusBar();

			this.processor.EmitLocalWord(0x2, this.processor.PullGlobalWord(0xd20a));

			this.owner.Routing.TunePosition(horizLoc);

			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(vertLoc, 0, 38);
			this.processor.EmitGlobalWord(0xd75e, this.processor.RegAcc.Value16);

			// Another error, the code modified first parameter (playerID) to a
			// Visibility Mask and that conflicts with other code which expects playerID.
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			ushort chartSightFilter = this.processor.RegAcc.Value16;
			this.processor.EmitGlobalWord(0x6c96, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(256));

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L00ac;

		L0074:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0x18) * 16) + 80,
				(this.processor.PullLocalSignedWord(0x1c) * 16) + 8,
				16, 16, 0);

		L009c:
			this.processor.RegAcc.Value16 = 0x5;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.LowByte = this.processor.IncrementByte(this.processor.RegAcc.LowByte);
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0xa);

		L00ac:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x100);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L012b;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0xf);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L009c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L009c;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0108;

			this.owner.Routing.TunePosition(
				this.processor.PullLocalSignedWord(0x18) +
					this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, chartSightFilter);
			if (this.processor.FlagSet.IsNotEqual) goto L0108;
			goto L0074;

		L0108:
			this.owner.Routing.TunePosition(
				this.processor.PullLocalSignedWord(0x18) +
					this.processor.PullGlobalSignedWord(0xd4cc));

			RepaintMapTile((short)this.processor.RegAcc.Value16,
				this.processor.PullLocalSignedWord(0x1c) + this.processor.PullGlobalSignedWord(0xd75e));

			goto L009c;

		L012b:
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L0198;

		L0132:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6e3e)), 0xb8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0195;
			this.processor.EmitGlobalByte(0xba06, 0x0);
			
			this.owner.Conquest.ObtainCityLabel(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xdf20)));
			
			this.owner.BlockText.ShrinkTextToWidth(0xba06,
				(ushort)(327 - this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6dac))));

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				this.owner.Helpers.VerifyValueSpan(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6dac)) - 8, 80, 999),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6e3e)) + 16,
				11);

		L0195:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L0198:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c96);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0132;
			this.processor.RegAcc.Value16 = (ushort)horizLoc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x20);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)vertLoc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x13);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 8);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.EmitLocalWord(0x12, 0x0);
			}
		
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsLess) goto L01d5;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			goto L01db;

		L01d5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);

		L01db:
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x50);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L01ea;
			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));

		L01ea:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01f3;
			this.processor.RegAcc.Value16 = 0;

		L01f3:
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x12));
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x32;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0211;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitLocalWord(0x8, this.processor.SubtractWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16));

		L0211:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.EmitGlobalWord(0x6ed6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitGlobalWord(0x70ea, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 8, 80, 50, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.Helpers.VerifyValueSpan(
					this.processor.PullLocalSignedWord(0xc), 0, 16);

				this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalWord(0x6ed6, this.processor.RegAcc.Value16);

				this.owner.Helpers.VerifyValueSpan(
					this.processor.PullLocalSignedWord(0x12), 0, 65530);

				this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalWord(0x70ea, this.processor.RegAcc.Value16);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19e8),
					this.processor.PullLocalWord(0xc),
					this.processor.PullLocalWord(0x12),
					0x50, 0x32,
					this.processor.PullGlobalWord(0xaa),
					0, 8);
			}
			else
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19e8),
					(ushort)(this.processor.PullLocalWord(0x16) + 0xf0),
					this.processor.PullLocalWord(0x1a),
					this.processor.PullLocalWord(0x6),
					this.processor.PullLocalWord(0x8),
					this.processor.PullGlobalWord(0xaa),
					0, (ushort)(this.processor.PullLocalWord(0x14) + 8));

				this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x50);
				if (this.processor.FlagSet.IsBelow)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						0xf0,
						this.processor.PullLocalWord(0x1a),
						(ushort)(0x50 - this.processor.PullLocalWord(0x6)),
						this.processor.PullLocalWord(0x8),
						this.processor.PullGlobalWord(0xaa),
						this.processor.PullLocalWord(0x6),
						(ushort)(this.processor.PullLocalWord(0x14) + 8));
				}
			}

			this.processor.EmitLocalWord(0xa, 0x0);

		L02e7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xa));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L035b;
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ShownExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0308;

			if (this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L035b;

		L0308:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xa));

			this.owner.Routing.TunePosition(
				this.owner.World.Towns[this.processor.PullLocalWord(0xa)].Place.X -
					this.processor.PullLocalSignedWord(0x16));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0xa)].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1a));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L035b;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L035b;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x18),
				this.processor.PullLocalSignedWord(0x1c) + 8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

		L035b:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x80);
			if (this.processor.FlagSet.IsLess) goto L02e7;

			this.owner.Dialogs.RenderShadowBox(0, 8, 79, 49, 15, 8);

			this.owner.Dialogs.RenderBox(
				horizLoc - this.processor.PullLocalSignedWord(0xc) - 1,
				vertLoc - this.processor.PullLocalSignedWord(0x12) + 7,
				17, 10, 15);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.EmitGlobalWord(0xd20a, this.processor.RegAcc.Value16);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("FocusMapView");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort RenderMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderMapTile({horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x26);
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0430;

			ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0413;

			ObtainTileOwnerBits(horizLoc, vertLoc);

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc * 4, vertLoc * 4, 4, 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			goto L0e23;

		L0413:
			ObtainTerrainType(horizLoc, vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L042a;
			
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc * 4, vertLoc * 4, 4, 4, 1);

			goto L0e23;

		L042a:
			this.processor.RegAcc.Value16 = 0x2;
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc * 4, vertLoc * 4, 4, 4, 2);

			goto L0e23;

		L0430:
			this.owner.Routing.TunePosition(
				horizLoc - this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)vertLoc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x50);
			if (this.processor.FlagSet.IsLess) goto L0470;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0470;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L0470;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0475;

		L0470:
			this.processor.RegAcc.Value16 = 0;
			goto L0e23;

		L0475:
			ObtainTerrainType(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			ObtainKnownTileImprovements(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04ac;

			ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

		L04ac:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L04ba;
			goto L0789;

		L04ba:
			this.processor.EmitLocalWord(0x12, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04c9;
			goto L0549;

		L04c9:
			this.processor.EmitLocalWord(0xc, 0x1);

		L04ce:
			this.processor.EmitLocalWord(0x12, this.processor.ShiftRightSignedWord(this.processor.PullLocalWord(0x12), 0x1));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			ObtainTerrainType((short)this.processor.RegAcc.Value16,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0515;

			VerifyMapLimits(0, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0515;
			this.processor.EmitLocalByte(0x12, this.processor.BitOrByte(this.processor.PullLocalByte(0x12), 0x8));

		L0515:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L04ce;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19fc),
				(ushort)(this.processor.PullLocalWord(0x12) << 4),
				0x40, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

			goto L0789;

		L0549:
			this.processor.EmitLocalWord(0xc, 0x1);

		L054e:
			this.processor.EmitLocalWord(0x12, this.processor.ShiftRightSignedWord(this.processor.PullLocalWord(0x12), 0x1));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			ObtainTerrainType((short)this.processor.RegAcc.Value16,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0595;

			VerifyMapLimits(0, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0595;
			this.processor.EmitLocalByte(0x12, this.processor.BitOrByte(this.processor.PullLocalByte(0x12), 0x80));

		L0595:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L054e;
			this.processor.CopyLocalWord(0x12, 0xe);
			this.processor.RegCount.LowByte = 0x6;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L05ff;

		L05be:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xc);
			this.processor.RegCount.LowByte = this.processor.ShiftLeftByte(this.processor.RegCount.LowByte, 0x1);
			this.processor.RegSource.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x7);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xd294)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegData.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegData.Value16 = this.processor.BitAndWord(this.processor.RegData.Value16, 0x1);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);

		L05ef:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.RegAcc.Value16,
				(short)this.processor.UnstackWord(),
				this.processor.UnstackWord());

			this.processor.IncrementLocalWord(0xc);

		L05ff:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0635;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05be;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xc);
			this.processor.RegCount.LowByte = this.processor.ShiftLeftByte(this.processor.RegCount.LowByte, 0x1);
			this.processor.RegSource.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x7);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xd294)));
			this.processor.StackWord(this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			goto L05ef;

		L0635:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x1c);
			if (this.processor.FlagSet.IsNotEqual) goto L065e;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xe0, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L065e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xc1);
			if (this.processor.FlagSet.IsNotEqual) goto L0688;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xf0, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L0688:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L06b1;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0x100, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L06b1:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x70);
			if (this.processor.FlagSet.IsNotEqual) goto L06da;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0x110, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L06da:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8f);
			if (this.processor.FlagSet.IsNotEqual) goto L0704;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0x120, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L0704:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xf8);
			if (this.processor.FlagSet.IsNotEqual) goto L072e;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0x130, 0x64, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x6),
				this.processor.PullLocalWord(0xa));

		L072e:
			this.processor.EmitLocalWord(0xc, 0x1);

		L0733:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			ObtainTerrainType((short)this.processor.RegAcc.Value16,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L077f;
			
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.HalveAccumulator();
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd2d4)));

		L077f:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L0733;

		L0789:
			if (this.processor.PullLocalWord(0x18) != 0xa)
			{
				if (this.processor.PullGlobalWord(0xd762) == 0x0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19fc),
						0, 0x50, 16, 16,
						this.processor.PullGlobalWord(0xaa),
						this.processor.PullLocalWord(0x6),
						this.processor.PullLocalWord(0xa));
				}
				else
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						0x100, 0x78, 16, 16,
						this.processor.PullGlobalWord(0xaa),
						this.processor.PullLocalWord(0x6),
						this.processor.PullLocalWord(0xa));
				}
			}
			
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0800;
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0800;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdcfc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0800;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0800;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((4 << 1) + 0xd4ce)));

		L0800:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xb);
			if (this.processor.FlagSet.IsEqual) goto L0809;
			goto L088d;

		L0809:
			this.processor.EmitLocalWord(0x12, 0x0);
			this.processor.EmitLocalWord(0xc, 0x1);

		L0813:
			this.processor.EmitLocalWord(0x12, this.processor.ShiftRightSignedWord(this.processor.PullLocalWord(0x12), 0x1));

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			ObtainTerrainType(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xb);
			if (this.processor.FlagSet.IsEqual) goto L083f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0843;

		L083f:
			this.processor.EmitLocalByte(0x12, this.processor.BitOrByte(this.processor.PullLocalByte(0x12), 0x8));

		L0843:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L0813;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L086e;

			this.owner.VideoCard.ObtainDot(2, horizLoc, vertLoc + 150);

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L0872;

		L086e:
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x10));

		L0872:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((this.processor.PullLocalWord(0x12) << 1) + 0x6dfe)));

		L088d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0896;
			goto L099e;

		L0896:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L089f;
			goto L099e;

		L089f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L08a9;
			goto L0955;

		L08a9:
			this.processor.EmitLocalWord(0x12, 0x0);
			this.processor.EmitLocalWord(0xc, 0x1);

		L08b3:
			this.processor.EmitLocalWord(0x12, this.processor.ShiftRightSignedWord(this.processor.PullLocalWord(0x12), 0x1));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			ObtainTerrainType((short)this.processor.RegAcc.Value16,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x18));
			if (this.processor.FlagSet.IsNotEqual) goto L08fa;

			VerifyMapLimits(0, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L08fa;
			this.processor.EmitLocalByte(0x12, this.processor.BitOrByte(this.processor.PullLocalByte(0x12), 0x8));

		L08fa:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x9);
			if (this.processor.FlagSet.IsLess) goto L08b3;
			
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((this.processor.PullLocalWord(0x12) << 1) +
					(this.processor.PullLocalWord(0x18) << 5) + 0xb886)));

			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L099e;
			this.processor.ScaleIndex(0x7, (ushort)horizLoc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xb, (ushort)vertLoc);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.RegCount.LowByte, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L099e;
			this.processor.StackWord(this.processor.PullGlobalWord(0xb880));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			goto L0992;

		L0955:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0976;
			this.processor.ScaleIndex(0xb, (ushort)vertLoc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x7, (ushort)horizLoc);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftRightWord(this.processor.RegSource.Value16, 0x1);
			goto L097f;

		L0976:
			this.processor.RegSource.Value16 = (ushort)horizLoc;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, (ushort)vertLoc);
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x1);

		L097f:
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xb886)));
			this.processor.StackWord(this.processor.PullLocalWord(0xa));
			this.processor.StackWord(this.processor.PullLocalWord(0x6));

		L0992:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.UnstackWord(),
				(short)this.processor.UnstackWord(),
				this.processor.UnstackWord());

		L099e:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L09a7;
			goto L0a40;

		L09a7:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09c6;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullGlobalWord(0xd4da));
			goto L0a40;

		L09c6:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				16, 16, 2, 0);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				16, 16, 10, 15);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				16, 16, 9, 8);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				16, 16, 11, 0);

		L0a40:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L0a49;
			goto L0ae8;

		L0a49:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x10);
			if (this.processor.FlagSet.IsEqual) goto L0a53;
			this.processor.RegAcc.Value16 = 0;
			goto L0a56;

		L0a53:
			this.processor.RegAcc.Value16 = 0x6;

		L0a56:
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, 0x1);
			goto L0a7e;

		L0a60:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb278)));

		L0a69:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.UnstackWord());

		L0a7b:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0a7e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L0ac0;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			ObtainKnownTileImprovements(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1c), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L0a7b;
			this.processor.EmitLocalWord(0x20, 0xffff);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x11);
			if (this.processor.FlagSet.IsEqual) goto L0a60;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1c), 0x11);
			if (this.processor.FlagSet.IsEqual) goto L0a60;
			this.processor.StackWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xb298)));
			goto L0a69;

		L0ac0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0ae8;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6) + 7,
				this.processor.PullLocalSignedWord(0xa) + 7,
				2, 2,
				this.processor.PullLocalWord(0x20));

		L0ae8:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0b07;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdcfc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b07;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((5 << 1) + 0xd4ce)));

		L0b07:
			ContainsSpecialResource(horizLoc, vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0b30;
			
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0x18) + 0x10) << 1) + 0xd4ce)));

		L0b30:
			ContainsHut(
				this.processor.PullLocalWord(0x18),
				horizLoc, vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0b56;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x1f << 1) + 0xd4ce)));

		L0b56:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L0b6e;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x1e << 1) + 0xd4ce)));

		L0b6e:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0be0;
			this.processor.EmitLocalWord(0xc, 0x1);

		L0b7a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, (ushort)vertLoc);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0bd6;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.HalveAccumulator();
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7eec)));

		L0bd6:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0b7a;

		L0be0:
			ObtainTileOwnerBits(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0bf9;
			goto L0e1b;

		L0bf9:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdcfc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c03;
			goto L0e1b;

		L0c03:
			this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0xc));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx != this.owner.World.PersonParticipantIdx &&
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ShownExtent == 0 && 
				this.processor.PullGlobalWord(0xd806) == 0)
				goto L0e1b;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0x6) + 1,
				this.processor.PullLocalSignedWord(0xa) + 1,
				13, 13, 15);

			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x22) / 28].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0x6) + 2,
				this.processor.PullLocalSignedWord(0xa) + 1,
				12, 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1956)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x22) / 28].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x26),
				12, 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.RegDest.Value16,
				(short)this.processor.RegSource.Value16,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x1c << 1) + 0xd4ce)));

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x22) / 28].ParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x26),
				12, 12, 5,
				(byte)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1956)));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				goto L0d07;
			if (this.processor.PullGlobalWord(0xd806) == 0)
				goto L0d15;

		L0d07:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			goto L0d21;

		L0d15:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ShownExtent;

		L0d21:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xe), 10));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.RegBase.Value16 / 28].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0d6d;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6) + 5,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullGlobalWord(0x6e9e));
			goto L0d92;

		L0d6d:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				((this.processor.PullLocalSignedWord(0xe) < 10) ? 6 : 3) +
					this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa) + 5,
				0);

		L0d92:
			ObtainUnitAtTile(horizLoc, vertLoc);

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0db4;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Unfamiliar[0], 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0dca;

		L0db4:
			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				15, 15, 0);

		L0dca:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].StructureMarkers0, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L0def;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6) + 1,
				this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x1d << 1) + 0xd4ce)));

		L0def:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c96), 0x20);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e1b;
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6c96);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6dac), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6e3e), this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6c96);
			this.processor.EmitGlobalWord(0x6c96, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6c96)));
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdf20), this.processor.RegAcc.Value16);

		L0e1b:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = 0x1;

		L0e23:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderMapTile");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort RenderUnitOnWorldMap(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RenderUnitOnWorldMap({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveSource();
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0eb2;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0e53;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) * 4) + 1,
				(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) * 4) + 1,
				3, 3, 6);
			goto L0e55;

		L0e53:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) * 4) + 1,
				(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) * 4) + 1,
				3, 3, 0);

		L0e55:
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) * 4,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) * 4,
				3, 3,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));
			goto L0fae;

		L0eb2:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Routing.TunePosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) -
					this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x50));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x50);
			if (this.processor.FlagSet.IsLess) goto L0f09;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f09;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L0f09;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0f0e;

		L0f09:
			this.processor.RegAcc.Value16 = 0;
			goto L0fae;

		L0f0e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			ObtainTerrainType(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0f57;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0f57;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0f09;

		L0f57:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0f93;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4) + 1,
				this.processor.PullLocalSignedWord(0x6) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((participantIdx << 5) +
						this.processor.PullLocalWord(0x2) + 0x40) << 1) + 0xd4ce)));

		L0f93:
			RenderUnitSprite(participantIdx, troopIdx, 
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6));

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = 0x1;

		L0fae:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderUnitOnWorldMap");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		// Colors of the hit point squares
		private const ushort VitalityStripBackdrop = 0;
		private const ushort VitalityStripComplete = 10;
		private const ushort VitalityStripBlank = 8;
		private const int VitalityStripCellBreadth = 2;
		private const int VitalityStripCellAltitude = 1;

		/// <summary>
		/// Draws the hit points of a unit as a row of flat rectangles at the top of its 16 x 16 map square:
		/// green for each hit point the unit has, gray for each lost hit point.
		/// </summary>
		/// <param name="xPos">Left side of the map square on the screen</param>
		/// <param name="yPos">Top side of the map square on the screen</param>
		public void RenderHealthBar(int horizLoc, int vertLoc, int completeStrikeMarks, int strikeMarks)
		{
			ushort display = this.processor.PullGlobalWord(0xaa);

			// The unit image covers the map square without its left column and top row,
			// the bar stays inside the unit image so it is erased together with the unit
			int breadth = Math.Min(completeStrikeMarks * (VitalityStripCellBreadth + 1) + 1, 15);
			int remaining = horizLoc + 1 + (15 - breadth) / 2;
			int head = vertLoc + 1;

			this.owner.Services.FloodBox(display, remaining, head, breadth, VitalityStripCellAltitude + 2, VitalityStripBackdrop);

			for (int idx = 0; idx < completeStrikeMarks; idx++)
			{
				this.owner.Services.FloodBox(display, remaining + 1 + idx * (VitalityStripCellBreadth + 1), head + 1,
					VitalityStripCellBreadth, VitalityStripCellAltitude, idx < strikeMarks ? VitalityStripComplete : VitalityStripBlank);
			}
		}

		public void RenderUnitSprite(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderUnitSprite({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveSource();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)) +
						(participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L1005;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x1d << 1) + 0xd4ce)));
			goto L1045;

		L1005:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x4);
			if (this.processor.FlagSet.IsEqual) goto L1045;
			this.processor.CompareWord((ushort)participantIdx, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1027;
			this.processor.RegAcc.Value16 = 0x9;
			goto L102a;

		L1027:
			this.processor.RegAcc.Value16 = 0xf;

		L102a:
			this.owner.TextPainter.RenderTextWithShadow(0x2ba0,	horizLoc + 4, vertLoc + 7, this.processor.RegAcc.Value16);

		L1045:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L108d;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L108d;
			this.processor.CompareWord((ushort)participantIdx, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L106f;
			this.processor.RegAcc.Value16 = 0x9;
			goto L1072;

		L106f:
			this.processor.RegAcc.Value16 = 0xf;

		L1072:
			this.owner.TextPainter.RenderTextWithShadow(0x2ba2, horizLoc + 4, vertLoc + 7, this.processor.RegAcc.Value16);

		L108d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsNotEqual) goto L10a7;
			goto L117a;

		L10a7:
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L10b9;
			goto L117a;

		L10b9:
			this.processor.EmitLocalWord(0x2, 0x52);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L10d7;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10d1;
			this.processor.RegAcc.Value16 = 0x3f;
			goto L10d4;

		L10d1:
			this.processor.RegAcc.Value16 = 0x49;

		L10d4:
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

		L10d7:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4));
			this.processor.EmitLocalByte(0x4, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x80);
			if (this.processor.FlagSet.IsEqual) goto L1120;
			this.processor.EmitLocalWord(0x2, 0x4d);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L1104;
			this.processor.EmitLocalWord(0x2, 0x46);

		L1104:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1120;
			this.processor.EmitLocalWord(0x2, 0x50);

		L1120:
			this.owner.Libc.CopyText(0xba06, " ");

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.EmitGlobalByte(0xba06, this.processor.RegAcc.LowByte);
			this.processor.CompareWord((ushort)participantIdx, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1141;
			this.processor.RegAcc.Value16 = 0x9;
			goto L1144;

		L1141:
			this.processor.RegAcc.Value16 = 0xf;

		L1144:
			this.owner.TextPainter.RenderTextWithShadow(0xba06, horizLoc + 4, vertLoc + 7, this.processor.RegAcc.Value16);

			this.owner.Dialogs.RenderBox(horizLoc - 1, vertLoc - 1, 15, 15, 7);

		L117a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L11cf;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, 16, 16, 5, 7);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, 16, 16, 8, 7);

		L11cf:
			{
				// Every unit shows its hit points: green squares, and gray squares for lost hit points until it heals
				int troopKind = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x600 * participantIdx + 0xc * troopIdx + 0x81d7));
				int missingStrikeMarks = this.owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind);

				if (troopKind != 0xff)
				{
					bool seasoned = (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x600 * participantIdx + 0xc * troopIdx + 0x81d4)) & (int)UnitStatusEnum.Veteran) != 0;
					int completeStrikeMarks = this.owner.World.Vitals.ObtainFullHitPoints(participantIdx, troopIdx, troopKind, seasoned);

					RenderHealthBar(horizLoc - 1, vertLoc - 1, completeStrikeMarks, completeStrikeMarks - missingStrikeMarks);
				}
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderUnitSprite");
		}

		/// <summary>
		/// Draws the tile again and, unless units are hidden (0xdcfc), the unit on it: when the human sees it (all units are
		/// shown with 0xd806, the human's own, or the unit's bit of the human), and the tile has no city
		/// </summary>
		public void RepaintMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintMapTile({horizLoc}, {vertLoc})");

			RenderMapTile(horizLoc, vertLoc);

			if (this.processor.PullGlobalWord(0xdcfc) == 0)
			{
				ushort troopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

				if (troopIdx != 0xffff)
				{
					short ownerIdx = this.processor.PullGlobalSignedWord(0xd20a);
					short personIdx = this.owner.World.PersonParticipantIdx;

					bool seen = this.processor.PullGlobalWord(0xd806) != 0 || ownerIdx == personIdx ||
						(this.troops.Pull(ownerIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << personIdx)) != 0;

					if (seen && (ObtainTileImprovements(horizLoc, vertLoc) & 0x1) == 0)
					{
						RepaintUnitTile(this.processor.PullGlobalSignedWord(0xd20a), (short)troopIdx);
					}
				}
			}

			this.processor.Journal.CloseScope("RepaintMapTile");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void RepaintUnitTile(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitTile({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveSource();
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1308;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1308;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1308;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

		L12ac:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L12ed;

			RenderUnitOnWorldMap(participantIdx,
				this.processor.PullLocalSignedWord(0x2));

			this.processor.EmitLocalWord(0x2, 0xffff);

		L12ed:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L12fb;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L12ac;

		L12fb:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1321;

			RenderUnitOnWorldMap(participantIdx, troopIdx);

			goto L1321;

		L1308:
			RenderUnitOnWorldMap(participantIdx, (short)this.owner.UnitOps.LocateBestGuardianInStack(participantIdx, troopIdx));

		L1321:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RepaintUnitTile");
		}

		/// <summary>
		/// Is the position on the map
		/// </summary>
		/// <returns>1 on the map, 0 outside</returns>
		public ushort VerifyMapLimits(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyMapLimits({horizLoc}, {vertLoc})");

			ushort onMap = ReturnAcc(MapRules.IsOnMap(horizLoc, vertLoc) ? 1 : 0);

			this.processor.Journal.CloseScope("VerifyMapLimits");

			return onMap;
		}

		/// <summary>
		/// Terrain type of the tile (MapRules.OceanTerrain is the ocean)
		/// </summary>
		public ushort ObtainTerrainType(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTerrainType({horizLoc}, {vertLoc})");

			ushort terrain = ReturnAcc(this.chart.Terrain(horizLoc, vertLoc));

			this.processor.Journal.CloseScope("ObtainTerrainType");

			return terrain;
		}

		/// <summary>
		/// The player of the tile owner layer (bits 0 - 2), also when no units stand there
		/// </summary>
		public ushort ObtainTileOwnerBits(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc) & 0x7);
		}

		/// <summary>
		/// Sets the player of the tile owner layer (of a city), keeps the units bit
		/// </summary>
		public void MapAssignCityOwner(short participantIdx, int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & MapLayers.UnitsPresent) + participantIdx);
		}

		/// <summary>
		/// Puts the unit on the tile: into the stack of the units standing there, and marks the tile as having units of the player
		/// </summary>
		public void SituateUnitOnTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"SituateUnitOnTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			ushort stackTroopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

			if (stackTroopIdx != 0xffff)
			{
				this.owner.Combat.AttachUnitToStack(participantIdx, troopIdx, stackTroopIdx);
			}

			AssignChartDot(horizLoc + 160, vertLoc, (ushort)(participantIdx + MapLayers.UnitsPresent));

			this.processor.Journal.CloseScope("SituateUnitOnTile");
		}

		/// <summary>
		/// Writes a value of a map layer through SystemServices.AssignDot (bounds of the current screen), as the original
		/// </summary>
		private void AssignChartDot(int horizLoc, int vertLoc, ushort style)
		{
			this.owner.Services.AssignDot(2, horizLoc, vertLoc, style);
		}

		/// <summary>
		/// Takes the unit off the tile: out of its stack, or when it stood alone, the tile has no units any more
		/// </summary>
		public void DiscardUnitFromTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"DiscardUnitFromTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.owner.Combat.DiscardUnitFromStack(participantIdx, troopIdx);
			}
			else
			{
				AssignChartDot(horizLoc + 160, vertLoc, (byte)participantIdx);
			}

			this.processor.Journal.CloseScope("DiscardUnitFromTile");
		}

		/// <summary>
		/// The first unit standing on the tile. Sets the player of the unit into 0xd7f0 and 0xd20a.
		/// When the tile owner layer says that units are there but none is found, the layer is cleared.
		/// </summary>
		/// <returns>Unit index, or 0xffff when no unit stands on the tile</returns>
		public ushort ObtainUnitAtTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainUnitAtTile({horizLoc}, {vertLoc})");

			ushort found = 0xffff;
			ushort participantIdx = ObtainTileOwner(horizLoc, vertLoc);

			if (participantIdx != 0xffff)
			{
				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (this.troops.Exists(participantIdx, troopIdx) && this.troops.StandsAt(participantIdx, troopIdx, horizLoc, vertLoc))
					{
						found = (ushort)troopIdx;
						break;
					}
				}

				if (found != 0xffff)
				{
					this.processor.EmitGlobalWord(0xd7f0, participantIdx);
					this.processor.EmitGlobalWord(0xd20a, participantIdx);
				}
				else
				{
					this.owner.Services.AssignDot(2, horizLoc + 160, vertLoc, 0);
				}
			}

			ReturnAcc(found);

			this.processor.Journal.CloseScope("ObtainUnitAtTile");

			return found;
		}

		/// <summary>
		/// The player whose units stand on the tile
		/// </summary>
		/// <returns>Player, or 0xffff when no units stand on the tile</returns>
		public ushort ObtainTileOwner(int horizLoc, int vertLoc)
		{
			int bits = this.chart.OwnerBits(horizLoc, vertLoc);

			return ReturnAcc((bits & MapLayers.UnitsPresent) != 0 ? bits & 0x7 : 0xffff);
		}

		/// <summary>
		/// Marks that units stand on the tile, keeps the player
		/// </summary>
		public void TagTileOwned(int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & 0x7) + MapLayers.UnitsPresent);
		}

		/// <summary>
		/// The road bit (8) of the tile improvements
		/// </summary>
		public ushort ContainsRoad(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) & 0x8);
		}

		/// <summary>
		/// Improvements of the tile (bit 0 city, 1 irrigation, 2 mine, 3 road, ...)
		/// </summary>
		public ushort ObtainTileImprovements(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc));
		}

		/// <summary>
		/// Improvements of the tile as the human player saw them last time
		/// </summary>
		public ushort ObtainKnownTileImprovements(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainKnownTileImprovements({horizLoc}, {vertLoc})");

			ushort known = ReturnAcc(this.chart.KnownImprovements(horizLoc, vertLoc));

			this.processor.Journal.CloseScope("ObtainKnownTileImprovements");

			return known;
		}

		/// <summary>
		/// The human player sees the improvements of the tile as they are now
		/// </summary>
		public void KeepKnownTileImprovements(int horizLoc, int vertLoc)
		{
			this.chart.RevealImprovements(horizLoc, vertLoc);

			ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) >> 4);
		}

		/// <summary>
		/// Adds improvements to the tile: 0 removes all, a value from 0x10 sets the high bits, a smaller one the low bits.
		/// In the turn of the human player the human sees the change at once.
		/// </summary>
		public void AssignTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignTileImprovements({improvements}, {horizLoc}, {vertLoc})");

			if (improvements == 0)
			{
				AssignChartDot(horizLoc, vertLoc + 100, 0);
				AssignChartDot(horizLoc, vertLoc + 150, 0);
			}
			else if (improvements >= 0x10)
			{
				int high = ReturnAcc(this.chart.HighImprovements(horizLoc, vertLoc));
				AssignChartDot(horizLoc, vertLoc + 150, (ushort)((improvements >> 4) | high));
			}
			else
			{
				int low = ReturnAcc(this.chart.LowImprovements(horizLoc, vertLoc));
				AssignChartDot(horizLoc, vertLoc + 100, (ushort)(low | improvements));
			}

			if (this.processor.PullGlobalSignedWord(0x6b90) == this.owner.World.PersonParticipantIdx)
			{
				KeepKnownTileImprovements(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("AssignTileImprovements");
		}

		/// <summary>
		/// Removes the improvement bits from the tile
		/// </summary>
		public void DiscardTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			this.chart.ClearImprovements(improvements, horizLoc, vertLoc);
		}

		/// <summary>
		/// The city on the tile. Sets the player of the tile owner layer into 0xd7f0 and 0xd20a.
		/// When there is no city, an error message is shown.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateCityAtTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateCityAtTile({horizLoc}, {vertLoc})");

			ushort participantIdx = ObtainTileOwnerBits(horizLoc, vertLoc);
			ushort found = 0xffff;

			for (int townIdx = 0; townIdx < this.owner.World.Towns.Length; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.Place.X == horizLoc && town.Place.Y == vertLoc)
				{
					this.processor.EmitGlobalWord(0xd7f0, participantIdx);
					this.processor.EmitGlobalWord(0xd20a, participantIdx);
					found = (ushort)townIdx;
					break;
				}
			}

			if (found == 0xffff)
			{
				this.owner.TurnHooks.DisplayDialog(0x2bc6, 100, 80);
			}

			ReturnAcc(found);

			this.processor.Journal.CloseScope("LocateCityAtTile");

			return found;
		}

		/// <summary>
		/// Does the tile have a special resource (MapRules.HasSpecialResource with the world seed)
		/// </summary>
		public ushort ContainsSpecialResource(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsSpecialResource({horizLoc}, {vertLoc})");

			ushort special = ReturnAcc(MapRules.HasSpecialResource(horizLoc, vertLoc, this.owner.World.ChanceRoot) ? 1 : 0);

			this.processor.Journal.CloseScope("ContainsSpecialResource");

			return special;
		}

		/// <summary>
		/// Is there a hut (tribal village) on the tile: not on the ocean, on a tile nobody has visited and without a city
		/// </summary>
		/// <param name="terrain">Terrain type of the tile</param>
		public ushort ContainsHut(ushort terrain, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsHut({terrain}, {horizLoc}, {vertLoc})");

			bool hut = terrain != MapRules.OceanTerrain &&
				(this.owner.World.ChartSight[horizLoc, vertLoc] & 1) == 0 &&
				MapRules.IsHutPlace(horizLoc, vertLoc, this.owner.World.ChanceRoot) &&
				(ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			ushort result = ReturnAcc(hut ? 1 : 0);

			this.processor.Journal.CloseScope("ContainsHut");

			return result;
		}

		/// <summary>
		/// Number of the continent or the sea of the tile
		/// </summary>
		public ushort ObtainContinentID(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Continent(horizLoc, vertLoc));
		}

		/// <summary>
		/// Size of the continent (land) or the sea (ocean) of the tile, from the word arrays at 0xdcfe and 0xdd7e
		/// </summary>
		public ushort ObtainLandMassSize(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainLandMassSize({horizLoc}, {vertLoc})");

			ushort sizes = ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ? (ushort)0xdcfe : (ushort)0xdd7e;
			int continent = ObtainContinentID(horizLoc, vertLoc);
			ushort size = ReturnAcc(this.processor.PullGlobalWord((ushort)(sizes + (continent << 1))));

			this.processor.Journal.CloseScope("ObtainLandMassSize");

			return size;
		}
	}
}
