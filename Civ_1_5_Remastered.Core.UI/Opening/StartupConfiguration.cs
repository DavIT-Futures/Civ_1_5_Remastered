using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Civ_1_5_Remastered.Core.Shared.Services;
using Civ_1_5_Remastered.Imaging.Pictures;
using Civ_1_5_Remastered.Imaging.Typefaces;

namespace Civ_1_5_Remastered.Core.UI.Opening
{
	public class StartupConfiguration : GameComponent, IStartupConfiguration
	{
		private const byte BackgroundColorIndex = 0;
		private const byte BlueColorIndex = 1;
		private const byte YellowColorIndex = 14;

		private const int ScreenWidth = 640;
		private const int ScreenHeight = 400;

		public StartupConfiguration(IGameEngine owner)
			: base(owner)
		{
		}

		public void InquireConfiguration()
		{
			VideoGraphicsBitmap formerDisplay = this.Owner.VideoCard.Displays[0];
			VideoGraphicsBitmap configurationDisplay = new VideoGraphicsBitmap(ScreenWidth, ScreenHeight);

			AssignHardwareColors(configurationDisplay);
			RenderHeaderBox(configurationDisplay);

			lock (this.Owner.VideoCard.VideoLatch)
			{
				this.Owner.VideoCard.Displays[0] = configurationDisplay;
				configurationDisplay.Edited = true;
			}

			AskGraphicsStage(this.Owner, configurationDisplay);
			AskSoundStage(this.Owner, configurationDisplay);
			AskPointerStage(this.Owner, configurationDisplay);

			lock (this.Owner.VideoCard.VideoLatch)
			{
				this.Owner.VideoCard.Displays[0] = formerDisplay;
				formerDisplay.Edited = true;
			}
		}

