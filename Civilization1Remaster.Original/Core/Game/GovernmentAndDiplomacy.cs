using Civilization1Original.Emulation;
using System.Runtime.InteropServices;
using Civilization1Original.Game.State;

namespace Civilization1Original.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2517.
	/// </summary>
	public class GovernmentAndDiplomacy : GameModule
	{
		public GovernmentAndDiplomacy(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The treaties of a player each turn. Anarchy ends (the human player every 4th turn, others every 4th turn or with
		/// the wonder effect 1) in despotism, with the revolution flag 4 of the relation 0 (an AI player with it goes back to
		/// its government). For every other player: a broken treaty (0x100) is forgotten one time in 3 (war again, 0x200);
		/// every 16th turn the peace of a war (1 without 2) ends with a message when the human player knows one of them,
		/// and the flags 1 and 0x10 are cleared both ways. Every 32nd turn (the player's turn, 4 * player) an AI player that
		/// asked for help (0x20) against a player at war with the human player sends an emissary: a much weaker one pays
		/// tribute for an alliance, a stronger one threatens (the name read at 0x1992 + 16 * player, not of the player, like
		/// in the original). SI and DI get the values of the original before the calls.
		/// </summary>
		public void HandlePeaceTreaties(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePeaceTreaties({participantIdx})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			short person = this.owner.World.PersonParticipantIdx;
			Player player = this.owner.World.Participants[participantIdx];

			if (player.RegimeKind == 0)
			{
				if (participantIdx == person && (this.owner.World.RoundTally & 3) == 0)
				{
					this.processor.RegSource.Value16 = (ushort)(participantIdx << 4);

					if ((player.Relations[0] & 4) != 0)
					{
						player.Relations[0] &= 0xfffb;
					}
					else
					{
						SwitchGovernment(participantIdx, 1);
					}
				}
				else if ((this.owner.World.RoundTally & 3) == 0 || EnjoysWonderEffectOne(participantIdx))
				{
					SwitchGovernment(participantIdx, 1);

					if (participantIdx == person)
						player.Relations[0] &= 0xfffb;
				}
			}

			if (participantIdx != person && (player.Relations[0] & 4) != 0)
			{
				SwitchGovernment(participantIdx, player.RegimeKind);
			}

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				this.processor.EmitLocalWord(0x2, otherIdx);
				EndOldTreaties(participantIdx, otherIdx);
			}

			this.processor.EmitLocalWord(0x2, 8);

			if (participantIdx != 0 && (this.owner.World.RoundTally & 0x1f) == (ushort)(participantIdx << 2) && participantIdx != person)
			{
				ushort otherIdx;

				for (otherIdx = 1; otherIdx < 8; otherIdx++)
				{
					this.processor.EmitLocalWord(0x2, otherIdx);
					SendWarEmissary(participantIdx, otherIdx);

					this.processor.RegSource.Value16 = (ushort)(participantIdx << 4);
					player.Relations[otherIdx] &= 0xffdf;
				}

				this.processor.EmitLocalWord(0x2, otherIdx);
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandlePeaceTreaties");
		}

		/// <summary>The wonder effect 1 for the player (AX of PlayerEnjoysWonderEffect)</summary>
		private bool EnjoysWonderEffectOne(short participantIdx)
		{
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);

			return this.processor.RegAcc.Value16 != 0;
		}

		/// <summary>The forgotten broken treaty and, every 16th turn, the end of a peace between the two (see HandlePeaceTreaties)</summary>
		private void EndOldTreaties(short participantIdx, ushort otherIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			ushort[] relations = this.owner.World.Participants[participantIdx].Relations;
			ushort[] others = this.owner.World.Participants[otherIdx].Relations;

			this.processor.RegSource.Value16 = (ushort)((participantIdx << 4) + (otherIdx << 1));

			if ((relations[otherIdx] & 0x100) != 0 && this.owner.Libc.Dice.DrawInt(3) == 0)
			{
				relations[otherIdx] &= 0xfefd;
				relations[otherIdx] |= 0x200;
			}

			if ((this.owner.World.RoundTally & 0xf) != 0)
				return;

			this.processor.RegSource.Value16 = (ushort)(otherIdx << 1);

			if ((relations[otherIdx] & 0x10) == 0)
			{
				if ((relations[otherIdx] & 1) != 0 && (relations[otherIdx] & 2) == 0)
				{
					this.processor.RegDest.Value16 = (ushort)(person << 4);

					ushort[] known = this.owner.World.Participants[person].Relations;

					if (((known[participantIdx] & 0x40) != 0 || (known[otherIdx] & 0x40) != 0) &&
						participantIdx != person && otherIdx != (ushort)person)
					{
						Message("The war between\nthe ")
							.Text(this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1992)))
							.Text(" and the\n")
							.Text(this.processor.PullGlobalWord((ushort)((otherIdx << 1) + 0x1992)))
							.Text(" has ended.\n")
							.Style(0x1)
							.Dialog(100, 80);
					}
				}

				relations[otherIdx] &= 0xfffe;
				others[participantIdx] &= 0xfffe;
			}

			this.processor.RegSource.Value16 = (ushort)(otherIdx << 4);
			relations[otherIdx] &= 0xffef;
			others[participantIdx] &= 0xffef;
		}

		/// <summary>
		/// The emissary of an AI player that asked for help (0x20) against a player at war with the human player: a much weaker
		/// one offers tribute (the treasury / 100 times 50, 50 - 500) for an alliance, a stronger one (not twice as strong)
		/// threatens; an accepted offer makes them allies against the other (see HandlePeaceTreaties)
		/// </summary>
		private void SendWarEmissary(short participantIdx, ushort otherIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			Player player = this.owner.World.Participants[participantIdx];
			Player other = this.owner.World.Participants[otherIdx];

			this.processor.RegSource.Value16 = (ushort)(otherIdx << 1);
			this.processor.RegDest.Value16 = (ushort)(participantIdx << 4);

			if ((player.Relations[otherIdx] & 0x20) == 0 ||
				(this.owner.World.Participants[person].Relations[otherIdx] & 2) == 0 ||
				(player.Relations[person] & 0x20) != 0)
				return;

			if ((short)((ushort)player.MartialMight << 1) < other.MartialMight)
			{
				this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);

				if (this.processor.RegAcc.Value16 == 0)
				{
					this.owner.Helpers.VerifyValueSpan(player.Gold / 100, 1, 10);

					short tribute = (short)(this.processor.RegAcc.Value16 * 0x32);
					this.processor.EmitLocalWord(0x4, (ushort)tribute);

					this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x1992)));

					EmptyMessage()
						.Number(tribute);

					this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

					this.processor.EmitGlobalByte(0xba06, 0);

					if (AskKing(0x2988, 0x03d8))
					{
						this.owner.World.Participants[person].Gold += tribute;
						player.Gold += tribute;

						AllyAgainst(participantIdx, otherIdx);
					}

					this.owner.Audience.FinishAudience();
				}
			}

			if (other.MartialMight <= player.MartialMight || (short)((ushort)player.MartialMight << 1) < other.MartialMight)
				return;

			this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);

			if (this.processor.RegAcc.Value16 != 0)
				return;

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), this.processor.PullGlobalWord((ushort)(this.processor.RegDest.Value16 + 0x1992)));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), (player.Relations[person] & 2) == 0 ? (ushort)0x299b : (ushort)0x2990);

			if (AskKing(0x29a6, 0x0279))
			{
				AllyAgainst(participantIdx, otherIdx);

				player.TouchParticipantTimeleft = (short)(this.owner.World.RoundTally + 16);
			}

			this.owner.Audience.FinishAudience();
		}

		/// <summary>The emissary's text, then the king's answer: true for the first choice</summary>
		private bool AskKing(ushort charsAddr, ushort returnOffset)
		{
			this.owner.BlockText.DisplayTextDialog(charsAddr);

			this.processor.EmitGlobalWord(0x3936, 0x1);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(returnOffset); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			return this.processor.RegAcc.Value16 == 1;
		}

		/// <summary>The human player and the AI player become allies against the other one (flag 8 of the other's relation)</summary>
		private void AllyAgainst(short participantIdx, ushort otherIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			AssignDiplomacyFlags(participantIdx, person, 2);

			WipeDiplomacyFlags((short)otherIdx, person, 2);

			this.owner.World.Participants[otherIdx].Relations[person] |= 8;
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

			if (this.owner.World.IsHuman(participantIdx))
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
			Message(this.processor.PullGlobalWord((ushort)((participantIdx1 << 1) + 0x1992)))
				.Text(" and\n")
				.Text(this.processor.PullGlobalWord((ushort)((participantIdx2 << 1) + 0x1992)));
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
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx || !town.Has(CityStructure.Palace))
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
