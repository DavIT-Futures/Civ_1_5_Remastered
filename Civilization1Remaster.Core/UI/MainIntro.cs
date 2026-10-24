using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class MainIntro : GameModule
	{
		public MainIntro(GameEngine owner)
			: base(owner)
		{
		}

		// Local variables of DisplayMainIntro in its stack frame. The intro waits for the music and the timer, and the timer of
		// the tests ticks after a number of memory accesses, so the variables stay in the frame and are read as often as in
		// the original code.
		private const int IntroScreenCount = 0x2;
		private const int IntroSavedFlag = 0x44;
		private const int IntroColumn = 0x48;
		private const int IntroPathFound = 0x4c;
		private const int IntroLogo = 0x4e;

		/// <summary>Columns of the first part of the intro at which a line of credits.txt is read, or (0x32, 0xa0) the line is taken away</summary>
		private static readonly ushort[] CreditColumns = [0xa, 0x32, 0x46, 0x78, 0xa0, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		/// <summary>Columns of the second part of the intro at which a line of credits.txt is read, or (0x32, 0x74, 0x136) the line is taken away</summary>
		private static readonly ushort[] PlanetColumns = [0xa, 0x1e, 0x32, 0x40, 0x54, 0x74, 0x82, 0x96, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		/// <summary>
		/// The intro of the game: finds the disks and the graphics driver, reserves the screens, starts the sound and the
		/// timer, then scrolls the credits (credits.txt) and the planet in time with the music, shows the title logo and
		/// takes the choice of the main menu from the key pressed during the intro (0x6b32: 0 - 3, 0xffff without a choice)
		/// </summary>
		public void DisplayMainIntro()
		{
			this.processor.Journal.OpenScope("DisplayMainIntro()");

			this.processor.EnterFrame(0x4e);
			this.processor.PreserveSource();

			FindIntroDisks();
			DetectGraphicsDriver();
			ReserveIntroScreens();
			LoadIntroPictures();
			ScrollCredits();
			ScrollPlanet();
			TakeIntroChoice();
			CloseIntro();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("DisplayMainIntro");
		}

		/// <summary>The disks 1 - 7 that are present one after the other (0xdeba: the first one missing), then waits for the path 0x324a</summary>
		private void FindIntroDisks()
		{
			FirstMissingDisk = 1;
			this.processor.EmitLocalWord(IntroColumn, 1);

			do
			{
				if (this.owner.Startup.QueryForDisk(this.processor.PullLocalWord(IntroColumn), 0) == 0)
					break;

				FirstMissingDisk = (ushort)(this.processor.PullLocalWord(IntroColumn) + 1);
				this.processor.IncrementLocalWord(IntroColumn);
			}
			while ((short)this.processor.PullLocalWord(IntroColumn) < 8);

			this.processor.EmitLocalWord(IntroPathFound, 0);

			do
			{
				this.owner.Services.PathPresent(0x324a);
				this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 == 0xffff ? 0 : 1);
				this.processor.EmitLocalWord(IntroPathFound, this.processor.RegAcc.Value16);
			}
			while (this.processor.PullLocalWord(IntroPathFound) == 0);
		}

		/// <summary>The first letter of the driver name (0x1a22): m = VGA / MCGA (0xd762), e = EGA (0xb20e)</summary>
		private void DetectGraphicsDriver()
		{
			bool colourful = this.processor.PullGlobalByte(0x1a22) == 0x6d || this.processor.PullGlobalByte(0x1a22) == 0x4d;
			this.processor.RegAcc.Value16 = (ushort)(colourful ? 1 : 0);
			VgaMode = this.processor.RegAcc.Value16;

			bool ega = this.processor.PullGlobalByte(0x1a22) == 0x65 || this.processor.PullGlobalByte(0x1a22) == 0x45;
			this.processor.RegAcc.Value16 = (ushort)(ega ? 1 : 0);
			EgaMode = this.processor.RegAcc.Value16;

			// Change Num-Lock state
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 1);
		}

		/// <summary>Screens 0 - 2 (VGA) or 0 - 3; 0x6dfc is set when one of them can't be reserved</summary>
		private void ReserveIntroScreens()
		{
			ScreenReserveFailed = 0;
			this.processor.RegAcc.Value16 = (ushort)(VgaMode != 0 ? 3 : 4);
			this.processor.EmitLocalWord(IntroScreenCount, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(IntroColumn, 0);

			while (true)
			{
				short screenCount = (short)this.processor.PullLocalWord(IntroScreenCount);

				if ((short)this.processor.PullLocalWord(IntroColumn) >= screenCount)
					break;

				this.processor.EmitGlobalWord(0x32d6, this.owner.VideoCard.ReserveScreen(this.processor.PullLocalWord(IntroColumn)));

				if (this.processor.RegAcc.Value16 == 0)
					ScreenReserveFailed = 1;

				this.processor.EmitLocalWord(IntroColumn, (ushort)(this.processor.PullLocalWord(IntroColumn) + 1));
			}
		}

		/// <summary>Sound, timer, the pictures of the intro (the logo is kept in a picture), the colour cycles and credits.txt</summary>
		private void LoadIntroPictures()
		{
			this.owner.Services.PrepareSoundDriver();
			this.owner.Services.LaunchTimer();
			this.owner.Helpers.SeedRandomizer();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			if (VgaMode == 0)
				this.owner.Services.FloodBox(SpriteScreenBox, 0, 0, 320, 200, 0);

			this.owner.VideoCard.AssignHuesByIndexArray(0x19fe);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x328a, 1);
			this.processor.EmitLocalWord(IntroLogo, this.owner.VideoCard.ScreenToPicture(1, 0, 64, 320, 80));

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3293, 1);
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x4c, 0x140, 0x18,
				BackScreenBox, 0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(2, 0, 0, 0x329e, 1);
			this.owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x4c, 0x140, 0x18,
				PictureScreenBox, 0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x32a9, 1);
			this.owner.Services.FloodBox(ScreenBox, 0, 12, 320, 176, 0);
			this.owner.Services.AssignHueCycleSpan(1, 14, 146, 152);

			if (VgaMode != 0)
			{
				this.processor.EmitLocalWord(IntroColumn, 1);

				do
				{
					this.owner.Services.ActivateHueCycle(this.processor.PullLocalWord(IntroColumn));
					this.processor.IncrementLocalWord(IntroColumn);
				}
				while ((short)this.processor.PullLocalWord(IntroColumn) <= 3);
			}

			this.owner.World.SessionPrefMarkers |= 0x10;

			this.owner.GlobD768 = (ushort)this.owner.Libc.OpenStream("credits.txt", "rt");

			this.owner.Services.RestoreTimer();
			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0);
		}

		/// <summary>
		/// The first part: the picture comes in from the right, column after column, in time with the music, with the lines
		/// of the credits read at some columns; a key ends the part
		/// </summary>
		private void ScrollCredits()
		{
			for (this.processor.EmitLocalWord(IntroColumn, 0);
				(short)this.processor.PullLocalWord(IntroColumn) < 0x140;
				this.processor.EmitLocalWord(IntroColumn, (ushort)(this.processor.PullLocalWord(IntroColumn) + 1)))
			{
				ushort rest = (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn));

				this.owner.Services.FloodBox(BackScreenBox, 0, 176, (short)rest, 24, 0);
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x40, this.processor.PullLocalWord(IntroColumn), 0x18,
					BackScreenBox, rest, 0xb0);

				foreach (ushort column in CreditColumns)
				{
					if (this.processor.PullLocalWord(IntroColumn) != column)
						continue;

					if (column == 0x32 || column == 0xa0)
					{
						this.processor.EmitGlobalByte(0xba06, 0);
					}
					else
					{
						if (column == 0xa)
							this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 5);

						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
					}
				}

				if (this.owner.Libc.TextLength(0xba06) != 0)
					RenderCreditLine();

				FlickerCgaScreen();

				rest = (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn));

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, this.processor.PullLocalWord(IntroColumn), 0x40,
					ScreenBox, rest, 0xc);
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
					ScreenBox, 0, 0x4c);
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x58, this.processor.PullLocalWord(IntroColumn), 0x58,
					ScreenBox, rest, 0x64);

				ShowCgaScreen();

				// Behind the music: one column more (only from an even column); then wait until the music reaches the column
				if (MusicPassedColumn(0) && (this.processor.PullLocalByte(IntroColumn) & 1) == 0)
					this.processor.IncrementLocalWord(IntroColumn);

				while (!MusicReachedColumn(0))
				{
				}

				if ((ushort)this.owner.Libc.KeyAvailable() != 0)
					this.processor.EmitLocalWord(IntroColumn, 0x3e7);
			}
		}

		/// <summary>
		/// The second part: the picture of screen 2 pushes the first one out to the left, column after column, in time with
		/// the music, with more lines of the credits; a key ends the part
		/// </summary>
		private void ScrollPlanet()
		{
			for (this.processor.EmitLocalWord(IntroColumn, 0);
				(short)this.processor.PullLocalWord(IntroColumn) < 0x140;
				this.processor.EmitLocalWord(IntroColumn, (ushort)(this.processor.PullLocalWord(IntroColumn) + 1)))
			{
				this.owner.VideoCard.RenderImage(BackScreenBox,
					this.processor.PullLocalWord(IntroColumn), 0x40, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0x18,
					BackScreenBox, 0, 0xb0);

				if (this.processor.PullLocalWord(IntroColumn) != 0)
				{
					this.owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x40, this.processor.PullLocalWord(IntroColumn), 0x18,
						BackScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0xb0);
				}

				foreach (ushort column in PlanetColumns)
				{
					if (this.processor.PullLocalWord(IntroColumn) != column)
						continue;

					if (column == 0x32 || column == 0x74 || column == 0x136)
						this.processor.EmitGlobalByte(0xba06, 0);
					else
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
				}

				if (this.owner.Libc.TextLength(0xba06) != 0)
					RenderCreditLine();

				FlickerCgaScreen();

				if ((short)this.processor.PullLocalWord(IntroColumn) < 0x140)
				{
					this.owner.VideoCard.RenderImage(BackScreenBox,
						this.processor.PullLocalWord(IntroColumn), 0, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0x40,
						ScreenBox, 0, 0xc);
				}

				if (this.processor.PullLocalWord(IntroColumn) != 0)
				{
					this.owner.VideoCard.RenderImage(PictureScreenBox, 0, 0, this.processor.PullLocalWord(IntroColumn), 0x40,
						ScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0xc);
				}

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
					ScreenBox, 0, 0x4c);

				if ((short)this.processor.PullLocalWord(IntroColumn) < 0x140)
				{
					this.owner.VideoCard.RenderImage(BackScreenBox,
						this.processor.PullLocalWord(IntroColumn), 0x58, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0x58,
						ScreenBox, 0, 0x64);
				}

				if (this.processor.PullLocalWord(IntroColumn) != 0)
				{
					this.owner.VideoCard.RenderImage(PictureScreenBox, 0, 0x58, this.processor.PullLocalWord(IntroColumn), 0x58,
						ScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(IntroColumn)), 0x64);
				}

				ShowCgaScreen();

				// The music of this part starts 0x1a40 / 0x15 columns further; a key stops the waiting
				if (MusicPassedColumn(0x1a40) && (this.processor.PullLocalByte(IntroColumn) & 1) == 0)
					this.processor.IncrementLocalWord(IntroColumn);

				while (true)
				{
					if ((ushort)this.owner.Libc.KeyAvailable() != 0 || MusicReachedColumn(0x1a40))
						break;
				}

				if ((ushort)this.owner.Libc.KeyAvailable() != 0)
					this.processor.EmitLocalWord(IntroColumn, 0x3e7);
			}
		}

		/// <summary>The column of the intro the music is at: (0x15 * column + start) / 4, in 16 bits with the sign</summary>
		private short MusicColumnMark(ushort start)
		{
			short scaled = (short)(0x15 * (short)this.processor.PullLocalWord(IntroColumn) + start);

			return (short)(scaled / 4);
		}

		/// <summary>The music is already past the current column</summary>
		private bool MusicPassedColumn(ushort start)
		{
			short song = (short)this.owner.Services.ObtainSongPosition();

			return MusicColumnMark(start) < song;
		}

		/// <summary>The music has reached the current column</summary>
		private bool MusicReachedColumn(ushort start)
		{
			short song = (short)this.owner.Services.ObtainSongPosition();

			return MusicColumnMark(start) <= song;
		}

		/// <summary>The line of the credits (0xba06) in the band at the bottom, with its shadow, drawn into screen 0 too</summary>
		private void RenderCreditLine()
		{
			ushort screen = ScreenBox;
			this.processor.EmitLocalWord(IntroSavedFlag, this.processor.PullUInt16(this.processor.SegData.Value16, screen));
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, 1);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 184, (ushort)(VgaMode != 0 ? 0xfc : 0x7));
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 182, (ushort)(VgaMode != 0 ? 0xf8 : 0xf));
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 183, (ushort)(VgaMode != 0 ? 0xfa : 0xf));

			screen = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, this.processor.PullLocalWord(IntroSavedFlag));
		}

		/// <summary>Without VGA the flag of screen 0 switches between 0 and 3 every column</summary>
		private void FlickerCgaScreen()
		{
			if (VgaMode != 0)
				return;

			ushort screen = ScreenBox;
			ushort flag = (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, screen) != 0 ? 0 : 3);
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, flag);
		}

		/// <summary>Without VGA screen 0 is shown after a dot drawn with its flag 0</summary>
		private void ShowCgaScreen()
		{
			if (VgaMode != 0)
				return;

			ushort screen = ScreenBox;
			this.processor.EmitLocalWord(IntroSavedFlag, this.processor.PullUInt16(this.processor.SegData.Value16, screen));
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, 0);

			this.owner.Services.AssignDot(0, 0, 0);

			screen = ScreenBox;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(IntroSavedFlag);
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, this.processor.RegAcc.Value16);

			this.owner.Services.DisplayScreen(this.processor.RegAcc.Value16);
		}

		/// <summary>
		/// The key pressed during the intro chooses from the main menu (N, L, E, C); without a key the title logo is shown
		/// and the intro waits for one
		/// </summary>
		private void TakeIntroChoice()
		{
			MainMenuChoice = 0xfffe;

			if ((ushort)this.owner.Libc.KeyAvailable() != 0)
			{
				MainMenuChoice = 0xffff;

				switch (this.owner.Dialogs.ObtainNavigationKey())
				{
					case 0x4e:
					case 0x6e:
						MainMenuChoice = 0;
						break;

					case 0x4c:
					case 0x6c:
						MainMenuChoice = 1;
						break;

					case 0x45:
					case 0x65:
						MainMenuChoice = 2;
						break;

					case 0x43:
					case 0x63:
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
			while ((short)this.owner.Services.ObtainSongPosition() < 0xd00);

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			this.owner.Services.RenderPictureToScreen(BackScreenBox, 0, 64, this.processor.PullLocalWord(IntroLogo));

			if (VgaMode != 0)
			{
				this.owner.Services.AssignHueCycleSpan(4, 14, 224, 239);
				this.owner.Services.ActivateHueCycle(4);
			}

			this.processor.EmitLocalWord(IntroColumn, 0);

			do
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, this.processor.PullLocalWord(IntroColumn), 0x40, 0x4, 0x50,
					ScreenBox, this.processor.PullLocalWord(IntroColumn), 0x40);

				this.owner.TextPainter.AwaitTime(1);

				this.processor.EmitLocalWord(IntroColumn, (ushort)(this.processor.PullLocalWord(IntroColumn) + 4));
			}
			while ((short)this.processor.PullLocalWord(IntroColumn) < 0x140);

			for (this.processor.EmitLocalWord(IntroColumn, 0); (short)this.processor.PullLocalWord(IntroColumn) < 0x64; this.processor.IncrementLocalWord(IntroColumn))
			{
				if ((ushort)this.owner.Libc.KeyAvailable() != 0)
					break;

				this.owner.TextPainter.AwaitTime(5);
			}

			this.owner.TurnLogic.AwaitForKey();

			MainMenuChoice = 0xffff;
		}

		/// <summary>Stops the colour cycles, leaves the title logo on screen 0 and lets the picture of the logo go</summary>
		private void CloseIntro()
		{
			if (VgaMode != 0)
			{
				for (this.processor.EmitLocalWord(IntroColumn, 1);
					(short)this.processor.PullLocalWord(IntroColumn) <= 4;
					this.processor.EmitLocalWord(IntroColumn, (ushort)(this.processor.PullLocalWord(IntroColumn) + 1)))
				{
					this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(IntroColumn));
				}
			}
			else
			{
				this.processor.EmitUInt16(this.processor.SegData.Value16, ScreenBox, 0);
			}

			this.owner.Services.FloodBox(BackScreenBox, 0, 0, 320, 200, 0);
			this.owner.Services.RenderPictureToScreen(BackScreenBox, 0, 64, this.processor.PullLocalWord(IntroLogo));
			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x14);
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);
			this.owner.Services.DisplayScreen(0);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 1);

			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(IntroLogo), 0);
		}

		// Local variables of DisplayCivilizationScreen in its stack frame (they stay there: the screen waits for the timer, the
		// timer of the tests ticks after a number of memory accesses, and the frame of the ship animation 0x208 is never set
		// before it is first read, so it starts with what an earlier function left on the stack)
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

		/// <summary>
		/// The end of the space race: the ship of the player (the human player or another one) lands on Alpha Centauri. The
		/// pictures of the flying ship come from 0x32d8, the text (file 0x3301, part 0x32f2 for the human player, else 0x32f9)
		/// scrolls in parts with the ship, then the landing (or the palace of another player) is shown with the closing text,
		/// line after line, and a key closes the screen
		/// </summary>
		public void DisplayCivilizationScreen(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationScreen({participantIdx})");

			this.processor.EnterFrame(0x246);
			this.processor.PreserveSource();

			ImportShipPictures();
			PrepareLandingText(participantIdx);
			FlyShip();
			ShowLanding(participantIdx);
			RenderClosingText();
			CloseLanding(participantIdx);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("DisplayCivilizationScreen");
		}

		/// <summary>The 24 pictures of the flying ship (6 in a row, 50 dots apart) into the table in the frame, then the background</summary>
		private void ImportShipPictures()
		{
			this.owner.Startup.QueryForDisk(6, 1);
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32d8, 0);

			this.processor.EmitLocalWord(ScreenColumn, 0);

			do
			{
				ushort picture = this.owner.VideoCard.ScreenToPicture(1,
					(ushort)(((this.processor.PullLocalWord(ScreenColumn) % 6) * 0x32) + 1),
					(ushort)(((this.processor.PullLocalWord(ScreenColumn) / 6) * 0x32) + 1),
					0x31, 0x31);

				ushort entry = (ushort)(this.processor.PullLocalWord(ScreenColumn) << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + entry - ScreenShipPictures), picture);

				this.processor.IncrementLocalWord(ScreenColumn);
			}
			while ((short)this.processor.PullLocalWord(ScreenColumn) < 0x18);

			this.owner.Helpers.HaltHueCycling();

			if (VgaMode != 0)
			{
				this.processor.EmitLocalWord(ScreenBackground, 0xec);
				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 236);
			}
			else
			{
				this.processor.EmitLocalWord(ScreenBackground, 0);
				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32e4, 1);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x18, 0x140, 0xb0,
				BackScreenBox, 0, 0);
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x20, 0x140, 0x18,
				BackScreenBox, 0, 0xb0);

			this.owner.Startup.QueryForDisk(0, 1);

			if (VgaMode == 0)
				this.owner.Services.FloodBox(SpriteScreenBox, 0, 0, 320, 200, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, this.processor.PullLocalWord(ScreenBackground));
			this.owner.Services.FloodBox(ScreenBox, 0, 12, 320, 176, this.processor.PullLocalWord(ScreenBackground));
		}

		/// <summary>
		/// The code texts (0x30b8: title and name of the human player, 0x30ba: the civilization of the ship, 0x30bc: its
		/// colonists, 0x30be: the year of the launch, 0x30c0: this year), the text of the screen reflowed into the frame
		/// (0x200) and the start of the scrolling
		/// </summary>
		private void PrepareLandingText(short participantIdx)
		{
			ushort regime = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			Message(GovernmentTitle(regime))
				.Text(" ");

			ushort person = (ushort)this.owner.World.PersonParticipantIdx;
			this.owner.Libc.AppendText(0xba06, LeaderName(person));
			this.owner.Libc.CopyText(CodeText1, 0xba06);

			this.owner.Libc.CopyText(CodeText2,
				CivilizationName((ushort)participantIdx));

			EmptyMessage()
				.Number(this.owner.World.Participants[participantIdx].StarshipInhabitants);
			this.owner.Libc.CopyText(CodeText3, 0xba06);

			EmptyMessage()
				.Number(this.owner.World.Participants[participantIdx].StarshipLiftoffSeason);
			this.owner.Libc.CopyText(CodeText4, 0xba06);

			EmptyMessage()
				.Number(this.owner.World.Season);
			this.owner.Libc.CopyText(CodeText5, 0xba06);

			this.owner.BlockText.ImportTextFromFile(0x3301,
				(ushort)((participantIdx != this.owner.World.PersonParticipantIdx) ? 0x32f9 : 0x32f2));
			this.owner.BlockText.ReflowText();
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - ScreenText), 0xba06);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 6);
			this.processor.EmitLocalWord(ScreenStarHoriz, 0xfff6);
			this.processor.EmitLocalWord(ScreenStarVert, 0x5c);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(ScreenPartEnd, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(ScreenPartBegin, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(ScreenTextLength, this.owner.Libc.TextLength(0xba06));

			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0);
		}

		/// <summary>
		/// The ship flies in from the right, column after column, in time with the timer (0x5c), a star (a dot) moving along;
		/// every 22 columns the next part of the text (up to "^") is shown in the band at the bottom
		/// </summary>
		private void FlyShip()
		{
			for (this.processor.EmitLocalWord(ScreenColumn, 0); (short)this.processor.PullLocalWord(ScreenColumn) < 0x140; )
			{
				this.owner.Services.RestoreTimer();

				if ((short)this.processor.PullLocalWord(ScreenColumn) < 0x140)
				{
					this.owner.Services.FloodBox(BackScreenBox, 0, 176,
						320 - this.processor.PullLocalSignedWord(ScreenColumn), 24, this.processor.PullLocalWord(ScreenBackground));
				}

				if (this.processor.PullLocalWord(ScreenColumn) != 0)
				{
					this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x14, this.processor.PullLocalWord(ScreenColumn), 0x18,
						BackScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(ScreenColumn)), 0xb0);
				}

				if ((short)this.processor.PullLocalWord(ScreenColumn) % 0x16 == 0)
					CutNextTextPart(0xba06);

				short textLength = (short)this.processor.PullLocalWord(ScreenTextLength);

				if ((short)this.processor.PullLocalWord(ScreenPartEnd) < textLength)
					RenderLandingTextPart();

				RenderShipFrame();
				this.processor.IncrementLocalWord(ScreenColumn);
			}
		}

		/// <summary>
		/// The next part of the text at the address (0xba06, or the frame copy at bp - 0x200 through the stack segment):
		/// from after the end of the last part up to "^" (made the end of the text) or the end of the whole text
		/// </summary>
		private void CutNextTextPart(ushort text)
		{
			this.processor.EmitLocalWord(ScreenPartBegin, (ushort)(this.processor.PullLocalWord(ScreenPartEnd) + 1));

			ushort end;

			while (true)
			{
				this.processor.EmitLocalWord(ScreenPartEnd, (ushort)(this.processor.PullLocalWord(ScreenPartEnd) + 1));
				end = this.processor.PullLocalWord(ScreenPartEnd);

				if (PullTextByte(text, end) == 0x5e || (short)end >= (short)this.processor.PullLocalWord(ScreenTextLength))
					break;
			}

			if (text == 0xba06)
			{
				if ((short)end < (short)this.processor.PullLocalWord(ScreenTextLength))
					EmitTextByte(text, end, 0);
			}
			else
			{
				// The copy in the frame reads the end of the part again
				short textLength = (short)this.processor.PullLocalWord(ScreenTextLength);

				if ((short)this.processor.PullLocalWord(ScreenPartEnd) < textLength)
					EmitTextByte(text, this.processor.PullLocalWord(ScreenPartEnd), 0);
			}
		}

		/// <summary>A letter of the text in the data segment (0xba06) or of the copy in the frame</summary>
		private byte PullTextByte(ushort text, ushort idx)
		{
			return text == 0xba06
				? this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(idx + 0xba06))
				: this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + idx - ScreenText));
		}

		private void EmitTextByte(ushort text, ushort idx, byte letter)
		{
			if (text == 0xba06)
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(idx + 0xba06), letter);
			else
				this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + idx - ScreenText), letter);
		}

		/// <summary>The current part of the text in the band at the bottom, with its shadow, drawn into screen 0 too</summary>
		private void RenderLandingTextPart()
		{
			ushort screen = ScreenBox;
			this.processor.EmitLocalWord(ScreenSavedFlag, this.processor.PullUInt16(this.processor.SegData.Value16, screen));
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, 1);

			ushort hue = (ushort)(VgaMode != 0 ? 0x38 : 0x7);
			this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + this.processor.PullLocalWord(ScreenPartBegin)), 160, 184, hue);

			hue = (ushort)(VgaMode != 0 ? 0x3c : 0xf);
			this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + this.processor.PullLocalWord(ScreenPartBegin)), 160, 183, hue);

			screen = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, screen, this.processor.PullLocalWord(ScreenSavedFlag));
		}

		/// <summary>
		/// The next of the 23 frames of the ship, the space and the band scrolled in by the column, the star, and the wait
		/// for the timer (8 beats; a column more when it is past 12 beats on an even column)
		/// </summary>
		private void RenderShipFrame()
		{
			this.processor.EmitLocalWord(ScreenShipFrame, (ushort)(this.processor.PullLocalWord(ScreenShipFrame) + 1));

			if ((short)this.processor.PullLocalWord(ScreenShipFrame) >= 0x17)
				this.processor.EmitLocalWord(ScreenShipFrame, 0);

			this.owner.Services.RenderPictureToScreen(BackScreenBox, 135, 51,
				this.processor.PullUInt16(this.processor.SegStack.Value16,
					(ushort)(this.processor.RegFrame.Value16 + (this.processor.PullLocalWord(ScreenShipFrame) << 1) - ScreenShipPictures)));

			FlickerCgaScreen();

			if ((short)this.processor.PullLocalWord(ScreenColumn) < 0x140)
			{
				this.owner.Services.FloodBox(ScreenBox, 0, 12, 319, 20, this.processor.PullLocalWord(ScreenBackground));
			}

			if (this.processor.PullLocalWord(ScreenColumn) != 0)
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, this.processor.PullLocalWord(ScreenColumn), 0x14,
					ScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(ScreenColumn)), 0xc);
			}

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0xb0, 0x140, 0x18,
				ScreenBox, 0, 0x20);

			if ((short)this.processor.PullLocalWord(ScreenColumn) < 0x140)
			{
				this.owner.Services.FloodBox(ScreenBox, 0, 56,
					320 - this.processor.PullLocalSignedWord(ScreenColumn), 132, this.processor.PullLocalWord(ScreenBackground));
			}

			if (this.processor.PullLocalWord(ScreenColumn) != 0)
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x2c, this.processor.PullLocalWord(ScreenColumn), 0x84,
					ScreenBox, (ushort)(0x140 - this.processor.PullLocalWord(ScreenColumn)), 0x38);
			}

			// The star moves one dot every column, two on odd columns (where it is drawn)
			if ((this.processor.PullLocalByte(ScreenColumn) & 1) != 0)
			{
				this.processor.IncrementLocalWord(ScreenStarHoriz);
				this.owner.Services.AssignDot(
					this.processor.PullLocalSignedWord(ScreenStarHoriz) - this.processor.PullLocalSignedWord(ScreenColumn),
					this.processor.PullLocalSignedWord(ScreenStarVert), 15);
			}

			this.processor.EmitLocalWord(ScreenStarHoriz, (ushort)(this.processor.PullLocalWord(ScreenStarHoriz) + 1));

			if (VgaMode == 0)
			{
				ushort screen = ScreenBox;
				this.processor.EmitLocalWord(ScreenSavedFlag, this.processor.PullUInt16(this.processor.SegData.Value16, screen));
				this.processor.EmitUInt16(this.processor.SegData.Value16, screen, 0);

				this.owner.Services.AssignDot(0, 0, 0);

				screen = ScreenBox;
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(ScreenSavedFlag);
				this.processor.EmitUInt16(this.processor.SegData.Value16, screen, this.processor.RegAcc.Value16);

				this.owner.Services.DisplayScreen(this.processor.RegAcc.Value16);
			}

			if ((short)this.processor.PullGlobalWord(0x5c) > 0xc && (this.processor.PullLocalByte(ScreenColumn) & 1) == 0)
				this.processor.IncrementLocalWord(ScreenColumn);

			while ((short)this.processor.PullGlobalWord(0x5c) < 8)
			{
			}
		}

		/// <summary>
		/// After the flight: the text of the screen is taken from the copy in the frame (from the part after the last one
		/// shown), and the landing of the human player's ship (with its colour cycles) or the palace of another player is shown
		/// </summary>
		private void ShowLanding(short participantIdx)
		{
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(ScreenShipPictures), 0);

			this.processor.EmitUInt16(this.processor.SegData.Value16, ScreenBox, 0);
			this.owner.Services.DisplayScreen(0);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - ScreenText), (ushort)(this.processor.PullLocalWord(ScreenPartEnd) + 0xba07));

			ushort shown = (ushort)(this.processor.PullLocalWord(ScreenPartEnd) + 1);
			this.processor.EmitLocalWord(ScreenTextLength, (ushort)(this.processor.PullLocalWord(ScreenTextLength) - shown));
			this.processor.EmitLocalWord(ScreenPartEnd, 0xffff);

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				this.owner.Throne.ImportPalaceGraphics(1);
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				return;
			}

			this.owner.Startup.QueryForDisk(6, 1);
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x330a, 0);
			this.owner.Services.PerformSound(1, 0);

			if (VgaMode != 0)
				this.owner.Services.FloodColors(5, 0, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x3316, 1);
			this.owner.Startup.QueryForDisk(0, 1);
			this.owner.Services.PerformSound(0x22, 0);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			if (VgaMode == 0)
				return;

			this.owner.Services.AssignHueCycleSpan(1, 15, 16, 31);
			this.owner.Services.AssignHueCycleSpan(2, 100, 32, 34);
			this.owner.Services.AssignHueCycleSpan(3, 42, 35, 40);
			this.owner.Services.AssignHueCycleSpan(4, 300, 41, 47);
			this.owner.Services.AssignHueCycleSpan(5, 60, 48, 53);
			this.owner.Services.AssignHueCycleSpan(6, 37, 54, 63);

			this.processor.EmitLocalWord(ScreenColumn, 1);

			do
			{
				this.owner.Services.ActivateHueCycle(this.processor.PullLocalWord(ScreenColumn));
				this.processor.IncrementLocalWord(ScreenColumn);
			}
			while ((short)this.processor.PullLocalWord(ScreenColumn) <= 6);
		}

		/// <summary>The rest of the text, part after part (up to "^"), at the top of the screen with a blinking dot after each part</summary>
		private void RenderClosingText()
		{
			do
			{
				CutNextTextPart((ushort)(this.processor.RegFrame.Value16 - ScreenText));

				short textLength = (short)this.processor.PullLocalWord(ScreenTextLength);

				if ((short)this.processor.PullLocalWord(ScreenPartEnd) < textLength)
				{
					this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x20,
						ScreenBox, 0, 0);

					this.owner.TextPainter.RenderCenteredTextToScreen0(
						(ushort)(this.processor.RegFrame.Value16 + this.processor.PullLocalWord(ScreenPartBegin) - ScreenText), 160, 5, 11);
					this.owner.TextPainter.RenderCenteredTextToScreen0(
						(ushort)(this.processor.RegFrame.Value16 + this.processor.PullLocalWord(ScreenPartBegin) - ScreenText), 160, 4, 15);

					this.owner.Services.AssignDot(280, 32, 15);
					this.owner.TextPainter.AwaitTime(80);
					this.owner.Services.AssignDot(280, 32, 0);
					this.owner.TextPainter.AwaitTime(80);
				}

				textLength = (short)this.processor.PullLocalWord(ScreenTextLength);

				if ((short)this.processor.PullLocalWord(ScreenPartEnd) >= textLength)
					break;
			}
			while (true);
		}

		/// <summary>A key closes the screen: colour cycles off, sound, the credits file closed, and the scenery of the civilization</summary>
		private void CloseLanding(short participantIdx)
		{
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			if (participantIdx == this.owner.World.PersonParticipantIdx && VgaMode != 0)
			{
				this.processor.EmitLocalWord(ScreenColumn, 1);

				do
				{
					this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(ScreenColumn));
					this.processor.IncrementLocalWord(ScreenColumn);
				}
				while ((short)this.processor.PullLocalWord(ScreenColumn) <= 6);
			}

			this.processor.EmitUInt16(this.processor.SegData.Value16, ScreenBox, 0);
			this.owner.Services.PerformSound(1, 0);
			this.owner.Libc.CloseStream((short)this.owner.GlobD768);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 1);

			if (VgaMode == 0)
			{
				this.owner.Startup.QueryForDisk(7, 1);
				this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3322, 0);
				this.owner.Startup.QueryForDisk(0, 1);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			this.owner.TurnHooks.DisplayCivilizationScenery();
			this.owner.Helpers.SetupHueCycling();
		}

		/// <summary>
		/// The opening news of a new game: the picture 0x332e and, with the human player's leader name in capitals (code text
		/// 0x30b8) and the last technology the player knows ("Irrigation" without any, code text 0x30be), the text 0x3342 of
		/// file 0x3348 shown part after part ("^" ends a part; the last part is not shown), each in a band at row 8 (colour 14
		/// with a shadow when 0xd762 is set, else 9), waiting 200 between parts unless a part ends with "!" or a key is waiting;
		/// then the leader's sound and the screen of the civilization
		/// </summary>
		public void DisplayTerrainTutorial()
		{
			this.processor.Journal.OpenScope("DisplayTerrainTutorial()");

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(6, 1);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.Services.PerformSound(0x23, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x332e, 1);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			this.owner.Libc.CopyText(0xba06, LeaderName(humanIdx));
			this.owner.Libc.UppercaseText(0xba06);
			this.owner.Libc.CopyText(CodeText1, 0xba06);

			this.owner.Libc.CopyText(0xba06, "Irrigation");

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(humanIdx, technologyIdx) != 0)
				{
					this.owner.Libc.CopyText(0xba06, TechnologyText(technologyIdx));
				}
			}

			this.owner.Libc.CopyText(CodeText4, 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.BlockText.ImportTextFromFile(0x3348, 0x3342);

			this.owner.BlockText.ReflowText();

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 6);

			short end = (short)this.owner.Libc.TextLength(0xba06);
			short place = -1;

			do
			{
				ushort partAddr = (ushort)(0xba06 + place + 1);

				do
				{
					place++;
				} while (this.processor.PullGlobalByte((ushort)(0xba06 + place)) != 0x5e && place < end);

				if (place >= end)
					break;

				this.processor.EmitGlobalByte((ushort)(0xba06 + place), 0);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 6, 0x140, 0x14, ScreenBox, 0, 6);

				ushort hue = 9;

				if (VgaMode != 0)
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 7, 15);
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 13);

					hue = 14;
				}
				else
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 1);
				}

				this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 8, hue);

				if (this.processor.PullGlobalByte((ushort)(0xba05 + place)) != 0x21 && this.owner.Libc.KeyAvailable() == 0)
				{
					this.owner.TextPainter.AwaitTime(200);
				}
			} while (place < end);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord(
				(ushort)((this.owner.World.Participants[humanIdx].ChiefVisuals * 0x3a) + 0x1518)), 0);

			this.owner.TextPainter.AwaitTime(300);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.TurnHooks.DisplayCivilizationScenery();

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 1);

			this.processor.Journal.CloseScope("DisplayTerrainTutorial");
		}

		/// <summary>
		/// The tutorial of the screen: points at the 8 tiles around the centre of the map window (0xd4cc + 7, 0xd75e + 6) with
		/// the names of their terrain (of the special resource, or "Village"), then at the menu bar, the map window and the
		/// active unit, and waits for a key or a click. SI holds 2 * direction at the pointers, like in the original: DisplayMenu
		/// leaves a value made from it in DI, which goes back to the caller.
		/// </summary>
		public void DisplayInterfaceTutorial()
		{
			this.processor.Journal.OpenScope("DisplayInterfaceTutorial()");

			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			short centreHoriz = (short)(MapViewHoriz + 7);
			short centreVert = (short)(MapViewVert + 6);
			MapLayers chart = this.owner.MapOps.Chart;

			for (int heading = 1; heading <= 8; heading++)
			{
				int horizLoc = this.owner.Routing.TunePosition(chart.DirectionHoriz(heading) + centreHoriz);
				short vertLoc = (short)(chart.DirectionVert(heading) + centreVert);
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				// Terrain names of 0x13 bytes at 0x282, names of their special resources at 0x366
				this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x282));

				if (this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x366));
				}

				if (this.owner.MapOps.ContainsHut(terrain, horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, "Village");
				}

				this.processor.RegSource.Value16 = (ushort)(heading << 1);

				RenderTutorialPointer((chart.DirectionHoriz(heading) << 4) + 200, (chart.DirectionVert(heading) << 4) + 112,
					(chart.DirectionHoriz(heading) << 6) + 170, (chart.DirectionVert(heading) * 48) + 100);
			}

			this.owner.Libc.CopyText(0xba06, "Menu Bar");

			RenderTutorialPointer(160, 6, 200, 16);

			this.owner.Libc.CopyText(0xba06, "Map Window");

			RenderTutorialPointer(48, 32, 88, 24);

			this.owner.Libc.CopyText(0xba06, "Active Unit");

			RenderTutorialPointer(40, 128, 88, 170);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.RestoreSource();

			this.processor.Journal.CloseScope("DisplayInterfaceTutorial");
		}

		/// <summary>
		/// A tutorial hint: a line (colour 15) from the pointed place to the text, then the text at 0xba06 as a dialog
		/// </summary>
		public void RenderTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2)
		{
			this.processor.Journal.OpenScope($"RenderTutorialPointer({horizSpot1}, {vertSpot1}, {horizSpot2}, {vertSpot2})");

			this.owner.VideoCard.RenderLine(ScreenBox, horizSpot1, vertSpot1, horizSpot2, vertSpot2 + 6, 15);

			this.owner.Libc.AppendText(0xba06, "\n");

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, horizSpot2, vertSpot2, 1);

			this.processor.Journal.CloseScope("RenderTutorialPointer");
		}
	}
}
