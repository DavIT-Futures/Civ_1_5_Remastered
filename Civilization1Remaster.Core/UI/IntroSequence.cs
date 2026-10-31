
namespace Civilization1Remaster.Core.UI
{
	public class IntroSequence : GameComponent, IIntroSequence
	{
		// The local variables of ShowCivilizationScreen by their distance below BP in the original (the frame of the ship
		// animation 0x208 is never set before it is first read: 0, like a zeroed frame); the text below them is screenText
		private const int ScreenText = 0x200;

		private const int ScreenPartEnd = 0x204;
		private const int ScreenShipFrame = 0x208;
		private const int ScreenShipPictures = 0x238;
		private const int ScreenSavedFlag = 0x23a;
		private const int ScreenPartBegin = 0x23c;
		private const int ScreenColumn = 0x23e;
		private const int ScreenTextLength = 0x240;
		private const int ScreenStarHoriz = 0x242;
		private const int ScreenStarVert = 0x244;
		private const int ScreenBackground = 0x246;

		/// <summary>Columns of the first part of the intro at which a line of credits.txt is read, or (0x32, 0xa0) the line is taken away</summary>
		private static readonly ushort[] CreditColumns = [0xa, 0x32, 0x46, 0x78, 0xa0, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		/// <summary>Columns of the second part of the intro at which a line of credits.txt is read, or (0x32, 0x74, 0x136) the line is taken away</summary>
		private static readonly ushort[] PlanetColumns = [0xa, 0x1e, 0x32, 0x40, 0x54, 0x74, 0x82, 0x96, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		private ushort introScreenCount;
		private ushort introSavedFlag;
		private ushort introColumn;
		private ushort introPathFound;
		private ushort introLogo;

		/// <summary>The words of the frame of ShowCivilizationScreen above its text (B3: in C#, new for every call)</summary>
		private FrameWords screenFrame = null!;

		/// <summary>The copy of the text of ShowCivilizationScreen (0x200 bytes at BP - 0x200 in the original)</summary>
		private byte[] screenText = new byte[ScreenText];

		public IntroSequence(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ShowMainIntro()
		{
			this.Processor.Journal.OpenScope("DisplayMainIntro()");

			FindIntroDisks();
			DetectGraphicsDriver();
			ReserveIntroScreens();
			LoadIntroPictures();
			ScrollCredits();
			ScrollPlanet();
			TakeIntroChoice();
			CloseIntro();

			this.Processor.Journal.CloseScope("DisplayMainIntro");
		}

		/// <inheritdoc/>
		public void ShowCivilizationScreen(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayCivilizationScreen({participantIdx})");

			FrameWords outerFrame = this.screenFrame;
			byte[] outerText = this.screenText;
			this.screenFrame = new FrameWords(this.Processor, 0x246);
			this.screenText = new byte[ScreenText];

			LoadShipPictures();
			PrepareLandingText(participantIdx);
			FlyShip();
			ShowLanding(participantIdx);
			DrawClosingText();
			CloseLanding(participantIdx);

			this.screenFrame = outerFrame;
			this.screenText = outerText;
			this.Processor.Journal.CloseScope("DisplayCivilizationScreen");
		}

		/// <inheritdoc/>
		public void ShowTerrainTutorial()
		{
			this.Processor.Journal.OpenScope("DisplayTerrainTutorial()");

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.AskForDisk(6, 1);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.Services.PerformSound(0x23, 0);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileArchPic, 1);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			short humanIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Words.Set(LeaderName(humanIdx));
			this.Owner.Words.Uppercase();
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			this.Owner.Words.Set("Irrigation");

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(humanIdx, technologyIdx) != 0)
				{
					this.Owner.Words.Set(TechnologyText(technologyIdx));
				}
			}

			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			this.Owner.BlockText.LoadTextFromFile(GameTexts.FileKingTxtC, GameTexts.TopicArch);

			this.Owner.BlockText.ReflowText();

			DrawingBox.TypefaceIdx = 6;

			short end = (short)this.Owner.Words.Length;
			short place = -1;

			do
			{
				ushort partAddr = (ushort)(TextBuffer.Address + place + 1);

				do
				{
					place++;
				} while (this.Owner.Words[place] != 0x5e && place < end);

				if (place >= end)
					break;

				this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + place), 0);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 6, 0x140, 0x14, ScreenBox, 0, 6);

				ushort hue = 9;

