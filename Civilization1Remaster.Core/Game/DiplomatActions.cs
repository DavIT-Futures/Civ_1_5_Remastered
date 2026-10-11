using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

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
			int victimPlayerIdx = this.processor.PullLocalWord(0x26);
			int victimGold = this.owner.World.Participants[victimPlayerIdx].Gold;
			int distanceToCapital = this.processor.PullLocalWord(0xc);
			int citySize = this.owner.World.Towns[townIdx].TrueExtent;
			bool inDisorder = (this.owner.World.Towns[townIdx].ConditionMarker & 0x1) != 0;

			int inciteCost = DiplomatRules.ComputeInciteRevoltCost(victimGold, distanceToCapital, citySize, inDisorder);
			this.processor.EmitLocalWord(0x8, (ushort)inciteCost);

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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="playerID1"></param>
		public void BuyUnit(short participantIdx, short troopIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"BuyUnit({participantIdx}, {troopIdx}, {participantIdx1})");

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 2);
			if (this.processor.RegAcc.Value16 > 1)
			{
				this.processor.Journal.CloseScope("BuyUnit");
				return;
			}

			int unitX = this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.X;
			int unitY = this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.Y;

			this.owner.Politics.LocateNearestCityOfPlayer(participantIdx, (sbyte)unitX, (sbyte)unitY);
			int distance = this.processor.RegAcc.Value16;

			int unitType = this.owner.World.Participants[participantIdx].Troops[troopIdx].KindIdx;
			int unitDefOffset = unitType * 0x22;
			int unitCost = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(unitDefOffset + 0x1142));
			int victimGold = this.owner.World.Participants[participantIdx].Gold;

			int bribeCost = DiplomatRules.ComputeBribeCost(victimGold, distance, unitCost, unitType);

			if (participantIdx1 != this.owner.World.PersonParticipantIdx)
			{
				if ((this.owner.World.Participants[participantIdx1].Gold / 2) < bribeCost)
				{
					this.processor.Journal.CloseScope("BuyUnit");
					return;
				}
			}

			ushort victimCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
			this.owner.Libc.CopyText(0xba06, victimCivNamePtr);
			this.owner.Libc.AppendText(0xba06, " ");
			this.owner.Libc.AppendText(0xba06, (ushort)(unitType * 0x22 + 0x112a));
			this.owner.Libc.AppendText(0xba06, "\nwill desert for ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)bribeCost, 10));
			this.owner.Libc.AppendText(0xba06, "$.\nTreasury: ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx1].Gold, 10));
			this.owner.Libc.AppendText(0xba06, "$\n");

			if (bribeCost <= this.owner.World.Participants[participantIdx1].Gold)
			{
				this.owner.Libc.AppendText(0xba06, " Forget it.\n Pay\n");
			}

			if (participantIdx1 == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				if (this.processor.RegAcc.Value16 != 1)
				{
					this.processor.Journal.CloseScope("BuyUnit");
					return;
				}
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.FocusMapView(participantIdx, unitX - 8, unitY - 6);
				this.owner.Libc.CopyText(0xba06, victimCivNamePtr);
				this.owner.Libc.AppendText(0xba06, " ");
				this.owner.Libc.AppendText(0xba06, (ushort)(unitType * 0x22 + 0x112a));
				this.owner.Libc.AppendText(0xba06, " unit\nbribed by ");
				ushort buyerCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992));
				this.owner.Libc.AppendText(0xba06, buyerCivNamePtr);
				this.owner.Libc.AppendText(0xba06, "!\n");
				this.processor.EmitGlobalWord(0x2f9e, 0x0);
				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 40);
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			this.owner.World.Participants[participantIdx].LiveTroops[unitType]--;

			this.owner.UnitOps.ConstructUnit(participantIdx1, (short)unitType, (sbyte)unitX, (sbyte)unitY);
			short newTroopIdx = (short)this.processor.RegAcc.Value16;
			if (newTroopIdx != -1)
			{
				this.owner.World.Participants[participantIdx1].LiveTroops[unitType]++;
				this.owner.World.Participants[participantIdx1].Troops[newTroopIdx].ShownViaParticipant |= (ushort)(1 << participantIdx);
			}

			this.owner.World.Participants[participantIdx1].Gold -= (short)bribeCost;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile((sbyte)unitX, (sbyte)unitY);
				this.owner.TurnHooks.RenderStatusBar();
				this.owner.TextPainter.AwaitTime(30);
			}

			this.processor.Journal.CloseScope("BuyUnit");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void RenderExplosion(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderExplosion({horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x3a);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0985;
			goto L0af0;

		L0985:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x5318, 1);

			this.processor.EmitLocalWord(0x3a, 0x0);

		L099f:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x3a) % 7) * 0x2d) + 1),
				(ushort)(((this.processor.PullLocalWord(0x3a) / 7) * 0x2d) + 1),
				0x2c, 0x2c);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x38), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x3a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L099f;

			this.owner.Routing.TunePosition(
				horizLoc - this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x42);
			horizLoc = (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)vertLoc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			vertLoc = (short)this.processor.RegAcc.Value16;

			this.owner.Helpers.VerifyValueSpan(horizLoc, 80, 276);

			horizLoc = (short)this.processor.RegAcc.Value16;

			this.owner.Helpers.VerifyValueSpan(vertLoc, 8, 0x9c);

			vertLoc = (short)this.processor.RegAcc.Value16;

			this.owner.Services.PerformSound(0x2a, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				0x2c, 0x2c,
				this.processor.PullGlobalWord(0x19d4),
				horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L0abe;

		L0a71:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, 0x7);

		L0a77:
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x38)));

			this.owner.TextPainter.AwaitTime(5);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				horizLoc, vertLoc,
				0x2c, 0x2c,
				this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc);

			this.processor.IncrementLocalWord(0x3a);

		L0abe:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x23);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0acf;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a71;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3a);
			goto L0a77;

		L0acf:
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x38), 0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.DisplayCivilizationScenery();

		L0af0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderExplosion");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="playerID"></param>
		public void ComputeInciteCost(short townIdx, short participantIdx)
		{
			this.processor.Journal.OpenScope($"ComputeInciteCost({townIdx}, {participantIdx})");

			short originalOwner = this.owner.World.Towns[townIdx].ParticipantIdx;
			int cityX = this.owner.World.Towns[townIdx].Place.X;
			int cityY = this.owner.World.Towns[townIdx].Place.Y;

			if (participantIdx == -1)
			{
				int bestScore = 0;
				short bestTownCandidate = -1;

				for (short otherTownIdx = 0; otherTownIdx < 128; otherTownIdx++)
				{
					if (otherTownIdx == townIdx)
						continue;

					if (this.owner.World.Towns[otherTownIdx].ConditionMarker == 0xff)
						continue;

					if (this.owner.World.Towns[otherTownIdx].ParticipantIdx == originalOwner)
						continue;

					int dist = TradeRules.ComputeDistance(
						cityX, cityY,
						this.owner.World.Towns[otherTownIdx].Place.X,
						this.owner.World.Towns[otherTownIdx].Place.Y);

					if (dist == 0)
						dist = 1;

					this.owner.CityOps.HandleCity(otherTownIdx, -1);
					int score = ((this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4)) << 5) / dist;

					if (score > bestScore)
					{
						bestScore = score;
						bestTownCandidate = otherTownIdx;
					}
				}

				if (bestScore > 0 && bestTownCandidate != -1)
				{
					participantIdx = this.owner.World.Towns[bestTownCandidate].ParticipantIdx;
				}
			}

			if (participantIdx == -1)
			{
				this.processor.Journal.CloseScope("ComputeInciteCost");
				return;
			}

			sbyte mask = (sbyte)(1 << participantIdx);
			this.owner.World.ChartSight[cityX, cityY] |= mask;

			for (int i = 0; i < 20; i++)
			{
				int dx = this.owner.TownDeltas[i].X;
				int dy = this.owner.TownDeltas[i].Y;
				this.owner.World.ChartSight[cityX + dx, cityY + dy] |= mask;
			}

			short humanPlayer = this.owner.World.PersonParticipantIdx;
			bool showMessage = (originalOwner == humanPlayer || participantIdx == humanPlayer);
			if (!showMessage)
			{
				bool humanKnowsOriginal = (this.owner.World.Participants[humanPlayer].Relations[originalOwner] & 0x40) != 0;
				bool humanKnowsNew = (this.owner.World.Participants[humanPlayer].Relations[participantIdx] & 0x40) != 0;
				showMessage = humanKnowsOriginal || humanKnowsNew;
			}

			if (showMessage)
			{
				if ((this.owner.World.ChartSight[cityX, cityY] & (1 << humanPlayer)) != 0)
				{
					this.owner.MapOps.FocusMapView(humanPlayer, cityX - 8, cityY - 5);
				}

				ushort origCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((originalOwner << 1) + 0x1992));
				this.owner.Libc.CopyText(0xba06, origCivNamePtr);

				if (participantIdx == 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x5322);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, 0x533c);
				}

				this.owner.Conquest.ObtainCityLabel(townIdx);

				if (participantIdx == 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x536c);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, ".\n");
					ushort newCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
					this.owner.Libc.AppendText(0xba06, newCivNamePtr);
					this.owner.Libc.AppendText(0xba06, 0x5355);
				}

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 32);
			}

			for (short uIdx = 0; uIdx < 128; uIdx++)
			{
				if (this.owner.World.Participants[originalOwner].Troops[uIdx].KindIdx == 0xff)
					continue;

				int ux = this.owner.World.Participants[originalOwner].Troops[uIdx].Place.X;
				int uy = this.owner.World.Participants[originalOwner].Troops[uIdx].Place.Y;

				int uDist = TradeRules.ComputeDistance(cityX, cityY, ux, uy);
				if (uDist > 1)
					continue;

				if (uDist != 0)
				{
					this.owner.MapOps.ObtainTileImprovements((sbyte)ux, (sbyte)uy);
					if ((this.processor.RegAcc.LowByte & 1) != 0)
						continue;
				}

				int unitType = this.owner.World.Participants[originalOwner].Troops[uIdx].KindIdx;
				this.owner.UnitOps.EliminateUnit(originalOwner, uIdx);
				this.owner.MapOps.MapAssignCityOwner(participantIdx, (sbyte)ux, (sbyte)uy);
				this.owner.MapOps.TagTileOwned((sbyte)ux, (sbyte)uy);

				this.owner.UnitOps.ConstructUnit(participantIdx, (short)unitType, (sbyte)ux, (sbyte)uy);
				short newUnitIdx = (short)this.processor.RegAcc.Value16;
				if (newUnitIdx != -1)
				{
					this.owner.World.Participants[participantIdx].Troops[newUnitIdx].ShownViaParticipant |= (ushort)(1 << originalOwner);
					this.owner.World.Participants[participantIdx].Troops[newUnitIdx].BaseTownIdx = townIdx;
				}
			}

			short savedPref = this.owner.World.SessionPrefMarkers;
			this.processor.EmitGlobalWord(0x28bc, 0x1);
			if (this.processor.PullGlobalWord(0xdeba) <= 1)
			{
				this.owner.World.SessionPrefMarkers &= 0x7ff7;
			}

			this.owner.Conquest.SeizeCity(participantIdx, townIdx, 1);

			this.processor.EmitGlobalWord(0x28bc, 0x0);
			this.owner.World.SessionPrefMarkers = savedPref;

			this.owner.MapOps.MapAssignCityOwner(participantIdx,
				this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.processor.Journal.CloseScope("ComputeInciteCost");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		public void ExamineCity(short townIdx)
		{
			this.processor.Journal.OpenScope($"ExamineCity({townIdx})");

			// function body
			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0ecd;
			goto L114d;

		L0ecd:
			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x536e, 0);

			this.processor.EmitLocalWord(0xc, 0x0);

		L0ef1:
			this.owner.VideoCard.ScreenToPicture(1, 1,
				(ushort)((this.processor.PullLocalWord(0xc) << 4) + 1),
				0x2f, 0xf);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x8), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0xc);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x4);
			if (this.processor.FlagSet.IsLess) goto L0ef1;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x537b, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x5384, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " founded: ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.Libc.AppendText(0xba06, ".");

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x6);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x78, 0x140, 0x28,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitLocalWord(0x10, 0x0);

			this.owner.TurnLogic.AwaitForKey();

			this.processor.EmitLocalWord(0xa, 0x50);
			goto L1011;

		L100e:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L1011:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x78);
			if (this.processor.FlagSet.IsLess) goto L101a;
			goto L10cc;

		L101a:
			this.owner.Services.RestoreTimer();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0x28,
				this.processor.PullGlobalWord(0x19d4),
				0, 0x78);

			this.processor.EmitLocalWord(0xc, 0x0);

		L1047:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0xa));
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x3);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				(13 * this.processor.PullLocalSignedWord(0xc)) +
					this.processor.PullLocalSignedWord(0xa),
				((10 * this.processor.PullLocalWord(0xc)) & 0xf) + 120,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x8)));

			this.processor.IncrementLocalWord(0xc);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1047;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x78, 0x140, 0x28,
				this.processor.PullGlobalWord(0xaa),
				0, 0x78);

			this.processor.IncrementLocalWord(0x10);

		L10aa:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c); 
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsLess) goto L10aa;

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L10cc;
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L10cc;
			goto L100e;

		L10cc:
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x8), 0x539a);

			this.owner.Startup.QueryForDisk(0, 1);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1104;

			this.owner.Services.FloodColors(10, 0, 0, 0);

		L1104:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1123;

			this.owner.CityScene.DisplayCityScreen(townIdx, -3);

			goto L1143;

		L1123:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnHooks.DisplayCivilizationScenery();

		L1143:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

		L114d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ExamineCity");
		}
	}
}
