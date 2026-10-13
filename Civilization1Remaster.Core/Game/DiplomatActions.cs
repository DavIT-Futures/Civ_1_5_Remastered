using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 22.
	/// </summary>
	public class DiplomatActions
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public DiplomatActions(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void DiplomatArrivesAtCity(short townIdx, short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DiplomatArrivesAtCity({townIdx}, {participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x28);
			this.processor.PreserveIndexRegisters();
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " diplomat arrives\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "...\n Establish Embassy\n Investigate City\n Steal Technology\n Industrial Sabotage\n Incite a Revolt\n Meet with King\n");

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L010c;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x26)] & 0x40) != 0)
				goto L0090;

			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0096;

		L0090:
			this.processor.EmitGlobalWord(0xb276, 0x1);

		L0096:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L00aa;
			this.processor.EmitGlobalByte(0xb276, this.processor.BitOrByte(this.processor.PullGlobalByte(0xb276), 0x4));

		L00aa:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L00be;
			this.processor.EmitGlobalByte(0xb276, this.processor.BitOrByte(this.processor.PullGlobalByte(0xb276), 0x10));

		L00be:
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L00c9;
			this.processor.EmitGlobalByte(0xb276, this.processor.BitOrByte(this.processor.PullGlobalByte(0xb276), 0x20));

		L00c9:
			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);

		L00dd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L014c;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L012d;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L00f1;
			goto L017a;

		L00f1:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L00f9;
			goto L02d5;

		L00f9:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0101;
			goto L0442;

		L0101:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0109;
			goto L05eb;

		L0109:
			goto L0633;

		L010c:
			this.processor.EmitLocalWord(0x24, 0x2);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L00dd;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 2) == 0)
				goto L00dd;

			goto L013b;

		L012d:
			this.owner.Advisors.RenewCityScreen(townIdx);

		L013b:
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			
			goto L0633;

		L014c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x26)] |= 0x40;

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.owner.Briefings.DisplayIntelligenceReport(this.processor.PullLocalSignedWord(0x26));

			goto L0633;

		L017a:
			if (participantIdx != this.owner.World.PersonParticipantIdx) goto L01d0;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L0194;
			goto L0633;

		L0194:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			
			this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[townIdx].ParticipantIdx);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L01b0;
			goto L0633;

		L01b0:
			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, this.owner.World.Towns[townIdx].ParticipantIdx, 2);
			
			goto L01e2;

		L01d0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L01e2;
			goto L02d5;

		L01e2:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(72));

			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x22, 0x0);
			goto L0200;

		L01fd:
			this.processor.EmitLocalWord(0x22, this.processor.IncrementWord(this.processor.PullLocalWord(0x22)));

		L0200:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x48);
			if (this.processor.FlagSet.IsLess) goto L0209;
			goto L02c0;

		L0209:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x48;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegData.Value16);

			if (participantIdx == 0) goto L01fd;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullLocalSignedWord(0x26), 
				(short)this.processor.RegData.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L01fd;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, this.processor.PullLocalSignedWord(0x4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L01fd;

			this.owner.Advisors.EarnTechnology(participantIdx, 
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " steal\n");

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);

			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x0);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker |= 0x20;

		L02c0:
			this.processor.CompareByte(this.processor.PullGlobalByte(0xba06), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L02ca;
			goto L013b;

		L02ca:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L038a;

		L02d5:
			this.processor.EmitLocalWord(0x28, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L02fc;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L02fc;
			goto L03a3;

		L02fc:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(24));

			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x63);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0341;
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L02fc;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = this.owner.World.Towns[townIdx].StructureMarkers0;
			this.processor.RegData.Value16 = this.owner.World.Towns[townIdx].StructureMarkers1;
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x22);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.BitOrWord(this.processor.RegCount.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02fc;

		L0341:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x63);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L039e;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x22);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegData.Value16;

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0x22));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " destroyed\n");
			goto L03a3;

		L038a:
			this.owner.TurnHooks.DisplayDialog(0x5231, 80, 80);

			goto L0633;

		L039e:
			this.processor.EmitLocalWord(0x28, 0x0);

		L03a3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L03fd;
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L03c9;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			goto L03e0;

		L03c9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);

		L03e0:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " production sabotaged\n");

		L03fd:
			this.owner.Libc.AppendText(0xba06, "in ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x0);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			goto L013b;

		L0442:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.Politics.LocateNearestCityOfPlayer(this.processor.PullLocalSignedWord(0x26),
				this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L046b;
			goto L0633;

		L046b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x26)].Gold;
			this.processor.RegAcc.Value16 += 0x3e8;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, 0x3);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L04a4;
			this.processor.RegCount.Value16 = 0x2;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

		L04a4:
			this.owner.Libc.CopyText(0xba06, "Dissidents in ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "\nwill revolt for $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x8), 10));

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Gold;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L054b;

			this.owner.Libc.AppendText(0xba06, " Forget it.\n Incite revolt\n");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[participantIdx].Gold);
			if (this.processor.FlagSet.IsGreater) goto L054b;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x26)] & 2) != 0)
			{
				this.owner.Libc.AppendText(0xba06, " Subvert city ($x2)\n");
			}

		L054b:
			this.processor.EmitGlobalWord(0x2f9e, 0x0);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0570;
			goto L0633;

		L0570:
			if (this.processor.FlagSet.IsNotEqual) goto L059e;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x26)].Relations[participantIdx] & 2) != 0)
			{
				this.owner.Politics.WipeDiplomacyFlags(this.processor.PullLocalSignedWord(0x26), participantIdx, 2);

				this.owner.World.Participants[this.processor.PullLocalWord(0x26)].Relations[participantIdx] |= 8;
			}

		L059e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x24));
			this.owner.World.Participants[participantIdx].Gold -= (short)this.processor.RegAcc.Value16;

			ComputeInciteCost(townIdx, participantIdx);

			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.owner.MapOps.RepaintMapTile(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			
			this.owner.TurnHooks.RenderStatusBar();

			goto L0633;

		L05eb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x26), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L061e;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Audience.LaunchAudience(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)), 1);

		L061e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L0633:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DiplomatArrivesAtCity");
		}

		/// <summary>
		/// A diplomat of participantIdx1 bribes the unit (only a unit without others in its stack): the price is (gold + 750) /
		/// (distance to the owner's capital + 2) * the unit's cost, halved for all but settlers. The AI pays when it has twice the
		/// price; the human player is asked ("Forget it." / "Pay" when the treasury covers it). The unit changes sides (seen by its
		/// former owner) and the human player, if bribed, is told. The stack frame and the saved SI and DI stay like in the
		/// original: TallyUnitsByDomainInStack changes SI.
		/// </summary>
		public void BuyUnit(short participantIdx, short troopIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"BuyUnit({participantIdx}, {troopIdx}, {participantIdx1})");

			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();

			short humanIdx = this.owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.processor);

			if ((short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 2) <= 1)
			{
				short distance = (short)this.owner.Politics.LocateNearestCityOfPlayer(participantIdx,
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				sbyte kind = troops.TroopKind(participantIdx, troopIdx);
				short price = (short)(CPU.SignedQuotient((short)(this.owner.World.Participants[participantIdx].Gold + 0x2ee), (short)(distance + 2)) *
					UnitKindTable.Field(this.processor, kind, UnitKindTable.Price));

				if (kind != 0)
				{
					price = CPU.SignedQuotient(price, 2);
				}

				if (participantIdx1 == humanIdx || this.owner.World.Participants[participantIdx1].Gold / 2 >= price)
				{
					OfferBribe(participantIdx, troopIdx, participantIdx1, price);
				}
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("BuyUnit");
		}

		/// <summary>The offer of BuyUnit and, when it is taken, the unit changing sides</summary>
		private void OfferBribe(short participantIdx, short troopIdx, short participantIdx1, short price)
		{
			short humanIdx = this.owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.processor);
			ushort kindName = (ushort)((troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride) + UnitKindTable.Origin);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));
			this.owner.Libc.AppendText(0xba06, " ");
			this.owner.Libc.AppendText(0xba06, kindName);
			this.owner.Libc.AppendText(0xba06, "\nwill desert for ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(price, 10));
			this.owner.Libc.AppendText(0xba06, "$.\nTreasury: ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx1].Gold, 10));
			this.owner.Libc.AppendText(0xba06, "$\n");

			if (price <= this.owner.World.Participants[participantIdx1].Gold)
			{
				this.owner.Libc.AppendText(0xba06, " Forget it.\n Pay\n");
			}

			if (participantIdx1 == humanIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				if (this.processor.RegAcc.Value16 != 1)
					return;
			}

			if (participantIdx == humanIdx)
			{
				this.owner.MapOps.FocusMapView(participantIdx, troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 8,
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));
				this.owner.Libc.AppendText(0xba06, " ");
				this.owner.Libc.AppendText(0xba06, kindName);
				this.owner.Libc.AppendText(0xba06, " unit\nbribed by ");
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx1 << 1) + 0x1992)));
				this.owner.Libc.AppendText(0xba06, "!\n");

				this.processor.EmitGlobalWord(0x2f9e, 0);

				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 40);
			}

			short kind = troops.TroopKind(participantIdx, troopIdx);

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.owner.World.Participants[participantIdx].LiveTroops[kind]--;

			// The record keeps its position after EliminateUnit
			ushort bribedIdx = this.owner.UnitOps.ConstructUnit(participantIdx1, kind,
				troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if (bribedIdx != 0xffff)
			{
				this.owner.World.Participants[participantIdx1].LiveTroops[kind]++;

				troops.Assign(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants) | (1 << participantIdx)));
			}

			this.owner.World.Participants[participantIdx1].Gold -= price;

			if (participantIdx == humanIdx)
			{
				this.owner.MapOps.RepaintMapTile(troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				this.owner.TurnHooks.RenderStatusBar();

				this.owner.TextPainter.AwaitTime(30);
			}
		}

		/// <summary>
		/// The nuclear explosion on the tile: 28 pictures cut from the explosion picture (0x5318) into the stack frame, the
		/// sound, then 35 steps of the animation (the last 7 repeat the pictures 21 - 27), each put on the screen for a while and
		/// taken away again from the back screen.
		/// </summary>
		public void RenderExplosion(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderExplosion({horizLoc}, {vertLoc})");

			// The pictures and the step counter stay in the stack frame, like in the original
			this.processor.EnterFrame(0x3a);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(1, 0);

			if (this.processor.RegAcc.Value16 != 0)
			{
				ushort pictures = (ushort)(this.processor.RegFrame.Value16 - 0x38);

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x5318, 1);

				for (int pictureIdx = 0; pictureIdx < 0x1c; pictureIdx++)
				{
					this.owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIdx % 7) * 0x2d) + 1), (ushort)(((pictureIdx / 7) * 0x2d) + 1), 0x2c, 0x2c);

					this.processor.EmitGlobalWord((ushort)(pictures + (pictureIdx << 1)), this.processor.RegAcc.Value16);
				}

				horizLoc = (short)((this.owner.Routing.TunePosition(horizLoc - this.processor.PullGlobalSignedWord(0xd4cc)) << 4) + 0x42);
				vertLoc = (short)(((vertLoc - this.processor.PullGlobalWord(0xd75e)) << 4) - 6);

				horizLoc = (short)this.owner.Helpers.VerifyValueSpan(horizLoc, 80, 276);
				vertLoc = (short)this.owner.Helpers.VerifyValueSpan(vertLoc, 8, 0x9c);

				this.owner.Services.PerformSound(0x2a, 0);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, 0x2c, 0x2c,
					this.processor.PullGlobalWord(0x19d4), horizLoc, vertLoc);

				for (int stepIdx = 0; stepIdx < 0x23; stepIdx++)
				{
					int pictureIdx = stepIdx < 0x1c ? stepIdx : stepIdx - 7;

					this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc,
						this.processor.PullGlobalWord((ushort)(pictures + (pictureIdx << 1))));

					this.owner.TextPainter.AwaitTime(5);

					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), horizLoc, vertLoc, 0x2c, 0x2c,
						this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc);
				}

				this.processor.EmitLocalWord(0x3a, 0x23);

				this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(pictures), 0);

				this.owner.Startup.QueryForDisk(0, 1);

				this.owner.TurnHooks.DisplayCivilizationScenery();
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderExplosion");
		}

		/// <summary>
		/// In fact the revolt of a city: it goes over to the player (for -1 to the owner of the city of another player with the
		/// highest (food surplus * 32) / distance, scanning the cities from 0 until the first city that does not qualify; when
		/// none, nothing happens). The new owner sees around the city; the human player hears of it when it is involved or knows
		/// one of them (the map shows the city when it sees it). The old owner's units in and next to the city (not in another
		/// city) change sides, then the city is seized (without the animations with one disk, 0x28bc set).
		/// </summary>
		public void ComputeInciteCost(short townIdx, short participantIdx)
		{
			this.processor.Journal.OpenScope($"ComputeInciteCost({townIdx}, {participantIdx})");

			// HandleCity reads leftovers under the stack, so it is called at the depth of the original, and the final values of the
			// locals stay in the frame
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			City town = this.owner.World.Towns[townIdx];
			short ownerIdx = town.ParticipantIdx;
			short horizLoc = (short)town.Place.X;
			short vertLoc = (short)town.Place.Y;
			short bestSurplus = 0;
			ushort bestIdx = 0xffff;

			this.processor.EmitLocalWord(0x2, (ushort)ownerIdx);
			this.processor.EmitLocalWord(0x10, (ushort)horizLoc);
			this.processor.EmitLocalWord(0x12, (ushort)vertLoc);
			this.processor.EmitLocalWord(0xa, 0x3e7);
			this.processor.EmitLocalWord(0x1c, 0x0);

			if (participantIdx == -1)
			{
				ushort otherIdx;

				for (otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					City other = this.owner.World.Towns[otherIdx];

					if (otherIdx == (ushort)townIdx || other.ConditionMarker == 0xff || other.ParticipantIdx == ownerIdx)
						break;

					int distance = this.owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y);

					this.processor.EmitLocalWord(0x8, (ushort)distance);

					this.owner.CityOps.HandleCity((short)otherIdx, -1);

					short surplus = CPU.SignedQuotient((short)((this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4)) << 5), (short)distance);

					this.processor.EmitLocalWord(0x14, (ushort)surplus);

					if (surplus > bestSurplus)
					{
						bestSurplus = surplus;
						bestIdx = otherIdx;
					}
				}

				this.processor.EmitLocalWord(0x6, otherIdx);

				if (bestSurplus != 0)
				{
					participantIdx = this.owner.World.Towns[bestIdx].ParticipantIdx;
				}
			}

			this.processor.EmitLocalWord(0xe, (ushort)bestSurplus);
			this.processor.EmitLocalWord(0x1a, bestIdx);

			if (participantIdx != -1)
			{
				Revolt(townIdx, participantIdx, ownerIdx, horizLoc, vertLoc);
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ComputeInciteCost");
		}

		/// <summary>The city goes over to the player (see ComputeInciteCost), the locals of its frame are written like in the original</summary>
		private void Revolt(short townIdx, short participantIdx, short ownerIdx, short horizLoc, short vertLoc)
		{
			City town = this.owner.World.Towns[townIdx];
			sbyte sight = (sbyte)(byte)(1 << (byte)participantIdx);

			// ES is left at the segment 0x3772, like in the original
			this.processor.SegExtra.Value16 = 0x3772;

			this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] |= sight;

			for (int deltaIdx = 0; deltaIdx < 0x14; deltaIdx++)
			{
				this.owner.World.ChartSight[(ushort)(short)(this.owner.TownDeltas[deltaIdx].X + horizLoc),
					(ushort)(short)(this.owner.TownDeltas[deltaIdx].Y + vertLoc)] |= sight;
			}

			short humanIdx = this.owner.World.PersonParticipantIdx;
			ushort[] humanRelations = this.owner.World.Participants[humanIdx].Relations;

			if (ownerIdx == humanIdx || participantIdx == humanIdx || (humanRelations[ownerIdx] & 0x40) != 0 || (humanRelations[participantIdx] & 0x40) != 0)
			{
				if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)humanIdx)) != 0)
				{
					this.owner.MapOps.FocusMapView(humanIdx, town.Place.X - 8, town.Place.Y - 5);
				}

				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((ownerIdx << 1) + 0x1992)));

				this.owner.Libc.AppendText(0xba06, participantIdx == 0 ? (ushort)0x5322 : (ushort)0x533c);

				this.owner.Conquest.ObtainCityLabel(townIdx);

				if (participantIdx == 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x536c);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, ".\n");

					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

					this.owner.Libc.AppendText(0xba06, 0x5355);
				}

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 32);
			}

			UnitMemory troops = new UnitMemory(this.processor);
			int lastDistance = 0;
			ushort? lastKind = null, lastTroop = null;

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				int distance = this.owner.Helpers.ObtainDistance(horizLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					vertLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				lastDistance = distance;

				if (troops.Pull(ownerIdx, troopIdx, UnitMemory.Kind) == 0xff || (short)distance > 1)
					continue;

				if (distance != 0)
				{
					this.owner.MapOps.ObtainTileImprovements(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
						troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

					// Not the units in another city
					if ((this.processor.RegAcc.LowByte & 1) != 0)
						continue;
				}

				sbyte kind = troops.TroopKind(ownerIdx, troopIdx);
				lastKind = (ushort)kind;

				this.owner.UnitOps.EliminateUnit(ownerIdx, troopIdx);

				this.owner.MapOps.MapAssignCityOwner(participantIdx, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				this.owner.MapOps.TagTileOwned(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				this.owner.UnitOps.ConstructUnit(participantIdx, kind, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				ushort newIdx = this.processor.RegAcc.Value16;
				lastTroop = newIdx;

				if (newIdx == 0xffff)
					continue;

				troops.Assign(participantIdx, newIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx, newIdx, UnitMemory.ShownToParticipants) | (byte)(1 << (byte)ownerIdx)));
				troops.Assign(participantIdx, newIdx, UnitMemory.BaseTown, (byte)townIdx);
			}

			this.processor.EmitLocalWord(0x16, 0x80);
			this.processor.EmitLocalWord(0x8, (ushort)lastDistance);

			if (lastKind != null)
			{
				this.processor.EmitLocalWord(0x4, lastKind.Value);
				this.processor.EmitLocalWord(0xc, lastTroop!.Value);
			}

			short preferences = this.owner.World.SessionPrefMarkers;

			this.processor.EmitLocalWord(0x18, (ushort)preferences);
			this.processor.EmitGlobalWord(0x28bc, 0x1);

			if ((short)this.processor.PullGlobalWord(0xdeba) <= 1)
			{
				this.owner.World.SessionPrefMarkers &= 0x7ff7;
			}

			this.owner.Conquest.SeizeCity(participantIdx, townIdx, 1);

			this.processor.EmitGlobalWord(0x28bc, 0x0);
			this.owner.World.SessionPrefMarkers = preferences;

			this.owner.MapOps.MapAssignCityOwner(participantIdx, town.Place.X, town.Place.Y);
		}

		/// <summary>
		/// A diplomat establishes an embassy... in fact investigates the city: the title "... founded: year." over a picture with
		/// four walking figures (cut from 0x536e into the stack frame) moving to the right until a key or a click (frame by
		/// frame, 10 timer ticks each), then the city screen (unless a key is still waiting).
		/// </summary>
		public void ExamineCity(short townIdx)
		{
			this.processor.Journal.OpenScope($"ExamineCity({townIdx})");

			// The pictures of the figures and the counters stay in the stack frame, like in the original
			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(1, 0);

			if (this.processor.RegAcc.Value16 != 0)
			{
				ushort figures = (ushort)(this.processor.RegFrame.Value16 - 0x8);

				this.owner.Helpers.HaltHueCycling();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x536e, 0);

				for (int figureIdx = 0; figureIdx < 4; figureIdx++)
				{
					this.owner.VideoCard.ScreenToPicture(1, 1, (ushort)((figureIdx << 4) + 1), 0x2f, 0xf);

					this.processor.EmitGlobalWord((ushort)(figures + (figureIdx << 1)), this.processor.RegAcc.Value16);
				}

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x537b, 0);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x5384, 1);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

				this.processor.EmitGlobalByte(0xba06, 0x0);

				this.owner.Conquest.ObtainCityLabel(townIdx);

				this.owner.Libc.AppendText(0xba06, " founded: ");

				this.owner.TurnHooks.ObtainCurrentYearAsText();

				this.owner.Libc.AppendText(0xba06, ".");

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x6);

				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 0);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0x78, 0x140, 0x28, this.processor.PullGlobalWord(0x19d4), 0, 0);

				ushort steps = 0;

				this.owner.TurnLogic.AwaitForKey();

				short horizLoc = 0x50;

				for (; horizLoc < 0x78; horizLoc++)
				{
					this.owner.Services.RestoreTimer();

					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0x28, this.processor.PullGlobalWord(0x19d4), 0, 0x78);

					for (short figureIdx = 0; figureIdx < 4; figureIdx++)
					{
						this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4), (13 * figureIdx) + horizLoc,
							((10 * figureIdx) & 0xf) + 120, this.processor.PullGlobalWord((ushort)(figures + (((figureIdx + horizLoc) & 3) << 1))));
					}

					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0x78, 0x140, 0x28, this.processor.PullGlobalWord(0xaa), 0, 0x78);

					steps++;

					while ((short)this.processor.PullGlobalWord(0x5c) < 0xa)
					{
					}

					this.owner.Startup.ObtainMouseState();

					this.owner.Libc.KeyAvailable();

					if (this.processor.RegAcc.Value16 != 0 || this.owner.GlobDb3a != 0)
						break;
				}

				this.processor.EmitLocalWord(0xa, (ushort)horizLoc);
				this.processor.EmitLocalWord(0xc, 0x4);
				this.processor.EmitLocalWord(0x10, steps);

				this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(figures), 0x539a);

				this.owner.Startup.QueryForDisk(0, 1);

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.FloodColors(10, 0, 0, 0);
				}

				this.processor.EmitGlobalByte(0xba06, 0x0);

				this.owner.Libc.KeyAvailable();

				if (this.processor.RegAcc.Value16 == 0)
				{
					this.owner.CityScene.DisplayCityScreen(townIdx, -3);
				}
				else
				{
					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

					this.owner.TurnHooks.DisplayCivilizationScenery();
				}

				this.owner.Startup.DisplayMouseCursor();

				this.owner.TurnLogic.AwaitForKey();
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ExamineCity");
		}
	}
}
