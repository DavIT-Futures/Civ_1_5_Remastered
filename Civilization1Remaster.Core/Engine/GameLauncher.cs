namespace Civilization1Remaster.Core.Engine
{
	public class GameLauncher : GameComponent, IGameLauncher
	{
		public const ushort VideoDriverOptionAddress = 0x1a22;
		public const ushort SoundDriverOptionAddress = 0x1a30;
		public const ushort DiskNumberPromptAddress = 0x1ade;

		public GameLauncher(GameEngine owner)
			: base(owner)
		{
		}

		public void RunGame()
		{
			using var _ = this.Processor.Journal.Scope("RunGame()");

			this.Processor.EmitGlobalByte(VideoDriverOptionAddress, 0x4d);
			this.Processor.EmitGlobalByte(SoundDriverOptionAddress, 0x4e);
			Mouse.Present = 0x1;

			this.Owner.Prologue.ShowMainIntro();

			AskForDisk(0, 1);

			short freeStorage = (short)this.Owner.VideoCard.ObtainFreeStorage();

			if (Screen.ReserveFailed != 0)
			{
				freeStorage = 0;
			}

			short requiredStorage = (short)(Screen.VgaMode != 0 ? 0x1f40 : 0xfa0);

			if (requiredStorage > freeStorage)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.LightRed);

				EmptyMessage()
					.Number(freeStorage);
				this.Owner.Codes.Set(3, this.Owner.Words.ToString());
				this.Owner.Words.Clear();

				this.Owner.BlockText.ShowTextDialog(GameTexts.TopicLomem);
				this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 64, 49, 1);
			}

			if (Screen.VgaMode != 0 && freeStorage < 0x23be)
				Screen.LowMemoryVga = 1;

			Mouse.Icon = (ushort)(this.Owner.ImageToolkit.LoadIcon(0x277));

			if (Mouse.Present != 0)
			{
				Mouse.Present = this.Owner.Services.PrepareMouse();
			}

			if (Mouse.Present != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Icon);
				ShowMouseCursor();
			}

			ShowTitleAndMainMenu();

			ChooseNewWorldOptions();

			if (Menu.MainMenuChoice == 1)
			{
				MapView.Horiz = (ushort)(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].HorizBegin - 7);
				MapView.Vert = 0x13;

				this.Owner.TurnHooks.RepaintMainScreen();
			}

			ushort arrowTier = Mouse.ShowLevel;

			while (true)
			{
				Turn.GameEndRequest = 0;

				do
				{
					this.Owner.TurnHooks.HandleGameEnd();
				}
				while (Turn.GameEndRequest == 0);

				if (Turn.GameEndRequest != SaveGameStore.FetchSessionAsk)
					break;

				LoadGameDuringPlay(arrowTier);
			}

			if (Mouse.Present != 0)
				this.Owner.Services.DeactivateMouse();

			this.Owner.Services.PerformSound(0, 0);
			this.Owner.Services.HaltSound();
			this.Owner.Services.HaltTimer();
		}

		public void LoadGameDuringPlay(ushort arrowTier)
		{
			using var _ = this.Processor.Journal.Scope($"ImportGameDuringPlay({arrowTier})");

			Turn.GameEndRequest = 0x0;
			Turn.ChosenOrder = 0x0;

			while ((short)Mouse.ShowLevel > (short)arrowTier)
				HideMouseCursor();

			while ((short)Mouse.ShowLevel < (short)arrowTier)
				ShowMouseCursor();

			ushort bay = this.Owner.SaveLoad.FetchBay;
			this.Owner.SaveLoad.FetchBay = 0xffff;
			this.Owner.SaveLoad.LoadGameFromSlot(bay);

			HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.LightGray);

			this.Owner.NewGameWizard.ShowTribeSelection();

			Screen.BoxMotif = 0x1;

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSp257Pic, 1);

			this.Owner.Helpers.SetupHueCycling();

			ShowMouseCursor();

			ChooseNewWorldOptions();

			Menu.MainMenuChoice = 0x1;
			MapView.Horiz = (ushort)(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].HorizBegin - 7);
			MapView.Vert = 0x13;

			this.Owner.TurnHooks.RepaintMainScreen();
		}

		public void ReadMouseState()
		{
			this.Processor.PumpMessages();

			if (Mouse.Present != 0)
			{
				ushort clicks = this.Owner.Services.TakeMouseClick();

				this.Owner.GlobDb3a = (ushort)(clicks | this.Owner.Pointer.Buttons);
				this.Owner.GlobDb3c = this.Owner.Pointer.Horiz;
				this.Owner.GlobDb3e = this.Owner.Pointer.Vert;
			}
			else
			{
				this.Owner.GlobDb3e = 0;
				this.Owner.GlobDb3c = 0;
				this.Owner.GlobDb3a = 0;
			}
		}

		public void ShowMouseCursor()
		{
			using var _ = this.Processor.Journal.Scope("DisplayMouseCursor()");

			Mouse.ShowLevel = (ushort)(Mouse.ShowLevel + 1);

			if (Mouse.Present != 0 && Mouse.ShowLevel == 1)
			{
				this.Owner.Services.DrawMouseCursor();
			}
		}

		public void HideMouseCursor()
		{
			using var _ = this.Processor.Journal.Scope("ConcealMouseCursor()");

			if (Mouse.Present != 0 && Mouse.ShowLevel == 1)
			{
				this.Owner.Services.BlankMouseCursor();
			}
		
			Mouse.ShowLevel = (ushort)(Mouse.ShowLevel - 1);
		}

		public void ShowMouseCursorFully()
		{
			using var _ = this.Processor.Journal.Scope("DisplayMouseCursorFully()");

			Mouse.SavedShowLevel = Mouse.ShowLevel;

			while ((short)Mouse.ShowLevel < 1)
			{
				ShowMouseCursor();
			}
		}

		public void HideMouseCursorToSavedLevel()
		{
			using var _ = this.Processor.Journal.Scope("ConcealMouseCursorToSavedLevel()");

			while ((short)Mouse.ShowLevel > (short)Mouse.SavedShowLevel)
			{
				HideMouseCursor();
			}
		}

		public ushort AskForDisk(ushort diskIdx, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"QueryForDisk({diskIdx}, {marker})");

			ushort result;

			if (marker == 0 && this.Owner.Services.PathPresent(this.Processor.PullGlobalWord((ushort)((diskIdx << 1) + 0x1ace))) != 0xffff)
			{
				result = 1;
			}
			else
			{
				if (marker == 0)
				{
					diskIdx = 0;
				}

				while (this.Owner.Services.PathPresent(this.Processor.PullGlobalWord((ushort)((diskIdx << 1) + 0x1ace))) == 0xffff ||
					marker == 0xffff)
				{
					this.Owner.Services.ShowScreen(DrawingBox.DisplayIdx);

					if (marker == 0xffff)
					{
						marker = 1;
					}

					HideMouseCursor();

					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

					this.Processor.EmitGlobalByte(DiskNumberPromptAddress,
						(byte)(CPU.SignedQuotient((short)diskIdx, (short)Files.FirstMissingDisk) + 0x31));

					string message = "Please insert Disk " + "A" + ".\n \n    Press Return\n    to continue.\n";

					ushort kept = King.Mood;
					King.Mood = 0xffff;
					Menu.AnyKey = 0x1;

					this.Owner.Dialogs.ShowMenu(message, 100, 81, 1);

					King.Mood = kept;

					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

					Screen.RepaintRequest = 0x1;

					ShowMouseCursor();
				}

				result = marker;
			}

			Files.DiskInUse = diskIdx;

			return result;
		}

		public void ShowTitleAndMainMenu()
		{
			using var _ = this.Processor.Journal.Scope("TitleLogoAndMainChoices()");

			NewWorld.EarthGame = 0x0;

			bool chosen = false;

			while (!chosen)
			{
				while (Menu.MainMenuChoice == 0xffff)
				{
					this.Owner.Words.Set(" Start a New Game\n Load a Saved Game\n EARTH\n Customize World\n View Hall of Fame\n");

					Menu.MainMenuChoice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 100, 140, 1);
				}

				WaitForTitleSongMeasure();

				this.Owner.Services.PerformSound(1, 0);

				switch (Menu.MainMenuChoice)
				{
					case 0:
						HideMouseCursor();

						for (int optionIdx = 0; optionIdx < 4; optionIdx++)
						{
							this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(0x7ef6 + (optionIdx << 1)), 0x1);
						}

						MakeNewWorld();
						chosen = true;
						break;

					case 1:
						if (this.Owner.SaveLoad.LoadGameFromMenu(0xffff) == 0xffff)
						{
							Menu.MainMenuChoice = 0xffff;
							break;
						}

						HideMouseCursor();

						this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.LightGray);
						chosen = true;
						break;

					case 2:
						HideMouseCursor();

						NewWorld.EarthGame = 0x1;

						this.Owner.WorldSetup.OpeningSequence();

						ShowMadeWorld(0x1c02);
						chosen = true;
						break;

					case 3:
						CustomizeWorld();

						MakeNewWorld();
						chosen = true;
						break;

					case 4:
						HideMouseCursor();

						this.Owner.Legends.LoadHallOfFame();

						this.Owner.Legends.ShowHallOfFame(0xffff);

						ShowMouseCursor();

						Menu.MainMenuChoice = 0xffff;
						break;

					default:
						chosen = true;
						break;
				}
			}

			this.Owner.NewGameWizard.ShowTribeSelection();

			Screen.BoxMotif = 0x1;

			if (Screen.VgaMode != 0 && (Menu.MainMenuChoice == 0 ||
				Menu.MainMenuChoice == 2 || Menu.MainMenuChoice == 3))
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSp257Pic, 1);

			this.Owner.Helpers.SetupHueCycling();

			ShowMouseCursor();
		}

		public void ChooseNewWorldOptions()
		{
			using var _ = this.Processor.Journal.Scope("FreshWorldChoices()");

			if (Mouse.Present != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);
			}

			if (this.Owner.World.RoundTally == 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileDiffsPic, 1);

				AskForDisk(0, 1);

				this.Owner.NewGameWizard.ConfigureFreshGame();
			}

			this.Owner.NewGameWizard.LoadGovernmentGraphics();

			AskForDisk(0, 1);
		}

		private void WaitForTitleSongMeasure()
		{
			short position;

			do
			{
				position = (short)this.Owner.Services.ReadSongPosition();
			}
			while (position > 0x1495 && (short)(position - 0x1495) % 0x120 != 0);
		}

		private void MakeNewWorld()
		{
			this.Processor.Journal = this.Owner.PrologueJournal;
			using (this.Processor.Journal.Scope("'Intro start'"))
			using (this.Owner.VideoJournal.Scope("'Intro start'"))
			using (this.Owner.IrqJournal.Scope("'Intro start'"))
			{
				this.Owner.WorldSetup.OpeningSequence();
			}

			this.Processor.Journal = this.Owner.Journal;

			ShowMadeWorld(0x1b33);
		}

		private void ShowMadeWorld(ushort pictureName)
		{
			DrawingBox.DisplayIdx = 0x0;

			this.Owner.Services.ShowScreen(0);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, pictureName, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
				Screen.PictureScreenBox, 0xa0, 0x32);

			AskForDisk(0, 1);
		}

		private void CustomizeWorld()
		{
			HideMouseCursor();

			AskForDisk(7, 1);

			if (Screen.VgaMode != 0)
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileCustomPic, 1);

			AskForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			ShowMouseCursor();

			ChooseWorldOption(0x7ef6, 1, "LAND MASS:\n Small\n Normal\n Large\n", 0x1b6a);
			ChooseWorldOption(0x7ef8, 51, "TEMPERATURE:\n Cool\n Temperate\n Warm\n", 0x1b94);
			ChooseWorldOption(0x7efa, 101, "CLIMATE:\n Arid\n Normal\n Wet\n", 0x1bbd);
			ChooseWorldOption(0x7efc, 151, "AGE:\n 3 billion years\n 4 billion years\n 5 billion years\n", 0x1bfe);

			HideMouseCursor();
		}

		private void ChooseWorldOption(ushort optionAddr, short menuVert, string choices, ushort helpTopic)
		{
			Menu.DefaultLine = 0x1;

			while (true)
			{
				this.Owner.Services.AssignMousePosition(210, (ushort)(menuVert + 10));

				this.Owner.Words.Set(choices);

				this.Processor.EmitGlobalWord(optionAddr, this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 200, menuVert, 1));

				if (this.Processor.PullGlobalWord(optionAddr) == 0xffff)
					continue;

				if (Menu.HelpTopic == 0)
					break;

				this.Owner.HelpSystem.ShowHelpTopic(helpTopic);

				Menu.DefaultLine = this.Processor.PullGlobalWord(optionAddr);
			}
		}
	}
}
