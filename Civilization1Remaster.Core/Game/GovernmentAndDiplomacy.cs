using Civilization1Remaster.Emulation;
using System.Runtime.InteropServices;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2517.
	/// </summary>
	public class GovernmentAndDiplomacy
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GovernmentAndDiplomacy(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void HandlePeaceTreaties(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePeaceTreaties({participantIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();
			
			if (this.owner.World.Participants[participantIdx].RegimeKind != 0)
				goto L0088;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L004c;

			this.processor.TestBitsByte((byte)(this.owner.World.RoundTally & 0xff), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L004c;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			
			if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) == 0)
				goto L003c;

			this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;
			goto L0088;

		L003c:
			SwitchGovernment(participantIdx, 1);
			
			goto L0088;

		L004c:
			this.processor.TestBitsByte((byte)(this.owner.World.RoundTally & 0xff), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0066;

			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0088;

		L0066:
			SwitchGovernment(participantIdx, 1);
			
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0088;
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;

		L0088:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L00b1;
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) == 0)
				goto L00b1;

			SwitchGovernment(participantIdx, this.owner.World.Participants[participantIdx].RegimeKind);

		L00b1:
			this.processor.EmitLocalWord(0x2, 0x1);

		L00b6:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] & 0x100) == 0)
				goto L00e9;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L00e9;

			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] &= 0xfefd;
			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] |= 0x200;
			
		L00e9:
			this.processor.TestBitsByte((byte)(this.owner.World.RoundTally & 0xff), 0xf);
			if (this.processor.FlagSet.IsEqual) goto L00f3;
			goto L0202;

		L00f3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] & 0x10) != 0)
				goto L01e2;

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] & 1) == 0)
				goto L01c2;

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] & 2) != 0)
				goto L01c2;

			this.processor.RegDest.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				goto L0141;

			this.processor.RegBase.Value16 = this.processor.RegSource.Value16;
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegDest.Value16);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x2)] & 0x40) == 0)
				goto L01c2;

		L0141:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L01c2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L01c2;

			this.owner.Libc.CopyText(0xba06, "The war between\nthe ");

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " and the\n");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " has ended.\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x1);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L01c2:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] &= 0xfffe;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[participantIdx] &= 0xfffe;

		L01e2:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] &= 0xffef;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[participantIdx] &= 0xffef;

		L0202:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L020e;
			goto L00b6;

		L020e:
			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0217;
			goto L049b;

		L0217:
			this.processor.RegAcc.LowByte = (byte)(this.owner.World.RoundTally & 0xff);
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1f);
			this.processor.RegCount.Value16 = (ushort)participantIdx;
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L022d;
			goto L049b;

		L022d:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0238;
			goto L049b;

		L0238:
			this.processor.EmitLocalWord(0x2, 0x1);
			goto L02db;

		L0240:
			this.processor.RegAcc.Value16 = 0x299b;

		L0243:
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), this.processor.RegAcc.Value16);

			this.owner.BlockText.DisplayTextDialog(0x29a6);

			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x0279); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L02c2;

			AssignDiplomacyFlags(participantIdx, this.owner.World.PersonParticipantIdx, 2);

			WipeDiplomacyFlags(this.processor.PullLocalSignedWord(0x2), this.owner.World.PersonParticipantIdx, 2);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[this.owner.World.PersonParticipantIdx] |= 8;

			this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = 
				(short)(owner.World.RoundTally + 16);

		L02c2:
			this.owner.Audience.FinishAudience();

		L02c7:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] &= 0xffdf;

			this.processor.IncrementLocalWord(0x2);

		L02db:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L02e4;
			goto L049b;

		L02e4:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.RegDest.Value16;
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegSource.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x2)] & 0x20) == 0)
				goto L02c7;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x2)] & 2) == 0)
				goto L02c7;

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 0x20) != 0)
				goto L02c7;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].MartialMight;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].MartialMight);
			if (this.processor.FlagSet.IsLess) goto L0329;
			goto L042d;

		L0329:
			this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0342;
			goto L042d;

		L0342:
			this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[participantIdx].Gold / 100, 1, 10);

			this.processor.RegCount.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x4), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x2988);

			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x03d8); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0428;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Gold += (short)this.processor.RegAcc.Value16;
			this.owner.World.Participants[participantIdx].Gold += (short)this.processor.RegAcc.Value16;

			AssignDiplomacyFlags(participantIdx, this.owner.World.PersonParticipantIdx, 2);

			WipeDiplomacyFlags(this.processor.PullLocalSignedWord(0x2), this.owner.World.PersonParticipantIdx, 2);
			
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[this.owner.World.PersonParticipantIdx] |= 8;

		L0428:
			this.owner.Audience.FinishAudience();

		L042d:
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x2)].MartialMight <=
				this.owner.World.Participants[participantIdx].MartialMight)
				goto L02c7;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].MartialMight;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x2)].MartialMight);
			if (this.processor.FlagSet.IsLess)
				goto L02c7;

			this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0468;
			goto L02c7;

		L0468:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1992)));

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L0240;

			this.processor.RegAcc.Value16 = 0x2990;
			goto L0243;

		L049b:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandlePeaceTreaties");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="governmentType"></param>
		public void SwitchGovernment(short participantIdx, short regimeKind)
		{
			this.processor.Journal.OpenScope($"SwitchGovernment({participantIdx}, {regimeKind})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveIndexRegisters();

			this.processor.EmitLocalWord(0x12, (ushort)this.owner.World.Participants[participantIdx].RegimeKind);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L04c6;
			goto L05e9;

		L04c6:
			this.owner.Libc.CopyText(0xba06, "Select type of\nGovernment...\n ");

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, 0x1);

		L04e3:
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L055a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L050f;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L058d;

		L050f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0528;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2b);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L058d;

		L0528:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0541;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L058d;

		L0541:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L055a;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 4);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L058d;

		L055a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xe), this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));

			this.owner.Libc.AppendText(0xba06, "\n ");

		L058d:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0599;
			goto L04e3;

		L0599:
			this.processor.CompareWord((ushort)regimeKind, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05cb;

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 64);

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05bd;
			goto L04c6;

		L05bd:
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xe));
			goto L05d3;

		L05cb:
			this.processor.RegAcc.Value16 = (ushort)regimeKind;

		L05d3:
			this.owner.World.Participants[participantIdx].RegimeKind = (short)this.processor.RegAcc.Value16;

			this.owner.NewGameWizard.ImportGovernmentGraphics();

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.NewGameWizard.DisplayRevolutionScreen();

			goto L06f0;

		L05e9:
			this.processor.RegAcc.Value16 = (ushort)regimeKind;
			this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].RegimeKind, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0608;

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) == 0)
				goto L06d6;

		L0608:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) == 0)
				goto L06d6;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) == 0)
				goto L067c;

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " government\nchanged to ");

			this.processor.RegBase.Value16 = (ushort)regimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;

			goto L06bc;

		L067c:
			this.owner.Libc.AppendText(0xba06, "The ");

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " government\nhas been overthrown!\n");

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[participantIdx].Relations[0] |= 4;

		L06bc:
			this.processor.EmitGlobalWord(0x2f9e, 0x1);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

		L06d6:
			this.processor.RegAcc.Value16 = (ushort)regimeKind;
			this.owner.World.Participants[participantIdx].RegimeKind = (short)this.processor.RegAcc.Value16;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;

		L06f0:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0731;

			this.owner.UnitOps.AttachReplayContent(8, (byte)participantIdx, (byte)this.owner.World.Participants[participantIdx].RegimeKind);

			this.processor.EmitLocalWord(0x10, 0x0);

		L0717:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x10)] &= 0xfff7;

			this.processor.IncrementLocalWord(0x10);

			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0717;

		L0731:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SwitchGovernment");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"BargainPeaceTreaty({participantIdx1}, {participantIdx2}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();

			this.processor.CompareWord((ushort)participantIdx1, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0748;
			goto L0a2a;

		L0748:
			this.processor.CompareWord((ushort)participantIdx2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0751;
			goto L0a2a;

		L0751:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx1, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L078d;

			this.processor.RegSource.Value16 = (ushort)participantIdx2;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) == 0)
				goto L0778;

			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx2].TouchParticipantTimeleft;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x10);
			if (this.processor.FlagSet.IsLess) goto L078d;

		L0778:
			this.owner.Audience.LaunchAudience(participantIdx2, horizLoc, vertLoc, 0);

		L078d:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx2, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L07ce;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) == 0)
				goto L07b9;

			this.processor.RegAcc.Value16 = (ushort)owner.World.RoundTally;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx1].TouchParticipantTimeleft;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x10);
			if (this.processor.FlagSet.IsLess) goto L07ce;

		L07b9:
			this.owner.Audience.LaunchAudience(participantIdx1, horizLoc, vertLoc, 0);

		L07ce:
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = (ushort)participantIdx2;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			
			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= 0x11;

			this.processor.RegDest.Value16 = (ushort)participantIdx2;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 0x11;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx1, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0802;
			goto L0a2a;

		L0802:
			this.processor.CompareWord((ushort)participantIdx2, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L080a;
			goto L0a2a;

		L080a:
			this.processor.RegAcc.LowByte = (byte)participantIdx1;
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)participantIdx2);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0821;

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) != 0)
				goto L0a2a;

		L0821:
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 0x8) != 0)
				goto L0a2a;

			this.processor.RegSource.Value16 = (ushort)participantIdx2;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 8) != 0)
				goto L0a2a;

			AIWeighsTreaty(participantIdx1, participantIdx2);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L087e;

			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx2, 7);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L087e;
			if (this.owner.World.Participants[participantIdx1].RegimeKind >= 4)
				goto L087e;
			goto L0966;

		L087e:
			AIWeighsTreaty(participantIdx2, participantIdx1);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L08b1;

			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx1, 7);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L08b1;
			
			if (this.owner.World.Participants[participantIdx2].RegimeKind >= 4)
				goto L08b1;
			goto L0966;

		L08b1:
			SwapTechnology(participantIdx1, participantIdx2);

			this.processor.RegSource.Value16 = (ushort)participantIdx2;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
				goto L0a2a;

			this.processor.RegDest.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx1] & 0x40) != 0)
				goto L08f1;

			this.processor.RegBase.Value16 = this.processor.RegSource.Value16;
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegDest.Value16);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx2] & 0x40) == 0)
				goto L0955;

		L08f1:
			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " and\n");

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " sign\na peace treaty.\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x1);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L0955:
			AssignDiplomacyFlags(participantIdx1, participantIdx2, 2);
			
			goto L0a2a;

		L0966:
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
				goto L0982;

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) != 0)
				goto L0a2a;

		L0982:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx1] & 0x40) != 0)
				goto L09a2;

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx2] & 0x40) == 0)
				goto L0a19;

		L09a2:
			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " and\n");

			this.processor.RegSource.Value16 = (ushort)participantIdx2;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x2a36);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2a4d);
			}

			this.processor.EmitGlobalWord(0x2f9e, 0x1);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L0a19:
			WipeDiplomacyFlags(participantIdx1, participantIdx2, 2);

		L0a2a:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("BargainPeaceTreaty");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		/// <param name="flags"></param>
		public void AssignDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			this.processor.Journal.OpenScope($"AssignDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			// function body
			if (markers == 2 &&
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) == 0)
			{
				this.owner.UnitOps.AttachReplayContent(3, (byte)(((ushort)participantIdx1 << 4) | (ushort)participantIdx2));
			}

			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= markers;
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= markers;

			if (markers == 2)
			{
				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xffdf;
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xffdf;
			}

			// Far return
			this.processor.Journal.CloseScope("AssignDiplomacyFlags");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		/// <param name="flags"></param>
		public void WipeDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			this.processor.Journal.OpenScope($"WipeDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			// function body
			if (markers == 2 &&
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) != 0)
			{
				this.owner.UnitOps.AttachReplayContent(2, (byte)((short)(participantIdx1 << 4) | participantIdx2));
			}
		
			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= (ushort)(~markers);
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= (ushort)(~markers);

			if ((markers & 2) != 0)
			{
				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfffb;
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfffb;
			}

			// Far return
			this.processor.Journal.CloseScope("WipeDiplomacyFlags");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		/// <returns></returns>
		public ushort AIWeighsTreaty(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"AIWeighsTreaty({participantIdx1}, {participantIdx2})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();

			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0b32;

		L0b2c:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0ce9;

		L0b32:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx1].Relations[this.owner.World.PersonParticipantIdx] & 3) != 1)
				goto L0b69;

			if (this.owner.World.Participants[participantIdx2].MartialMight >=
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight)
				goto L0b69;

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx2].Relations[this.owner.World.PersonParticipantIdx] & 4) != 0)
				goto L0b69;

		L0b64:
			this.processor.RegAcc.Value16 = 0;
			goto L0ce9;

		L0b69:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx2].Relations[this.owner.World.PersonParticipantIdx] & 3) != 1)
				goto L0b9b;

			if (this.owner.World.Participants[participantIdx1].MartialMight >=
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].MartialMight)
				goto L0b9b;

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx1].Relations[this.owner.World.PersonParticipantIdx] & 4) == 0)
				goto L0b64;

		L0b9b:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx2].LiveTroops[25];

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x38, (ushort)participantIdx1);

			if (this.owner.World.Participants[participantIdx1].LiveTroops[25] < (short)this.processor.RegCount.Value16)
				goto L0b64;

			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.EmitLocalWord(0x8, 0x1);

		L0bbf:
			this.processor.RegAcc.Value16 = (ushort)participantIdx1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0be8;
			this.processor.RegAcc.Value16 = (ushort)participantIdx2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0be8;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx1].Relations[this.processor.PullLocalWord(0x8)] & 3) == 1)
			{
				this.processor.IncrementLocalWord(0xa);
			}

		L0be8:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0bbf;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
			{
				this.processor.IncrementLocalWord(0xa);
			}
		
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx1].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1512));
			this.processor.EmitLocalWord(0xa, this.processor.SubtractWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x1);
			this.processor.EmitLocalWord(0x8, 0x1);

		L0c2e:
			this.processor.RegSource.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].TownTally == 0)
				goto L0c69;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx2].Landmasses[this.processor.PullLocalSignedWord(0x8)].Guard;

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].Guard;

			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 
				(ushort)this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].Assault);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c69;
			goto L0b64;

		L0c69:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)participantIdx1;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			if (this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].Guard == 0)
				goto L0cb7;

			this.processor.RegBase.Value16 = (ushort)participantIdx2;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx2].Landmasses[this.processor.PullLocalWord(0x8)].Assault;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0cb7;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].Guard;
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx2].Landmasses[this.processor.PullLocalSignedWord(0x8)].Guard;

			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx1].Landmasses[this.processor.PullLocalWord(0x8)].TownTally;
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L0cb7:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0cc3;
			goto L0c2e;

		L0cc3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLess) goto L0cdd;
			goto L0b2c;

		L0cdd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ce6;
			goto L0b64;

		L0ce6:
			goto L0b2c;

		L0ce9:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("AIWeighsTreaty");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		public void SwapTechnology(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"SwapTechnology({participantIdx1}, {participantIdx2})");

			// function body
			this.processor.EnterFrame(0x8);

			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.EmitLocalWord(0x6, 0x0);

		L0cff:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				participantIdx1,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0d2d;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				participantIdx2,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0d2d;

			this.processor.EmitLocalByte(0x8, this.processor.BitOrByte(this.processor.PullLocalByte(0x8), 0x1));
			this.processor.CopyLocalWord(0x6, 0x2);

		L0d2d:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				participantIdx2,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0d5b;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				participantIdx1,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0d5b;

			this.processor.EmitLocalByte(0x8, this.processor.BitOrByte(this.processor.PullLocalByte(0x8), 0x2));
			this.processor.CopyLocalWord(0x6, 0x4);

		L0d5b:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x48);
			if (this.processor.FlagSet.IsLess) goto L0cff;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0d91;
			
			this.owner.Advisors.EarnTechnology(participantIdx1,
				this.processor.PullLocalSignedWord(0x2),
				participantIdx2);
			
			this.owner.Advisors.EarnTechnology(participantIdx2,
				this.processor.PullLocalSignedWord(0x4),
				participantIdx1);
			
			this.processor.RegAcc.Value16 = 0x1;
			goto L0d93;

		L0d91:
			this.processor.RegAcc.Value16 = 0;

		L0d93:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("SwapTechnology");
		}

		/// <summary>
		/// Distance from the position to the nearest city of the player with a palace (structure bit 0), at most 16
		/// </summary>
		/// <returns>Distance, also in AX</returns>
		public ushort LocateNearestCityOfPlayer(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateNearestCityOfPlayer({participantIdx}, {horizLoc}, {vertLoc})");

			short nearest = 16;

			foreach (City town in this.owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx || (town.StructureMarkers0 & 0x1) == 0)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(town.Place.X, horizLoc, town.Place.Y, vertLoc);

				if (distance < nearest)
				{
					nearest = distance;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)nearest;

			this.processor.Journal.CloseScope("LocateNearestCityOfPlayer");

			return (ushort)nearest;
		}
	}
}
