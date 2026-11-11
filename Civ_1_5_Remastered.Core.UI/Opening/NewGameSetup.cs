namespace Civ_1_5_Remastered.Core.UI.Opening
{
	public class NewGameSetup : GameComponent, INewGameSetup
	{
		private const ushort SoundDriverOptionAddress = 0x1a30;
		private const ushort PalaceSectionLevelsAddress = IPalaceBuilder.PalaceSectionLevelsAddress;
		private const ushort PalaceSectionStylesAddress = IPalaceBuilder.PalaceSectionStylesAddress;
		private const ushort ParticipantCountersAddress = 0xd20c;
		private const ushort LeaderEarthStartPositionTableAddress = 0x35da;
		private const ushort VideoGraphicsPaletteBufferAddress = 0x6b34;
		private const ushort ImportedPaletteBufferAddress = 0xbdf4;
		private const ushort VideoGraphicsUnitSpritesTableAddress = 0xb886;
		private const ushort AdvisorTitleTextTableAddress = 0x2fac;

		/// <summary>The original colours of the slots (primary, secondary), barbarians first.</summary>
		private static readonly (byte Primary, byte Secondary)[] OriginalColours =
		[
			(Hues.LightRed, Hues.Red), (Hues.White, Hues.LightGray), (Hues.LightGreen, Hues.Green), (Hues.LightBlue, Hues.Blue),
			(Hues.Yellow, Hues.LightGreen), (Hues.LightCyan, Hues.Cyan), (Hues.LightMagenta, Hues.Red), (Hues.LightGray, Hues.DarkGray),
		];

		/// <summary>The slot the computer plays Poland in, chosen when a new game starts (-1: none).</summary>
		private short polandSlot = -1;

		public NewGameSetup(IGameEngine owner)
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

			DrawingBox.TypefaceIndex = 0x7;

			ChooseDifficulty();

			ChooseCompetition();

			ClearWorld();

			ChooseTribe();

			StartCivilizations();

			WelcomeHumanPlayer();
		}

		public ushort CreateCivilization(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructCivilization({participantIndex})");

			ushort result = 0;
			if (participantIndex != this.Owner.World.PersonParticipantIndex || this.Owner.World.RoundTally == 0)
			{
				result = RemakeCivilization(participantIndex);
			}

			return result;
		}

		public void DestroyCivilization(short participantIndex, short firstParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateCivilization({participantIndex}, {firstParticipantIndex})");

			if (participantIndex != 0 && participantIndex != this.Owner.World.PersonParticipantIndex && !OwnsCity(participantIndex))
			{
				Message(Tables.CivilizationName(participantIndex))
					.Text("\ncivilization\ndestroyed\nby ")
					.Text(Tables.CivilizationPlural(firstParticipantIndex))
					.Text("!\n");

				short humanIndex = this.Owner.World.PersonParticipantIndex;
				Screen.DialogStyle = (ushort)((this.Owner.World.Participants[humanIndex].Relations[participantIndex] & 0x40) != 0 ? 5 : 2);

				if (firstParticipantIndex == humanIndex)
				{
					Screen.DialogStyle = 3;
				}

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Conquest.DestroySpaceship(participantIndex);

				this.Owner.UnitOperations.AttachReplayContent(13, (byte)participantIndex, (byte)firstParticipantIndex);

				UnitMemory troops = new UnitMemory(this.Processor);

				for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (troops.Exists(participantIndex, troopIndex))
					{
						this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
					}
				}

				sbyte unseen = (sbyte)~(1 << participantIndex);

				for (int horizontalLocation = 0; horizontalLocation < 80; horizontalLocation++)
				{
					for (int verticalLocation = 0; verticalLocation < 50; verticalLocation++)
					{
						this.Owner.World.ChartSight[horizontalLocation, verticalLocation] &= unseen;
					}
				}

				CreateCivilization(participantIndex);
			}
		}

		public ushort CharacterizeStartLocation()
		{
			using var _ = this.Processor.Journal.Scope("CharacterizeStartLocation()");

			short[] points = new short[8];

			ushort participantIndex;

			for (participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				ushort troop = (ushort)(0x600 * participantIndex);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				this.Owner.MapOperations.ObtainContinentIdentifier((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));
			}

			ushort scattered = 0;
			short best = 0;

			for (participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				ushort troop = (ushort)(0x600 * participantIndex);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				short nearest = NearestCivilization(participantIndex, troop);

				if (nearest >= 10 && nearest != 0x3e7)
				{
					scattered |= (ushort)(1 << (byte)participantIndex);
				}

				short start = RateStartLocation(troop, nearest);

				if (start > best)
				{
					best = start;
				}

				points[participantIndex] = start;
			}

			short chosen = -1;

			for (participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				ushort troop = (ushort)(0x600 * participantIndex);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				this.Owner.Words.Set("Skills: ");

				if ((short)(best - points[participantIndex]) > 3)
				{
					points[participantIndex] += 3;

					this.Owner.UnitOperations.CreateUnit((short)participantIndex, 0, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal),
						(sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

					this.Owner.Words.Append("Settlers+");
				}

				for (ushort given = 0; (short)(best - points[participantIndex]) > (short)given; given++)
				{
					if (given != 0)
					{
						this.Owner.Words.Append("+");
					}

					this.Owner.Words.Append((ushort)((ushort)(this.Owner.Advisors.UncoverTechnology((short)participantIndex, 0) * 0x16) + 0x4da));
				}

				if (points[participantIndex] != best)
				{
					continue;
				}

				this.Owner.Words.Append("NONE");

				if (chosen == -1 || (scattered & (1 << (byte)participantIndex)) != 0)
				{
					chosen = (short)participantIndex;
				}
			}

			return (ushort)chosen;
		}

		public void ShowTribeSelection()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTribeSelection()");

			this.Owner.Startup.AskForDisk(7, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPartsPicture, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
				Screen.PictureScreenBox, 0xa0, 0x32);

			for (int index = 0; index < 3; index++)
			{
				SavePicture(0xdf62, index, 1, (ushort)((0x2a * index) + 0xa0), 0x8e, 0x29, 0x3a);
			}

			if (Screen.VideoGraphicsMode != 0)
			{
				CutVideoGraphicsUnitSprites();
			}
			else
			{
				CutEnhancedGraphicsUnitSprites();
			}

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPictureSecond, 0);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSpaceshipPalette, 0xbdee);

				for (int index = 0; index < 0x30; index++)
				{
					this.Processor.EmitGlobalByte((ushort)(VideoGraphicsPaletteBufferAddress + index), this.Processor.PullGlobalByte((ushort)(ImportedPaletteBufferAddress + index)));
				}
			}

			for (int index = 0; index < 8; index++)
			{
				SavePicture(0xb27a, index, 1, (ushort)(index << 4), 0x30, 0x10, 0x10);
				SavePicture(0xb29a, index, 1, (ushort)((index << 4) + 0x80), 0x60, 0x10, 0x10);
			}

			SavePicture(0xb2ba, 0, 1, 0x30, 0x20, 0x10, 0x10);

			for (int index = 1; index < 0x10; index++)
			{
				SavePicture(0x6dfe, index, 1, (ushort)(index << 4), 0x40, 0x10, 0x10);
				SavePicture(0x6e1e, index, 1, (ushort)(index << 4), 0x50, 0x10, 0x10);
			}

			for (int index = 0; index < 0x14; index++)
			{
				ushort horizontalLocation = (ushort)((7 * index) + 0xa0);

				SavePicture(0xd21c, index, 1, horizontalLocation, 0, 7, 15);
				SavePicture(0xd244, index, 1, horizontalLocation, 0x10, 7, 15);
				SavePicture(0xd26c, index, 1, horizontalLocation, 0x20, 7, 15);
			}

			for (int index = 0; index < 4; index++)
			{
				SavePicture(0x7eec, index, 1, (ushort)((index << 4) + 0x50), 0x80, 0x10, 0x10);
			}

			for (int index = 0; index < 7; index++)
			{
				SavePicture(0xd4ce, index, 1, (ushort)(index << 4), 0x20, 0x10, 0x10);
			}

			Mouse.Picture = Mouse.Icon;

			for (int index = 0; index < 8; index++)
			{
				SavePicture(0xd4de, index, 1, (ushort)(((index & 3) << 3) + 0x81), (ushort)(((index & 4) << 1) + 0x21), 7, 7);
			}

			for (int index = 0; index < 9; index++)
			{
				SavePicture(0x6e96, index, 1, (ushort)(index << 3), 0x80, 8, 0x10);
			}

			for (int index = 0; index < 0x10; index++)
			{
				SavePicture(0xd4ee, index, 1, (ushort)((index << 4) + 1), 0x71, 15, 15);
			}

			for (int index = 0; index < 8; index++)
			{
				SavePicture(0xd50e, index, 1, (ushort)((index << 4) + 1), 0x61, 15, 15);
			}

			SavePicture(0xb880, 0, 1, 0x99, 0x29, 7, 7);
			SavePicture(0xdf0c, 0, 1, 0xb0, 0x80, 0x14, 9);

			for (int playerIndex = 0; playerIndex < 8; playerIndex++)
			{
				CutPlayerCitySprites(playerIndex);
			}
		}

		public void LoadGovernmentGraphics()
		{
			using var _ = this.Processor.Journal.Scope("ImportGovernmentGraphics()");

			this.Owner.Startup.AskForDisk(2, 1);

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].RegimeKind;
			char regimeCharacter = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char technologyCharacter = regime != 3 && this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 0x14) != 0 ? 'm' : 'a';
			string documentName = $"govt{regimeCharacter}{technologyCharacter}.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, documentName, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0xa0, 0x3c,
				Screen.PictureScreenBox, 0xa0, 0x8c);

			this.Owner.Startup.AskForDisk(0, 1);
		}

		public void ShowRevolutionScreen()
		{
			using var _ = this.Processor.Journal.Scope("DisplayRevolutionScreen()");

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].RegimeKind;

			if (regime != 0)
			{
				this.Owner.Advisors.DrawAdvisorScenery();

				Message(Tables.CivilizationName(this.Owner.World.PersonParticipantIndex))
					.Text(" government\nchanged to ");
				this.Owner.Words.Append(Tables.GovernmentName(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].RegimeKind));
				this.Owner.Words.Append("!\n");

				this.Owner.Paper.ShowNewspaper(-2);

				this.Owner.Startup.HideMouseCursor();

				this.Owner.Services.FloodBox(Screen.Box, 0, 100, 320, 100, Hues.White);

				DrawingBox.TypefaceIndex = 0x6;

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(GameTexts.TextNewCabinet, 160, 102, Hues.Black);

				DrawingBox.TypefaceIndex = 0x2;

				for (int advisorIndex = 0; advisorIndex < 4; advisorIndex++)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)((0x28 * advisorIndex) + 0xa0), 0x8c, 0x28, 0x3c,
						Screen.Box, (ushort)((0x50 * advisorIndex) + 0x14), 0x76);

					this.Owner.Words.Set(this.Processor.PullGlobalWord((ushort)((advisorIndex << 1) + AdvisorTitleTextTableAddress)));
					this.Owner.Words[this.Owner.Words.Length - 1] = 0x0;

					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), (80 * advisorIndex) + 40, (advisorIndex & 1) != 0 ? 186 : 180, Hues.Black);
				}

				DrawingBox.TypefaceIndex = 0x1;

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.Advisors.RecoverAdvisorScenery();
			}
		}

		public void ClearSpaceshipContent(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"WipeSpaceshipContent({participantIndex})");

			this.Owner.World.Participants[participantIndex].StarshipArrivalSeason = -1;

			this.Processor.SegmentExtra.Word = 0x3772;

			for (int row = 0; row < 0xf; row++)
			{
				for (int column = 0; column < 0xc; column++)
				{
					this.Processor.EmitByte(this.Processor.SegmentExtra.Word,
						(ushort)((0xb4 * participantIndex) + (0xc * row) + column + 0x16e0), 0xff);
				}
			}
		}

		/// <summary>
		/// The colours of every slot for the civilizations playing in them: the original ones, light red with white
		/// for Poland and Japan. A loaded game calls it, so a slot keeps no colour from a game played before.
		/// </summary>
		public void ApplyCivilizationColours()
		{
			ApplyColours(true);
		}

		public void CutPolishUnitSprites(int playerIndex)
		{
			if (!this.Owner.VideoCard.Displays.ContainsKey(1) || !this.Owner.VideoCard.Displays.ContainsKey(Screen.BackScreenBox))
			{
				return;
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPictureSecond, 0);
			CutPlayerCitySprites(playerIndex);
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileDiffsPicture, 1);
		}

		public void CutPlayerCitySprites(int playerIndex)
		{
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0xa0, 0x140, 0x20,
				Screen.BackScreenBox, 0, 0x80);

			ushort colours = (ushort)(playerIndex << 1);
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

			if (primaryColour == Hues.LightRed && secondaryColour == Hues.White)
			{
				for (int vertical = 0; vertical < 32; vertical++)
				{
					byte stripeHue = (byte)(((vertical / 2) % 2 == 1) ? secondaryColour : primaryColour);
					this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128 + vertical, 320, 1, Hues.LightGreen, stripeHue);
				}
			}
			else
			{
				this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.LightGreen,
					(byte)primaryColour);
			}

			this.Owner.VideoCard.SubstituteHue(Screen.BackScreenBox, 0, 128, 320, 32, Hues.Green,
				(byte)secondaryColour);

			for (int index = 0; index < 0x1c; index++)
			{
				SavePicture(0xd54e, (playerIndex << 5) + index, 1, (ushort)(((index % 0x14) << 4) + 1), (ushort)(((index / 0x14) << 4) + 0x81), 15, 15);
			}
		}

		private void ChooseDifficulty()
		{
			this.Owner.Words.Set("Difficulty Level...\n Chieftain (easiest)\n Warlord\n Prince\n King\n Emperor (toughest)\n");

			this.Owner.World.HardnessTier = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 35, 1);

			if (Mouse.Present != 0 && Menu.MouseUsed != 0)
			{
				this.Owner.Startup.ReadMouseState();

				if ((short)this.Owner.MenuPointerHorizontal < 0x87)
				{
					this.Owner.World.HardnessTier = (short)this.Owner.Helpers.ClampValue(((short)this.Owner.MenuPointerVertical - 12) / 35, 0, 4);
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

		private void DrawDifficultyPortrait(ushort horizontalLocation, ushort verticalLocation)
		{
			ushort column = (ushort)((this.Owner.World.HardnessTier & 1) != 0 ? 0x50 : 0x15);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, column,
				(ushort)((0x23 * this.Owner.World.HardnessTier) + 6), 0x35, 0x2f, Screen.Box, horizontalLocation, verticalLocation);
		}

		private void ChooseCompetition()
		{
			this.Owner.Words.Set("Level of Competition...\n ");

			for (short tally = 7; tally > 2; tally--)
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(tally, 10));

				this.Owner.Words.Append(" Civilizations\n ");
			}

			do
			{
				this.Owner.World.ComputerRivalTally = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 35, 1);
			}
			while ((ushort)this.Owner.World.ComputerRivalTally == 0xffff);

			this.Owner.World.ComputerRivalTally = (short)this.Owner.Helpers.ClampValue(6 - this.Owner.World.ComputerRivalTally, 2, 6);
			this.Owner.World.LiveRealms = (short)(0xff >> (byte)(6 - (byte)this.Owner.World.ComputerRivalTally));

			this.Owner.Startup.HideMouseCursor();

			for (short rivalIndex = this.Owner.World.ComputerRivalTally; rivalIndex >= 0; rivalIndex--)
			{
				DrawDifficultyPortrait((ushort)((rivalIndex << 1) + 0x14), (ushort)((3 * rivalIndex) + 0x64));
			}
		}

		private void ClearWorld()
		{
			RestoreOriginalCivilizations();

			this.Owner.World.Season = -4000;
			this.Owner.World.DiagonalMarkers = 0xf;
			this.Owner.World.Extras.WipeAll();

			for (int townIndex = 0; townIndex < 0x80; townIndex++)
			{
				this.Owner.World.Towns[townIndex].LabelIndex = 0xff;
				this.Owner.World.Towns[townIndex].ConditionMarker = 0xff;
			}

			for (int index = 0; index < 256; index++)
			{
				this.Owner.World.TownPlaces[index].X = -1;
			}

			for (int scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIndex] = 0;
			}

			for (int marvelIndex = 0; marvelIndex <= 0x15; marvelIndex++)
			{
				this.Owner.World.MarvelTownIndex[marvelIndex] = -1;
			}

			for (short participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				this.Owner.World.Participants[participantIndex].ChiefVisuals = -1;

				ClearSpaceshipContent(participantIndex);
			}

			for (int horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				for (int verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
				{
					this.Owner.World.ChartSight[horizontalLocation, verticalLocation] = 0;
				}
			}

			this.Owner.World.SessionPreferenceMarkers = 0xfa;

			if (this.Processor.PullGlobalByte(SoundDriverOptionAddress) == 0x4e)
			{
				this.Owner.World.SessionPreferenceMarkers &= 0x7fef;
			}
		}

		private void ChooseTribe()
		{
			this.Owner.Words.Set("Pick your tribe...\n ");

			_ = this.Owner.Words.Length;

			int rivalCount = this.Owner.World.ComputerRivalTally + 1;

			this.Owner.Words.Append("Poles\n ");
			this.Owner.Words.Append("Japanese\n ");

			for (int leader = 1; leader <= rivalCount; leader++)
			{
				this.Owner.Words.Append(GameTables.LeaderText(leader, LeaderTable.Civilization));
				this.Owner.Words.Append("\n ");
			}

			for (int leader = 1; leader <= rivalCount; leader++)
			{
				this.Owner.Words.Append((ushort)((0x3a * leader) + 0x16d2));
				this.Owner.Words.Append("\n ");
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				Screen.Box, 0x96, 0);

			this.Owner.Startup.ShowMouseCursor();

			short selection = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 160, 27, 1);
			bool isPoland = false;
			bool isJapan = false;

			if (selection == 0)
			{
				isPoland = true;
				this.Owner.World.PersonParticipantIndex = 1;
			}
			else if (selection == 1)
			{
				isJapan = true;
				this.Owner.World.PersonParticipantIndex = 1;
			}
			else if (selection >= 2 && selection < rivalCount + 2)
			{
				this.Owner.World.PersonParticipantIndex = (short)(selection - 1);
			}
			else if (selection >= rivalCount + 2 && selection < (2 * rivalCount) + 2)
			{
				int secondLeaderIndex = selection - (rivalCount + 2);
				this.Owner.World.PersonParticipantIndex = (short)(secondLeaderIndex + 9);
			}

			if (this.Owner.World.HardnessTier == 0)
			{
				this.Owner.World.SessionPreferenceMarkers |= 1;
			}

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex & 7].PresentInquiryIndex = -1;

			this.Owner.World.FollowingCollectionRound += (short)this.Owner.Runtime.Dice.DrawInteger(50);

			for (int index = 0; index < 0xc; index++)
			{
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (index << 1)), 0xffff);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionStylesAddress + (index << 1)), 0x0);
			}

			for (int index = 3; index <= 5; index++)
			{
				this.Processor.EmitGlobalWord((ushort)(0xd92a + (index << 1)), 0);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (index << 1)), 0);
			}

			bool randomTribe = !isPoland && !isJapan && this.Owner.World.PersonParticipantIndex == 0;
			bool secondLeader = false;

			if (randomTribe)
			{
				this.Owner.World.PersonParticipantIndex = (short)(this.Owner.Runtime.Dice.DrawInteger(this.Owner.World.ComputerRivalTally) + 1);
			}
			else if (!isPoland && !isJapan)
			{
				secondLeader = this.Owner.World.PersonParticipantIndex > 7;
				this.Owner.World.PersonParticipantIndex &= 7;
			}

			short personIndex = this.Owner.World.PersonParticipantIndex;
			ushort names = (ushort)(personIndex << 1);

			this.Processor.EmitGlobalByte(Tables.CivilizationNameAt(names), 0x0);

			if (randomTribe)
			{
				this.Owner.CityNaming.ShowCityFoundedMessage();

				this.Owner.World.Participants[personIndex].ChiefVisuals = personIndex;

				this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderText(personIndex, LeaderTable.Name));
			}
			else if (isPoland)
			{
				EstablishPoland(personIndex);
			}
			else if (isJapan)
			{
				EstablishJapan(personIndex, LeaderPortraits.CityBlock(personIndex, false) << 4, true);
			}
			else
			{
				Player person = this.Owner.World.Participants[personIndex];

				person.ChiefVisuals = (short)(secondLeader ? personIndex + 8 : personIndex);

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

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(Tables.CivilizationPluralAt(names), 46, 92, Hues.White);
		}

		/// <summary>
		/// Poland in a colour slot: Casimir, the Polish names, light red colours, Polish units, cities and the
		/// start on the Earth map. Poland takes slot 1 (the white Romans and Russians), for the human player who
		/// chooses it, and for the computer whenever slot 1 is not the human player's.
		/// </summary>
		private void EstablishPoland(short participantIndex)
		{
			EstablishPoland(participantIndex, LeaderPortraits.CityBlock(participantIndex, false) << 4, this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals != LeaderPortraits.JapaneseLeader);
		}

		/// <summary>
		/// Poland in a slot, naming its cities from <paramref name="firstLabel"/>; <paramref name="reserve"/> also
		/// writes the Polish names into the shared reserve of city names (only when a game starts).
		/// </summary>
		private void EstablishPoland(short participantIndex, int firstLabel, bool reserve)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			ushort names = (ushort)(participantIndex << 1);

			player.ChiefVisuals = LeaderPortraits.PolishLeader;

			this.Owner.Names.Set(Tables.LeaderNameAt(names), "Casimir Great");
			this.Owner.Names.Set(Tables.CivilizationNameAt(names), "Polish");
			this.Owner.Names.Set(Tables.CivilizationPluralAt(names), "Poles");

			SetColours(names, Hues.LightRed, Hues.White);

			this.Processor.EmitGlobalByte((ushort)(LeaderEarthStartPositionTableAddress + 32), 35);
			this.Processor.EmitGlobalByte((ushort)(LeaderEarthStartPositionTableAddress + 33), 14);

			this.Processor.EmitGlobalByte((ushort)(0x3938 + LeaderPortraits.PolishLeader), LeaderPortraits.Casimir);

			CutPolishUnitSprites(participantIndex);

			// Poland names its cities in the block of the slot's own civilization, which does not play; the shared
			// reserve stays Japanese when the human player plays Japan.
			ApplyPolishCityNames(firstLabel, reserve);
		}

		/// <summary>
		/// Japan in a slot: Tokugawa, the Japanese names, light red colours, units, cities and the start on the
		/// Earth map. The human player's Japan takes slot 1 and the first block of city names; the computer plays
		/// Japan only when its Poland is reborn (see <see cref="RebornLeader"/>).
		/// </summary>
		private void EstablishJapan(short participantIndex, int firstLabel, bool reserve)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			ushort names = (ushort)(participantIndex << 1);

			player.ChiefVisuals = LeaderPortraits.JapaneseLeader;

			this.Owner.Names.Set(Tables.LeaderNameAt(names), "Tokugawa");
			this.Owner.Names.Set(Tables.CivilizationNameAt(names), "Japanese");
			this.Owner.Names.Set(Tables.CivilizationPluralAt(names), "Japanese");

			SetColours(names, Hues.LightRed, Hues.White);

			this.Processor.EmitGlobalByte((ushort)(LeaderEarthStartPositionTableAddress + 34), 72);
			this.Processor.EmitGlobalByte((ushort)(LeaderEarthStartPositionTableAddress + 35), 17);

			this.Processor.EmitGlobalByte((ushort)(0x3938 + LeaderPortraits.JapaneseLeader), LeaderPortraits.Tokugawa);

			CutPolishUnitSprites(participantIndex);

			ApplyJapaneseCityNames(firstLabel, reserve);
		}

		private void ApplyPolishCityNames(int firstLabel, bool reserve)
		{
			string[] polishMainCities = new string[]
			{
				"Warsaw", "Krakow", "Gdansk", "Poznan",
				"Wroclaw", "Gniezno", "Szczecin", "Lublin",
				"Torun", "Lodz", "Katowice", "Plock",
				"Bialystok", "Czestochowa", "Bydgoszcz", "Zamosc"
			};

			for (int cityIndex = 0; cityIndex < polishMainCities.Length && cityIndex < 16; cityIndex++)
			{
				this.Owner.World.TownLabels[firstLabel + cityIndex] = polishMainCities[cityIndex].PadRight(12, ' ') + "\0";
			}

			if (!reserve)
			{
				return;
			}

			string[] polishReserveCities = new string[]
			{
				"Sandomierz", "Kielce", "Radom", "Rzeszow",
				"Opole", "Olsztyn", "Gdynia", "Kalisz",
				"Legnica", "Gliwice", "Elblag", "Koszalin",
				"Tarnow", "Przemysl", "Zielona Gora", "Wloclawek",
				"Rybnik", "Walbrzych", "Inowroclaw", "Piotrkow",
				"Slupsk", "Grudziadz", "Zabrze", "Bielsko-Biala",
				"Lomza", "Suwalki", "Ostroleka", "Chelm",
				"Zagan", "Malbork", "Sanok", "Wieliczka"
			};

			for (int cityIndex = 0; cityIndex < polishReserveCities.Length && (224 + cityIndex) < this.Owner.World.TownLabels.Length; cityIndex++)
			{
				this.Owner.World.TownLabels[224 + cityIndex] = polishReserveCities[cityIndex].PadRight(12, ' ') + "\0";
			}
		}

		private void ApplyJapaneseCityNames(int firstLabel, bool reserve)
		{
			string[] japaneseMainCities = new string[]
			{
				"Kyoto", "Tokyo", "Osaka", "Edo",
				"Kagoshima", "Nara", "Nagoya", "Yokohama",
				"Hiroshima", "Nagasaki", "Sapporo", "Sendai",
				"Fukuoka", "Kobe", "Kamakura", "Shimonoseki"
			};

			for (int cityIndex = 0; cityIndex < japaneseMainCities.Length && cityIndex < 16; cityIndex++)
			{
				this.Owner.World.TownLabels[firstLabel + cityIndex] = japaneseMainCities[cityIndex].PadRight(12, ' ') + "\0";
			}

			if (!reserve)
			{
				return;
			}

			string[] japaneseReserveCities = new string[]
			{
				"Kanazawa", "Niigata", "Shizuoka", "Okayama",
				"Kumamoto", "Matsuyama", "Hakodate", "Takamatsu",
				"Toyama", "Nagano", "Gifu", "Himeji",
				"Ube", "Chiba", "Aomori", "Akita",
				"Morioka", "Fukushima", "Yamagata", "Mito",
				"Utsunomiya", "Maebashi", "Kofu", "Tsu",
				"Otsu", "Wakayama", "Tottori", "Matsue",
				"Tokushima", "Kochi", "Miyazaki", "Naha"
			};

			for (int cityIndex = 0; cityIndex < japaneseReserveCities.Length && (224 + cityIndex) < this.Owner.World.TownLabels.Length; cityIndex++)
			{
				this.Owner.World.TownLabels[224 + cityIndex] = japaneseReserveCities[cityIndex].PadRight(12, ' ') + "\0";
			}
		}

		private void StartCivilizations()
		{
			this.polandSlot = ChoosePolandSlot();

			for (short participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				CreateCivilization(participantIndex);

				int cleared = participantIndex;

				for (int kind = 0; kind < 8; kind++)
				{
					this.Owner.World.Participants[cleared].TroopsRuined[kind] = 0;
				}
			}

			CharacterizeStartLocation();

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];

			this.Owner.World.ParticipantMarkers = (short)(1 << this.Owner.World.PersonParticipantIndex);
			person.LevyRatio = 5;
			person.ResearchLevyRatio = 5;

			if (this.Owner.World.HardnessTier == 0)
			{
				person.Gold = 50;
			}

			this.Owner.World.ParticipantIdentifierMarkers = 0;

			for (int participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				// Marks the slots that began with the second leader of their pair; of Poland and Japan, Poland.
				short leader = this.Owner.World.Participants[participantIndex].ChiefVisuals;

				if (leader >= 8 && leader != LeaderPortraits.JapaneseLeader)
				{
					this.Owner.World.ParticipantIdentifierMarkers |= (short)(1 << participantIndex);
				}
			}
		}

		private void WelcomeHumanPlayer()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 150, 0, 170, 200, Hues.Black);

			this.Owner.CityNaming.ShowFoundCityMessage();

			this.Owner.Startup.ShowMouseCursor();

			DrawingBox.TypefaceIndex = 0x1;

			ushort names = (ushort)(this.Owner.World.PersonParticipantIndex << 1);

			this.Owner.Codes.Set(3, Tables.LeaderNameAt(names));

			this.Owner.Codes.Set(0, Tables.CivilizationPluralAt(names));

			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicInitialization);

			_ = this.Owner.Words.Length;

			int shown = 0;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, scienceIndex) == 0)
				{
					continue;
				}

				this.Owner.Words.Append(GameTables.TechnologyText(scienceIndex));

				this.Owner.Words.Append(", ");

				shown++;

				if (shown == 2)
				{
					this.Owner.Words.Append("\n");
				}
			}

			this.Owner.Words.Append("and Roads.\n");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals);

			this.Owner.Services.PerformSound(Tables.LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

			this.Owner.UnitOperations.DrawBusyIndicator();

			DrawDifficultyPortrait(0x86, 0x14);

			this.Owner.BlockText.DrawTextChunk(99, 88, 81, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		private ushort RemakeCivilization(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			player.StarshipInhabitants = 0;
			player.RunningSagaStanding = 0;
			player.FoundScienceTally = 0;
			player.InquiryHeadway = 0;
			player.Gold = 0;
			player.RegimeKind = 1;
			player.LevyRatio = 4;
			player.ResearchLevyRatio = 4;

			ushort[] foreignTowns = new ushort[16];

			for (int continentIndex = 0; continentIndex < 16; continentIndex++)
			{
				player.Landmasses[continentIndex].Approach = 0;

				for (int otherIndex = 1; otherIndex < 8; otherIndex++)
				{
					foreignTowns[continentIndex] += (ushort)this.Owner.World.Participants[otherIndex].Landmasses[continentIndex].TownTally;
				}
			}

			player.TownTally = 0;
			player.TroopTally = 0;
			player.SoilTally = 1;
			player.ColonistTally = 1;
			player.TouchParticipantTimeLeft = -1;

			for (int index = 0; index < player.ScienceObtainedSince.Length; index++)
			{
				player.ScienceObtainedSince[index] = -1;
			}

			for (int troopIndex = 0; troopIndex < 0x80; troopIndex++)
			{
				ushort troop = (ushort)((0x600 * participantIndex) + (0xc * troopIndex));

				this.Troops.AssignAt(troop, UnitMemory.Kind, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.Status, 0x0);
			}

			this.Processor.EmitGlobalWord((ushort)(ParticipantCountersAddress + (participantIndex << 1)), 0);
			player.Tally = 0;

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Services.FloodBox(Screen.PictureScreenBox, 240, 0, 80, 50, Hues.Black);
			}

			this.Owner.Computer.ClearStrategicLocations(participantIndex);

			for (int otherIndex = 0; otherIndex < 8; otherIndex++)
			{
				player.Relations[otherIndex] = 0;
				this.Owner.World.Participants[otherIndex].Relations[participantIndex] = 0;
			}

			for (int index = 0; index < 5; index++)
			{
				player.FoundScienceMarkers[index] = 0;
			}

			if (this.Owner.World.RoundTally != 0)
			{
				ShareKnownTechnologies(participantIndex);
			}

			if (participantIndex != 0 && (this.Owner.World.LiveRealms & (1 << participantIndex)) != 0)
			{
				if (!SettleCivilization(participantIndex, foreignTowns))
				{
					return 0;
				}

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

		private void ShareKnownTechnologies(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIndex, scienceIndex) == 0)
				{
					continue;
				}

				int finder = (byte)this.Owner.World.ScienceLeadingFoundVia[scienceIndex] & 7;

				if (finder == 0 || finder == (ushort)personIndex)
				{
					continue;
				}

				if (this.Owner.Runtime.Dice.DrawInteger(2) == 0)
				{
					this.Owner.Advisors.EarnTechnology(participantIndex, scienceIndex, participantIndex);
				}
			}
		}

		private bool SettleCivilization(short participantIndex, ushort[] foreignTowns)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				if (this.Owner.World.RoundTally == 0 && participantIndex == this.polandSlot)
				{
					// Promoting Poland: the computer always plays it (see ChoosePolandSlot).
					EstablishPoland(participantIndex);
				}
				else if (this.Owner.World.RoundTally == 0)
				{
					ChooseFreeLeader(participantIndex);
				}
				else
				{
					// A civilization reborn in a slot takes the other leader of the pair.
					player.ChiefVisuals = RebornLeader(player.ChiefVisuals);
				}
			}

			(short horizontalLocation, short verticalLocation, ushort attemptTally) = ChooseStartPlace(participantIndex, foreignTowns);

			if (this.Owner.World.RoundTally == 0 && NewWorld.EarthGame != 0)
			{
				ushort place = (ushort)(player.ChiefVisuals << 1);

				horizontalLocation = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress));
				verticalLocation = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress + 1));
				attemptTally = 0;

				if (EuropeMap.Chosen(NewWorld.EarthGame))
				{
					(horizontalLocation, verticalLocation) = EuropeMap.Start(player.ChiefVisuals);
				}
			}

			if (this.Owner.World.RoundTally != 0)
			{
				bool identity = (this.Owner.World.ParticipantIdentifierMarkers & (1 << participantIndex)) != 0;

				if (player.ChiefVisuals >= LeaderPortraits.PolishLeader)
				{
					// The identity marks a slot that began with Poland: it is reborn once, as Japan (and a slot that
					// began with Japan once as Poland), and not while another slot plays that civilization.
					if ((player.ChiefVisuals == LeaderPortraits.PolishLeader) == identity || AnotherSlotPlays(participantIndex, player.ChiefVisuals))
					{
						attemptTally = 0x7d0;
					}
				}
				else
				{
					if (player.ChiefVisuals >= 8 && identity)
					{
						attemptTally = 0x7d0;
					}

					if (player.ChiefVisuals < 8 && !identity)
					{
						attemptTally = 0x7d0;
					}
				}
			}

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				player.ResearchLevyRatio = (short)(Tables.LeaderWord(player.ChiefVisuals, LeaderTable.Leaning) + 3);
				player.LevyRatio = (short)(9 - player.ResearchLevyRatio);
			}

			ushort latestRound = (ushort)(0x15e - (0x32 * this.Owner.World.HardnessTier));

			if ((latestRound < this.Owner.World.RoundTally || attemptTally >= 0x7d0) &&
				participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				player.ChiefVisuals = RebornLeader(player.ChiefVisuals);
				this.Owner.World.LiveRealms &= (short)~(1 << participantIndex);

				if ((ushort)((1 << this.Owner.World.PersonParticipantIndex) | 1) == (ushort)this.Owner.World.LiveRealms)
				{
					Turn.GameEndRequest = 1;
					Turn.ConquestEnd = 1;
				}

				return false;
			}

			if (this.Owner.World.RoundTally != 0 && participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				// Reborn as Poland or Japan: its names and its own block of city names (the slot's second civilization,
				// which never plays beside them); the names of existing cities stay as they are.
				if (player.ChiefVisuals == LeaderPortraits.PolishLeader)
				{
					EstablishPoland(participantIndex, LeaderPortraits.CityBlock(participantIndex, true) << 4, false);
				}
				else if (player.ChiefVisuals == LeaderPortraits.JapaneseLeader)
				{
					EstablishJapan(participantIndex, LeaderPortraits.CityBlock(participantIndex, true) << 4, false);
				}
			}

			NameCivilization(participantIndex);

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex, horizontalLocation, verticalLocation);

			this.Owner.UnitOperations.CreateUnit(participantIndex, 0, horizontalLocation, verticalLocation);

			player.HorizontalBegin = horizontalLocation;

			for (short troopIndex = 0; troopIndex < 0x80; troopIndex++)
			{
				ushort troop = (ushort)(0xc * troopIndex);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal),
					verticalLocation, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

				if (distance <= 8)
				{
					this.Owner.UnitOperations.DestroyUnit(0, troopIndex);
				}
			}

			return true;
		}

		/// <summary>
		/// The civilizations as the original game has them: the colours of every slot and all city names. A game
		/// with Poland or Japan changes them, so every new game starts from the original and sets Poland or Japan
		/// again only when it plays.
		/// </summary>
		private void RestoreOriginalCivilizations()
		{
			string[] original = new GameState().TownLabels;

			for (int cityIndex = 0; cityIndex < original.Length && cityIndex < this.Owner.World.TownLabels.Length; cityIndex++)
			{
				this.Owner.World.TownLabels[cityIndex] = original[cityIndex];
			}

			ApplyColours(false);
		}

		private void ApplyColours(bool withAddedCivilizations)
		{
			for (int slot = 1; slot < OriginalColours.Length; slot++)
			{
				(byte primary, byte secondary) = OriginalColours[slot];

				if (withAddedCivilizations && this.Owner.World.Participants[slot].ChiefVisuals >= LeaderPortraits.PolishLeader)
				{
					(primary, secondary) = (Hues.LightRed, Hues.White);
				}

				ushort names = (ushort)(slot << 1);

				if (Tables.PlayerColourAt(names) == primary && Tables.PlayerSecondaryColourAt(names) == secondary)
				{
					continue;
				}

				SetColours(names, primary, secondary);

				// The units and cities of the slot are drawn in its colours: cut them again.
				CutPolishUnitSprites(slot);
			}
		}

		private void SetColours(ushort names, byte primary, byte secondary)
		{
			this.Owner.Tables.Content[0x1946 - GameTables.Begin + names] = primary;
			this.Owner.Tables.Content[0x1946 - GameTables.Begin + names + 1] = 0;
			this.Owner.Tables.Content[0x1956 - GameTables.Begin + names] = secondary;
			this.Owner.Tables.Content[0x1956 - GameTables.Begin + names + 1] = 0;

			this.Processor.EmitGlobalWord((ushort)(0x1946 + names), primary);
			this.Processor.EmitGlobalWord((ushort)(0x1956 + names), secondary);
		}

		/// <summary>
		/// Promoting Poland: the computer plays it in every game the human player does not play it. It takes slot 1
		/// (the white Romans and Russians); when the human player plays in slot 1 (Romans, Russians or Japan), it
		/// takes another slot of the game instead, chosen at random, in place of that slot's civilization.
		/// </summary>
		private short ChoosePolandSlot()
		{
			short person = this.Owner.World.PersonParticipantIndex;

			if (this.Owner.World.Participants[person].ChiefVisuals == LeaderPortraits.PolishLeader)
			{
				return -1;
			}

			if (person != 1)
			{
				return 1;
			}

			// The slots of the game are 1 .. ComputerRivalTally + 1; the human player has slot 1.
			int lastSlot = this.Owner.World.ComputerRivalTally + 1;

			return lastSlot < 2 ? (short)-1 : (short)(this.Owner.Runtime.Dice.DrawInteger(lastSlot - 1) + 2);
		}

		/// <summary>The other leader of a slot's pair: the original pairs differ by 8, Poland and Japan pair up.</summary>
		private short RebornLeader(short leader)
		{
			return leader switch
			{
				LeaderPortraits.PolishLeader => LeaderPortraits.JapaneseLeader,
				LeaderPortraits.JapaneseLeader => LeaderPortraits.PolishLeader,
				_ => (short)(leader ^ 8),
			};
		}

		private bool AnotherSlotPlays(short participantIndex, short leader)
		{
			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex != participantIndex && this.Owner.World.Participants[otherIndex].ChiefVisuals == leader &&
					((this.Owner.World.LiveRealms & (1 << otherIndex)) != 0 || otherIndex == this.Owner.World.PersonParticipantIndex))
				{
					return true;
				}
			}

			return false;
		}

		private void ChooseFreeLeader(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			bool free;

			do
			{
				player.ChiefVisuals = (short)this.Owner.Runtime.Dice.DrawInteger(16);

				free = (player.ChiefVisuals & 7) == participantIndex;

				for (int otherIndex = 0; otherIndex < 8; otherIndex++)
				{
					if (otherIndex != participantIndex && this.Owner.World.Participants[otherIndex].ChiefVisuals == player.ChiefVisuals)
					{
						free = false;
					}
				}
			}
			while (!free);
		}

		private (short HorizontalLocation, short VerticalLocation, ushort AttemptTally) ChooseStartPlace(short participantIndex, ushort[] foreignTowns)
		{
			ushort attemptTally = 0;

			while (true)
			{
				short horizontalLocation = (short)(this.Owner.Runtime.Dice.DrawInteger(64) + 8);
				short verticalLocation = (short)(this.Owner.Runtime.Dice.DrawInteger(34) + 8);

				ushort townIndex = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);
				short nearest = townIndex == 0xffff ? (short)0x1e : (short)this.Owner.Helpers.ObtainDistance(horizontalLocation, this.Owner.World.Towns[townIndex].Place.X, verticalLocation, this.Owner.World.Towns[townIndex].Place.Y);

				if (this.Owner.World.RoundTally == 0)
				{
					for (int otherIndex = 1; otherIndex < participantIndex; otherIndex++)
					{
						ushort troop = (ushort)(0x600 * otherIndex);

						short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal),
							verticalLocation, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

						if (distance < nearest)
						{
							nearest = distance;
						}
					}
				}

				short landValue = (short)this.Owner.VideoCard.ObtainDot(2, horizontalLocation + 0x50, verticalLocation);

				attemptTally++;

				if ((short)attemptTally > 0x7d0 &&
					(participantIndex != this.Owner.World.PersonParticipantIndex || this.Owner.World.RoundTally != 0))
				{
					return (horizontalLocation, verticalLocation, attemptTally);
				}

				if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
				{
					continue;
				}

				if ((short)(0xc - ((short)attemptTally / 32)) > landValue)
				{
					continue;
				}

				short relief = (short)((short)attemptTally / 64);

				if (nearest < (short)(0xa - relief))
				{
					continue;
				}

				ushort continentIndex = this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation);
				short siteDemand = (short)(0x20 - (this.Owner.World.RoundTally / 16) - relief);

				if (this.Owner.World.Landmasses[continentIndex].ConstructionSpotTally < siteDemand)
				{
					continue;
				}

				if (this.Owner.World.Season > 0 && foreignTowns[this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation)] != 0)
				{
					continue;
				}

				if (this.Owner.MapOperations.ContainsHut(this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation), horizontalLocation, verticalLocation) != 0)
				{
					continue;
				}

				return (horizontalLocation, verticalLocation, attemptTally);
			}
		}

		private void NameCivilization(short participantIndex)
		{
			// Poland and Japan are named when they are established; the leader table has no row for them.
			if (this.Owner.World.Participants[participantIndex].ChiefVisuals >= LeaderPortraits.PolishLeader)
			{
				return;
			}

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIndex].ChiefVisuals);
			ushort names = (ushort)(participantIndex << 1);

			this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Name));

			if (participantIndex == this.Owner.World.PersonParticipantIndex &&
				this.Processor.PullGlobalByte(Tables.CivilizationNameAt(names)) != 0)
			{
				return;
			}

			this.Owner.Names.Set(Tables.CivilizationNameAt(names), GameTables.LeaderTextAt(leader, LeaderTable.Civilization));

			this.Owner.Words.Set(GameTables.LeaderTextAt(leader, LeaderTable.CivilizationPlural));

			if (this.Owner.Words[0] == 0)
			{
				Message(Tables.CivilizationNameAt(names))
					.Text("s");
			}

			this.Owner.Names.Set(Tables.CivilizationPluralAt(names), this.Owner.Words.ToString());
		}

		private bool OwnsCity(short participantIndex)
		{
			for (int townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker != 0xff && this.Owner.World.Towns[townIndex].ParticipantIndex == participantIndex)
				{
					return true;
				}
			}

			return false;
		}

		private short NearestCivilization(ushort participantIndex, ushort troop)
		{
			short nearest = 0x3e7;
			ushort otherIndex;

			for (otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex == participantIndex)
				{
					continue;
				}

				ushort other = (ushort)(0x600 * otherIndex);

				if (this.Troops.PullAt(other, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				ushort otherContinent = this.Owner.MapOperations.ObtainContinentIdentifier((sbyte)this.Troops.PullAt(other, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(other, UnitMemory.Vertical));

				if (this.Owner.MapOperations.ObtainContinentIdentifier((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical)) != otherContinent)
				{
					continue;
				}

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(other, UnitMemory.Horizontal),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical), (sbyte)this.Troops.PullAt(other, UnitMemory.Vertical));

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
			ushort neighbourIndex;

			for (neighbourIndex = 0; neighbourIndex <= 8; neighbourIndex++)
			{
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal) + (short)Tables.DirectionHorizontal(neighbourIndex),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical) + (short)Tables.DirectionVertical(neighbourIndex));

				if (terrain == 0xa)
				{
					ocean++;
				}

				if (terrain == 2)
				{
					grassland++;
				}

				if (terrain == 0xb)
				{
					river++;
				}
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

			switch (CentralProcessor.SignedQuotient((short)this.Owner.MapOperations.ObtainLandMassSize((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical)), 0x32))
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

		private void SavePicture(ushort tableAddress, int index, ushort displayIndex, ushort horizontalLocation, ushort verticalLocation, ushort breadth, ushort altitude)
		{
			ushort picture = this.Owner.VideoCard.ScreenToPicture(displayIndex, horizontalLocation, verticalLocation, breadth, altitude);

			this.Processor.EmitGlobalWord((ushort)(tableAddress + (index << 1)), picture);
		}

		private void CutVideoGraphicsUnitSprites()
		{
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileTerrainPicture, 0);

			for (int row = 0; row < 0xa; row++)
			{
				for (int column = 0; column < 0x10; column++)
				{
					ushort entry = (ushort)(VideoGraphicsUnitSpritesTableAddress + (row << 5) + (column << 1));

					if ((row >= 6 || Screen.LowMemoryVideoGraphics != 0) && column != 0)
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
					ushort horizontalLocation = quarter < 2
						? (ushort)(((quarter & 1) << 3) + (tile << 4))
						: (ushort)(((tile << 4) - ((quarter & 1) << 3)) + 8);
					ushort verticalLocation = (ushort)(quarter < 2 ? 0xb0 : 0xb8);

					SavePicture((ushort)(0xd294 + (tile << 3)), quarter, 1, horizontalLocation, verticalLocation, 8, 8);
				}
			}

			for (int index = 0; index < 4; index++)
			{
				SavePicture(0xd2d4, index, 1, (ushort)((index << 4) + 0x80), 0xb0, 0x10, 0x10);
			}
		}

		private void CutEnhancedGraphicsUnitSprites()
		{
			this.Owner.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPictureSecond, 1);

			this.Owner.VideoCard.RenderImage(Screen.SpriteScreenBox, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			for (int index = 0; index < 0xc; index++)
			{
				SavePicture(0xb886, index << 4, 3, (ushort)(index << 4), 0, 0x10, 0x10);
				SavePicture(0xb888, index << 4, 3, (ushort)(index << 4), 0x10, 0x10, 0x10);
			}
		}
	}
}
