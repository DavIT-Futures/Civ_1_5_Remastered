
namespace Civilization1Remaster.Core.Game
{
	/// <summary>The technologies: their discovery, the next research, the news and what a new technology changes</summary>
	public partial class ResearchOffice
	{
		/// <inheritdoc/>
		public ushort UncoverTechnology(short participantIdx, short participantIdx1)
		{
			using var _ = this.Processor.Journal.Scope($"UncoverTechnology({participantIdx}, {participantIdx1})");


			ushort result = participantIdx == 0 ? (ushort)0xffff : UncoverTechnologyOf(participantIdx, participantIdx1);


			return result;
		}

		/// <inheritdoc/>
		public void EarnTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			using var _ = this.Processor.Journal.Scope($"EarnTechnology({participantIdx}, {scienceIdx}, {participantIdx1})");


			if (PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
			{
				if (scienceIdx != 0x47)
				{
					this.Owner.World.Participants[participantIdx].FoundScienceMarkers[scienceIdx >> 4] |= (ushort)(1 << (scienceIdx & 0xf));
					this.Owner.World.Participants[participantIdx].ScienceObtainedSince[scienceIdx] = participantIdx1;
				}

				if (this.Owner.World.RoundTally != 0)
				{
					this.Owner.World.Participants[participantIdx].FoundScienceTally++;

					AnnounceTechnology(participantIdx, scienceIdx, participantIdx1);

					ApplyTechnology(participantIdx, scienceIdx);
				}
			}
		}

		/// <inheritdoc/>
		public ushort PlayerKnowsTechnology(short participantIdx, short scienceIdx)
		{
			return (ushort)(ScienceRules.PlayerKnowsTechnology(
				this.Owner.World.Participants[participantIdx].FoundScienceMarkers,
				participantIdx,
				scienceIdx) ? 1 : 0);
		}

		/// <inheritdoc/>
		public ushort ObtainTechnologyInfo(short participantIdx, ushort scienceIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTechnologyInfo({participantIdx}, {scienceIdx})");

			sbyte b0 = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(scienceIdx * 2 + 0xb0a));
			sbyte b1 = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(scienceIdx * 2 + 0xb0b));
			int leaderVisuals = this.Owner.World.Participants[participantIdx].ChiefVisuals;
			short leaderFactor = (short)LeaderWord(leaderVisuals, LeaderTable.Leaning);
			ushort result = ScienceRules.CalculateTechnologyInfo(b0, b1, leaderFactor);

