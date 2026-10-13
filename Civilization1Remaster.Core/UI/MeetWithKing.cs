using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class MeetWithKing
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public MeetWithKing(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="param4"></param>
		public void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort arg4)
		{
			this.processor.Journal.OpenScope($"LaunchAudience({participantIdx}, {horizLoc}, {vertLoc}, {arg4})");

			// function body
			this.processor.EnterFrame(0x36);
			this.processor.PreserveIndexRegisters();

			this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].TouchParticipantTimeleft;
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[participantIdx].ChiefVisuals == 14)
			{
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b4), "Empress");

				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b6), "Empress");
			}

			this.processor.CompareWord(arg4, 0x0);
			if (this.processor.FlagSet.IsEqual)
			{
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = this.owner.World.RoundTally;
			}

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0xd804, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = arg4;
			this.processor.EmitGlobalWord(0x3934, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x1);
			this.processor.EmitGlobalWord(0x6b8e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x26, 0x1);
			goto L01da;

		L008f:
			this.processor.RegAcc.Value16 = 0;

		L0091:
			this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].TownTally, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L010c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalSignedWord(0x26)].Assault;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalSignedWord(0x26)].Guard;

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L010c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalSignedWord(0x26)].Assault;
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;
			
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalSignedWord(0x26)].Guard;

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.RegCount.Value16;
			
			if (this.owner.World.HardnessTier == 0) goto L00fe;
			this.processor.RegAcc.Value16 = 0x2;
			goto L0101;

		L00fe:
			this.processor.RegAcc.Value16 = 0x4;

		L0101:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegDest.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x2e, this.processor.SumWord(this.processor.PullLocalWord(0x2e), this.processor.RegAcc.Value16));
			goto L01d7;

		L010c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Assault == 0)
				goto L0131;

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Assault == 0)
				goto L0131;
			this.processor.EmitLocalWord(0x24, 0x1);

		L0131:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x26)].TownTally == 0)
				goto L0182;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].TownTally, 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0170;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalSignedWord(0x26)].Guard;

			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Guard;
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegCount.Value16));
			goto L01d7;

		L0170:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Guard;
			goto L01d4;

		L0182:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].TownTally == 0)
				goto L01ac;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Assault -
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Guard);
			this.processor.HalveAccumulator();
			goto L01d4;

		L01ac:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Guard-
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x26)].Guard);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.processor.DivideAccumulatorByPowerOfTwo(0x2);

		L01d4:
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));

		L01d7:
			this.processor.EmitLocalWord(0x26, this.processor.IncrementWord(this.processor.PullLocalWord(0x26)));

		L01da:
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01f5;
			if (this.owner.World.Participants[participantIdx].TownTally > 1) goto L01ef;
			goto L008f;

		L01ef:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0091;

		L01f5:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing != 7 ||
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TownTally <= 4 ||
				this.owner.World.Participants[participantIdx].TownTally <= 1 ||
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25] != 0 ||
				this.owner.World.RoundTally <= 200)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0267;

			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) == 0)
				goto L02a0;

			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[participantIdx].MartialMight);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight);
			if (this.processor.FlagSet.IsLessOrEqual) goto L02a0;

		L0267:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25];

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L02a0;
			this.processor.EmitLocalWord(0x24, 0x1);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x6),
				10 * this.owner.World.Participants[participantIdx].Standing, 9999);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2e, 0x0);

		L02a0:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[25] == 0) 
				goto L02c1;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x80;

		L02c1:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[25] == 0) goto L02fb;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25];

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L02fb;
			this.processor.EmitLocalWord(0x24, 0x1);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x6), 100, 9999);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L02fb:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.PersonParticipantIdx);
			
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25] == 0)
				goto L034c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LiveTroops[25];

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0321;
			this.processor.EmitLocalWord(0x2e, 0x4);
			goto L034c;

		L0321:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].LiveTroops[25];
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.HalveAccumulator();
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToSource(0x38, (ushort)participantIdx);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LiveTroops[25];
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L034c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L035c;
			this.processor.EmitLocalWord(0x24, 0x0);
			this.processor.EmitLocalWord(0x8, 0x0);

		L035c:
			this.owner.CityOps.PlayerEnjoysWonderEffect(this.owner.World.PersonParticipantIdx, 7);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0384;

			this.owner.CityOps.PlayerEnjoysWonderEffect(this.owner.World.PersonParticipantIdx, 0x12);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0397;

		L0384:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L0397;
			this.processor.EmitLocalWord(0x34, 0xffff);

		L0397:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) == 0)
				goto L03b0;

			this.processor.EmitLocalWord(0x6, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x6), 0x1));
			goto L03c2;

		L03b0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L03c2;
			this.processor.RegCount.Value16 = 0x4;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L03c2:
			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullLocalSignedWord(0x6) *
					(this.owner.World.HardnessTier + 1)) / 32,
				0, 20);

			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0417;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0417;
			this.processor.CompareWord(this.processor.RegSource.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLess) goto L0417;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L0417:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[25] == 0)
				goto L0464;

			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0x80) != 0)
				goto L0452;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0452;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

		L0452:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x80;

		L0464:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0482;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[participantIdx].MartialMight);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0487;

		L0482:
			this.processor.EmitLocalWord(0x24, 0x0);

		L0487:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0492;
			this.processor.EmitLocalWord(0x8, 0x2);

		L0492:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04b1;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0x4) != 0)
			{
				this.processor.EmitLocalWord(0x8, 0x0);
			}

		L04b1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2a, 0x0);
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0x8) != 0)
			{
				this.processor.EmitLocalWord(0x2a, 0xfffe);
			}
		
			this.processor.EmitLocalWord(0x30, 0xffff);
			this.processor.EmitLocalWord(0x20, 0x1);

		L04df:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0544;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x20)] & 3) == 1)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x20)].MartialMight;
				this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x2);
				this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].MartialMight, this.processor.RegAcc.Value16);
				if (this.processor.FlagSet.IsLess)
				{
					this.processor.IncrementLocalWord(0x2a);
				}

				if (this.owner.World.Participants[this.processor.PullLocalWord(0x20)].MartialMight >
					this.owner.World.Participants[participantIdx].MartialMight)
				{
					this.processor.IncrementLocalWord(0x2a);
				}

				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x20);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

				this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
				this.processor.RegCount.LowByte = 0x4;
				this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

				if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x20)] & 0x2) != 0)
				{
					this.processor.CopyLocalWord(0x20, 0x30);
				}
			}

		L0544:
			this.processor.EmitLocalWord(0x20, this.processor.IncrementWord(this.processor.PullLocalWord(0x20)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x8);
			if (this.processor.FlagSet.IsLess) goto L04df;

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1512));
			this.processor.EmitLocalWord(0x2a, this.processor.SubtractWord(this.processor.PullLocalWord(0x2a), this.processor.RegAcc.Value16));

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight <
				 this.owner.World.Participants[participantIdx].MartialMight)
			{
				this.processor.DecrementLocalWord(0x2a);
			}
		
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			// if bit 2 is 0 CX=1, otherwise CX=0
			this.processor.RegCount.Value16 = (ushort)((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0 ? 1 : 0);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0x2a));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05b3;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L05b3;
			this.processor.EmitLocalWord(0x24, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L05b3;
			this.processor.EmitLocalWord(0x6, 0x0);

		L05b3:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			if (this.owner.World.Participants[(ushort)participantIdx].RegimeKind < 4)
				goto L0606;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0606;
			
			this.processor.RegDest.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) != 0)
				goto L0606;

			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[participantIdx].MartialMight;
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L05f7;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L05f7;
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L0606;

		L05f7:
			this.owner.Politics.SwitchGovernment(participantIdx, 2);

		L0606:
			this.processor.EmitLocalWord(0x28, 0x0);

			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L064f;

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L064f;
			
			if (this.owner.World.Participants[participantIdx].MartialMight <
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight)
				goto L0640;

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) == 0)
				goto L064f;

		L0640:
			this.processor.EmitLocalWord(0x24, 0x0);
			this.processor.EmitLocalWord(0x28, 0x1);
			this.processor.EmitLocalWord(0x8, 0x0);

		L064f:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x32, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

		L0666:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.IncrementLocalWord(0xe);
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.IncrementLocalWord(0x32);
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
					this.processor.PullLocalSignedWord(0x20));
				this.processor.TestAccumulator();
				if (this.processor.FlagSet.IsNotEqual)
				{
					this.processor.CopyLocalWord(0x20, 0xa);
				}
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0709;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0709;

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx, this.processor.PullLocalWord(0x20));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2c));
			if (this.processor.FlagSet.IsLess) goto L0709;

			this.processor.CopyLocalWord(0x20, 0xc);

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);

		L0709:
			this.processor.EmitLocalWord(0x20, this.processor.IncrementWord(this.processor.PullLocalWord(0x20)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0715;
			goto L0666;

		L0715:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0x20) != 0)
				goto L072f;

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0738;

		L072f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x6);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0738;
			goto L0822;

		L0738:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x32);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0743;
			goto L0822;

		L0743:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L074c;
			goto L0822;

		L074c:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0762;
			goto L0822;

		L0762:
			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), this.processor.RegAcc.Value16);

			this.owner.BlockText.DisplayTextDialog(0x3776);

			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0822;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.EmitGlobalWord(0x3c62, 0x1);

			this.owner.Conquest.ChooseTechnologyToTake(participantIdx, owner.World.PersonParticipantIdx);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67ec);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x3c62, 0x1);

			this.owner.Conquest.ChooseTechnologyToTake(this.owner.World.PersonParticipantIdx, participantIdx);
			
			this.owner.Helpers.HaltHueCycling();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67ec);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x24, 0x0);
			this.processor.EmitGlobalWord(0x3936, 0x1);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			goto L064f;

		L0822:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L084e;
			this.processor.CompareWord(arg4, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L084e;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 2)
				goto L084e;

			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L084e;
			goto L165d;

		L084e:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0869;
			this.processor.EmitLocalWord(0x24, 0x1);
			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.EmitLocalWord(0x2e, 0x0);
			this.processor.EmitLocalWord(0x28, 0x0);

		L0869:
			this.processor.CompareWord(this.processor.PullLocalWord(0x30), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0872;
			goto L0952;

		L0872:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0888;
			goto L0952;

		L0888:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0891;
			goto L0952;

		L0891:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08b6;
			this.processor.RegAcc.Value16 = 0x3780;
			goto L08b9;

		L08b6:
			this.processor.RegAcc.Value16 = 0x3787;

		L08b9:
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), this.processor.RegAcc.Value16);

			this.owner.BlockText.DisplayTextDialog(0x378f);

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08dd;
			this.processor.RegAcc.Value16 = 0x2;
			goto L08e0;

		L08dd:
			this.processor.RegAcc.Value16 = 0x1;

		L08e0:
			this.processor.EmitGlobalWord(0x3936, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd76c, 0xc);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08fb;

			this.owner.Services.PerformSound(0x21, 0);

		L08fb:
			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0952;
			this.processor.EmitLocalWord(0x24, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L092d;
			this.processor.EmitLocalWord(0x6, 0x0);

		L092d:
			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x30),
				2);
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x30);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0x30)].Relations[this.owner.World.PersonParticipantIdx] |= 8;

		L0952:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L095b;
			goto L0a28;

		L095b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0964;
			goto L0a28;

		L0964:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0976;
			goto L0a28;

		L0976:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L098c;
			goto L0a28;

		L098c:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x6), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3797);

			this.processor.EmitGlobalWord(0x6b8e, 0x0);
			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.owner.Services.PerformSound(0x21, 0);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0a23;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold -= (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[participantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x24, 0x0);

		L0a23:
			this.processor.EmitLocalWord(0x6, 0x3e7);

		L0a28:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0a31;
			goto L0ad0;

		L0a31:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0a3a;
			goto L0ad0;

		L0a3a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x3e7);
			if (this.processor.FlagSet.IsNotEqual) goto L0a44;
			goto L0ad0;

		L0a44:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ad0;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), this.processor.RegAcc.Value16);

			this.owner.BlockText.DisplayTextDialog(0x37a0);

			this.processor.EmitGlobalWord(0x6b8e, 0x0);
			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.owner.Services.PerformSound(0x21, 0);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0acb;

			this.owner.Advisors.EarnTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0xc),
				this.owner.World.PersonParticipantIdx);
			
			this.processor.EmitLocalWord(0x24, 0x0);

		L0acb:
			this.processor.EmitLocalWord(0x6, 0x3e7);

		L0ad0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ad9;
			goto L0b5f;

		L0ad9:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L0b5f;

			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x32);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0b5f;

			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b54;

			this.owner.Services.PerformSound(0x21, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			
			this.owner.BlockText.DisplayTextDialog(0x37a9);

		L0b1a:
			this.owner.BlockText.ArrangeTextRow(0x28);

			this.processor.EmitGlobalWord(0x6b8e, 0x1);
			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);

			goto L0c13;

		L0b54:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x37b2);

			goto L0bed;

		L0b5f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0b98;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x3e7);
			if (this.processor.FlagSet.IsNotEqual) goto L0b98;

			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0b82;
			goto L0c29;

		L0b82:
			this.owner.Services.PerformSound(0x21, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			
			this.owner.BlockText.DisplayTextDialog(0x37bb);

			goto L0b1a;

		L0b98:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ba1;
			goto L0c29;

		L0ba1:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 1) == 0)
				goto L0bbb;

			this.processor.CompareWord(arg4, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c29;

		L0bbb:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0be5;

			this.owner.Services.PerformSound(0x21, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			
			this.owner.BlockText.DisplayTextDialog(0x37c3);

			goto L0b1a;

		L0be5:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x37c8);

		L0bed:
			this.owner.BlockText.ArrangeTextRow(0x28);

			this.owner.TurnHooks.DisplayDialog(0xba06, 40, 100);

		L0c13:
			this.owner.Politics.WipeDiplomacyFlags(owner.World.PersonParticipantIdx, participantIdx, 2);

		L0c29:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c32;
			goto L1097;

		L0c32:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L0c4f;

			this.processor.CompareWord(arg4, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c4f;
			goto L1097;

		L0c4f:
			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0c65;
			goto L1097;

		L0c65:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x37cd);

			this.processor.EmitGlobalWord(0x6b8e, 0x0);
			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0cc6;

			DisplayKingMessage(participantIdx);

			LogAudience(participantIdx);

			this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = 
				(short)(owner.World.RoundTally + 16);
			goto L1097;

		L0cc6:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind < 4) goto L0d03;

			this.owner.Libc.CopyText(0xba06, "You are overruled by the Senate.\nTreaty signed.\n");

			DisplayKingDialog(0xba06, 0x14, 0x8b);

			LogAudience(participantIdx);
			
			goto L1097;

		L0d03:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L0d2a;

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, participantIdx, 2);

		L0d2a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d33;
			goto L0e0e;

		L0d33:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0d3c;
			goto L0e0e;

		L0d3c:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0xa) != 0)
				goto L0e0e;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), this.processor.RegAcc.Value16);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3805);

			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0e0e;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.EmitGlobalWord(0x3c62, 0x1);

			this.owner.Advisors.EarnTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xa), participantIdx);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67ec);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), this.processor.RegAcc.Value16);

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x3936, 0x0);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			DisplayKingMessage(participantIdx);

			LogAudience(participantIdx);
			
			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

		L0e0e:
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0xa) != 0)
				goto L0eed;

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullLocalSignedWord(0x2e) * 2) - 4,
				0, this.owner.World.Participants[participantIdx].Gold / 50);

			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0e5a;
			goto L0eed;

		L0e5a:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x22), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x380e);

			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0eed;

			DisplayKingMessage(participantIdx);

			LogAudience(participantIdx);
			
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.SubtractWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

		L0eed:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0ef6;
			goto L1041;

		L0ef6:
			if (this.owner.World.Participants[participantIdx].TownTally <= 1) goto L0f05;
			goto L1041;

		L0f05:
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[(ushort)participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0xa) != 0)
				goto L1041;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Gold, 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3818);

			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0f8a;
			goto L1041;

		L0f8a:
			DisplayKingMessage(participantIdx);

			LogAudience(participantIdx);

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold +=
				this.owner.World.Participants[participantIdx].Gold;
			this.owner.World.Participants[participantIdx].Gold = 0;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.EmitLocalWord(0x20, 0x0);

		L0fd1:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L100e;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x20));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L100e;

			this.processor.EmitGlobalWord(0x3c62, 0x1);
			
			this.owner.Advisors.EarnTechnology(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x20),
				participantIdx);

		L100e:
			this.processor.EmitLocalWord(0x20, this.processor.IncrementWord(this.processor.PullLocalWord(0x20)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x48);
			if (this.processor.FlagSet.IsLess) goto L0fd1;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67ec);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), this.processor.RegAcc.Value16);

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x3936, 0x0);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

		L1041:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
				goto L1097;

			this.owner.Services.PerformSound(0x21, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3820);

			this.processor.EmitGlobalWord(0x6b8e, 0x1);
			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0x4);

			DisplayKingDialog(0xba06, 0x14, 0x8b);

		L1097:
			if (this.owner.World.Participants[participantIdx].ChiefVisuals == 14)
			{
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b4), this.processor.PullGlobalWord(0x33aa));

				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b6), this.processor.PullGlobalWord(0x33a8));
			}
		
			this.processor.EmitGlobalWord(0x6b8e, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10d2;
			goto L11d6;

		L10d2:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 2)
				goto L11d6;

			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1102;
			goto L11d6;

		L1102:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L112f;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x3e7);
			if (this.processor.FlagSet.IsEqual) goto L112f;
			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalWord(0xd76c, 0x4);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x382a);

			goto L11c3;

		L112f:
			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.owner.Libc.CopyText(0xba06, "We welcome the friendship of the\n");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " people and their most\nwise and munificent leader:\n");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.owner.Libc.AppendText(0xba06, ".\n");

		L11c3:
			DisplayKingDialog(0xba06, 0x14, 0x8b);

		L11d6:
			this.processor.EmitGlobalWord(0x6b8e, 0x1);

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L15e2;

			WelcomeEmissary(participantIdx, horizLoc, vertLoc);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1209;
			goto L15e2;

		L1209:
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x388f);

			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x34));
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x10);
			if (this.processor.FlagSet.IsLess) goto L1247;
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind < 4) goto L124d;

		L1247:
			this.processor.EmitGlobalWord(0xb276, 0x4);

		L124d:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L126b;
			this.processor.EmitGlobalByte(0xb276, this.processor.BitOrByte(this.processor.PullGlobalByte(0xb276), 0x2));

		L126b:
			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1289;
			goto L13a5;

		L1289:
			this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x2e),
				0, this.owner.World.Participants[participantIdx].Gold / 50);

			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1332;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x22), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3897);

			this.processor.EmitGlobalWord(0x3936, 0x0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[participantIdx].Gold -= 
				(short)this.processor.PullLocalWord(0x22);
			this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = this.owner.World.RoundTally;
			goto L13a5;

		L1332:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1376;

			this.owner.Services.PerformSound(0x21, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x38a1);

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, participantIdx, 2);
			
			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0xc);
			goto L1392;

		L1376:
			this.owner.Libc.CopyText(0xba06, "We ignore your feeble threats.\n");

			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalWord(0xd76c, 0x4);

		L1392:
			DisplayKingDialog(0xba06, 0x14, 0x8b);

		L13a5:
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L13ae;
			goto L15e2;

		L13ae:
			this.owner.Libc.CopyText(0xba06, "You must attack the evil:\n ");

			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x20, 0x1);
			goto L1400;

		L13ca:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1e), this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, "\n ");

		L13fd:
			this.processor.EmitLocalWord(0x20, this.processor.IncrementWord(this.processor.PullLocalWord(0x20)));

		L1400:
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1426;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L13fd;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L13fd;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x20);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsNotEqual) goto L13ca;
			goto L13fd;

		L1426:
			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L144a;
			goto L15e2;

		L144a:
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1e));
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.processor.EmitGlobalWord(0xd76c, 0x4);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x20)] & 1) != 0)
				goto L1487;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x38e9);

			goto L15cf;

		L1487:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x20)] & 2) != 0)
				goto L14c1;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x38f4);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			goto L1209;

		L14c1:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x20);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.Helpers.VerifyValueSpan(
				((this.owner.World.Participants[this.processor.PullLocalWord(0x20)].MartialMight / 2) *
					(this.owner.World.Participants[participantIdx].Gold / 100)) / 50,
				2, 9999);

			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x22), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x38fb);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1562;
			goto L15e2;

		L1562:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L15be;

			this.owner.World.Participants[participantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold -= (short)this.processor.RegAcc.Value16;

			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " declare war\non the ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ".\n");
			goto L15cf;

		L15be:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x391e);

		L15cf:
			DisplayKingDialog(0xba06, 0x14, 0x8b);

		L15e2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L15fa;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x100;

		L15fa:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] &= 0xffdf;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] &= 0xffdf;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L165d;
			
			if (this.owner.World.Participants[participantIdx].TownTally <= 4)
				goto L165d;

			if ((this.owner.World.Participants[owner.World.PersonParticipantIdx].Standing + 1) >=
				this.owner.World.Participants[participantIdx].Standing)
				goto L165d;
			
			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 2) != 0)
				goto L165d;

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), 
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1992)));

			this.owner.HelpSystem.DisplayHelpPopup(0x392b);

		L165d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1668;

			FinishAudience();

		L1668:
			if (this.owner.World.Participants[participantIdx].ChiefVisuals == 14)
			{
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b4), this.processor.PullGlobalWord(0x33aa));

				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x19b6), this.processor.PullGlobalWord(0x33a8));
			}

			this.processor.EmitGlobalWord(0xd804, 0x0);
			this.processor.EmitGlobalWord(0x3934, 0xffff);
			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LaunchAudience");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void FinishAudience()
		{
			this.processor.Journal.OpenScope("FinishAudience()");

			// function body
			ReleaseKingResources();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x3934, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd804, this.processor.RegAcc.Value16);

			// Far return
			this.processor.Journal.CloseScope("FinishAudience");
		}

		/// <summary>
		/// The audience with the player's emissary, once a turn (0xd804): unless the answer 0x3934 is known, shows the map at
		/// the position (horizLoc -1: not moved) and asks whether to receive the emissary (with "Check Intelligence" when the human player
		/// has relations bit 0x40 towards the player, which shows the report and asks again); 0x3934 is minus the choice. Unless
		/// refused (-1), shows the king and his greeting in the player's font (0x36fa).
		/// </summary>
		/// <returns>0x3934, also in AX</returns>
		public ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"WelcomeEmissary({participantIdx}, {horizLoc}, {vertLoc})");

			if (this.processor.PullGlobalWord(0xd804) == 0)
			{
				this.processor.EmitGlobalWord(0xd804, 1);

				if (this.processor.PullGlobalWord(0x3934) == 0)
				{
					AskToReceiveEmissary(participantIdx, horizLoc, vertLoc);
				}

				if (this.processor.PullGlobalWord(0x3934) != 0xffff)
				{
					ReceiveEmissary(participantIdx);
				}
			}

			ushort answer = this.processor.PullGlobalWord(0x3934);
			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("WelcomeEmissary");

			return answer;
		}

		/// <summary>The question of WelcomeEmissary: 0x3934 becomes minus the chosen line (-2 shows the intelligence report first)</summary>
		private void AskToReceiveEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			if (horizLoc != -1)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, horizLoc - 8, vertLoc - 2);
			}

			while (true)
			{
				this.owner.Libc.CopyText(0xba06, "An emissary from the\n");
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)));
				this.owner.Libc.AppendText(0xba06, " wishes to\nspeak with you.\n");
				this.owner.Libc.AppendText(0xba06, "Will you receive him?\n Yes.\n No.\n");

				if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.owner.Libc.AppendText(0xba06, " Check Intelligence\n");
				}

				this.owner.Services.PerformSound(this.processor.PullGlobalWord(LeaderRecord(participantIdx, 0x1518)), 3);

				this.owner.Startup.DisplayMouseCursorFully();

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.processor.EmitGlobalWord(0x3934, (ushort)-this.processor.RegAcc.Value16);

				this.owner.Startup.ConcealMouseCursorToSavedLevel();

				if (this.processor.PullGlobalWord(0x3934) != 0xfffe)
					break;

				this.owner.Briefings.DisplayIntelligenceReport(participantIdx);
			}
		}

		/// <summary>The audience of WelcomeEmissary: the king's screen, his greeting and dialog</summary>
		private void ReceiveEmissary(short participantIdx)
		{
			this.owner.Startup.ConcealMouseCursor();

			DisplayKingScreen(participantIdx);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord(LeaderRecord(participantIdx, 0x151a)), 3);

			this.owner.Startup.DisplayMouseCursor();

			// The font of the leader (table at 0x36fa), font 3 instead of 10
			ushort typeface = (ushort)((sbyte)this.processor.PullUInt8(0x36fa, (ushort)this.owner.World.Participants[participantIdx].ChiefVisuals) + 8);
			this.processor.EmitGlobalWord(0x67ec, typeface == 0xa ? (ushort)3 : typeface);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), this.processor.PullGlobalWord(0x67ec));
			this.processor.EmitGlobalWord(0xd2dc, 1);
			this.processor.EmitGlobalWord(0x3936, 1);
			this.processor.EmitGlobalWord(0x67e8, 0xffff);
			this.processor.EmitGlobalWord(0x7ef4, 0);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			ObtainKingSalutation(participantIdx);

			DisplayKingDialog(0xba06, 0x14, 0x8b);
		}

		/// <summary>A word of the player's leader record (0x3a bytes per leader)</summary>
		private ushort LeaderRecord(short participantIdx, ushort field)
		{
			return (ushort)((this.owner.World.Participants[participantIdx].ChiefVisuals * 0x3a) + field);
		}

		/// <summary>
		/// The greeting of the king at 0xba06: the opening (0x39b1 with relations bit 8 towards the human player, else 0x39e8),
		/// one of 4 random titles with the government (0x19b2) and the leader's name (0x19a2), "of the" civilization (0x1992),
		/// and the nuclear threat when the player has nuclear units (type 25)
		/// </summary>
		public void ObtainKingSalutation(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainKingSalutation({participantIdx})");

			ushort leader = (ushort)((participantIdx << 1) + 0x19a2);
			ushort Regime() => this.processor.PullGlobalWord((ushort)((this.owner.World.Participants[participantIdx].RegimeKind << 1) + 0x19b2));

			this.owner.Libc.CopyText(0xba06, (this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) != 0 ? (ushort)0x39b1 : (ushort)0x39e8);

			switch (this.owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.owner.Libc.AppendText(0xba06, "the most exalted\n");
					this.owner.Libc.AppendText(0xba06, Regime());
					this.owner.Libc.AppendText(0xba06, ": ");
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord(leader));
					this.owner.Libc.AppendText(0xba06, " ");
					break;

				case 1:
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord(leader));
					this.owner.Libc.AppendText(0xba06, ", ruler\nand ");
					this.owner.Libc.AppendText(0xba06, Regime());
					this.owner.Libc.AppendText(0xba06, " ");
					break;

				case 2:
					this.owner.Libc.AppendText(0xba06, "our most wise\n");
					this.owner.Libc.AppendText(0xba06, Regime());
					this.owner.Libc.AppendText(0xba06, ": ");
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord(leader));
					this.owner.Libc.AppendText(0xba06, " ");
					break;

				case 3:
					this.owner.Libc.AppendText(0xba06, "he who makes mortals\ntremble: ");
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord(leader));
					this.owner.Libc.AppendText(0xba06, " ");
					break;
			}

			this.owner.Libc.AppendText(0xba06, "of the ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)));
			this.owner.Libc.AppendText(0xba06, "...\n");

			if (this.owner.World.Participants[participantIdx].LiveTroops[25] != 0)
			{
				this.owner.Libc.AppendText(0xba06, "Our words are backed\nwith NUCLEAR WEAPONS!\n");
			}

			this.processor.Journal.CloseScope("ObtainKingSalutation");
		}

		/// <summary>
		/// The message of the leader of the player to the human player (text 0x3a8c, with the names of both civilizations from
		/// the table at 0x1982 put into the text parameters 0x30be and 0x30b8)
		/// </summary>
		public void DisplayKingMessage(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayKingMessage({participantIdx})");

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), CivilizationNameAddr(participantIdx));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), CivilizationNameAddr(this.owner.World.PersonParticipantIdx));

			this.processor.EmitGlobalWord(0xd76c, 0xc);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x3a8c);

			DisplayKingDialog(0xba06, 0x14, 0x8b);

			this.processor.Journal.CloseScope("DisplayKingMessage");
		}

		private ushort CivilizationNameAddr(short participantIdx)
		{
			return this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982));
		}

		/// <summary>
		/// After an audience: the player and the human have met (diplomacy flag 2), the player waits 16 turns before the next
		/// contact. At the easiest level the units of the player whose nearest city is one of the human's, at most 2 away, are
		/// removed.
		/// </summary>
		public void LogAudience(short participantIdx)
		{
			this.processor.Journal.OpenScope($"LogAudience({participantIdx})");

			this.owner.Politics.AssignDiplomacyFlags(this.owner.World.PersonParticipantIdx, participantIdx, 2);

			this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = (short)(this.owner.World.RoundTally + 16);

			if (this.owner.World.HardnessTier == 0)
			{
				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx))
						continue;

					ushort townIdx = this.owner.Helpers.LocateNearestCity(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
						(short)this.processor.PullGlobalWord(0x6c9a) <= 2)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}
			}

			this.processor.Journal.CloseScope("LogAudience");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ChoreographKingSpeaking()
		{
			this.processor.Journal.OpenScope("ChoreographKingSpeaking()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.EmitGlobalWord(0x7ef4, 0xffff);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd76c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1b50;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1b50;
			goto L1cc0;

		L1b50:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x67f4);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3ab8)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1b9a;
			if (this.processor.FlagSet.IsLessOrEqual) goto L1b64;

		L1b5d:
			this.processor.EmitGlobalWord(0x67f4, this.processor.IncrementWord(this.processor.PullGlobalWord(0x67f4)));
			goto L1c64;

		L1b64:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x67f4);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3ab8));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xfffd);
			if (this.processor.FlagSet.IsEqual) goto L1b91;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xfffe);
			if (this.processor.FlagSet.IsEqual) goto L1b88;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1b7f;
			goto L1c64;

		L1b7f:
			this.processor.EmitGlobalWord(0x67f4, 0x0);
			goto L1c64;

		L1b88:
			this.processor.EmitGlobalWord(0x67f4, 0x6);
			goto L1c64;

		L1b91:
			this.processor.EmitGlobalWord(0x67f4, 0xe);
			goto L1c64;

		L1b9a:
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3936);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1bb2;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1c08;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1c16;
			goto L1bce;

		L1bb2:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b8e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1bbd;
			this.processor.RegAcc.LowByte = 0x4;
			goto L1bbf;

		L1bbd:
			this.processor.RegAcc.LowByte = 0x2;

		L1bbf:
			this.processor.EmitGlobalByte(0x3a9a, this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x6);
			if (this.processor.FlagSet.IsEqual) goto L1bce;
			this.processor.EmitLocalWord(0x6, 0x3);

		L1bce:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd76c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1be1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1be1;
			this.processor.EmitLocalWord(0x6, 0x0);

		L1be1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1c44;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1bf4;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitGlobalWord(0x67f4, this.processor.RegAcc.Value16);

		L1bf4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L1bfe;
			goto L1b5d;

		L1bfe:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L1c64;
			goto L1b5d;

		L1c08:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1bce;
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L1bce;

		L1c16:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b8e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c21;
			this.processor.RegAcc.LowByte = 0xb;
			goto L1c23;

		L1c21:
			this.processor.RegAcc.LowByte = 0x7;

		L1c23:
			this.processor.EmitGlobalByte(0x3ab4, this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b8e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c31;
			this.processor.RegAcc.LowByte = 0xb;
			goto L1c33;

		L1c31:
			this.processor.RegAcc.LowByte = 0x7;

		L1c33:
			this.processor.EmitGlobalByte(0x3ab7, this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0xe);
			if (this.processor.FlagSet.IsEqual) goto L1bce;
			this.processor.EmitLocalWord(0x6, 0xc);
			goto L1bce;

		L1c44:
			this.processor.EmitGlobalWord(0x7ef4, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L1c57;
			this.processor.EmitGlobalWord(0x7ef4, 0x1);

		L1c57:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L1c64;
			this.processor.EmitGlobalWord(0x7ef4, 0x2);

		L1c64:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1caa;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1caa;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x67f4);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3ab8)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1caa;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c90;
			this.processor.EmitGlobalWord(0x67f4, 0x1);

		L1c90:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x6);
			if (this.processor.FlagSet.IsNotEqual) goto L1c9d;
			this.processor.EmitGlobalWord(0x67f4, 0xa);

		L1c9d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L1caa;
			this.processor.EmitGlobalWord(0x67f4, 0x11);

		L1caa:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x67f4);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3a94));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3aa6));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			goto L1d02;

		L1cc0:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1ccc;
			this.processor.RegAcc.Value16 = 0x5;
			goto L1ccf;

		L1ccc:
			this.processor.RegAcc.Value16 = 0xffff;

		L1ccf:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1ce7;
			this.processor.RegAcc.Value16 = 0x9;
			goto L1ce9;

		L1ce7:
			this.processor.RegAcc.Value16 = 0;

		L1ce9:
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullGlobalByte(0xd76c), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1cf8;
			this.processor.EmitLocalWord(0x2, 0x8);

		L1cf8:
			this.processor.EmitGlobalWord(0xd76c, this.processor.DecrementWord(this.processor.PullGlobalWord(0xd76c)));
			this.processor.EmitGlobalWord(0x67f6, 0x4);

		L1d02:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1d0d;
			this.processor.EmitGlobalWord(0x67f6, this.processor.DecrementWord(this.processor.PullGlobalWord(0x67f6)));

		L1d0d:
			ChoreographKingLikeness(
				this.processor.PullLocalWord(0x4),
				this.processor.PullLocalWord(0x2),
				this.processor.PullGlobalWord(0x7ef4));

			this.owner.TextPainter.AwaitTime(10);

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChoreographKingSpeaking");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayKingScreen(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayKingScreen({participantIdx})");

			// function body
			this.processor.EnterFrame(0x16);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.EmitGlobalWord(0x67f2, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x67f0, 0x0);
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L1d49;
			goto L1e2c;

		L1d49:
			this.owner.Startup.QueryForDisk(2, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1d60;
			goto L1e3e;

		L1d60:
			this.processor.EmitGlobalWord(0x67f0, 0x1);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.FloodBoxWithMotif(0, 128, 320, 72);

			this.owner.Dialogs.RenderShadowBox(0, 128, 319, 71, 15, 8);

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)));

			this.owner.Libc.AppendText(0xba06, ": ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " of the ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ".");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, 130, 15);

			this.owner.Startup.DisplayMouseCursor();

			goto L21a6;

		L1e2c:
			this.owner.Startup.QueryForDisk(2, 1);

		L1e3e:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "govt0a.pic");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x30);
			this.processor.EmitLocalByte(0xc, this.processor.RegAcc.LowByte);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x14);
			this.processor.EmitGlobalWord(0x67ee, this.processor.RegAcc.Value16);
			
			if (this.owner.World.Participants[participantIdx].RegimeKind != 3) goto L1e85;

			this.processor.EmitLocalByte(0xc, 0x33);
			goto L1e90;

		L1e85:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67ee), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1e90;
			this.processor.EmitLocalByte(0xb, 0x6d);

		L1e90:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.processor.EmitLocalWord(0x12, 0x0);

		L1ea5:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((0x50 * this.processor.PullLocalWord(0x12)) + 1),
				0x65, 0x4e, 0x63);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x6, this.processor.PullLocalWord(0x12));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x804e), this.processor.RegCount.Value16);
			this.processor.IncrementLocalWord(0x12);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1ea5;
			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[participantIdx].ChiefVisuals;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3938));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0x67f8, this.processor.RegAcc.Value16);
			
			this.owner.Startup.QueryForDisk((ushort)((this.processor.PullGlobalWord(0x67f8) / 7) + 4), 1);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "king00.pic");

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67f8);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalByte(0xb, this.processor.SumByte(this.processor.PullLocalByte(0xb), this.processor.RegData.LowByte));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67f8);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalByte(0xc, this.processor.SumByte(this.processor.PullLocalByte(0xc), this.processor.RegAcc.LowByte));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1f46;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1f42;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f8), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1f46;

		L1f42:
			this.processor.EmitLocalByte(0xd, 0x6b);

		L1f46:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			this.processor.EmitGlobalWord(0xb1e6, this.processor.RegAcc.Value16);

			ImportKingScenery(participantIdx);
			
			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x804e), 0x3aee);
			
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.processor.EmitLocalWord(0x14, 0x0);
			this.processor.EmitLocalWord(0x12, 0x0);

		L1fb3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L1fbf;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L1fc3;

		L1fbf:
			this.processor.EmitLocalWord(0x14, this.processor.SumWord(this.processor.PullLocalWord(0x14), 0x2));

		L1fc3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x5;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegData.Value16;
			this.processor.RegCount.Value16 = 0x3c;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x5;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1), 0x3b, 0x14);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7efe), this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegSource.Value16, (ushort)(this.processor.RegDest.Value16 + 0x15), 0x3b, 0x1d);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f00), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x14);
			this.processor.IncrementLocalWord(0x12);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xe);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L203f;
			goto L1fb3;

		L203f:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.QueryForDisk(2, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2076;
			goto L217a;

		L2076:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "govt0a.pic");

			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x30);
			this.processor.EmitLocalByte(0xc, this.processor.RegAcc.LowByte);
			this.processor.CompareWord(this.processor.RegSource.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L20a6;
			this.processor.EmitLocalByte(0xc, 0x33);
			goto L20b1;

		L20a6:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67ee), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L20b1;
			this.processor.EmitLocalByte(0xb, 0x6d);

		L20b1:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.processor.EmitLocalWord(0x12, 0x0);
			goto L210f;

		L20c8:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));

		L20cb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L210c;

			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((0x1d * this.processor.PullLocalWord(0x14)) + 0xa1),
				(ushort)((0x19 * this.processor.PullLocalWord(0x12)) + 1),
				0x1c, 0x18);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToSource(0x6, this.processor.PullLocalWord(0x12));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x804e), this.processor.RegCount.Value16);
			goto L20c8;

		L210c:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));

		L210f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L211c;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L20cb;

		L211c:
			this.processor.EmitGlobalWord(0x67fa, 0x1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);
			goto L21a0;

		L217a:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.processor.EmitGlobalWord(0x67fa, 0x0);

		L21a0:
			this.processor.EmitGlobalWord(0x67ea, 0x1);

		L21a6:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayKingScreen");
		}

		/// <summary>
		/// Unless 0x67f0 is set: shows the throne room of the player, the background "backNa.pic" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" when 0x67ee is set) with (Standing + 1) / 2 figures (columns at 0x3afe, pictures at
		/// 0x804e) and the king (0xb1e6), with the colours of "kingNN.pal" (NN is 0x67f8) and of the background. The file names are
		/// made in buffers in the stack frame, like in the original.
		/// </summary>
		public void ImportKingScenery(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ImportKingScenery({participantIdx})");

			this.processor.EnterFrame(0x1e);
			this.processor.PreserveSource();

			if (this.processor.PullGlobalWord(0x67f0) == 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				ushort sceneryName = (ushort)(this.processor.RegFrame.Value16 - 0xe);
				this.owner.Libc.CopyText(sceneryName, "back0a.pic");

				short regime = this.owner.World.Participants[participantIdx].RegimeKind;
				this.processor.EmitGlobalByte((ushort)(sceneryName + 4), (byte)((regime / 2) + 0x30));

				if (regime == 3)
				{
					this.processor.EmitGlobalByte((ushort)(sceneryName + 4), 0x33);
				}
				else if (this.processor.PullGlobalWord(0x67ee) != 0)
				{
					this.processor.EmitGlobalByte((ushort)(sceneryName + 5), 0x6d);
				}

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, sceneryName, 0);

				for (int figureIdx = 0; (short)((this.owner.World.Participants[participantIdx].Standing + 1) >> 1) > figureIdx; figureIdx++)
				{
					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
						(short)this.processor.PullGlobalWord((ushort)((figureIdx << 1) + 0x3afe)), 34,
						this.processor.PullGlobalWord((ushort)((((figureIdx + participantIdx) & 3) * 6) + 0x804e)));
				}

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4), 90, 0, this.processor.PullGlobalWord(0xb1e6));

				this.processor.EmitGlobalWord(0x67f4, 0);

				// The background's colours: "backNa.pal"
				this.owner.Libc.CopyText((ushort)(sceneryName + 7), "pal");

				ushort hueName = (ushort)(this.processor.RegFrame.Value16 - 0x1c);
				this.owner.Libc.CopyText(hueName, "king00.pal");

				short king = (short)this.processor.PullGlobalWord(0x67f8);
				this.processor.EmitGlobalByte((ushort)(hueName + 5), (byte)(this.processor.PullGlobalByte((ushort)(hueName + 5)) + king % 10));
				this.processor.EmitGlobalByte((ushort)(hueName + 4), (byte)(this.processor.PullGlobalByte((ushort)(hueName + 4)) + king / 10));

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, hueName, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, sceneryName, 0xbdee);

				// The colours 0x40 - 0x8f of the king over the background's
				for (ushort place = 0xc0; place < 0x1b0; place++)
				{
					this.processor.EmitGlobalByte((ushort)(place + 0xbdf4), this.processor.PullGlobalByte((ushort)(place + 0xc1dc)));
				}

				// The loop counter's last value stays in the frame, like in the original
				this.processor.EmitLocalWord(0x1e, 0x1b0);

				this.owner.Helpers.HaltHueCycling();

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

				this.owner.VideoCard.AssignHuesFromHueStruct(0xbdee);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("'LoadKingBackground'");
		}

		/// <summary>
		/// Ends the meeting music and frees the picture of the king (0x7efe), when one is loaded (0x67ea)
		/// </summary>
		public void ReleaseKingResources()
		{
			this.processor.Journal.OpenScope("ReleaseKingResources()");

			this.owner.Services.PerformSound(1, 0);

			if (this.processor.PullGlobalWord(0x67ea) != 0)
			{
				this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x7efe), 0);
			}

			this.processor.EmitGlobalWord(0x67ea, 0x0);

			this.processor.Journal.CloseScope("ReleaseKingResources");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		public void ChoreographKingLikeness(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ChoreographKingLikeness({arg1}, {arg2}, {arg3})");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2374;
			goto L2517;

		L2374:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegAcc.Value16 = arg3;
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x67f2)].RegimeKind;
			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.HalveAccumulator();
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xa, this.processor.RegData.Value16);
			this.processor.CompareWord(this.processor.RegSource.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L23a4;
			this.processor.EmitLocalWord(0xa, 0x6);
			goto L23af;

		L23a4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67ee), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L23af;
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x3));

		L23af:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L23b8;
			goto L2485;

		L23b8:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67e8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L23c3;
			goto L2485;

		L23c3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67fa), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L23cd;
			goto L2485;

		L23cd:
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L246d;

		L23d5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x67f2));
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3afe));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x50, this.processor.PullLocalWord(0xc));
			this.processor.SegExtra.Value16 = 0x36fa; // segment
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x80));
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegCount.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x82));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa0);
			this.processor.EmitLocalWord(0x4, this.processor.SubtractWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0xf0));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x64);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.ScaleIndex(0x19, this.processor.PullLocalWord(0xc));
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0xf2));
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), this.processor.RegCount.Value16));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6) + 34,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((this.processor.PullLocalWord(0xe) << 1) +
					(0x6 * this.processor.PullLocalWord(0xc)) + 0x804e)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.EmitGlobalWord(0x67e8, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x8);

		L246d:
			this.processor.RegAcc.Value16 = (ushort)((this.owner.World.Participants[this.processor.PullGlobalWord(0x67f2)].Standing + 1) >> 1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			if (this.processor.FlagSet.IsLessOrEqual) goto L2485;
			goto L23d5;

		L2485:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x67f8);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x5a);
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x2;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.SegExtra.Value16 = 0x36fa; // segment
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x10));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xb4);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x12));
			this.processor.EmitLocalWord(0x4, this.processor.SubtractWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x48));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x40);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4a));
			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.CompareWord(arg1, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L24eb;
			
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6) - 2,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg1 << 2) + 0x7efe)));

		L24eb:
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2512;
			
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6) + 18,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg2 << 2) + 0x7f00)));

		L2512:
			this.owner.Startup.DisplayMouseCursor();

		L2517:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChoreographKingLikeness");
		}

		/// <summary>
		/// A dialog over the bottom part of the king screen (rows 128 - 199, kept in the back screen 0x19d4 and put back);
		/// column 20 is moved to 36
		/// </summary>
		/// <returns>The answer of DisplayMenu, also in AX</returns>
		public ushort DisplayKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"DisplayKingDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0x80, 0x140, 0x48,
				this.processor.PullGlobalWord(0x19d4), 0, 0x80);

			if (horizLoc == 0x14)
			{
				horizLoc = 0x24;
			}

			this.owner.Startup.DisplayMouseCursorFully();

			this.owner.Dialogs.DisplayMenu(charsAddr, (short)horizLoc, (short)vertLoc, 1);

			ushort answer = this.processor.RegAcc.Value16;

			this.owner.Startup.ConcealMouseCursorToSavedLevel();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0x80, 0x140, 0x48,
				this.processor.PullGlobalWord(0xaa), 0, 0x80);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("DisplayKingDialog");

			return answer;
		}
	}
}