				if (VgaMode != 0)
				{
					this.Owner.TextPainter.DrawCenteredTextToScreen0(partAddr, 160, 7, Hues.White);
					this.Owner.TextPainter.DrawCenteredTextToScreen0(partAddr, 160, 9, Hues.LightMagenta);

					hue = 14;
				}
				else
				{
					this.Owner.TextPainter.DrawCenteredTextToScreen0(partAddr, 160, 9, Hues.Blue);
				}

				this.Owner.TextPainter.DrawCenteredTextToScreen0(partAddr, 160, 8, hue);

				if (this.Owner.Words[place - 1] != 0x21 && this.Owner.Libc.KeyAvailable() == 0)
				{
					this.Owner.TextPainter.WaitForTime(200);
				}
			} while (place < end);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[humanIdx].ChiefVisuals, LeaderTable.ShortTune), 0);

			this.Owner.TextPainter.WaitForTime(300);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.TurnHooks.ShowCivilizationScenery();

			DrawingBox.TypefaceIdx = 1;

			this.Processor.Journal.CloseScope("DisplayTerrainTutorial");
		}

		/// <inheritdoc/>
		public void ShowInterfaceTutorial()
		{
			this.Processor.Journal.OpenScope("DisplayInterfaceTutorial()");


			this.Owner.Startup.HideMouseCursor();

			short centreHoriz = (short)(MapViewHoriz + 7);
			short centreVert = (short)(MapViewVert + 6);
			IMapPlanes chart = this.Owner.MapOps.Chart;

			for (int heading = 1; heading <= 8; heading++)
			{
				int horizLoc = this.Owner.Routing.TunePosition(chart.DirectionHoriz(heading) + centreHoriz);
				short vertLoc = (short)(chart.DirectionVert(heading) + centreVert);
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				// Terrain names of 0x13 bytes at 0x282, names of their special resources at 0x366
				this.Owner.Words.Set(TerrainText(terrain));

				if (this.Owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0)
				{
					this.Owner.Words.Set((ushort)((terrain * 0x13) + 0x366));
				}

				if (this.Owner.MapOps.ContainsHut(terrain, horizLoc, vertLoc) != 0)
				{
					this.Owner.Words.Set("Village");
				}

				DrawTutorialPointer((chart.DirectionHoriz(heading) << 4) + 200, (chart.DirectionVert(heading) << 4) + 112,
					(chart.DirectionHoriz(heading) << 6) + 170, (chart.DirectionVert(heading) * 48) + 100);
			}

			this.Owner.Words.Set("Menu Bar");

			DrawTutorialPointer(160, 6, 200, 16);

			this.Owner.Words.Set("Map Window");

			DrawTutorialPointer(48, 32, 88, 24);

			this.Owner.Words.Set("Active Unit");

			DrawTutorialPointer(40, 128, 88, 170);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();


			this.Processor.Journal.CloseScope("DisplayInterfaceTutorial");
		}

		/// <inheritdoc/>
		public void DrawTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2)
		{
			this.Processor.Journal.OpenScope($"RenderTutorialPointer({horizSpot1}, {vertSpot1}, {horizSpot2}, {vertSpot2})");

			this.Owner.VideoCard.RenderLine(ScreenBox, horizSpot1, vertSpot1, horizSpot2, vertSpot2 + 6, Hues.White);

			this.Owner.Words.Append("\n");

			DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, horizSpot2, vertSpot2, 1);

			this.Processor.Journal.CloseScope("RenderTutorialPointer");
		}

		/// <summary>The disks 1 - 7 that are present one after the other (0xdeba: the first one missing), then waits for the path 0x324a</summary>
		private void FindIntroDisks()
		{
			FirstMissingDisk = 1;
			this.introColumn = 1;

			do
			{
				if (this.Owner.Startup.AskForDisk(this.introColumn, 0) == 0)
					break;

				FirstMissingDisk = (ushort)(this.introColumn + 1);
				this.introColumn++;
			}
			while ((short)this.introColumn < 8);

			this.introPathFound = 0;

			do
			{
				ushort found = this.Owner.Services.PathPresent(0x324a);
				this.introPathFound = (ushort)(found == 0xffff ? 0 : 1);
			}
			while (this.introPathFound == 0);
		}

		/// <summary>The first letter of the driver name: m = VGA / MCGA (0xd762), e = EGA (0xb20e)</summary>
		private void DetectGraphicsDriver()
		{
			bool colourful = this.Processor.PullGlobalByte(GameLauncher.VideoDriverOptionAddress) == 0x6d || this.Processor.PullGlobalByte(GameLauncher.VideoDriverOptionAddress) == 0x4d;
			VgaMode = (ushort)(colourful ? 1 : 0);

			bool ega = this.Processor.PullGlobalByte(GameLauncher.VideoDriverOptionAddress) == 0x65 || this.Processor.PullGlobalByte(GameLauncher.VideoDriverOptionAddress) == 0x45;
			EgaMode = (ushort)(ega ? 1 : 0);

			// Change Num-Lock state
			DrawingBox.TypefaceIdx = 1;
		}

		/// <summary>Screens 0 - 2 (VGA) or 0 - 3; 0x6dfc is set when one of them can't be reserved</summary>
		private void ReserveIntroScreens()
		{
			ScreenReserveFailed = 0;
			this.introScreenCount = (ushort)(VgaMode != 0 ? 3 : 4);

			this.introColumn = 0;

			while (true)
			{
				short screenCount = (short)this.introScreenCount;

				if ((short)this.introColumn >= screenCount)
					break;

				IntroReservedScreen = this.Owner.VideoCard.ReserveScreen(this.introColumn);

				if (IntroReservedScreen == 0)
					ScreenReserveFailed = 1;

				this.introColumn++;
			}
		}

		/// <summary>Sound, timer, the pictures of the intro (the logo is kept in a picture), the colour cycles and credits.txt</summary>
		private void LoadIntroPictures()
		{
			this.Owner.Services.PrepareSoundDriver();
			this.Owner.Services.LaunchTimer();
			this.Owner.Helpers.SeedRandomizer();

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			if (VgaMode == 0)
				this.Owner.Services.FloodBox(SpriteScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.VideoCard.AssignHuesByIndexArray(0x19fe);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileLogoPic, 1);
			this.introLogo = this.Owner.VideoCard.ScreenToPicture(1, 0, 64, 320, 80);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileBirth0Pic, 1);
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x4c, 0x140, 0x18,
				BackScreenBox, 0, 0xb0);

			this.Owner.ImageToolkit.LoadPictureOrColors(2, 0, 0, GameTexts.FileBirth1Pic, 1);
			this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x4c, 0x140, 0x18,
				PictureScreenBox, 0, 0xb0);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileLogoPicB, 1);
			this.Owner.Services.FloodBox(ScreenBox, 0, 12, 320, 176, Hues.Black);
			this.Owner.Services.AssignHueCycleSpan(1, 14, 146, 152);

			if (VgaMode != 0)
			{
				this.introColumn = 1;

				do
				{
					this.Owner.Services.ActivateHueCycle(this.introColumn);
					this.introColumn++;
				}
				while ((short)this.introColumn <= 3);
			}

			this.Owner.World.SessionPrefMarkers |= 0x10;

			this.Owner.GlobD768 = (ushort)this.Owner.Libc.OpenStream("credits.txt", "rt");

			this.Owner.Services.RestoreTimer();
			this.Owner.Services.PerformSound(3, 0);

			this.Owner.Words.Clear();
		}

		/// <summary>
		/// The first part: the picture comes in from the right, column after column, in time with the music, with the lines
		/// of the credits read at some columns; a key ends the part
		/// </summary>
		private void ScrollCredits()
		{
			for (this.introColumn = 0;
				(short)this.introColumn < 0x140;
				this.introColumn++)
			{
				ushort rest = (ushort)(0x140 - this.introColumn);

				this.Owner.Services.FloodBox(BackScreenBox, 0, 176, (short)rest, 24, Hues.Black);
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x40, this.introColumn, 0x18,
					BackScreenBox, rest, 0xb0);

				foreach (ushort column in CreditColumns)
				{
					if (this.introColumn != column)
						continue;

					if (column == 0x32 || column == 0xa0)
					{
						this.Owner.Words.Clear();
					}
					else
					{
						if (column == 0xa)
							DrawingBox.TypefaceIdx = 5;

						this.Owner.Libc.ScanStream((short)this.Owner.GlobD768, "%[^\n]\n", TextBuffer.Address);
					}
				}

				if (this.Owner.Words.Length != 0)
					DrawCreditLine();

				FlickerCgaScreen();

				rest = (ushort)(0x140 - this.introColumn);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, this.introColumn, 0x40,
					ScreenBox, rest, 0xc);
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
					ScreenBox, 0, 0x4c);
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x58, this.introColumn, 0x58,
					ScreenBox, rest, 0x64);

				ShowCgaScreen();

				// Behind the music: one column more (only from an even column); then wait until the music reaches the column
				if (MusicPassedColumn(0) && (this.introColumn & 1) == 0)
					this.introColumn++;

				while (!MusicReachedColumn(0))
				{
				}

				if ((ushort)this.Owner.Libc.KeyAvailable() != 0)
					this.introColumn = 0x3e7;
			}
		}

		/// <summary>
		/// The second part: the picture of screen 2 pushes the first one out to the left, column after column, in time with
		/// the music, with more lines of the credits; a key ends the part
		/// </summary>
		private void ScrollPlanet()
		{
			for (this.introColumn = 0;
				(short)this.introColumn < 0x140;
				this.introColumn++)
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox,
					this.introColumn, 0x40, (ushort)(0x140 - this.introColumn), 0x18,
					BackScreenBox, 0, 0xb0);

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x40, this.introColumn, 0x18,
						BackScreenBox, (ushort)(0x140 - this.introColumn), 0xb0);
				}

				foreach (ushort column in PlanetColumns)
				{
					if (this.introColumn != column)
						continue;

					if (column == 0x32 || column == 0x74 || column == 0x136)
						this.Owner.Words.Clear();
					else
						this.Owner.Libc.ScanStream((short)this.Owner.GlobD768, "%[^\n]\n", TextBuffer.Address);
				}

				if (this.Owner.Words.Length != 0)
					DrawCreditLine();

				FlickerCgaScreen();

				if ((short)this.introColumn < 0x140)
				{
					this.Owner.VideoCard.RenderImage(BackScreenBox,
						this.introColumn, 0, (ushort)(0x140 - this.introColumn), 0x40,
						ScreenBox, 0, 0xc);
				}

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0, this.introColumn, 0x40,
						ScreenBox, (ushort)(0x140 - this.introColumn), 0xc);
				}

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
					ScreenBox, 0, 0x4c);

				if ((short)this.introColumn < 0x140)
				{
					this.Owner.VideoCard.RenderImage(BackScreenBox,
						this.introColumn, 0x58, (ushort)(0x140 - this.introColumn), 0x58,
						ScreenBox, 0, 0x64);
				}

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x58, this.introColumn, 0x58,
						ScreenBox, (ushort)(0x140 - this.introColumn), 0x64);
				}

				ShowCgaScreen();

				// The music of this part starts 0x1a40 / 0x15 columns further; a key stops the waiting
				if (MusicPassedColumn(0x1a40) && (this.introColumn & 1) == 0)
					this.introColumn++;

				while (true)
				{
					if ((ushort)this.Owner.Libc.KeyAvailable() != 0 || MusicReachedColumn(0x1a40))
						break;
				}

				if ((ushort)this.Owner.Libc.KeyAvailable() != 0)
					this.introColumn = 0x3e7;
			}
		}

		/// <summary>The column of the intro the music is at: (0x15 * column + start) / 4, in 16 bits with the sign</summary>
		private short MusicColumnMark(ushort start)
		{
			short scaled = (short)(0x15 * (short)this.introColumn + start);

			return (short)(scaled / 4);
		}

		/// <summary>The music is already past the current column</summary>
		private bool MusicPassedColumn(ushort start)
		{
			short song = (short)this.Owner.Services.ReadSongPosition();

			return MusicColumnMark(start) < song;
		}

		/// <summary>The music has reached the current column</summary>
		private bool MusicReachedColumn(ushort start)
		{
			short song = (short)this.Owner.Services.ReadSongPosition();

			return MusicColumnMark(start) <= song;
		}

		/// <summary>The line of the credits (TextBuffer.Address) in the band at the bottom, with its shadow, drawn into screen 0 too</summary>
		private void DrawCreditLine()
		{
			ushort screen = ScreenBox;
			this.introSavedFlag = this.Processor.PullUInt16(this.Processor.SegData.Value16, screen);
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, 1);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 184, (ushort)(VgaMode != 0 ? 0xfc : 0x7));
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 182, (ushort)(VgaMode != 0 ? 0xf8 : 0xf));
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 183, (ushort)(VgaMode != 0 ? 0xfa : 0xf));

			screen = ScreenBox;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, this.introSavedFlag);
		}

		/// <summary>Without VGA the flag of screen 0 switches between 0 and 3 every column</summary>
		private void FlickerCgaScreen()
		{
			if (VgaMode != 0)
				return;

			ushort screen = ScreenBox;
			ushort flag = (ushort)(this.Processor.PullUInt16(this.Processor.SegData.Value16, screen) != 0 ? 0 : 3);
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, flag);
		}

		/// <summary>Without VGA screen 0 is shown after a dot drawn with its flag 0</summary>
		private void ShowCgaScreen()
		{
			if (VgaMode != 0)
				return;

			ushort screen = ScreenBox;
			this.introSavedFlag = this.Processor.PullUInt16(this.Processor.SegData.Value16, screen);
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, 0);

			this.Owner.Services.AssignDot(0, 0, 0);

			screen = ScreenBox;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, this.introSavedFlag);

			this.Owner.Services.ShowScreen(this.introSavedFlag);
		}

		/// <summary>
		/// The key pressed during the intro chooses from the main menu (N, L, E, C); without a key the title logo is shown
		/// and the intro waits for one
		/// </summary>
		private void TakeIntroChoice()
		{
			MainMenuChoice = 0xfffe;

			if ((ushort)this.Owner.Libc.KeyAvailable() != 0)
			{
				MainMenuChoice = 0xffff;

				switch (this.Owner.Dialogs.ReadNavigationKey())
				{
					case 'N':
					case 'n':
						MainMenuChoice = 0;
						break;

					case 'L':
					case 'l':
						MainMenuChoice = 1;
						break;

					case 'E':
					case 'e':
						MainMenuChoice = 2;
						break;

					case 'C':
					case 'c':
						MainMenuChoice = 3;
						break;
				}
			}

			if (MainMenuChoice == 0xfffe)
				ShowTitleLogo();
		}

		/// <summary>When the music reaches 0xd00 the logo is wiped in, 4 columns at a time; then a key ends the intro</summary>
		private void ShowTitleLogo()
		{
			do
			{
			}
			while ((short)this.Owner.Services.ReadSongPosition() < 0xd00);

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			this.Owner.Services.DrawPictureToScreen(BackScreenBox, 0, 64, this.introLogo);

			if (VgaMode != 0)
			{
				this.Owner.Services.AssignHueCycleSpan(4, 14, 224, 239);
				this.Owner.Services.ActivateHueCycle(4);
			}

			this.introColumn = 0;

			do
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox, this.introColumn, 0x40, 0x4, 0x50,
					ScreenBox, this.introColumn, 0x40);

				this.Owner.TextPainter.WaitForTime(1);

				this.introColumn = (ushort)(this.introColumn + 4);
			}
			while ((short)this.introColumn < 0x140);

			for (this.introColumn = 0; (short)this.introColumn < 0x64; this.introColumn++)
			{
				if ((ushort)this.Owner.Libc.KeyAvailable() != 0)
					break;

				this.Owner.TextPainter.WaitForTime(5);
			}

			this.Owner.TurnLogic.WaitForKey();

			MainMenuChoice = 0xffff;
		}

		/// <summary>Stops the colour cycles, leaves the title logo on screen 0 and lets the picture of the logo go</summary>
		private void CloseIntro()
		{
			if (VgaMode != 0)
			{
				for (this.introColumn = 1;
					(short)this.introColumn <= 4;
					this.introColumn++)
				{
					this.Owner.Services.DeactivateHueCycle(this.introColumn);
				}
			}
			else
			{
				DrawingBox.DisplayIdx = 0;
			}

			this.Owner.Services.FloodBox(BackScreenBox, 0, 0, 320, 200, Hues.Black);
			this.Owner.Services.DrawPictureToScreen(BackScreenBox, 0, 64, this.introLogo);
			this.Owner.VideoCard.DrawScreenToMainScreen(1, 0x14);
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);
			this.Owner.Services.ShowScreen(0);

			DrawingBox.TypefaceIdx = 1;

			this.Owner.Helpers.ReleaseResource(this.introLogo, 0);
		}

		/// <summary>The text into screenText (strcpy; what does not fit is lost, the frame words are in C#)</summary>
		private void SetScreenText(string text)
		{
			for (int idx = 0; idx <= text.Length && idx < this.screenText.Length; idx++)
				this.screenText[idx] = idx < text.Length ? (byte)text[idx] : (byte)0;
		}

		/// <summary>The text of screenText from the index on</summary>
		private string ScreenTextFrom(int index)
		{
			int end = index;

			while (end >= 0 && end < this.screenText.Length && this.screenText[end] != 0)
				end++;

			return index < 0 || index >= this.screenText.Length ? "" : System.Text.Encoding.Latin1.GetString(this.screenText, index, end - index);
		}

		/// <summary>The 24 pictures of the flying ship (6 in a row, 50 dots apart) into the table in the frame, then the background</summary>
		private void LoadShipPictures()
		{
			this.Owner.Startup.AskForDisk(6, 1);
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FilePlanet2Pic, 0);

			this.screenFrame.SetFrame(ScreenColumn, 0);

			do
			{
				ushort picture = this.Owner.VideoCard.ScreenToPicture(1,
					(ushort)(((this.screenFrame.Frame(ScreenColumn) % 6) * 0x32) + 1),
					(ushort)(((this.screenFrame.Frame(ScreenColumn) / 6) * 0x32) + 1),
					0x31, 0x31);

				ushort entry = (ushort)(this.screenFrame.Frame(ScreenColumn) << 1);
				this.screenFrame.SetFrame(ScreenShipPictures - entry, picture);

				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
			}
			while ((short)this.screenFrame.Frame(ScreenColumn) < 0x18);

			this.Owner.Helpers.HaltHueCycling();

			if (VgaMode != 0)
			{
				this.screenFrame.SetFrame(ScreenBackground, 0xec);
				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 236);
			}
			else
			{
				this.screenFrame.SetFrame(ScreenBackground, 0);
				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FilePlanet1Pic, 1);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x18, 0x140, 0xb0,
				BackScreenBox, 0, 0);
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x20, 0x140, 0x18,
				BackScreenBox, 0, 0xb0);

			this.Owner.Startup.AskForDisk(0, 1);

			if (VgaMode == 0)
				this.Owner.Services.FloodBox(SpriteScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, this.screenFrame.Frame(ScreenBackground));
			this.Owner.Services.FloodBox(ScreenBox, 0, 12, 320, 176, this.screenFrame.Frame(ScreenBackground));
		}

		/// <summary>
		/// The code texts (0x30b8: title and name of the human player, 0x30ba: the civilization of the ship, 0x30bc: its
		/// colonists, 0x30be: the year of the launch, 0x30c0: this year), the text of the screen reflowed into the frame
		/// (0x200) and the start of the scrolling
		/// </summary>
		private void PrepareLandingText(short participantIdx)
		{
			ushort regime = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;
			Message(GovernmentTitle(regime))
				.Text(" ");

			ushort person = (ushort)this.Owner.World.PersonParticipantIdx;
			this.Owner.Words.Append(LeaderName(person));
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			this.Owner.Codes.Set(1, CivilizationName((ushort)participantIdx));

			EmptyMessage()
				.Number(this.Owner.World.Participants[participantIdx].StarshipInhabitants);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());

			EmptyMessage()
				.Number(this.Owner.World.Participants[participantIdx].StarshipLiftoffSeason);
			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			EmptyMessage()
				.Number(this.Owner.World.Season);
			this.Owner.Codes.Set(4, this.Owner.Words.ToString());

			this.Owner.BlockText.LoadTextFromFile(GameTexts.FileKingTxtB,
				(ushort)((participantIdx != this.Owner.World.PersonParticipantIdx) ? 0x32f9 : 0x32f2));
			this.Owner.BlockText.ReflowText();
			SetScreenText(this.Owner.Words.ToString());

			DrawingBox.TypefaceIdx = 6;
			this.screenFrame.SetFrame(ScreenStarHoriz, 0xfff6);
			this.screenFrame.SetFrame(ScreenStarVert, 0x5c);
			this.screenFrame.SetFrame(ScreenPartEnd, 0xffff);
			this.screenFrame.SetFrame(ScreenPartBegin, 0xffff);

			this.screenFrame.SetFrame(ScreenTextLength, this.Owner.Words.Length);

			this.Owner.Services.PerformSound(3, 0);

			this.Owner.Words.Clear();
		}

		/// <summary>
		/// The ship flies in from the right, column after column, in time with the timer (0x5c), a star (a dot) moving along;
		/// every 22 columns the next part of the text (up to "^") is shown in the band at the bottom
		/// </summary>
		private void FlyShip()
		{
			for (this.screenFrame.SetFrame(ScreenColumn, 0); (short)this.screenFrame.Frame(ScreenColumn) < 0x140; )
			{
				this.Owner.Services.RestoreTimer();

				if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
				{
					this.Owner.Services.FloodBox(BackScreenBox, 0, 176,
						320 - (short)this.screenFrame.Frame(ScreenColumn), 24, this.screenFrame.Frame(ScreenBackground));
				}

				if (this.screenFrame.Frame(ScreenColumn) != 0)
				{
					this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x14, this.screenFrame.Frame(ScreenColumn), 0x18,
						BackScreenBox, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0xb0);
				}

				if ((short)this.screenFrame.Frame(ScreenColumn) % 0x16 == 0)
					CutNextTextPart(true);

				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
					DrawLandingTextPart();

				DrawShipFrame();
				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
			}
		}

		/// <summary>
		/// The next part of the text (of the message buffer, or of its copy screenText): from after the end of the last part
		/// up to "^" (made the end of the text) or the end of the whole text
		/// </summary>
		private void CutNextTextPart(bool inBuffer)
		{
			this.screenFrame.SetFrame(ScreenPartBegin, (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1));

			ushort end;

			while (true)
			{
				this.screenFrame.SetFrame(ScreenPartEnd, (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1));
				end = this.screenFrame.Frame(ScreenPartEnd);

				if (PullTextByte(inBuffer, end) == 0x5e || (short)end >= (short)this.screenFrame.Frame(ScreenTextLength))
					break;
			}

			if (inBuffer)
			{
				if ((short)end < (short)this.screenFrame.Frame(ScreenTextLength))
					EmitTextByte(inBuffer, end, 0);
			}
			else
			{
				// The copy in the frame reads the end of the part again
				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
					EmitTextByte(inBuffer, this.screenFrame.Frame(ScreenPartEnd), 0);
			}
		}

		/// <summary>A letter of the text in the message buffer or of its copy screenText (0 past its end)</summary>
		private byte PullTextByte(bool inBuffer, ushort idx)
		{
			return inBuffer ? this.Owner.Words[idx] : idx < this.screenText.Length ? this.screenText[idx] : (byte)0;
		}

		private void EmitTextByte(bool inBuffer, ushort idx, byte letter)
		{
			if (inBuffer)
				this.Owner.Words[idx] = letter;
			else if (idx < this.screenText.Length)
				this.screenText[idx] = letter;
		}

		/// <summary>The current part of the text in the band at the bottom, with its shadow, drawn into screen 0 too</summary>
		private void DrawLandingTextPart()
		{
			ushort screen = ScreenBox;
			this.screenFrame.SetFrame(ScreenSavedFlag, this.Processor.PullUInt16(this.Processor.SegData.Value16, screen));
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, 1);

			ushort hue = (ushort)(VgaMode != 0 ? 0x38 : 0x7);
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.TextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 184, hue);

			hue = (ushort)(VgaMode != 0 ? 0x3c : 0xf);
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.TextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 183, hue);

			screen = ScreenBox;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, this.screenFrame.Frame(ScreenSavedFlag));
		}

		/// <summary>
		/// The next of the 23 frames of the ship, the space and the band scrolled in by the column, the star, and the wait
		/// for the timer (8 beats; a column more when it is past 12 beats on an even column)
		/// </summary>
		private void DrawShipFrame()
		{
			this.screenFrame.SetFrame(ScreenShipFrame, (ushort)(this.screenFrame.Frame(ScreenShipFrame) + 1));

			if ((short)this.screenFrame.Frame(ScreenShipFrame) >= 0x17)
				this.screenFrame.SetFrame(ScreenShipFrame, 0);

			this.Owner.Services.DrawPictureToScreen(BackScreenBox, 135, 51,
				this.screenFrame.Frame(ScreenShipPictures - (this.screenFrame.Frame(ScreenShipFrame) << 1)));

			FlickerCgaScreen();

			if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
			{
				this.Owner.Services.FloodBox(ScreenBox, 0, 12, 319, 20, this.screenFrame.Frame(ScreenBackground));
			}

			if (this.screenFrame.Frame(ScreenColumn) != 0)
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, this.screenFrame.Frame(ScreenColumn), 0x14,
					ScreenBox, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0xc);
			}

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
				ScreenBox, 0, 0x20);

			if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
			{
				this.Owner.Services.FloodBox(ScreenBox, 0, 56,
					320 - (short)this.screenFrame.Frame(ScreenColumn), 132, this.screenFrame.Frame(ScreenBackground));
			}

			if (this.screenFrame.Frame(ScreenColumn) != 0)
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x2c, this.screenFrame.Frame(ScreenColumn), 0x84,
					ScreenBox, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0x38);
			}

			// The star moves one dot every column, two on odd columns (where it is drawn)
			if ((this.screenFrame.FrameByte(ScreenColumn) & 1) != 0)
			{
				this.screenFrame.SetFrame(ScreenStarHoriz, (ushort)(this.screenFrame.Frame(ScreenStarHoriz) + 1));
				this.Owner.Services.AssignDot(
					(short)this.screenFrame.Frame(ScreenStarHoriz) - (short)this.screenFrame.Frame(ScreenColumn),
					(short)this.screenFrame.Frame(ScreenStarVert), 15);
			}

			this.screenFrame.SetFrame(ScreenStarHoriz, (ushort)(this.screenFrame.Frame(ScreenStarHoriz) + 1));

			if (VgaMode == 0)
			{
				ushort screen = ScreenBox;
				this.screenFrame.SetFrame(ScreenSavedFlag, this.Processor.PullUInt16(this.Processor.SegData.Value16, screen));
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, 0);

				this.Owner.Services.AssignDot(0, 0, 0);

				screen = ScreenBox;
				ushort savedFlag = this.screenFrame.Frame(ScreenSavedFlag);
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, screen, savedFlag);

				this.Owner.Services.ShowScreen(savedFlag);
			}

			if ((short)this.Owner.Timer.Beats > 0xc && (this.screenFrame.FrameByte(ScreenColumn) & 1) == 0)
				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));

			while ((short)this.Owner.Timer.Beats < 8)
			{
			}
		}

		/// <summary>
		/// After the flight: the text of the screen is taken from the copy in the frame (from the part after the last one
		/// shown), and the landing of the human player's ship (with its colour cycles) or the palace of another player is shown
		/// </summary>
		private void ShowLanding(short participantIdx)
		{
			this.Owner.Helpers.ReleaseResource(this.screenFrame.Frame(ScreenShipPictures), 0);

			DrawingBox.DisplayIdx = 0;
			this.Owner.Services.ShowScreen(0);

			SetScreenText(this.Owner.Words.TextFrom((ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1)));

			ushort shown = (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1);
			this.screenFrame.SetFrame(ScreenTextLength, (ushort)(this.screenFrame.Frame(ScreenTextLength) - shown));
			this.screenFrame.SetFrame(ScreenPartEnd, 0xffff);

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Throne.LoadPalaceGraphics(1);
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				return;
			}

			this.Owner.Startup.AskForDisk(6, 1);
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpacestPic, 0);
			this.Owner.Services.PerformSound(1, 0);

			if (VgaMode != 0)
				this.Owner.Services.FillColors(5, 0, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSpacestPicB, 1);
			this.Owner.Startup.AskForDisk(0, 1);
			this.Owner.Services.PerformSound(0x22, 0);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			if (VgaMode == 0)
				return;

			this.Owner.Services.AssignHueCycleSpan(1, 15, 16, 31);
			this.Owner.Services.AssignHueCycleSpan(2, 100, 32, 34);
			this.Owner.Services.AssignHueCycleSpan(3, 42, 35, 40);
			this.Owner.Services.AssignHueCycleSpan(4, 300, 41, 47);
			this.Owner.Services.AssignHueCycleSpan(5, 60, 48, 53);
			this.Owner.Services.AssignHueCycleSpan(6, 37, 54, 63);

			this.screenFrame.SetFrame(ScreenColumn, 1);

			do
			{
				this.Owner.Services.ActivateHueCycle(this.screenFrame.Frame(ScreenColumn));
				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
			}
			while ((short)this.screenFrame.Frame(ScreenColumn) <= 6);
		}

		/// <summary>The rest of the text, part after part (up to "^"), at the top of the screen with a blinking dot after each part</summary>
		private void DrawClosingText()
		{
			do
			{
				CutNextTextPart(false);

				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
				{
					this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x20,
						ScreenBox, 0, 0);

					this.Owner.TextPainter.DrawCenteredTextToScreen0(ScreenTextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 5, Hues.LightCyan);
					this.Owner.TextPainter.DrawCenteredTextToScreen0(ScreenTextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 4, Hues.White);

					this.Owner.Services.AssignDot(280, 32, 15);
					this.Owner.TextPainter.WaitForTime(80);
					this.Owner.Services.AssignDot(280, 32, 0);
					this.Owner.TextPainter.WaitForTime(80);
				}

				textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) >= textLength)
					break;
			}
			while (true);
		}

		/// <summary>A key closes the screen: colour cycles off, sound, the credits file closed, and the scenery of the civilization</summary>
		private void CloseLanding(short participantIdx)
		{
			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			if (participantIdx == this.Owner.World.PersonParticipantIdx && VgaMode != 0)
			{
				this.screenFrame.SetFrame(ScreenColumn, 1);

				do
				{
					this.Owner.Services.DeactivateHueCycle(this.screenFrame.Frame(ScreenColumn));
					this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
				}
				while ((short)this.screenFrame.Frame(ScreenColumn) <= 6);
			}

			DrawingBox.DisplayIdx = 0;
			this.Owner.Services.PerformSound(1, 0);
			this.Owner.Libc.CloseStream((short)this.Owner.GlobD768);

			DrawingBox.TypefaceIdx = 1;

			if (VgaMode == 0)
			{
				this.Owner.Startup.AskForDisk(7, 1);
				this.Owner.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPic, 0);
				this.Owner.Startup.AskForDisk(0, 1);
			}

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			this.Owner.TurnHooks.ShowCivilizationScenery();
			this.Owner.Helpers.SetupHueCycling();
		}
	}
}
