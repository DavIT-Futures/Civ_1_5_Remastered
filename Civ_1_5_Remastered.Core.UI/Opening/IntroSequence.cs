namespace Civ_1_5_Remastered.Core.UI.Opening
{
	public class IntroSequence : GameComponent, IIntroSequence
	{
		private const int ScreenText = 0x200;

		private const int ScreenPartEnd = 0x204;
		private const int ScreenShipFrame = 0x208;
		private const int ScreenShipPictures = 0x238;
		private const int ScreenSavedFlag = 0x23a;
		private const int ScreenPartBegin = 0x23c;
		private const int ScreenColumn = 0x23e;
		private const int ScreenTextLength = 0x240;
		private const int ScreenStarHorizontal = 0x242;
		private const int ScreenStarVertical = 0x244;
		private const int ScreenBackground = 0x246;

		private static readonly ushort[] CreditColumns = [0xa, 0x32, 0x46, 0x78, 0xa0, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		private static readonly ushort[] PlanetColumns = [0xa, 0x1e, 0x32, 0x40, 0x54, 0x74, 0x82, 0x96, 0xaa, 0xbe, 0xd2, 0xe6, 0xfa, 0x10e, 0x122, 0x136];

		private ushort introScreenCount;
		private ushort introSavedFlag;
		private ushort introColumn;
		private ushort introPathFound;
		private ushort introLogo;

		private FrameWords screenFrame = null!;

		private byte[] screenText = new byte[ScreenText];

		public IntroSequence(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowMainIntro()
		{
			using var _ = this.Processor.Journal.Scope("DisplayMainIntro()");

			FindIntroDisks();
			DetectGraphicsDriver();
			ReserveIntroScreens();
			LoadIntroPictures();
			ScrollCredits();
			ScrollPlanet();
			TakeIntroChoice();
			CloseIntro();
		}

		public void ShowCivilizationScreen(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilizationScreen({participantIndex})");

			FrameWords outerFrame = this.screenFrame;
			byte[] outerText = this.screenText;
			this.screenFrame = new FrameWords(this.Processor, 0x246);
			this.screenText = new byte[ScreenText];

			LoadShipPictures();
			PrepareLandingText(participantIndex);
			FlyShip();
			ShowLanding(participantIndex);
			DrawClosingText();
			CloseLanding(participantIndex);

			this.screenFrame = outerFrame;
			this.screenText = outerText;
		}

		public void ShowTerrainTutorial()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTerrainTutorial()");

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.AskForDisk(6, 1);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.Services.PerformSound(0x23, 0);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileArchPicture, 1);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			short humanIndex = this.Owner.World.PersonParticipantIndex;

			this.Owner.Words.Set(Tables.LeaderName(humanIndex));
			this.Owner.Words.Uppercase();
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			this.Owner.Words.Set("Irrigation");

			for (short technologyIndex = 0; technologyIndex < 0x48; technologyIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(humanIndex, technologyIndex) != 0)
				{
					this.Owner.Words.Set(GameTables.TechnologyText(technologyIndex));
				}
			}

			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			this.Owner.BlockText.LoadTextFromFile(GameTexts.FileKingTextThird, GameTexts.TopicArch);

			this.Owner.BlockText.ReflowText();

			DrawingBox.TypefaceIndex = 6;

			short end = (short)this.Owner.Words.Length;
			short place = -1;

			do
			{
				ushort partAddress = (ushort)(TextBuffer.Address + place + 1);

				do
				{
					place++;
				}
				while (this.Owner.Words[place] != 0x5e && place < end);

				if (place >= end)
				{
					break;
				}

				this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + place), 0);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 6, 0x140, 0x14, Screen.Box, 0, 6);

				ushort hue = 9;

				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(partAddress, 160, 7, Hues.White);
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(partAddress, 160, 9, Hues.LightMagenta);

					hue = 14;
				}
				else
				{
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(partAddress, 160, 9, Hues.Blue);
				}

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(partAddress, 160, 8, hue);

				if (this.Owner.Words[place - 1] != 0x21 && this.Owner.Runtime.KeyAvailable() == 0)
				{
					this.Owner.TextPainter.WaitForTime(200);
				}
			}
			while (place < end);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[humanIndex].ChiefVisuals, LeaderTable.ShortTune), 0);

			this.Owner.TextPainter.WaitForTime(300);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.TurnHooks.ShowCivilizationScenery();

			DrawingBox.TypefaceIndex = 1;
		}

		public void ShowInterfaceTutorial()
		{
			using var _ = this.Processor.Journal.Scope("DisplayInterfaceTutorial()");

			this.Owner.Startup.HideMouseCursor();

			short centreHorizontal = (short)(MapView.Horizontal + 7);
			short centreVertical = (short)(MapView.Vertical + 6);
			IMapPlanes chart = this.Owner.MapOperations.Chart;

			for (int heading = 1; heading <= 8; heading++)
			{
				int horizontalLocation = this.Owner.Routing.TunePosition(chart.DirectionHorizontal(heading) + centreHorizontal);
				short verticalLocation = (short)(chart.DirectionVertical(heading) + centreVertical);
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

				this.Owner.Words.Set(GameTables.TerrainText(terrain));

				if (this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) != 0)
				{
					this.Owner.Words.Set((ushort)((terrain * 0x13) + 0x366));
				}

				if (this.Owner.MapOperations.ContainsHut(terrain, horizontalLocation, verticalLocation) != 0)
				{
					this.Owner.Words.Set("Village");
				}

				DrawTutorialPointer((chart.DirectionHorizontal(heading) << 4) + 200, (chart.DirectionVertical(heading) << 4) + 112,
					(chart.DirectionHorizontal(heading) << 6) + 170, (chart.DirectionVertical(heading) * 48) + 100);
			}

			this.Owner.Words.Set("Menu Bar");

			DrawTutorialPointer(160, 6, 200, 16);

			this.Owner.Words.Set("Map Window");

			DrawTutorialPointer(48, 32, 88, 24);

			this.Owner.Words.Set("Active Unit");

			DrawTutorialPointer(40, 128, 88, 170);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		public void DrawTutorialPointer(int firstHorizontalSpot, int firstVerticalSpot, int secondHorizontalSpot, int secondVerticalSpot)
		{
			using var _ = this.Processor.Journal.Scope($"RenderTutorialPointer({firstHorizontalSpot}, {firstVerticalSpot}, {secondHorizontalSpot}, {secondVerticalSpot})");

			this.Owner.VideoCard.RenderLine(Screen.Box, firstHorizontalSpot, firstVerticalSpot, secondHorizontalSpot, secondVerticalSpot + 6, Hues.White);

			this.Owner.Words.Append("\n");

			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, secondHorizontalSpot, secondVerticalSpot, 1);
		}

		private void FindIntroDisks()
		{
			Files.FirstMissingDisk = 1;
			this.introColumn = 1;

			do
			{
				if (this.Owner.Startup.AskForDisk(this.introColumn, 0) == 0)
				{
					break;
				}

				Files.FirstMissingDisk = (ushort)(this.introColumn + 1);
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

		private void DetectGraphicsDriver()
		{
			bool colourful = this.Processor.PullGlobalByte(IGameLauncher.VideoDriverOptionAddress) == 0x6d || this.Processor.PullGlobalByte(IGameLauncher.VideoDriverOptionAddress) == 0x4d;
			Screen.VideoGraphicsMode = (ushort)(colourful ? 1 : 0);

			bool enhancedGraphics = this.Processor.PullGlobalByte(IGameLauncher.VideoDriverOptionAddress) == 0x65 || this.Processor.PullGlobalByte(IGameLauncher.VideoDriverOptionAddress) == 0x45;
			Screen.EnhancedGraphicsMode = (ushort)(enhancedGraphics ? 1 : 0);

			DrawingBox.TypefaceIndex = 1;
		}

		private void ReserveIntroScreens()
		{
			Screen.ReserveFailed = 0;
			this.introScreenCount = (ushort)(Screen.VideoGraphicsMode != 0 ? 3 : 4);

			this.introColumn = 0;

			while (true)
			{
				short screenCount = (short)this.introScreenCount;

				if ((short)this.introColumn >= screenCount)
				{
					break;
				}

				Story.IntroReservedScreen = this.Owner.VideoCard.ReserveScreen(this.introColumn);

				if (Story.IntroReservedScreen == 0)
				{
					Screen.ReserveFailed = 1;
				}

				this.introColumn++;
			}
		}

		private void LoadIntroPictures()
		{
			this.Owner.Services.PrepareSoundDriver();
			this.Owner.Services.LaunchTimer();
			this.Owner.Helpers.SeedRandomizer();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			if (Screen.VideoGraphicsMode == 0)
			{
				this.Owner.Services.FloodBox(Screen.SpriteScreenBox, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.VideoCard.AssignHuesByIndexArray(0x19fe);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileLogoPicture, 1);
			this.introLogo = this.Owner.VideoCard.ScreenToPicture(1, 0, 64, 320, 80);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileBirthPicture, 1);
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x4c, 0x140, 0x18,
				Screen.BackScreenBox, 0, 0xb0);

			this.Owner.ImageToolkit.LoadPictureOrColors(2, 0, 0, GameTexts.FileBirthFirstPicture, 1);
			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0x4c, 0x140, 0x18,
				Screen.PictureScreenBox, 0, 0xb0);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileLogoPictureSecond, 1);
			this.Owner.Services.FloodBox(Screen.Box, 0, 12, 320, 176, Hues.Black);
			this.Owner.Services.AssignHueCycleSpan(1, 14, 146, 152);

			if (Screen.VideoGraphicsMode != 0)
			{
				this.introColumn = 1;

				do
				{
					this.Owner.Services.ActivateHueCycle(this.introColumn);
					this.introColumn++;
				}
				while ((short)this.introColumn <= 3);
			}

			this.Owner.World.SessionPreferenceMarkers |= 0x10;

			this.Owner.CreditsStream = (ushort)this.Owner.Runtime.OpenStream("credits.txt", "rt");

			this.Owner.Services.RestoreTimer();
			this.Owner.Services.PerformSound(3, 0);

			this.Owner.Words.Clear();
		}

		private void ScrollCredits()
		{
			for (this.introColumn = 0;
				(short)this.introColumn < 0x140;
				this.introColumn++)
			{
				ushort rest = (ushort)(0x140 - this.introColumn);

				this.Owner.Services.FloodBox(Screen.BackScreenBox, 0, 176, (short)rest, 24, Hues.Black);
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x40, this.introColumn, 0x18,
					Screen.BackScreenBox, rest, 0xb0);

				foreach (ushort column in CreditColumns)
				{
					if (this.introColumn != column)
					{
						continue;
					}

					if (column == 0x32 || column == 0xa0)
					{
						this.Owner.Words.Clear();
					}
					else
					{
						if (column == 0xa)
						{
							DrawingBox.TypefaceIndex = 5;
						}

						this.Owner.Runtime.ScanStream((short)this.Owner.CreditsStream, "%[^\n]\n", TextBuffer.Address);
					}
				}

				if (this.Owner.Words.Length != 0)
				{
					DrawCreditLine();
				}

				FlickerColorGraphicsScreen();

				rest = (ushort)(0x140 - this.introColumn);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, this.introColumn, 0x40,
					Screen.Box, rest, 0xc);
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0xb0, 0x140, 0x18,
					Screen.Box, 0, 0x4c);
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x58, this.introColumn, 0x58,
					Screen.Box, rest, 0x64);

				ShowColorGraphicsScreen();

				if (MusicPassedColumn(0) && (this.introColumn & 1) == 0)
				{
					this.introColumn++;
				}

				while (!MusicReachedColumn(0))
				{
					if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
					{
						break;
					}

					this.Owner.TextPainter.WaitForTime(1);
				}

				if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
				{
					this.introColumn = 0x3e7;
				}
			}
		}

		private void ScrollPlanet()
		{
			for (this.introColumn = 0;
				(short)this.introColumn < 0x140;
				this.introColumn++)
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox,
					this.introColumn, 0x40, (ushort)(0x140 - this.introColumn), 0x18,
					Screen.BackScreenBox, 0, 0xb0);

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0x40, this.introColumn, 0x18,
						Screen.BackScreenBox, (ushort)(0x140 - this.introColumn), 0xb0);
				}

				foreach (ushort column in PlanetColumns)
				{
					if (this.introColumn != column)
					{
						continue;
					}

					if (column == 0x32 || column == 0x74 || column == 0x136)
					{
						this.Owner.Words.Clear();
					}
					else
					{
						this.Owner.Runtime.ScanStream((short)this.Owner.CreditsStream, "%[^\n]\n", TextBuffer.Address);
					}
				}

				if (this.Owner.Words.Length != 0)
				{
					DrawCreditLine();
				}

				FlickerColorGraphicsScreen();

				if ((short)this.introColumn < 0x140)
				{
					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox,
						this.introColumn, 0, (ushort)(0x140 - this.introColumn), 0x40,
						Screen.Box, 0, 0xc);
				}

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0, this.introColumn, 0x40,
						Screen.Box, (ushort)(0x140 - this.introColumn), 0xc);
				}

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0xb0, 0x140, 0x18,
					Screen.Box, 0, 0x4c);

				if ((short)this.introColumn < 0x140)
				{
					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox,
						this.introColumn, 0x58, (ushort)(0x140 - this.introColumn), 0x58,
						Screen.Box, 0, 0x64);
				}

				if (this.introColumn != 0)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0x58, this.introColumn, 0x58,
						Screen.Box, (ushort)(0x140 - this.introColumn), 0x64);
				}

				ShowColorGraphicsScreen();

				if (MusicPassedColumn(0x1a40) && (this.introColumn & 1) == 0)
				{
					this.introColumn++;
				}

				while (true)
				{
					if ((ushort)this.Owner.Runtime.KeyAvailable() != 0 || MusicReachedColumn(0x1a40))
					{
						break;
					}

					this.Owner.TextPainter.WaitForTime(1);
				}

				if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
				{
					this.introColumn = 0x3e7;
				}
			}
		}

		private short MusicColumnMark(ushort start)
		{
			short scaled = (short)((0x15 * (short)this.introColumn) + start);

			return (short)(scaled / 4);
		}

		private bool MusicPassedColumn(ushort start)
		{
			short song = (short)this.Owner.Services.ReadSongPosition();

			return MusicColumnMark(start) < song;
		}

		private bool MusicReachedColumn(ushort start)
		{
			short song = (short)this.Owner.Services.ReadSongPosition();

			return MusicColumnMark(start) <= song;
		}

		private void DrawCreditLine()
		{
			ushort screen = Screen.Box;
			this.introSavedFlag = this.Processor.PullWord(this.Processor.SegmentData.Word, screen);
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, 1);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 184, (ushort)(Screen.VideoGraphicsMode != 0 ? 0xfc : 0x7));
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 182, (ushort)(Screen.VideoGraphicsMode != 0 ? 0xf8 : 0xf));
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 183, (ushort)(Screen.VideoGraphicsMode != 0 ? 0xfa : 0xf));

			screen = Screen.Box;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, this.introSavedFlag);
		}

		private void FlickerColorGraphicsScreen()
		{
			if (Screen.VideoGraphicsMode != 0)
			{
				return;
			}

			ushort screen = Screen.Box;
			ushort flag = (ushort)(this.Processor.PullWord(this.Processor.SegmentData.Word, screen) != 0 ? 0 : 3);
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, flag);
		}

		private void ShowColorGraphicsScreen()
		{
			if (Screen.VideoGraphicsMode != 0)
			{
				return;
			}

			ushort screen = Screen.Box;
			this.introSavedFlag = this.Processor.PullWord(this.Processor.SegmentData.Word, screen);
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, 0);

			this.Owner.Services.AssignDot(0, 0, 0);

			screen = Screen.Box;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, this.introSavedFlag);

			this.Owner.Services.ShowScreen(this.introSavedFlag);
		}

		private void TakeIntroChoice()
		{
			Menu.MainMenuChoice = 0xfffe;

			if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
			{
				Menu.MainMenuChoice = 0xffff;

				switch (this.Owner.Dialogs.ReadNavigationKey())
				{
					case 'N':
					case 'n':
						Menu.MainMenuChoice = 0;
						break;

					case 'L':
					case 'l':
						Menu.MainMenuChoice = 1;
						break;

					case 'E':
					case 'e':
						Menu.MainMenuChoice = 2;
						break;

					case 'C':
					case 'c':
						Menu.MainMenuChoice = 3;
						break;
				}
			}

			if (Menu.MainMenuChoice == 0xfffe)
			{
				ShowTitleLogo();
			}
		}

		private void ShowTitleLogo()
		{
			do
			{
				if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
				{
					break;
				}

				this.Owner.TextPainter.WaitForTime(1);
			}
			while ((short)this.Owner.Services.ReadSongPosition() < 0xd00);

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, 0, 64, this.introLogo);

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.Services.AssignHueCycleSpan(4, 14, 224, 239);
				this.Owner.Services.ActivateHueCycle(4);
			}

			this.introColumn = 0;

			do
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, this.introColumn, 0x40, 0x4, 0x50,
					Screen.Box, this.introColumn, 0x40);

				this.Owner.TextPainter.WaitForTime(1);

				this.introColumn = (ushort)(this.introColumn + 4);
			}
			while ((short)this.introColumn < 0x140);

			for (this.introColumn = 0; (short)this.introColumn < 0x64; this.introColumn++)
			{
				if ((ushort)this.Owner.Runtime.KeyAvailable() != 0)
				{
					break;
				}

				this.Owner.TextPainter.WaitForTime(5);
			}

			this.Owner.TurnLogic.WaitForKey();

			Menu.MainMenuChoice = 0xffff;
		}

		private void CloseIntro()
		{
			if (Screen.VideoGraphicsMode != 0)
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
				DrawingBox.DisplayIndex = 0;
			}

			this.Owner.Services.FloodBox(Screen.BackScreenBox, 0, 0, 320, 200, Hues.Black);
			this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, 0, 64, this.introLogo);
			this.Owner.VideoCard.DrawScreenToMainScreen(1, 0x14);
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);
			this.Owner.Services.ShowScreen(0);

			DrawingBox.TypefaceIndex = 1;

			this.Owner.Helpers.ReleaseResource(this.introLogo, 0);
		}

		private void SetScreenText(string text)
		{
			for (int index = 0; index <= text.Length && index < this.screenText.Length; index++)
			{
				this.screenText[index] = index < text.Length ? (byte)text[index] : (byte)0;
			}
		}

		private string ScreenTextFrom(int index)
		{
			int end = index;

			while (end >= 0 && end < this.screenText.Length && this.screenText[end] != 0)
			{
				end++;
			}

			return index < 0 || index >= this.screenText.Length ? string.Empty : System.Text.Encoding.Latin1.GetString(this.screenText, index, end - index);
		}

		private void LoadShipPictures()
		{
			this.Owner.Startup.AskForDisk(6, 1);
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FilePlanetSecondPicture, 0);

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

			if (Screen.VideoGraphicsMode != 0)
			{
				this.screenFrame.SetFrame(ScreenBackground, 0xec);
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, 236);
			}
			else
			{
				this.screenFrame.SetFrame(ScreenBackground, 0);
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FilePlanetFirstPicture, 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x18, 0x140, 0xb0,
				Screen.BackScreenBox, 0, 0);
			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x20, 0x140, 0x18,
				Screen.BackScreenBox, 0, 0xb0);

			this.Owner.Startup.AskForDisk(0, 1);

			if (Screen.VideoGraphicsMode == 0)
			{
				this.Owner.Services.FloodBox(Screen.SpriteScreenBox, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, this.screenFrame.Frame(ScreenBackground));
			this.Owner.Services.FloodBox(Screen.Box, 0, 12, 320, 176, this.screenFrame.Frame(ScreenBackground));
		}

		private void PrepareLandingText(short participantIndex)
		{
			ushort regime = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].RegimeKind;
			Message(Tables.GovernmentTitle(regime))
				.Text(" ");

			ushort person = (ushort)this.Owner.World.PersonParticipantIndex;
			this.Owner.Words.Append(Tables.LeaderName(person));
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			this.Owner.Codes.Set(1, Tables.CivilizationName((ushort)participantIndex));

			EmptyMessage()
				.Number(this.Owner.World.Participants[participantIndex].StarshipInhabitants);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());

			EmptyMessage()
				.Number(this.Owner.World.Participants[participantIndex].StarshipLiftoffSeason);
			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			EmptyMessage()
				.Number(this.Owner.World.Season);
			this.Owner.Codes.Set(4, this.Owner.Words.ToString());

			this.Owner.BlockText.LoadTextFromFile(GameTexts.FileKingTextSecond,
				(ushort)((participantIndex != this.Owner.World.PersonParticipantIndex) ? 0x32f9 : 0x32f2));
			this.Owner.BlockText.ReflowText();
			SetScreenText(this.Owner.Words.ToString());

			DrawingBox.TypefaceIndex = 6;
			this.screenFrame.SetFrame(ScreenStarHorizontal, 0xfff6);
			this.screenFrame.SetFrame(ScreenStarVertical, 0x5c);
			this.screenFrame.SetFrame(ScreenPartEnd, 0xffff);
			this.screenFrame.SetFrame(ScreenPartBegin, 0xffff);

			this.screenFrame.SetFrame(ScreenTextLength, this.Owner.Words.Length);

			this.Owner.Services.PerformSound(3, 0);

			this.Owner.Words.Clear();
		}

		private void FlyShip()
		{
			for (this.screenFrame.SetFrame(ScreenColumn, 0); (short)this.screenFrame.Frame(ScreenColumn) < 0x140;)
			{
				this.Owner.Services.RestoreTimer();

				if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
				{
					this.Owner.Services.FloodBox(Screen.BackScreenBox, 0, 176,
						320 - (short)this.screenFrame.Frame(ScreenColumn), 24, this.screenFrame.Frame(ScreenBackground));
				}

				if (this.screenFrame.Frame(ScreenColumn) != 0)
				{
					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x14, this.screenFrame.Frame(ScreenColumn), 0x18,
						Screen.BackScreenBox, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0xb0);
				}

				if ((short)this.screenFrame.Frame(ScreenColumn) % 0x16 == 0)
				{
					CutNextTextPart(true);
				}

				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
				{
					DrawLandingTextPart();
				}

				DrawShipFrame();
				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
			}
		}

		private void CutNextTextPart(bool inBuffer)
		{
			this.screenFrame.SetFrame(ScreenPartBegin, (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1));

			ushort end;

			while (true)
			{
				this.screenFrame.SetFrame(ScreenPartEnd, (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1));
				end = this.screenFrame.Frame(ScreenPartEnd);

				if (PullTextByte(inBuffer, end) == 0x5e || (short)end >= (short)this.screenFrame.Frame(ScreenTextLength))
				{
					break;
				}
			}

			if (inBuffer)
			{
				if ((short)end < (short)this.screenFrame.Frame(ScreenTextLength))
				{
					EmitTextByte(inBuffer, end, 0);
				}
			}
			else
			{
				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
				{
					EmitTextByte(inBuffer, this.screenFrame.Frame(ScreenPartEnd), 0);
				}
			}
		}

		private byte PullTextByte(bool inBuffer, ushort index)
		{
			return inBuffer ? this.Owner.Words[index] : index < this.screenText.Length ? this.screenText[index] : (byte)0;
		}

		private void EmitTextByte(bool inBuffer, ushort index, byte letter)
		{
			if (inBuffer)
			{
				this.Owner.Words[index] = letter;
			}
			else if (index < this.screenText.Length)
			{
				this.screenText[index] = letter;
			}
		}

		private void DrawLandingTextPart()
		{
			ushort screen = Screen.Box;
			this.screenFrame.SetFrame(ScreenSavedFlag, this.Processor.PullWord(this.Processor.SegmentData.Word, screen));
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, 1);

			ushort hue = (ushort)(Screen.VideoGraphicsMode != 0 ? 0x38 : 0x7);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.TextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 184, hue);

			hue = (ushort)(Screen.VideoGraphicsMode != 0 ? 0x3c : 0xf);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.TextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 183, hue);

			screen = Screen.Box;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, this.screenFrame.Frame(ScreenSavedFlag));
		}

		private void DrawShipFrame()
		{
			this.screenFrame.SetFrame(ScreenShipFrame, (ushort)(this.screenFrame.Frame(ScreenShipFrame) + 1));

			if ((short)this.screenFrame.Frame(ScreenShipFrame) >= 0x17)
			{
				this.screenFrame.SetFrame(ScreenShipFrame, 0);
			}

			this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, 135, 51,
				this.screenFrame.Frame(ScreenShipPictures - (this.screenFrame.Frame(ScreenShipFrame) << 1)));

			FlickerColorGraphicsScreen();

			if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 12, 319, 20, this.screenFrame.Frame(ScreenBackground));
			}

			if (this.screenFrame.Frame(ScreenColumn) != 0)
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, this.screenFrame.Frame(ScreenColumn), 0x14,
					Screen.Box, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0xc);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0xb0, 0x140, 0x18,
				Screen.Box, 0, 0x20);

			if ((short)this.screenFrame.Frame(ScreenColumn) < 0x140)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 56,
					320 - (short)this.screenFrame.Frame(ScreenColumn), 132, this.screenFrame.Frame(ScreenBackground));
			}

			if (this.screenFrame.Frame(ScreenColumn) != 0)
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x2c, this.screenFrame.Frame(ScreenColumn), 0x84,
					Screen.Box, (ushort)(0x140 - this.screenFrame.Frame(ScreenColumn)), 0x38);
			}

			if ((this.screenFrame.FrameByte(ScreenColumn) & 1) != 0)
			{
				this.screenFrame.SetFrame(ScreenStarHorizontal, (ushort)(this.screenFrame.Frame(ScreenStarHorizontal) + 1));
				this.Owner.Services.AssignDot(
					(short)this.screenFrame.Frame(ScreenStarHorizontal) - (short)this.screenFrame.Frame(ScreenColumn),
					(short)this.screenFrame.Frame(ScreenStarVertical), 15);
			}

			this.screenFrame.SetFrame(ScreenStarHorizontal, (ushort)(this.screenFrame.Frame(ScreenStarHorizontal) + 1));

			if (Screen.VideoGraphicsMode == 0)
			{
				ushort screen = Screen.Box;
				this.screenFrame.SetFrame(ScreenSavedFlag, this.Processor.PullWord(this.Processor.SegmentData.Word, screen));
				this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, 0);

				this.Owner.Services.AssignDot(0, 0, 0);

				screen = Screen.Box;
				ushort savedFlag = this.screenFrame.Frame(ScreenSavedFlag);
				this.Processor.EmitWord(this.Processor.SegmentData.Word, screen, savedFlag);

				this.Owner.Services.ShowScreen(savedFlag);
			}

			if ((short)this.Owner.Timer.Beats > 0xc && (this.screenFrame.FrameByte(ScreenColumn) & 1) == 0)
			{
				this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
			}

			while ((short)this.Owner.Timer.Beats < 8)
			{
			}
		}

		private void ShowLanding(short participantIndex)
		{
			this.Owner.Helpers.ReleaseResource(this.screenFrame.Frame(ScreenShipPictures), 0);

			DrawingBox.DisplayIndex = 0;
			this.Owner.Services.ShowScreen(0);

			SetScreenText(this.Owner.Words.TextFrom((ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1)));

			ushort shown = (ushort)(this.screenFrame.Frame(ScreenPartEnd) + 1);
			this.screenFrame.SetFrame(ScreenTextLength, (ushort)(this.screenFrame.Frame(ScreenTextLength) - shown));
			this.screenFrame.SetFrame(ScreenPartEnd, 0xffff);

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Throne.LoadPalaceGraphics(1);
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);

				return;
			}

			this.Owner.Startup.AskForDisk(6, 1);
			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceStructuresPicture, 0);
			this.Owner.Services.PerformSound(1, 0);

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSpaceStructuresPictureSecond, 1);
			this.Owner.Startup.AskForDisk(0, 1);
			this.Owner.Services.PerformSound(0x22, 0);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			if (Screen.VideoGraphicsMode == 0)
			{
				return;
			}

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

		private void DrawClosingText()
		{
			do
			{
				CutNextTextPart(false);

				short textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) < textLength)
				{
					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0x20,
						Screen.Box, 0, 0);

					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(ScreenTextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 5, Hues.LightCyan);
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(ScreenTextFrom(this.screenFrame.Frame(ScreenPartBegin)), 160, 4, Hues.White);

					this.Owner.Services.AssignDot(280, 32, 15);
					this.Owner.TextPainter.WaitForTime(80);
					this.Owner.Services.AssignDot(280, 32, 0);
					this.Owner.TextPainter.WaitForTime(80);
				}

				textLength = (short)this.screenFrame.Frame(ScreenTextLength);

				if ((short)this.screenFrame.Frame(ScreenPartEnd) >= textLength)
				{
					break;
				}
			}
			while (true);
		}

		private void CloseLanding(short participantIndex)
		{
			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			if (participantIndex == this.Owner.World.PersonParticipantIndex && Screen.VideoGraphicsMode != 0)
			{
				this.screenFrame.SetFrame(ScreenColumn, 1);

				do
				{
					this.Owner.Services.DeactivateHueCycle(this.screenFrame.Frame(ScreenColumn));
					this.screenFrame.SetFrame(ScreenColumn, (ushort)(this.screenFrame.Frame(ScreenColumn) + 1));
				}
				while ((short)this.screenFrame.Frame(ScreenColumn) <= 6);
			}

			DrawingBox.DisplayIndex = 0;
			this.Owner.Services.PerformSound(1, 0);
			this.Owner.Runtime.CloseStream((short)this.Owner.CreditsStream);

			DrawingBox.TypefaceIndex = 1;

			if (Screen.VideoGraphicsMode == 0)
			{
				this.Owner.Startup.AskForDisk(7, 1);
				this.Owner.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPicture, 0);
				this.Owner.Startup.AskForDisk(0, 1);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			this.Owner.TurnHooks.ShowCivilizationScenery();
			this.Owner.Helpers.SetupHueCycling();
		}
	}
}
