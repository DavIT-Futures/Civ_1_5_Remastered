using System.Collections.Generic;

namespace Civilization1Remaster.Core.Game
{
	public class ForeignAffairs : GameComponent, IForeignAffairs
	{
		public ForeignAffairs(GameEngine owner)
			: base(owner)
		{
		}

		public void HandlePeaceTreaties(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"HandlePeaceTreaties({participantIndex})");

			short person = this.Owner.World.PersonParticipantIndex;
			Player player = this.Owner.World.Participants[participantIndex];

			if (player.RegimeKind == 0)
			{
				if (participantIndex == person && (this.Owner.World.RoundTally & 3) == 0)
				{
					if ((player.Relations[0] & 4) != 0)
					{
						player.Relations[0] &= 0xfffb;
					}
					else
					{
						SwitchGovernment(participantIndex, 1);
					}
				}
				else if ((this.Owner.World.RoundTally & 3) == 0 || EnjoysWonderEffectOne(participantIndex))
				{
					SwitchGovernment(participantIndex, 1);

					if (participantIndex == person)
					{
						player.Relations[0] &= 0xfffb;
					}
				}
			}

			if (participantIndex != person && (player.Relations[0] & 4) != 0)
			{
				SwitchGovernment(participantIndex, player.RegimeKind);
			}

			for (ushort otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				EndOldTreaties(participantIndex, otherIndex);
			}

			if (participantIndex != 0 && (this.Owner.World.RoundTally & 0x1f) == (ushort)(participantIndex << 2) && participantIndex != person)
			{
				ushort otherIndex;

				for (otherIndex = 1; otherIndex < 8; otherIndex++)
				{
					SendWarEmissary(participantIndex, otherIndex);

					player.Relations[otherIndex] &= 0xffdf;
				}
			}
		}

		public void SwitchGovernment(short participantIndex, short regimeKind)
		{
			using var _ = this.Processor.Journal.Scope($"SwitchGovernment({participantIndex}, {regimeKind})");

			Player participant = this.Owner.World.Participants[participantIndex];
			short formerRegime = participant.RegimeKind;

			if (this.Owner.World.IsHuman(participantIndex))
			{
				short chosen;

				while (true)
				{
					this.Owner.Words.Set("Select type of\nGovernment...\n ");

					List<short> regimes = new List<short>();

					for (short regime = 1; regime < 6; regime++)
					{
						if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 1) == 0 && !RegimeKnown(participantIndex, regime))
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
					(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations[participantIndex] & 0x40) != 0)
				{
					this.Owner.Words.Clear();

					if ((participant.Relations[0] & 4) != 0)
					{
						this.Owner.Words.Append(Tables.CivilizationName(participantIndex));

						this.Owner.Words.Append(" government\nchanged to ");

						this.Owner.Words.Append(Tables.GovernmentName(regimeKind));

						this.Owner.Words.Append("!\n");

						participant.Relations[0] &= 0xfffb;
					}
					else
					{
						this.Owner.Words.Append("The ");

						this.Owner.Words.Append(Tables.CivilizationName(participantIndex));

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
				this.Owner.UnitOperations.AttachReplayContent(8, (byte)participantIndex, (byte)participant.RegimeKind);

				for (int otherIndex = 0; otherIndex < 8; otherIndex++)
				{
					participant.Relations[otherIndex] &= 0xfff7;
				}
			}
		}

		public void BargainPeaceTreaty(short firstParticipantIndex, short secondParticipantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"BargainPeaceTreaty({firstParticipantIndex}, {secondParticipantIndex}, {horizontalLocation}, {verticalLocation})");

			if (firstParticipantIndex != 0 && secondParticipantIndex != 0)
			{
				Bargain(firstParticipantIndex, secondParticipantIndex, horizontalLocation, verticalLocation);
			}
		}

		public void AssignDiplomacyFlags(short firstParticipantIndex, short secondParticipantIndex, ushort markers)
		{
			using var _ = this.Processor.Journal.Scope($"AssignDiplomacyFlags({firstParticipantIndex}, {secondParticipantIndex}, {markers})");

			if (markers == 2 &&
				(this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] & markers) == 0)
			{
				this.Owner.UnitOperations.AttachReplayContent(3, (byte)(((ushort)firstParticipantIndex << 4) | (ushort)secondParticipantIndex));
			}

