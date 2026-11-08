namespace Civ_1_5_Remastered.Core.UI.Meetings
{
	public partial class RulerAudience
	{
		private bool ExchangeTechnologies(short participantIndex, int horizontalLocation, int verticalLocation, ushort occasion)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			while (true)
			{
				this.audienceOfferedTechnology = 0xffff;
				this.audienceWantedTechnology = 0xffff;
				short offeredValue = 0;
				short leaderTechnologies = 0;
				short personTechnologies = 0;

				for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) != 0)
					{
						personTechnologies++;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
					{
						leaderTechnologies++;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) == 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
					{
						this.audienceWantedTechnology = (ushort)scienceIndex;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) != 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) == 0 &&
						(short)this.Owner.Advisors.ObtainTechnologyInformation(participantIndex, (ushort)scienceIndex) >= offeredValue)
					{
						this.audienceOfferedTechnology = (ushort)scienceIndex;
						offeredValue = (short)this.Owner.Advisors.ObtainTechnologyInformation(participantIndex, (ushort)scienceIndex);
					}
				}

				if (((Relation(participantIndex) & 0x20) != 0 || HostileValue != 0) && leaderTechnologies > 6)
				{
					break;
				}

				if (personTechnologies < leaderTechnologies ||
					this.audienceWantedTechnology == 0xffff || WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
				{
					break;
				}

				King.Mood = 1;
				this.Owner.Words.Clear();
				this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicExchange);
				King.Speaking = 0xc;

				if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				{
					break;
				}

				this.Owner.Startup.HideMouseCursor();
				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
				this.Owner.Startup.ShowMouseCursor();

				King.Mood = 0xffff;
				Turn.CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(participantIndex, personIndex);
				RestoreScreenStyle();

				Turn.CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(personIndex, participantIndex);
				this.Owner.Helpers.HaltHueCycling();
				RestoreScreenStyle();

				this.audienceHostile = 0;
				King.Mood = 1;

				this.Owner.Startup.HideMouseCursor();
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
				this.Owner.Startup.ShowMouseCursor();
			}

			return !(HostileValue == 0 && occasion == 0 && (Relation(participantIndex) & 2) == 2 && (short)this.audienceDanger > 2);
		}

		private void OfferReparations(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;
			ushort relation = Relation(participantIndex);

			if ((relation & DiplomacyRules.VendettaRelation) == 0 ||
				WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
			{
				return;
			}

			int grudge = DiplomacyRules.GrudgeUnits(relation);
			short gold = (short)DiplomacyRules.ComputeReparations(grudge, this.Owner.World.HardnessTier);
			short half = (short)(gold / 100 * 50);
			short technology = -1;
			short technologyValue = -1;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) == 0 ||
					this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
				{
					continue;
				}

				short value = (short)this.Owner.Advisors.ObtainTechnologyInformation(participantIndex, (ushort)scienceIndex);

				if (value >= technologyValue)
				{
					technology = scienceIndex;
					technologyValue = value;
				}
			}

			Message("Your treachery is not forgotten!\nOur VENDETTA lasts ")
				.Number(grudge * DiplomacyRules.TurnsPerUnit)
				.Text(" more turns.\nPay reparations and we forget.\n We owe you nothing.\n Pay ")
				.Number(gold)
				.Text(" gold.\n");

			ushort disabled = 0;

			if (Person.Gold < gold)
			{
				disabled |= 2;
			}

			if (technology >= 0)
			{
				this.Owner.Words.Append(" Give ");
				this.Owner.Words.Append(TechnologyName((ushort)technology));
				EmptyMessageAppend(" and ", half, " gold.\n");

				if (Person.Gold < half)
				{
					disabled |= 4;
				}
			}

			Menu.DisabledLines = disabled;
			King.AudienceWelcomed = 0;
			King.Mood = 2;
			King.Speaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			ushort choice = ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

			if (choice == 1 && Person.Gold >= gold)
			{
				Person.Gold -= gold;
				Leader(participantIndex).Gold += gold;
			}
			else if (choice == 2 && technology >= 0 && Person.Gold >= half)
			{
				Person.Gold -= half;
				Leader(participantIndex).Gold += half;
				this.Owner.Advisors.EarnTechnology(participantIndex, technology, personIndex);
			}
			else
			{
				return;
			}

			DiplomacyRules.EndVendetta(this.Owner.World.Participants, participantIndex, personIndex);
			Leader(participantIndex).Relations[personIndex] &= 0xfcff;
			this.audienceAppeased = 1;

			King.Mood = 1;
			King.Speaking = 4;
			this.Owner.Words.Set("Very well. The past is buried.\nLet there be trust between us.\n");
			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private void EmptyMessageAppend(string before, short amount, string after)
		{
			this.Owner.Words.Append(before);
			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(amount, 10));
			this.Owner.Words.Append(after);
		}

		private void AskForAlliance(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (this.audienceCommonEnemy == 0xffff || WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff || this.audienceDemand == 0)
			{
				return;
			}

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(1, Tables.CivilizationPlural(this.audienceCommonEnemy));
			this.Owner.Codes.Set(3, (ushort)(HostileValue != 0 ? 0x3780 : 0x3787));
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCancel);

			King.Mood = (ushort)(HostileValue != 0 ? 2 : 1);
			King.Speaking = 0xc;

			if (HostileValue != 0)
			{
				this.Owner.Services.PerformSound(0x21, 0);
			}

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
			{
				return;
			}

			this.audienceHostile = 0;

			if (this.Owner.Runtime.Dice.DrawInteger(2) != 0)
			{
				this.audienceTribute = 0;
			}

			short enemyIndex = (short)this.audienceCommonEnemy;
			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIndex, enemyIndex, 2);
			DiplomacyRules.BeginVendetta(this.Owner.World.Participants, enemyIndex, this.Owner.World.PersonParticipantIndex, DiplomacyRules.AllianceTargetGrudge);
		}

		private void DemandTribute(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (this.audienceTribute == 0 || this.audienceDemand != 2 || Person.Gold <= (short)this.audienceTribute ||
				WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
			{
				return;
			}

			EmptyMessage()
				.Number((short)this.audienceTribute);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicTribute);

			King.AudienceWelcomed = 0;
			King.Mood = 2;
			King.Speaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				short tribute = (short)this.audienceTribute;
				Person.Gold -= tribute;
				Leader(participantIndex).Gold += tribute;
				this.audienceHostile = 0;
				this.audienceAppeased = 1;
				DiplomacyRules.SoftenVendetta(this.Owner.World.Participants, participantIndex, this.Owner.World.PersonParticipantIndex);
			}

			this.audienceTribute = 0x3e7;
		}

		private void DemandTechnology(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (this.audienceDemand == 0 || this.audienceOfferedTechnology == 0xffff || this.audienceTribute == 0x3e7 ||
				WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
			{
				return;
			}

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(3, TechnologyName(this.audienceOfferedTechnology));
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicTakeCivilization);

			King.AudienceWelcomed = 0;
			King.Mood = 2;
			King.Speaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				this.Owner.Advisors.EarnTechnology(participantIndex, (short)this.audienceOfferedTechnology, this.Owner.World.PersonParticipantIndex);
				this.audienceHostile = 0;
				this.audienceAppeased = 1;
				DiplomacyRules.SoftenVendetta(this.Owner.World.Participants, participantIndex, this.Owner.World.PersonParticipantIndex);
			}

			this.audienceTribute = 0x3e7;
		}

		private void Threaten(short participantIndex, int horizontalLocation, int verticalLocation, ushort occasion)
		{
			ushort text;
			bool welcome;

			if (HostileValue != 0 && (Relation(participantIndex) & 2) != 0 && (short)this.audienceTribute > 0x32)
			{
				welcome = WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) != 0xffff;
				text = (ushort)(welcome ? 0x37a9 : 0x37b2);
			}
			else if (HostileValue != 0 && this.audienceTribute == 0x3e7)
			{
				if (WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
				{
					return;
				}

				welcome = true;
				text = 0x37bb;
			}
			else
			{
				if (HostileValue == 0 || ((Relation(participantIndex) & 1) != 0 && occasion == 0))
				{
					return;
				}

				welcome = WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) != 0xffff;
				text = (ushort)(welcome ? 0x37c3 : 0x37c8);
			}

			if (welcome)
			{
				this.Owner.Services.PerformSound(0x21, 0);
			}

			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(text);
			this.Owner.BlockText.ArrangeTextRow(0x28);

			if (welcome)
			{
				King.AudienceWelcomed = 1;
				King.Mood = 2;
				King.Speaking = 0xc;
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
			}
			else
			{
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 40, 100);
			}

			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIndex, participantIndex, 2);
		}

		private bool OfferTreaty(short participantIndex, int horizontalLocation, int verticalLocation, ushort occasion)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			if (((Relation(participantIndex) & 2) != 0 && occasion == 0) || WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
			{
				return true;
			}

			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicPeace);
			King.AudienceWelcomed = 0;
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				ShowKingMessage(participantIndex);
				LogAudience(participantIndex);
				Leader(participantIndex).TouchParticipantTimeLeft = (short)(this.Owner.World.RoundTally + 16);
				return true;
			}

			bool embassy = (Person.Relations[participantIndex] & DiplomacyRules.EmbassyRelation) != 0;

			if (Person.RegimeKind >= 4 && !DiplomacyRules.SenateConsents(Relation(participantIndex), embassy))
			{
				this.Owner.Words.Set("You are overruled by the Senate.\nTreaty signed.\n");
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				LogAudience(participantIndex);
				return true;
			}

			if ((Relation(participantIndex) & 2) != 0)
			{
				this.Owner.Politics.ClearDiplomacyFlags(personIndex, participantIndex, 2);
			}

			if ((short)this.audienceDanger >= 4 && this.audienceWantedTechnology != 0xffff)
			{
				if ((Relation(participantIndex) & 0xa) == 0)
				{
					OfferTechnologyForPeace(participantIndex);
				}
			}

			if ((Relation(participantIndex) & 0xa) == 0)
			{
				OfferGoldForPeace(participantIndex);
			}

			if (this.audienceDanger != 0 && Leader(participantIndex).TownTally <= 1)
			{
				if ((Relation(participantIndex) & 0xa) == 0)
				{
					OfferEverythingForPeace(participantIndex);
				}
			}

			return false;
		}

		private void OfferTechnologyForPeace(short participantIndex)
		{
			this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGiveCivilization);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
			{
				return;
			}

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			this.Owner.Helpers.HaltHueCycling();

			King.Mood = 0xffff;
			Turn.CivilopediaWithoutPictures = 1;
			this.Owner.Advisors.EarnTechnology(this.Owner.World.PersonParticipantIndex, (short)this.audienceWantedTechnology, participantIndex);
			RestoreScreenStyle();
			this.Owner.Helpers.HaltHueCycling();
			King.Mood = 0;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();

			ShowKingMessage(participantIndex);
			LogAudience(participantIndex);
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		private void OfferGoldForPeace(short participantIndex)
		{
			ushort amount = (ushort)((ushort)this.Owner.Helpers.ClampValue(((short)this.audienceDanger * 2) - 4, 0, Leader(participantIndex).Gold / 50) * 0x32);

			if (amount == 0)
			{
				return;
			}

			EmptyMessage()
				.Number((short)amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGiveCash);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
			{
				return;
			}

			ShowKingMessage(participantIndex);
			LogAudience(participantIndex);
			Person.Gold += (short)amount;

			ushort taken = (ushort)(participantIndex << 4);
			this.Processor.EmitWord(this.Processor.SegmentData.Word, taken, (ushort)(this.Processor.PullWord(this.Processor.SegmentData.Word, taken) - amount));
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		private void OfferEverythingForPeace(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			EmptyMessage()
				.Number(Leader(participantIndex).Gold);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGrovel);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
			{
				return;
			}

			ShowKingMessage(participantIndex);
			LogAudience(participantIndex);

			Person.Gold += Leader(participantIndex).Gold;
			Leader(participantIndex).Gold = 0;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			this.Owner.Helpers.HaltHueCycling();
			King.Mood = 0xffff;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
				{
					Turn.CivilopediaWithoutPictures = 1;
					this.Owner.Advisors.EarnTechnology(personIndex, scienceIndex, participantIndex);
				}
			}

			RestoreScreenStyle();
			this.Owner.Helpers.HaltHueCycling();
			King.Mood = 0;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();

			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		private void DeclareWar(short participantIndex)
		{
			if ((Relation(participantIndex) & 2) != 0)
			{
				return;
			}

			this.Owner.Services.PerformSound(0x21, 0);
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicMobilize);
			King.AudienceWelcomed = 1;
			King.Mood = 2;
			King.Speaking = 4;
			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private void WelcomeFriendship(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (HostileValue != 0 || (Relation(participantIndex) & 2) != 2 || WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) == 0xffff)
			{
				return;
			}

			if (this.audienceDemand != 0 && this.audienceTribute != 0x3e7)
			{
				King.Mood = 1;
				King.Speaking = 4;
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicAttitude);
			}
			else
			{
				short personIndex = this.Owner.World.PersonParticipantIndex;

				King.Mood = 0;
				King.Speaking = 0xc;
				Message("We welcome the friendship of the\n")
					.Text(Tables.CivilizationName(personIndex))
					.Text(" people and their most\nwise and munificent leader:\n")
					.Text(Tables.GovernmentTitle(Person.RegimeKind))
					.Text(" ")
					.Text(Tables.LeaderName(personIndex))
					.Text(".\n");
			}

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private void NegotiateUnderPeace(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			while (true)
			{
				this.Owner.Codes.Set(3, Tables.CivilizationPlural(participantIndex));
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicThreat);

				if ((short)(this.Owner.World.RoundTally - this.audienceLastAudience) < 0x10 || Person.RegimeKind >= 4)
				{
					Menu.DisabledLines = 4;
				}

				if (((1 << participantIndex) | (1 << (byte)personIndex)) == (ushort)this.Owner.World.LiveRealms)
				{
					Menu.DisabledLinesLow = (byte)((byte)Menu.DisabledLines | 2);
				}

				ushort choice = ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

				if (choice == 2)
				{
					DemandGoldUnderPeace(participantIndex);
				}

				if (choice != 1 || !AskToAttack(participantIndex))
				{
					return;
				}
			}
		}

		private void DemandGoldUnderPeace(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			short amount = (short)((ushort)this.Owner.Helpers.ClampValue((short)this.audienceDanger, 0, Leader(participantIndex).Gold / 50) * 0x32);

			if (amount != 0)
			{
				EmptyMessage()
					.Number(amount);
				this.Owner.Codes.Set(2, this.Owner.Words.ToString());
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGiveMore);
				King.Mood = 0;
				King.Speaking = 0xc;

				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				Person.Gold += amount;
				Leader(participantIndex).Gold -= amount;
				Leader(participantIndex).TouchParticipantTimeLeft = this.Owner.World.RoundTally;
				return;
			}

			if (this.audienceWasHostile != 0)
			{
				this.Owner.Services.PerformSound(0x21, 0);
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicProvoke);
				this.Owner.Politics.ClearDiplomacyFlags(personIndex, participantIndex, 2);
				King.Mood = 2;
				King.Speaking = 0xc;
			}
			else
			{
				this.Owner.Words.Set("We ignore your feeble threats.\n");
				King.Mood = 1;
				King.Speaking = 4;
			}

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private bool AskToAttack(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			this.Owner.Words.Set("You must attack the evil:\n ");
			short[] enemyTable = new short[8];
			int enemyCount = 0;

			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex == personIndex || otherIndex == participantIndex || (this.Owner.World.LiveRealms & (1 << (byte)otherIndex)) == 0)
				{
					continue;
				}

				enemyTable[enemyCount++] = otherIndex;

				this.Owner.Words.Append(Tables.CivilizationPlural(otherIndex));
				this.Owner.Words.Append("\n ");
			}

			ushort choice = ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
			King.Mood = 1;

			if (choice == 0xffff)
			{
				return false;
			}

			short enemyIndex = enemyTable[choice];

			this.Owner.Codes.Set(3, Tables.CivilizationPlural(enemyIndex));
			King.Speaking = 4;

			if ((Leader(participantIndex).Relations[enemyIndex] & 1) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicNoContact);
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return false;
			}

			if ((Leader(participantIndex).Relations[enemyIndex] & 2) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicAtWar);
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return true;
			}

			short amount = (short)((ushort)this.Owner.Helpers.ClampValue(((Leader(enemyIndex).MartialMight / 2) * (Leader(participantIndex).Gold / 100)) / 50, 2, 9999) * 0x32);

			EmptyMessage()
				.Number(amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicMercenary);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
			{
				return false;
			}

			if (Person.Gold < amount)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicUnfortunate);
			}
			else
			{
				Leader(participantIndex).Gold += amount;
				Person.Gold -= amount;

				Message(Tables.CivilizationPlural(participantIndex))
					.Text(" declare war\non the ")
					.Text(Tables.CivilizationPlural(enemyIndex))
					.Text(".\n");
			}

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
			return false;
		}

		private void AdviseAboutLeader(short participantIndex)
		{
			if (King.Answer == 0xffff || Leader(participantIndex).TownTally <= 4 ||
				Person.Standing + 1 >= Leader(participantIndex).Standing ||
				(Person.Relations[participantIndex] & 2) != 0)
			{
				return;
			}

			this.Owner.Codes.Set(1, Tables.CivilizationPlural(participantIndex));
			this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicSchism);
		}

		private void AskToReceiveEmissary(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (horizontalLocation != -1)
			{
				this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex, horizontalLocation - Viewport.MiddleColumn - 1, verticalLocation - 2);
			}

			while (true)
			{
				Message("An emissary from the\n")
					.Text(Tables.CivilizationPlural(participantIndex))
					.Text(" wishes to\nspeak with you.\n")
					.Text("Will you receive him?\n Yes.\n No.\n");

				if ((this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations[participantIndex] & 0x40) != 0)
				{
					this.Owner.Words.Append(" Check Intelligence\n");
				}

				this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.ShortTune), 3);

				this.Owner.Startup.ShowMouseCursorFully();

				King.Answer = (ushort)-this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Startup.HideMouseCursorToSavedLevel();

				if (King.Answer != 0xfffe)
				{
					break;
				}

				this.Owner.Briefings.ShowIntelligenceReport(participantIndex);
			}
		}

		private void ReceiveEmissary(short participantIndex)
		{
			this.Owner.Startup.HideMouseCursor();

			ShowKingScreen(participantIndex);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.Startup.ShowMouseCursor();

			ushort typeface = (ushort)((sbyte)this.Processor.PullByte(LeaderFontTableSegment, (ushort)this.Owner.World.Participants[participantIndex].ChiefVisuals) + 8);
			King.SavedScreenStyle = typeface == 0xa ? (ushort)3 : typeface;

			DrawingBox.TypefaceIndex = King.SavedScreenStyle;
			King.AudienceInitialPrompt = 1;
			King.Mood = 1;
			King.AdvisorPoseShown = 0xffff;
			King.Pose = 0;
			King.Speaking = 0xc;

			ObtainKingSalutation(participantIndex);

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}
	}
}
