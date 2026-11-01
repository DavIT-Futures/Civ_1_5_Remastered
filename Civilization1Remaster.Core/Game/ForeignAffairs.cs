using System.Collections.Generic;

namespace Civilization1Remaster.Core.Game
{
	public class ForeignAffairs : GameComponent, IForeignAffairs
	{
		public ForeignAffairs(GameEngine owner)
			: base(owner)
		{
		}

		public void HandlePeaceTreaties(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"HandlePeaceTreaties({participantIdx})");

			short person = this.Owner.World.PersonParticipantIdx;
			Player player = this.Owner.World.Participants[participantIdx];

			if (player.RegimeKind == 0)
			{
				if (participantIdx == person && (this.Owner.World.RoundTally & 3) == 0)
				{
					if ((player.Relations[0] & 4) != 0)
					{
						player.Relations[0] &= 0xfffb;
					}
					else
					{
						SwitchGovernment(participantIdx, 1);
					}
				}
				else if ((this.Owner.World.RoundTally & 3) == 0 || EnjoysWonderEffectOne(participantIdx))
				{
					SwitchGovernment(participantIdx, 1);

					if (participantIdx == person)
					{
						player.Relations[0] &= 0xfffb;
					}
				}
			}

			if (participantIdx != person && (player.Relations[0] & 4) != 0)
			{
				SwitchGovernment(participantIdx, player.RegimeKind);
			}

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				EndOldTreaties(participantIdx, otherIdx);
			}

