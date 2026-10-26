using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x11a8.
	/// </summary>
	public class GameStartup : GameModule
	{
		/// <summary>The words of the frame of TitleLogoAndMainChoices, shared by its helpers (B3: in C#, new for every call)</summary>
		private FrameWords titleLogoAndMainChoicesFrame = null!;

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
			this.processor.Journal.OpenScope("RunGame()");

			this.processor.EnterFrame(0xa);

			// Main menu selection
			// '1' - VGA; first letter of driver
			this.processor.EmitGlobalByte(0x1a22, 0x4d);
			// '1' - No sound 0x4e, '4' - Sound blaster 0x41, '5' - Roland MIDI board 0x52; first letter of driver
			this.processor.EmitGlobalByte(0x1a30, 0x4e);
			// '1' - Mouse and Keyboard 0x1, '2' - Keyboard only 0x0
			MousePresent = 0x1;

			this.owner.Prologue.DisplayMainIntro();

			QueryForDisk(0, 1);

			// The free memory stays in the frame (bp - 4) like in the original: the timer of the tests counts the accesses to memory
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x0055); // stack management - push return offset
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment
			this.processor.EmitLocalWord(0x4, this.owner.VideoCard.ObtainFreeStorage());

			if (ScreenReserveFailed != 0)
			{
				// A screen could not be reserved
				this.processor.EmitLocalWord(0x4, 0);
			}

			this.processor.RegAcc.Value16 = (ushort)(VgaMode != 0 ? 0x1f40 : 0xfa0);

			if ((short)this.processor.RegAcc.Value16 > (short)this.processor.PullLocalWord(0x4))
			{
				// Not enough memory: the amount in the text 0x270, then a menu with it
				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 12);

				EmptyMessage()
					.Number((short)this.processor.PullLocalWord(0x4));
				this.owner.Libc.CopyText(CodeText4, TextBuffer.Address);
				this.owner.Words.Clear();

				this.owner.BlockText.DisplayTextDialog(0x270);
				this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 64, 49, 1);
			}

			if (VgaMode != 0 && (short)this.processor.PullLocalWord(0x4) < 0x23be)
				LowMemoryVga = 1;

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x0110); // stack management - push return offset
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			PointerIcon = (ushort)(this.owner.ImageToolkit.ImportIcon(0x277));

			if (MousePresent != 0)
			{
				MousePresent = this.owner.Services.PrepareMouse();
			}

			if (MousePresent != 0)
			{
				this.owner.Services.AssignMouseCursorImage(0, 0, PointerIcon);
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
				MapViewHoriz = (ushort)(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin - 7);
				this.processor.RegAcc.Value16 = MapViewHoriz;
				MapViewVert = 0x13;

				this.owner.TurnHooks.RepaintMainScreen();
			}

			// Mouse pointer show/hide level at the start of the game, a game loaded during play starts with the same level
			ushort arrowTier = PointerShowLevel;

			while (true)
			{
				GameEndRequest = 0;

				do
				{
					this.owner.TurnHooks.HandleGameEnd();
				}
				while (GameEndRequest == 0);

				if (GameEndRequest != GameLoadAndSave.FetchSessionAsk)
					break;

				ImportGameDuringPlay(arrowTier);
			}

			if (MousePresent != 0)
				this.owner.Services.DeactivateMouse();

			this.owner.Services.PerformSound(0, 0);
			this.owner.Services.HaltSound();
			this.owner.Services.HaltTimer();

			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("RunGame");
		}

		/// <summary>
		/// Loads the game chosen with Load Game in the GAME menu, after the main loop has left the turn.
		/// It does the same steps as Load a Saved Game in the start menu.
		/// </summary>
		/// <param name="pointerLevel">Mouse pointer show/hide level at the start of the game</param>
		public void ImportGameDuringPlay(ushort arrowTier)
		{
			this.processor.Journal.OpenScope($"ImportGameDuringPlay({arrowTier})");

			GameEndRequest = 0x0;
			ChosenOrder = 0x0;

			// The turn was left in the middle, restore the pointer level
			while ((short)PointerShowLevel > (short)arrowTier)
				ConcealMouseCursor();

			while ((short)PointerShowLevel < (short)arrowTier)
				DisplayMouseCursor();

			ushort bay = this.owner.SaveLoad.FetchBay;
			this.owner.SaveLoad.FetchBay = 0xffff;
			this.owner.SaveLoad.ImportGameFromSlot(bay);

			// From here the same as after Load a Saved Game in LogoAndMainGameMenu
			ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 7);

			this.owner.NewGameWizard.DisplayTribeSelection();

			BoxMotif = 0x1;

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1c0c, 1);

			this.owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			FreshWorldChoices();

			MainMenuChoice = 0x1;
			MapViewHoriz = (ushort)(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin - 7);
			MapViewVert = 0x13;

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.Journal.CloseScope("ImportGameDuringPlay");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ObtainMouseState()
		{
			// function body
			if (MousePresent != 0)
			{
				this.owner.Services.ObtainAndWipeMouseClick();

				this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.owner.Pointer.Buttons);
				this.owner.GlobDb3a = this.processor.RegAcc.Value16;
				this.owner.GlobDb3c = this.owner.Pointer.Horiz;
				this.owner.GlobDb3e = this.owner.Pointer.Vert;
				this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
				this.owner.GlobDb3e = 0;
				this.owner.GlobDb3c = 0;
				this.owner.GlobDb3a = 0;
			}
		}

		/// <summary>
		/// Raises the show level of the mouse pointer (0xdeea); the pointer is drawn when the level reaches 1 and there is a
		/// mouse (0x1a3c)
		/// </summary>
		public void DisplayMouseCursor()
		{
			this.processor.Journal.OpenScope("DisplayMouseCursor()");

			PointerShowLevel = (ushort)(PointerShowLevel + 1);

			if (MousePresent != 0 && PointerShowLevel == 1)
			{
				this.owner.Services.RenderMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayMouseCursor");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ConcealMouseCursor()
		{
			this.processor.Journal.OpenScope("ConcealMouseCursor()");

			// function body
			if (MousePresent != 0 && PointerShowLevel == 1)
			{
				this.owner.Services.BlankMouseCursor();
			}
		
			PointerShowLevel = this.processor.DecrementWord(PointerShowLevel);

			// Far return
			this.processor.Journal.CloseScope("ConcealMouseCursor");
		}

		/// <summary>
		/// Remembers the show level of the mouse pointer (0xdeea) in 0x652e and shows the pointer until the level is at least 1
		/// </summary>
		public void DisplayMouseCursorFully()
		{
			this.processor.Journal.OpenScope("DisplayMouseCursorFully()");

			SavedPointerShowLevel = PointerShowLevel;

			while ((short)PointerShowLevel < 1)
			{
				DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayMouseCursorFully");
		}

		/// <summary>
		/// Hides the mouse pointer back to the show level remembered by DisplayMouseCursorFully (0x652e)
		/// </summary>
		public void ConcealMouseCursorToSavedLevel()
		{
			this.processor.Journal.OpenScope("ConcealMouseCursorToSavedLevel()");

			while ((short)PointerShowLevel > (short)SavedPointerShowLevel)
			{
				ConcealMouseCursor();
			}

			this.processor.Journal.CloseScope("ConcealMouseCursorToSavedLevel");
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
			this.processor.Journal.OpenScope($"QueryForDisk({diskIdx}, {marker})");

			this.processor.EnterFrame(0x102);

			ushort result;

			if (marker == 0 && this.owner.Services.PathPresent(this.processor.PullGlobalWord((ushort)((diskIdx << 1) + 0x1ace))) != 0xffff)
			{
				result = 1;
			}
			else
			{
				if (marker == 0)
				{
					diskIdx = 0;
				}

				while (this.owner.Services.PathPresent(this.processor.PullGlobalWord((ushort)((diskIdx << 1) + 0x1ace))) == 0xffff ||
					marker == 0xffff)
				{
					this.owner.Services.DisplayScreen(DrawingBox.DisplayIdx);

					if (marker == 0xffff)
					{
						marker = 1;
					}

					ConcealMouseCursor();

					this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

					this.processor.EmitGlobalByte(0x1ade,
						(byte)(CPU.SignedQuotient((short)diskIdx, (short)FirstMissingDisk) + 0x31));

					ushort message = (ushort)(this.processor.RegFrame.Value16 - 0x100);
					this.owner.Libc.CopyText(message, "Please insert Disk ");
					this.owner.Libc.AppendText(message, "A");
					this.owner.Libc.AppendText(message, ".\n \n    Press Return\n    to continue.\n");

					ushort kept = KingMood;
					KingMood = 0xffff;
					MenuAnyKey = 0x1;

					this.owner.Dialogs.DisplayMenu(message, 100, 81, 1);

					KingMood = kept;

					this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

					ScreenRepaintRequest = 0x1;

					DisplayMouseCursor();
				}

				result = marker;
			}

			DiskInUse = diskIdx;
			this.processor.RegAcc.Value16 = result;

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("QueryForDisk");

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
			this.processor.Journal.OpenScope("TitleLogoAndMainChoices()");

			// The frame like the original (the song position is kept at bp - 2)
			this.processor.EnterFrame(0x6);
			FrameWords outerFrame = this.titleLogoAndMainChoicesFrame;
			this.titleLogoAndMainChoicesFrame = new FrameWords(this.processor, 0x6);
			EarthGame = 0x0;

			bool chosen = false;

			while (!chosen)
			{
				while (MainMenuChoice == 0xffff)
				{
					this.owner.Words.Set(" Start a New Game\n Load a Saved Game\n EARTH\n Customize World\n View Hall of Fame\n");

					MainMenuChoice = this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 100, 140, 1);
				}

				AwaitTitleSongMeasure();

				this.owner.Services.PerformSound(1, 0);

				switch (MainMenuChoice)
				{
					case 0:
						ConcealMouseCursor();

						// The default world: all options 1
						for (int optionIdx = 0; optionIdx < 4; optionIdx++)
						{
							this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(0x7ef6 + (optionIdx << 1)), 0x1);
						}

						MakeNewWorld();
						chosen = true;
						break;

					case 1:
						if (this.owner.SaveLoad.ImportGameFromMenu(0xffff) == 0xffff)
						{
							// No game loaded: the menu again
							MainMenuChoice = 0xffff;
							break;
						}

						ConcealMouseCursor();

						this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 7);
						chosen = true;
						break;

					case 2:
						ConcealMouseCursor();

						EarthGame = 0x1;

						this.owner.WorldSetup.OpeningSequence();

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

						this.owner.Legends.ImportHallOfFame();

						this.owner.Legends.DisplayHallOfFame(0xffff);

						DisplayMouseCursor();

						MainMenuChoice = 0xffff;
						break;

					default:
						chosen = true;
						break;
				}
			}

			this.owner.NewGameWizard.DisplayTribeSelection();

			BoxMotif = 0x1;

			if (VgaMode != 0 && (MainMenuChoice == 0 ||
				MainMenuChoice == 2 || MainMenuChoice == 3))
			{
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1c0c, 1);

			this.owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			this.titleLogoAndMainChoicesFrame = outerFrame;
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("TitleLogoAndMainChoices");
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
				position = (short)this.owner.Services.ObtainSongPosition();

				this.titleLogoAndMainChoicesFrame.SetFrame(0x2, (ushort)position);
			}
			while (position > 0x1495 && (short)(position - 0x1495) % 0x120 != 0);
		}

		/// <summary>
		/// Makes the new world with the story (OpeningSequence, its own journal) and shows it
		/// </summary>
		private void MakeNewWorld()
		{
			this.processor.Journal = this.owner.PrologueJournal;
			this.processor.Journal.OpenScope("'Intro start'");
			this.owner.VideoJournal.OpenScope("'Intro start'");
			this.owner.IrqJournal.OpenScope("'Intro start'");

			this.owner.WorldSetup.OpeningSequence();

			this.owner.IrqJournal.CloseScope("'Intro end'");
			this.owner.VideoJournal.CloseScope("'Intro end'");
			this.processor.Journal.CloseScope("'Intro end'");
			this.processor.Journal = this.owner.Journal;

			ShowMadeWorld(0x1b33);
		}

		/// <summary>
		/// The made world on the screen, with the right half of the picture (file name at pictureName) over it
		/// </summary>
		private void ShowMadeWorld(ushort pictureName)
		{
			DrawingBox.DisplayIdx = 0x0;

			this.owner.Services.DisplayScreen(0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, pictureName, 0);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
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
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1b3d, 1);

			QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
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
				this.owner.Services.AssignMousePosition(210, (ushort)(menuVert + 10));

				this.owner.Words.Set(choices);

				this.processor.EmitGlobalWord(optionAddr, this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 200, menuVert, 1));

				if (this.processor.PullGlobalWord(optionAddr) == 0xffff)
					continue;

				if (MenuHelpTopic == 0)
					break;

				this.owner.HelpSystem.DisplayHelpTopic(helpTopic);

				DefaultLine = this.processor.PullGlobalWord(optionAddr);
			}
		}

		/// <summary>
		/// The choices of a new game (only before the first turn, after the picture 0x1c16) and the government pictures
		/// </summary>
		public void FreshWorldChoices()
		{
			this.processor.Journal.OpenScope("FreshWorldChoices()");

			if (MousePresent != 0)
			{
				this.owner.Services.AssignMouseCursorImage(0, 0, PointerPicture);
			}

			if (this.owner.World.RoundTally == 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1c16, 1);

				QueryForDisk(0, 1);

				this.owner.NewGameWizard.ConfigureFreshGame();
			}

			this.owner.NewGameWizard.ImportGovernmentGraphics();

			QueryForDisk(0, 1);

			this.processor.Journal.CloseScope("FreshWorldChoices");
		}
	}
}
