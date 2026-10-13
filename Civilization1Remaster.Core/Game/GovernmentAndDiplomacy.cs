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
		/// The player's government becomes regimeKind. A human player (ParticipantMarkers) chooses it from the governments it
		/// may have (all with the wonder effect 1, else despotism plus those of the technologies 5, 0x2b, 0x2c and 4; regimeKind 0
		/// gives anarchy without asking), then the revolution screen. For an AI player a known civilization (0x40 of the human's
		/// relation) is announced as overthrown (anarchy, 4 of the relation 0) or as changed to the new government after it;
		/// the luxuries become 4 - government / 2. A new government goes into the replay and ends the 8 of every relation.
		/// </summary>
		public void SwitchGovernment(short participantIdx, short regimeKind)
		{
			this.processor.Journal.OpenScope($"SwitchGovernment({participantIdx}, {regimeKind})");

			// DisplayDialog leaves SI in DI: both are given back to the caller as they were
			ushort source = this.processor.RegSource.Value16;
			ushort dest = this.processor.RegDest.Value16;

			Player participant = this.owner.World.Participants[participantIdx];
			short formerRegime = participant.RegimeKind;

			if (((1 << (byte)participantIdx) & (ushort)this.owner.World.ParticipantMarkers) != 0)
			{
				short chosen;

				while (true)
				{
					this.owner.Libc.CopyText(0xba06, "Select type of\nGovernment...\n ");

					List<short> regimes = new List<short>();

					for (short regime = 1; regime < 6; regime++)
					{
						this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);

						if (this.processor.RegAcc.Value16 == 0 && !RegimeKnown(participantIdx, regime))
							continue;

						regimes.Add(regime);

						this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((regime << 1) + 0x1966)));

						this.owner.Libc.AppendText(0xba06, "\n ");
					}

					if (regimeKind == 0)
					{
						chosen = 0;
						break;
					}

					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 64);

					short choice = (short)this.processor.RegAcc.Value16;

					if (choice >= 0)
					{
						chosen = regimes[choice];
						break;
					}
				}

				participant.RegimeKind = chosen;

				this.owner.NewGameWizard.ImportGovernmentGraphics();

				this.owner.TurnHooks.RepaintMainScreen();

				this.owner.NewGameWizard.DisplayRevolutionScreen();
			}
			else
			{
				if ((participant.RegimeKind != regimeKind || (participant.Relations[0] & 4) != 0) &&
					(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.processor.EmitGlobalByte(0xba06, 0x0);

					if ((participant.Relations[0] & 4) != 0)
					{
						this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

						this.owner.Libc.AppendText(0xba06, " government\nchanged to ");

						this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((regimeKind << 1) + 0x1966)));

						this.owner.Libc.AppendText(0xba06, "!\n");

						participant.Relations[0] &= 0xfffb;
					}
					else
					{
						this.owner.Libc.AppendText(0xba06, "The ");

						this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

						this.owner.Libc.AppendText(0xba06, " government\nhas been overthrown!\n");

						participant.Relations[0] |= 4;
					}

					this.processor.EmitGlobalWord(0x2f9e, 0x1);

					this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);
				}

				participant.RegimeKind = regimeKind;
				participant.LevyRatio = (short)(4 - (regimeKind / 2));
			}

			if (formerRegime != participant.RegimeKind)
			{
				this.owner.UnitOps.AttachReplayContent(8, (byte)participantIdx, (byte)participant.RegimeKind);

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					participant.Relations[otherIdx] &= 0xfff7;
				}
			}

			this.processor.RegSource.Value16 = source;
			this.processor.RegDest.Value16 = dest;

			// Far return
			this.processor.Journal.CloseScope("SwitchGovernment");
		}

		/// <summary>The player knows the technology of the government: despotism always, monarchy 5, communism 0x2b, democracy 0x2c, republic 4</summary>
		private bool RegimeKnown(short participantIdx, short regime)
		{
			short scienceIdx = regime switch { 2 => 5, 3 => 0x2b, 4 => 0x2c, 5 => 4, _ => -1 };

			return scienceIdx < 0 || this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0;
		}

		/// <summary>
		/// Two players (not the barbarians) meet: with the human player an audience with the other one (unless in contact and
		/// met in the last 16 turns); both get contact (0x11). Two AI players (one meeting in four, or the first one) without
		/// vendetta (8) decide: when one of them wants war (AIWeighsTreaty, without the wonder effect 7 of the other and with a
		/// government below 4) a treaty or a first contact ends in war, else they exchange technologies and make peace; the
		/// human player hears of it when it knows one of them (0x40).
		/// </summary>
		public void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"BargainPeaceTreaty({participantIdx1}, {participantIdx2}, {horizLoc}, {vertLoc})");

			// DisplayDialog leaves SI in DI: both are given back to the caller as they were
			ushort source = this.processor.RegSource.Value16;
			ushort dest = this.processor.RegDest.Value16;

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				Bargain(participantIdx1, participantIdx2, horizLoc, vertLoc);
			}

			this.processor.RegSource.Value16 = source;
			this.processor.RegDest.Value16 = dest;

			// Far return
			this.processor.Journal.CloseScope("BargainPeaceTreaty");
		}

		private void Bargain(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			Player participant1 = this.owner.World.Participants[participantIdx1];
			Player participant2 = this.owner.World.Participants[participantIdx2];
			short humanIdx = this.owner.World.PersonParticipantIdx;

			if (participantIdx1 == humanIdx &&
				((participant1.Relations[participantIdx2] & 1) == 0 || (short)(this.owner.World.RoundTally - participant2.TouchParticipantTimeleft) >= 0x10))
			{
				this.owner.Audience.LaunchAudience(participantIdx2, horizLoc, vertLoc, 0);
			}

			if (participantIdx2 == humanIdx &&
				((participant1.Relations[participantIdx2] & 1) == 0 || (short)(this.owner.World.RoundTally - participant1.TouchParticipantTimeleft) >= 0x10))
			{
				this.owner.Audience.LaunchAudience(participantIdx1, horizLoc, vertLoc, 0);
			}

			participant1.Relations[participantIdx2] |= 0x11;
			participant2.Relations[participantIdx1] |= 0x11;

			if (participantIdx1 == humanIdx || participantIdx2 == humanIdx)
				return;

			if ((((byte)participantIdx1 + (byte)participantIdx2 + (byte)this.owner.World.RoundTally) & 3) != 0 &&
				(participant1.Relations[participantIdx2] & 1) != 0)
				return;

			if ((participant1.Relations[participantIdx2] & 8) != 0 || (participant2.Relations[participantIdx1] & 8) != 0)
				return;

			bool war = WantsWar(participantIdx1, participantIdx2) || WantsWar(participantIdx2, participantIdx1);

			if (!war)
			{
				SwapTechnology(participantIdx1, participantIdx2);

				if ((participant1.Relations[participantIdx2] & 2) != 0)
					return;

				if ((this.owner.World.Participants[humanIdx].Relations[participantIdx1] & 0x40) != 0 ||
					(this.owner.World.Participants[humanIdx].Relations[participantIdx2] & 0x40) != 0)
				{
					AppendPair(participantIdx1, participantIdx2);

					this.owner.Libc.AppendText(0xba06, " sign\na peace treaty.\n");

					this.processor.EmitGlobalWord(0x2f9e, 0x1);

					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				}

				AssignDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}
			else
			{
				if ((participant1.Relations[participantIdx2] & 2) == 0 && (participant1.Relations[participantIdx2] & 1) != 0)
					return;

				if ((this.owner.World.Participants[humanIdx].Relations[participantIdx1] & 0x40) != 0 ||
					(this.owner.World.Participants[humanIdx].Relations[participantIdx2] & 0x40) != 0)
				{
					AppendPair(participantIdx1, participantIdx2);

					this.owner.Libc.AppendText(0xba06, (participant1.Relations[participantIdx2] & 2) != 0 ? (ushort)0x2a36 : (ushort)0x2a4d);

					this.processor.EmitGlobalWord(0x2f9e, 0x1);

					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				}

				WipeDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}
		}

		/// <summary>The AI player wants war with the other one: AIWeighsTreaty, the other one without the wonder effect 7, a government below 4</summary>
		private bool WantsWar(short participantIdx, short otherIdx)
		{
			if (AIWeighsTreaty(participantIdx, otherIdx) == 0)
				return false;

			this.owner.CityOps.PlayerEnjoysWonderEffect(otherIdx, 7);

			return this.processor.RegAcc.Value16 == 0 && this.owner.World.Participants[participantIdx].RegimeKind < 4;
		}

		/// <summary>"X and\nY" of the two civilizations at 0xba06</summary>
		private void AppendPair(short participantIdx1, short participantIdx2)
		{
			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx1 << 1) + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " and\n");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx2 << 1) + 0x1992)));
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
		/// Whether the AI player 1 accepts peace with the AI player 2 (only with the 1 of the diagnostic markers, else always):
		/// not when the other one is weaker than the human player and the first is at peace with the human (without the 4 of the
		/// other's relation), not with fewer spaceship parts (unit 25), not when on a continent with its cities its defence (half)
		/// plus attack is below the other's defence; then it accepts when 4 * its defence / (1 + the other's attack) on the
		/// shared continents is at least its peace treaties with the others (+1 with the 2 of the relation) minus the leader's
		/// trait (0x1512 + 0x3a * leader) plus 4, or when it has no cities on them.
		/// </summary>
		/// <returns>1 when it accepts, else 0, also in AX</returns>
		public ushort AIWeighsTreaty(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"AIWeighsTreaty({participantIdx1}, {participantIdx2})");

			ushort accepted = WeighTreaty(participantIdx1, participantIdx2) ? (ushort)1 : (ushort)0;

			this.processor.RegAcc.Value16 = accepted;

			// Far return
			this.processor.Journal.CloseScope("AIWeighsTreaty");

			return accepted;
		}

		private bool WeighTreaty(short participantIdx1, short participantIdx2)
		{
			if ((this.owner.World.DiagMarkers & 1) == 0)
				return true;

			short humanIdx = this.owner.World.PersonParticipantIdx;
			Player participant1 = this.owner.World.Participants[participantIdx1];
			Player participant2 = this.owner.World.Participants[participantIdx2];

			if ((participant1.Relations[humanIdx] & 3) == 1 && participant2.MartialMight < this.owner.World.Participants[humanIdx].MartialMight &&
				(participant2.Relations[humanIdx] & 4) == 0)
				return false;

			if ((participant2.Relations[humanIdx] & 3) == 1 && participant1.MartialMight < this.owner.World.Participants[humanIdx].MartialMight &&
				(participant1.Relations[humanIdx] & 4) == 0)
				return false;

			if (participant1.LiveTroops[25] < participant2.LiveTroops[25])
				return false;

			short treaties = 0;

			for (int otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx != participantIdx1 && otherIdx != participantIdx2 && (participant1.Relations[otherIdx] & 3) == 1)
				{
					treaties++;
				}
			}

			if ((participant1.Relations[participantIdx2] & 2) != 0)
			{
				treaties++;
			}

			treaties = (short)(treaties - this.processor.PullGlobalWord((ushort)(0x1512 + (0x3a * participant1.ChiefVisuals))));

			short towns = 0;
			short defence = 0;
			short attack = 1;

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent1 = participant1.Landmasses[continentIdx];
				PlayerContinent continent2 = participant2.Landmasses[continentIdx];

				if (continent1.TownTally != 0 && (short)((continent1.Guard / 2) + continent1.Assault) < continent2.Guard)
					return false;

				if (continent1.Guard == 0 || continent2.Assault == 0)
					continue;

				defence += continent1.Guard;
				attack += (short)((continent2.Guard / 2) + continent2.Assault);
				towns += continent1.TownTally;
			}

			return CPU.SignedQuotient((short)(defence << 2), attack) >= (short)(treaties + 4) || towns == 0;
		}

		/// <summary>
		/// Two players exchange technologies when each knows one the other does not (the last such of the 72 for each side)
		/// </summary>
		/// <remarks>In AX: 1 when they exchanged, else 0</remarks>
		public void SwapTechnology(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"SwapTechnology({participantIdx1}, {participantIdx2})");

			short firstGains = -1, secondGains = -1;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx1, scienceIdx) == 0 &&
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx2, scienceIdx) != 0)
				{
					firstGains = scienceIdx;
				}

				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx2, scienceIdx) == 0 &&
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx1, scienceIdx) != 0)
				{
					secondGains = scienceIdx;
				}
			}

			if (firstGains >= 0 && secondGains >= 0)
			{
				this.owner.Advisors.EarnTechnology(participantIdx1, firstGains, participantIdx2);

				this.owner.Advisors.EarnTechnology(participantIdx2, secondGains, participantIdx1);

				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
			}

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