			return result;
		}

		/// <summary>The choice of an AI player, or the discovery and the next choice of the human player (see UncoverTechnology)</summary>
		private ushort UncoverTechnologyOf(short participantIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			Player human = this.Owner.World.Participants[person];

			short pick = -1;
			short highest = -1;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				short draw = (short)this.Owner.Libc.Dice.DrawInt((ushort)(ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) << 2));

				if (draw <= highest || (scienceIdx == human.PresentInquiryIdx && participantIdx == person))
					continue;

				highest = draw;
				pick = scienceIdx;
			}

			if (participantIdx == person && this.Owner.World.RoundTally != 0)
				return ChooseNextResearch(participantIdx, participantIdx1, pick);

			if (this.Owner.World.RoundTally != 0 && this.Owner.World.HardnessTier == 0 &&
				PlayerKnowsTechnology(person, pick) == 0 && this.Owner.Libc.Dice.DrawInt(2) != 0)
				return 0xffff;

			if (highest != -1)
			{
				EarnTechnology(participantIdx, pick, participantIdx1);

				this.Owner.Bot.RefreshCityProduction(participantIdx, 0xffff);
			}

			return (ushort)pick;
		}

		/// <summary>
		/// Whether the player can research the technology: unknown, with both prerequisites (0x4ee / 0x4ef + 0x16 * technology)
		/// known; SI points at its entry when it is unknown, like in the original
		/// </summary>
		private bool MayResearch(short participantIdx, short scienceIdx)
		{
			if (PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
				return false;

			ushort science = (ushort)(0x16 * scienceIdx);

			return PlayerKnowsTechnology(participantIdx, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.Prerequisite1Address))) != 0 &&
				PlayerKnowsTechnology(participantIdx, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.Prerequisite2Address))) != 0;
		}

		/// <summary>The human player's discovery and his choice of the next research (see UncoverTechnology)</summary>
		private ushort ChooseNextResearch(short participantIdx, short participantIdx1, short pick)
		{
			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];
			ushort[] choiceList = new ushort[0x48];
			bool again = false;

			while (true)
			{
				if (human.PresentInquiryIdx != -1)
				{
					AnnounceDiscovery(participantIdx, participantIdx1);
				}

				if (this.Owner.World.Participants[participantIdx].FoundScienceTally <= 1)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstciv);
				}

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if ((this.Owner.World.SessionPrefMarkers & 1) != 0 && pick != -1 && this.Owner.World.HardnessTier == 0)
				{
					RecommendResearch(pick);
				}

				ushort choices = ListResearchChoices(participantIdx, again, choiceList);

				Screen.DialogStyle = 0x6;

				if (choices == 0)
				{
					this.Owner.Words.Append(" Future Technology ");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(human.ComingScienceTally + 1), 10));

					this.Owner.Words.Append("\n");

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

					human.PresentInquiryIdx = 71;

					return 0x48;
				}

				Menu.Footer = 0x1;

				ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 80, 64, 1);

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if (choice == 0xffff)
					choice = 0;

				ushort scienceIdx = choiceList[choice];

				if (Menu.HelpTopic == 0)
				{
					human.PresentInquiryIdx = (short)scienceIdx;

					return scienceIdx;
				}

				this.Owner.Encyclopedia.ShowCivilopediaRecord(scienceIdx, 0);

				this.Owner.TurnHooks.RepaintMainScreen();

				// Asked again, with the same choices
				again = true;
			}
		}

		/// <summary>
		/// The human player's research is done: the discovery picture (discovr1 / discovr2 with technology 0x36, VGA, disk 1,
		/// preference 8, not for future technology) with the message and the icon of its first prerequisite, else the newspaper;
		/// then he learns it and the research is -1
		/// </summary>
		private void AnnounceDiscovery(short participantIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			Player human = this.Owner.World.Participants[person];

			short first = (short)this.Owner.Encyclopedia.ObtainFirstPrerequisite((ushort)human.PresentInquiryIdx);
			bool picture = human.PresentInquiryIdx != 71 && (short)Files.FirstMissingDisk > 3 &&
				this.Owner.Startup.AskForDisk(1, 0) != 0 && (this.Owner.World.SessionPrefMarkers & 8) != 0;

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

			Message(CivilizationName((ushort)participantIdx))
				.Text(PlayerKnowsTechnology(person, 0x14) != 0 ? (ushort)0x22cc : (ushort)0x22d8)
				.Text("\ndiscover the secret\nof ")
				.Text(TechnologyText(human.PresentInquiryIdx));

			if (human.PresentInquiryIdx == 71)
			{
				this.Owner.Words.Append(" ");

				human.ComingScienceTally++;

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(human.ComingScienceTally, 10));
			}

			this.Owner.Words.Append("!\n");

			// The original plays only a short fanfare (table at 0x1518, one to five seconds),
			// the national anthem (table at 0x151a, as in the palace and the throne room) plays until the next research is chosen
			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (picture)
			{
				ShowDiscoveryPicture(pictureName, first, advanced);
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(-1);
			}

			EarnTechnology(participantIdx, human.PresentInquiryIdx, participantIdx1);

			this.Owner.Services.PerformSound(1, 0);

			human.PresentInquiryIdx = -1;
		}

		/// <summary>The discovery message on the picture and the icon of the first prerequisite (iconpgN.pic, N = first / 9)</summary>
		private void ShowDiscoveryPicture(string pictureName, short first, bool advanced)
		{
			if (!advanced)
				DrawingBox.TypefaceIdx = 0x6;

			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 96, advanced ? (short)25 : (short)1, 1);

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.Words.Set("iconpg1.pic");

			this.Owner.Words[6] = (byte)(this.Owner.Words[6] + CPU.SignedQuotient(first, 9));

			this.Owner.Startup.AskForDisk(3, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.Encyclopedia.DrawIcon((ushort)(((first % 3) * 111) + 1), (ushort)((((first % 9) / 3) * 69) + 1), 110, 68, 174, advanced ? (ushort)87 : (ushort)96);

			if (Screen.VgaMode != 0)
			{
				string palName = advanced ? "discovr2.pal" : "discovr1.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, palName, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileIconpg1Pal, 0xc5be);

				this.Owner.Libc.CopyBytes(0xc5c4, 0xc1dc, 0xff);

				this.Owner.Services.LoadColorsFromBuffer(30, 0xc5be);
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();
		}

		/// <summary>The science advisor recommends the technology with the units and wonders it allows</summary>
		private void RecommendResearch(short pick)
		{
			Message("Science Advisor:\nI recommend we\ndevelop ")
				.Text(TechnologyText(pick))
				.Text(".\n");

			ushort length = this.Owner.Words.Length;

			ushort allowed = 0;

			this.Owner.Words.Append("So we can build:\n-");

			for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				ushort kindRecord = (ushort)(0x22 * kindIdx);

				if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) != (ushort)pick)
					continue;

				this.Owner.Words.Append(UnitTextAt(kindRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
			{
				ushort wonderRecord = (ushort)(0x1e * wonderIdx);

				if (BuildingWordAt(wonderRecord, BuildingTable.Technology) != (ushort)pick)
					continue;

				this.Owner.Words.Append(BuildingRecordTextAt(wonderRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			if (allowed == 0)
				this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0);

			DrawingBox.TypefaceIdx = 0x2;
			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 8, (short)-((allowed << 3) - 0xa8), 1);

			DrawingBox.TypefaceIdx = 0x1;
		}

		/// <summary>
		/// The menu of the researchable technologies (their list in the stack frame at -0x3a): from the third on, on the harder
		/// levels only those (technology - known technologies) divisible by 3; again: only the same choices in the same order;
		/// the number of choices
		/// </summary>
		private ushort ListResearchChoices(short participantIdx, bool again, ushort[] choiceList)
		{
			Message("Which discovery should our\n")
				.Text(PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? (ushort)0x2388 : (ushort)0x2393)
				.Text(" be pursuing, sire?\nPick one...\n ");

			ushort choices = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				if (!again)
				{
					if ((short)choices > 1 && (short)(scienceIdx - this.Owner.World.Participants[participantIdx].FoundScienceTally) % 3 == 0 &&
						this.Owner.World.HardnessTier != 0)
						continue;
				}
				else
				{
					if (choiceList[choices] != (ushort)scienceIdx)
						continue;
				}

				this.Owner.Words.Append(TechnologyText(scienceIdx));

				this.Owner.Words.Append("\n ");

				choiceList[choices] = (ushort)scienceIdx;
				choices++;
			}

			return choices;
		}

		/// <summary>The message about a new technology, or the emissary of a player without contact (see EarnTechnology)</summary>
		private void AnnounceTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				if ((this.Owner.World.Participants[person].Relations[participantIdx] & 0x40) == 0)
				{
					SendTechnologyEmissary(participantIdx, scienceIdx);
					return;
				}
			}

			EmptyMessage()
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(participantIdx == participantIdx1 || participantIdx1 == 0 ? (ushort)0x23d7 : (ushort)0x23e1)
				.Text(TechnologyText(scienceIdx));

			if (TechnologyNewsRules.NamesSource(participantIdx, participantIdx1))
			{
				this.Owner.Words.Append(" from\nthe ");

				if (participantIdx1 == TechnologyNewsRules.GreatLibrary)
				{
					this.Owner.Words.Append("Great Library");
				}
				else
				{
					this.Owner.Words.Append(CivilizationPlural((ushort)participantIdx1));
				}
			}

			this.Owner.Words.Append("!\n ");

			if (participantIdx != person)
			{
				// The wonders (0xbb6, names 0xb9a) and the units (0x114a, names 0x112a) the technology allows
				for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
				{
					ushort wonderRecord = (ushort)(0x1e * wonderIdx);

					if (BuildingWordAt(wonderRecord, BuildingTable.Technology) == (ushort)scienceIdx)
					{
						this.Owner.Words.Append(BuildingRecordTextAt(wonderRecord));

						this.Owner.Words.Append("\n ");
					}
				}

				for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
				{
					ushort kindRecord = (ushort)(0x22 * kindIdx);

					if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) == (ushort)scienceIdx)
					{
						this.Owner.Words.Append(UnitTextAt(kindRecord));

						this.Owner.Words.Append("\n ");
					}
				}
			}

			if ((participantIdx != person || participantIdx1 == -2) && King.Answer == 0xffff)
			{
				Menu.AnyKey = 1;

				Screen.DialogStyle = participantIdx == person ? (ushort)6 : (ushort)0;

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		/// <summary>
		/// The first time anybody learns the technology, a player without contact who knows technology 8 sends an emissary to
		/// the human player who doesn't know it, one time in 3; when he is received the king tells about it (0x240e)
		/// </summary>
		private void SendTechnologyEmissary(short participantIdx, short scienceIdx)
		{
			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) != 0 ||
				PlayerKnowsTechnology(participantIdx, 8) == 0 ||
				PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, scienceIdx) != 0 ||
				this.Owner.Libc.Dice.DrawInt(3) != 0)
				return;

			if (this.Owner.Audience.WelcomeEmissary(participantIdx, -1, 0) == 0xffff)
				return;

			King.Answer = 0;
			King.EmissaryAudience = 0;
			this.Owner.Words.Clear();

			this.Owner.Codes.Set(0, CivilizationPlural((ushort)participantIdx));

			this.Owner.Codes.Set(3, TechnologyText(scienceIdx));

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGape);

			King.Mood = 0x2;
			King.Speaking = 0xc;

			this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

			this.Owner.Audience.FinishAudience();
		}

		/// <summary>What a new technology does after its message (see EarnTechnology)</summary>
		private void ApplyTechnology(short participantIdx, short scienceIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				if (scienceIdx != 0x47)
				{
					this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)scienceIdx, 0);
				}

				if (scienceIdx == 0x14)
				{
					this.Owner.NewGameWizard.LoadGovernmentGraphics();

					this.Owner.TurnHooks.RepaintMainScreen();
				}

				if (King.Answer == 0xffff)
				{
					this.Owner.TurnHooks.RepaintMainScreen();

					if (scienceIdx == 0x5)
					{
						this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicMonarchy);
					}
				}
			}
			else if (this.Owner.World.HardnessTier == 0 && scienceIdx == 0x16)
			{
				// The exchange of ambassadors that followed never happens: the test of the original, ((Relations == 0) & 0x40),
				// is always false
			}

			if (ScienceRules.DoesTechObsoleteBarracks(scienceIdx))
			{
				for (int townIdx = 0; townIdx < 128; townIdx++)
				{
					if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff &&
						this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					{
						this.Owner.World.Towns[townIdx].StructureMarkers0 &= 0xfffd;
					}
				}

				if (participantIdx == person && King.Answer == 0xffff)
				{
					Message("Development of ")
						.Text(TechnologyText(scienceIdx))
						.Text("\nmakes existing Barracks\nobsolete.\n")
						.Style(0x3)
						.Dialog(80, 100);
				}
			}

			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == 0)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIdx] = participantIdx;

				this.Owner.UnitOps.AttachReplayContent(5, (byte)participantIdx, (byte)scienceIdx);

				// The human player's wonders (0x10fe: the technology that makes each obsolete) lose their effect
				for (ushort wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (WonderObsoleteTechnology(wonderIdx) != (ushort)scienceIdx ||
						!WonderCityRules.ReadsCity(this.Owner.World.MarvelTownIdx[wonderIdx]) ||
						this.Owner.World.Towns[this.Owner.World.MarvelTownIdx[wonderIdx]].ParticipantIdx != person)
						continue;

					Message(TechnologyText(scienceIdx))
						.Text(" cancels\nthe effect of\n")
						.Text(WonderText(wonderIdx))
						.Text(".\n");

					this.Owner.Paper.ShowNewspaper(-1);
				}
			}

			// The Great Library (wonder 5) gives a technology that two players know
			ushort libraryOwner = (ushort)this.Owner.World.MarvelTownIdx[5];

			if (libraryOwner == 0xffff || !WonderCityRules.ReadsCity(libraryOwner))
				return;

			libraryOwner = (ushort)this.Owner.World.Towns[libraryOwner].ParticipantIdx;

			if (PlayerKnowsTechnology((short)libraryOwner, scienceIdx) != 0)
				return;

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect((short)libraryOwner, 5) == 0)
				return;

			int knowing = 0;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (PlayerKnowsTechnology(otherIdx, scienceIdx) != 0)
					knowing++;
			}

			if (knowing < 2)
				return;

			EarnTechnology((short)libraryOwner, scienceIdx, -2);
		}
	}
}
