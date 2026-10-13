using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1d12.
	/// </summary>
	public class CityManagement
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public CityManagement(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ObtainCityTileIndex(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainCityTileIndex({horizLoc}, {vertLoc})");

			int index = CitizenSpecialistRules.FindCityTileIndex(horizLoc, vertLoc);
			this.processor.RegAcc.Value16 = (index >= 0) ? (ushort)index : (ushort)0xffff;

			this.processor.Journal.CloseScope("ObtainCityTileIndex");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="flag"></param>
		public void HandleCity(short townIdx, short marker)
		{
			this.processor.Journal.OpenScope($"HandleCity({townIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x10c);
			this.processor.PreserveIndexRegisters();

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte(this.owner.World.Towns[townIdx].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L6927;

			this.processor.EmitGlobalWord(0x2496, 0x0);
			this.processor.EmitLocalWord(0xb8, 0x0);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xda, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xfe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xca, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			this.processor.EmitLocalWord(0xd8, 0x0);
			goto L0090;

		L008c:
			this.processor.EmitLocalWord(0xd8, this.processor.IncrementWord(this.processor.PullLocalWord(0xd8)));

		L0090:
			this.processor.CompareWord(this.processor.PullLocalWord(0xd8), 0x5);
			if (this.processor.FlagSet.IsLess) goto L009a;
			goto L00ce;

		L009a:
			this.processor.EmitLocalWord(0xe4, 0x0);
			goto L00a7;

		L00a3:
			this.processor.EmitLocalWord(0xe4, this.processor.IncrementWord(this.processor.PullLocalWord(0xe4)));

		L00a7:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe4), 0x5);
			if (this.processor.FlagSet.IsLess) goto L00b1;
			goto L00cb;

		L00b1:
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xd8));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa6), 0x0);
			goto L00a3;

		L00cb:
			goto L008c;

		L00ce:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xd8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.EmitGlobalWord(0x653e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb2, 0x7);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0110;
			goto L0116;

		L0110:
			this.processor.RegAcc.Value16 = 0x0;
			goto L0119;

		L0116:
			this.processor.RegAcc.Value16 = 0x20;

		L0119:
			this.processor.EmitGlobalWord(0x6b30, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x3e7);
			if (this.processor.FlagSet.IsLessOrEqual) goto L012f;
			goto L0141;

		L012f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsLess) goto L0141;
			goto L014f;

		L0141:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;

		L014f:
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L015a;

		L0157:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L015a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0164;
			goto L02f7;

		L0164:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0176;
			goto L0181;

		L0176:
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0181;
			goto L0184;

		L0181:
			goto L0157;

		L0184:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0xd8),
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.processor.PullLocalSignedWord(0xe4),
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

			this.processor.EmitLocalWord(0xb0, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb2);
			this.processor.CompareWord(this.processor.PullLocalWord(0xb0), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L01bb;
			goto L01cd;

		L01bb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L01c5;
			goto L01cd;

		L01c5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb0);
			this.processor.EmitLocalWord(0xb2, this.processor.RegAcc.Value16);

		L01cd:			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx !=
				this.owner.World.Towns[townIdx].ParticipantIdx)
			goto L0211;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L01fe;
			goto L0211;

		L01fe:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6b30);
			this.processor.CompareWord(this.processor.PullLocalWord(0xb0), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L020a;
			goto L0211;

		L020a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb0);
			this.processor.EmitGlobalWord(0x6b30, this.processor.RegAcc.Value16);

		L0211:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb0), 0x5);
			if (this.processor.FlagSet.IsLessOrEqual) goto L021b;
			goto L02f4;

		L021b:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0228;

		L0224:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0228:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x14);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0232;
			goto L02f4;

		L0232:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));
			this.processor.RegCount.Value16 &= this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L025c;
			goto L0266;

		L025c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x14);
			if (this.processor.FlagSet.IsEqual) goto L0266;
			goto L02f1;

		L0266:
			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc6);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xd8));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L02b7;
			goto L02f1;

		L02b7:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe4));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L02d0;
			goto L02f1;

		L02d0:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc6);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xd8));
			this.processor.RegCount.Value16 = 0xa;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x8e), 0x1);

		L02f1:
			goto L0224;

		L02f4:
			goto L0157;

		L02f7:
			this.processor.EmitLocalWord(0x8e, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.EmitGlobalWord(0x6548, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6540, 0x15);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4cc);
			this.processor.EmitLocalWord(0xc2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd75e);
			this.processor.EmitLocalWord(0xd0, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind <= 1)
			{
				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0x2;
			}			

			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xb882, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1000);
			if (this.processor.FlagSet.IsEqual) goto L0356;
			goto L03c6;

		L0356:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x23);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L036e;

			goto L0372;

		L036e:
			this.processor.EmitGlobalWord(0xb882, this.processor.IncrementWord(this.processor.PullGlobalWord(0xb882)));

		L0372:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x3a);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L038a;

			goto L038e;

		L038a:
			this.processor.EmitGlobalWord(0xb882, this.processor.IncrementWord(this.processor.PullGlobalWord(0xb882)));

		L038e:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x29);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L03a6;

			goto L03aa;

		L03a6:
			this.processor.EmitGlobalWord(0xb882, this.processor.IncrementWord(this.processor.PullGlobalWord(0xb882)));

		L03aa:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x3c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L03c2;

			goto L03c6;

		L03c2:
			this.processor.EmitGlobalWord(0xb882, this.processor.IncrementWord(this.processor.PullGlobalWord(0xb882)));

		L03c6:
			this.processor.EmitLocalWord(0x4a, 0xa);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegData.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegCount.LowByte = 0x1a;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) >> this.processor.RegCount.LowByte);

			this.processor.EmitGlobalWord(0xe8b8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L03f1;
			goto L045f;

		L03f1:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			
			if (this.owner.World.Season >= 0 &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing == 7 &&
				this.owner.World.HardnessTier > 1)
			{
				this.processor.EmitLocalWord(0x4a, this.processor.SubtractWord(this.processor.PullLocalWord(0x4a), 0x2));
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0431;
			goto L045f;

		L0431:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0443;
			goto L044c;

		L0443:
			this.processor.EmitGlobalWord(0xe8b8, 0x0);
			goto L045f;

		L044c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x0;
			this.owner.World.Towns[townIdx].LaborerMarkers0 = 0;
			this.owner.World.Towns[townIdx].LaborerMarkers1 = 0;

		L045f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xd8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L048f;
			goto L0701;

		L048f:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0497;
			goto L0701;

		L0497:
			this.owner.UnitOps.ConstructUnitsFromCityList(townIdx);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			RenderFrame(2, 1, 208, 21);

			this.processor.EmitLocalWord(0xe8, 0x0);
			this.processor.EmitLocalWord(0xea, 0x1);
			goto L04ed;

		L04e9:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L04ed:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0503;
			goto L050e;

		L0503:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));
			goto L04e9;

		L050e:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.Libc.AppendText(0xba06, " (Pop:");

			this.owner.Helpers.PopulationToText(this.processor.PullLocalSignedWord(0xe8));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 104, 2, 15);

			RenderFrame(127, 23, 208, 104);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x5);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitGlobalWord(0xd75e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.owner.MapOps.RenderMapTile(this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.EmitLocalWord(0xea, 0x0);
			goto L05e2;

		L05de:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L05e2:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L05ee;
			goto L0701;

		L05ee:
			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			if (this.processor.PullGlobalSignedWord(0x6548) != this.owner.World.PersonParticipantIdx)
				goto L065c;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xc6),
				this.processor.PullLocalWord(0xd2)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L05de;

		L065c:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L067a;
			goto L0699;

		L067a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0686;
			goto L0699;

		L0686:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			goto L05de;

		L0699:
			this.owner.MapOps.RenderMapTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)((short)((10 * this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X) +
				(this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y * 2)));

			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L06ca;
			goto L06fe;

		L06ca:
			this.owner.Dialogs.RenderBox(
				(this.processor.PullLocalSignedWord(0xc6) - this.processor.PullGlobalSignedWord(0xd4cc)) * 16 + 80,
				(this.processor.PullLocalSignedWord(0xd2) - this.processor.PullGlobalSignedWord(0xd75e)) * 16 + 8,
				15, 15, 12);

		L06fe:
			goto L05de;

		L0701:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L070c;
			goto L0965;

		L070c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0x7f;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].GrainTally, 0x0);
			if (this.processor.FlagSet.IsLess) goto L072b;
			goto L0881;

		L072b:
			this.processor.EmitLocalWord(0xe8, 0xffff);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L073e;

		L073a:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L073e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0749;
			goto L0792;

		L0749:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0765;
			goto L078f;

		L0765:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L0784;
			goto L078f;

		L0784:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			goto L0792;

		L078f:
			goto L073a;

		L0792:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L079e;
			goto L07fc;

		L079e:
			this.owner.Libc.CopyText(0xba06, "Food storage exhausted\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);
			
			if (this.processor.PullLocalWord(0xe8) != 0xffff)
			{
				this.owner.Libc.AppendText(0xba06, 0x2510);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2522);
			}

			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Paper.DisplayNewspaper(townIdx);
			
			this.owner.Services.PerformSound(1, 0);

		L07fc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0806;
			goto L0819;

		L0806:
			this.owner.UnitOps.EliminateUnit(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xe8));
			
			goto L0863;

		L0819:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent--;
			if (this.processor.FlagSet.IsEqual) goto L082a;
			goto L0863;

		L082a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.Advisors.FinalizeProduction(
				townIdx,
				(ushort)((short)this.owner.World.Towns[townIdx].Place.X),
				(ushort)((short)this.owner.World.Towns[townIdx].Place.Y));

			this.owner.NewGameWizard.EliminateCivilization(this.processor.PullGlobalSignedWord(0x6548), 0);

			this.owner.Startup.DisplayMouseCursor();

			goto L6927;

		L0863:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].GrainTally = 0;

			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

		L0881:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Towns[townIdx].GrainTally);
			if (this.processor.FlagSet.IsLessOrEqual) goto L089b;
			goto L0965;

		L089b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent++;
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L08b9;
			goto L08cd;

		L08b9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x5;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].TrueExtent);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x5);
			goto L08d0;

		L08cd:
			this.processor.RegAcc.Value16 = 0x0;

		L08d0:
			this.owner.World.Towns[townIdx].GrainTally = (short)this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreater) goto L08e6;
			goto L0955;

		L08e6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L08f9;
			goto L0955;

		L08f9:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0903;
			goto L0955;

		L0903:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent--;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L091b;
			goto L0955;

		L091b:
			this.processor.EmitGlobalWord(0x2f9e, 0x4);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " requires an AQUEDUCT\nfor further growth.\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L0955:
			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

		L0965:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitGlobalWord(0x6546, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xe3c6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0992;

		L098e:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0992:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x2);
			if (this.processor.FlagSet.IsLess) goto L099c;
			goto L09b9;

		L099c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xea)], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L09b2;
			goto L09b6;

		L09b2:
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));

		L09b6:
			goto L098e;

		L09b9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsLess) goto L09cf;
			goto L09e6;

		L09cf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegCount.Value16);

		L09e6:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L09f3;

		L09ef:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L09f3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x80);
			if (this.processor.FlagSet.IsLess) goto L09fe;
			goto L0b4e;

		L09fe:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0a1a;
			goto L0b4b;

		L0a1a:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L0a39;
			goto L0b4b;

		L0a39:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L0a55;
			goto L0b2b;

		L0a55:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a6f;
			this.processor.EmitGlobalWord(0xd2f6, this.processor.IncrementWord(this.processor.PullGlobalWord(0xd2f6)));
			goto L0a8d;

		L0a6f:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind > 1 &&
				(this.owner.World.DiagMarkers & 2) != 0)
			{
				this.processor.EmitGlobalWord(0xd2f6, this.processor.IncrementWord(this.processor.PullGlobalWord(0xd2f6)));
			}

		L0a8d:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ab1;
			goto L0b2b;

		L0ab1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0ad5;
			goto L0b27;

		L0ad5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.X);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L0afe;
			goto L0b27;

		L0afe:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L0b27;
			goto L0b2b;

		L0b27:
			this.processor.EmitGlobalWord(0x6546, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6546)));

		L0b2b:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0b47;
			goto L0b4b;

		L0b47:
			this.processor.EmitGlobalWord(0xe3c6, this.processor.IncrementWord(this.processor.PullGlobalWord(0xe3c6)));

		L0b4b:
			goto L09ef;

		L0b4e:
			this.processor.EmitGlobalWord(0x70e8, 0x0);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0b61;

		L0b5d:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0b61:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0b6d;
			goto L0cae;

		L0b6d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x0);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0bba;
			goto L0bec;

		L0bba:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xc6),
				this.processor.PullLocalWord(0xd2)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0be1;
			goto L0bec;

		L0be1:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x1);

		L0bec:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0c0a;
			goto L0c49;

		L0c0a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c16;
			goto L0c49;

		L0c16:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)((short)((10 * this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X) +
				(this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y * 2)));

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0c43;
			goto L0c49;

		L0c43:
			this.processor.EmitGlobalWord(0x70e8, 0x1);

		L0c49:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)((short)((10 * this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X) +
				(this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y * 2)));

			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c6a;
			goto L0c75;

		L0c6a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x1);

		L0c75:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c84;
			goto L0cab;

		L0c84:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.RegData.Value16 = this.processor.InvertWord(this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].LaborerMarkers0 &= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 &= this.processor.RegBase.Value16;

		L0cab:
			goto L0b5d;

		L0cae:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0cbb;

		L0cb7:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0cbb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x4);
			if (this.processor.FlagSet.IsLess) goto L0cc5;
			goto L0cd4;

		L0cc5:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da), 0x0);
			goto L0cb7;

		L0cd4:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xec, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xee, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0cf1;
			goto L0d1b;

		L0cf1:
			if (((townIdx + this.owner.World.RoundTally) & 0x3) == 0)
				goto L0cff;

			goto L0d1b;

		L0cff:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0d11;
			goto L0d1b;

		L0d11:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0d1b;
			goto L0e91;

		L0d1b:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0d28;

		L0d24:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0d28:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0d34;
			goto L0d64;

		L0d34:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0d5e;
			goto L0d61;

		L0d5e:
			this.processor.EmitLocalWord(0x50, this.processor.DecrementWord(this.processor.PullLocalWord(0x50)));

		L0d61:
			goto L0d24;

		L0d64:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d6d;
			goto L0e07;

		L0d6d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0d8b;

		L0d87:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0d8b:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0d97;
			goto L0e04;

		L0d97:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0da0;
			goto L0e01;

		L0da0:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0dca;
			goto L0e01;

		L0dca:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x1);

			RenderCityTile(townIdx, this.processor.PullLocalWord(0xea), marker);

			this.processor.DecrementLocalWord(0x50);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.EmitLocalWord(0xee, this.processor.BitOrWord(this.processor.PullLocalWord(0xee), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0xec, this.processor.BitOrWord(this.processor.PullLocalWord(0xec), this.processor.RegData.Value16));

		L0e01:
			goto L0d87;

		L0e04:
			goto L0e7a;

		L0e07:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0e12;
			goto L0e69;

		L0e12:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0e1e;
			goto L0e69;

		L0e1e:
			this.owner.Libc.CopyText(0xba06, "Population decrease\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitGlobalWord(0xb1e8, 0x1);

		L0e69:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);

		L0e7a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe8b8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0e85;
			goto L0e8e;

		L0e85:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0e8e;
			goto L0e91;

		L0e8e:
			goto L1292;

		L0e91:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0e9a;
			goto L0ec9;

		L0e9a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ea3;
			goto L0ec9;

		L0ea3:
			this.processor.EmitLocalWord(0x16, 0x1);

			RenderCityTile(townIdx, 20, marker);

			this.processor.EmitLocalWord(0xee, this.processor.BitOrWord(this.processor.PullLocalWord(0xee), 0x0));
			this.processor.EmitLocalWord(0xec, this.processor.BitOrWord(this.processor.PullLocalWord(0xec), 0x10));
			this.processor.DecrementLocalWord(0x50);

		L0ec9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x50);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70da));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegData.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0efa;
			goto L0f0c;

		L0efa:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsLess) goto L0f0c;
			goto L1088;

		L0f0c:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe8b8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0f17;
			goto L1088;

		L0f17:
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0f21;
			goto L1088;

		L0f21:
			this.processor.EmitLocalWord(0x108, 0xffff);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xd6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xce, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L0f3f;

		L0f3b:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L0f3f:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0f4b;
			goto L103e;

		L0f4b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f5a;
			goto L103b;

		L0f5a:
			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 0);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 1);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xd4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0fcd;
			goto L0fe4;

		L0fcd:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0fd7;
			goto L0fe4;

		L0fd7:
			this.processor.CompareWord(this.processor.PullLocalWord(0xd4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0fe1;
			goto L0fe4;

		L0fe1:
			goto L0f3b;

		L0fe4:
			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 2);

			this.processor.EmitLocalWord(0xd4, this.processor.SumWord(this.processor.PullLocalWord(0xd4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xce);
			this.processor.CompareWord(this.processor.PullLocalWord(0xcc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1009;
			goto L1023;

		L1009:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xce);
			this.processor.CompareWord(this.processor.PullLocalWord(0xcc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1016;
			goto L103b;

		L1016:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xd4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L1023;
			goto L103b;

		L1023:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.processor.EmitLocalWord(0xce, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xd4, 0xd6);
			this.processor.CopyLocalWord(0xea, 0x108);

		L103b:
			goto L0f3b;

		L103e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1048;
			goto L1082;

		L1048:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x1);

			RenderCityTile(townIdx, this.processor.PullLocalWord(0x108), marker);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x108);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.EmitLocalWord(0xee, this.processor.BitOrWord(this.processor.PullLocalWord(0xee), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0xec, this.processor.BitOrWord(this.processor.PullLocalWord(0xec), this.processor.RegData.Value16));
			this.processor.DecrementLocalWord(0x50);
			goto L1085;

		L1082:
			goto L1088;

		L1085:
			goto L0ec9;

		L1088:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1094;
			goto L10aa;

		L1094:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L10a6;
			goto L10aa;

		L10a6:
			this.processor.EmitGlobalWord(0xe8b8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xe8b8)));

		L10aa:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe8b8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L10b5;
			goto L1292;

		L10b5:
			this.processor.EmitLocalWord(0x108, 0xffff);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xe2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xd6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xce, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L10d7;

		L10d3:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L10d7:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6540);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L10e3;
			goto L1248;

		L10e3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10f2;
			goto L1245;

		L10f2:
			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x70da) - (this.owner.World.Towns[townIdx].TrueExtent * 2) -
					(this.processor.PullLocalSignedWord(0x48) *
					this.processor.PullGlobalSignedWord(0xe3c6)), 
				1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x10;
			this.processor.RegData.Value16 = 0x0;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 0);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0xd4, this.processor.RegAcc.Value16);

			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 1);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0xc, this.processor.RegSource.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x70dc) - this.processor.PullGlobalSignedWord(0xd2f6),
				1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].TrueExtent);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0xd4, this.processor.SumWord(this.processor.PullLocalWord(0xd4), this.processor.RegAcc.Value16));

			MayCityWorkTile(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2), 2);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x46, this.processor.RegSource.Value16);

			this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70de), 1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0xd4, this.processor.SumWord(this.processor.PullLocalWord(0xd4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xd4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L1226;
			goto L1245;

		L1226:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd4);
			this.processor.EmitLocalWord(0xd6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xea, 0x108);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x46));
			this.processor.EmitLocalWord(0xe2, this.processor.RegAcc.Value16);

		L1245:
			goto L10d3;

		L1248:
			this.processor.CompareWord(this.processor.PullLocalWord(0x108), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1252;
			goto L128c;

		L1252:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x108);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 0x1);

			RenderCityTile(townIdx, this.processor.PullLocalWord(0x108), marker);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x108);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.EmitLocalWord(0xee, this.processor.BitOrWord(this.processor.PullLocalWord(0xee), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0xec, this.processor.BitOrWord(this.processor.PullLocalWord(0xec), this.processor.RegData.Value16));
			this.processor.DecrementLocalWord(0x50);
			goto L128f;

		L128c:
			goto L1292;

		L128f:
			goto L10aa;

		L1292:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x50);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = 0x1a;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xee));
			this.processor.RegData.Value16 = this.processor.SumCarryWord(this.processor.RegData.Value16, this.processor.PullLocalWord(0xec));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].LaborerMarkers0 = this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 = this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x50);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xae, this.processor.RegAcc.Value16);

		L12c2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xd8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x50, 0x8);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L12f9;

		L12f5:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L12f9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1305;
			goto L132c;

		L1305:
			ObtainCitizenType(this.processor.PullLocalWord(0xea));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1319;
			goto L1329;

		L1319:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 3);

		L1329:
			goto L12f5;

		L132c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L133a;

		L1336:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L133a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1344;
			goto L1357;

		L1344:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 0);

			goto L1336;

		L1357:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xac, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xf8, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6c98, 0x1);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4000);
			if (this.processor.FlagSet.IsNotEqual) goto L137b;
			goto L1381;

		L137b:
			this.processor.EmitLocalWord(0xf8, 0x2);

		L1381:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8000);
			if (this.processor.FlagSet.IsNotEqual) goto L1394;
			goto L139a;

		L1394:
			this.processor.EmitLocalWord(0xf8, 0x4);

		L139a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L13ad;
			goto L13b3;

		L13ad:
			this.processor.EmitLocalWord(0xac, 0x2);

		L13b3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L13c6;
			goto L13d2;

		L13c6:
			this.processor.EmitGlobalWord(0x6c98, 0x2);
			this.processor.EmitLocalWord(0xac, 0x2);

		L13d2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L13e5;
			goto L13f1;

		L13e5:
			this.processor.EmitGlobalWord(0x6c98, 0x2);
			this.processor.EmitLocalWord(0xac, 0x2);

		L13f1:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 15);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1409;
			goto L145f;

		L1409:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)this.owner.World.MarvelTownIdx[15]);
			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[15]].Place.X,
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[15]].Place.Y);

			this.processor.EmitLocalWord(0x10c, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10c));
			if (this.processor.FlagSet.IsEqual) goto L1453;
			goto L145f;

		L1453:
			this.processor.EmitGlobalWord(0x6c98, 0x2);
			this.processor.EmitLocalWord(0xac, 0x2);

		L145f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1472;
			goto L1478;

		L1472:
			this.processor.EmitGlobalWord(0x6c98, 0x3);

		L1478:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xac),
				0,
				this.processor.PullLocalSignedWord(0xf8));

			this.processor.EmitLocalWord(0xac, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitLocalWord(0x10a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xac);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x70dc));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xf8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x70dc));
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			this.processor.EmitGlobalWord(0x70dc, this.processor.SumWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L14de;
			goto L2673;

		L14de:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegData.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegData.Value16 = this.processor.SubtractWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			this.processor.RegData.Value16 = this.processor.SubtractWord(this.processor.RegData.Value16, this.processor.RegCount.Value16);
			this.owner.World.Towns[townIdx].GrainTally += (short)this.processor.RegData.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.EmitLocalWord(0xe8, this.processor.SubtractWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1522;
			goto L1528;

		L1522:
			this.processor.EmitLocalWord(0xe8, 0x0);

		L1528:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);

			if (this.owner.World.Towns[townIdx].PresentMakingIdx < 0)
				goto L1b63;

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Towns[townIdx].MaterialsTally);
			if (this.processor.FlagSet.IsLessOrEqual) goto L156a;
			goto L1b60;

		L156a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L157c;
			goto L159b;

		L157c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L158e;
			goto L159b;

		L158e:
			if (this.owner.World.HardnessTier == 0) goto L1598;
			goto L159b;

		L1598:
			goto L2307;

		L159b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.owner.World.Towns[townIdx].MaterialsTally -= (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0xba, 0xffff);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L15c8;
			goto L15da;

		L15c8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L15da;
			goto L1606;

		L15da:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.UnitOps.ConstructUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				this.owner.World.Towns[townIdx].PresentMakingIdx,
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);
			
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);

		L1606:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsByte((byte)this.owner.World.ScienceLeadingFoundVia[this.processor.RegAcc.Value16], 0x8);
			if (this.processor.FlagSet.IsEqual) goto L1626;
			goto L1664;

		L1626:
			this.owner.UnitOps.AttachReplayContent(6,
				this.processor.PullGlobalByte(0x6548),
				(byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.owner.World.ScienceLeadingFoundVia[this.processor.RegAcc.Value16] |= 8;

		L1664:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1676;
			goto L1697;

		L1676:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1680;
			goto L1697;

		L1680:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x20));

		L1697:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L16a1;
			goto L174b;

		L16a1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L16b3;
			goto L174b;

		L16b3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L16c5;
			goto L16d5;

		L16c5:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TownTally > 1) goto L16d5;

			goto L174b;

		L16d5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent--;
			if (this.processor.FlagSet.IsEqual) goto L16e6;
			goto L174b;

		L16e6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.Advisors.FinalizeProduction(
				townIdx,
				(ushort)((short)this.owner.World.Towns[townIdx].Place.X),
				(ushort)((short)this.owner.World.Towns[townIdx].Place.Y));

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.owner.UnitOps.ConstructUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				this.owner.World.Towns[townIdx].PresentMakingIdx,
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);
			
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);

			this.owner.NewGameWizard.EliminateCivilization(this.processor.PullGlobalSignedWord(0x6548), 0);

			this.owner.Startup.DisplayMouseCursor();

			goto L6927;

		L174b:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1757;
			goto L1ab6;

		L1757:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1761;
			goto L1ab6;

		L1761:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L1773;
			goto L1816;

		L1773:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L1789;

		L1786:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L1789:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L1793;
			goto L1802;

		L1793:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L17a9;
			goto L17ff;

		L17a9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L17bb;
			goto L17ff;

		L17bb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				goto L17ff;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc8));
			if (this.processor.FlagSet.IsGreater) goto L17e7;
			goto L17ff;

		L17e7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x4c, 0x108);

		L17ff:
			goto L1786;

		L1802:
			this.owner.Conquest.FoundTradeRoute(
				this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xba),
				this.processor.PullLocalSignedWord(0x108));

		L1816:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L1828;
			goto L1a77;

		L1828:
			this.processor.EmitLocalWord(0x108, 0xffff);
			this.processor.EmitLocalWord(0xc8, 0x7fff);
			this.processor.EmitLocalWord(0x106, 0x0);
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L1845;

		L1842:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L1845:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L184f;
			goto L19af;

		L184f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L1842;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1842;

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));

			this.owner.Helpers.LocateNearestUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xba),
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x3);
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6c9a, this.processor.RegAcc.Value16);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L18c8;
			goto L18ce;

		L18c8:
			this.processor.EmitGlobalWord(0x6c9a, 0x3);

		L18ce:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L18da;
			goto L18fe;

		L18da:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L18e6;
			goto L19ac;

		L18e6:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.PullLocalWord(0x106)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L18fe;
			goto L19ac;

		L18fe:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe8));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L192e;
			goto L19ac;

		L192e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe8));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x10c, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10c));
			if (this.processor.FlagSet.IsEqual) goto L1981;
			goto L19ac;

		L1981:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L198d;
			goto L1996;

		L198d:
			this.processor.EmitLocalWord(0x106, 0x1);
			goto L199d;

		L1996:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);

		L199d:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x4c, 0xb0);

		L19ac:
			goto L1842;

		L19af:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L19b9;
			goto L1a5c;

		L19b9:
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
				goto L1a5c;

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			this.owner.UnitOps.ConstructUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				0x1a,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1a0f;
			goto L1a59;

		L1a0f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xb0));

			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xb0)].Place.X);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), this.processor.RegCount.LowByte);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xb0));

			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xb0)].Place.Y);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81db), this.processor.RegCount.LowByte);

		L1a59:
			goto L1a77;

		L1a5c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegAcc.Value16;

		L1a77:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x19);
			if (this.processor.FlagSet.IsEqual) goto L1a89;
			goto L1aa8;

		L1a89:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LiveTroops[25] == 1)
			{
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TouchParticipantTimeleft = -1;
			}

		L1aa8:
			this.owner.Bot.PickNewCityProduction(townIdx);
			
			goto L1b60;

		L1ab6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1ace;
			goto L1b60;

		L1ace:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1ae0;
			goto L1af2;

		L1ae0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1af2;
			goto L1b60;

		L1af2:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " builds ");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x3);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

		L1b60:
			goto L220f;

		L1b63:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Towns[townIdx].MaterialsTally);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1b89;
			goto L220f;

		L1b89:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1ba6;
			goto L1beb;

		L1ba6:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.MarvelTownIdx[Math.Abs(this.processor.PullLocalSignedWord(0xe8)) - 24] != -1)
				goto L1be2;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.MarvelTownIdx[Math.Abs(this.processor.PullLocalSignedWord(0xe8)) - 24] = townIdx;

			this.owner.UnitOps.AttachReplayContent(10,
				this.processor.PullGlobalByte(0x6548),
				(byte)(this.processor.PullLocalWord(0xe8) - 0x18));

			goto L1be8;

		L1be2:
			this.processor.EmitLocalWord(0xe8, 0xffff);

		L1be8:
			goto L1c1d;

		L1beb:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe8);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c17;
			goto L1c1d;

		L1c17:
			this.processor.EmitLocalWord(0xe8, 0xffff);

		L1c1d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1c27;
			goto L220f;

		L1c27:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x29);
			if (this.processor.FlagSet.IsEqual) goto L1c31;
			goto L1c80;

		L1c31:
			this.owner.Libc.CopyText(0xba06, "Sensors report a\nNUCLEAR WEAPONS test\nnear ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Paper.DisplayNewspaper(-1);
			
			this.owner.Services.PerformSound(1, 0);

		L1c80:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x16);
			if (this.processor.FlagSet.IsLess) goto L1c8a;
			goto L1caf;

		L1c8a:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe8);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 |= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 |= this.processor.RegBase.Value16;

		L1caf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.owner.World.Towns[townIdx].MaterialsTally -= (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1ce2;
			goto L1cec;

		L1ce2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1cec;
			goto L1dfc;

		L1cec:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L1cfe;
			goto L1d07;

		L1cfe:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			goto L1d29;

		L1d07:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.World.ChartSight[this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y] = -1;

		L1d29:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " builds\n");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1d7c;
			goto L1de0;

		L1d7c:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L1d86;
			goto L1de0;

		L1d86:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x15);
			if (this.processor.FlagSet.IsGreater) goto L1d90;
			goto L1d9a;

		L1d90:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1d9a;
			goto L1de0;

		L1d9a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsEqual) goto L1dac;
			goto L1de0;

		L1dac:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1db6;
			goto L1de0;

		L1db6:
			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1dce;
			goto L1de0;

		L1dce:
			this.owner.CityScene.DisplayCityScreen(townIdx,
				this.processor.PullLocalSignedWord(0xe8));

			goto L1deb;

		L1de0:
			this.owner.Paper.DisplayNewspaper(townIdx);

		L1deb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			goto L1e60;

		L1dfc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1e06;
			goto L1e13;

		L1e06:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = -2;

		L1e13:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e42;
			goto L1e60;

		L1e42:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L1e60:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e6a;
			goto L2010;

		L1e6a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1e74;
			goto L2010;

		L1e74:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1e86;
			goto L2010;

		L1e86:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1e92;
			goto L1ea9;

		L1e92:
			this.owner.RocketScreen.AttachSpaceshipPart(
				this.processor.PullGlobalSignedWord(0x6548),
				(ushort)(this.processor.PullLocalWord(0xe8) - 22));
			
			goto L2010;

		L1ea9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x16);

			this.owner.RocketScreen.ComputeSpaceshipSuccess(this.processor.PullGlobalSignedWord(0x6548), (short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1ecb;
			goto L1f2f;

		L1ecb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsLess) goto L1ed5;
			goto L1f2f;

		L1ed5:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegCount.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xea);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1f05;
			goto L1f2b;

		L1f05:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1f17;
			goto L1f2b;

		L1f17:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.DecrementByte(this.processor.RegAcc.LowByte);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

		L1f2b:
			this.processor.EmitLocalWord(0xc8, this.processor.DecrementWord(this.processor.PullLocalWord(0xc8)));

		L1f2f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1f41;
			goto L1ffa;

		L1f41:
			if (this.owner.World.BotStarshipVictoryRatio >= 40) goto L1f4b;
			goto L1ffa;

		L1f4b:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1f55;
			goto L1feb;

		L1f55:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x28);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L1f6d;

			goto L1ff7;

		L1f6d:
			if (this.owner.World.BotStarshipVictoryRatio <= 75) goto L1f77;
			goto L1feb;

		L1f77:
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing >
				this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Standing) goto L1feb;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1fc5;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].StarshipArrivalSeason;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Season);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipArrivalSeason;
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, (ushort)this.owner.World.Season);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1feb;

		L1fc5:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1ff7;
			if (this.owner.World.Participants[this.owner.World.LiveRealms].Gold <= 1000) goto L1ff7;

		L1feb:
			this.owner.RocketScreen.FireSpaceship(this.processor.PullGlobalSignedWord(0x6548));

		L1ff7:
			goto L2010;

		L1ffa:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2004;
			goto L2010;

		L2004:
			this.owner.RocketScreen.FireSpaceship(this.processor.PullGlobalSignedWord(0x6548));

		L2010:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L201a;
			goto L212c;

		L201a:
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L2025;

		L2022:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L2025:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L202f;
			goto L205a;

		L202f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsEqual) goto L2045;
			goto L2057;

		L2045:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].StructureMarkers0 &= 0xfffe;
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].StructureMarkers1 &= 0xffff;

		L2057:
			goto L2022;

		L205a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2066;
			goto L207e;

		L2066:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullGlobalWord(0x6548)] & 0x40) == 0)
				goto L212c;

		L207e:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L208f;
			goto L20a5;

		L208f:
			this.owner.Libc.CopyText(0xba06, "Diplomats report:\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x1);

		L20a5:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " capital\nmoved to ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x5);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].HorizBegin = 
				(short)this.owner.World.Towns[townIdx].Place.X;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 |= 1;
			this.owner.World.Towns[townIdx].StructureMarkers1 |= 0;

		L212c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x26);
			if (this.processor.FlagSet.IsEqual) goto L2136;
			goto L2160;

		L2136:
			this.owner.Startup.DisplayMouseCursorFully();
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L2160:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x2b);
			if (this.processor.FlagSet.IsEqual) goto L216a;
			goto L220f;

		L216a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2176;
			goto L220f;

		L2176:
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L2181;

		L217e:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L2181:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L218b;
			goto L220f;

		L218b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L219d;
			goto L220c;

		L219d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L21b3;
			goto L220c;

		L21b3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].TrueExtent;
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.ChartSight[this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y] |= (sbyte)this.processor.RegCount.LowByte;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));

			this.owner.MapOps.KeepKnownTileImprovements(
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

		L220c:
			goto L217e;

		L220f:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L221b;
			goto L2307;

		L221b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L222d;
			goto L2307;

		L222d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L223f;
			goto L225b;

		L223f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L2251;
			goto L2307;

		L2251:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L225b;
			goto L2307;

		L225b:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L2281;
			goto L22fa;

		L2281:
			this.processor.EmitGlobalWord(0xb1e8, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2299;
			goto L22b7;

		L2299:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L22b7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xe8);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L22d9;
			goto L22f7;

		L22d9:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L22f7:
			goto L2307;

		L22fa:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

		L2307:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L2319;
			goto L2673;

		L2319:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L232b;
			goto L2336;

		L232b:
			this.owner.Bot.PickNewCityProduction(townIdx);

		L2336:
			this.processor.EmitLocalWord(0xcc, 0x0);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			// ??? Needs further investigation why this happens
			if (this.processor.RegAcc.Value16 >= this.owner.World.Participants.Length)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.RegAcc.Value16].Landmasses[this.processor.PullGlobalWord(0x6548)].Approach;
			}
			this.processor.EmitLocalWord(0x104, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L236a;
			goto L237e;

		L236a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2374;
			goto L237e;

		L2374:
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L237e;
			goto L23d3;

		L237e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2388;
			goto L23d3;

		L2388:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L239a;
			goto L23d3;

		L239a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x104);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L23b7;
			goto L23d3;

		L23b7:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x6);
			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L23d3:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L23e8;
			goto L2432;

		L23e8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L23ff;
			goto L2432;

		L23ff:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x18);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2416;
			goto L2432;

		L2416:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x7);
			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L2432:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2444;
			goto L24b3;

		L2444:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L2456;
			goto L24b3;

		L2456:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2468;
			goto L24b3;

		L2468:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,
						(ushort)(((-this.owner.World.Towns[townIdx].PresentMakingIdx) * 0x1e) + 0xbb2)) *
						this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 8);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L24b3:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L24c9;
			goto L24db;

		L24c9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L24db;
			goto L253c;

		L24db:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L24ed;
			goto L253c;

		L24ed:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L24ff;
			goto L253c;

		L24ff:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,
					(ushort)((0x22 * this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 3);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L253c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L254e;
			goto L259d;

		L254e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2560;
			goto L259d;

		L2560:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((0x22 *
					this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 3);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L259d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x19);
			if (this.processor.FlagSet.IsEqual) goto L25af;
			goto L2619;

		L25af:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LiveTroops[25] != 0)
				goto L2619;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25d4;
			goto L2619;

		L25d4:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,(ushort)((0x22 *
					this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 4);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L2619:
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, 0x7d0);
			if (this.processor.FlagSet.IsGreater) goto L262a;
			goto L2646;

		L262a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x9);
			this.processor.EmitLocalWord(0xcc, this.processor.SumWord(this.processor.PullLocalWord(0xcc), this.processor.RegAcc.Value16));

		L2646:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

		L2673:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L267f;
			goto L277e;

		L267f:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2687;
			goto L277e;

		L2687:
			RenderFrame(2, 67, 124, 104);

			RenderFrame(95, 106, 227, 197);

			RenderLabelBox(95, 106, 128, 114, 0x25ea, 9);

			RenderLabelBox(129, 106, 160, 114, 0x25ef, 9);

			RenderLabelBox(161, 106, 193, 114, 0x25f5, 9);

			RenderLabelBox(194, 106, 226, 114, 0x25f9, 9);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				(33 * this.processor.PullGlobalSignedWord(0x2496)) + 96, 107, 32, 7, 9, 15);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x2496);
			goto L2773;

		L2768:
			RenderMiniMap();
			goto L277e;

		L2773:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L277e;
			goto L2768;

		L277e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xd8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2e0, 0x0);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 3)
			{
				this.processor.EmitGlobalWord(0x6b30, 0xa);
			}

			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x50);
			this.processor.RegBase.Value16 = 0x3;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6b30));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L27f4;
			goto L2806;

		L27f4:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2806;
			goto L2812;

		L2806:
			this.processor.RegCount.Value16 = 0x2;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2e0);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

		L2812:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.EmitGlobalWord(0xd2e0, 0x0);
			}

			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x70de);
			this.processor.RegAcc.LowByte = this.processor.SubtractByte(this.processor.RegAcc.LowByte, this.processor.PullGlobalByte(0xd2e0));
			this.owner.World.Towns[townIdx].RootCommerce = (sbyte)this.processor.RegAcc.LowByte;
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L2848;

		L2844:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L2848:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L2852;
			goto L28d9;

		L2852:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].CommerceTownIndices[this.processor.PullLocalWord(0xbc)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L2871;
			goto L28d6;

		L2871:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L2887;
			goto L28b0;

		L2887:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.DivideAccumulatorByPowerOfTwo(0x3);
			this.processor.EmitGlobalWord(0x70de, this.processor.SumWord(this.processor.PullGlobalWord(0x70de), this.processor.RegAcc.Value16));
			goto L28d6;

		L28b0:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.EmitGlobalWord(0x70de, this.processor.SumWord(this.processor.PullGlobalWord(0x70de), this.processor.RegAcc.Value16));

		L28d6:
			goto L2844;

		L28d9:
			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x50);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6b30));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 3);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.EmitGlobalWord(0xd2e0, 0x0);
			}

			if ((this.owner.World.Towns[townIdx].StructureMarkers0 & 0x40) != 0)
			{
				this.processor.RegCount.Value16 = 0x2;
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2e0);
				this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
				this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
				this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);
			}

			this.owner.Helpers.VerifyValueSpan(
				((-(this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].ResearchLevyRatio +
					this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LevyRatio - 10) *
					(this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0))) + 5) / 10,
				0, this.processor.PullGlobalSignedWord(0x70de));

			this.processor.EmitGlobalWord(0x70e0, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				((this.owner.World.Participants[this.processor.PullGlobalSignedWord(0x6548)].LevyRatio *
					(this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0))) + 5) / 10,
				0,
				this.processor.PullGlobalSignedWord(0x70de) - 
					this.processor.PullGlobalSignedWord(0x70e0) -
					this.processor.PullGlobalSignedWord(0xd2e0));

			this.processor.EmitGlobalWord(0xe17a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e0));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe17a));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd2e0));
			this.processor.EmitGlobalWord(0x70e6, this.processor.RegAcc.Value16);

			TallySpecialists(1);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

			TallySpecialists(2);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0x70e6, this.processor.SumWord(this.processor.PullGlobalWord(0x70e6), this.processor.RegAcc.Value16));

			TallySpecialists(3);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L29fe;
			goto L2a16;

		L29fe:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

		L2a16:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x200);
			if (this.processor.FlagSet.IsNotEqual) goto L2a29;
			goto L2a41;

		L2a29:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

		L2a41:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L2a5a;
			goto L2a8b;

		L2a5a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xc);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2a7e;
			goto L2a8b;

		L2a7e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2a8b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x800);
			if (this.processor.FlagSet.IsNotEqual) goto L2a9e;
			goto L2acf;

		L2a9e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xc);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac2;
			goto L2acf;

		L2ac2:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2acf:
			ObtainWonderCityID(10);

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L2ae3;
			goto L2aeb;

		L2ae3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2aeb:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitGlobalWord(0x70e6, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x70e2, 0x0);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2b04;
			goto L2b79;

		L2b04:
			this.processor.RegAcc.Value16 = (ushort)owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xe);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.HalveAccumulator();
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegData.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegData.Value16;
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TownTally;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.HardnessTier);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa8, this.processor.RegAcc.Value16);
			goto L2b90;

		L2b79:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa8, this.processor.RegAcc.Value16);

		L2b90:
			this.processor.EmitGlobalWord(0x6542, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			if (this.processor.FlagSet.IsLess) goto L2bac;
			goto L2bd3;

		L2bac:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6542, this.processor.RegCount.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);

		L2bd3:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bed;
			goto L2c3d;

		L2bed:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2bf5;
			goto L2c3d;

		L2bf5:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bff;
			goto L2c3d;

		L2bff:
			this.processor.EmitLocalWord(0x42, 0x74);

			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2c3d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e2, this.processor.RegAcc.Value16);

			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c62;
			goto L2cce;

		L2c62:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2c6a;
			goto L2cce;

		L2c6a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c74;
			goto L2cce;

		L2c74:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2c7e;
			goto L2cce;

		L2c7e:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				208,
				this.processor.PullLocalSignedWord(0x42) + 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xe << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2cce:
			this.processor.EmitLocalWord(0x6, 0xd0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2000);
			if (this.processor.FlagSet.IsNotEqual) goto L2ce6;
			goto L2d1f;

		L2ce6:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x3));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2cf7;
			goto L2d1f;

		L2cf7:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2cff;
			goto L2d1f;

		L2cff:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d09;
			goto L2d1f;

		L2d09:
			RenderResourceIcon(14, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2d1f:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1d);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2d37;

			goto L2dbc;

		L2d37:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x400);
			if (this.processor.FlagSet.IsNotEqual) goto L2d4a;
			goto L2d6e;

		L2d4a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 9);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2d62;
			goto L2d68;

		L2d62:
			this.processor.RegAcc.Value16 = 0x6;
			goto L2d6b;

		L2d68:
			this.processor.RegAcc.Value16 = 0x4;

		L2d6b:
			goto L2d71;

		L2d6e:
			this.processor.RegAcc.Value16 = 0x0;

		L2d71:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d81;
			goto L2dbc;

		L2d81:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2d89;
			goto L2dbc;

		L2d89:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d93;
			goto L2dbc;

		L2d93:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x400);
			if (this.processor.FlagSet.IsNotEqual) goto L2da6;
			goto L2dbc;

		L2da6:
			RenderResourceIcon(11, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2dbc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L2dce;
			goto L2e7b;

		L2dce:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1a);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2de6;

			goto L2dee;

		L2de6:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x2));
			goto L2e0a;

		L2dee:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x19);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2e06;

			goto L2e0a;

		L2e06:
			this.processor.EmitGlobalWord(0x70e4, this.processor.DecrementWord(this.processor.PullGlobalWord(0x70e4)));

		L2e0a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 6);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2e22;
			goto L2e47;

		L2e22:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1a);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2e3a;

			goto L2e40;

		L2e3a:
			this.processor.RegAcc.Value16 = 0x2;
			goto L2e43;

		L2e40:
			this.processor.RegAcc.Value16 = 0x1;

		L2e43:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));

		L2e47:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e53;
			goto L2e7b;

		L2e53:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e5b;
			goto L2e7b;

		L2e5b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e65;
			goto L2e7b;

		L2e65:
			RenderResourceIcon(4, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2e7b:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e95;
			goto L2ef3;

		L2e95:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e9d;
			goto L2ef3;

		L2e9d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2ea7;
			goto L2ef3;

		L2ea7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2408);
			if (this.processor.FlagSet.IsNotEqual) goto L2eba;
			goto L2ef3;

		L2eba:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2ef3:
			this.processor.EmitLocalWord(0x6, 0xd0);
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind < 4) goto L2f08;
			goto L3088;

		L2f08:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe6, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);

		L2f2a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2f34;
			goto L2ffa;

		L2f34:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x20);
			if (this.processor.FlagSet.IsLess) goto L2f40;
			goto L2ffa;

		L2f40:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f64;
			goto L2fc9;

		L2f64:
			this.processor.EmitLocalWord(0xe6, this.processor.IncrementWord(this.processor.PullLocalWord(0xe6)));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2f74;
			goto L2fc9;

		L2f74:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2f7c;
			goto L2fc9;

		L2f7c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2f86;
			goto L2fc9;

		L2f86:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe6), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2f90;
			goto L2fc9;

		L2f90:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42) - 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
						(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
					(ushort)((0x600 * this.processor.PullGlobalWord(0x6548)) +
						(0xc * this.processor.PullLocalWord(0xba)) + 0x81d7)) +
					(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x2));

		L2fc9:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2ff1;
			goto L2ff7;

		L2ff1:
			this.processor.EmitLocalWord(0xba, 0xffff);

		L2ff7:
			goto L2f2a;

		L2ffa:
			this.processor.EmitLocalWord(0xba, 0x0);
			goto L3007;

		L3003:
			this.processor.EmitLocalWord(0xba, this.processor.IncrementWord(this.processor.PullLocalWord(0xba)));

		L3007:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0x2);
			if (this.processor.FlagSet.IsLess) goto L3011;
			goto L305d;

		L3011:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L301b;
			goto L305a;

		L301b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xba)], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L3031;
			goto L305a;

		L3031:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xba)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3f);
			this.processor.RegCount.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3056;
			goto L305a;

		L3056:
			this.processor.EmitLocalWord(0xe6, this.processor.IncrementWord(this.processor.PullLocalWord(0xe6)));

		L305a:
			goto L3003;

		L305d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe6), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L3067;
			goto L306d;

		L3067:
			this.processor.EmitLocalWord(0xe6, 0x3);

		L306d:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xe6),
				0, this.processor.PullGlobalSignedWord(0x70e4));

			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			goto L3126;

		L3088:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegCount.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.IncrementLocalWord(0xe8);
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L30c1;
			goto L3126;

		L30c1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.EmitGlobalWord(0x70e4, this.processor.SumWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L30d9;
			goto L3126;

		L30d9:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L30e1;
			goto L3126;

		L30e1:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L30eb;
			goto L3126;

		L30eb:
			this.processor.EmitLocalWord(0xba, 0x0);
			goto L30f8;

		L30f4:
			this.processor.EmitLocalWord(0xba, this.processor.IncrementWord(this.processor.PullLocalWord(0xba)));

		L30f8:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xba));
			if (this.processor.FlagSet.IsGreater) goto L3109;
			goto L3126;

		L3109:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42) + 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x2));
			goto L30f4;

		L3126:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3140;
			goto L318b;

		L3140:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3148;
			goto L318b;

		L3148:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3152;
			goto L318b;

		L3152:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L318b:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 2);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31ae;
			goto L31b2;

		L31ae:
			this.processor.EmitGlobalWord(0x70e2, this.processor.IncrementWord(this.processor.PullGlobalWord(0x70e2)));

		L31b2:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0x15);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31ca;
			goto L31ce;

		L31ca:
			this.processor.EmitGlobalWord(0x70e2, this.processor.IncrementWord(this.processor.PullGlobalWord(0x70e2)));

		L31ce:
			ObtainWonderCityID(11);

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L31e2;
			goto L31e8;

		L31e2:
			this.processor.EmitGlobalWord(0x70e4, 0x0);

		L31e8:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xd);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3200;
			goto L323d;

		L3200:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)this.owner.World.MarvelTownIdx[13]);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[13]].Place.X,
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[13]].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3238;
			goto L323d;

		L3238:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x2));

		L323d:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3257;
			goto L32cd;

		L3257:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L325f;
			goto L32cd;

		L325f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3269;
			goto L32cd;

		L3269:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe8));
			if (this.processor.FlagSet.IsNotEqual) goto L3279;
			goto L32cd;

		L3279:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.owner.TextPainter.RenderTextToScreen0(0x25fe, 190, this.processor.PullLocalSignedWord(0x42) + 5, 15);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L32cd:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L32d7;
			goto L32e0;

		L32d7:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x70e2, this.processor.RegAcc.Value16);

		L32e0:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeb6, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L32fc;

		L32f8:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L32fc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x2);
			if (this.processor.FlagSet.IsLess) goto L3306;
			goto L3323;

		L3306:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xea)], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L331c;
			goto L3320;

		L331c:
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));

		L3320:
			goto L32f8;

		L3323:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsLess) goto L3339;
			goto L3350;

		L3339:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegCount.Value16);

		L3350:
			this.processor.EmitLocalWord(0xf6, 0x8);
			this.processor.EmitLocalWord(0xfc, 0x45);
			this.processor.EmitLocalWord(0xfa, 0x64);
			this.processor.EmitLocalWord(0x100, 0x74);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L3375;

		L3371:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L3375:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3380;
			goto L39ef;

		L3380:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L339c;
			goto L38d4;

		L339c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L33bb;
			goto L38d4;

		L33bb:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L33d7;
			goto L340f;

		L33d7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L33f1;
			goto L340b;

		L33f1:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind > 1) goto L3401;
			goto L340f;

		L3401:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L340b;
			goto L340f;

		L340b:
			this.processor.EmitGlobalWord(0xd2f6, this.processor.IncrementWord(this.processor.PullGlobalWord(0xd2f6)));

		L340f:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L341a;
			goto L3672;

		L341a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3426;
			goto L3463;

		L3426:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3438;
			goto L3672;

		L3438:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsEqual) goto L3447;
			goto L3672;

		L3447:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3453;
			goto L3672;

		L3453:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L3463;
			goto L3672;

		L3463:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x106, 0x0);
			goto L347b;

		L3477:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));

		L347b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3486;
			goto L353f;

		L3486:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L34a2;
			goto L353c;

		L34a2:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L34c1;
			goto L353c;

		L34c1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L34dd;
			goto L353c;

		L34dd:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x106));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.Helpers.ObtainDistance(
				this.owner.World.Towns[townIdx].Place.X,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.owner.World.Towns[townIdx].Place.Y,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xb2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xb2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L352c;
			goto L353c;

		L352c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb2);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x106, 0x108);

		L353c:
			goto L3477;

		L353f:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L354b;
			goto L35e1;

		L354b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8078), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3555;
			goto L3577;

		L3555:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally < 
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally)
				goto L356e;

			goto L3577;

		L356e:
			this.processor.EmitGlobalWord(0xdb42, 0xfc19);
			goto L35de;

		L3577:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3581;
			goto L35de;

		L3581:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L35a5;
			goto L35de;

		L35a5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L35c1;
			goto L35de;

		L35c1:
			this.owner.UnitOps.EliminateUnit(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0x108));
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xfe;

		L35de:
			goto L3672;

		L35e1:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L35ed;
			goto L365f;

		L35ed:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " can't support\n");

			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n Unit Disbanded.\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x3);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L365f:
			this.owner.UnitOps.EliminateUnit(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0x108));
			
			goto L32e0;

		L3672:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L367e;
			goto L38d4;

		L367e:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3686;
			goto L38d4;

		L3686:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L36a2;
			goto L384a;

		L36a2:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2f6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L36ac;
			goto L36c8;

		L36ac:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));

		L36c8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L36e4;
			goto L372a;

		L36e4:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 2)
			{
				this.owner.Services.RenderPictureToScreen(
					this.processor.PullGlobalWord(0xaa),
					this.processor.PullLocalSignedWord(0xf6) + 2,
					this.processor.PullLocalSignedWord(0xfc) + 12,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
			}
			goto L384a;

		L372a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegCount.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.IncrementLocalWord(0xe8);
			}

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L3769;
			goto L384a;

		L3769:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L378d;
			goto L384a;

		L378d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3797;
			goto L384a;

		L3797:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L37bb;
			goto L380d;

		L37bb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.X);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L37e4;
			goto L380d;

		L37e4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L380d;
			goto L384a;

		L380d:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L382f;
			goto L384a;

		L382f:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6) + 2,
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

		L384a:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
						(ushort)((0x600 * this.processor.PullGlobalWord(0x6548)) +
							(0xc * this.processor.PullLocalWord(0xea)) + 0x81d7)) +
						(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			RenderMiniMapDot(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)), 7);

			this.processor.EmitLocalWord(0xf6, this.processor.SumWord(this.processor.PullLocalWord(0xf6), 0x10));
			this.processor.CompareWord(this.processor.PullLocalWord(0xf6), 0x70);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L38ba;
			goto L38d4;

		L38ba:
			this.processor.EmitLocalWord(0xf6, 0x8);
			this.processor.EmitLocalWord(0xfc, this.processor.SumWord(this.processor.PullLocalWord(0xfc), 0x10));
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x55);
			if (this.processor.FlagSet.IsGreater) goto L38cf;
			goto L38d4;

		L38cf:
			this.processor.EmitLocalWord(0xfc, this.processor.SubtractWord(this.processor.PullLocalWord(0xfc), 0x18));

		L38d4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x12);
			if (this.processor.FlagSet.IsLess) goto L38dd;
			goto L39ec;

		L38dd:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L38e7;
			goto L39ec;

		L38e7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L3903;
			goto L39ec;

		L3903:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L390f;
			goto L39ec;

		L390f:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3917;
			goto L39ec;

		L3917:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xd8));
			if (this.processor.FlagSet.IsEqual) goto L3938;
			goto L39ec;

		L3938:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe4));
			if (this.processor.FlagSet.IsEqual) goto L3959;
			goto L39ec;

		L3959:
			this.owner.MapOps.RenderUnitSprite(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xea),
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100));

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));

			this.owner.Conquest.ObtainCityLabel(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df)));
			
			this.processor.EmitGlobalByte(0xba09, 0x2e);
			this.processor.EmitGlobalByte(0xba0a, 0x0);

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100) + 15,
				0);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.IncrementLocalWord(0x4e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xfa, this.processor.SumWord(this.processor.PullLocalWord(0xfa), 0x12));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L39e1;
			goto L39ec;

		L39e1:
			this.processor.EmitLocalWord(0xfa, 0x64);
			this.processor.EmitLocalWord(0x100, this.processor.SumWord(this.processor.PullLocalWord(0x100), 0x10));

		L39ec:
			goto L3371;

		L39ef:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L39fa;
			goto L3a72;

		L39fa:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L3a06;
			goto L3a16;

		L3a06:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70dc));
			this.processor.RegCount.Value16 = 0x5;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xe3c2, this.processor.SumWord(this.processor.PullGlobalWord(0xe3c2), this.processor.RegAcc.Value16));

		L3a16:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0xdeb8),
				0, this.owner.World.Towns[townIdx].TrueExtent);

			this.processor.EmitGlobalWord(0xe3c2, this.processor.SumWord(this.processor.PullGlobalWord(0xe3c2), this.processor.RegAcc.Value16));
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L3a4a;
			goto L3a50;

		L3a4a:
			this.processor.RegAcc.Value16 = 0x5;
			goto L3a53;

		L3a50:
			this.processor.RegAcc.Value16 = 0x7;

		L3a53:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.EmitGlobalWord(0xdb42, this.processor.SubtractWord(this.processor.PullGlobalWord(0xdb42), this.processor.RegAcc.Value16));

		L3a72:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3a7e;
			goto L5cd0;

		L3a7e:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3a86;
			goto L5cd0;

		L3a86:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);

			RenderFrame(211, 1, 317, 97);

			this.processor.RegAcc.Value16 = 0x2;
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xda), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3ac6;
			goto L3b80;

		L3ac6:
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L3ad3;

		L3acf:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L3ad3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x15);
			if (this.processor.FlagSet.IsLess) goto L3add;
			goto L3b80;

		L3add:
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0xbc) + 1], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3aef;
			goto L3b7d;

		L3aef:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 63);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 253, this.processor.PullLocalSignedWord(0xe4) + 2, 15);

			RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0xe8) + 1),
				(short)(((this.processor.PullLocalByte(0x106) & 1) != 0) ? 213 : 233),
				(short)(this.processor.PullLocalSignedWord(0xe4) - 2));

			this.processor.EmitLocalWord(0xe4, this.processor.SumWord(this.processor.PullLocalWord(0xe4), 0x6));
			this.processor.IncrementLocalWord(0x106);
			this.processor.IncrementLocalWord(0x40);

		L3b7d:
			goto L3acf;

		L3b80:
			this.processor.EmitLocalWord(0xfe, 0x0);
			this.processor.CopyLocalWord(0xda, 0xbc);
			goto L3b95;

		L3b91:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L3b95:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L3b9f;
			goto L3c83;

		L3b9f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, 
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3bc9;
			goto L3c80;

		L3bc9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), 0x10);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3bd3;
			goto L3be9;

		L3bd3:
			this.processor.EmitLocalWord(0xca, this.processor.BitOrWord(this.processor.PullLocalWord(0xca), 0x2));
			this.processor.EmitLocalWord(0xca, this.processor.BitAndWord(this.processor.PullLocalWord(0xca), 0xfffe));
			this.processor.CopyLocalWord(0xbc, 0xfe);
			goto L3c83;

		L3be9:
			this.processor.EmitLocalWord(0xb4, this.processor.IncrementWord(this.processor.PullLocalWord(0xb4)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				309,
				this.processor.PullLocalSignedWord(0xe4) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0xb << 1) + 0xd4ce)));

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 56);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 253, this.processor.PullLocalSignedWord(0xe4) + 2, 15);

			RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0xbc) + 1),
				(short)(((this.processor.PullLocalByte(0x106) & 1) != 0) ? 213 : 233),
				(short)(this.processor.PullLocalSignedWord(0xe4) - 2));

			this.processor.EmitLocalWord(0xe4, this.processor.SumWord(this.processor.PullLocalWord(0xe4), 0x6));
			this.processor.IncrementLocalWord(0x106);

		L3c80:
			goto L3b91;

		L3c83:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				231, 0, 250, 0, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				231, 1, 250, 1, 1);

			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L3cc5;
			goto L3ce5;

		L3cc5:
			RenderLabelBox(287, 88, 315, 96, 0x262a, 9);

		L3ce5:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 309, 2, 8, 96, 14, 12);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3d1b;
			goto L3d36;

		L3d1b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.EmitLocalWord(0xde, this.processor.RegAcc.Value16);
			goto L3d54;

		L3d36:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.EmitLocalWord(0xde, this.processor.RegAcc.Value16);

		L3d54:
			this.owner.Helpers.VerifyValueSpan(
				((this.processor.PullLocalSignedWord(0xde) - 1) / 10) + 1,
				((this.owner.World.Towns[townIdx].MaterialsTally - 1) / 100) + 1, 99);

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xe8));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.RegData.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xde);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				230, 99,
				this.processor.PullLocalSignedWord(0xf6) + 3,
				this.processor.PullLocalSignedWord(0xfc) + 19,
				1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3e01;
			goto L3e59;

		L3e01:
			RenderLabelBox(231, 106, 263, 114, (ushort)(((this.owner.World.Towns[townIdx].ConditionMarker & 0x10) != 0) ? 0x262f : 0x2635), 9);

			RenderLabelBox(294, 106, 311, 114, 0x263c, 9);

		L3e59:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3e6b;
			goto L3e99;

		L3e6b:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				264, 100,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((byte)this.owner.World.Towns[townIdx].PresentMakingIdx +
						(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));
			goto L3ee5;

		L3e99:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 0x56);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 274, 100, 15);

		L3ee5:
			FloodBoxWithMotif(231, 116,
				this.processor.PullLocalSignedWord(0xf6) + 1,
				this.processor.PullLocalSignedWord(0xfc) + 1);

			this.processor.EmitLocalWord(0xea, 0x0);
			goto L3f0e;

		L3f0a:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L3f0e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L3f23;
			goto L3f60;

		L3f23:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(((this.processor.PullLocalSignedWord(0xea) %
					(this.processor.PullLocalSignedWord(0x4a) *
						this.processor.PullLocalSignedWord(0x44))) *
					this.processor.PullLocalSignedWord(0xe8)) /
					this.processor.PullLocalSignedWord(0x44)) + 232,
				((this.processor.PullLocalSignedWord(0xea) /
					(this.processor.PullLocalSignedWord(0x4a) *
					this.processor.PullLocalSignedWord(0x44))) * 8) + 117,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
			goto L3f0a;

		L3f60:
			if (this.owner.World.Towns[townIdx].TrueExtent != 0)
			{
				this.owner.Helpers.VerifyValueSpan(80 / this.owner.World.Towns[townIdx].TrueExtent, 1, 8);
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				2, 106, 91,
				(this.processor.PullLocalSignedWord(0x4a) *
					this.processor.PullLocalSignedWord(0xfc)) + 12,
				1);

			this.owner.TextPainter.RenderTextToScreen0(0x2640, 8, 108, 15);

			FloodBoxWithMotif(3, 115,
				(this.owner.World.Towns[townIdx].TrueExtent * this.processor.PullLocalSignedWord(0xe8)) + 9,
				(this.processor.PullLocalSignedWord(0x4a) * this.processor.PullLocalSignedWord(0xfc)) + 2);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L4016;
			goto L4043;

		L4016:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				5, 155, 
				(this.owner.World.Towns[townIdx].TrueExtent * this.processor.PullLocalSignedWord(0xe8)) + 9, 155, 1);

		L4043:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4050;

		L404c:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4050:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Towns[townIdx].GrainTally,
				0, (this.owner.World.Towns[townIdx].TrueExtent + 1) *
					this.processor.PullLocalSignedWord(0x4a));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsGreater) goto L407b;
			goto L40ba;

		L407b:
			this.processor.RegSource.Value16 = (ushort)(this.owner.World.Towns[townIdx].TrueExtent + 1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				((this.processor.PullLocalSignedWord(0xea) % (short)this.processor.RegSource.Value16) *
					this.processor.PullLocalSignedWord(0xe8)) + 4,
				((this.processor.PullLocalSignedWord(0xea) / (short)this.processor.RegSource.Value16) *
					this.processor.PullLocalSignedWord(0xfc)) + 116,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0x8 << 1) + 0xd4ce)));
			goto L404c;

		L40ba:
			RenderFrame(2, 23, 124, 65);

			this.processor.EmitLocalWord(0xfc, 0x19);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 2, 23, 122, 9, 1);

			this.owner.TextPainter.RenderTextToScreen0(0x264d, 8, this.processor.PullLocalSignedWord(0xfc), 15);

			this.processor.EmitLocalWord(0xfc, this.processor.SumWord(this.processor.PullLocalWord(0xfc), 0x8));
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L4122;

		L411e:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L4122:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L412c;
			goto L427d;

		L412c:
			this.processor.EmitLocalWord(0xd8, 0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L413c;
			goto L4190;

		L413c:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0xbc) << 1) + 0x70da)),
				(this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) * 
					this.processor.PullGlobalSignedWord(0xe3c6)), 999);

			this.owner.Helpers.VerifyValueSpan(116 / ((short)this.processor.RegAcc.Value16 + 1), 1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			goto L41b8;

		L4190:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullInt16(this.processor.SegData.Value16, 
					(ushort)((this.processor.PullLocalWord(0xbc) << 1) + 0x70da)) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

		L41b8:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L41c5;

		L41c1:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L41c5:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L41d8;
			goto L427a;

		L41d8:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L41e2;
			goto L420a;

		L41e2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsEqual) goto L4205;
			goto L420a;

		L4205:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x4));

		L420a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L4214;
			goto L422f;

		L4214:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2f6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L421e;
			goto L422f;

		L421e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L422a;
			goto L422f;

		L422a:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x4));

		L422f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4239;
			goto L424e;

		L4239:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd2e0));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsEqual) goto L4249;
			goto L424e;

		L4249:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x2));

		L424e:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8),
				(this.processor.PullLocalSignedWord(0xbc) * 8) +
					this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0xbc) + 8) << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L41c1;

		L427a:
			goto L411e;

		L427d:
			this.processor.EmitLocalWord(0xd8, 0x8);

			this.owner.Helpers.VerifyValueSpan(
				224 / (this.processor.PullGlobalSignedWord(0x70e0) + 
					this.processor.PullGlobalSignedWord(0x70e6) +
					this.processor.PullGlobalSignedWord(0xe17a) + 
					this.processor.PullGlobalSignedWord(0xd2e0) + 2),
				1, 16);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L42bf;

		L42bb:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L42bf:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L42cb;
			goto L42f4;

		L42cb:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xe << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L42bb;

		L42f4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L42fe;
			goto L4303;

		L42fe:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x8));

		L4303:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd8);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4318;

		L4314:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4318:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4324;
			goto L434d;

		L4324:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xb << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L4314;

		L434d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xe17a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4357;
			goto L435c;

		L4357:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x8));

		L435c:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4369;

		L4365:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4369:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4375;
			goto L439e;

		L4375:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xc << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L4365;

		L439e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullGlobalWord(0x70da));
			if (this.processor.FlagSet.IsGreater) goto L43c1;
			goto L4497;

		L43c1:
			this.owner.Helpers.VerifyValueSpan(
				116 / ((this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) *
					this.processor.PullGlobalSignedWord(0xe3c6)) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L4407;

		L4403:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4407:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsGreater) goto L442a;
			goto L4449;

		L442a:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
			goto L4403;

		L4449:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xe8) * this.processor.PullGlobalSignedWord(0x70da)) + 8,
				this.processor.PullLocalSignedWord(0xfc),
				(((this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) * this.processor.PullGlobalSignedWord(0xe3c6)) -
					this.processor.PullGlobalSignedWord(0x70da)) * this.processor.PullLocalSignedWord(0xe8)) + 4, 
				8, 15, 0);

		L4497:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L44a3;
			goto L453a;

		L44a3:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullGlobalSignedWord(0x70dc) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L44d3;

		L44cf:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L44d3:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L44df;
			goto L4502;

		L44df:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
			goto L44cf;

		L4502:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullGlobalSignedWord(0x70dc) * this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 8,
				(this.processor.PullGlobalSignedWord(0xd2f6) - this.processor.PullGlobalSignedWord(0x70dc)) * 
					this.processor.PullLocalSignedWord(0xe8),
				8, 15, 0);

		L453a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2e0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4544;
			goto L45a0;

		L4544:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullGlobalSignedWord(0x70de) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				((this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0)) *
					this.processor.PullLocalSignedWord(0xe8)) + 6,
				this.processor.PullLocalSignedWord(0xfc) + 16,
				(this.processor.PullLocalSignedWord(0xe8) * this.processor.PullGlobalSignedWord(0xd2e0)) + 2,
				8, 15, 0);

		L45a0:
			FloodBoxWithMotif(8, 8, 200, 13);

			RenderCitizenIcons(townIdx, 8, 8, this.processor.PullLocalWord(0x8), 192);

			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan((this.owner.World.Towns[townIdx].TrueExtent * 8) + 24, 0, 128);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L460c;

		L4608:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L460c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L4616;
			goto L4770;

		L4616:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].CommerceTownIndices[this.processor.PullLocalWord(0xbc)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L4635;
			goto L476d;

		L4635:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x4c));

			this.owner.Libc.AppendText(0xba06, ":+");

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L466b;
			goto L46bb;

		L466b:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(((short)this.owner.World.Towns[townIdx].RootCommerce +
					(short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce + 4) >> 3),
					10));
			goto L4708;

		L46bb:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(((short)this.owner.World.Towns[townIdx].RootCommerce +
					(short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce + 4) >> 4),
					10));

		L4708:
			this.owner.Libc.AppendText(0xba06, "} ");

			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4722;
			goto L4744;

		L4722:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));

			RenderMiniMapDot((ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X),
				(ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y), 10);

		L4744:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L474e;
			goto L476d;

		L474e:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 98,
				(this.processor.PullLocalSignedWord(0xbc) * 6) + 179, 10);

		L476d:
			goto L4608;

		L4770:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L477a;
			goto L479d;

		L477a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullGlobalWord(0x653e));

			RenderMiniMapDot((ushort)((short)this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].Place.X),
				(ushort)((short)this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].Place.Y), 15);

		L479d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L47a7;
			goto L4852;

		L47a7:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0xe0, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0xe0, this.processor.SumWord(this.processor.PullLocalWord(0xe0), this.processor.RegAcc.Value16));

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xe0), 1, 99);

			this.owner.Helpers.VerifyValueSpan(100 / (short)this.processor.RegAcc.Value16, 1, 8);

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L481b;

		L4817:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L481b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4828;
			goto L4852;

		L4828:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xfc)) + 98,
				(this.processor.PullLocalWord(0xea) & 1) + 161,
				this.processor.PullGlobalWord(0xb2ba));
			goto L4817;

		L4852:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L485e;
			goto L5cbd;

		L485e:
			RenderLabelBox(284, 190, 316, 198, 0x2662, 12);

			RenderLabelBox(231, 190, 272, 198, 0x2667, 9);

			this.processor.EmitLocalWord(0xc0, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L48b6;
			goto L48fb;

		L48b6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xeb);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L48c8;
			goto L48fb;

		L48c8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegData.Value16 = this.processor.BitAndWord(this.processor.RegData.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L48f5;
			goto L48fb;

		L48f5:
			this.processor.EmitLocalWord(0xc0, 0x1);

		L48fb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xeb);
			if (this.processor.FlagSet.IsLess) goto L490d;
			goto L4937;

		L490d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L491f;
			goto L4937;

		L491f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L4931;
			goto L4937;

		L4931:
			this.processor.EmitLocalWord(0xc0, 0x1);

		L4937:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsLess) goto L4949;
			goto L496c;

		L4949:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.NegateWord(this.processor.RegBase.Value16);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.MarvelTownIdx[Math.Abs(this.owner.World.Towns[townIdx].PresentMakingIdx) - 24] == -1)
				goto L496c;

			this.processor.EmitLocalWord(0xc0, 0x1);

		L496c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L497e;
			goto L4994;

		L497e:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopTally >= 127)
			{
				this.processor.EmitLocalWord(0xc0, 0x1);
			}

		L4994:
			this.owner.Startup.DisplayMouseCursor();

		L4999:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L49a3;
			goto L49ab;

		L49a3:
			this.owner.Startup.ObtainMouseState();

			goto L4999;

		L49ab:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49b5;
			goto L4a4e;

		L49b5:
			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49c2;
			goto L4a4e;

		L49c2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49cc;
			goto L4a4e;

		L49cc:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.processor.PullLocalWord(0xc0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L49db;
			goto L4a4b;

		L49db:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.IncrementLocalWord(0xc0);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xc0);
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L49ef;
			goto L4a16;

		L49ef:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 231, 106, 32, 8, 14, 9);

			goto L4a3a;

		L4a16:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 231, 106, 32, 8, 9, 14);

		L4a3a:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TextPainter.AwaitTime(10);

		L4a4b:
			goto L49ab;

		L4a4e:
			this.processor.EmitLocalWord(0xf0, 0xffff);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4a61;
			goto L4a76;

		L4a61:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xf0, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x0;
			this.owner.GlobDb3e = 0;
			this.owner.GlobDb3c = 0;
			goto L4a86;

		L4a76:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4a80;
			goto L4a86;

		L4a80:
			this.processor.EmitLocalWord(0xf0, 0x70);

		L4a86:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4a95;
			goto L4abf;

		L4a95:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4aa0;
			goto L4abf;

		L4aa0:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsLess) goto L4aab;
			goto L4abf;

		L4aab:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x6a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4ab5;
			goto L4abf;

		L4ab5:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsGreater) goto L4abf;
			goto L4ac9;

		L4abf:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x41);
			if (this.processor.FlagSet.IsEqual) goto L4ac9;
			goto L4bba;

		L4ac9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker ^= 0x10;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L4ae8;
			goto L4baa;

		L4ae8:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.EmitLocalWord(0x102, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x102), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L4b0e;
			goto L4ba7;

		L4b0e:
			this.processor.EmitGlobalWord(0xb1e8, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4b26;
			goto L4b44;

		L4b26:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L4b44:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x102);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4b66;
			goto L4b84;

		L4b66:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L4b84:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 230, 99, 90, 100, 0);

			goto L4baa;

		L4ba7:
			goto L045f;

		L4baa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L4bba:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L4bc4;
			goto L4be2;

		L4bc4:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L4bce;
			goto L4ff5;

		L4bce:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x6a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4bd8;
			goto L4ff5;

		L4bd8:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsLessOrEqual) goto L4be2;
			goto L4ff5;

		L4be2:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x128);
			if (this.processor.FlagSet.IsLess) goto L4bed;
			goto L4bf7;

		L4bed:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x62);
			if (this.processor.FlagSet.IsEqual) goto L4bf7;
			goto L4e80;

		L4bf7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4c09;
			goto L4c66;

		L4c09:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				(10 * this.processor.PullLocalSignedWord(0x106)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, 999);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xe8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x14;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			goto L4cc9;

		L4c66:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				(10 * this.processor.PullLocalSignedWord(0x106)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, 999);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x15);
			if (this.processor.FlagSet.IsGreater) goto L4cc5;
			goto L4cc9;

		L4cc5:
			this.processor.EmitLocalWord(0xe8, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0xe8), 0x1));

		L4cc9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L4cdb;
			goto L4cdf;

		L4cdb:
			this.processor.EmitLocalWord(0xe8, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0xe8), 0x1));

		L4cdf:
			this.owner.Libc.CopyText(0xba06, "Cost to complete\n");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4d01;
			goto L4d22;

		L4d01:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);
			goto L4d46;

		L4d22:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L4d46:
			this.owner.Libc.AppendText(0xba06, ": $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xe8), 10));

			this.owner.Libc.AppendText(0xba06, "\nTreasury: $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, 10));

			this.owner.Libc.AppendText(0xba06, "\n");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4dd0;
			goto L4df5;

		L4dd0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L4de2;
			goto L4df5;

		L4de2:
			this.owner.Libc.AppendText(0xba06, "CIVIL DISORDER\n");
			goto L4e18;

		L4df5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e08;
			goto L4e18;

		L4e08:
			this.owner.Libc.AppendText(0xba06, " Yes\n No\n");

		L4e18:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 100, 80, 1);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L4e42;
			goto L4e78;

		L4e42:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e55;
			goto L4e78;

		L4e55:
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x106));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].MaterialsTally = (short)this.processor.RegCount.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;

		L4e78:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L4e80:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e8b;
			goto L4e96;

		L4e8b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e96;
			goto L4ea0;

		L4e96:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x63);
			if (this.processor.FlagSet.IsEqual) goto L4ea0;
			goto L4f37;

		L4ea0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

			this.owner.Startup.DisplayMouseCursor();

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4ec4;
			goto L4ee2;

		L4ec4:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L4ee2:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4f11;
			goto L4f2f;

		L4f11:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L4f2f:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L4f37:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x69);
			if (this.processor.FlagSet.IsEqual) goto L4f41;
			goto L4f4a;

		L4f41:
			this.processor.EmitGlobalWord(0x2496, 0x0);
			goto L4faf;

		L4f4a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x68);
			if (this.processor.FlagSet.IsEqual) goto L4f54;
			goto L4f5d;

		L4f54:
			this.processor.EmitGlobalWord(0x2496, 0x1);
			goto L4faf;

		L4f5d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x6d);
			if (this.processor.FlagSet.IsEqual) goto L4f67;
			goto L4f70;

		L4f67:
			this.processor.EmitGlobalWord(0x2496, 0x2);
			goto L4faf;

		L4f70:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x76);
			if (this.processor.FlagSet.IsEqual) goto L4f7a;
			goto L4f83;

		L4f7a:
			this.processor.EmitGlobalWord(0x2496, 0x3);
			goto L4faf;

		L4f83:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x60);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4f8d;
			goto L4ff5;

		L4f8d:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe0);
			if (this.processor.FlagSet.IsLess) goto L4f98;
			goto L4ff5;

		L4f98:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x60);
			this.processor.DivideAccumulatorByPowerOfTwo(0x5);
			this.processor.EmitGlobalWord(0x2496, this.processor.RegAcc.Value16);

		L4faf:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L4fc6;
			goto L4ff2;

		L4fc6:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.CityScene.DisplayCityScreen(townIdx, -1);
			
			this.processor.EmitGlobalWord(0x6b64, 0x1);
			this.processor.EmitGlobalWord(0x2496, 0x0);
			goto L045f;

		L4ff2:
			goto L12c2;

		L4ff5:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x61);
			if (this.processor.FlagSet.IsEqual) goto L4fff;
			goto L520e;

		L4fff:
			this.processor.EmitLocalWord(0x106, 0x0);

		L5005:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xcf);
			if (this.processor.FlagSet.IsEqual) goto L5026;
			goto L5039;

		L5026:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L5032;
			goto L5039;

		L5032:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));
			goto L5005;

		L5039:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5045;
			goto L504b;

		L5045:
			this.processor.EmitLocalWord(0x106, 0x0);

		L504b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegData.Value16;
			this.processor.RegCount.Value16 = 0x12;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x64);
			this.processor.EmitLocalWord(0xfa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x74);
			this.processor.EmitLocalWord(0x100, this.processor.RegAcc.Value16);

		L5078:
			FloodBoxWithMotif(this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100), 16, 16);

			this.owner.TextPainter.AwaitTime(10);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.MapOps.RenderUnitSprite(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)),
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100));

			this.owner.TextPainter.AwaitTime(10);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L50d2;
			goto L5078;

		L50d2:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xfc);
			goto L5121;

		L50e2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L50ee:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L50fa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5108:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5116:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5121:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L5129;
			goto L5108;

		L5129:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsNotEqual) goto L5131;
			goto L50ee;

		L5131:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsNotEqual) goto L5139;
			goto L50e2;

		L5139:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L5141;
			goto L50fa;

		L5141:
			goto L5116;

		L5144:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xea),
				0, this.processor.PullLocalSignedWord(0x4e) - 1);

			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L5167;
			goto L5171;

		L5167:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5171;
			goto L51f4;

		L5171:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L5192;
			goto L51bc;

		L5192:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x74)));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L51bc:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L51f4:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L51fe;
			goto L504b;

		L51fe:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L520e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x73);
			if (this.processor.FlagSet.IsEqual) goto L5218;
			goto L532e;

		L5218:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L522a;
			goto L532e;

		L522a:
			this.processor.EmitLocalWord(0x106, 0x0);

		L5230:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				252,
				((this.processor.PullLocalSignedWord(0x106) +
					this.processor.PullLocalSignedWord(0x40)) * 6) + 3,
				56, 7, 9, 1);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				252,
				((this.processor.PullLocalSignedWord(0x106) +
					this.processor.PullLocalSignedWord(0x40)) * 6) + 3,
				56, 7, 1, 9);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L52b5;
			goto L5230;

		L52b5:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xfc);
			goto L52e0;

		L52c5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L52f3;

		L52d1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L52f3;

		L52e0:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L52e8;
			goto L52d1;

		L52e8:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L52f0;
			goto L52c5;

		L52f0:

		L52f3:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xea),
				0, this.processor.PullLocalSignedWord(0xb4) - 1);

			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L5317;
			goto L5321;

		L5317:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5321;
			goto L5324;

		L5321:
			goto L5740;

		L5324:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L532e;
			goto L5230;

		L532e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x31);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5338;
			goto L53c8;

		L5338:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x39);
			if (this.processor.FlagSet.IsLessOrEqual) goto L5342;
			goto L53c8;

		L5342:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xf0);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x31);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L5359;
			goto L53c5;

		L5359:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L536b;
			goto L53ad;

		L536b:
			ObtainCitizenType(this.processor.PullLocalWord(0xea));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x3);
			if (this.processor.FlagSet.IsLess) goto L5385;
			goto L539a;

		L5385:
			AssignCitizenType(this.processor.PullLocalWord(0xea),
				(ushort)(this.processor.PullLocalWord(0xe8) + 1));

			goto L53aa;

		L539a:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 1);

		L53aa:
			goto L53b5;

		L53ad:
			this.owner.Libc.CopyText(0xba06, "A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.owner.Dialogs.DisplayMenu(0xba06, 32, 32, 1);

			goto L045f;

		L53b5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L53c5:
			goto L045f;

		L53c8:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x70);
			if (this.processor.FlagSet.IsEqual) goto L53d2;
			goto L563f;

		L53d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L53dc;
			goto L53ed;

		L53dc:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xbe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb8, 0x1);

		L53ed:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				(ushort)((this.processor.PullLocalWord(0xb6) << 4) + 0xa0),
				(ushort)((this.processor.PullLocalWord(0xbe) << 4) + 0x38),
				16, 16,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0xb6) * 16 + 160,
				this.processor.PullLocalSignedWord(0xbe) * 16 + 56,
				15, 15, 15);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				(ushort)((this.processor.PullLocalWord(0xb6) << 4) + 0xa0),
				(ushort)((this.processor.PullLocalWord(0xbe) << 4) + 0x38));

			this.owner.TextPainter.AwaitTime(10);

			this.processor.EmitGlobalWord(0x6b64, 0x1);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L54b4;
			goto L53ed;

		L54b4:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb6);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xbe, 0xfc);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			goto L551f;

		L54d4:
			this.processor.EmitLocalWord(0xbe, this.processor.DecrementWord(this.processor.PullLocalWord(0xbe)));
			goto L556a;

		L54db:
			this.processor.EmitLocalWord(0xbe, this.processor.DecrementWord(this.processor.PullLocalWord(0xbe)));
			this.processor.IncrementLocalWord(0xb6);
			goto L556a;

		L54e6:
			this.processor.EmitLocalWord(0xb6, this.processor.IncrementWord(this.processor.PullLocalWord(0xb6)));
			goto L556a;

		L54ed:
			this.processor.EmitLocalWord(0xb6, this.processor.IncrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.IncrementLocalWord(0xbe);
			goto L556a;

		L54f8:
			this.processor.EmitLocalWord(0xbe, this.processor.IncrementWord(this.processor.PullLocalWord(0xbe)));
			goto L556a;

		L54ff:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.IncrementLocalWord(0xbe);
			goto L556a;

		L550a:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			goto L556a;

		L5511:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.DecrementLocalWord(0xbe);
			goto L556a;

		L551f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsNotEqual) goto L5527;
			goto L550a;

		L5527:
			if (this.processor.FlagSet.IsLessOrEqual) goto L552c;
			goto L5547;

		L552c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4700);
			if (this.processor.FlagSet.IsNotEqual) goto L5534;
			goto L5511;

		L5534:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L553c;
			goto L54d4;

		L553c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4900);
			if (this.processor.FlagSet.IsNotEqual) goto L5544;
			goto L54db;

		L5544:
			goto L556a;

		L5547:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsNotEqual) goto L554f;
			goto L54e6;

		L554f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f00);
			if (this.processor.FlagSet.IsNotEqual) goto L5557;
			goto L54ff;

		L5557:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L555f;
			goto L54f8;

		L555f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5100);
			if (this.processor.FlagSet.IsNotEqual) goto L5567;
			goto L54ed;

		L5567:

		L556a:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xb6));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L557e;
			goto L55f5;

		L557e:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xbe));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L5592;
			goto L55f5;

		L5592:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xbe));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xb6));

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLess) goto L55b6;
			goto L55f5;

		L55b6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xb6));

			this.processor.RegBase.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0xbe));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L55f5;
			goto L5605;

		L55f5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xf6);
			this.processor.EmitLocalWord(0xb6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xfc, 0xbe);

		L5605:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L560f;
			goto L5619;

		L560f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5619;
			goto L562c;

		L5619:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb6);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xbe, 0xfc);
			goto L5aa6;

		L562c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L5636;
			goto L53ed;

		L5636:
			this.processor.EmitLocalWord(0xb8, 0x0);
			goto L045f;

		L563f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x4d);
			if (this.processor.FlagSet.IsEqual) goto L5649;
			goto L5671;

		L5649:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L5653;
			goto L5671;

		L5653:
			this.processor.EmitLocalWord(0xca, this.processor.BitXorWord(this.processor.PullLocalWord(0xca), 0x1));
			this.processor.CopyLocalWord(0xfe, 0xda);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5671:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x72);
			if (this.processor.FlagSet.IsNotEqual) goto L567b;
			goto L56a6;

		L567b:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5685;
			goto L56c8;

		L5685:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe7);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5690;
			goto L56c8;

		L5690:
			this.processor.CompareWord(this.owner.GlobDb3e, 0xbe);
			if (this.processor.FlagSet.IsGreater) goto L569b;
			goto L56c8;

		L569b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsLess) goto L56a6;
			goto L56c8;

		L56a6:
			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			
			this.owner.CityNaming.DisplayCityLabelPrompt(townIdx);
			
			this.processor.EmitGlobalWord(0x6b64, 0x1);
			goto L045f;

		L56c8:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L56d2;
			goto L5cba;

		L56d2:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x120);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L56dd;
			goto L5719;

		L56dd:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x60);
			if (this.processor.FlagSet.IsLess) goto L56e7;
			goto L5719;

		L56e7:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x58);
			if (this.processor.FlagSet.IsGreater) goto L56f1;
			goto L5719;

		L56f1:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L56fb;
			goto L5719;

		L56fb:
			this.processor.EmitLocalWord(0xca, this.processor.BitXorWord(this.processor.PullLocalWord(0xca), 0x1));
			this.processor.CopyLocalWord(0xfe, 0xda);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5719:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x12c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5724;
			goto L58ac;

		L5724:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x5e);
			if (this.processor.FlagSet.IsLess) goto L572e;
			goto L58ac;

		L572e:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x40));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

		L5740:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xda);
			this.processor.EmitLocalWord(0xbc, this.processor.RegAcc.Value16);
			goto L574f;

		L574b:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L574f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L5759;
			goto L58ac;

		L5759:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5783;
			goto L58a9;

		L5783:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.DecrementLocalWord(0x106);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L5793;
			goto L58a9;

		L5793:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L57a5;
			goto L58a9;

		L57a5:
			this.owner.Libc.CopyText(0xba06, "Do you want to sell\nyour ");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " for ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(0xa * (short)this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0x1e * this.processor.PullLocalWord(0xbc) + 0xbd0) & 0xffff))), 10));

			this.owner.Libc.AppendText(0xba06, "$?\n No.\n Yes.\n");

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Dialogs.DisplayMenu(0xba06, 128, 80, 1);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5844;
			goto L58a1;

		L5844:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd0)));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			uint scratchUint = (uint)((long)this.owner.World.Towns[townIdx].StructureMarkers0 |
				((long)this.owner.World.Towns[townIdx].StructureMarkers1 << 16));
			uint scratch1 = (uint)((long)this.processor.RegCount.Value16 | ((long)this.processor.RegBase.Value16 << 16));
			scratchUint -= scratch1;
			this.owner.World.Towns[townIdx].StructureMarkers0 = (ushort)(scratchUint & 0xffff);
			this.owner.World.Towns[townIdx].StructureMarkers1 = (ushort)((scratchUint & 0xffff0000) >> 16);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker |= 0x80;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L589b;
			goto L58a1;

		L589b:
			this.processor.EmitGlobalWord(0x6b64, 0x1);

		L58a1:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L58a9:
			goto L574b;

		L58ac:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xc8);
			if (this.processor.FlagSet.IsLess) goto L58b7;
			goto L596b;

		L58b7:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x14);
			if (this.processor.FlagSet.IsLess) goto L58c1;
			goto L596b;

		L58c1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L58d3;
			goto L5953;

		L58d3:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xf4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			this.processor.EmitLocalWord(0xea, this.processor.SubtractWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5902;
			goto L590e;

		L5902:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L590e;
			goto L5911;

		L590e:
			goto L045f;

		L5911:
			ObtainCitizenType(this.processor.PullLocalWord(0xea));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x3);
			if (this.processor.FlagSet.IsLess) goto L592b;
			goto L5940;

		L592b:
			AssignCitizenType(this.processor.PullLocalWord(0xea),
				(ushort)(this.processor.PullLocalWord(0xe8) + 1));

			goto L5950;

		L5940:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 1);

		L5950:
			goto L595b;

		L5953:
			this.owner.Libc.CopyText(0xba06, "A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.owner.Dialogs.DisplayMenu(0xba06, 32, 32, 1);

			goto L045f;

		L595b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L596b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x64);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5975;
			goto L5a60;

		L5975:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x2580);
			if (this.processor.FlagSet.IsLess) goto L5980;
			goto L5a60;

		L5980:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L598a;
			goto L5a60;

		L598a:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x74);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x64);
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x106, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L59cd;
			goto L5a60;

		L59cd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L59ee;
			goto L5a18;

		L59ee:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x74)));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L5a18:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5a60:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x18);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5a6a;
			goto L5cba;

		L5a6a:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x68);
			if (this.processor.FlagSet.IsLess) goto L5a74;
			goto L5cba;

		L5a74:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

		L5aa6:
			ObtainCityTileIndex(this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc));

			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L5ac4;
			goto L5cb7;

		L5ac4:
			if (this.processor.PullLocalWord(0xea) == 20)
			{
				// A click on the city square in the middle of the map: all citizens are taken off their tiles
				// (also entertainers, taxmen and scientists) and the game's own assignment puts them back on the best tiles,
				// first enough food so the city doesn't starve, then the best mix of food, shields and trade.
				this.owner.World.Towns[townIdx].LaborerMarkers0 = 0;
				this.owner.World.Towns[townIdx].LaborerMarkers1 = 0;
				this.processor.EmitGlobalWord(0xe8b8, 0);
				goto L045f;
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x14);
			if (this.processor.FlagSet.IsLess) goto L5ace;
			goto L5cb7;

		L5ace:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));

			this.processor.RegBase.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5b13;
			goto L5cb7;

		L5b13:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5b4a;
			goto L5b6e;

		L5b4a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)((short)((10 * this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X) +
				(this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y * 2)));

			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L5b6b;
			goto L5b6e;

		L5b6b:
			goto L12c2;

		L5b6e:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].LaborerMarkers0 ^= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 ^= this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5bbb;
			goto L5be5;

		L5bbb:
			this.processor.EmitLocalWord(0xae, this.processor.IncrementWord(this.processor.PullLocalWord(0xae)));

			RenderCityTile(townIdx, this.processor.PullLocalWord(0xea), marker);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xe8b8), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.EmitGlobalWord(0xe8b8, this.processor.DecrementWord(this.processor.PullGlobalWord(0xe8b8)));
				this.processor.DecrementLocalWord(0x50);
			}
			goto L5c78;

		L5be5:
			this.processor.EmitLocalWord(0xae, this.processor.DecrementWord(this.processor.PullLocalWord(0xae)));
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L5bf6;

		L5bf2:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L5bf6:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L5c00;
			goto L5c45;

		L5c00:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			MayCityWorkTile(
				this.owner.World.Towns[townIdx].Place.X +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X,
				this.owner.World.Towns[townIdx].Place.Y +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y,
				this.processor.PullLocalWord(0xbc));

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da), 
				this.processor.SubtractWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da)), this.processor.RegAcc.Value16));
			goto L5bf2;

		L5c45:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.RenderMapTile(
				this.owner.World.Towns[townIdx].Place.X +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X,
				this.owner.World.Towns[townIdx].Place.Y +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y);

			this.processor.EmitGlobalWord(0xe8b8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xe8b8)));
			this.processor.IncrementLocalWord(0x50);

		L5c78:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x50);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = 0x1a;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, 0xffff);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, 0x3ff);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.SumCarryWord(this.processor.RegData.Value16, this.processor.RegBase.Value16);
			this.owner.World.Towns[townIdx].LaborerMarkers0 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 = this.processor.RegData.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0xae), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L5cae;
			goto L5cb4;

		L5cae:
			goto L045f;

		L5cb4:
			goto L12c2;

		L5cb7:
			goto L045f;

		L5cba:
			goto L5cc2;

		L5cbd:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

		L5cc2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc2);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd0);
			this.processor.EmitGlobalWord(0xd75e, this.processor.RegAcc.Value16);

		L5cd0:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5cdb;
			goto L68cc;

		L5cdb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L5ce5;
			goto L62d8;

		L5ce5:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsLess) goto L5cfa;
			goto L5f3f;

		L5cfa:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L5d0c;
			goto L5df3;

		L5d0c:
			this.owner.Libc.CopyText(0xba06, "Civil Disorder in\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "! Mayor\nflees in panic.\n");

			this.owner.Services.PerformSound(0x24, 0);

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L5d4d;
			goto L5d9f;

		L5d4d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5d5f;
			goto L5d9f;

		L5d5f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b92), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L5d69;
			goto L5d9f;

		L5d69:
			this.owner.CityScene.DisplayCityScreen(townIdx, -2);
			
			this.owner.CityScene.RiotAnimation();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.EmitGlobalWord(0x6b92, 0x1);
			goto L5db9;

		L5d9f:
			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L5db9:
			this.owner.Services.PerformSound(1, 0);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5dd7;
			goto L5df3;

		L5dd7:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L5de7;
			goto L5df3;

		L5de7:
			this.owner.HelpSystem.DisplayHelpPopup(0x2708);

		L5df3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5e05;
			goto L5eb4;

		L5e05:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker |= 1;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L5e24;
			goto L5ea1;

		L5e24:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5e36;
			goto L5e54;

		L5e36:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L5e54:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5e83;
			goto L5ea1;

		L5e83:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L5ea1:
			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

			goto L5f3c;

		L5eb4:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5) goto L5ec4;
			goto L5f3c;

		L5ec4:
			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Libc.CopyText(0xba06, "Discontented citizens of\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " revolt:\nGovernment Collapses!\n");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5f07;
			goto L5f20;

		L5f07:
			this.owner.Paper.DisplayNewspaper(townIdx);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Relations[0] |= 4;

		L5f20:
			this.owner.Politics.SwitchGovernment(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Services.PerformSound(1, 0);

		L5f3c:
			goto L606c;

		L5f3f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5f49;
			goto L604f;

		L5f49:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L5f5b;
			goto L604f;

		L5f5b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xfe;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L5f7a;
			goto L5fc2;

		L5f7a:
			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.Libc.CopyText(0xba06, "Order restored\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			goto L603f;

		L5fc2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5fd4;
			goto L5ff2;

		L5fd4:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L5ff2:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L6021;
			goto L603f;

		L6021:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L603f:
			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

		L604f:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L605f;
			goto L606c;

		L605f:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;

		L606c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6076;
			goto L6213;

		L6076:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L6088;
			goto L6213;

		L6088:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.HalveAccumulator();
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			if (this.processor.FlagSet.IsLessOrEqual) goto L60a4;
			goto L6213;

		L60a4:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L60b4;
			goto L6213;

		L60b4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L60c6;
			goto L61ac;

		L60c6:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L60d8;
			goto L619c;

		L60d8:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L60e8;
			goto L619c;

		L60e8:
			this.owner.Libc.CopyText(0xba06, "'We love the ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, "'\nday celebrated in\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.owner.Services.PerformSound(0x22, 0);

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L6155;
			goto L6185;

		L6155:
			this.owner.CityScene.DisplayCityScreen(townIdx, -2);
			
			this.owner.CityScene.RenderCitySceneryForHuman();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			goto L6190;

		L6185:
			this.owner.Paper.DisplayNewspaper(townIdx);

		L6190:
			this.owner.Services.PerformSound(1, 0);

		L619c:
			this.owner.World.Towns[townIdx].ConditionMarker |= 0x40;
			goto L6210;

		L61ac:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L61bc;
			goto L6210;

		L61bc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullGlobalWord(0x70da));
			if (this.processor.FlagSet.IsLess) goto L61df;
			goto L6210;

		L61df:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L61f1;
			goto L6204;

		L61f1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsNotEqual) goto L6204;
			goto L6210;

		L6204:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent++;

		L6210:
			goto L62d8;

		L6213:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L621d;
			goto L62d8;

		L621d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L622f;
			goto L62d8;

		L622f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L6241;
			goto L62cb;

		L6241:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L6251;
			goto L62cb;

		L6251:
			this.owner.Services.PerformSound(0x23, 0);

			this.owner.Libc.CopyText(0xba06, "'We love the ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, "'\ncelebration canceled\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.Paper.DisplayNewspaper(townIdx);
			
			this.owner.Services.PerformSound(1, 0);

		L62cb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xbf;

		L62d8:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L62eb;
			goto L635d;

		L62eb:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx != -1) goto L62f5;
			goto L630d;

		L62f5:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548),
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L630d;

			goto L6327;

		L630d:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx == -1) goto L6317;
			goto L635d;

		L6317:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway != 0) goto L6327;
			goto L635d;

		L6327:
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = -1;
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L6337;
			goto L6351;

		L6337:
			this.owner.Startup.DisplayMouseCursorFully();
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L6351:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway = 0;

		L635d:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 0)
			{
				this.processor.EmitLocalWord(0xe8, 0x0);
			}

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L638d;
			goto L63ba;

		L638d:
			if (this.owner.World.HardnessTier == 0) goto L6397;
			goto L63ba;

		L6397:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx >= 0 &&
				(this.owner.World.ScienceLeadingFoundVia[this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx] & 7) != 0)
			{
				this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway +=
					this.processor.PullLocalSignedWord(0xe8);
			}

		L63ba:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0x14);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L63d2;
			goto L63e5;

		L63d2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.HalveAccumulator();
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway += (short)this.processor.RegAcc.Value16;

		L63e5:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L63f1;
			goto L63fb;

		L63f1:
			this.processor.RegAcc.Value16 = 0xe;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.HardnessTier);
			goto L6403;

		L63fb:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);

		L6403:
			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2de);
			this.processor.EmitLocalWord(0xf4, this.processor.SumWord(this.processor.PullLocalWord(0xf4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L641a;
			goto L6441;

		L641a:
			this.processor.RegAcc.Value16 = 0xb;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xf4));
			if (this.processor.FlagSet.IsGreater) goto L6430;
			goto L6441;

		L6430:
			this.processor.RegAcc.Value16 = 0xb;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);

		L6441:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L644d;
			goto L6457;

		L644d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb1e8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6457;
			goto L64b8;

		L6457:
			if (this.owner.World.Season < 0)
			{
				this.processor.RegCount.Value16 = 0x1;
			}
			else
			{
				this.processor.RegCount.Value16 = 0x2;
			}

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xf4));
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L6488;
			goto L64b8;

		L6488:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway = 0;
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L649e;
			goto L64b8;

		L649e:
			this.owner.Startup.DisplayMouseCursorFully();

			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L64b8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Tally += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L64e3;
			goto L68cc;

		L64e3:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0xe0, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0xe0, this.processor.SumWord(this.processor.PullLocalWord(0xe0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x100);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L6545;
			goto L6640;

		L6545:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(20));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0xe8)].X +
				this.processor.PullLocalSignedWord(0xd8)));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0xe8)].Y +
				this.processor.PullLocalSignedWord(0xe4)));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L6590;
			goto L6640;

		L6590:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L65a8;
			goto L6640;

		L65a8:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L65bf;
			goto L6640;

		L65bf:
			AttachSmog(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xc6) - 8,
				this.processor.PullLocalSignedWord(0xd2) - 6);

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.EmitGlobalWord(0x2f9e, 0x6);

			this.owner.Libc.CopyText(0xba06, "Pollution near ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

		L6640:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L6652;
			goto L672d;

		L6652:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L6665;
			goto L672d;

		L6665:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6679;
			goto L672d;

		L6679:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x42);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L6691;

			goto L672d;

		L6691:
			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xd8) - 8,
				this.processor.PullLocalSignedWord(0xe4) - 6);

			this.owner.Diplomats.RenderExplosion(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.owner.Combat.EnactNuclearFallout(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= 0xffff;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= 0xffef;

			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Libc.CopyText(0xba06, "Nuclear Catastrophe\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\nContamination feared!\n");

			this.owner.Paper.DisplayNewspaper(townIdx);
			
			this.owner.Services.PerformSound(1, 0);

		L672d:
			this.processor.EmitLocalWord(0xbc, 0x1);
			goto L673a;

		L6736:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L673a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L6744;
			goto L68cc;

		L6744:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L676e;
			goto L68c9;

		L676e:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L677e;
			goto L68c9;

		L677e:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd2));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L679f;
			goto L680b;

		L679f:
			if (this.owner.World.HardnessTier >= 2) goto L67a9;
			goto L67b3;

		L67a9:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67b3:
			if (this.owner.World.HardnessTier >= 4) goto L67bd;
			goto L67c7;

		L67bd:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67c7:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x22);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L67df;

			goto L67e9;

		L67df:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67e9:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x25);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L6801;

			goto L680b;

		L6801:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L680b:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold < 0) goto L681b;
			goto L68c9;

		L681b:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold = 0;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.RegData.Value16 = this.processor.InvertWord(this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegBase.Value16;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "\ncan't maintain\n");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.ScaleIndexToBase(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd0)));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;

		L68c9:
			goto L6736;

		L68cc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L68de;
			goto L690f;

		L68de:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			// ??? this playerID reference needs to be checked!
			this.owner.UnitOps.ContainsAdjacentForeignUnit(this.owner.World.Towns[townIdx].ParticipantIdx,
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6904;
			goto L690f;

		L6904:
			this.owner.UnitOps.KeepUnitsInCityList(townIdx);

		L690f:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));

		L6927:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleCity");
		}

		/// <summary>
		/// A tile of the city (TownDeltas[townTile] from the city): with marker 1 draws it on the city screen with the icons of
		/// its food, shields and trade (MayCityWorkTile 0 - 2; closer together the more there are, a mark when there are none),
		/// else puts the city's player on the tile in map layer 2 (0x2494 set meanwhile). Either way the amounts are added to the
		/// totals at 0x70da.
		/// </summary>
		public void RenderCityTile(short townIdx, ushort townTile, short marker)
		{
			this.processor.Journal.OpenScope($"RenderCityTile({townIdx}, {townTile}, {marker})");

			short horizLoc = (short)(this.owner.World.Towns[townIdx].Place.X + this.owner.TownDeltas[townTile].X);
			short vertLoc = (short)(this.owner.World.Towns[townIdx].Place.Y + this.owner.TownDeltas[townTile].Y);
			bool shown = marker == 1;
			short total = 0;
			short spacing = 0;

			if (shown)
			{
				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				for (ushort resource = 0; resource < 3; resource++)
				{
					total += (short)MayCityWorkTile(horizLoc, vertLoc, resource);
				}

				spacing = (short)(total <= 4 ? 8 : total <= 6 ? 5 : 3);
			}
			else
			{
				this.processor.EmitGlobalWord(0x2494, 1);

				this.owner.Services.AssignDot(2, horizLoc + 0x50, vertLoc, (ushort)this.owner.World.Towns[townIdx].ParticipantIdx);
			}

			short iconHoriz = 0;
			short iconVert = 0;

			for (ushort resource = 0; resource < 3; resource++)
			{
				short amount = (short)MayCityWorkTile(horizLoc, vertLoc, resource);

				this.processor.EmitGlobalWord((ushort)((resource << 1) + 0x70da), (ushort)(this.processor.PullGlobalWord((ushort)((resource << 1) + 0x70da)) + amount));

				for (; amount > 0; amount--)
				{
					if (!shown)
						continue;

					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
						161 + (this.owner.TownDeltas[townTile].X * 16) + iconHoriz, 57 + (this.owner.TownDeltas[townTile].Y * 16) + iconVert,
						this.processor.PullGlobalWord((ushort)(((resource + 8) << 1) + 0xd4ce)));

					if (iconHoriz < 8)
					{
						iconHoriz += spacing;
					}
					else
					{
						iconHoriz = 0;
						iconVert += 8;
					}
				}
			}

			if (shown && total == 0)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
					165 + (this.owner.TownDeltas[townTile].X * 16), 61 + (this.owner.TownDeltas[townTile].Y * 16),
					this.processor.PullGlobalWord((0xd << 1) + 0xd4ce));
			}

			this.processor.EmitGlobalWord(0x2494, 0);

			this.processor.Journal.CloseScope("RenderCityTile");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="param3"></param>
		/// <returns></returns>
		public ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort arg3)
		{
			this.processor.Journal.OpenScope($"MayCityWorkTile({horizLoc}, {vertLoc}, {arg3})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveSource();

			this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L6ad8;
			goto L6c90;

		L6ad8:
			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L6b0d;
			this.processor.ScaleIndexToSource(0x13, this.processor.PullLocalWord(0x6));
			this.processor.RegBase.Value16 = arg3;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x374));
			goto L6b1c;

		L6b0d:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x6));
			this.processor.RegBase.Value16 = arg3;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x290));

		L6b1c:
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L6b53;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L6b43;
			this.processor.RegAcc.Value16 = 0x2;
			goto L6b46;

		L6b43:
			this.processor.RegAcc.Value16 = 0x4;

		L6b46:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L6b53;
			this.processor.EmitLocalByte(0x4, this.processor.BitOrByte(this.processor.PullLocalByte(0x4), 0x8));

		L6b53:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L6baa;
			this.processor.CompareWord(arg3, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L6b77;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L6b77;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x6));
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)));
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L6b77:
			this.processor.CompareWord(arg3, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L6b95;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x4);
			if (this.processor.FlagSet.IsEqual) goto L6b95;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x6));
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)));
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L6b95:
			this.processor.CompareWord(arg3, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L6baa;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L6baa;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L6baa;
			this.processor.IncrementLocalWord(0x2);

		L6baa:
			this.processor.CompareWord(arg3, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L6bd6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L6bbc;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L6bd6;

		L6bbc:
			this.processor.RegAcc.Value16 = 0x7;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)horizLoc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xb, (ushort)vertLoc);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.RegCount.LowByte, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L6bd6;
			this.processor.EmitLocalWord(0x2, 0x0);

		L6bd6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6bf6;
			this.processor.CompareWord(arg3, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L6bf6;

			ObtainWonderCityID(3);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x653e));
			if (this.processor.FlagSet.IsNotEqual) goto L6bf6;
			this.processor.IncrementLocalWord(0x2);

		L6bf6:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x10);
			if (this.processor.FlagSet.IsEqual) goto L6c07;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L6c07:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L6c39;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullGlobalWord(0x653e));
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L6c39;

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind <= 1)
			{
				this.processor.DecrementLocalWord(0x2);
			}

			this.processor.CompareWord(this.processor.PullGlobalWord(0x2494), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6c39;
			this.processor.EmitGlobalWord(0xe3c2, this.processor.SubtractWord(this.processor.PullGlobalWord(0xe3c2), 0x2));

		L6c39:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6c77;
			this.processor.CompareWord(arg3, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L6c77;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2494), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6c50;
			this.processor.EmitGlobalWord(0xdb42, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdb42)));

		L6c50:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x653e));
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L6c65;
			this.processor.RegAcc.Value16 = 0x2;
			goto L6c68;

		L6c65:
			this.processor.RegAcc.Value16 = 0x4;

		L6c68:
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind);
			if (this.processor.FlagSet.IsGreater) goto L6c77;
			this.processor.IncrementLocalWord(0x2);

		L6c77:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x4), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L6c89;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

		L6c89:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L6c92;

		L6c90:
			this.processor.RegAcc.Value16 = 0;

		L6c92:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("MayCityWorkTile");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Does the player have the effect of the wonder: it stands in a city of the player and is not obsolete
		/// </summary>
		/// <returns>1 or 0</returns>
		public ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent)
		{
			this.processor.Journal.OpenScope($"PlayerEnjoysWonderEffect({participantIdx}, {miracleIdent})");

			bool enjoys = false;

			if (!IsWonderObsolete(miracleIdent))
			{
				ushort townIdx = (ushort)this.owner.World.MarvelTownIdx[miracleIdent];

				enjoys = townIdx != 0xffff && this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx;
			}

			this.processor.RegAcc.Value16 = (ushort)(enjoys ? 1 : 0);

			this.processor.Journal.CloseScope("PlayerEnjoysWonderEffect");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The city of the wonder, also when the wonder is not finished (-1)
		/// </summary>
		/// <returns>City index, or 0xffff when the wonder is obsolete</returns>
		public ushort ObtainWonderCityID(short miracleIdent)
		{
			this.processor.Journal.OpenScope($"ObtainWonderCityID({miracleIdent})");

			ushort townIdx = IsWonderObsolete(miracleIdent) ? (ushort)0xffff : (ushort)this.owner.World.MarvelTownIdx[miracleIdent];
			this.processor.RegAcc.Value16 = townIdx;

			this.processor.Journal.CloseScope("ObtainWonderCityID");

			return townIdx;
		}

		/// <summary>
		/// A wonder has no effect any more when any civilization knows the technology that makes it obsolete
		/// (word table at 0x10fe of the data segment)
		/// </summary>
		private bool IsWonderObsolete(short miracleIdent)
		{
			short obsoletingTechnology = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(0x10fe + (miracleIdent << 1)));

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, obsoletingTechnology) != 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Pollution (improvement bit 0x40) appears on the tile, unless it is there already; the human player sees it
		/// </summary>
		public void AttachSmog(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"AttachSmog({horizLoc}, {vertLoc})");

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) == 0)
			{
				this.owner.MapOps.AssignTileImprovements(0x40, horizLoc, vertLoc);

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

				this.owner.World.DirtyCellTally++;
			}

			this.processor.Journal.CloseScope("AttachSmog");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void AssignCitizenType(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"AssignCitizenType({arg1}, {arg2})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort currentMarkers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			ushort updated = CitizenSpecialistRules.SetSpecialistType(currentMarkers, arg1, (CitizenSpecialistRules.SpecialistType)arg2);
			this.owner.World.Towns[cityIdx].LaborerMarkers2 = updated;

			this.processor.Journal.CloseScope("AssignCitizenType");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort ObtainCitizenType(ushort arg1)
		{
			this.processor.Journal.OpenScope($"ObtainCitizenType({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			var specType = CitizenSpecialistRules.GetSpecialistType(markers, arg1);
			this.processor.RegAcc.Value16 = (ushort)specType;

			this.processor.Journal.CloseScope("ObtainCitizenType");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort TallySpecialists(ushort arg1)
		{
			this.processor.Journal.OpenScope($"TallySpecialists({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			int tally = CitizenSpecialistRules.CountSpecialists(markers, (CitizenSpecialistRules.SpecialistType)arg1);
			this.processor.RegAcc.Value16 = (ushort)tally;

			this.processor.Journal.CloseScope("TallySpecialists");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Keeps the specialists of the city in range: entertainers (0x70e2) and taxmen (0x70e4) are clamped to the city size,
		/// scientists (0x6542) become taxmen while there are fewer taxmen than scientists, then specialists are given up
		/// (scientists first, then entertainers, and a taxman each time) until all of them fit in the size less the workers
		/// (clamped to 0 - 99)
		/// </summary>
		public void ReshuffleCitizens(short townIdx, ushort workerTally)
		{
			this.processor.Journal.OpenScope($"ReshuffleCitizens({townIdx}, {workerTally})");

			City town = this.owner.World.Towns[townIdx];

			this.processor.EmitGlobalWord(0x70e2,
				(ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e2), 0, town.TrueExtent));

			while (this.processor.PullGlobalWord(0x6542) != 0 &&
				this.processor.PullGlobalSignedWord(0x70e4) < this.processor.PullGlobalSignedWord(0x6542))
			{
				this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				this.processor.EmitGlobalWord(0x70e4, (ushort)(this.processor.PullGlobalWord(0x70e4) + 1));
			}

			ushort taxmen = (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e4), 0, town.TrueExtent);

			while (true)
			{
				this.processor.EmitGlobalWord(0x70e4, taxmen);

				short room = (short)this.owner.Helpers.VerifyValueSpan(town.TrueExtent - (short)workerTally, 0, 99);

				if ((short)(this.processor.PullGlobalWord(0x70e2) + this.processor.PullGlobalWord(0x70e4)) <= room)
					break;

				if (this.processor.PullGlobalWord(0x6542) != 0)
				{
					this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				}
				else
				{
					this.processor.EmitGlobalWord(0x70e2, (ushort)(this.processor.PullGlobalWord(0x70e2) - 1));
					this.processor.EmitGlobalWord(0x70e2,
						(ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e2), 0, town.TrueExtent));
				}

				this.processor.EmitGlobalWord(0x70e4, (ushort)(this.processor.PullGlobalWord(0x70e4) - 1));
				taxmen = (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e4), 0, town.TrueExtent);
			}

			this.processor.Journal.CloseScope("ReshuffleCitizens");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="param4"></param>
		/// <param name="param5"></param>
		/// <returns></returns>
		public ushort RenderCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort arg4, ushort arg5)
		{
			this.processor.Journal.OpenScope($"RenderCitizenIcons({townIdx}, {horizLoc}, {vertLoc}, {arg4}, {arg5})");

			// function body
			this.processor.EnterFrame(0xa);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.EmitLocalByte(0xa, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x7;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0xa));
			this.processor.CompareWord(this.processor.RegAcc.Value16, arg5);
			if (this.processor.FlagSet.IsLessOrEqual) goto L6f04;
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = arg5;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			goto L6f09;

		L6f04:
			this.processor.EmitLocalWord(0x8, 0x7);

		L6f09:
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L6f37;

		L6f10:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0x6) & 1) << 1) + 0x6e96)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			horizLoc += (short)this.processor.RegAcc.Value16;
			this.processor.IncrementLocalWord(0x6);

		L6f37:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L6f10;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6f49;
			horizLoc += 2;

		L6f49:
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L6f77;

		L6f50:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0x6) & 1) << 1) + 0x6e9a)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			horizLoc += (short)this.processor.RegAcc.Value16;
			this.processor.IncrementLocalWord(0x6);

		L6f77:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, arg4);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			if (this.processor.FlagSet.IsGreater) goto L6f50;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6f9e;
			horizLoc += 2;

		L6f9e:
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L6ff6;

		L6fa5:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0x6) & 1) << 1) + 0x6e9e)));

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6542);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L6fed;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,	12, 14, 5, 12);

		L6fed:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			horizLoc += (short)this.processor.RegAcc.Value16;
			this.processor.IncrementLocalWord(0x6);

		L6ff6:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L6fa5;
			horizLoc += 4;
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L7036;

		L7009:
			ObtainCitizenType(this.processor.PullLocalWord(0x6));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.RegAcc.Value16 << 1) + 0x6ea0)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			horizLoc += (short)this.processor.RegAcc.Value16;
			this.processor.IncrementLocalWord(0x6);

		L7036:
			this.processor.RegAcc.Value16 = arg4;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L7009;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitizenIcons");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPosSrc"></param>
		/// <param name="xPosDst"></param>
		/// <param name="yPosDst"></param>
		public void RenderResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo)
		{
			this.processor.Journal.OpenScope($"RenderResourceIcon({horizLocOrigin}, {horizLocTo}, {vertLocTo})");

			// function body
			if (horizLocOrigin < 22 || horizLocOrigin > 24)
			{
				horizLocOrigin--;

				if (horizLocOrigin >= 24)
				{
					horizLocOrigin -= 3;
				}

				if (horizLocOrigin >= 40)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						((horizLocOrigin & 1) * 19) + 161, 100, 18, 10,
						this.processor.PullGlobalWord(0xaa),
						horizLocTo, vertLocTo);
				}
				else
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						((horizLocOrigin / 5) * 19) + 161,
						((horizLocOrigin % 5) * 10) + 50,
						18, 10,
						this.processor.PullGlobalWord(0xaa),
						horizLocTo, vertLocTo);
				}
			}

			// Far return
			this.processor.Journal.CloseScope("RenderResourceIcon");
		}

		/// <summary>
		/// A box from one corner to the other: the background pattern (FloodBoxWithMotif) and the frame (RenderBox, style 1)
		/// </summary>
		public void RenderFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1)
		{
			this.processor.Journal.OpenScope($"RenderFrame({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1})");

			int breadth = horizSpot1 - horizLoc;
			int altitude = vertSpot1 - vertLoc;

			FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.owner.Dialogs.RenderBox(horizLoc, vertLoc, breadth, altitude, 1);

			this.processor.Journal.CloseScope("RenderFrame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			this.processor.Journal.OpenScope($"FloodBoxWithMotif({horizLoc}, {vertLoc}, {breadth}, {altitude})");

			// function body
			if (this.processor.PullGlobalWord(0xd762) == 0)
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, breadth, altitude, 9);
			}
			else
			{
				int boxVertLoc = vertLoc;
				int boxAltitude = altitude;

				while (boxAltitude > 0)
				{
					int tileAltitude = Math.Min(boxAltitude, 16);
					int boxHorizLoc = horizLoc;
					int boxBreadth = breadth;

					while (boxBreadth > 0)
					{
						int tileBreadth = Math.Min(boxBreadth, 16);

						this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8),
							208, 100, tileBreadth, tileAltitude,
							this.processor.PullGlobalWord(0xaa),
							boxHorizLoc, boxVertLoc);

						boxHorizLoc += tileBreadth;
						boxBreadth -= tileBreadth;
					}

					boxVertLoc += tileAltitude;
					boxAltitude -= tileAltitude;
				}
			}
		
			// Far return
			this.processor.Journal.CloseScope("FloodBoxWithMotif");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos1"></param>
		/// <param name="stringPtr"></param>
		/// <param name="mode"></param>
		public void RenderLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderLabelBox({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, 0x{charsAddr:x4}, {style})");

			// function body
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizSpot1 - horizLoc, vertSpot1 - vertLoc, style);
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizSpot1, vertLoc, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizLoc, vertSpot1, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc + 1, vertSpot1, horizSpot1, vertSpot1, (ushort)((style < 8) ? 8 : (style - 8)));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizSpot1, vertLoc, horizSpot1, vertSpot1, (ushort)((style < 0x8) ? 8 : (style - 8)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(charsAddr,
				((horizLoc + horizSpot1) / 2) + 1, ((vertLoc + vertSpot1) / 2) - 2, (ushort)(style ^ 8));

			// Far return
			this.processor.Journal.CloseScope("RenderLabelBox");
		}

		/// <summary>
		/// Draws the small map of the city screen: the 80 x 50 tiles around the human player's view (columns from HorizBegin - 40),
		/// every seen tile as a 2 x 2 dot (ocean in colour 1, land in colour 2), two tiles in every 3 pixels: the four tiles of
		/// a 2 x 2 block are shifted by the offsets at 0x280c (columns) and 0x2810 (rows)
		/// </summary>
		public void RenderMiniMap()
		{
			this.processor.Journal.OpenScope("RenderMiniMap()");

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 101, 117, 120, 75, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				for (int column = 0; column < 80; column++)
				{
					int horizLoc = this.owner.Routing.TunePosition(this.owner.World.Participants[humanIdx].HorizBegin + column - 40);

					if ((this.owner.World.ChartSight[(ushort)horizLoc, vertLoc] & (1 << humanIdx)) == 0)
						continue;

					ushort quarter = (ushort)(((vertLoc & 1) << 1) + (column & 1));
					int dotHoriz = (column / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x280c + quarter)) + 0x65;
					int dotVert = (vertLoc / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x2810 + quarter)) + 0x76;
					ushort hue = (ushort)(this.owner.MapOps.ObtainTerrainType((short)horizLoc, vertLoc) == MapRules.OceanTerrain ? 1 : 2);

					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), dotHoriz, dotVert, 2, 2, hue);
				}
			}

			this.owner.Dialogs.RenderBox(100, 117, 121, 75, 9);

			this.processor.Journal.CloseScope("RenderMiniMap");
		}

		/// <summary>
		/// A dot of 2 x 2 pixels on the small world map (only when 0x2496 = 2), the world centred on the start column of the
		/// human player
		/// </summary>
		public void RenderMiniMapDot(int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderMiniMapDot({horizLoc}, {vertLoc}, {style})");

			if (this.processor.PullGlobalWord(0x2496) == 2)
			{
				horizLoc = this.owner.Routing.TunePosition(
					horizLoc - this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin + 40);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
					((horizLoc * 3) / 2) + 101, ((vertLoc * 3) / 2) + 118, 2, 2, style);
			}

			this.processor.Journal.CloseScope("RenderMiniMapDot");
		}
	}
}