			this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] |= markers;
			this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] |= markers;

			if (markers == 2)
			{
				this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] &= 0xffdf;
				this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] &= 0xffdf;
			}
		}

		public void ClearDiplomacyFlags(short firstParticipantIndex, short secondParticipantIndex, ushort markers)
		{
			using var _ = this.Processor.Journal.Scope($"WipeDiplomacyFlags({firstParticipantIndex}, {secondParticipantIndex}, {markers})");

			if (markers == 2 &&
				(this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] & markers) != 0)
			{
				this.Owner.UnitOperations.AttachReplayContent(2, (byte)((short)(firstParticipantIndex << 4) | secondParticipantIndex));
			}

			this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] &= (ushort)(~markers);
			this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] &= (ushort)(~markers);

			if ((markers & 2) != 0)
			{
				this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] &= 0xfffb;
				this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] &= 0xfffb;
			}
		}

		public ushort ComputerWeighsTreaty(short firstParticipantIndex, short secondParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"AIWeighsTreaty({firstParticipantIndex}, {secondParticipantIndex})");

			ushort accepted = WeighTreaty(firstParticipantIndex, secondParticipantIndex) ? (ushort)1 : (ushort)0;

			return accepted;
		}

		public void SwapTechnology(short firstParticipantIndex, short secondParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"SwapTechnology({firstParticipantIndex}, {secondParticipantIndex})");

			short firstGains = -1, secondGains = -1;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(firstParticipantIndex, scienceIndex) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(secondParticipantIndex, scienceIndex) != 0)
				{
					firstGains = scienceIndex;
				}

				if (this.Owner.Advisors.PlayerKnowsTechnology(secondParticipantIndex, scienceIndex) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(firstParticipantIndex, scienceIndex) != 0)
				{
					secondGains = scienceIndex;
				}
			}

			if (firstGains >= 0 && secondGains >= 0)
			{
				this.Owner.Advisors.EarnTechnology(firstParticipantIndex, firstGains, secondParticipantIndex);

				this.Owner.Advisors.EarnTechnology(secondParticipantIndex, secondGains, firstParticipantIndex);
			}
		}

		public ushort FindNearestCityOfPlayer(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestCityOfPlayer({participantIndex}, {horizontalLocation}, {verticalLocation})");

			short nearest = 16;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex || !town.Has(CityStructure.Palace))
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(town.Place.X, horizontalLocation, town.Place.Y, verticalLocation);

				if (distance < nearest)
				{
					nearest = distance;
				}
			}

			return (ushort)nearest;
		}

		private bool EnjoysWonderEffectOne(short participantIndex)
		{
			return this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 1) != 0;
		}

		private void EndOldTreaties(short participantIndex, ushort otherIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			ushort[] relations = this.Owner.World.Participants[participantIndex].Relations;
			ushort[] others = this.Owner.World.Participants[otherIndex].Relations;

			if ((relations[otherIndex] & 0x100) != 0 && this.Owner.Runtime.Dice.DrawInteger(3) == 0)
			{
				relations[otherIndex] &= 0xfefd;
				relations[otherIndex] |= 0x200;
			}

			if ((this.Owner.World.RoundTally & 0xf) != 0)
			{
				return;
			}

			if ((relations[otherIndex] & 0x10) == 0)
			{
				if ((relations[otherIndex] & 1) != 0 && (relations[otherIndex] & 2) == 0)
				{
					ushort[] known = this.Owner.World.Participants[person].Relations;

					if (((known[participantIndex] & 0x40) != 0 || (known[otherIndex] & 0x40) != 0) &&
						participantIndex != person && otherIndex != (ushort)person)
					{
						Message("The war between\nthe ")
							.Text(Tables.CivilizationPlural((ushort)participantIndex))
							.Text(" and the\n")
							.Text(Tables.CivilizationPlural(otherIndex))
							.Text(" has ended.\n")
							.Style(0x1)
							.Dialog(100, 80);
					}
				}

				relations[otherIndex] &= 0xfffe;
				others[participantIndex] &= 0xfffe;
			}

			relations[otherIndex] &= 0xffef;
			others[participantIndex] &= 0xffef;
		}

		private void SendWarEmissary(short participantIndex, ushort otherIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			Player player = this.Owner.World.Participants[participantIndex];
			Player other = this.Owner.World.Participants[otherIndex];

			if ((player.Relations[otherIndex] & 0x20) == 0 ||
				(this.Owner.World.Participants[person].Relations[otherIndex] & 2) == 0 ||
				(player.Relations[person] & 0x20) != 0)
			{
				return;
			}

			if ((short)((ushort)player.MartialMight << 1) < other.MartialMight)
			{
				if (this.Owner.Audience.WelcomeEmissary(participantIndex, -1, 0) == 0)
				{
					short tribute = (short)((ushort)this.Owner.Helpers.ClampValue(player.Gold / 100, 1, 10) * 0x32);

					this.Owner.Codes.Set(1, Tables.CivilizationPlural(otherIndex));

					EmptyMessage()
						.Number(tribute);

					this.Owner.Codes.Set(2, this.Owner.Words.ToString());

					this.Owner.Words.Clear();

					if (AskKing(0x2988))
					{
						this.Owner.World.Participants[person].Gold += tribute;
						player.Gold += tribute;

						AllyAgainst(participantIndex, otherIndex);
					}

					this.Owner.Audience.FinishAudience();
				}
			}

			if (other.MartialMight <= player.MartialMight || (short)((ushort)player.MartialMight << 1) < other.MartialMight)
			{
				return;
			}

			if (this.Owner.Audience.WelcomeEmissary(participantIndex, -1, 0) != 0)
			{
				return;
			}

			this.Owner.Words.Clear();

			ushort enemyName = TurnEndRules.EmissaryEnemyName(otherIndex);
			this.Owner.Codes.Set(1, Tables.CivilizationPluralAt(enemyName));

			this.Owner.Codes.Set(3, (player.Relations[person] & 2) == 0 ? (ushort)0x299b : (ushort)0x2990);

			if (AskKing(0x29a6))
			{
				AllyAgainst(participantIndex, otherIndex);

				player.TouchParticipantTimeLeft = (short)(this.Owner.World.RoundTally + 16);
			}

			this.Owner.Audience.FinishAudience();
		}

		private bool AskKing(ushort charactersAddress)
		{
			this.Owner.BlockText.ShowTextDialog(charactersAddress);

			King.Mood = 0x1;
			King.Speaking = 0xc;

			return this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1;
		}

		private void AllyAgainst(short participantIndex, ushort otherIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			AssignDiplomacyFlags(participantIndex, person, 2);

			ClearDiplomacyFlags((short)otherIndex, person, 2);

			this.Owner.World.Participants[otherIndex].Relations[person] |= 8;
		}

		private bool RegimeKnown(short participantIndex, short regime)
		{
			short scienceIndex = regime switch { 2 => 5, 3 => 0x2b, 4 => 0x2c, 5 => 4, _ => -1 };

			return scienceIndex < 0 || this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0;
		}

		private void Bargain(short firstParticipantIndex, short secondParticipantIndex, int horizontalLocation, int verticalLocation)
		{
			Player firstParticipant = this.Owner.World.Participants[firstParticipantIndex];
			Player secondParticipant = this.Owner.World.Participants[secondParticipantIndex];
			short humanIndex = this.Owner.World.PersonParticipantIndex;

			if (firstParticipantIndex == humanIndex &&
				((firstParticipant.Relations[secondParticipantIndex] & 1) == 0 || (short)(this.Owner.World.RoundTally - secondParticipant.TouchParticipantTimeLeft) >= 0x10))
			{
				this.Owner.Audience.LaunchAudience(secondParticipantIndex, horizontalLocation, verticalLocation, 0);
			}

			if (secondParticipantIndex == humanIndex &&
				((firstParticipant.Relations[secondParticipantIndex] & 1) == 0 || (short)(this.Owner.World.RoundTally - firstParticipant.TouchParticipantTimeLeft) >= 0x10))
			{
				this.Owner.Audience.LaunchAudience(firstParticipantIndex, horizontalLocation, verticalLocation, 0);
			}

			firstParticipant.Relations[secondParticipantIndex] |= 0x11;
			secondParticipant.Relations[firstParticipantIndex] |= 0x11;

			if (firstParticipantIndex == humanIndex || secondParticipantIndex == humanIndex)
			{
				return;
			}

			if ((((byte)firstParticipantIndex + (byte)secondParticipantIndex + (byte)this.Owner.World.RoundTally) & 3) != 0 &&
				(firstParticipant.Relations[secondParticipantIndex] & 1) != 0)
			{
				return;
			}

			if ((firstParticipant.Relations[secondParticipantIndex] & 8) != 0 || (secondParticipant.Relations[firstParticipantIndex] & 8) != 0)
			{
				return;
			}

			bool war = WantsWar(firstParticipantIndex, secondParticipantIndex) || WantsWar(secondParticipantIndex, firstParticipantIndex);

			if (!war)
			{
				SwapTechnology(firstParticipantIndex, secondParticipantIndex);

				if ((firstParticipant.Relations[secondParticipantIndex] & 2) != 0)
				{
					return;
				}

				if ((this.Owner.World.Participants[humanIndex].Relations[firstParticipantIndex] & 0x40) != 0 ||
					(this.Owner.World.Participants[humanIndex].Relations[secondParticipantIndex] & 0x40) != 0)
				{
					AppendPair(firstParticipantIndex, secondParticipantIndex);

					this.Owner.Words.Append(" sign\na peace treaty.\n");

					Screen.DialogStyle = 0x1;

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
				}

				AssignDiplomacyFlags(firstParticipantIndex, secondParticipantIndex, 2);
			}
			else
			{
				if ((firstParticipant.Relations[secondParticipantIndex] & 2) == 0 && (firstParticipant.Relations[secondParticipantIndex] & 1) != 0)
				{
					return;
				}

				if ((this.Owner.World.Participants[humanIndex].Relations[firstParticipantIndex] & 0x40) != 0 ||
					(this.Owner.World.Participants[humanIndex].Relations[secondParticipantIndex] & 0x40) != 0)
				{
					AppendPair(firstParticipantIndex, secondParticipantIndex);

					this.Owner.Words.Append((firstParticipant.Relations[secondParticipantIndex] & 2) != 0 ? (ushort)0x2a36 : (ushort)0x2a4d);

					Screen.DialogStyle = 0x1;

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
				}

				ClearDiplomacyFlags(firstParticipantIndex, secondParticipantIndex, 2);
			}
		}

		private bool WantsWar(short participantIndex, short otherIndex)
		{
			if (ComputerWeighsTreaty(participantIndex, otherIndex) == 0)
			{
				return false;
			}

			return this.Owner.CityOperations.PlayerEnjoysWonderEffect(otherIndex, 7) == 0 && this.Owner.World.Participants[participantIndex].RegimeKind < 4;
		}

		private void AppendPair(short firstParticipantIndex, short secondParticipantIndex)
		{
			Message(Tables.CivilizationPlural(firstParticipantIndex))
				.Text(" and\n")
				.Text(Tables.CivilizationPlural(secondParticipantIndex));
		}

		private bool WeighTreaty(short firstParticipantIndex, short secondParticipantIndex)
		{
			if ((this.Owner.World.DiagonalMarkers & 1) == 0)
			{
				return true;
			}

			short humanIndex = this.Owner.World.PersonParticipantIndex;
			Player firstParticipant = this.Owner.World.Participants[firstParticipantIndex];
			Player secondParticipant = this.Owner.World.Participants[secondParticipantIndex];

			if ((firstParticipant.Relations[humanIndex] & 3) == 1 && secondParticipant.MartialMight < this.Owner.World.Participants[humanIndex].MartialMight &&
				(secondParticipant.Relations[humanIndex] & 4) == 0)
			{
				return false;
			}

			if ((secondParticipant.Relations[humanIndex] & 3) == 1 && firstParticipant.MartialMight < this.Owner.World.Participants[humanIndex].MartialMight &&
				(firstParticipant.Relations[humanIndex] & 4) == 0)
			{
				return false;
			}

			if (firstParticipant.LiveTroops[25] < secondParticipant.LiveTroops[25])
			{
				return false;
			}

			short treaties = 0;

			for (int otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex != firstParticipantIndex && otherIndex != secondParticipantIndex && (firstParticipant.Relations[otherIndex] & 3) == 1)
				{
					treaties++;
				}
			}

			if ((firstParticipant.Relations[secondParticipantIndex] & 2) != 0)
			{
				treaties++;
			}

			treaties = (short)(treaties - Tables.LeaderWord(firstParticipant.ChiefVisuals, LeaderTable.Aggression));

			short towns = 0;
			short defence = 0;
			short attack = 1;

			for (int continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				PlayerContinent firstContinent = firstParticipant.Landmasses[continentIndex];
				PlayerContinent secondContinent = secondParticipant.Landmasses[continentIndex];

				if (firstContinent.TownTally != 0 && (short)((firstContinent.Guard / 2) + firstContinent.Assault) < secondContinent.Guard)
				{
					return false;
				}

				if (firstContinent.Guard == 0 || secondContinent.Assault == 0)
				{
					continue;
				}

				defence += firstContinent.Guard;
				attack += (short)((secondContinent.Guard / 2) + secondContinent.Assault);
				towns += firstContinent.TownTally;
			}

			return CentralProcessor.SignedQuotient((short)(defence << 2), attack) >= (short)(treaties + 4) || towns == 0;
		}
	}
}
