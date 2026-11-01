namespace Civilization1Remaster.Core.UI
{
	public class NewGameSetup : GameComponent, INewGameSetup
	{
		private const ushort SoundDriverOptionAddress = 0x1a30;
		private const ushort PalaceSectionLevelsAddress = PalaceBuilder.PalaceSectionLevelsAddress;
		private const ushort PalaceSectionStylesAddress = PalaceBuilder.PalaceSectionStylesAddress;
		private const ushort ParticipantCountersAddress = 0xd20c;
		private const ushort LeaderEarthStartPositionTableAddress = 0x35da;
		private const ushort VgaPaletteBufferAddress = 0x6b34;
		private const ushort ImportedPaletteBufferAddress = 0xbdf4;
		private const ushort VgaUnitSpritesTableAddress = 0xb886;
		private const ushort AdvisorTitleTextTableAddress = 0x2fac;

		public NewGameSetup(GameEngine owner)
			: base(owner)
		{
		}

		public void ConfigureFreshGame()
		{
			using var _ = this.Processor.Journal.Scope("ConfigureFreshGame()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			DrawingBox.TypefaceIdx = 0x7;

			ChooseDifficulty();

			ChooseCompetition();

			ClearWorld();

			ChooseTribe();

			StartCivilizations();

			WelcomeHumanPlayer();
		}

		public ushort CreateCivilization(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructCivilization({participantIdx})");

			ushort result = 0;
			if (participantIdx != this.Owner.World.PersonParticipantIdx || this.Owner.World.RoundTally == 0)
			{
				result = RemakeCivilization(participantIdx);
			}

			return result;
		}

		public void DestroyCivilization(short participantIdx, short participantIdx1)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateCivilization({participantIdx}, {participantIdx1})");

			if (participantIdx != 0 && participantIdx != this.Owner.World.PersonParticipantIdx && !OwnsCity(participantIdx))
			{
				Message(Tables.CivilizationName(participantIdx))
					.Text("\ncivilization\ndestroyed\nby ")
					.Text(Tables.CivilizationPlural(participantIdx1))
					.Text("!\n");

				short humanIdx = this.Owner.World.PersonParticipantIdx;
				Screen.DialogStyle = (ushort)((this.Owner.World.Participants[humanIdx].Relations[participantIdx] & 0x40) != 0 ? 5 : 2);

				if (participantIdx1 == humanIdx)
				{
					Screen.DialogStyle = 3;
				}

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Conquest.DestroySpaceship(participantIdx);

				this.Owner.UnitOps.AttachReplayContent(13, (byte)participantIdx, (byte)participantIdx1);

				UnitMemory troops = new UnitMemory(this.Processor);

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (troops.Exists(participantIdx, troopIdx))
					{
						this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);
					}
				}

				sbyte unseen = (sbyte)~(1 << participantIdx);

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					for (int vertLoc = 0; vertLoc < 50; vertLoc++)
					{
						this.Owner.World.ChartSight[horizLoc, vertLoc] &= unseen;
					}
				}

				CreateCivilization(participantIdx);
			}
		}

		public ushort CharacterizeStartLocation()
		{
			using var _ = this.Processor.Journal.Scope("CharacterizeStartLocation()");

			short[] points = new short[8];

			ushort participantIdx;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));
			}

			ushort scattered = 0;
			short best = 0;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				short nearest = NearestCivilization(participantIdx, troop);

				if (nearest >= 10 && nearest != 0x3e7)
				{
					scattered |= (ushort)(1 << (byte)participantIdx);
				}

				short start = RateStartLocation(troop, nearest);

				if (start > best)
				{
					best = start;
				}

				points[participantIdx] = start;
			}

			short chosen = -1;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				this.Owner.Words.Set("Skills: ");

				if ((short)(best - points[participantIdx]) > 3)
				{
					points[participantIdx] += 3;

					this.Owner.UnitOps.CreateUnit((short)participantIdx, 0, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
						(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

					this.Owner.Words.Append("Settlers+");
				}

				for (ushort given = 0; (short)(best - points[participantIdx]) > (short)given; given++)
				{
					if (given != 0)
						this.Owner.Words.Append("+");

					this.Owner.Words.Append((ushort)((ushort)(this.Owner.Advisors.UncoverTechnology((short)participantIdx, 0) * 0x16) + 0x4da));
				}

				if (points[participantIdx] != best)
					continue;

				this.Owner.Words.Append("NONE");

				if (chosen == -1 || (scattered & (1 << (byte)participantIdx)) != 0)
				{
					chosen = (short)participantIdx;
				}
			}

			return (ushort)chosen;
		}

		public void ShowTribeSelection()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTribeSelection()");

			this.Owner.Startup.AskForDisk(7, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSp299Pic, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
				Screen.PictureScreenBox, 0xa0, 0x32);

			for (int idx = 0; idx < 3; idx++)
			{
				SavePicture(0xdf62, idx, 1, (ushort)((0x2a * idx) + 0xa0), 0x8e, 0x29, 0x3a);
			}

			if (Screen.VgaMode != 0)
			{
				CutVgaUnitSprites();
			}
			else
			{
				CutEgaUnitSprites();
			}

			if (Screen.VgaMode != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSp257PicB, 0);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSp256Pal, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.Processor.EmitGlobalByte((ushort)(VgaPaletteBufferAddress + idx), this.Processor.PullGlobalByte((ushort)(ImportedPaletteBufferAddress + idx)));
				}
			}

			for (int idx = 0; idx < 8; idx++)
			{
				SavePicture(0xb27a, idx, 1, (ushort)(idx << 4), 0x30, 0x10, 0x10);
				SavePicture(0xb29a, idx, 1, (ushort)((idx << 4) + 0x80), 0x60, 0x10, 0x10);
			}

			SavePicture(0xb2ba, 0, 1, 0x30, 0x20, 0x10, 0x10);

			for (int idx = 1; idx < 0x10; idx++)
			{
				SavePicture(0x6dfe, idx, 1, (ushort)(idx << 4), 0x40, 0x10, 0x10);
				SavePicture(0x6e1e, idx, 1, (ushort)(idx << 4), 0x50, 0x10, 0x10);
			}

			for (int idx = 0; idx < 0x14; idx++)
			{
				ushort horizLoc = (ushort)((7 * idx) + 0xa0);

				SavePicture(0xd21c, idx, 1, horizLoc, 0, 7, 15);
				SavePicture(0xd244, idx, 1, horizLoc, 0x10, 7, 15);
				SavePicture(0xd26c, idx, 1, horizLoc, 0x20, 7, 15);
			}

			for (int idx = 0; idx < 4; idx++)
			{
				SavePicture(0x7eec, idx, 1, (ushort)((idx << 4) + 0x50), 0x80, 0x10, 0x10);
			}

			for (int idx = 0; idx < 7; idx++)
			{
				SavePicture(0xd4ce, idx, 1, (ushort)(idx << 4), 0x20, 0x10, 0x10);
			}

			Mouse.Picture = Mouse.Icon;

			for (int idx = 0; idx < 8; idx++)
			{
				SavePicture(0xd4de, idx, 1, (ushort)(((idx & 3) << 3) + 0x81), (ushort)(((idx & 4) << 1) + 0x21), 7, 7);
			}

			for (int idx = 0; idx < 9; idx++)
			{
				SavePicture(0x6e96, idx, 1, (ushort)(idx << 3), 0x80, 8, 0x10);
			}

			for (int idx = 0; idx < 0x10; idx++)
			{
				SavePicture(0xd4ee, idx, 1, (ushort)((idx << 4) + 1), 0x71, 15, 15);
			}

			for (int idx = 0; idx < 8; idx++)
			{
				SavePicture(0xd50e, idx, 1, (ushort)((idx << 4) + 1), 0x61, 15, 15);
			}

			SavePicture(0xb880, 0, 1, 0x99, 0x29, 7, 7);
			SavePicture(0xdf0c, 0, 1, 0xb0, 0x80, 0x14, 9);

			for (int playerIdx = 0; playerIdx < 8; playerIdx++)
			{
				CutPlayerCitySprites(playerIdx);
			}
		}

		public void LoadGovernmentGraphics()
		{
			using var _ = this.Processor.Journal.Scope("ImportGovernmentGraphics()");

			this.Owner.Startup.AskForDisk(2, 1);

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;
			char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char techChar = regime != 3 && this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? 'm' : 'a';
			string docname = $"govt{regimeChar}{techChar}.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, docname, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0xa0, 0x3c,
				Screen.PictureScreenBox, 0xa0, 0x8c);

			this.Owner.Startup.AskForDisk(0, 1);
		}

		public void ShowRevolutionScreen()
		{
			using var _ = this.Processor.Journal.Scope("DisplayRevolutionScreen()");

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;

			if (regime != 0)
			{
				this.Owner.Advisors.DrawAdvisorScenery();

				Message(Tables.CivilizationName(this.Owner.World.PersonParticipantIdx))
					.Text(" government\nchanged to ");
				this.Owner.Words.Append(Tables.GovernmentName(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind));
				this.Owner.Words.Append("!\n");

				this.Owner.Paper.ShowNewspaper(-2);

				this.Owner.Startup.HideMouseCursor();

				this.Owner.Services.FloodBox(Screen.Box, 0, 100, 320, 100, Hues.White);

				DrawingBox.TypefaceIdx = 0x6;

				this.Owner.TextPainter.DrawCenteredTextToScreen0(GameTexts.TextNewCabinet, 160, 102, Hues.Black);

				DrawingBox.TypefaceIdx = 0x2;

				for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)((0x28 * advisorIdx) + 0xa0), 0x8c, 0x28, 0x3c,
						Screen.Box, (ushort)((0x50 * advisorIdx) + 0x14), 0x76);

					this.Owner.Words.Set(this.Processor.PullGlobalWord((ushort)((advisorIdx << 1) + AdvisorTitleTextTableAddress)));
					this.Owner.Words[this.Owner.Words.Length - 1] = 0x0;

					this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), (80 * advisorIdx) + 40, (advisorIdx & 1) != 0 ? 186 : 180, Hues.Black);
				}

				DrawingBox.TypefaceIdx = 0x1;

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.Advisors.RecoverAdvisorScenery();
			}
		}

		public void ClearSpaceshipContent(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"WipeSpaceshipContent({participantIdx})");

			this.Owner.World.Participants[participantIdx].StarshipArrivalSeason = -1;

			this.Processor.SegExtra.Value16 = 0x3772;

			for (int row = 0; row < 0xf; row++)
			{
				for (int column = 0; column < 0xc; column++)
				{
					this.Processor.EmitUInt8(this.Processor.SegExtra.Value16,
						(ushort)((0xb4 * participantIdx) + (0xc * row) + column + 0x16e0), 0xff);
				}
			}
		}

		private void ChooseDifficulty()
		{
			this.Owner.Words.Set("Difficulty Level...\n Chieftain (easiest)\n Warlord\n Prince\n King\n Emperor (toughest)\n");

			this.Owner.World.HardnessTier = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 35, 1);

			if (Mouse.Present != 0 && Menu.MouseUsed != 0)
			{
				this.Owner.Startup.ReadMouseState();

				if ((short)this.Owner.GlobDb3c < 0x87)
				{
					this.Owner.World.HardnessTier = (short)this.Owner.Helpers.ClampValue(((short)this.Owner.GlobDb3e - 12) / 35, 0, 4);
				}
			}

			if (this.Owner.World.HardnessTier == -1)
			{
				this.Owner.World.HardnessTier = 0;
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 150, 200, Hues.Black);

			DrawDifficultyPortrait(0x14, 0x64);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				Screen.Box, 0x96, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private void DrawDifficultyPortrait(ushort horizLoc, ushort vertLoc)
		{
			ushort column = (ushort)((this.Owner.World.HardnessTier & 1) != 0 ? 0x50 : 0x15);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, column,
				(ushort)((0x23 * this.Owner.World.HardnessTier) + 6), 0x35, 0x2f, Screen.Box, horizLoc, vertLoc);
		}

		private void ChooseCompetition()
		{
			this.Owner.Words.Set("Level of Competition...\n ");

			for (short tally = 7; tally > 2; tally--)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(tally, 10));

				this.Owner.Words.Append(" Civilizations\n ");
			}

			do
			{
				this.Owner.World.BotRivalTally = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 35, 1);
			}
			while ((ushort)this.Owner.World.BotRivalTally == 0xffff);

			this.Owner.World.BotRivalTally = (short)this.Owner.Helpers.ClampValue(6 - this.Owner.World.BotRivalTally, 2, 6);
			this.Owner.World.LiveRealms = (short)(0xff >> (byte)(6 - (byte)this.Owner.World.BotRivalTally));

			this.Owner.Startup.HideMouseCursor();

			for (short rivalIdx = this.Owner.World.BotRivalTally; rivalIdx >= 0; rivalIdx--)
			{
				DrawDifficultyPortrait((ushort)((rivalIdx << 1) + 0x14), (ushort)((3 * rivalIdx) + 0x64));
			}
		}

		private void ClearWorld()
		{
			this.Owner.World.Season = -4000;
			this.Owner.World.DiagMarkers = 0xf;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				this.Owner.World.Towns[townIdx].LabelIdx = 0xff;
				this.Owner.World.Towns[townIdx].ConditionMarker = 0xff;
			}

			for (int idx = 0; idx < 256; idx++)
			{
				this.Owner.World.TownPlaces[idx].X = -1;
			}

			for (int scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIdx] = 0;
			}

			for (int marvelIdx = 0; marvelIdx <= 0x15; marvelIdx++)
			{
				this.Owner.World.MarvelTownIdx[marvelIdx] = -1;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.Owner.World.Participants[participantIdx].ChiefVisuals = -1;

				ClearSpaceshipContent(participantIdx);
			}

			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					this.Owner.World.ChartSight[horizLoc, vertLoc] = 0;
				}
			}

			this.Owner.World.SessionPrefMarkers = 0xfa;

			if (this.Processor.PullGlobalByte(SoundDriverOptionAddress) == 0x4e)
			{
				this.Owner.World.SessionPrefMarkers &= 0x7fef;
			}
		}

		private void ChooseTribe()
		{
			this.Owner.Words.Set("Pick your tribe...\n ");

			_ = this.Owner.Words.Length;

			for (int leader = 1; leader < this.Owner.World.BotRivalTally + 2; leader++)
			{
				this.Owner.Words.Append(GameTables.LeaderText(leader, LeaderTable.Civilization));

				this.Owner.Words.Append("\n ");
			}

			for (int leader = 1; leader < this.Owner.World.BotRivalTally + 2; leader++)
			{
				this.Owner.Words.Append((ushort)((0x3a * leader) + 0x16d2));

				this.Owner.Words.Append("\n ");
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				Screen.Box, 0x96, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.World.PersonParticipantIdx = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 35, 1);

			if (this.Owner.World.PersonParticipantIdx <= this.Owner.World.BotRivalTally)
			{
				this.Owner.World.PersonParticipantIdx++;
			}
			else
			{
				this.Owner.World.PersonParticipantIdx = (short)(this.Owner.World.PersonParticipantIdx - this.Owner.World.BotRivalTally + 8);
			}

			if (this.Owner.World.HardnessTier == 0)
			{
				this.Owner.World.SessionPrefMarkers |= 1;
			}

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx & 7].PresentInquiryIdx = -1;

			this.Owner.World.FollowingCollectionRound += (short)this.Owner.Libc.Dice.DrawInt(50);

			for (int idx = 0; idx < 0xc; idx++)
			{
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (idx << 1)), 0xffff);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionStylesAddress + (idx << 1)), 0x0);
			}

			for (int idx = 3; idx <= 5; idx++)
			{
				this.Processor.EmitGlobalWord((ushort)(0xd92a + (idx << 1)), 0);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (idx << 1)), 0);
			}

			bool randomTribe = this.Owner.World.PersonParticipantIdx == 0;
			bool secondLeader = false;

			if (randomTribe)
			{
				this.Owner.World.PersonParticipantIdx = (short)(this.Owner.Libc.Dice.DrawInt(this.Owner.World.BotRivalTally) + 1);
			}
			else
			{
				secondLeader = this.Owner.World.PersonParticipantIdx > 7;
				this.Owner.World.PersonParticipantIdx &= 7;
			}

			short personIdx = this.Owner.World.PersonParticipantIdx;
			ushort names = (ushort)(personIdx << 1);

			this.Processor.EmitGlobalByte(Tables.CivilizationNameAt(names), 0x0);

			if (randomTribe)
			{
				this.Owner.CityNaming.ShowCityFoundedMessage();

				this.Owner.World.Participants[personIdx].ChiefVisuals = personIdx;

				this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderText(personIdx, LeaderTable.Name));
			}
			else
			{
				Player person = this.Owner.World.Participants[personIdx];

				person.ChiefVisuals = (short)(secondLeader ? personIdx + 8 : personIdx);

				ushort leader = (ushort)(0x3a * person.ChiefVisuals);

				this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Name));

				this.Owner.Names.Set(Tables.CivilizationNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Civilization));

				this.Owner.Words.Set(GameTables.LeaderTextAt(leader, LeaderTable.CivilizationPlural));

				if (this.Owner.Words[0] == 0)
				{
					Message(Tables.CivilizationNameAt(names))
						.Text("s");
				}

				this.Owner.Names.Set(Tables.CivilizationPluralAt(names), this.Owner.Words.ToString());
			}

			this.Owner.TextPainter.DrawCenteredTextToScreen0(Tables.CivilizationPluralAt(names), 46, 92, Hues.White);
		}

		private void StartCivilizations()
		{
			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				CreateCivilization(participantIdx);

				int cleared = participantIdx;

				for (int kind = 0; kind < 8; kind++)
				{
					this.Owner.World.Participants[cleared].TroopsRuined[kind] = 0;
				}
			}

			CharacterizeStartLocation();

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			this.Owner.World.ParticipantMarkers = (short)(1 << this.Owner.World.PersonParticipantIdx);
			person.LevyRatio = 5;
			person.ResearchLevyRatio = 5;

			if (this.Owner.World.HardnessTier == 0)
			{
				person.Gold = 50;
			}

			this.Owner.World.ParticipantIdentMarkers = 0;

			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if (this.Owner.World.Participants[participantIdx].ChiefVisuals >= 8)
				{
					this.Owner.World.ParticipantIdentMarkers |= (short)(1 << participantIdx);
				}
			}
		}

		private void WelcomeHumanPlayer()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 150, 0, 170, 200, Hues.Black);

			this.Owner.CityNaming.ShowFoundCityMessage();

			this.Owner.Startup.ShowMouseCursor();

			DrawingBox.TypefaceIdx = 0x1;

			ushort names = (ushort)(this.Owner.World.PersonParticipantIdx << 1);

			this.Owner.Codes.Set(3, Tables.LeaderNameAt(names));

			this.Owner.Codes.Set(0, Tables.CivilizationPluralAt(names));

			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicInit);

			_ = this.Owner.Words.Length;

			int shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, scienceIdx) == 0)
					continue;

				this.Owner.Words.Append(GameTables.TechnologyText(scienceIdx));

				this.Owner.Words.Append(", ");

				shown++;

				if (shown == 2)
				{
					this.Owner.Words.Append("\n");
				}
			}

			this.Owner.Words.Append("and Roads.\n");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);

			this.Owner.Services.PerformSound(Tables.LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

			this.Owner.UnitOps.DrawBusyIndicator();

			DrawDifficultyPortrait(0x86, 0x14);

			this.Owner.BlockText.DrawTextChunk(99, 88, 81, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		private ushort RemakeCivilization(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			player.StarshipInhabitants = 0;
			player.RunningSagaStanding = 0;
			player.FoundScienceTally = 0;
			player.InquiryHeadway = 0;
			player.Gold = 0;
			player.RegimeKind = 1;
			player.LevyRatio = 4;
			player.ResearchLevyRatio = 4;

			ushort[] foreignTowns = new ushort[16];

			for (int continentIdx = 0; continentIdx < 16; continentIdx++)
			{
				player.Landmasses[continentIdx].Approach = 0;

				for (int otherIdx = 1; otherIdx < 8; otherIdx++)
				{
					foreignTowns[continentIdx] += (ushort)this.Owner.World.Participants[otherIdx].Landmasses[continentIdx].TownTally;
				}
			}

			player.TownTally = 0;
			player.TroopTally = 0;
			player.SoilTally = 1;
			player.ColonistTally = 1;
			player.TouchParticipantTimeleft = -1;

			for (int idx = 0; idx < player.ScienceObtainedSince.Length; idx++)
			{
				player.ScienceObtainedSince[idx] = -1;
			}

			for (int troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)((0x600 * participantIdx) + (0xc * troopIdx));

				this.Troops.AssignAt(troop, UnitMemory.Kind, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.Status, 0x0);
			}

			this.Processor.EmitGlobalWord((ushort)(ParticipantCountersAddress + (participantIdx << 1)), 0);
			player.Tally = 0;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Services.FloodBox(Screen.PictureScreenBox, 240, 0, 80, 50, Hues.Black);
			}

			this.Owner.Bot.ClearStrategicLocations(participantIdx);

			for (int otherIdx = 0; otherIdx < 8; otherIdx++)
			{
				player.Relations[otherIdx] = 0;
				this.Owner.World.Participants[otherIdx].Relations[participantIdx] = 0;
			}

			for (int idx = 0; idx < 5; idx++)
			{
				player.FoundScienceMarkers[idx] = 0;
			}

			if (this.Owner.World.RoundTally != 0)
			{
				ShareKnownTechnologies(participantIdx);
			}

			if (participantIdx != 0 && (this.Owner.World.LiveRealms & (1 << participantIdx)) != 0)
			{
				if (!SettleCivilization(participantIdx, foreignTowns))
					return 0;

				player.FoundScienceTally = 1;
			}

			for (int kind = 0; kind < 28; kind++)
			{
				player.MissingTroops[kind] = 0;
				player.LiveTroops[kind] = 0;
				player.TroopsInsideMaking[kind] = 0;
			}

			return 1;
		}

		private void ShareKnownTechnologies(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0)
					continue;

				int finder = (byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7;

				if (finder == 0 || finder == (ushort)personIdx)
					continue;

				if (this.Owner.Libc.Dice.DrawInt(2) == 0)
				{
					this.Owner.Advisors.EarnTechnology(participantIdx, scienceIdx, participantIdx);
				}
			}
		}

		private bool SettleCivilization(short participantIdx, ushort[] foreignTowns)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				if (this.Owner.World.RoundTally == 0)
				{
					ChooseFreeLeader(participantIdx);
				}
				else
				{
					player.ChiefVisuals ^= 8;
				}
			}

			(short horizLoc, short vertLoc, ushort attemptTally) = ChooseStartPlace(participantIdx, foreignTowns);

			if (this.Owner.World.RoundTally == 0 && NewWorld.EarthGame != 0)
			{
				ushort place = (ushort)(player.ChiefVisuals << 1);

				horizLoc = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress));
				vertLoc = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress + 1));
				attemptTally = 0;
			}

			if (this.Owner.World.RoundTally != 0)
			{
				bool identity = (this.Owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				if (player.ChiefVisuals >= 8 && identity)
				{
					attemptTally = 0x7d0;
				}

				if (player.ChiefVisuals < 8 && !identity)
				{
					attemptTally = 0x7d0;
				}
			}

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				player.ResearchLevyRatio = (short)(Tables.LeaderWord(player.ChiefVisuals, LeaderTable.Leaning) + 3);
				player.LevyRatio = (short)(9 - player.ResearchLevyRatio);
			}

			ushort latestRound = (ushort)(0x15e - (0x32 * this.Owner.World.HardnessTier));

			if ((latestRound < this.Owner.World.RoundTally || attemptTally >= 0x7d0) &&
				participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				player.ChiefVisuals ^= 8;
				this.Owner.World.LiveRealms &= (short)~(1 << participantIdx);

				if ((ushort)((1 << this.Owner.World.PersonParticipantIdx) | 1) == (ushort)this.Owner.World.LiveRealms)
				{
					Turn.GameEndRequest = 1;
					Turn.ConquestEnd = 1;
				}

				return false;
			}

			NameCivilization(participantIdx);

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);

			this.Owner.UnitOps.CreateUnit(participantIdx, 0, horizLoc, vertLoc);

			player.HorizBegin = horizLoc;

			for (short troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)(0xc * troopIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
					vertLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

				if (distance <= 8)
				{
					this.Owner.UnitOps.DestroyUnit(0, troopIdx);
				}
			}

			return true;
		}

		private void ChooseFreeLeader(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			bool free;

			do
			{
				player.ChiefVisuals = (short)this.Owner.Libc.Dice.DrawInt(16);

				free = (player.ChiefVisuals & 7) == participantIdx;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if (otherIdx != participantIdx && this.Owner.World.Participants[otherIdx].ChiefVisuals == player.ChiefVisuals)
					{
						free = false;
					}
				}
			}
			while (!free);
		}

		private (short HorizLoc, short VertLoc, ushort AttemptTally) ChooseStartPlace(short participantIdx, ushort[] foreignTowns)
		{
			ushort attemptTally = 0;

			while (true)
			{
				short horizLoc = (short)(this.Owner.Libc.Dice.DrawInt(64) + 8);
				short vertLoc = (short)(this.Owner.Libc.Dice.DrawInt(34) + 8);

				ushort townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
				short nearest = townIdx == 0xffff ? (short)0x1e : (short)this.Owner.Helpers.ObtainDistance(horizLoc,
					this.Owner.World.Towns[townIdx].Place.X, vertLoc, this.Owner.World.Towns[townIdx].Place.Y);

				if (this.Owner.World.RoundTally == 0)
				{
					for (int otherIdx = 1; otherIdx < participantIdx; otherIdx++)
					{
						ushort troop = (ushort)(0x600 * otherIdx);

						short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
							vertLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

						if (distance < nearest)
						{
							nearest = distance;
						}
					}
				}

				short landValue = (short)this.Owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

				attemptTally++;

				if ((short)attemptTally > 0x7d0 &&
					(participantIdx != this.Owner.World.PersonParticipantIdx || this.Owner.World.RoundTally != 0))
				{
					return (horizLoc, vertLoc, attemptTally);
				}

				if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if ((short)(0xc - ((short)attemptTally / 32)) > landValue)
					continue;

				short relief = (short)((short)attemptTally / 64);

				if (nearest < (short)(0xa - relief))
					continue;

				ushort continentIdx = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
				short siteDemand = (short)(0x20 - (this.Owner.World.RoundTally / 16) - relief);

				if (this.Owner.World.Landmasses[continentIdx].ConstructionSpotTally < siteDemand)
					continue;

				if (this.Owner.World.Season > 0 && foreignTowns[this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc)] != 0)
					continue;

				if (this.Owner.MapOps.ContainsHut(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc), horizLoc, vertLoc) != 0)
					continue;

				return (horizLoc, vertLoc, attemptTally);
			}
		}

		private void NameCivilization(short participantIdx)
		{
			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIdx].ChiefVisuals);
			ushort names = (ushort)(participantIdx << 1);

			this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Name));

			if (participantIdx == this.Owner.World.PersonParticipantIdx &&
				this.Processor.PullGlobalByte(Tables.CivilizationNameAt(names)) != 0)
				return;

			this.Owner.Names.Set(Tables.CivilizationNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Civilization));

			this.Owner.Words.Set(GameTables.LeaderTextAt(leader, LeaderTable.CivilizationPlural));

			if (this.Owner.Words[0] == 0)
			{
				Message(Tables.CivilizationNameAt(names))
					.Text("s");
			}

			this.Owner.Names.Set(Tables.CivilizationPluralAt(names), this.Owner.Words.ToString());
		}

		private bool OwnsCity(short participantIdx)
		{
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff && this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					return true;
			}

			return false;
		}

		private short NearestCivilization(ushort participantIdx, ushort troop)
		{
			short nearest = 0x3e7;
			ushort otherIdx;

			for (otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == participantIdx)
					continue;

				ushort other = (ushort)(0x600 * otherIdx);

				if (this.Troops.PullAt(other, UnitMemory.Kind) == 0xff)
					continue;

				ushort otherContinent = this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(other, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(other, UnitMemory.Vert));

				if (this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert)) != otherContinent)
					continue;

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(other, UnitMemory.Horiz),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert), (sbyte)this.Troops.PullAt(other, UnitMemory.Vert));

				if (distance < nearest)
				{
					nearest = distance;
				}
			}

			return nearest;
		}

		private short RateStartLocation(ushort troop, short nearest)
		{
			short start = 0;

			this.Owner.Words.Set("Nearest civilization: ");

			if (nearest < 10)
			{
				this.Owner.Words.Append("close");
			}
			else if (nearest < 0x14)
			{
				this.Owner.Words.Append("moderate");
				start++;
			}
			else if (nearest != 0x3e7)
			{
				this.Owner.Words.Append("far away");
				start += 2;
			}
			else
			{
				this.Owner.Words.Append("isolated");
				start += 4;
			}

			ushort ocean = 0, grassland = 0, river = 0;
			ushort neighbourIdx;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz) + (short)Tables.DirectionHoriz(neighbourIdx),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert) + (short)Tables.DirectionVert(neighbourIdx));

				if (terrain == 0xa)
					ocean++;

				if (terrain == 2)
					grassland++;

				if (terrain == 0xb)
					river++;
			}

			this.Owner.Words.Set(ocean != 0 ? (ushort)0x3653 : (ushort)0x365c);

			if ((short)river > 1)
			{
				this.Owner.Words.Append("river valley on a ");
				start += 2;
			}
			else if ((short)grassland >= 3)
			{
				this.Owner.Words.Append("grasslands on a ");
				start++;
			}
			else
			{
				this.Owner.Words.Append("plains on a ");
			}

			switch (CPU.SignedQuotient((short)this.Owner.MapOps.ObtainLandMassSize((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert)), 0x32))
			{
				case 0:
					this.Owner.Words.Append(0x3695);
					break;

				case 1:
					this.Owner.Words.Append(0x36a3);
					break;

				case 2:
					this.Owner.Words.Append(0x36b1);
					start++;
					break;

				case 3:
					this.Owner.Words.Append(0x36c2);
					start++;
					break;

				default:
					this.Owner.Words.Append("large continent.");
					start += 2;
					break;
			}

			return start;
		}

		private void SavePicture(ushort tableAddr, int idx, ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			ushort picture = this.Owner.VideoCard.ScreenToPicture(displayIdx, horizLoc, vertLoc, breadth, altitude);

			this.Processor.EmitGlobalWord((ushort)(tableAddr + (idx << 1)), picture);
		}

		private void CutVgaUnitSprites()
		{
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileTer257Pic, 0);

			for (int row = 0; row < 0xa; row++)
			{
				for (int column = 0; column < 0x10; column++)
				{
					ushort entry = (ushort)(VgaUnitSpritesTableAddress + (row << 5) + (column << 1));

					if ((row >= 6 || Screen.LowMemoryVga != 0) && column != 0)
					{
						this.Processor.EmitGlobalWord(entry, this.Processor.PullGlobalWord((ushort)(entry - 2)));
					}
					else
					{
						this.Processor.EmitGlobalWord(entry,
							this.Owner.VideoCard.ScreenToPicture(1, (ushort)(column << 4), (ushort)(row << 4), 0x10, 0x10));
					}
				}
			}

			for (int tile = 0; tile < 8; tile++)
			{
				for (int quarter = 0; quarter < 4; quarter++)
				{
					ushort horizLoc = quarter < 2
						? (ushort)(((quarter & 1) << 3) + (tile << 4))
						: (ushort)(((tile << 4) - ((quarter & 1) << 3)) + 8);
					ushort vertLoc = (ushort)(quarter < 2 ? 0xb0 : 0xb8);

					SavePicture((ushort)(0xd294 + (tile << 3)), quarter, 1, horizLoc, vertLoc, 8, 8);
				}
			}

			for (int idx = 0; idx < 4; idx++)
			{
				SavePicture(0xd2d4, idx, 1, (ushort)((idx << 4) + 0x80), 0xb0, 0x10, 0x10);
			}
		}

		private void CutEgaUnitSprites()
		{
			this.Owner.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPicB, 1);

			this.Owner.VideoCard.RenderImage(Screen.SpriteScreenBox, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			for (int idx = 0; idx < 0xc; idx++)
			{
				SavePicture(0xb886, idx << 4, 3, (ushort)(idx << 4), 0, 0x10, 0x10);
				SavePicture(0xb888, idx << 4, 3, (ushort)(idx << 4), 0x10, 0x10, 0x10);
			}
		}

		private void CutPlayerCitySprites(int playerIdx)
		{
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0xa0, 0x140, 0x20,
				Screen.BackScreenBox, 0, 0x80);

			ushort colours = (ushort)(playerIdx << 1);
			ushort primaryColour = Tables.PlayerColourAt(colours);
			ushort secondaryColour = Tables.PlayerSecondaryColourAt(colours);

			if (primaryColour == 0xf)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.White, Hues.LightCyan);
			}

			if (primaryColour == 0x7)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.LightGray, Hues.Cyan);
			}

			this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.LightGreen,
				(byte)primaryColour);

			this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.Green,
				(byte)secondaryColour);

			for (int idx = 0; idx < 0x1c; idx++)
			{
				SavePicture(0xd54e, (playerIdx << 5) + idx, 1, (ushort)(((idx % 0x14) << 4) + 1), (ushort)(((idx / 0x14) << 4) + 0x81), 15, 15);
			}
		}
	}
}
