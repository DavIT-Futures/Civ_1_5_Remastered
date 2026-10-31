using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x11a8.
	/// </summary>
	public class GameStartup : GameModule
	{
		public const ushort VideoDriverOptionAddress = 0x1a22;
		public const ushort SoundDriverOptionAddress = 0x1a30;
		public const ushort DiskNumberPromptAddress = 0x1ade;

		/// <summary>The words of the frame of TitleLogoAndMainChoices, shared by its helpers (B3: in C#, new for every call)</summary>

		public GameStartup(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Main game entry: the intro, the check of the free memory (a warning below 8000 paragraphs with VGA, 4000 without;
		/// 0x1a3e set below 9150 with VGA), the mouse cursor, the main menu and the new world, then the turns of the game
		/// until it ends (a game loaded during play starts the turns again)
		/// </summary>
		public void RunGame()
		{
			this.Processor.Journal.OpenScope("RunGame()");


			// Main menu selection
			// '1' - VGA; first letter of driver
			this.Processor.EmitGlobalByte(VideoDriverOptionAddress, 0x4d);
			// '1' - No sound 0x4e, '4' - Sound blaster 0x41, '5' - Roland MIDI board 0x52; first letter of driver
			this.Processor.EmitGlobalByte(SoundDriverOptionAddress, 0x4e);
			// '1' - Mouse and Keyboard 0x1, '2' - Keyboard only 0x0
			MousePresent = 0x1;

			this.Owner.Prologue.DisplayMainIntro();

			QueryForDisk(0, 1);

			// The free memory (the local 4 of the original, B3: in C#)
			short freeStorage = (short)this.Owner.VideoCard.ObtainFreeStorage();

			if (ScreenReserveFailed != 0)
			{
				// A screen could not be reserved
				freeStorage = 0;
			}

			short requiredStorage = (short)(VgaMode != 0 ? 0x1f40 : 0xfa0);

			if (requiredStorage > freeStorage)
			{
				// Not enough memory: the amount in the text 0x270, then a menu with it
				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.LightRed);

				EmptyMessage()
					.Number(freeStorage);
				this.Owner.Codes.Set(3, this.Owner.Words.ToString());
				this.Owner.Words.Clear();

				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicLomem);
				this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 64, 49, 1);
			}

			if (VgaMode != 0 && freeStorage < 0x23be)
				LowMemoryVga = 1;

			PointerIcon = (ushort)(this.Owner.ImageToolkit.ImportIcon(0x277));

			if (MousePresent != 0)
			{
				MousePresent = this.Owner.Services.PrepareMouse();
			}

			if (MousePresent != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, PointerIcon);
				DisplayMouseCursor();
			}

			// Game type, load, etc. menu
			// And then after menu Intro
			TitleLogoAndMainChoices();

			// Start Game menu, level, tribe, name
			FreshWorldChoices();

			if (MainMenuChoice == 1)
			{
				// A loaded game: the map view around the first city of the human player
				MapViewHoriz = (ushort)(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].HorizBegin - 7);
				MapViewVert = 0x13;

				this.Owner.TurnHooks.RepaintMainScreen();
			}

			// Mouse pointer show/hide level at the start of the game, a game loaded during play starts with the same level
			ushort arrowTier = PointerShowLevel;

			while (true)
			{
				GameEndRequest = 0;

				do
				{
					this.Owner.TurnHooks.HandleGameEnd();
				}
				while (GameEndRequest == 0);

				if (GameEndRequest != GameLoadAndSave.FetchSessionAsk)
					break;

				ImportGameDuringPlay(arrowTier);
			}

			if (MousePresent != 0)
				this.Owner.Services.DeactivateMouse();

			this.Owner.Services.PerformSound(0, 0);
			this.Owner.Services.HaltSound();
			this.Owner.Services.HaltTimer();

			this.Processor.Journal.CloseScope("RunGame");
		}

		/// <summary>
		/// Loads the game chosen with Load Game in the GAME menu, after the main loop has left the turn.
		/// It does the same steps as Load a Saved Game in the start menu.
		/// </summary>
		/// <param name="pointerLevel">Mouse pointer show/hide level at the start of the game</param>
		public void ImportGameDuringPlay(ushort arrowTier)
		{
			this.Processor.Journal.OpenScope($"ImportGameDuringPlay({arrowTier})");

			GameEndRequest = 0x0;
			ChosenOrder = 0x0;

			// The turn was left in the middle, restore the pointer level
			while ((short)PointerShowLevel > (short)arrowTier)
				ConcealMouseCursor();

			while ((short)PointerShowLevel < (short)arrowTier)
				DisplayMouseCursor();

			ushort bay = this.Owner.SaveLoad.FetchBay;
			this.Owner.SaveLoad.FetchBay = 0xffff;
			this.Owner.SaveLoad.ImportGameFromSlot(bay);

			// From here the same as after Load a Saved Game in LogoAndMainGameMenu
			ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.LightGray);

			this.Owner.NewGameWizard.DisplayTribeSelection();

			BoxMotif = 0x1;

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileSp257Pic, 1);

			this.Owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			FreshWorldChoices();

			MainMenuChoice = 0x1;
			MapViewHoriz = (ushort)(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].HorizBegin - 7);
			MapViewVert = 0x13;

			this.Owner.TurnHooks.RepaintMainScreen();

			this.Processor.Journal.CloseScope("ImportGameDuringPlay");
		}

		/// <summary>
		/// The mouse: GlobDb3a the clicks since the last call and the buttons held, GlobDb3c / GlobDb3e the place; all 0
		/// without a mouse. The interrupts are served first.
		/// </summary>
		public void ObtainMouseState()
		{
			// The game waits for the buttons in loops of this call alone: the interrupts (the mouse of the window, the timer)
			// come here, as they came with the reads of 0x586e through the CPU before the state moved to C#
			this.Processor.PumpMessages();

			if (MousePresent != 0)
			{
				ushort clicks = this.Owner.Services.ObtainAndWipeMouseClick();

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

		/// <summary>
		/// Raises the show level of the mouse pointer (0xdeea); the pointer is drawn when the level reaches 1 and there is a
		/// mouse (0x1a3c)
		/// </summary>
		public void DisplayMouseCursor()
		{
			this.Processor.Journal.OpenScope("DisplayMouseCursor()");

			PointerShowLevel = (ushort)(PointerShowLevel + 1);

			if (MousePresent != 0 && PointerShowLevel == 1)
			{
				this.Owner.Services.RenderMouseCursor();
			}

			this.Processor.Journal.CloseScope("DisplayMouseCursor");
		}

		/// <summary>
		/// Hides the mouse pointer one level more (PointerShowLevel); it is blanked when it goes from shown, level 1
		/// </summary>
		public void ConcealMouseCursor()
		{
			this.Processor.Journal.OpenScope("ConcealMouseCursor()");

			if (MousePresent != 0 && PointerShowLevel == 1)
			{
				this.Owner.Services.BlankMouseCursor();
			}
		
			PointerShowLevel = (ushort)(PointerShowLevel - 1);

			this.Processor.Journal.CloseScope("ConcealMouseCursor");
		}

		/// <summary>
		/// Remembers the show level of the mouse pointer (0xdeea) in 0x652e and shows the pointer until the level is at least 1
		/// </summary>
		public void DisplayMouseCursorFully()
		{
			this.Processor.Journal.OpenScope("DisplayMouseCursorFully()");

			SavedPointerShowLevel = PointerShowLevel;

			while ((short)PointerShowLevel < 1)
			{
				DisplayMouseCursor();
			}

			this.Processor.Journal.CloseScope("DisplayMouseCursorFully");
		}

		/// <summary>
		/// Hides the mouse pointer back to the show level remembered by DisplayMouseCursorFully (0x652e)
		/// </summary>
		public void ConcealMouseCursorToSavedLevel()
		{
			this.Processor.Journal.OpenScope("ConcealMouseCursorToSavedLevel()");

			while ((short)PointerShowLevel > (short)SavedPointerShowLevel)
			{
				ConcealMouseCursor();
			}

			this.Processor.Journal.CloseScope("ConcealMouseCursorToSavedLevel");
		}

		/// <summary>
		/// Makes sure the file that marks game disk "diskIdx" (path at 0x1ace + 2 * disk) is there, asking "Please insert
		/// Disk ..." until it is; the disk in use goes to 0xe17c. Marker 0: disk 0 instead when the disk is missing, without
		/// asking; marker 0xffff: ask once even when it is there (then marker 1). The message is made in a buffer in the stack
		/// frame, like in the original.
		/// </summary>
		/// <returns>1 when found at once with marker 0, else the marker (also left in AX)</returns>
		public ushort QueryForDisk(ushort diskIdx, ushort marker)
		{
			this.Processor.Journal.OpenScope($"QueryForDisk({diskIdx}, {marker})");


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
					this.Owner.Services.DisplayScreen(DrawingBox.DisplayIdx);

					if (marker == 0xffff)
					{
						marker = 1;
					}

					ConcealMouseCursor();

					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.White);

					this.Processor.EmitGlobalByte(DiskNumberPromptAddress,
						(byte)(CPU.SignedQuotient((short)diskIdx, (short)FirstMissingDisk) + 0x31));

					string message = "Please insert Disk " + "A" + ".\n \n    Press Return\n    to continue.\n";

					ushort kept = KingMood;
					KingMood = 0xffff;
					MenuAnyKey = 0x1;

					this.Owner.Dialogs.DisplayMenu(message, 100, 81, 1);

					KingMood = kept;

					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.White);

					ScreenRepaintRequest = 0x1;

					DisplayMouseCursor();
				}

				result = marker;
			}

			DiskInUse = diskIdx;



			this.Processor.Journal.CloseScope("QueryForDisk");

			return result;
		}

		/// <summary>
		/// The main menu after the title (the choice in 0x6b32, asked when it is -1): 0 = a new game (all world options 1,
		/// the world is made with the story), 1 = a saved game, 2 = EARTH (0xd76a = 1), 3 = a new game with the world options
		/// asked (land mass, temperature, climate, age in 0x7ef6 - 0x7efc), 4 = the Hall of Fame and the menu again. The choice
		/// is played at a measure of the title music. Then the tribe is chosen and the game screen prepared.
		/// </summary>
		public void TitleLogoAndMainChoices()
		{
			this.Processor.Journal.OpenScope("TitleLogoAndMainChoices()");

			EarthGame = 0x0;

			bool chosen = false;

			while (!chosen)
			{
				while (MainMenuChoice == 0xffff)
				{
					this.Owner.Words.Set(" Start a New Game\n Load a Saved Game\n EARTH\n Customize World\n View Hall of Fame\n");

					MainMenuChoice = this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 100, 140, 1);
				}

				AwaitTitleSongMeasure();

				this.Owner.Services.PerformSound(1, 0);

				switch (MainMenuChoice)
				{
					case 0:
						ConcealMouseCursor();

						// The default world: all options 1
						for (int optionIdx = 0; optionIdx < 4; optionIdx++)
						{
							this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(0x7ef6 + (optionIdx << 1)), 0x1);
						}

						MakeNewWorld();
						chosen = true;
						break;

					case 1:
						if (this.Owner.SaveLoad.ImportGameFromMenu(0xffff) == 0xffff)
						{
							// No game loaded: the menu again
							MainMenuChoice = 0xffff;
							break;
						}

						ConcealMouseCursor();

						this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.LightGray);
						chosen = true;
						break;

					case 2:
						ConcealMouseCursor();

						EarthGame = 0x1;

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
						ConcealMouseCursor();

						this.Owner.Legends.ImportHallOfFame();

						this.Owner.Legends.DisplayHallOfFame(0xffff);

						DisplayMouseCursor();

						MainMenuChoice = 0xffff;
						break;

					default:
						chosen = true;
						break;
				}
			}

			this.Owner.NewGameWizard.DisplayTribeSelection();

			BoxMotif = 0x1;

			if (VgaMode != 0 && (MainMenuChoice == 0 ||
				MainMenuChoice == 2 || MainMenuChoice == 3))
			{
				this.Owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileSp257Pic, 1);

			this.Owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			this.Processor.Journal.CloseScope("TitleLogoAndMainChoices");
		}

		/// <summary>
		/// The choices of a new game (only before the first turn, after the picture 0x1c16) and the government pictures
		/// </summary>
		public void FreshWorldChoices()
		{
			this.Processor.Journal.OpenScope("FreshWorldChoices()");

			if (MousePresent != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, PointerPicture);
			}

			if (this.Owner.World.RoundTally == 0)
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileDiffsPic, 1);

				QueryForDisk(0, 1);

				this.Owner.NewGameWizard.ConfigureFreshGame();
			}

			this.Owner.NewGameWizard.ImportGovernmentGraphics();

			QueryForDisk(0, 1);

			this.Processor.Journal.CloseScope("FreshWorldChoices");
		}

		/// <summary>
		/// Waits until the title music is at the start (up to 0x1495) or at a measure (0x120 after it); the position is kept
		/// in the frame, like the original (a memory access every round, so the timer of the tests goes on)
		/// </summary>
		private void AwaitTitleSongMeasure()
		{
			short position;

			do
			{
				position = (short)this.Owner.Services.ObtainSongPosition();

			}
			while (position > 0x1495 && (short)(position - 0x1495) % 0x120 != 0);
		}

		/// <summary>
		/// Makes the new world with the story (OpeningSequence, its own journal) and shows it
		/// </summary>
		private void MakeNewWorld()
		{
			this.Processor.Journal = this.Owner.PrologueJournal;
			this.Processor.Journal.OpenScope("'Intro start'");
			this.Owner.VideoJournal.OpenScope("'Intro start'");
			this.Owner.IrqJournal.OpenScope("'Intro start'");

			this.Owner.WorldSetup.OpeningSequence();

			this.Owner.IrqJournal.CloseScope("'Intro end'");
			this.Owner.VideoJournal.CloseScope("'Intro end'");
			this.Processor.Journal.CloseScope("'Intro end'");
			this.Processor.Journal = this.Owner.Journal;

			ShowMadeWorld(0x1b33);
		}

		/// <summary>
		/// The made world on the screen, with the right half of the picture (file name at pictureName) over it
		/// </summary>
		private void ShowMadeWorld(ushort pictureName)
		{
			DrawingBox.DisplayIdx = 0x0;

			this.Owner.Services.DisplayScreen(0);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, pictureName, 0);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
				PictureScreenBox, 0xa0, 0x32);

			QueryForDisk(0, 1);
		}

		/// <summary>
		/// The world options of "Customize World" on their picture: land mass, temperature, climate and age
		/// </summary>
		private void CustomizeWorld()
		{
			ConcealMouseCursor();

			QueryForDisk(7, 1);

			if (VgaMode != 0)
			{
				this.Owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileCustomPic, 1);

			QueryForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			DisplayMouseCursor();

			ChooseWorldOption(0x7ef6, 1, "LAND MASS:\n Small\n Normal\n Large\n", 0x1b6a);
			ChooseWorldOption(0x7ef8, 51, "TEMPERATURE:\n Cool\n Temperate\n Warm\n", 0x1b94);
			ChooseWorldOption(0x7efa, 101, "CLIMATE:\n Arid\n Normal\n Wet\n", 0x1bbd);
			ChooseWorldOption(0x7efc, 151, "AGE:\n 3 billion years\n 4 billion years\n 5 billion years\n", 0x1bfe);

			ConcealMouseCursor();
		}

		/// <summary>
		/// One world option (menu at 200, menuVert, the mouse on its second line, the default line 0x2f9a starts at 1): asked
		/// again for no answer, and after the help topic (0x2f9c, then the answer is the default line)
		/// </summary>
		private void ChooseWorldOption(ushort optionAddr, short menuVert, string choices, ushort helpTopic)
		{
			DefaultLine = 0x1;

			while (true)
			{
				this.Owner.Services.AssignMousePosition(210, (ushort)(menuVert + 10));

				this.Owner.Words.Set(choices);

				this.Processor.EmitGlobalWord(optionAddr, this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 200, menuVert, 1));

				if (this.Processor.PullGlobalWord(optionAddr) == 0xffff)
					continue;

				if (MenuHelpTopic == 0)
					break;

				this.Owner.HelpSystem.DisplayHelpTopic(helpTopic);

				DefaultLine = this.Processor.PullGlobalWord(optionAddr);
			}
		}
	}
}