			if (participantIdx != 0 && (this.Owner.World.RoundTally & 0x1f) == (ushort)(participantIdx << 2) && participantIdx != person)
			{
				ushort otherIdx;

				for (otherIdx = 1; otherIdx < 8; otherIdx++)
				{
					SendWarEmissary(participantIdx, otherIdx);

					player.Relations[otherIdx] &= 0xffdf;
				}
			}
		}

		public void SwitchGovernment(short participantIdx, short regimeKind)
		{
			using var _ = this.Processor.Journal.Scope($"SwitchGovernment({participantIdx}, {regimeKind})");

			Player participant = this.Owner.World.Participants[participantIdx];
			short formerRegime = participant.RegimeKind;

			if (this.Owner.World.IsHuman(participantIdx))
			{
				short chosen;

				while (true)
				{
					this.Owner.Words.Set("Select type of\nGovernment...\n ");

					List<short> regimes = new List<short>();

					for (short regime = 1; regime < 6; regime++)
					{
						if (this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1) == 0 && !RegimeKnown(participantIdx, regime))
						{
							continue;
						}

						regimes.Add(regime);

						this.Owner.Words.Append(Tables.GovernmentName(regime));

						this.Owner.Words.Append("\n ");
					}

					if (regimeKind == 0)
					{
						chosen = 0;
						break;
					}

					short choice = (short)this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 64);

					if (choice >= 0)
					{
						chosen = regimes[choice];
						break;
					}
				}

				participant.RegimeKind = chosen;

				this.Owner.NewGameWizard.LoadGovernmentGraphics();

				this.Owner.TurnHooks.RepaintMainScreen();

				this.Owner.NewGameWizard.ShowRevolutionScreen();
			}
			else
			{
				if ((participant.RegimeKind != regimeKind || (participant.Relations[0] & 4) != 0) &&
					(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.Owner.Words.Clear();

					if ((participant.Relations[0] & 4) != 0)
					{
						this.Owner.Words.Append(Tables.CivilizationName(participantIdx));

						this.Owner.Words.Append(" government\nchanged to ");

						this.Owner.Words.Append(Tables.GovernmentName(regimeKind));

						this.Owner.Words.Append("!\n");

						participant.Relations[0] &= 0xfffb;
					}
					else
					{
						this.Owner.Words.Append("The ");

						this.Owner.Words.Append(Tables.CivilizationName(participantIdx));

						this.Owner.Words.Append(" government\nhas been overthrown!\n");

						participant.Relations[0] |= 4;
					}

					Screen.DialogStyle = 0x1;

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 64);
				}

				participant.RegimeKind = regimeKind;
				participant.LevyRatio = (short)(4 - (regimeKind / 2));
			}

			if (formerRegime != participant.RegimeKind)
			{
				this.Owner.UnitOps.AttachReplayContent(8, (byte)participantIdx, (byte)participant.RegimeKind);

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					participant.Relations[otherIdx] &= 0xfff7;
				}
			}
		}

		public void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"BargainPeaceTreaty({participantIdx1}, {participantIdx2}, {horizLoc}, {vertLoc})");

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				Bargain(participantIdx1, participantIdx2, horizLoc, vertLoc);
			}
		}

		public void AssignDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			using var _ = this.Processor.Journal.Scope($"AssignDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			if (markers == 2 &&
				(this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) == 0)
			{
				this.Owner.UnitOps.AttachReplayContent(3, (byte)(((ushort)participantIdx1 << 4) | (ushort)participantIdx2));
			}

			this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] |= markers;
			this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] |= markers;

			if (markers == 2)
			{
				this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xffdf;
				this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xffdf;
			}
		}

		public void ClearDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			using var _ = this.Processor.Journal.Scope($"WipeDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			if (markers == 2 &&
				(this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) != 0)
			{
				this.Owner.UnitOps.AttachReplayContent(2, (byte)((short)(participantIdx1 << 4) | participantIdx2));
			}

			this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] &= (ushort)(~markers);
			this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] &= (ushort)(~markers);

			if ((markers & 2) != 0)
			{
				this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfffb;
				this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfffb;
			}
		}

		public ushort AIWeighsTreaty(short participantIdx1, short participantIdx2)
		{
			using var _ = this.Processor.Journal.Scope($"AIWeighsTreaty({participantIdx1}, {participantIdx2})");

			ushort accepted = WeighTreaty(participantIdx1, participantIdx2) ? (ushort)1 : (ushort)0;

			return accepted;
		}

		public void SwapTechnology(short participantIdx1, short participantIdx2)
		{
			using var _ = this.Processor.Journal.Scope($"SwapTechnology({participantIdx1}, {participantIdx2})");

			short firstGains = -1, secondGains = -1;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx1, scienceIdx) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx2, scienceIdx) != 0)
				{
					firstGains = scienceIdx;
				}

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx2, scienceIdx) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx1, scienceIdx) != 0)
				{
					secondGains = scienceIdx;
				}
			}

			if (firstGains >= 0 && secondGains >= 0)
			{
				this.Owner.Advisors.EarnTechnology(participantIdx1, firstGains, participantIdx2);

				this.Owner.Advisors.EarnTechnology(participantIdx2, secondGains, participantIdx1);
			}
		}

		public ushort FindNearestCityOfPlayer(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestCityOfPlayer({participantIdx}, {horizLoc}, {vertLoc})");

			short nearest = 16;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx || !town.Has(CityStructure.Palace))
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(town.Place.X, horizLoc, town.Place.Y, vertLoc);

				if (distance < nearest)
				{
					nearest = distance;
				}
			}

			return (ushort)nearest;
		}

		private bool EnjoysWonderEffectOne(short participantIdx)
		{
			return this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1) != 0;
		}

		private void EndOldTreaties(short participantIdx, ushort otherIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			ushort[] relations = this.Owner.World.Participants[participantIdx].Relations;
			ushort[] others = this.Owner.World.Participants[otherIdx].Relations;

			if ((relations[otherIdx] & 0x100) != 0 && this.Owner.Libc.Dice.DrawInt(3) == 0)
			{
				relations[otherIdx] &= 0xfefd;
				relations[otherIdx] |= 0x200;
			}

			if ((this.Owner.World.RoundTally & 0xf) != 0)
			{
				return;
			}

			if ((relations[otherIdx] & 0x10) == 0)
			{
				if ((relations[otherIdx] & 1) != 0 && (relations[otherIdx] & 2) == 0)
				{
					ushort[] known = this.Owner.World.Participants[person].Relations;

					if (((known[participantIdx] & 0x40) != 0 || (known[otherIdx] & 0x40) != 0) &&
						participantIdx != person && otherIdx != (ushort)person)
					{
						Message("The war between\nthe ")
							.Text(Tables.CivilizationPlural((ushort)participantIdx))
							.Text(" and the\n")
							.Text(Tables.CivilizationPlural(otherIdx))
							.Text(" has ended.\n")
							.Style(0x1)
							.Dialog(100, 80);
					}
				}

				relations[otherIdx] &= 0xfffe;
				others[participantIdx] &= 0xfffe;
			}

			relations[otherIdx] &= 0xffef;
			others[participantIdx] &= 0xffef;
		}

		private void SendWarEmissary(short participantIdx, ushort otherIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			Player player = this.Owner.World.Participants[participantIdx];
			Player other = this.Owner.World.Participants[otherIdx];

			if ((player.Relations[otherIdx] & 0x20) == 0 ||
				(this.Owner.World.Participants[person].Relations[otherIdx] & 2) == 0 ||
				(player.Relations[person] & 0x20) != 0)
			{
				return;
			}

			if ((short)((ushort)player.MartialMight << 1) < other.MartialMight)
			{
				if (this.Owner.Audience.WelcomeEmissary(participantIdx, -1, 0) == 0)
				{
					short tribute = (short)((ushort)this.Owner.Helpers.ClampValue(player.Gold / 100, 1, 10) * 0x32);

					this.Owner.Codes.Set(1, Tables.CivilizationPlural(otherIdx));

					EmptyMessage()
						.Number(tribute);

					this.Owner.Codes.Set(2, this.Owner.Words.ToString());

					this.Owner.Words.Clear();

					if (AskKing(0x2988))
					{
						this.Owner.World.Participants[person].Gold += tribute;
						player.Gold += tribute;

						AllyAgainst(participantIdx, otherIdx);
					}

					this.Owner.Audience.FinishAudience();
				}
			}

			if (other.MartialMight <= player.MartialMight || (short)((ushort)player.MartialMight << 1) < other.MartialMight)
			{
				return;
			}

			if (this.Owner.Audience.WelcomeEmissary(participantIdx, -1, 0) != 0)
			{
				return;
			}

			this.Owner.Words.Clear();

			ushort enemyName = TurnEndRules.EmissaryEnemyName(otherIdx);
			this.Owner.Codes.Set(1, Tables.CivilizationPluralAt(enemyName));

			this.Owner.Codes.Set(3, (player.Relations[person] & 2) == 0 ? (ushort)0x299b : (ushort)0x2990);

			if (AskKing(0x29a6))
			{
				AllyAgainst(participantIdx, otherIdx);

				player.TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally + 16);
			}

			this.Owner.Audience.FinishAudience();
		}

		private bool AskKing(ushort charsAddr)
		{
			this.Owner.BlockText.ShowTextDialog(charsAddr);

			King.Mood = 0x1;
			King.Speaking = 0xc;

			return this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1;
		}

		private void AllyAgainst(short participantIdx, ushort otherIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			AssignDiplomacyFlags(participantIdx, person, 2);

			ClearDiplomacyFlags((short)otherIdx, person, 2);

			this.Owner.World.Participants[otherIdx].Relations[person] |= 8;
		}

		private bool RegimeKnown(short participantIdx, short regime)
		{
			short scienceIdx = regime switch { 2 => 5, 3 => 0x2b, 4 => 0x2c, 5 => 4, _ => -1 };

			return scienceIdx < 0 || this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0;
		}

		private void Bargain(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			Player participant1 = this.Owner.World.Participants[participantIdx1];
			Player participant2 = this.Owner.World.Participants[participantIdx2];
			short humanIdx = this.Owner.World.PersonParticipantIdx;

			if (participantIdx1 == humanIdx &&
				((participant1.Relations[participantIdx2] & 1) == 0 || (short)(this.Owner.World.RoundTally - participant2.TouchParticipantTimeleft) >= 0x10))
			{
				this.Owner.Audience.LaunchAudience(participantIdx2, horizLoc, vertLoc, 0);
			}

			if (participantIdx2 == humanIdx &&
				((participant1.Relations[participantIdx2] & 1) == 0 || (short)(this.Owner.World.RoundTally - participant1.TouchParticipantTimeleft) >= 0x10))
			{
				this.Owner.Audience.LaunchAudience(participantIdx1, horizLoc, vertLoc, 0);
			}

			participant1.Relations[participantIdx2] |= 0x11;
			participant2.Relations[participantIdx1] |= 0x11;

			if (participantIdx1 == humanIdx || participantIdx2 == humanIdx)
			{
				return;
			}

			if ((((byte)participantIdx1 + (byte)participantIdx2 + (byte)this.Owner.World.RoundTally) & 3) != 0 &&
				(participant1.Relations[participantIdx2] & 1) != 0)
			{
				return;
			}

			if ((participant1.Relations[participantIdx2] & 8) != 0 || (participant2.Relations[participantIdx1] & 8) != 0)
			{
				return;
			}

			bool war = WantsWar(participantIdx1, participantIdx2) || WantsWar(participantIdx2, participantIdx1);

			if (!war)
			{
				SwapTechnology(participantIdx1, participantIdx2);

				if ((participant1.Relations[participantIdx2] & 2) != 0)
				{
					return;
				}

				if ((this.Owner.World.Participants[humanIdx].Relations[participantIdx1] & 0x40) != 0 ||
					(this.Owner.World.Participants[humanIdx].Relations[participantIdx2] & 0x40) != 0)
				{
					AppendPair(participantIdx1, participantIdx2);

					this.Owner.Words.Append(" sign\na peace treaty.\n");

					Screen.DialogStyle = 0x1;

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
				}

				AssignDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}
			else
			{
				if ((participant1.Relations[participantIdx2] & 2) == 0 && (participant1.Relations[participantIdx2] & 1) != 0)
				{
					return;
				}

				if ((this.Owner.World.Participants[humanIdx].Relations[participantIdx1] & 0x40) != 0 ||
					(this.Owner.World.Participants[humanIdx].Relations[participantIdx2] & 0x40) != 0)
				{
					AppendPair(participantIdx1, participantIdx2);

					this.Owner.Words.Append((participant1.Relations[participantIdx2] & 2) != 0 ? (ushort)0x2a36 : (ushort)0x2a4d);

					Screen.DialogStyle = 0x1;

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
				}

				ClearDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}
		}

		private bool WantsWar(short participantIdx, short otherIdx)
		{
			if (AIWeighsTreaty(participantIdx, otherIdx) == 0)
			{
				return false;
			}

			return this.Owner.CityOps.PlayerEnjoysWonderEffect(otherIdx, 7) == 0 && this.Owner.World.Participants[participantIdx].RegimeKind < 4;
		}

		private void AppendPair(short participantIdx1, short participantIdx2)
		{
			Message(Tables.CivilizationPlural(participantIdx1))
				.Text(" and\n")
				.Text(Tables.CivilizationPlural(participantIdx2));
		}

		private bool WeighTreaty(short participantIdx1, short participantIdx2)
		{
			if ((this.Owner.World.DiagMarkers & 1) == 0)
			{
				return true;
			}

			short humanIdx = this.Owner.World.PersonParticipantIdx;
			Player participant1 = this.Owner.World.Participants[participantIdx1];
			Player participant2 = this.Owner.World.Participants[participantIdx2];

			if ((participant1.Relations[humanIdx] & 3) == 1 && participant2.MartialMight < this.Owner.World.Participants[humanIdx].MartialMight &&
				(participant2.Relations[humanIdx] & 4) == 0)
			{
				return false;
			}

			if ((participant2.Relations[humanIdx] & 3) == 1 && participant1.MartialMight < this.Owner.World.Participants[humanIdx].MartialMight &&
				(participant1.Relations[humanIdx] & 4) == 0)
			{
				return false;
			}

			if (participant1.LiveTroops[25] < participant2.LiveTroops[25])
			{
				return false;
			}

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

			treaties = (short)(treaties - Tables.LeaderWord(participant1.ChiefVisuals, LeaderTable.Aggression));

			short towns = 0;
			short defence = 0;
			short attack = 1;

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent1 = participant1.Landmasses[continentIdx];
				PlayerContinent continent2 = participant2.Landmasses[continentIdx];

				if (continent1.TownTally != 0 && (short)((continent1.Guard / 2) + continent1.Assault) < continent2.Guard)
				{
					return false;
				}

				if (continent1.Guard == 0 || continent2.Assault == 0)
				{
					continue;
				}

				defence += continent1.Guard;
				attack += (short)((continent2.Guard / 2) + continent2.Assault);
				towns += continent1.TownTally;
			}

			return CPU.SignedQuotient((short)(defence << 2), attack) >= (short)(treaties + 4) || towns == 0;
		}
	}
}
