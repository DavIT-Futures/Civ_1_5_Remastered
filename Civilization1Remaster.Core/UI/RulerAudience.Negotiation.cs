namespace Civilization1Remaster.Core.UI
{
	public partial class RulerAudience
	{
		private bool ExchangeTechnologies(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			while (true)
			{
				this.audienceOfferedTechnology = 0xffff;
				this.audienceWantedTechnology = 0xffff;
				short offeredValue = 0;
				short leaderTechnologies = 0;
				short personTechnologies = 0;

				for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0)
						personTechnologies++;

					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
						leaderTechnologies++;

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					{
						this.audienceWantedTechnology = (ushort)scienceIdx;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0 &&
						(short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) >= offeredValue)
					{
						this.audienceOfferedTechnology = (ushort)scienceIdx;
						offeredValue = (short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx);
					}
				}

				if (((Relation(participantIdx) & 0x20) != 0 || HostileValue != 0) && leaderTechnologies > 6)
					break;

				if (personTechnologies < leaderTechnologies ||
					this.audienceWantedTechnology == 0xffff || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				{
					break;
				}

				King.Mood = 1;
				this.Owner.Words.Clear();
				this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicExchange);
				King.Speaking = 0xc;

				if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
					break;

				this.Owner.Startup.HideMouseCursor();
				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
				this.Owner.Startup.ShowMouseCursor();

				King.Mood = 0xffff;
				Turn.CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(participantIdx, personIdx);
				RestoreScreenStyle();

				Turn.CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(personIdx, participantIdx);
				this.Owner.Helpers.HaltHueCycling();
				RestoreScreenStyle();

				this.audienceHostile = 0;
				King.Mood = 1;

				this.Owner.Startup.HideMouseCursor();
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
				this.Owner.Startup.ShowMouseCursor();
			}

			return !(HostileValue == 0 && occasion == 0 && (Relation(participantIdx) & 2) == 2 && (short)this.audienceDanger > 2);
		}

		private void AskForAlliance(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceCommonEnemy == 0xffff || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff || this.audienceDemand == 0)
				return;

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(1, Tables.CivilizationPlural(this.audienceCommonEnemy));
			this.Owner.Codes.Set(3, (ushort)(HostileValue != 0 ? 0x3780 : 0x3787));
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCancel);

			King.Mood = (ushort)(HostileValue != 0 ? 2 : 1);
			King.Speaking = 0xc;

			if (HostileValue != 0)
				this.Owner.Services.PerformSound(0x21, 0);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.audienceHostile = 0;

			if (this.Owner.Libc.Dice.DrawInt(2) != 0)
				this.audienceTribute = 0;

			short enemyIdx = (short)this.audienceCommonEnemy;
			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIdx, enemyIdx, 2);
			this.Owner.World.Participants[(ushort)enemyIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 8;
		}

		private void DemandTribute(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceTribute == 0 || this.audienceDemand != 2 || Person.Gold <= (short)this.audienceTribute ||
				WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
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
				Leader(participantIdx).Gold += tribute;
				this.audienceHostile = 0;
			}

			this.audienceTribute = 0x3e7;
		}

		private void DemandTechnology(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceDemand == 0 || this.audienceOfferedTechnology == 0xffff || this.audienceTribute == 0x3e7 ||
				WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
			{
				return;
			}

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(3, TechnologyName(this.audienceOfferedTechnology));
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicTakeciv);

			King.AudienceWelcomed = 0;
			King.Mood = 2;
			King.Speaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				this.Owner.Advisors.EarnTechnology(participantIdx, (short)this.audienceOfferedTechnology, this.Owner.World.PersonParticipantIdx);
				this.audienceHostile = 0;
			}

			this.audienceTribute = 0x3e7;
		}

		private void Threaten(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			ushort text;
			bool welcome;

			if (HostileValue != 0 && (Relation(participantIdx) & 2) != 0 && (short)this.audienceTribute > 0x32)
			{
				welcome = WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff;
				text = (ushort)(welcome ? 0x37a9 : 0x37b2);
			}
			else if (HostileValue != 0 && this.audienceTribute == 0x3e7)
			{
				if (WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
					return;

				welcome = true;
				text = 0x37bb;
			}
			else
			{
				if (HostileValue == 0 || ((Relation(participantIdx) & 1) != 0 && occasion == 0))
					return;

				welcome = WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff;
				text = (ushort)(welcome ? 0x37c3 : 0x37c8);
			}

			if (welcome)
				this.Owner.Services.PerformSound(0x21, 0);

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

			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIdx, participantIdx, 2);
		}

		private bool OfferTreaty(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			if (((Relation(participantIdx) & 2) != 0 && occasion == 0) || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				return true;

			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicPeace);
			King.AudienceWelcomed = 0;
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				ShowKingMessage(participantIdx);
				LogAudience(participantIdx);
				Leader(participantIdx).TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally + 16);
				return true;
			}

			if (Person.RegimeKind >= 4)
			{
				this.Owner.Words.Set("You are overruled by the Senate.\nTreaty signed.\n");
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				LogAudience(participantIdx);
				return true;
			}

			if ((Relation(participantIdx) & 2) != 0)
				this.Owner.Politics.ClearDiplomacyFlags(personIdx, participantIdx, 2);

			if ((short)this.audienceDanger >= 4 && this.audienceWantedTechnology != 0xffff)
			{
				if ((Relation(participantIdx) & 0xa) == 0)
					OfferTechnologyForPeace(participantIdx);
			}

			if ((Relation(participantIdx) & 0xa) == 0)
				OfferGoldForPeace(participantIdx);

			if (this.audienceDanger != 0 && Leader(participantIdx).TownTally <= 1)
			{
				if ((Relation(participantIdx) & 0xa) == 0)
					OfferEverythingForPeace(participantIdx);
			}

			return false;
		}

		private void OfferTechnologyForPeace(short participantIdx)
		{
			this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGiveciv);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			this.Owner.Helpers.HaltHueCycling();

			King.Mood = 0xffff;
			Turn.CivilopediaWithoutPictures = 1;
			this.Owner.Advisors.EarnTechnology(this.Owner.World.PersonParticipantIdx, (short)this.audienceWantedTechnology, participantIdx);
			RestoreScreenStyle();
			this.Owner.Helpers.HaltHueCycling();
			King.Mood = 0;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();

			ShowKingMessage(participantIdx);
			LogAudience(participantIdx);
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		private void OfferGoldForPeace(short participantIdx)
		{
			ushort amount = (ushort)((ushort)this.Owner.Helpers.ClampValue(((short)this.audienceDanger * 2) - 4, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount == 0)
				return;

			EmptyMessage()
				.Number((short)amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGivecash);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			ShowKingMessage(participantIdx);
			LogAudience(participantIdx);
			Person.Gold += (short)amount;

			ushort taken = (ushort)(participantIdx << 4);
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, taken, (ushort)(this.Processor.PullUInt16(this.Processor.SegData.Value16, taken) - amount));
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		private void OfferEverythingForPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			EmptyMessage()
				.Number(Leader(participantIdx).Gold);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGrovel);
			King.Mood = 0;
			King.Speaking = 0xc;

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			ShowKingMessage(participantIdx);
			LogAudience(participantIdx);

			Person.Gold += Leader(participantIdx).Gold;
			Leader(participantIdx).Gold = 0;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			this.Owner.Helpers.HaltHueCycling();
			King.Mood = 0xffff;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
				{
					Turn.CivilopediaWithoutPictures = 1;
					this.Owner.Advisors.EarnTechnology(personIdx, scienceIdx, participantIdx);
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

		private void DeclareWar(short participantIdx)
		{
			if ((Relation(participantIdx) & 2) != 0)
				return;

			this.Owner.Services.PerformSound(0x21, 0);
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicMobilize);
			King.AudienceWelcomed = 1;
			King.Mood = 2;
			King.Speaking = 4;
			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private void WelcomeFriendship(short participantIdx, int horizLoc, int vertLoc)
		{
			if (HostileValue != 0 || (Relation(participantIdx) & 2) != 2 || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				return;

			if (this.audienceDemand != 0 && this.audienceTribute != 0x3e7)
			{
				King.Mood = 1;
				King.Speaking = 4;
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicAttitude);
			}
			else
			{
				short personIdx = this.Owner.World.PersonParticipantIdx;

				King.Mood = 0;
				King.Speaking = 0xc;
				Message("We welcome the friendship of the\n")
					.Text(Tables.CivilizationName(personIdx))
					.Text(" people and their most\nwise and munificent leader:\n")
					.Text(Tables.GovernmentTitle(Person.RegimeKind))
					.Text(" ")
					.Text(Tables.LeaderName(personIdx))
					.Text(".\n");
			}

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private void NegotiateUnderPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			while (true)
			{
				this.Owner.Codes.Set(3, Tables.CivilizationPlural(participantIdx));
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicThreat);

				if ((short)(this.Owner.World.RoundTally - this.audienceLastAudience) < 0x10 || Person.RegimeKind >= 4)
					Menu.DisabledLines = 4;

				if (((1 << participantIdx) | (1 << (byte)personIdx)) == (ushort)this.Owner.World.LiveRealms)
					Menu.DisabledLinesLow = (byte)((byte)Menu.DisabledLines | 2);

				ushort choice = ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

				if (choice == 2)
					DemandGoldUnderPeace(participantIdx);

				if (choice != 1 || !AskToAttack(participantIdx))
					return;
			}
		}

		private void DemandGoldUnderPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			short amount = (short)((ushort)this.Owner.Helpers.ClampValue((short)this.audienceDanger, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount != 0)
			{
				EmptyMessage()
					.Number(amount);
				this.Owner.Codes.Set(2, this.Owner.Words.ToString());
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGivemore);
				King.Mood = 0;
				King.Speaking = 0xc;

				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				Person.Gold += amount;
				Leader(participantIdx).Gold -= amount;
				Leader(participantIdx).TouchParticipantTimeleft = this.Owner.World.RoundTally;
				return;
			}

			if (this.audienceWasHostile != 0)
			{
				this.Owner.Services.PerformSound(0x21, 0);
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicProvoke);
				this.Owner.Politics.ClearDiplomacyFlags(personIdx, participantIdx, 2);
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

		private bool AskToAttack(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Words.Set("You must attack the evil:\n ");
			short[] enemyTable = new short[8];
			int enemyCount = 0;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == personIdx || otherIdx == participantIdx || (this.Owner.World.LiveRealms & (1 << (byte)otherIdx)) == 0)
					continue;

				enemyTable[enemyCount++] = otherIdx;

				this.Owner.Words.Append(Tables.CivilizationPlural(otherIdx));
				this.Owner.Words.Append("\n ");
			}

			ushort choice = ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
			King.Mood = 1;

			if (choice == 0xffff)
				return false;

			short enemyIdx = enemyTable[choice];

			this.Owner.Codes.Set(3, Tables.CivilizationPlural(enemyIdx));
			King.Speaking = 4;

			if ((Leader(participantIdx).Relations[enemyIdx] & 1) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicNocontact);
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return false;
			}

			if ((Leader(participantIdx).Relations[enemyIdx] & 2) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicAtwar);
				ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return true;
			}

			short amount = (short)((ushort)this.Owner.Helpers.ClampValue(((Leader(enemyIdx).MartialMight / 2) * (Leader(participantIdx).Gold / 100)) / 50, 2, 9999) * 0x32);

			EmptyMessage()
				.Number(amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicMercenary);

			if (ShowKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return false;

			if (Person.Gold < amount)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicUnfortunate);
			}
			else
			{
				Leader(participantIdx).Gold += amount;
				Person.Gold -= amount;

				Message(Tables.CivilizationPlural(participantIdx))
					.Text(" declare war\non the ")
					.Text(Tables.CivilizationPlural(enemyIdx))
					.Text(".\n");
			}

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
			return false;
		}

		private void AdviseAboutLeader(short participantIdx)
		{
			if (King.Answer == 0xffff || Leader(participantIdx).TownTally <= 4 ||
				Person.Standing + 1 >= Leader(participantIdx).Standing ||
				(Person.Relations[participantIdx] & 2) != 0)
			{
				return;
			}

			this.Owner.Codes.Set(1, Tables.CivilizationPlural(participantIdx));
			this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicSchism);
		}

		private void AskToReceiveEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			if (horizLoc != -1)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, horizLoc - 8, vertLoc - 2);
			}

			while (true)
			{
				Message("An emissary from the\n")
					.Text(Tables.CivilizationPlural(participantIdx))
					.Text(" wishes to\nspeak with you.\n")
					.Text("Will you receive him?\n Yes.\n No.\n");

				if ((this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.Owner.Words.Append(" Check Intelligence\n");
				}

				this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.ShortTune), 3);

				this.Owner.Startup.ShowMouseCursorFully();

				King.Answer = (ushort)-this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Startup.HideMouseCursorToSavedLevel();

				if (King.Answer != 0xfffe)
					break;

				this.Owner.Briefings.ShowIntelligenceReport(participantIdx);
			}
		}

		private void ReceiveEmissary(short participantIdx)
		{
			this.Owner.Startup.HideMouseCursor();

			ShowKingScreen(participantIdx);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.Startup.ShowMouseCursor();

			ushort typeface = (ushort)((sbyte)this.Processor.PullUInt8(LeaderFontTableSegment, (ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals) + 8);
			King.SavedScreenStyle = typeface == 0xa ? (ushort)3 : typeface;

			DrawingBox.TypefaceIdx = King.SavedScreenStyle;
			King.AudienceInitialPrompt = 1;
			King.Mood = 1;
			King.AdvisorPoseShown = 0xffff;
			King.Pose = 0;
			King.Speaking = 0xc;

			ObtainKingSalutation(participantIdx);

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}
	}
}
