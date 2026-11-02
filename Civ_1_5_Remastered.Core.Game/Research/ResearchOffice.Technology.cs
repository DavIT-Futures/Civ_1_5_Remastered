namespace Civ_1_5_Remastered.Core.Game.Research
{
	public partial class ResearchOffice
	{
		public ushort UncoverTechnology(short participantIndex, short firstParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"UncoverTechnology({participantIndex}, {firstParticipantIndex})");

			ushort result = participantIndex == 0 ? (ushort)0xffff : UncoverTechnologyOf(participantIndex, firstParticipantIndex);

			return result;
		}

		public void EarnTechnology(short participantIndex, short scienceIndex, short firstParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EarnTechnology({participantIndex}, {scienceIndex}, {firstParticipantIndex})");

			if (PlayerKnowsTechnology(participantIndex, scienceIndex) == 0)
			{
				if (scienceIndex != 0x47)
				{
					this.Owner.World.Participants[participantIndex].FoundScienceMarkers[scienceIndex >> 4] |= (ushort)(1 << (scienceIndex & 0xf));
					this.Owner.World.Participants[participantIndex].ScienceObtainedSince[scienceIndex] = firstParticipantIndex;
				}

				if (this.Owner.World.RoundTally != 0)
				{
					this.Owner.World.Participants[participantIndex].FoundScienceTally++;

					AnnounceTechnology(participantIndex, scienceIndex, firstParticipantIndex);

					ApplyTechnology(participantIndex, scienceIndex);
				}
			}
		}

		public ushort PlayerKnowsTechnology(short participantIndex, short scienceIndex)
		{
			return (ushort)(ScienceRules.PlayerKnowsTechnology(this.Owner.World.Participants[participantIndex].FoundScienceMarkers, participantIndex, scienceIndex) ? 1 : 0);
		}

		public ushort ObtainTechnologyInformation(short participantIndex, ushort scienceIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTechnologyInfo({participantIndex}, {scienceIndex})");

			sbyte baseAmount = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((scienceIndex * 2) + 0xb0a));
			sbyte leaderWeight = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((scienceIndex * 2) + 0xb0b));
			int leaderVisuals = this.Owner.World.Participants[participantIndex].ChiefVisuals;
			short leaderFactor = (short)Tables.LeaderWord(leaderVisuals, LeaderTable.Leaning);
			ushort result = ScienceRules.CalculateTechnologyInformation(baseAmount, leaderWeight, leaderFactor);

			return result;
		}

		private ushort UncoverTechnologyOf(short participantIndex, short firstParticipantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			Player human = this.Owner.World.Participants[person];

			short pick = -1;
			short highest = -1;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (!MayResearch(participantIndex, scienceIndex))
				{
					continue;
				}

				short draw = (short)this.Owner.Runtime.Dice.DrawInteger((ushort)(ObtainTechnologyInformation(participantIndex, (ushort)scienceIndex) << 2));

				if (draw <= highest || (scienceIndex == human.PresentInquiryIndex && participantIndex == person))
				{
					continue;
				}

				highest = draw;
				pick = scienceIndex;
			}

			if (participantIndex == person && this.Owner.World.RoundTally != 0)
			{
				return ChooseNextResearch(participantIndex, firstParticipantIndex, pick);
			}

			if (this.Owner.World.RoundTally != 0 && this.Owner.World.HardnessTier == 0 &&
				PlayerKnowsTechnology(person, pick) == 0 && this.Owner.Runtime.Dice.DrawInteger(2) != 0)
			{
				return 0xffff;
			}

			if (highest != -1)
			{
				EarnTechnology(participantIndex, pick, firstParticipantIndex);

				this.Owner.Computer.RefreshCityProduction(participantIndex, 0xffff);
			}

			return (ushort)pick;
		}

		private bool MayResearch(short participantIndex, short scienceIndex)
		{
			if (PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
			{
				return false;
			}

			ushort science = (ushort)(0x16 * scienceIndex);

			return PlayerKnowsTechnology(participantIndex, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.PrerequisiteFirstAddress))) != 0 &&
				PlayerKnowsTechnology(participantIndex, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.PrerequisiteSecondAddress))) != 0;
		}

		private ushort ChooseNextResearch(short participantIndex, short firstParticipantIndex, short pick)
		{
			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];
			ushort[] choiceList = new ushort[0x48];
			bool again = false;

			while (true)
			{
				if (human.PresentInquiryIndex != -1)
				{
					AnnounceDiscovery(participantIndex, firstParticipantIndex);
				}

				if (this.Owner.World.Participants[participantIndex].FoundScienceTally <= 1)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstCivilization);
				}

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if ((this.Owner.World.SessionPreferenceMarkers & 1) != 0 && pick != -1 && this.Owner.World.HardnessTier == 0)
				{
					RecommendResearch(pick);
				}

				ushort choices = ListResearchChoices(participantIndex, again, choiceList);

				Screen.DialogStyle = 0x6;

				if (choices == 0)
				{
					this.Owner.Words.Append(" Future Technology ");

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(human.ComingScienceTally + 1), 10));

					this.Owner.Words.Append("\n");

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

					human.PresentInquiryIndex = 71;

					return 0x48;
				}

				Menu.Footer = 0x1;

				ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 80, 64, 1);

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if (choice == 0xffff)
				{
					choice = 0;
				}

				ushort scienceIndex = choiceList[choice];

				if (Menu.HelpTopic == 0)
				{
					human.PresentInquiryIndex = (short)scienceIndex;

					return scienceIndex;
				}

				this.Owner.Encyclopedia.ShowCivilopediaRecord(scienceIndex, 0);

				this.Owner.TurnHooks.RepaintMainScreen();

				again = true;
			}
		}

		private void AnnounceDiscovery(short participantIndex, short firstParticipantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			Player human = this.Owner.World.Participants[person];

			short first = (short)this.Owner.Encyclopedia.ObtainFirstPrerequisite((ushort)human.PresentInquiryIndex);
			bool picture = human.PresentInquiryIndex != 71 && (short)Files.FirstMissingDisk > 3 &&
				this.Owner.Startup.AskForDisk(1, 0) != 0 && (this.Owner.World.SessionPreferenceMarkers & 8) != 0;

			bool advanced = PlayerKnowsTechnology(person, 0x36) != 0;
			string pictureName = advanced ? "discovr2.pic" : "discovr1.pic";

			if (picture)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, pictureName, 0);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, pictureName, 1);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}

			Message(Tables.CivilizationName((ushort)participantIndex))
				.Text(PlayerKnowsTechnology(person, 0x14) != 0 ? (ushort)0x22cc : (ushort)0x22d8)
				.Text("\ndiscover the secret\nof ")
				.Text(GameTables.TechnologyText(human.PresentInquiryIndex));

			if (human.PresentInquiryIndex == 71)
			{
				this.Owner.Words.Append(" ");

				human.ComingScienceTally++;

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(human.ComingScienceTally, 10));
			}

			this.Owner.Words.Append("!\n");

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.Tune), 3);

			if (picture)
			{
				ShowDiscoveryPicture(first, advanced);
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(-1);
			}

			EarnTechnology(participantIndex, human.PresentInquiryIndex, firstParticipantIndex);

			this.Owner.Services.PerformSound(1, 0);

			human.PresentInquiryIndex = -1;
		}

		private void ShowDiscoveryPicture(short first, bool advanced)
		{
			if (!advanced)
			{
				DrawingBox.TypefaceIndex = 0x6;
			}

			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 96, advanced ? (short)25 : (short)1, 1);

			DrawingBox.TypefaceIndex = 0x1;

			this.Owner.Words.Set("iconpg1.pic");

			this.Owner.Words[6] = (byte)(this.Owner.Words[6] + CentralProcessor.SignedQuotient(first, 9));

			this.Owner.Startup.AskForDisk(3, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.Encyclopedia.DrawIcon((ushort)(((first % 3) * 111) + 1), (ushort)((((first % 9) / 3) * 69) + 1), 110, 68, 174, advanced ? (ushort)87 : (ushort)96);

			if (Screen.VideoGraphicsMode != 0)
			{
				string paletteName = advanced ? "discovr2.pal" : "discovr1.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, paletteName, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileIconPagePalette, 0xc5be);

				this.Owner.Runtime.CopyBytes(0xc5c4, 0xc1dc, 0xff);

				this.Owner.Services.LoadColorsFromBuffer(30, 0xc5be);
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();
		}

		private void RecommendResearch(short pick)
		{
			Message("Science Advisor:\nI recommend we\ndevelop ")
				.Text(GameTables.TechnologyText(pick))
				.Text(".\n");

			ushort length = this.Owner.Words.Length;

			ushort allowed = 0;

			this.Owner.Words.Append("So we can build:\n-");

			for (ushort kindIndex = 0; kindIndex < 0x1c; kindIndex++)
			{
				ushort kindRecord = (ushort)(0x22 * kindIndex);

				if (Tables.UnitKindWordAt(kindRecord, UnitKindTable.Technology) != (ushort)pick)
				{
					continue;
				}

				this.Owner.Words.Append(GameTables.UnitTextAt(kindRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			for (ushort wonderIndex = 0; wonderIndex < 0x18; wonderIndex++)
			{
				ushort wonderRecord = (ushort)(0x1e * wonderIndex);

				if (Tables.BuildingWordAt(wonderRecord, BuildingTable.Technology) != (ushort)pick)
				{
					continue;
				}

				this.Owner.Words.Append(GameTables.BuildingRecordTextAt(wonderRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			if (allowed == 0)
			{
				this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0);
			}

			DrawingBox.TypefaceIndex = 0x2;
			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 8, (short)-((allowed << 3) - 0xa8), 1);

			DrawingBox.TypefaceIndex = 0x1;
		}

		private ushort ListResearchChoices(short participantIndex, bool again, ushort[] choiceList)
		{
			Message("Which discovery should our\n")
				.Text(PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 0x14) != 0 ? (ushort)0x2388 : (ushort)0x2393)
				.Text(" be pursuing, sire?\nPick one...\n ");

			ushort choices = 0;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (!MayResearch(participantIndex, scienceIndex))
				{
					continue;
				}

				if (!again)
				{
					if ((short)choices > 1 && (short)(scienceIndex - this.Owner.World.Participants[participantIndex].FoundScienceTally) % 3 == 0 &&
						this.Owner.World.HardnessTier != 0)
					{
						continue;
					}
				}
				else
				{
					if (choiceList[choices] != (ushort)scienceIndex)
					{
						continue;
					}
				}

				this.Owner.Words.Append(GameTables.TechnologyText(scienceIndex));

				this.Owner.Words.Append("\n ");

				choiceList[choices] = (ushort)scienceIndex;
				choices++;
			}

			return choices;
		}

		private void AnnounceTechnology(short participantIndex, short scienceIndex, short firstParticipantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			if (participantIndex != person)
			{
				if ((this.Owner.World.Participants[person].Relations[participantIndex] & 0x40) == 0)
				{
					SendTechnologyEmissary(participantIndex, scienceIndex);
					return;
				}
			}

			EmptyMessage()
				.Text(Tables.CivilizationPlural((ushort)participantIndex))
				.Text(participantIndex == firstParticipantIndex || firstParticipantIndex == 0 ? (ushort)0x23d7 : (ushort)0x23e1)
				.Text(GameTables.TechnologyText(scienceIndex));

			if (TechnologyNewsRules.NamesSource(participantIndex, firstParticipantIndex))
			{
				this.Owner.Words.Append(" from\nthe ");

				if (firstParticipantIndex == TechnologyNewsRules.GreatLibrary)
				{
					this.Owner.Words.Append("Great Library");
				}
				else
				{
					this.Owner.Words.Append(Tables.CivilizationPlural((ushort)firstParticipantIndex));
				}
			}

			this.Owner.Words.Append("!\n ");

			if (participantIndex != person)
			{
				for (ushort wonderIndex = 0; wonderIndex < 0x18; wonderIndex++)
				{
					ushort wonderRecord = (ushort)(0x1e * wonderIndex);

					if (Tables.BuildingWordAt(wonderRecord, BuildingTable.Technology) == (ushort)scienceIndex)
					{
						this.Owner.Words.Append(GameTables.BuildingRecordTextAt(wonderRecord));

						this.Owner.Words.Append("\n ");
					}
				}

				for (ushort kindIndex = 0; kindIndex < 0x1c; kindIndex++)
				{
					ushort kindRecord = (ushort)(0x22 * kindIndex);

					if (Tables.UnitKindWordAt(kindRecord, UnitKindTable.Technology) == (ushort)scienceIndex)
					{
						this.Owner.Words.Append(GameTables.UnitTextAt(kindRecord));

						this.Owner.Words.Append("\n ");
					}
				}
			}

			if ((participantIndex != person || firstParticipantIndex == -2) && King.Answer == 0xffff)
			{
				Menu.AnyKey = 1;

				Screen.DialogStyle = participantIndex == person ? (ushort)6 : (ushort)0;

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		private void SendTechnologyEmissary(short participantIndex, short scienceIndex)
		{
			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIndex] & 7) != 0 ||
				PlayerKnowsTechnology(participantIndex, 8) == 0 ||
				PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, scienceIndex) != 0 ||
				this.Owner.Runtime.Dice.DrawInteger(3) != 0)
			{
				return;
			}

			if (this.Owner.Audience.WelcomeEmissary(participantIndex, -1, 0) == 0xffff)
			{
				return;
			}

			King.Answer = 0;
			King.EmissaryAudience = 0;
			this.Owner.Words.Clear();

			this.Owner.Codes.Set(0, Tables.CivilizationPlural((ushort)participantIndex));

			this.Owner.Codes.Set(3, GameTables.TechnologyText(scienceIndex));

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGape);

			King.Mood = 0x2;
			King.Speaking = 0xc;

			this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

			this.Owner.Audience.FinishAudience();
		}

		private void ApplyTechnology(short participantIndex, short scienceIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			if (participantIndex == person)
			{
				if (scienceIndex != 0x47)
				{
					this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)scienceIndex, 0);
				}

				if (scienceIndex == 0x14)
				{
					this.Owner.NewGameWizard.LoadGovernmentGraphics();

					this.Owner.TurnHooks.RepaintMainScreen();
				}

				if (King.Answer == 0xffff)
				{
					this.Owner.TurnHooks.RepaintMainScreen();

					if (scienceIndex == 0x5)
					{
						this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicMonarchy);
					}
				}
			}
			else if (this.Owner.World.HardnessTier == 0 && scienceIndex == 0x16)
			{
			}

			if (ScienceRules.DoesTechnologyObsoleteBarracks(scienceIndex))
			{
				for (int townIndex = 0; townIndex < 128; townIndex++)
				{
					if (this.Owner.World.Towns[townIndex].ConditionMarker != 0xff &&
						this.Owner.World.Towns[townIndex].ParticipantIndex == participantIndex)
					{
						this.Owner.World.Towns[townIndex].StructureMarkersLow &= 0xfffd;
					}
				}

				if (participantIndex == person && King.Answer == 0xffff)
				{
					Message("Development of ")
						.Text(GameTables.TechnologyText(scienceIndex))
						.Text("\nmakes existing Barracks\nobsolete.\n")
						.Style(0x3)
						.Dialog(80, 100);
				}
			}

			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIndex] & 7) == 0)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIndex] = participantIndex;

				this.Owner.UnitOperations.AttachReplayContent(5, (byte)participantIndex, (byte)scienceIndex);
			}

			ushort libraryOwner = (ushort)this.Owner.World.MarvelTownIndex[5];

			if (libraryOwner == 0xffff || !WonderCityRules.ReadsCity(libraryOwner))
			{
				return;
			}

			libraryOwner = (ushort)this.Owner.World.Towns[libraryOwner].ParticipantIndex;

			if (PlayerKnowsTechnology((short)libraryOwner, scienceIndex) != 0)
			{
				return;
			}

			if (this.Owner.CityOperations.PlayerEnjoysWonderEffect((short)libraryOwner, 5) == 0)
			{
				return;
			}

			int knowing = 0;

			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (PlayerKnowsTechnology(otherIndex, scienceIndex) != 0)
				{
					knowing++;
				}
			}

			if (knowing < 2)
			{
				return;
			}

			EarnTechnology((short)libraryOwner, scienceIndex, -2);
		}
	}
}
