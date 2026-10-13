using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1403.
	/// </summary>
	public class PlayerTurn
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public PlayerTurn(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void HandlePlayerTurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePlayerTurn({participantIdx})");

			// function body
			this.processor.EnterFrame(0x52);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.EmitGlobalWord(0x6b90, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.ParticipantMarkers;
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.EmitLocalWord(0x4, 0x0);

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L0084;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.owner.World.ParticipantMarkers = (short)this.processor.RegAcc.Value16;
			if (this.owner.World.RoundTally != 20) goto L004e;
			goto L0058;

		L004e:
			if (this.owner.World.RoundTally == 60) goto L0058;
			goto L0064;

		L0058:
			this.owner.HelpSystem.DisplayHelpPopup(0x1e60);

		L0064:
			if (this.owner.World.RoundTally != 40) goto L006e;
			goto L0078;

		L006e:
			if (this.owner.World.RoundTally == 80) goto L0078;
			goto L0084;

		L0078:
			this.owner.HelpSystem.DisplayHelpPopup(0x1e67);

		L0084:
			this.processor.EmitLocalWord(0x32, 0x0);
			goto L008f;

		L008c:
			this.processor.EmitLocalWord(0x32, this.processor.IncrementWord(this.processor.PullLocalWord(0x32)));

		L008f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0099;
			goto L0128;

		L0099:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L00b3;
			goto L0125;

		L00b3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdf60), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L00bd;
			goto L0125;

		L00bd:
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L00e2;
			goto L0125;

		L00e2:
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 4);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L00f9;
			goto L0110;

		L00f9:
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 8);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0110;
			goto L0125;

		L0110:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 
				this.processor.SumByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3));

		L0125:
			goto L008c;

		L0128:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x46, 0x7f);

		L013c:
			this.processor.EmitLocalWord(0x1e, 0x1);
			this.processor.EmitLocalWord(0x32, 0x0);
			goto L014c;

		L0149:
			this.processor.EmitLocalWord(0x32, this.processor.IncrementWord(this.processor.PullLocalWord(0x32)));

		L014c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0156;
			goto L3e4e;

		L0156:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1ae0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0160;
			goto L0165;

		L0160:
			this.owner.TurnHooks.RepaintMainScreen();

		L0165:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L016f;
			goto L0177;

		L016f:
			this.processor.EmitLocalWord(0x32, 0x80);
			goto L0149;

		L0177:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0182;
			goto L0187;

		L0182:
			this.processor.EmitLocalWord(0x2e, 0x1);

		L0187:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L01a1;
			goto L01a4;

		L01a1:
			goto L0149;

		L01a4:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d7);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01c0;
			goto L01c3;

		L01c0:
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, 0x1);

		L01c3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L01cc;
			goto L01fc;

		L01cc:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			this.owner.UnitOps.ShiftUnit(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				0);

		L01fc:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L0216;
			goto L0219;

		L0216:
			goto L0149;

		L0219:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0233;
			goto L0236;

		L0233:
			goto L0149;

		L0236:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0250;
			goto L0356;

		L0250:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfb));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x8));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L02d7;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0295;
			goto L02d7;

		L0295:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x3a));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x3a)].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02d0;
			goto L02d7;

		L02d0:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df), this.processor.RegAcc.LowByte);

		L02d7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0304;
			goto L0353;

		L0304:
			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsEqual) goto L032a;
			goto L0353;

		L032a:
			this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0348;
			goto L0353;

		L0348:
			this.owner.UnitOps.KeepUnitsInCityList(this.processor.PullLocalSignedWord(0x3a));

		L0353:
			goto L0149;

		L0356:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L043b;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsEqual) goto L037b;
			goto L043b;

		L037b:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd4cc), this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsBelow) goto L038c;
			goto L03bb;

		L038c:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd75e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsAbove) goto L03a0;
			goto L03bb;

		L03a0:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4cc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xe);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsAboveOrEqual) goto L03ad;
			goto L03bb;

		L03ad:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd75e);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4e));
			if (this.processor.FlagSet.IsBelowOrEqual) goto L03bb;
			goto L043b;

		L03bb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L03c4;
			goto L03cf;

		L03c4:
			this.processor.EmitLocalWord(0x1e, 0x0);
			goto L0149;

		L03cf:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L03d9;
			goto L03e5;

		L03d9:
			this.owner.TextPainter.AwaitTime(30);

		L03e5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) - 7,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)) - 6);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x32);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7f);
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x0);
			if (this.owner.World.RoundTally == 0) goto L042d;
			goto L043b;

		L042d:
			RenderUnitInfo(participantIdx,
				this.processor.PullLocalWord(0x32));

		L043b:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L044c;
			goto L0451;

		L044c:
			AwaitForKey();

		L0451:
			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L045f;
			goto L0473;

		L045f:
			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

		L0473:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x36, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);

		L047f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0490;
			goto L04ed;

		L0490:
			this.owner.Bot.ShiftAIUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04aa;
			goto L04bf;

		L04aa:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L04bf:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x4);
			if (this.processor.FlagSet.IsGreater) goto L04cb;
			goto L04ea;

		L04cb:
			this.processor.EmitLocalWord(0x44, 0x20);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);

		L04ea:
			goto L0d0a;

		L04ed:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L04f7;
			goto L05cc;

		L04f7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsEqual) goto L0511;
			goto L051b;

		L0511:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L051b;
			goto L05cc;

		L051b:
			this.processor.EmitLocalWord(0x44, 0x72);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L053a;
			goto L0550;

		L053a:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0544;
			goto L054a;

		L0544:
			this.processor.RegAcc.Value16 = 0x6d;
			goto L054d;

		L054a:
			this.processor.RegAcc.Value16 = 0x69;

		L054d:
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);

		L0550:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4));
			this.processor.EmitLocalByte(0x4e, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4e), 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L0570;
			goto L05a2;

		L0570:
			this.processor.EmitLocalWord(0x44, 0x6d);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4e), 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L057e;
			goto L0583;

		L057e:
			this.processor.EmitLocalWord(0x44, 0x66);

		L0583:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L059d;
			goto L05a2;

		L059d:
			this.processor.EmitLocalWord(0x44, 0x70);

		L05a2:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L05c4;
			goto L05c9;

		L05c4:
			this.processor.EmitLocalWord(0x44, 0x68);

		L05c9:
			goto L0d00;

		L05cc:
			RenderUnitInfo(participantIdx,
				this.processor.PullLocalWord(0x32));
			
			if (this.owner.World.RoundTally == 0) goto L05e4;
			goto L0613;

		L05e4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05ed;
			goto L0613;

		L05ed:
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.HelpSystem.DisplayHelpPopup(0x1e6e);
			
			this.processor.EmitLocalWord(0x8, 0x1);

		L0613:
			if (this.processor.PullLocalWord(0x32) < 128)
			{
				this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
				this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
				this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
				if (this.processor.FlagSet.IsEqual) goto L062d;
			}
			goto L0649;

		L062d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0636;
			goto L0649;

		L0636:
			this.owner.HelpSystem.DisplayTileInfo(participantIdx, this.processor.PullLocalWord(0x32));
			
			this.processor.EmitLocalWord(0x8, 0x1);

		L0649:
			this.owner.UnitOps.ChoreographUnitMove(participantIdx, 
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12));

			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L0667;
			goto L066c;

		L0667:
			this.processor.EmitLocalWord(0x6, 0x1);

		L066c:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0676;
			goto L06b4;

		L0676:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0xd);
			if (this.processor.FlagSet.IsEqual) goto L0687;
			goto L06b1;

		L0687:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0690;
			goto L06b1;

		L0690:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x12, 0x26);
			this.owner.GlobDb3c = 0x50;
			this.owner.GlobDb3e = 0x8;
			this.owner.GlobDb3a = 0x1;
			goto L09cf;

		L06b1:
			goto L0d00;

		L06b4:
			this.owner.Routing.TunePosition(
				(((short)this.owner.GlobDb3c - 80) / 16) + this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.owner.GlobDb3e, 0x8);
			if (this.processor.FlagSet.IsLess) goto L06fd;
			goto L073a;

		L06fd:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), -1);

			AwaitForKey();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd4ca), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L071e;
			goto L0737;

		L071e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4ca);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L072e;
			goto L0734;

		L072e:
			goto L0d0a;

		L0734:
			goto L3365;

		L0737:
			goto L3c40;

		L073a:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x50);
			if (this.processor.FlagSet.IsLess) goto L0744;
			goto L0846;

		L0744:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x3a);
			if (this.processor.FlagSet.IsLess) goto L074e;
			goto L078c;

		L074e:
			this.owner.Routing.TunePosition(
				(short)this.owner.GlobDb3c + this.processor.PullGlobalSignedWord(0x6ed6) - 7);

			this.owner.MapOps.FocusMapView(participantIdx,
				(short)this.processor.RegAcc.Value16,
				this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3e + this.processor.PullGlobalSignedWord(0x70ea) - 14, 0, 49));

			goto L0840;

		L078c:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x48);
			if (this.processor.FlagSet.IsLess) goto L0796;
			goto L0833;

		L0796:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Throne.ImportPalaceGraphics(0);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e79, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Helpers.LaunchHueCycling();

			goto L0840;

		L0833:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsEqual) goto L083d;
			goto L0840;

		L083d:
			goto L3ea1;

		L0840:
			this.owner.GlobDb3a = 0x0;

		L0846:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0850;
			goto L096c;

		L0850:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L085a;
			goto L096c;

		L085a:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0869;
			goto L085a;

		L0869:
			this.owner.Routing.TunePosition(
				((short)this.owner.GlobDb3c - 80) / 16 + this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			this.owner.Helpers.MergeCoordinates(
				(short)(this.processor.PullLocalSignedWord(0x1c) -
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5))),
				(short)(this.processor.PullLocalSignedWord(0x26) -
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6))));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f);
			if (this.processor.FlagSet.IsEqual) goto L08f7;
			goto L0918;

		L08f7:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0913;
			goto L0918;

		L0913:
			this.processor.EmitLocalWord(0x18, 0x1);

		L0918:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0921;
			goto L0966;

		L0921:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), 0xff);
			this.owner.GlobDb3a = 0x0;
			this.processor.EmitLocalWord(0x36, 0x0);

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

			goto L096c;

		L0966:
			this.owner.GlobDb3a = 0x2;

		L096c:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0976;
			goto L09cf;

		L0976:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L099b;
			goto L09a5;

		L099b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L09a5;
			goto L09cf;

		L09a5:
			this.owner.Startup.ConcealMouseCursor();
			
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.Encyclopedia.DisplayCivilopediaRecord(this.processor.RegAcc.Value16, 3);
			
			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Startup.DisplayMouseCursor();

		L09cf:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L09d9;
			goto L0ce5;

		L09d9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L09e2;
			goto L0b3c;

		L09e2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L09ec;
			goto L0b3c;

		L09ec:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0a06;
			goto L0ab8;

		L0a06:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0a2b;
			goto L0adb;

		L0a2b:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0a4d;
			goto L0a56;

		L0a4d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0a56;
			goto L0ab8;

		L0a56:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0a78;
			goto L0a81;

		L0a78:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0a81;
			goto L0ab8;

		L0a81:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0a96;
			goto L0ab8;

		L0a96:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0ab8;
			goto L0adb;

		L0ab8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), 0xff);

		L0adb:
			this.processor.EmitLocalWord(0x36, 0x0);

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0afd;
			goto L0b39;

		L0afd:
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0b26;
			goto L0b34;

		L0b26:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L0b34:
			this.processor.EmitLocalWord(0xe, 0xffff);

		L0b39:
			goto L0ce5;

		L0b3c:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0b51;
			goto L0bf4;

		L0b51:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L0b68;
			goto L0b72;

		L0b68:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b72;
			goto L0bf4;

		L0b72:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);
			
			this.owner.Advisors.RenewCityScreen(this.processor.PullLocalSignedWord(0x3c));
			
			this.owner.TurnHooks.RenderStatusBar();

			this.processor.EmitLocalWord(0x1e, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0bb5;
			goto L0bf1;

		L0bb5:
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0bde;
			goto L0bec;

		L0bde:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L0bec:
			this.processor.EmitLocalWord(0xe, 0xffff);

		L0bf1:
			goto L0ce5;

		L0bf4:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c0f;
			goto L0c27;

		L0c0f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0c18;
			goto L0c27;

		L0c18:
			this.owner.ArmyPicker.DisplayUnitInfo(
				this.processor.PullGlobalSignedWord(0xd7f0),
				this.processor.PullLocalSignedWord(0x44));

		L0c27:
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0xffff);
			if (this.processor.FlagSet.IsEqual)
				goto L0ccc;

			if (participantIdx != this.processor.PullGlobalSignedWord(0xd7f0))
				goto L0ccc;

			this.owner.UnitOps.DisplayBattleScreen(this.processor.PullGlobalSignedWord(0xd7f0),
				this.processor.PullLocalSignedWord(0x44));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x3a));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsEqual) goto L0c67;
			goto L0cc9;

		L0c67:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7f);
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x0);
			this.processor.EmitLocalWord(0x1e, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c8a;
			goto L0cc6;

		L0c8a:
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0cb3;
			goto L0cc1;

		L0cb3:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L0cc1:
			this.processor.EmitLocalWord(0xe, 0xffff);

		L0cc6:
			goto L3c40;

		L0cc9:
			goto L0ce5;

		L0ccc:
			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullLocalSignedWord(0x1c) - 7,
				this.processor.PullLocalSignedWord(0x26) - 6);

		L0ce5:
			this.processor.EmitLocalWord(0x44, 0xffff);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0cf3;
			goto L0cfb;

		L0cf3:
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0d00;

		L0cfb:
			AwaitForKey();

		L0d00:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0d0a;
			goto L1b5e;

		L0d0a:
			if (this.processor.PullLocalWord(0x32) > 127)
				goto L0d24;

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0d24;

			goto L0d38;

		L0d24:
			this.owner.Routing.CarryOutGoto(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			goto L0d4d;

		L0d38:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), 0xff);

		L0d4d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			if (this.processor.PullLocalWord(0x32) < 128)
			{
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
				this.processor.RegAcc.HighByte = 0;
				this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);
				this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
				this.processor.RegAcc.HighByte = 0;
				this.processor.EmitLocalWord(0x30, this.processor.RegAcc.Value16);
			}
			else
			{
				this.processor.EmitLocalWord(0x2c, 0);
				this.processor.EmitLocalWord(0x30, 0);
			}

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			goto L1aed;

		L0d75:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			goto L1b5e;

		L0d9b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0db0;
			goto L0dd9;

		L0db0:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x4e, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.PullLocalByte(0x4e), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L0dd0;
			goto L0dd9;

		L0dd0:
			this.processor.CompareByte(this.processor.PullLocalByte(0x4e), 0xe);
			if (this.processor.FlagSet.IsEqual) goto L0dd9;
			goto L0deb;

		L0dd9:
			DisplayHelpForHuman(0x1e83);

			goto L1b5e;

		L0deb:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0e16;
			goto L0f1c;

		L0e16:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			
			this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0e32;
			goto L0f1c;

		L0e32:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L0e4c;
			goto L0e61;

		L0e4c:
			this.owner.MapOps.DiscardTileImprovements(6,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L0e91;

		L0e61:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L0e6a;
			goto L0e7f;

		L0e6a:
			this.owner.MapOps.DiscardTileImprovements(0x10,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L0e91;

		L0e7f:
			this.owner.MapOps.DiscardTileImprovements(8,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L0e91:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				goto L0ea7;
			goto L0eb5;

		L0ea7:
			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L0eb5:
			RepaintTileIfVisible(this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30));

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x10));

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx,
				2);

			if (participantIdx == this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx)
				goto L0f07;

			this.owner.Bot.SendUnitToStrategicLocation((short)this.processor.RegDest.Value16,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				1, 4);

		L0f07:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L0f1c:
			goto L1b5e;

		L0f1f:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			goto L1b5e;

		L0f37:
			this.processor.EmitLocalWord(0x28, 0x1);
			this.processor.EmitLocalWord(0x2e, 0x1);
			goto L1b5e;

		L0f44:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x1));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			goto L1b5e;

		L0f6f:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x4e, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x4e));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f97;
			goto L0fa0;

		L0f97:
			this.processor.CompareByte(this.processor.PullLocalByte(0x4e), 0x17);
			if (this.processor.FlagSet.IsEqual) goto L0fa0;
			goto L0ffa;

		L0fa0:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0fba;
			goto L0ffa;

		L0fba:
			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 0x14f6);

			this.processor.EmitLocalWord(0x1e, 0x0);
			this.processor.EmitLocalWord(0x28, 0x1);

			LocateBestGuardian(participantIdx, this.processor.PullLocalWord(0x32));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x32);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7f);
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x0);

		L0ffa:
			goto L1b5e;

		L0ffd:
			// GoTo. The original game showed the square cursor (moved with the arrow keys, Enter picks the destination)
			// only when there was no mouse, with a mouse it only changed the mouse pointer, which was easy to miss.
			// The square cursor is now shown in both cases, so the destination can be picked with the keyboard or the mouse.
			if (this.processor.PullLocalWord(0x36) == 0)
			{
				if (this.processor.PullLocalWord(0x4) == 0)
				{
					this.processor.EmitLocalWord(0x4, 0x1);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * 0x600 + this.processor.PullLocalWord(0x32) * 0xc);
					this.processor.EmitLocalWord(0xe, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));
					this.processor.EmitLocalWord(0x12, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
				}
			}
			else if (this.processor.PullLocalWord(0x4) != 0)
			{
				// GoTo cancelled, remove the square cursor
				this.processor.EmitLocalWord(0x4, 0x0);

				if ((this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
					this.processor.PullLocalWord(0x12)] & (1 << participantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						this.processor.PullLocalSignedWord(0xe),
						this.processor.PullLocalSignedWord(0x12));
				}

				this.processor.EmitLocalWord(0xe, 0xffff);
			}

			if (this.processor.PullGlobalWord(0x1a3c) != 0)
			{
				// GoTo mouse pointer, or the normal one when GoTo is cancelled
				this.processor.RegBase.Value16 = (ushort)((this.processor.PullLocalWord(0x36) != 0) ? 7 : 2);
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ce)));
			}

			this.processor.EmitLocalWord(0x36, this.processor.BitXorWord(this.processor.PullLocalWord(0x36), 0x1));
			goto L1b5e;

		L10bb:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6c9a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10d6;
			goto L10f0;

		L10d6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x10);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df), this.processor.RegAcc.LowByte);
			goto L117d;

		L10f0:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1112;
			goto L117d;

		L1112:
			this.owner.UnitOps.LocateNearestFriendlyCity(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L112c;
			goto L1137;

		L112c:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x2));
			goto L1c10;

		L1137:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L115a;

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			goto L117d;

		L115a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

		L117d:
			goto L1b5e;

		L1180:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L119a;
			goto L11a9;

		L119a:
			DisplayHelpForHuman(0x1e8c);

			goto L1b5e;

		L11a9:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L11c3;
			goto L11f1;

		L11c3:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x7d));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);

			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L1b5e;

		L11f1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x82));

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9)), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1231;
			goto L1234;

		L1231:
			goto L1b5e;

		L1234:
			this.owner.MapOps.DiscardTileImprovements(0x40,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));
			
			owner.World.DirtyCellTally--;

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x7d));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);
			goto L1b5e;

		L1283:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L12a5;
			goto L13fd;

		L12a5:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L12af;
			goto L130e;

		L12af:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x4));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L130b;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L12e7;
			goto L130b;

		L12e7:
			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df), this.processor.RegCount.LowByte);

		L130b:
			goto L13fd;

		L130e:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x21);
			if (this.processor.FlagSet.IsEqual) goto L1323;
			goto L133a;

		L1323:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L133a;

			goto L1368;

		L133a:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x3f));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);

			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L1b5e;

		L1368:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc0));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L13b2;
			goto L13b5;

		L13b2:
			goto L1b5e;

		L13b5:
			this.owner.MapOps.AssignTileImprovements(0x20,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));
			
			this.owner.UnitOps.ShiftUnit(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				1);
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x3f));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);

		L13fd:
			goto L1b5e;

		L1400:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L141a;
			goto L1429;

		L141a:
			DisplayHelpForHuman(0x1e96);

			goto L1b5e;

		L1429:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L1443;
			goto L1463;

		L1443:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L145a;

			goto L146c;

		L145a:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1463;
			goto L146c;

		L1463:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L146c;
			goto L149a;

		L146c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);

			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L1b5e;

		L149a:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xb);
			if (this.processor.FlagSet.IsEqual) goto L14b4;
			goto L14ce;

		L14b4:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L14cb;

			goto L14ce;

		L14cb:
			goto L1b5e;

		L14ce:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x2));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L14ff;
			goto L1505;

		L14ff:
			this.processor.RegAcc.Value16 = 0x4;
			goto L1508;

		L1505:
			this.processor.RegAcc.Value16 = 0x2;

		L1508:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L152f;
			goto L1532;

		L152f:
			goto L1b5e;

		L1532:
			this.owner.MapOps.AssignTileImprovements(
				(ushort)(((this.processor.PullLocalByte(0x40) & 8) != 0) ? 0x10 : 8),
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));
			
			this.owner.UnitOps.ShiftUnit(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				1);
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);
			goto L1b5e;

		L158c:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L15a6;
			goto L15c5;

		L15a6:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L15b0;
			goto L15c5;

		L15b0:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L15c5;
			goto L162c;

		L15c5:
			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L15f0;
			goto L15ff;

		L15f0:
			DisplayHelpForHuman(0x1ea0);

			goto L1614;

		L15ff:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9), 0x0);

		L1614:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xbf));
			goto L1b5e;

		L162c:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a));
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1655;
			goto L168a;

		L1655:
			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			DisplayHelpForHuman(0x1eaa);

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xbf));
			goto L1b5e;

		L168a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xfffe);
			if (this.processor.FlagSet.IsEqual) goto L1693;
			goto L16de;

		L1693:
			VerifyCoastalTile(this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L16a9;
			goto L16de;

		L16a9:
			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			DisplayHelpForHuman(0x1eb1);

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xbf));
			goto L1b5e;

		L16de:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x40));
			
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x3e));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44c)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L172a;
			goto L172d;

		L172a:
			goto L1b5e;

		L172d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1736;
			goto L1775;

		L1736:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalWord(0x3a));

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30)] |= 1;

			this.owner.MapOps.DiscardTileImprovements(6,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L1799;

		L1775:
			this.owner.MapOps.DiscardTileImprovements(4,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.AssignTileImprovements(2,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L1799:
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xbf));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);
			goto L1b5e;

		L17c4:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L17de;
			goto L17f3;

		L17de:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L17f3;
			goto L185a;

		L17f3:
			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L181e;
			goto L182d;

		L181e:
			DisplayHelpForHuman(0x1eba);

			goto L1842;

		L182d:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9), 0x0);

		L1842:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x7f));
			goto L1b5e;

		L185a:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e));
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1883;
			goto L18b8;

		L1883:
			this.owner.UnitOps.PanMapToPosition(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			DisplayHelpForHuman(0x1ec4);

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x7f));
			goto L1b5e;

		L18b8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x80));
			
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x3e));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9),
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x450)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L1904;
			goto L1907;

		L1904:
			goto L1b5e;

		L1907:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1910;
			goto L1970;

		L1910:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9),
				this.processor.IncrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9)), 0x5);
			if (this.processor.FlagSet.IsLessOrEqual) goto L192e;
			goto L1931;

		L192e:
			goto L1b5e;

		L1931:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalWord(0x3a));

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30)] |= 1;

			this.owner.MapOps.DiscardTileImprovements(6,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			goto L1994;

		L1970:
			this.owner.MapOps.DiscardTileImprovements(2,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.AssignTileImprovements(4,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L1994:
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x7f));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), 0x0);
			goto L1b5e;

		L19bf:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L19d9;
			goto L19e8;

		L19d9:
			DisplayHelpForHuman(0x1ecc);

			goto L1b5e;

		L19e8:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L19fe;
			goto L1a10;

		L19fe:
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1a07;
			goto L1a10;

		L1a07:
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0x30);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1a10;
			goto L1a13;

		L1a10:
			goto L1b5e;

		L1a13:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1a28;
			goto L1a92;

		L1a28:
			this.owner.MapOps.LocateCityAtTile(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsLess) goto L1a4d;
			goto L1a83;

		L1a4d:
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent++;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d8), 0x0);
			
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			goto L1a8f;

		L1a83:
			DisplayHelpForHuman(0x1ed6);

		L1a8f:
			goto L1b5e;

		L1a92:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			
			this.owner.Founding.EstablishCity(participantIdx,
				this.processor.PullLocalWord(0x24),
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30),
				1);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1acb;
			goto L1ae7;

		L1acb:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

		L1ae7:
			goto L1b5e;

		L1aed:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x68);
			if (this.processor.FlagSet.IsNotEqual) goto L1af5;
			goto L10bb;

		L1af5:
			if (this.processor.FlagSet.IsLessOrEqual) goto L1afa;
			goto L1b2d;

		L1afa:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L1b02;
			goto L0f1f;

		L1b02:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x44);
			if (this.processor.FlagSet.IsNotEqual) goto L1b0a;
			goto L0d75;

		L1b0a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x50);
			if (this.processor.FlagSet.IsNotEqual) goto L1b12;
			goto L0d9b;

		L1b12:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x62);
			if (this.processor.FlagSet.IsNotEqual) goto L1b1a;
			goto L19bf;

		L1b1a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x66);
			if (this.processor.FlagSet.IsNotEqual) goto L1b22;
			goto L1283;

		L1b22:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x67);
			if (this.processor.FlagSet.IsNotEqual) goto L1b2a;
			goto L0ffd;

		L1b2a:
			goto L1b5e;

		L1b2d:
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x69);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xe);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L1b38;
			goto L1b5e;

		L1b38:
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L158c;
				case 1:
					goto L1b5e;
				case 2:
					goto L1b5e;
				case 3:
					goto L1b5e;
				case 4:
					goto L17c4;
				case 5:
					goto L1b5e;
				case 6:
					goto L1b5e;
				case 7:
					goto L1180;
				case 8:
					goto L1b5e;
				case 9:
					goto L1400;
				case 10:
					goto L0f44;
				case 11:
					goto L1b5e;
				case 12:
					goto L0f6f;
				case 13:
					goto L1b5e;
				case 14:
					goto L0f37;
			}

		L1b5e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1b68;
			goto L1b71;

		L1b68:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1b71;
			goto L3365;

		L1b71:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			goto L32e4;

		L1b77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1b80;
			goto L1b9c;

		L1b80:
			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullLocalSignedWord(0xe) - 7,
				this.processor.PullLocalSignedWord(0x12) - 6);

			goto L1bb5;

		L1b9c:
			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullLocalSignedWord(0x2c) - 7,
				this.processor.PullLocalSignedWord(0x30) - 6);

		L1bb5:
			goto L3365;

		L1bb8:
			this.processor.EmitLocalWord(0x24, 0x8);
			goto L1c10;

		L1bc3:
			this.processor.EmitLocalWord(0x24, 0x1);
			goto L1c10;

		L1bce:
			this.processor.EmitLocalWord(0x24, 0x2);
			goto L1c10;

		L1bd9:
			this.processor.EmitLocalWord(0x24, 0x7);
			goto L1c10;

		L1be4:
			this.processor.EmitLocalWord(0x24, 0x3);
			goto L1c10;

		L1bef:
			this.processor.EmitLocalWord(0x24, 0x6);
			goto L1c10;

		L1bfa:
			this.processor.EmitLocalWord(0x24, 0x5);
			goto L1c10;

		L1c05:
			this.processor.EmitLocalWord(0x24, 0x4);

		L1c10:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1c19;
			goto L1cb0;

		L1c19:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c3d;
			goto L1c4b;

		L1c3d:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L1c4b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + 
					this.processor.PullLocalSignedWord(0xe));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x12),
				0, 49);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			VerifyPositionOnScreen(this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c94;
			goto L1cad;

		L1c94:
			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullLocalSignedWord(0xe) - 8,
				this.processor.PullLocalSignedWord(0x12) - 7);

		L1cad:
			goto L3365;

		L1cb0:
			if (participantIdx != 0)
				goto L1d96;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d7);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L1cd5;
			goto L1d96;

		L1cd5:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1cef;
			goto L1d96;

		L1cef:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L1cf8;
			goto L1d96;

		L1cf8:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1d00;
			goto L1d96;

		L1d00:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1d08;
			goto L1d96;

		L1d08:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			if (this.owner.World.Towns[this.processor.RegAcc.Value16].ParticipantIdx == this.owner.World.PersonParticipantIdx) goto L1d2b;
			goto L1d96;

		L1d2b:
			this.owner.MapOps.DiscardTileImprovements(6,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1d62;
			goto L1d7e;

		L1d62:
			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L1d7e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			goto L3365;

		L1d96:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + 
					this.processor.PullLocalSignedWord(0x2c));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x30),
				0, 49);

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1e2f;
			goto L219c;

		L1e2f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1e38;
			goto L1ed2;

		L1e38:
			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1e51;
			goto L1ed2;

		L1e51:
			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1e6a;
			goto L1ed2;

		L1e6a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L1e84;
			goto L1ed2;

		L1e84:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1e8d;
			goto L1ed2;

		L1e8d:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L1ea4;

			this.owner.Services.PerformSound(0x25, 0);

		L1ea4:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1eae;
			goto L1eba;

		L1eae:
			DisplayHelpForHuman(0x1edf);

		L1eba:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L1ed2:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1eec;
			goto L219c;

		L1eec:
			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1f06;
			goto L2157;

		L1f06:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx == participantIdx)
				goto L1f27;

			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1f24;
			goto L1f27;

		L1f24:
			goto L3365;

		L1f27:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L1f41;
			goto L1fb8;

		L1f41:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx;

			if (participantIdx == this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx)
				goto L1fb8;

			this.processor.CompareWord((ushort)this.owner.World.PersonParticipantIdx, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1f61;
			goto L1fa4;

		L1f61:
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.owner.TextPainter.AwaitTime(30);

			this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0x24));

		L1fa4:
			this.owner.Diplomats.DiplomatArrivesAtCity(this.processor.PullLocalSignedWord(0x10),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32));
			
			goto L3365;

		L1fb8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L1fd2;
			goto L2154;

		L1fd2:
			this.processor.RegAcc.LowByte = 0x1c;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x1c),
				this.owner.World.Towns[this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df))].Place.X,
				this.processor.PullLocalSignedWord(0x26),
				this.owner.World.Towns[this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df))].Place.Y);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x10));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx != participantIdx)
				goto L2066;

			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2018;
			goto L2066;

		L2018:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xa);
			if (this.processor.FlagSet.IsLess) goto L2021;
			goto L2066;

		L2021:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x1c;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81df)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81df))].Place.X,
				this.owner.World.Towns[this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81df))].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2066;
			goto L2154;

		L2066:
			this.owner.Libc.CopyText(0xba06, "Will you?\n Keep moving\n Establish trade route\n");

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x10));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx != participantIdx)
				goto L20b4;

			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsLess) goto L2095;
			goto L20b4;

		L2095:
			this.owner.Libc.AppendText(0xba06, " Help build WONDER.\n");

			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xa);
			if (this.processor.FlagSet.IsLess) goto L20ae;
			goto L20b4;

		L20ae:
			this.processor.EmitGlobalWord(0xb276, 0x2);

		L20b4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx == participantIdx)
				goto L20d1;

			this.processor.EmitLocalWord(0x3a, 0x1);
			goto L20e8;

		L20d1:
			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

		L20e8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L20f1;
			goto L210d;

		L20f1:
			this.owner.Conquest.FoundTradeRoute(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x10));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L210a;
			goto L210d;

		L210a:
			goto L3365;

		L210d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L2116;
			goto L2154;

		L2116:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142)));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x10));
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].MaterialsTally += (short)this.processor.RegCount.Value16;
			
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

			goto L3365;

		L2154:
			goto L219c;

		L2157:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L2171;
			goto L219c;

		L2171:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L217a;
			goto L219c;

		L217a:
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
				goto L219c;

			this.owner.Diplomats.BuyUnit(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
				participantIdx);
			
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);

		L219c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L21a5;
			goto L2209;

		L21a5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L21c7;
			goto L2209;

		L21c7:
			if (participantIdx != this.processor.PullGlobalSignedWord(0xd20a))
				goto L2203;

			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L21db;
			goto L2203;

		L21db:
			this.owner.UnitOps.TallySpecialUnitsInStack(participantIdx, this.processor.PullLocalSignedWord(0x18));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsGreater) goto L21f1;
			goto L2203;

		L21f1:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x1));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xf3));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x3);
			goto L2206;

		L2203:
			goto L3365;

		L2206:
			goto L267b;

		L2209:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2212;
			goto L267b;

		L2212:
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
				goto L267b;

			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2226;
			goto L225c;

		L2226:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2248;
			goto L225c;

		L2248:
			DisplayHelpForHuman(0x1f28);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L225c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2265;
			goto L2282;

		L2265:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x16);
			if (this.processor.FlagSet.IsEqual) goto L227f;
			goto L2282;

		L227f:
			goto L3365;

		L2282:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L22b2;

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L22af;
			goto L22b2;

		L22af:
			goto L3365;

		L22b2:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L22cc;
			goto L22cf;

		L22cc:
			goto L3365;

		L22cf:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd20a));
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x18));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L22f2;
			goto L2329;

		L22f2:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L230c;
			goto L2329;

		L230c:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2315;
			goto L2329;

		L2315:
			DisplayHelpForHuman(0x1f30);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L2329:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsLess) goto L2343;
			goto L23d5;

		L2343:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L23b8;

			this.owner.Libc.CopyText(0xba06, "Attack at\n");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 10));

			this.owner.Libc.AppendText(0xba06, "/3 strength?\n Cancel\n Attack\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L23ad;
			goto L23b5;

		L23ad:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L23b5:
			goto L23d5;

		L23b8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x2);
			if (this.processor.FlagSet.IsLess) goto L23d2;
			goto L23d5;

		L23d2:
			goto L3365;

		L23d5:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd20a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L23e1;
			goto L23f3;

		L23e1:
			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

		L23f3:
			this.owner.UnitOps.LocateBestGuardianInStack(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L241c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd20a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L241c;
			goto L2422;

		L241c:
			this.processor.EmitGlobalWord(0x70d8, 0x1);

		L2422:
			this.owner.Combat.SettleBattle(participantIdx, 
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
				1);
			
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.EmitGlobalWord(0x70d8, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2466;
			goto L2469;

		L2466:
			goto L3365;

		L2469:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2483;
			goto L248e;

		L2483:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			goto L3c40;

		L248e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d8);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.SubtractByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x3));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsLess) goto L24ad;
			goto L24b0;

		L24ad:
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, 0x0);

		L24b0:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xf);
			if (this.processor.FlagSet.IsEqual) goto L24ca;
			goto L24cf;

		L24ca:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L24cf:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L24d8;
			goto L264c;

		L24d8:
			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x10));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ConditionMarker |= 0x10;
			}

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L251e;
			goto L255b;

		L251e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x10)].StructureMarkers0, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L2530;
			goto L255b;

		L2530:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2539;
			goto L255b;

		L2539:
			if (this.owner.World.HardnessTier == 0) goto L2543;
			goto L254f;

		L2543:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd20a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L254f;
			goto L255b;

		L254f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent--;

		L255b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L256d;
			goto L25cf;

		L256d:
			this.owner.Advisors.FinalizeProduction(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26));

			this.owner.NewGameWizard.EliminateCivilization(this.processor.PullGlobalSignedWord(0xd20a), participantIdx);

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L25cf;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L25b2;
			goto L25cf;

		L25b2:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd20a));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x18));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L25cf:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L25f0;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd20a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L25e6;
			goto L25f0;

		L25e6:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25f0;
			goto L260e;

		L25f0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x10));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent;
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

		L260e:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2622;
			goto L2649;

		L2622:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd20a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L262e;
			goto L2649;

		L262e:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.Bot.RefreshCityProduction(this.processor.PullGlobalSignedWord(0xd20a), this.processor.RegAcc.Value16);

		L2649:
			goto L3365;

		L264c:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L2655;
			goto L2658;

		L2655:
			goto L3365;

		L2658:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2672;
			goto L2675;

		L2672:
			goto L3365;

		L2675:
			goto L3365;

		L267b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2684;
			goto L26b7;

		L2684:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L268d;
			goto L26b7;

		L268d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L26af;
			goto L26b7;

		L26af:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L26b7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L26d9;
			goto L273b;

		L26d9:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L26e2;
			goto L273b;

		L26e2:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L26f8;
			goto L273b;

		L26f8:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x19);
			if (this.processor.FlagSet.IsNotEqual) goto L2702;
			goto L2719;

		L2702:
			DisplayHelpForHuman(0x1f62);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			goto L3365;

		L2719:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.owner.Combat.ExplodeNuclearWeapon(participantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));
			
			goto L3365;

		L273b:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2755;
			goto L2758;

		L2755:
			goto L3365;

		L2758:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L277a;
			goto L2850;

		L277a:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, this.processor.PullLocalByte(0x40));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L2792;
			goto L27e4;

		L2792:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L279c;
			goto L27e4;

		L279c:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L27b1;
			goto L27db;

		L27b1:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L27db;

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L27d6;
			goto L27db;

		L27d6:
			this.processor.RegAcc.LowByte = 0x0;
			goto L27dd;

		L27db:
			this.processor.RegAcc.LowByte = 0x1;

		L27dd:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 
				this.processor.SubtractByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), this.processor.RegAcc.LowByte));
			goto L284d;

		L27e4:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d8);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L2800;
			goto L2829;

		L2800:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L2823;
			goto L2829;

		L2823:
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, 0x0);
			goto L3365;

		L2829:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x3e));
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8),
				this.processor.SubtractByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), this.processor.RegCount.LowByte));

		L284d:
			goto L2865;

		L2850:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8),
				this.processor.SubtractByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3));

		L2865:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d8);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsLess) goto L2881;
			goto L2884;

		L2881:
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, 0x0);

		L2884:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L28ff;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L28a9;
			goto L28e8;

		L28a9:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.BitXorWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x24));
			if (this.processor.FlagSet.IsEqual) goto L28b9;
			goto L28e8;

		L28b9:
			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L28d3;
			goto L28e8;

		L28d3:
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			goto L3365;

		L28e8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x24);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);

		L28ff:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2908;
			goto L29a0;

		L2908:
			if (participantIdx != this.processor.PullGlobalSignedWord(0xd20a) ||
				participantIdx == this.owner.World.PersonParticipantIdx)
				goto L29a0;

			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2927;
			goto L29a0;

		L2927:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138));
			this.processor.CompareWord(this.processor.RegDest.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L294b;
			goto L295d;

		L294b:
			this.processor.CompareWord(this.processor.RegDest.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2953;
			goto L29a0;

		L2953:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L295d;
			goto L29a0;

		L295d:
			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x18), 2);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L298c;
			goto L2992;

		L298c:
			this.processor.RegAcc.Value16 = 0x4;
			goto L2995;

		L2992:
			this.processor.RegAcc.Value16 = 0x2;

		L2995:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsLessOrEqual) goto L299d;
			goto L29a0;

		L299d:
			goto L3365;

		L29a0:
			this.owner.VideoCard.ObtainDot(2, 
				this.processor.PullLocalSignedWord(0x1c) + 80,
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L29c2;
			goto L2b06;

		L29c2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L29cb;
			goto L2b06;

		L29cb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L29d4;
			goto L2acb;

		L29d4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L29dd;
			goto L2acb;

		L29dd:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L29f7;
			goto L2acb;

		L29f7:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L2a00;
			goto L2acb;

		L2a00:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2a0b;
			goto L2acb;

		L2a0b:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2a21;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2a21;
			goto L2acb;

		L2a21:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2a43;
			goto L2acb;

		L2a43:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L2a59;
			goto L2acb;

		L2a59:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L2a92;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			
			this.owner.Combat.AffirmBreakTreaty(this.processor.PullLocalSignedWord(0x2));
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2a7a;
			goto L2a7d;

		L2a7a:
			goto L3365;

		L2a7d:
			this.owner.Politics.WipeDiplomacyFlags(participantIdx, this.processor.PullLocalSignedWord(0x2), 2);
			
			goto L2acb;

		L2a92:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2a9d;
			goto L2acb;

		L2a9d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[participantIdx] & 2) == 0)
				goto L2acb;

			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);
			goto L3365;

		L2acb:
			if (participantIdx == this.processor.PullLocalSignedWord(0x2))
				goto L2b06;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L2aeb;
			goto L2b06;

		L2aeb:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L2b06:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);
			
			if (participantIdx != 0)
				goto L2b9d;

			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2b29;
			goto L2b9d;

		L2b29:
			this.processor.RegSource.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2b54;
			goto L2b9d;

		L2b54:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x81dd);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2b72;
			goto L2b9d;

		L2b72:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			if (this.owner.World.Towns[this.processor.RegAcc.Value16].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
					this.processor.PullLocalWord(0x30)];
				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16), this.processor.RegAcc.LowByte));
			}

		L2b9d:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ba6;
			goto L2c18;

		L2ba6:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2bb1;
			goto L2c18;

		L2bb1:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2bd6;
			goto L2c18;

		L2bd6:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				goto L2bfc;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x48)] & 0x40) == 0)
				goto L2c18;

		L2bfc:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L2c18:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2c21;
			goto L2c51;

		L2c21:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2c36;
			goto L2c51;

		L2c36:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x48);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L2c51:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2c76;
			goto L2c94;

		L2c76:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2c94;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2c8b;
			goto L2cdd;

		L2c8b:
			if (participantIdx == 0)
				goto L2cdd;

		L2c94:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L2c9e;
			goto L2ca9;

		L2c9e:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L2ccf;

		L2ca9:
			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0x24));

			goto L2cdd;

		L2ccf:
			this.owner.MapOps.RenderMapTile(this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L2cdd:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CopyLocalWord(0x2c, 0x22);
			this.processor.CopyLocalWord(0x30, 0x2a);

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), 0x0);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2d6e;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2d69;
			goto L2d6e;

		L2d69:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x30));

		L2d6e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2d77;
			goto L2da5;

		L2d77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L2d80;
			goto L2da5;

		L2d80:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2d8b;
			goto L2da5;

		L2d8b:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x1c) + 80,
				this.processor.PullLocalSignedWord(0x26),
				0);

		L2da5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1c));
			if (this.processor.FlagSet.IsEqual) goto L2dc3;
			goto L2deb;

		L2dc3:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x26));
			if (this.processor.FlagSet.IsEqual) goto L2dd1;
			goto L2deb;

		L2dd1:
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2deb;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L2deb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2df4;
			goto L2e24;

		L2df4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2dfd;
			goto L2e24;

		L2dfd:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2e1f;
			goto L2e24;

		L2e1f:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L2e24:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x52, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x52));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e4c;
			goto L2e55;

		L2e4c:
			this.processor.CompareByte(this.processor.PullLocalByte(0x52), 0x17);
			if (this.processor.FlagSet.IsEqual) goto L2e55;
			goto L2ff4;

		L2e55:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2e5e;
			goto L2ff4;

		L2e5e:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);

		L2e67:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractBorrowWord(this.processor.RegAcc.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)));
			if (this.processor.FlagSet.IsEqual) goto L2ead;
			goto L2f9d;

		L2ead:
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2eb7;
			goto L2eef;

		L2eb7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2ec0;
			goto L2eef;

		L2ec0:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2f9d;

			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0xc);
			if (this.processor.FlagSet.IsEqual) goto L2ed5;
			goto L2f9d;

		L2ed5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x14));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L2eef;
			goto L2f9d;

		L2eef:
			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x22),
				this.processor.PullLocalSignedWord(0x2a));

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), 0xff);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f61;
			goto L2f6e;

		L2f61:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x14));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x1));

		L2f6e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.CompareWord(this.processor.RegDest.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f92;
			goto L2f9a;

		L2f92:
			this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
			this.processor.RegAcc.LowByte = this.processor.DecrementByte(this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L2f9a:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L2f9d:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4c);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), 0x14);
			if (this.processor.FlagSet.IsLess) goto L2faf;
			goto L2ff4;

		L2faf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2fb8;
			goto L2ff4;

		L2fb8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x52, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x52));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2fe2;
			goto L2e67;

		L2fe2:
			this.processor.CompareByte(this.processor.PullLocalByte(0x52), 0x17);
			if (this.processor.FlagSet.IsNotEqual) goto L2feb;
			goto L2e67;

		L2feb:
			if (participantIdx == 0)
				goto L2e67;

		L2ff4:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ffd;
			goto L3054;

		L2ffd:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3008;
			goto L3054;

		L3008:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x48);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

			this.owner.Conquest.SeizeCity(participantIdx, this.processor.PullLocalSignedWord(0x10), 0);

			this.owner.MapOps.TagTileOwned(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

		L3054:
			this.owner.MapOps.MapAssignCityOwner(participantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.UnitOps.ShiftUnit(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				1);
			
			this.owner.MapOps.ContainsHut(
				this.processor.PullLocalWord(0x3e),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L309a;
			goto L30bd;

		L309a:
			this.owner.UnitOps.VisitHut(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)] |= 1;

		L30bd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L30c6;
			goto L30fe;

		L30c6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L30e8;
			goto L30fe;

		L30e8:
			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 0x1560);

		L30fe:
			if (participantIdx != 0)
				goto L3184;

			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3110;
			goto L3184;

		L3110:
			this.processor.RegSource.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L313b;
			goto L3184;

		L313b:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x81dd);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3159;
			goto L3184;

		L3159:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			if (this.owner.World.Towns[this.processor.RegAcc.Value16].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
					this.processor.PullLocalWord(0x26)];

				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16), this.processor.RegAcc.LowByte));
			}

		L3184:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L31a9;
			goto L31c7;

		L31a9:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L31c7;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31be;
			goto L3242;

		L31be:
			if (participantIdx == 0)
				goto L3242;

		L31c7:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L31dc;

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L31dc;
			goto L3220;

		L31dc:
			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L321d;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3211;
			goto L321d;

		L3211:
			this.owner.TextPainter.AwaitTime(30);

		L321d:
			goto L3242;

		L3220:
			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3236;
			goto L3242;

		L3236:
			this.owner.TextPainter.AwaitTime(10);

		L3242:
			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3250;
			goto L3281;

		L3250:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3272;
			goto L3281;

		L3272:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);

		L3281:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L32a3;
			goto L32e1;

		L32a3:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L32ad;
			goto L32e1;

		L32ad:
			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L32c7;
			goto L32e1;

		L32c7:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L32e1:
			goto L3365;

		L32e4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L32ec;
			goto L1bb8;

		L32ec:
			if (this.processor.FlagSet.IsLessOrEqual) goto L32f1;
			goto L3312;

		L32f1:
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L32fc;
			goto L3365;

		L32fc:
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L1bc3;
				case 1:
					goto L1bce;
				case 2:
					goto L1be4;
				case 3:
					goto L1c05;
				case 4:
					goto L1bfa;
				case 5:
					goto L1bef;
				case 6:
					goto L1bd9;
			}

		L3312:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4900);
			if (this.processor.FlagSet.IsNotEqual) goto L331a;
			goto L1bce;

		L331a:
			if (this.processor.FlagSet.IsLessOrEqual) goto L331f;
			goto L333a;

		L331f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L3327;
			goto L1b77;

		L3327:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4700);
			if (this.processor.FlagSet.IsNotEqual) goto L332f;
			goto L1bb8;

		L332f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L3337;
			goto L1bc3;

		L3337:
			goto L3365;

		L333a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsNotEqual) goto L3342;
			goto L1bd9;

		L3342:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsNotEqual) goto L334a;
			goto L1be4;

		L334a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f00);
			if (this.processor.FlagSet.IsNotEqual) goto L3352;
			goto L1bef;

		L3352:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L335a;
			goto L1bfa;

		L335a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5100);
			if (this.processor.FlagSet.IsNotEqual) goto L3365;
			goto L1c05;

		L3365:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			goto L3abd;

		L336b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3375;
			goto L337d;

		L3375:
			this.owner.ArmyPicker.DisplayWhichArmy();

			goto L3c40;

		L337d:
			this.owner.Briefings.DisplayCityStatusReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L338c:
			this.owner.Briefings.RenderUnitCountsChart(this.owner.World.PersonParticipantIdx);
			
			goto L3c40;

		L339b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L33a5;
			goto L33cf;

		L33a5:
			this.owner.Startup.ConcealMouseCursor();
			
			this.owner.Services.DisplayScreen(2);
			
			this.owner.Libc.ReadKey();

			this.owner.Services.DisplayScreen(0);
			
			this.owner.Startup.DisplayMouseCursor();

			goto L33db;

		L33cf:
			this.owner.Briefings.DisplayMilitaryReport();

		L33db:
			goto L3c40;

		L33de:
			this.owner.Briefings.DisplayAttitudeReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L33ed:
			this.owner.Briefings.DisplayTradeReport(this.owner.World.PersonParticipantIdx);
			
			goto L3c40;

		L33fc:
			this.owner.Briefings.DisplayScienceReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L340b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3415;
			goto L343a;

		L3415:
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L3420;

		L341d:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L3420:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsLess) goto L3429;
			goto L3437;

		L3429:
			this.owner.RealmInfo.DisplayCivilizationOverview(this.processor.PullLocalSignedWord(0x3c));
			
			goto L341d;

		L3437:
			goto L343f;

		L343a:
			this.owner.Atlas.DisplayWondersOfTheWorld();

		L343f:
			goto L3c40;

		L3442:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L344c;
			goto L3459;

		L344c:
			this.owner.Atlas.DisplayCivilizationGraph();

			this.owner.Replayer.DisplayReplay();

			goto L345e;

		L3459:
			this.owner.Legends.DisplayTopCities();

		L345e:
			goto L3c40;

		L3461:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L346b;
			goto L3473;

		L346b:
			this.owner.Atlas.DisplayGameFigures();

			goto L3c40;

		L3473:
			this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true);

			goto L3c40;

		L348a:
			this.owner.Atlas.DisplayWorldMap(1);

			goto L3c40;

		L349d:
			this.processor.EmitLocalWord(0x24, 0x1);
			goto L34dd;

		L34a5:
			this.processor.EmitLocalWord(0x24, 0x2);
			goto L34dd;

		L34ad:
			this.processor.EmitLocalWord(0x24, 0x3);
			goto L34dd;

		L34b5:
			this.processor.EmitLocalWord(0x24, 0x4);
			goto L34dd;

		L34bd:
			this.processor.EmitLocalWord(0x24, 0x5);
			goto L34dd;

		L34c5:
			this.processor.EmitLocalWord(0x24, 0x6);
			goto L34dd;

		L34cd:
			this.processor.EmitLocalWord(0x24, 0x7);
			goto L34dd;

		L34d5:
			this.processor.EmitLocalWord(0x24, 0x8);

		L34dd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.SumWord(this.processor.PullGlobalWord(0xd4cc), this.processor.RegAcc.Value16));

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xd75e, this.processor.SumWord(this.processor.PullGlobalWord(0xd75e), this.processor.RegAcc.Value16));

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			goto L3c40;

		L3511:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsEqual) goto L351b;
			goto L351e;

		L351b:
			goto L3ea1;

		L351e:
			goto L3c40;

		L3521:
			this.owner.CityNaming.DisplayNewCityLocation();

			goto L3c40;

		L3529:
			if (this.owner.World.RoundTally != 0) goto L3533;
			goto L353f;

		L3533:
			this.owner.SaveLoad.DisplayStoreGameDialog(0xffff);

		L353f:
			goto L3c40;

		L3542:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x1f6a);

			this.owner.TurnHooks.DisplayDialog(0xba06, 64, 80);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L356f;
			goto L3572;

		L356f:
			goto L3c40;

		L3572:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.World.Participants[participantIdx].RegimeKind = 0;

			this.owner.Libc.CopyText(0xba06, "The ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " are\nrevolting! Citizens\ndemand new govt.\n");

			this.owner.Paper.DisplayNewspaper(-1);
			
			this.owner.NewGameWizard.ImportGovernmentGraphics();

			this.owner.TurnHooks.RepaintMainScreen();

			goto L3c40;

		L35c6:
			this.owner.Services.PrepareMouse();

			goto L3c40;

		L35ce:
			this.owner.Libc.CopyText(0xba06, "Select new Tax rate:\n ");

			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L35e9;

		L35e6:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L35e9:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[participantIdx].ResearchLevyRatio;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L35fe;
			goto L3673;

		L35fe:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(10 * (short)this.processor.PullLocalWord(0x3a)), 10));

			this.owner.Libc.AppendText(0xba06, "% Tax, (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((this.owner.World.Participants[participantIdx].LevyRatio +
					this.owner.World.Participants[participantIdx].ResearchLevyRatio -
					(short)this.processor.PullLocalWord(0x3a)) * 10), 10));

			this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			goto L35e6;

		L3673:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L369f;
			goto L36b6;

		L369f:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.owner.World.Participants[participantIdx].ResearchLevyRatio += 
				(short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);
			this.owner.World.Participants[participantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;

			this.owner.TurnHooks.RenderStatusBar();

		L36b6:
			goto L3c40;

		L36b9:
			this.owner.Libc.CopyText(0xba06, "Select new Luxuries rate:\n ");

			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L36d4;

		L36d1:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L36d4:
			this.processor.RegAcc.Value16 = 10;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L36ea;
			goto L375e;

		L36ea:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(10 * (short)this.processor.PullLocalWord(0x3a)), 10));

			this.owner.Libc.AppendText(0xba06, "% Luxuries, (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(-((short)this.processor.PullLocalWord(0x3a) +
				this.owner.World.Participants[participantIdx].LevyRatio - 10) * 10), 10));

			this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			goto L36d1;

		L375e:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[participantIdx].ResearchLevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L3793;
			goto L37a8;

		L3793:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 10);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].ResearchLevyRatio = (short)this.processor.RegAcc.Value16;

			this.owner.TurnHooks.RenderStatusBar();

		L37a8:
			goto L3c40;

		L37ab:
			this.processor.EmitGlobalWord(0xdcfc, 0x1);

			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.EmitGlobalWord(0xdcfc, 0x0);

			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			goto L3c40;

		L37e5:
			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L37f0;

		L37ed:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L37f0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x10);
			if (this.processor.FlagSet.IsLess) goto L37f9;
			goto L3837;

		L37f9:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x3a));

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1512), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1514), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1516), this.processor.RegAcc.Value16);
			goto L37ed;

		L3837:
			this.owner.TurnHooks.DisplayDialog(0x2003, 100, 80);

			goto L3c40;

		L384e:
			this.owner.World.SessionPrefMarkers ^= 0x10;

			this.owner.Libc.CopyText(0xba06, "Sounds ");

			if ((this.owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x2020);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2024);
			}
		
			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			goto L3c40;

		L389b:
			this.owner.HelpSystem.DisplayHelpPopup(0x2029);
			
			this.owner.HelpSystem.DisplayHelpPopup(0x2030);
			
			goto L3c40;

		L38b6:
			// Load Game from the GAME menu leaves the turn without asking
			if (this.processor.PullGlobalWord(0xdc48) == GameLoadAndSave.FetchSessionAsk) goto L38fb;

			this.owner.Libc.CopyText(0xba06, "Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L38e2;
			goto L38eb;

		L38e2:
			this.processor.EmitGlobalWord(0xdc48, 0x0);
			goto L3c40;

		L38eb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L38f5;
			goto L38fb;

		L38f5:
			this.processor.EmitGlobalWord(0xdc48, 0x1);

		L38fb:
			this.processor.EmitLocalWord(0x32, 0x80);
			goto L3c40;

		L3903:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L390d;
			goto L3952;

		L390d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3916;
			goto L3952;

		L3916:
			this.processor.EmitLocalWord(0x4, 0x0);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L393f;
			goto L394d;

		L393f:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L394d:
			this.processor.EmitLocalWord(0xe, 0xffff);

		L3952:
			goto L3c40;

		L3955:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L395f;
			goto L3a38;

		L395f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3968;
			goto L39a2;

		L3968:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L398c;
			goto L399a;

		L398c:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L399a:
			this.processor.EmitLocalWord(0xe, 0xffff);
			goto L3a33;

		L39a2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L39ac;
			goto L39ef;

		L39ac:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			VerifyPositionOnScreen(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L39da;
			goto L39ef;

		L39da:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			goto L3a01;

		L39ef:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4cc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd75e);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

		L3a01:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3a25;
			goto L3a33;

		L3a25:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L3a33:
			this.processor.EmitLocalWord(0x4, this.processor.BitXorWord(this.processor.PullLocalWord(0x4), 0x1));

		L3a38:
			goto L3c40;

		L3a3b:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 0);

			goto L3aa4;

		L3a50:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			goto L3aa4;

		L3a65:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 2);

			goto L3aa4;

		L3a7a:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 3);

			goto L3aa4;

		L3a8f:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 4);

			goto L3aa4;

		ToggleDebugMode:
			this.processor.EmitGlobalWord(0xd806, (ushort)(~this.processor.PullGlobalWord(0xd806)));
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L34dd;

		L3aa4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd4ca), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L3aae;
			goto L3ab7;

		L3aae:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4ca);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			goto L0d0a;

		L3ab7:
			goto L3c40;

		L3abd:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2300); // Alt + H
			if (this.processor.FlagSet.IsNotEqual) goto L3ac5;
			goto L389b;

		L3ac5:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3aca;
			goto L3b7d;

		L3aca:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3d); // '='
			if (this.processor.FlagSet.IsNotEqual) goto L3ad2;
			goto L35ce;

		L3ad2:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3ad7;
			goto L3b22;

		L3ad7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1b); // ESC
			if (this.processor.FlagSet.IsNotEqual) goto L3adf;
			goto L3903;

		L3adf:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3ae4;
			goto L3aff;

		L3ae4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xfffe); // ??
			if (this.processor.FlagSet.IsNotEqual) goto L3aec;
			goto L3542;

		L3aec:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9); // Tab
			if (this.processor.FlagSet.IsNotEqual) goto L3af4;
			goto L3955;

		L3af4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd); // Enter
			if (this.processor.FlagSet.IsNotEqual) goto L3afc;
			goto L3511;

		L3afc:
			goto L3c40;

		L3aff:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x20); // Space
			if (this.processor.FlagSet.IsNotEqual) goto L3b07;
			goto L3511;

		L3b07:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2b); // '+'
			if (this.processor.FlagSet.IsNotEqual) goto L3b0f;
			goto L35ce;

		L3b0f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2d); // '-'
			if (this.processor.FlagSet.IsNotEqual) goto L3b17;
			goto L36b9;

		L3b17:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f); // '/'
			if (this.processor.FlagSet.IsNotEqual) goto L3b1f;
			goto L3521;

		L3b1f:
			goto L3c40;

		L3b22:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1000); // Alt + Q
			if (this.processor.FlagSet.IsNotEqual) goto L3b2a;
			goto L38b6;

		L3b2a:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b2f;
			goto L3b52;

		L3b2f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3f); // '?'
			if (this.processor.FlagSet.IsNotEqual) goto L3b37;
			goto L3521;

		L3b37:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x53); // 'S'
			if (this.processor.FlagSet.IsNotEqual) goto L3b3f;
			goto L3529;

		L3b3f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5f); // '_'
			if (this.processor.FlagSet.IsNotEqual) goto L3b47;
			goto L36b9;

		L3b47:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x74); // 't'
			if (this.processor.FlagSet.IsNotEqual) goto L3b4f;
			goto L37ab;

		L3b4f:
			goto L3c40;

		L3b52:
			if (this.processor.RegAcc.Value16 == 0x2000) // Alt + D
				goto ToggleDebugMode;

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1100); // Alt + W
			if (this.processor.FlagSet.IsNotEqual) goto L3b5a;
			goto L3a7a;

		L3b5a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1300); // Alt + R
			if (this.processor.FlagSet.IsNotEqual) goto L3b62;
			goto L37e5;

		L3b62:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1800); // Alt + O
			if (this.processor.FlagSet.IsNotEqual) goto L3b6a;
			goto L3a50;

		L3b6a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1e00); // Alt + A
			if (this.processor.FlagSet.IsNotEqual) goto L3b72;
			goto L3a65;

		L3b72:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2200); // Alt + G
			if (this.processor.FlagSet.IsNotEqual) goto L3b7a;
			goto L3a3b;

		L3b7a:
			goto L3c40;

		L3b7d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4100); // F7
			if (this.processor.FlagSet.IsNotEqual) goto L3b85;
			goto L340b;

		L3b85:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b8a;
			goto L3bdd;

		L3b8a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3b00); // F1
			if (this.processor.FlagSet.IsNotEqual) goto L3b92;
			goto L336b;

		L3b92:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b97;
			goto L3bb2;

		L3b97:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2e00); // Alt + C
			if (this.processor.FlagSet.IsNotEqual) goto L3b9f;
			goto L3a8f;

		L3b9f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f00); // Alt + V
			if (this.processor.FlagSet.IsNotEqual) goto L3ba7;
			goto L384e;

		L3ba7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3200); // Alt + M
			if (this.processor.FlagSet.IsNotEqual) goto L3baf;
			goto L35c6;

		L3baf:
			goto L3c40;

		L3bb2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3c00); // F2
			if (this.processor.FlagSet.IsNotEqual) goto L3bba;
			goto L338c;

		L3bba:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3d00); // F3
			if (this.processor.FlagSet.IsNotEqual) goto L3bc2;
			goto L339b;

		L3bc2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3e00); // F4
			if (this.processor.FlagSet.IsNotEqual) goto L3bca;
			goto L33de;

		L3bca:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3f00); // F5
			if (this.processor.FlagSet.IsNotEqual) goto L3bd2;
			goto L33ed;

		L3bd2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4000); // F6
			if (this.processor.FlagSet.IsNotEqual) goto L3bda;
			goto L33fc;

		L3bda:
			goto L3c40;

		L3bdd:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4838); // Shift + Up
			if (this.processor.FlagSet.IsNotEqual) goto L3be5;
			goto L349d;

		L3be5:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3bea;
			goto L3c0d;

		L3bea:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4200); // F8
			if (this.processor.FlagSet.IsNotEqual) goto L3bf2;
			goto L3442;

		L3bf2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4300); // F9
			if (this.processor.FlagSet.IsNotEqual) goto L3bfa;
			goto L3461;

		L3bfa:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4400); // F10
			if (this.processor.FlagSet.IsNotEqual) goto L3c02;
			goto L348a;

		L3c02:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4737); // Shift + Home
			if (this.processor.FlagSet.IsNotEqual) goto L3c0a;
			goto L34d5;

		L3c0a:
			goto L3c40;

		L3c0d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4939); // Shift + PageUp
			if (this.processor.FlagSet.IsNotEqual) goto L3c15;
			goto L34a5;

		L3c15:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b34); // Shift + Left
			if (this.processor.FlagSet.IsNotEqual) goto L3c1d;
			goto L34cd;

		L3c1d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d36); // Shift + Right
			if (this.processor.FlagSet.IsNotEqual) goto L3c25;
			goto L34ad;

		L3c25:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f31); // Shift + End
			if (this.processor.FlagSet.IsNotEqual) goto L3c2d;
			goto L34c5;

		L3c2d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5032); // Shift + Down
			if (this.processor.FlagSet.IsNotEqual) goto L3c35;
			goto L34bd;

		L3c35:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5133); // Shift + PageDown
			if (this.processor.FlagSet.IsNotEqual) goto L3c40;
			goto L34b5;

		L3c40:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3c4a;
			goto L3c77;

		L3c4a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3c64;
			goto L3c77;

		L3c64:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3c6d;
			goto L3c77;

		L3c6d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3c77;
			goto L047f;

		L3c77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3c81;
			goto L3e2c;

		L3c81:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L3c9b;
			goto L3c9e;

		L3c9b:
			goto L0149;

		L3c9e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3cb8;
			goto L3cc0;

		L3cb8:
			this.processor.EmitLocalWord(0x1e, 0x0);
			goto L3e2c;

		L3cc0:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3ce2;
			goto L3d71;

		L3ce2:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3cff;
			goto L3d19;

		L3cff:
			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3d19;
			goto L3d39;

		L3d19:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L3d39:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9),
				this.processor.DecrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9)), 0x0);
			if (this.processor.FlagSet.IsLess) goto L3d57;
			goto L3d71;

		L3d57:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			DisplayHelpForHuman(0x2070);

		L3d71:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsEqual) goto L3d8b;
			goto L3e2c;

		L3d8b:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L3e2c;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3daa;
			goto L3e2c;

		L3daa:
			this.processor.EmitLocalWord(0x38, 0x0);
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L3dba;

		L3db7:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L3dba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L3dc3;
			goto L3e09;

		L3dc3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3dfe;
			goto L3e06;

		L3dfe:
			this.processor.EmitLocalWord(0x38, 0x1);
			goto L3e09;

		L3e06:
			goto L3db7;

		L3e09:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e12;
			goto L3e2c;

		L3e12:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			DisplayHelpForHuman(0x2076);

		L3e2c:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L3e3d;
			goto L3e4b;

		L3e3d:
			RenderUnitInfo(participantIdx,
				this.processor.PullLocalWord(0x32));

		L3e4b:
			goto L0149;

		L3e4e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e57;
			goto L3e61;

		L3e57:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3e61;
			goto L013c;

		L3e61:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e6b;
			goto L3ea1;

		L3e6b:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L3e7c;
			goto L3ea1;

		L3e7c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3e85;
			goto L3e8f;

		L3e85:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L3e8f;
			goto L3ea1;

		L3e8f:
			this.processor.EmitLocalWord(0x32, 0x80);
			this.processor.EmitLocalWord(0x1e, 0x0);
			this.processor.EmitLocalWord(0x8, 0x1);
			goto L043b;

		L3ea1:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L3ece;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			this.owner.Startup.DisplayMouseCursor();

		L3ece:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandlePlayerTurn");
		}

		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0 ||
				this.processor.PullGlobalWord(0xd806) != 0)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("RepaintTileIfVisible");
		}

		/// <summary>
		/// Repaints the tile of the unit when the human player sees it (own unit, or the unit is shown to the human)
		/// </summary>
		public void RepaintUnitIfVisible(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitIfVisible({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			short human = this.owner.World.PersonParticipantIdx;

			if (participantIdx == human ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("RepaintUnitIfVisible");
		}

		/// <summary>
		/// 0 on a tile with a city, irrigation or a mine (improvement bits 0 - 2). Otherwise from the terrain table at 0x44a
		/// (12 bytes a terrain): 2 when the word at +4 is -3 or less, 1 when the word at +0 is -2 and the tile is at the coast
		/// (VerifyCoastalTile), else 0.
		/// </summary>
		public ushort ObtainTileMoveCost(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTileMoveCost({horizLoc}, {vertLoc})");

			ushort cost = 0;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x7) == 0)
			{
				int terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				ushort terrainRecord = (ushort)(0x44a + 0xc * terrain);

				if ((short)this.processor.PullGlobalWord((ushort)(terrainRecord + 4)) <= -3)
				{
					cost = 2;
				}
				else if (this.processor.PullGlobalWord(terrainRecord) == 0xfffe)
				{
					VerifyCoastalTile(horizLoc, vertLoc);

					if (this.processor.RegAcc.Value16 != 0)
						cost = 1;
				}
			}

			this.processor.RegAcc.Value16 = cost;

			this.processor.Journal.CloseScope("ObtainTileMoveCost");

			return cost;
		}

		/// <summary>
		/// Whether the tile is on the coast: it is terrain 11 itself, or one of the 4 tiles next to it in a straight line
		/// (headings 1, 3, 5, 7; the column is not wrapped) is terrain 10 or 11 or has improvement bit 1 - but tiles with a city
		/// do not count
		/// </summary>
		/// <returns>1 on the coast, else 0 (also in AX)</returns>
		public ushort VerifyCoastalTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyCoastalTile({horizLoc}, {vertLoc})");

			MapLayers chart = this.owner.MapOps.Chart;
			ushort coastal = 0;

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
					short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

					ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);
					ushort improvements = this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}

			this.processor.RegAcc.Value16 = coastal;

			this.processor.Journal.CloseScope("VerifyCoastalTile");

			return coastal;
		}

		/// <summary>
		/// The panel of the active unit (left, under the world map): the civilization, the type (Veteran), the moves (with the
		/// fuel of aircraft), the home city, the terrain and its improvements, then the units of the city on the tile (the two
		/// supported at 0x1c * city ... Unfamiliar, drawn through the unit 127) and the stack (up to row 0xb8, "more" after).
		/// Unit 0x80: no unit.
		/// </summary>
		public void RenderUnitInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"RenderUnitInfo({participantIdx}, {arg2})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			if (arg2 == 0x80)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x207f, 4, 124, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x208b, 4, 136, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x2097, 4, 144, 0);
			}
			else
			{
				RenderUnitPanel(participantIdx, arg2);
			}

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("RenderUnitInfo");
		}

		private void RenderUnitPanel(short participantIdx, ushort arg2)
		{
			short troopIdx = (short)arg2;
			short vertLoc = 0x63;

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, 99, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, (ushort)((0x22 * this.troops.TroopKind(participantIdx, troopIdx)) + 0x112a));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x20) != 0)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20a3, 8, vertLoc, 0);

				vertLoc += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps);
			short moves = (short)this.owner.Helpers.VerifyValueSpan(steps / 3, 0, 99);

			this.owner.Libc.CopyText(0xba06, "Moves: ");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

			short thirds = (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.owner.Libc.AppendText(0xba06, ".");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
			{
				// Aircraft: the moves left with the fuel of the next turns
				this.owner.Libc.AppendText(0xba06, "(");

				moves += (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

				this.owner.Libc.AppendText(0xba06, ")");
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "(");

			this.owner.Libc.AppendText(0xba06, (ushort)((0x13 * this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))) + 0x282));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			byte improvements = this.processor.RegAcc.LowByte;

			// Railroad or road, irrigation or mines, pollution
			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[] { ((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0) })
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(shown, 4, vertLoc, 0);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short followingIdx = this.troops.Following(participantIdx, troopIdx);
			short column = 8;

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((this.processor.RegAcc.LowByte & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				// The units the city supports elsewhere are drawn through the unit 127 of the player
				for (int unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
				{
					byte unfamiliar = (byte)this.owner.World.Towns[townIdx].Unfamiliar[unfamiliarIdx];

					if (unfamiliar == 0xff)
						continue;

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIdx, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIdx, 127, UnitMemory.TravelHoriz, 0xff);

					this.owner.MapOps.RenderUnitSprite(participantIdx, 127, column, vertLoc);

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIdx != -1)
			{
				if (followingIdx == troopIdx || vertLoc >= 0xb8)
					break;

				this.owner.MapOps.RenderUnitSprite(participantIdx, followingIdx, column, vertLoc);

				column += 0x10;

				if (column > 0x40)
				{
					column = 8;
					vertLoc += 0x10;
				}

				followingIdx = this.troops.Following(participantIdx, followingIdx);
			}

			if (followingIdx != -1 && followingIdx != troopIdx)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20f1, 74, 192, 0);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort VerifyPositionOnScreen(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"VerifyPositionOnScreen({horizLoc}, {vertLoc})");

			// function body
			if (horizLoc < 16 && this.processor.PullGlobalSignedWord(0xd4cc) >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < this.processor.PullGlobalSignedWord(0xd4cc) ||
				horizLoc >= this.processor.PullGlobalSignedWord(0xd4cc) + 15 ||
				vertLoc < this.processor.PullGlobalSignedWord(0xd75e) ||
				vertLoc >= this.processor.PullGlobalSignedWord(0xd75e) + 12)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.owner.Journal.CloseScope("VerifyPositionOnScreen");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Throws away the keys waiting and waits until the mouse buttons are released
		/// </summary>
		public void AwaitForKey()
		{
			this.processor.Journal.OpenScope("AwaitForKey()");

			while (this.owner.Libc.KeyAvailable() != 0)
			{
				this.owner.Dialogs.ObtainNavigationKey();
			}

			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			this.processor.Journal.CloseScope("AwaitForKey");
		}

		/// <summary>
		/// The next unit to activate in the stack of the unit: following the ring (at most 16 steps), the non-ship unit with the
		/// smallest index after the unit, counting round past 127
		/// </summary>
		/// <returns>That unit, the unit itself when it stands alone or nothing comes (also in AX)</returns>
		public ushort LocateBestGuardian(short participantIdx, ushort troopIdx)
		{
			this.processor.Journal.OpenScope("LocateBestGuardian()");

			ushort best = troopIdx;
			short current = this.troops.Following(participantIdx, troopIdx);

			if (current != -1)
			{
				short smallestStep = 999;

				for (int stepIdx = 0; stepIdx < 0x10; stepIdx++)
				{
					if (this.troops.KindField(participantIdx, current, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					{
						short step = (short)(current > (short)troopIdx ? current - troopIdx : current - troopIdx + 0x80);

						if (step < smallestStep)
						{
							smallestStep = step;
							best = (ushort)current;
						}
					}

					current = this.troops.Following(participantIdx, current);

					if (current == (short)troopIdx)
						break;
				}
			}

			this.processor.RegAcc.Value16 = best;

			this.processor.Journal.CloseScope("LocateBestGuardian");

			return best;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpForHuman(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpForHuman(0x{charsAddr:x4})");

			// function body
			if (this.processor.PullGlobalSignedWord(0x6b90) == this.owner.World.PersonParticipantIdx)
			{
				this.owner.HelpSystem.DisplayHelpPopupForHuman(charsAddr);
			}
		
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpForHuman");
		}
	}
}