		private static void AskGraphicsStage(IGameEngine owner, VideoGraphicsBitmap screen)
		{
			ClearOptionRows(screen);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x2e, 0, 14, YellowColorIndex, null);
			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x2e, 79, 14, YellowColorIndex, null);

			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "Select graphics mode: ", 27, 14, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "1) VGA (256 color)", 27, 16, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "2) MCGA (256 color)", 27, 17, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "3) EGA (16 color)", 27, 18, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "4) Tandy 1000 (16 color)", 27, 19, YellowColorIndex, null);

			var mouseMapping = new Dictionary<int, char>
			{
				{ 16, '1' },
				{ 17, '2' },
				{ 18, '3' },
				{ 19, '4' },
			};

			char selectedChoice = AwaitChoice(owner, screen, 49, 14, "1234", mouseMapping);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, (byte)selectedChoice, 50, 14, YellowColorIndex, BackgroundColorIndex);
			screen.Edited = true;
			Thread.Sleep(120);

			owner.Processor.EmitGlobalByte(IGameLauncher.VideoDriverOptionAddress, 0x4d);
		}

		private static void AskSoundStage(IGameEngine owner, VideoGraphicsBitmap screen)
		{
			ClearOptionRows(screen);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x2e, 0, 14, YellowColorIndex, null);
			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x2e, 79, 14, YellowColorIndex, null);

			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "Select sound mode:    ", 27, 14, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "1) No sounds please", 27, 16, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "2) IBM sounds", 27, 17, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "3) Tandy sounds", 27, 18, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "4) AdLib/Sound Blaster", 27, 19, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "5) Roland MT-32 MIDI board", 27, 20, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "6) Custom sound driver", 27, 21, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "7) AdLib (Fast 486 / YM262 fix)", 27, 22, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "8) General MIDI (.MID files)", 27, 23, YellowColorIndex, null);

			var mouseMapping = new Dictionary<int, char>
			{
				{ 16, '1' },
				{ 17, '2' },
				{ 18, '3' },
				{ 19, '4' },
				{ 20, '5' },
				{ 21, '6' },
				{ 22, '7' },
				{ 23, '8' },
			};

			char selectedChoice = AwaitChoice(owner, screen, 49, 14, "12345678", mouseMapping);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, (byte)selectedChoice, 49, 14, YellowColorIndex, BackgroundColorIndex);
			screen.Edited = true;
			Thread.Sleep(120);

			switch (selectedChoice)
			{
				case '1':
					owner.Audio.DriverPreference = "none";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x4e);
					break;
				case '2':
					owner.Audio.DriverPreference = "ibm";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x49);
					break;
				case '3':
					owner.Audio.DriverPreference = "tandy";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x54);
					break;
				case '4':
					owner.Audio.DriverPreference = "adlib";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x41);
					break;
				case '5':
					owner.Audio.DriverPreference = "roland";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x52);
					break;
				case '6':
					owner.Audio.DriverPreference = "custom";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x43);
					break;
				case '7':
					owner.Audio.DriverPreference = "asound2";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x41);
					break;
				case '8':
					owner.Audio.DriverPreference = "rawmidi";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x52);
					break;
				default:
					owner.Audio.DriverPreference = "adlib";
					owner.Processor.EmitGlobalByte(IGameLauncher.SoundDriverOptionAddress, 0x41);
					break;
			}
		}

		private static void AskPointerStage(IGameEngine owner, VideoGraphicsBitmap screen)
		{
			ClearOptionRows(screen);

			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "1. Mouse and Keyboard  ", 27, 14, YellowColorIndex, null);
			ClassicRomTypeface.RenderText(screen.PictureStorage, screen.Pitch, "2. Keyboard only", 27, 15, YellowColorIndex, null);

			var mouseMapping = new Dictionary<int, char>
			{
				{ 14, '1' },
				{ 15, '2' },
			};

			char selectedChoice = AwaitChoice(owner, screen, 50, 14, "12", mouseMapping);

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, (byte)selectedChoice, 49, 14, YellowColorIndex, BackgroundColorIndex);
			screen.Edited = true;
			Thread.Sleep(120);

			owner.Globals.Mouse.Present = (ushort)(selectedChoice == '2' ? 0 : 1);
		}

		private static char AwaitChoice(
			IGameEngine owner,
			VideoGraphicsBitmap screen,
			int cursorColumn,
			int cursorRow,
			string allowedCharacters,
			Dictionary<int, char> rowToChoiceMap)
		{
			char selectedChoice = '\0';
			bool cursorVisible = true;
			long lastToggle = Environment.TickCount64;

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0x5f, cursorColumn, cursorRow, YellowColorIndex, BackgroundColorIndex);
			screen.Edited = true;

			while (selectedChoice == '\0')
			{
				long currentTick = Environment.TickCount64;
				if (currentTick - lastToggle >= 250)
				{
					cursorVisible = !cursorVisible;
					lastToggle = currentTick;
					byte glyphCode = (byte)(cursorVisible ? 0x5f : 0x20);
					ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, glyphCode, cursorColumn, cursorRow, YellowColorIndex, BackgroundColorIndex);
					screen.Edited = true;
				}

				lock (owner.VideoCard.VideoLatch)
				{
					while (owner.VideoCard.Tokens.Count > 0)
					{
						int token = owner.VideoCard.Tokens.Dequeue();
						char character = (char)token;

						if (allowedCharacters.IndexOf(character) >= 0)
						{
							selectedChoice = character;
							break;
						}

						if (token == 0x0d || character == '\r' || character == ' ')
						{
							selectedChoice = allowedCharacters[0];
							break;
						}
					}
				}

				if (selectedChoice == '\0')
				{
					ushort pointerClicks = owner.VideoCard.DisplayPointerClicks;
					if ((pointerClicks & 1) != 0)
					{
						Point pointerSite = owner.VideoCard.DisplayPointerSite;
						int clickedRow = pointerSite.Y / 16;
						if (rowToChoiceMap.TryGetValue(clickedRow, out char mouseChoice))
						{
							selectedChoice = mouseChoice;
						}
					}
				}

				if (selectedChoice == '\0')
				{
					Thread.Sleep(20);
				}
			}

			return selectedChoice;
		}

		private static void ClearOptionRows(VideoGraphicsBitmap screen)
		{
			int startOffset = 11 * 16 * screen.Pitch;
			int totalBytes = (25 - 11) * 16 * screen.Pitch;
			Array.Clear(screen.PictureStorage, startOffset, totalBytes);
			screen.Edited = true;
		}

		private static void RenderHeaderBox(VideoGraphicsBitmap screen)
		{
			for (int row = 16; row < 176; row++)
			{
				int rowOffset = row * screen.Pitch;
				for (int column = 8; column < 632; column++)
				{
					screen.PictureStorage[rowOffset + column] = BlueColorIndex;
				}
			}

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xc9, 1, 1, YellowColorIndex, BlueColorIndex);
			for (int column = 2; column <= 77; column++)
			{
				ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xcd, column, 1, YellowColorIndex, BlueColorIndex);
			}

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xbb, 78, 1, YellowColorIndex, BlueColorIndex);

			for (int row = 2; row <= 9; row++)
			{
				ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xba, 1, row, YellowColorIndex, BlueColorIndex);
				ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xba, 78, row, YellowColorIndex, BlueColorIndex);
			}

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xc8, 1, 10, YellowColorIndex, BlueColorIndex);
			for (int column = 2; column <= 77; column++)
			{
				ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xcd, column, 10, YellowColorIndex, BlueColorIndex);
			}

			ClassicRomTypeface.RenderCharacter(screen.PictureStorage, screen.Pitch, 0xbc, 78, 10, YellowColorIndex, BlueColorIndex);

			ClassicRomTypeface.RenderCenteredText(screen.PictureStorage, screen.Pitch, "CIVILIZATION (TM) - Version 474.03", 3, YellowColorIndex, BlueColorIndex);
			ClassicRomTypeface.RenderCenteredText(screen.PictureStorage, screen.Pitch, "From those civilized guys at MPS Labs.", 5, YellowColorIndex, BlueColorIndex);
			ClassicRomTypeface.RenderCenteredText(screen.PictureStorage, screen.Pitch, "Program & Audio-visual Copyright (C) 1991 by MicroProse Software,Inc.,", 7, YellowColorIndex, BlueColorIndex);
			ClassicRomTypeface.RenderCenteredText(screen.PictureStorage, screen.Pitch, "All Rights Reserved.", 8, YellowColorIndex, BlueColorIndex);
		}

		private static void AssignHardwareColors(VideoGraphicsBitmap screen)
		{
			Color[] hardwarePalette = new Color[]
			{
				Color.FromArgb(0, 0, 0),
				Color.FromArgb(0, 0, 168),
				Color.FromArgb(0, 168, 0),
				Color.FromArgb(0, 168, 168),
				Color.FromArgb(168, 0, 0),
				Color.FromArgb(168, 0, 168),
				Color.FromArgb(168, 84, 0),
				Color.FromArgb(168, 168, 168),
				Color.FromArgb(84, 84, 84),
				Color.FromArgb(84, 84, 252),
				Color.FromArgb(84, 252, 84),
				Color.FromArgb(84, 252, 252),
				Color.FromArgb(252, 84, 84),
				Color.FromArgb(252, 84, 252),
				Color.FromArgb(252, 252, 84),
				Color.FromArgb(252, 252, 252),
			};

			for (int index = 0; index < hardwarePalette.Length; index++)
			{
				screen.AssignHue(index, hardwarePalette[index]);
			}
		}
	}
}
