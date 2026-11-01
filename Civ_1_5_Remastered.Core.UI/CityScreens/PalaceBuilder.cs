namespace Civ_1_5_Remastered.Core.UI.CityScreens
{
	public class PalaceBuilder : GameComponent, IPalaceBuilder
	{
		public const ushort PalacePicturesTableAddress = 0xb23c;
		public const ushort PalaceSectionStyleMarkersAddress = 0xb25e;
		public const ushort PalaceLeftBushesAddress = 0xd930;
		public const ushort PalaceGardenLevelsAddress = 0xd930;
		public const ushort PalaceRightBushesAddress = 0xd934;

		private const ushort PalaceUpgradeOptionsPromptAddress = 0x4af0;
		private const ushort PalaceCastleResourceAddress = 0x4b27;
		private const ushort PalacePartHeightTableFirstAddress = 0x4b31;
		private const ushort PalacePartHeightTableSecondAddress = 0x4b32;
		private const ushort PalacePartHeightTableThirdAddress = 0x4b33;
		private const ushort PalacePartHeightTableFourthAddress = 0x4b34;
		private const ushort PalacePartHeightTableFifthAddress = 0x4b35;
		private const ushort PalacePartHeightTableAddress = 0x4b36;
		private const ushort PalaceGraphicsResourceAddress = 0x4b9f;

		private static readonly GameText PalaceUpgradeAnnouncementDialogAddress = new(0x4ade, "*PALACE");
		private static readonly GameText PalaceInitialScreenPictureAddress = new(0x4ae6, "cback.pic");
		private static readonly GameText PalaceStyleChoiceHeaderAddress = new(0x4b13, "Select new section.");
		private static readonly GameText PalaceScreenBackgroundPictureAddress = new(0x4b81, "sad.pic");
		private static readonly GameText PalaceDefaultBackgroundPictureAddress = new(0x4b89, "cback.pic");
		private static readonly GameText PalacePaletteAddress = new(0x4bb0, "sad.pic");

		public PalaceBuilder(IGameEngine owner)
			: base(owner)
		{
		}

		public void DisplayPalace()
		{
			using var _ = this.Processor.Journal.Scope("DisplayPalace()");

			ushort dataSegment = this.Processor.SegmentData.Word;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(PalaceUpgradeAnnouncementDialogAddress);
			string announcement = this.Owner.Words.ToString();
			this.Owner.Startup.AskForDisk(2, 1);

			DrawingBox.DisplayIndex = 0x1;
			LoadPalaceGraphics(0);
			DrawingBox.DisplayIndex = 0x0;

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			this.Owner.Helpers.HaltHueCycling();

			ushort chiefVisuals = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals;
			this.Owner.Services.PerformSound(Tables.LeaderWord(chiefVisuals, LeaderTable.Tune), 3);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, PalaceInitialScreenPictureAddress, 1);

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Dialogs.ShowMenu(announcement, 20, 16, 1);

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0x14, 0x10, 0x118, 0x2c,
				Screen.Box,
				0x14, 0x10);

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.BackScreenBox,
				0, 0);

			Screen.DialogNeedsRedraw = 1;
			this.Owner.Dialogs.ShowMenu(PalaceUpgradeOptionsPromptAddress, 40, 16, 1);

			ushort hasOptions = 0;
			for (ushort seconds = 1; (short)seconds < 8; seconds++)
			{
				short level = (short)PalaceSectionLevel(seconds);
				if (level < 4 &&
					((short)PalaceSectionLevel((ushort)(seconds - 1)) > 0 ||
					 (short)PalaceSectionLevel((ushort)(seconds + 1)) > 0 ||
					 level >= 0))
				{
					short labelHorizontal = (short)((0x30 * (short)seconds) - 0x24);
					EmptyMessage()
						.Number((short)seconds);
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), labelHorizontal, 144, Hues.Yellow);
					hasOptions = 1;
				}
			}

			for (ushort gardenIndex = 0; (short)gardenIndex < 3; gardenIndex++)
			{
				if ((short)PalaceGardenLevel(gardenIndex) < 3)
				{
					this.Owner.Words.Set("A");
					this.Processor.EmitGlobalByte(TextBuffer.Address, (byte)(this.Owner.Words[0] + (byte)gardenIndex));
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), ((short)gardenIndex * 120) + 40, 160, Hues.Yellow);
					hasOptions = 1;
				}
			}

			if (hasOptions != 0)
			{
				this.UpgradePalaceSectionInteractive(dataSegment);
			}

			this.Owner.Services.ShowScreen(0);
			this.Owner.Services.PerformSound(1, 0);
			this.Owner.Startup.AskForDisk(0, 1);

			if (Screen.VideoGraphicsMode == 0)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.TurnHooks.RepaintMainScreen();
			this.Owner.Startup.ShowMouseCursor();
		}

		public void LoadPalaceGraphics(ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ImportPalaceGraphics({marker})");

			ushort lastLeftEnd = 0;
			ushort builtRightmost = 0;

			this.Owner.Startup.AskForDisk(2, 1);

			DrawingBox.DisplayIndex = 1;
			Palace.FirstPicture = 0xffff;

			ushort built = 0;

			for (ushort sectionIndex = 0; sectionIndex < 9; sectionIndex++)
			{
				ushort part = PalacePart(sectionIndex);

				if (part != 0xffff)
				{
					built |= (ushort)(1 << (byte)part);
				}
			}

			for (ushort sectionIndex = 1; sectionIndex < 8; sectionIndex++)
			{
				SetPalaceSectionStyleMarkers(sectionIndex, 0xff, 0xff);

				if (PalacePart(sectionIndex) != 0xffff)
				{
					DrawPalaceSection(sectionIndex, 1);
				}
			}

			LoadPalaceCastle(built, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, marker != 0 ? PalaceScreenBackgroundPictureAddress : PalaceDefaultBackgroundPictureAddress, 0);

			if ((short)Palace.GardenBackground > 0)
			{
				string gardenPicture = $"cbacks{(char)(Palace.GardenBackground + '0')}.pic";
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x87, gardenPicture, 0);
			}

			ushort horizontalLocation = 0x10;

			if (Palace.StyleShift == 0xffff)
			{
				horizontalLocation -= 0x18;
			}

			if (Palace.Layout == 1)
			{
				horizontalLocation++;
			}

			const ushort verticalLocation = 0x24;

			short rightmost = 8;

			while (PalacePart((ushort)rightmost) == 0xffff)
			{
				rightmost--;
				builtRightmost = (ushort)rightmost;
			}

			ushort section;

			for (section = 1; section < 8; section++)
			{
				ushort part = PalacePart(section);

				if (part == 0xffff)
				{
					horizontalLocation += 0x30;
					continue;
				}

				ushort look = 2;

				if (section < 4)
				{
					look = PalacePart((ushort)(section - 1)) == 0xffff || section == 1 ? (ushort)0 : (ushort)1;
				}

				if (section > 4)
				{
					look = PalacePart((ushort)(section + 1)) == 0xffff || section == 7 ? (ushort)3 : (ushort)1;
				}

				switch (look)
				{
					case 0:
						if (section == 3)
						{
							DrawPalacePart(part, section, (ushort)(horizontalLocation + Palace.WingShift), verticalLocation, 1);
						}

						if (PalaceStyle(section) != 1 || section == 3)
						{
							DrawPalacePart(part, section, horizontalLocation, verticalLocation, 0);
						}

						horizontalLocation += Palace.WingShift;

						if (section == 3)
						{
							horizontalLocation += Palace.PartWidth;
						}

						lastLeftEnd = section;
						break;

					case 1:
						ushort leftEnd = lastLeftEnd;

						if ((ushort)(leftEnd + 1) == section && PalaceStyle(section) == PalaceStyle(leftEnd))
						{
							DrawPalacePart(part, leftEnd, (ushort)(horizontalLocation + Palace.PartWidth - 17), verticalLocation - 2, 0);
						}

						ushort rightEnd = builtRightmost;

						if ((ushort)(rightEnd - 1) == section && PalaceStyle(section) == PalaceStyle(rightEnd))
						{
							DrawPalacePart(part, rightEnd, (ushort)(horizontalLocation + Palace.PartWidth - 17), verticalLocation - 2, 1);
						}

						DrawPalacePart(part, section, horizontalLocation, verticalLocation, 0);

						horizontalLocation += Palace.PartWidth;

						DrawPalacePart(part, section, horizontalLocation, verticalLocation, 1);

						horizontalLocation += Palace.PartWidth;
						break;

					case 2:
						if (Palace.Layout == 1)
						{
							horizontalLocation--;
						}

						DrawPalacePart(part, section, (ushort)(horizontalLocation - 2), verticalLocation, 0);

						horizontalLocation += Palace.LeftWingWidth;

						if (Palace.Layout == 1)
						{
							horizontalLocation--;
						}

						break;

					case 3:
						if (section == 5)
						{
							DrawPalacePart(part, section, horizontalLocation, verticalLocation, 0);

							horizontalLocation += Palace.PartWidth;
						}

						ushort level = PalacePart((ushort)(section - 1));

						if (PalaceStyle(section) == 1 && (short)level > (short)PalacePart(section))
						{
							DrawPalacePart(part, section, (ushort)(horizontalLocation - 3), (ushort)(level - PalacePart(section) + verticalLocation + 1), 1);
						}
						else
						{
							DrawPalacePart(part, section, (ushort)(horizontalLocation - (PalaceStyle(section) == 1 ? 3 : 2)), verticalLocation, 1);
						}

						horizontalLocation += Palace.RightWingWidth;
						break;
				}
			}

			ushort lastLeft = lastLeftEnd;

			if (PalaceStyle(lastLeft) == 1 && lastLeft != 3)
			{
				DrawPalacePart(PalacePart(lastLeft), lastLeft, (ushort)((lastLeft * 48) - 36),
					(ushort)(PalacePart((ushort)(lastLeft + 1)) - PalacePart(lastLeft) + verticalLocation), 0);
			}

			this.Owner.Helpers.ReleaseResource(Palace.FirstPicture, PalaceGraphicsResourceAddress);

			if (Palace.LeftBushes != 0 || Palace.RightBushes != 0)
			{
				ushort leftBush = 0, rightBush = 0;

				if (Palace.LeftBushes != 0)
				{
					leftBush = DrawPalaceBush(PalaceLeftBushesAddress, 0x2e, 0);
				}

				if (Palace.RightBushes != 0)
				{
					rightBush = DrawPalaceBush(PalaceRightBushesAddress, 0x2f, 184);
				}

				this.Owner.Helpers.ReleaseResource(Palace.LeftBushes != 0 ? leftBush : rightBush, 0);
			}

			if (marker != 0)
			{
				DrawingBox.DisplayIndex = 0;

				if (Screen.VideoGraphicsMode != 0 && (short)Files.FirstMissingDisk > 7)
				{
					this.Owner.Services.FillColors(5, 0, 0, 0);
				}

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, PalacePaletteAddress, 1);

				this.Owner.Startup.AskForDisk(0, 1);
			}
		}

		public void DrawPalaceSection(ushort sectionIndex, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPalaceSection({sectionIndex}, {marker})");

			short section = (short)sectionIndex;
			ushort level = PalaceSectionLevel(sectionIndex);

			int place = 2;

			if (section < 4)
			{
				place = 1;

				if (PalaceSectionLevel((ushort)(sectionIndex - 1)) == 0xffff || sectionIndex == 1)
				{
					place = 0;
				}
			}

			if (section > 4)
			{
				place = 1;

				if (PalaceSectionLevel((ushort)(sectionIndex + 1)) == 0xffff || sectionIndex == 7)
				{
					place = 3;
				}
			}

			switch (place)
			{
				case 0:
					SetPalaceSectionStyleMarker(sectionIndex, 0, 0x3);

					if (sectionIndex == 3 || marker == 0)
					{
						SetPalaceSectionStyleMarker(sectionIndex, 1, (byte)(level != 0 ? 1 : 2));
					}

					break;

				case 1:
					SetPalaceSectionStyleMarker(sectionIndex, 0, (byte)(LeftNeighbourHigher(sectionIndex) || sectionIndex == 5 || PalaceSectionLevel((ushort)(sectionIndex - 2)) == 0xffff ? 1 : 2));
					SetPalaceSectionStyleMarker(sectionIndex, 1, 0x2);
					break;

				case 2:
					SetPalaceSectionStyleMarker(sectionIndex, 0, 0x0);
					break;

				case 3:
					if (sectionIndex == 5 || marker == 0)
					{
						SetPalaceSectionStyleMarker(sectionIndex, 0, (byte)(LeftNeighbourHigher(sectionIndex) || sectionIndex == 5 ? 1 : 2));
					}

					SetPalaceSectionStyleMarker(sectionIndex, 1, 0x4);
					break;
			}
		}

		public void DrawPalacePart(ushort level, ushort section, ushort horizontalLocation, ushort verticalShift, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPalacePart({level}, {section}, {horizontalLocation}, {verticalShift}, {marker})");

			ushort partPlace = PalaceSectionStyle(section);
			sbyte styleShift = (sbyte)PalaceSectionStyleMarker(section, marker);
			ushort heightIndex = (ushort)(styleShift + (5 * partPlace));
			sbyte height = (sbyte)this.Processor.PullGlobalByte((ushort)((0xf * level) + heightIndex + PalacePartHeightTableAddress));
			ushort verticalLocation = (ushort)(height + verticalShift + level);

			this.Owner.Services.DrawPictureToScreen(Screen.Box,
				(short)horizontalLocation,
				(short)verticalLocation,
				PalacePicture(section, marker));
		}

		public void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt)
		{
			using var _ = this.Processor.Journal.Scope($"ImportPalaceCastle({levelMask}, {onlyBuilt})");

			ushort level;

			for (level = 0; level < 5; level++)
			{
				if (((1 << level) & levelMask) == 0)
				{
					continue;
				}

				string documentName = $"castle{level}.pic";

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, documentName, 0);

				ushort section;

				for (section = 1; section < 8; section++)
				{
					if (PalaceSectionLevel(section) != level && onlyBuilt != 0)
					{
						continue;
					}

					ushort place = PalaceSectionStyle(section);

					if (onlyBuilt == 0)
					{
						place = (ushort)(section - 1);
					}

					ushort horizontalLocation = (ushort)((place & 1) * 0xa0);
					ushort verticalLocation = (ushort)((place & 2) * 0x32);

					for (ushort partIndex = 0; partIndex < 2; partIndex++)
					{
						ushort picture = PalacePictureAddress(section, partIndex);
						sbyte style = (sbyte)PalaceSectionStyleMarker(section, partIndex);
						bool middle = onlyBuilt != 0 ? PalaceSectionStyle(section) == 1 : section == 2;

						ushort pictureHandle = 0;
						switch (style)
						{
							case 0:
								pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, horizontalLocation, (ushort)(verticalLocation + 1), 0x34, 0x63);
								break;

							case 1:
								pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x35), (ushort)(verticalLocation + 1), 0x18, 0x63);
								break;

							case 2:
								pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x4e), (ushort)(verticalLocation + 1), 0x18, 0x63);
								break;

							case 3:
								if (middle)
								{
									pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, 0xa0, 0x65, 0x23, 0x63);
								}
								else
								{
									pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x67), (ushort)(verticalLocation + 1), 0x1c, 0x63);
								}

								break;

							case 4:
								if (middle)
								{
									pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, 0xc4, 0x65, 0x23, 0x63);
								}
								else
								{
									pictureHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x84), (ushort)(verticalLocation + 1), 0x1b, 0x63);
								}

								break;
						}

						if (style is >= 0 and <= 4)
						{
							SetPalacePicture(section, partIndex, pictureHandle);
						}

						if (style != -1 && Palace.FirstPicture == 0xffff)
						{
							Palace.FirstPicture = PalacePicture(section, partIndex);
						}
					}
				}
			}
		}

		private static ushort PalacePictureAddress(ushort sectionIndex, ushort partIndex)
		{
			return (ushort)(PalacePicturesTableAddress + (sectionIndex << 2) + (partIndex << 1));
		}

		private ushort PalaceSectionLevel(ushort sectionIndex)
		{
			return this.Processor.PullGlobalWord((ushort)(IPalaceBuilder.PalaceSectionLevelsAddress + (sectionIndex << 1)));
		}

		private void SetPalaceSectionLevel(ushort sectionIndex, ushort level)
		{
			this.Processor.EmitGlobalWord((ushort)(IPalaceBuilder.PalaceSectionLevelsAddress + (sectionIndex << 1)), level);
		}

		private ushort PalaceGardenLevel(ushort gardenIndex)
		{
			return this.Processor.PullGlobalWord((ushort)(PalaceGardenLevelsAddress + (gardenIndex << 1)));
		}

		private ushort PalaceSectionStyle(ushort sectionIndex)
		{
			return this.Processor.PullGlobalWord((ushort)(IPalaceBuilder.PalaceSectionStylesAddress + (sectionIndex << 1)));
		}

		private void SetPalaceSectionStyle(ushort sectionIndex, ushort style)
		{
			this.Processor.EmitGlobalWord((ushort)(IPalaceBuilder.PalaceSectionStylesAddress + (sectionIndex << 1)), style);
		}

		private byte PalaceSectionStyleMarker(ushort sectionIndex, ushort markerIndex)
		{
			return this.Processor.PullGlobalByte((ushort)(PalaceSectionStyleMarkersAddress + (sectionIndex << 1) + markerIndex));
		}

		private void SetPalaceSectionStyleMarker(ushort sectionIndex, ushort markerIndex, byte marker)
		{
			this.Processor.EmitGlobalByte((ushort)(PalaceSectionStyleMarkersAddress + (sectionIndex << 1) + markerIndex), marker);
		}

		private void SetPalaceSectionStyleMarkers(ushort sectionIndex, byte firstMarker, byte secondMarker)
		{
			ushort address = (ushort)(PalaceSectionStyleMarkersAddress + (sectionIndex << 1));
			this.Processor.EmitGlobalByte(address, firstMarker);
			this.Processor.EmitGlobalByte((ushort)(address + 1), secondMarker);
		}

		private ushort PalacePicture(ushort sectionIndex, ushort partIndex)
		{
			return this.Processor.PullGlobalWord(PalacePictureAddress(sectionIndex, partIndex));
		}

		private void SetPalacePicture(ushort sectionIndex, ushort partIndex, ushort pictureHandle)
		{
			this.Processor.EmitGlobalWord(PalacePictureAddress(sectionIndex, partIndex), pictureHandle);
		}

		private void UpgradePalaceSectionInteractive(ushort dataSegment)
		{
			this.Owner.Startup.ShowMouseCursor();

			ushort selectedSection;
			while (true)
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.MenuPointerState == 0 && this.Owner.Runtime.KeyAvailable() == 0);

				if (this.Owner.Runtime.KeyAvailable() != 0)
				{
					ushort navigationKey = this.Owner.Dialogs.ReadNavigationKey();
					selectedSection = (short)navigationKey >= 0x38
						? (ushort)(navigationKey - 0x58)
						: (ushort)(navigationKey - 0x30);
					if ((short)selectedSection < 1 || (short)selectedSection > 0xb)
					{
						continue;
					}
				}
				else if ((short)this.Owner.MenuPointerVertical >= 0x9a)
				{
					selectedSection = (ushort)(CentralProcessor.SignedQuotient((short)this.Owner.MenuPointerHorizontal, 0x6b) + 9);
				}
				else
				{
					selectedSection = (ushort)(CentralProcessor.SignedQuotient((short)(this.Owner.MenuPointerHorizontal + 0xc), 0x30) + 1);
				}

				if ((short)PalaceSectionLevel(selectedSection) >= 4)
				{
					continue;
				}

				if ((short)selectedSection >= 9 &&
					(short)PalaceSectionLevel(selectedSection) >= 3)
				{
					continue;
				}

				break;
			}

			if ((short)selectedSection < 9)
			{
				while ((short)selectedSection < 4)
				{
					if ((short)PalaceSectionLevel((ushort)(selectedSection + 1)) > 0 ||
						PalaceSectionLevel(selectedSection) != 0xffff)
					{
						break;
					}

					selectedSection++;
				}

				while ((short)selectedSection > 4)
				{
					if ((short)PalaceSectionLevel((ushort)(selectedSection - 1)) > 0 ||
						PalaceSectionLevel(selectedSection) != 0xffff)
					{
						break;
					}

					selectedSection--;
				}
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			ushort nextLevel = (ushort)this.Owner.Helpers.ClampValue(
				(short)PalaceSectionLevel(selectedSection) + 1,
				1, 4);

			ushort selectedStyle = 0;
			if ((short)selectedSection < 9)
			{
				selectedStyle = this.SelectPalaceArchitectureStyle(dataSegment, selectedSection, nextLevel);
			}

			SetPalaceSectionLevel(selectedSection, nextLevel);
			SetPalaceSectionStyle(selectedSection, selectedStyle);

			DrawingBox.DisplayIndex = 0x1;
			LoadPalaceGraphics(0);
			DrawingBox.DisplayIndex = 0x0;

			this.Owner.VideoCard.DrawScreenToMainScreen(1);
			this.Owner.TurnLogic.WaitForKey();
			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		private ushort SelectPalaceArchitectureStyle(ushort dataSegment, ushort selectedSection, ushort nextLevel)
		{
			for (ushort seconds = 1; (short)seconds < 8; seconds++)
			{
				SetPalaceSectionStyleMarkers(seconds, 0xff, 0xff);
			}

			DrawPalaceSection(selectedSection, 0);

			byte firstMarker = PalaceSectionStyleMarker(selectedSection, 0);
			byte secondMarker = PalaceSectionStyleMarker(selectedSection, 1);
			for (ushort styleIndex = 1; (short)styleIndex <= 3; styleIndex++)
			{
				SetPalaceSectionStyleMarkers(styleIndex, firstMarker, secondMarker);
			}

			Palace.FirstPicture = 0xffff;

			LoadPalaceCastle((ushort)(1 << (byte)nextLevel), 0);

			short boxHeight = 0x5c;
			this.Owner.Services.FloodBox(Screen.Box, 16, 2, 288, 102, Hues.LightBlue);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(PalaceStyleChoiceHeaderAddress, 160, 3, Hues.White);

			for (ushort optionIndex = 1; (short)optionIndex <= 3; optionIndex++)
			{
				short drawHorizontal = (short)((0x64 * (short)optionIndex) - 0x50);
				ushort boxColor = Screen.VideoGraphicsMode == 0 ? (ushort)3 : (ushort)1;
				this.Owner.Services.FloodBox(Screen.Box, drawHorizontal, 10, 80, boxHeight, boxColor);

				drawHorizontal = (short)(drawHorizontal + 0x10);
				ushort look = 2;
				if ((short)selectedSection < 4)
				{
					look = (PalaceSectionLevel((ushort)(selectedSection - 1)) == 0xffff || selectedSection == 1)
						? (ushort)0
						: (ushort)1;
				}

				if ((short)selectedSection > 4)
				{
					look = (PalaceSectionLevel((ushort)(selectedSection + 1)) == 0xffff || selectedSection == 7)
						? (ushort)3
						: (ushort)1;
				}

				switch (look)
				{
					case 0:
						if (selectedSection != 1)
						{
							this.Owner.Services.DrawPictureToScreen(
								Screen.Box,
								drawHorizontal + (short)Palace.WingShift,
								this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableSecondAddress)) + 1,
								PalacePicture(optionIndex, 1));
						}

						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							drawHorizontal,
							this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableFourthAddress)) + 1,
							PalacePicture(optionIndex, 0));
						break;

					case 1:
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							drawHorizontal,
							this.Processor.PullSignedByte(dataSegment, (ushort)((5 * optionIndex) + (0xf * nextLevel) + PalacePartHeightTableSecondAddress)) + 1,
							PalacePicture(optionIndex, 0));

						drawHorizontal = (short)(drawHorizontal + (short)Palace.PartWidth);
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							drawHorizontal,
							(short)(this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableThirdAddress)) + 1),
							PalacePicture(optionIndex, 1));
						break;

					case 2:
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							drawHorizontal,
							(short)(this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableFirstAddress)) + 1),
							PalacePicture(optionIndex, 0));
						break;

					case 3:
						if (selectedSection != 7)
						{
							this.Owner.Services.DrawPictureToScreen(
								Screen.Box,
								drawHorizontal,
								this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableSecondAddress)) + 1,
								PalacePicture(optionIndex, 0));
							drawHorizontal = (short)(drawHorizontal + (short)Palace.PartWidth);
						}

						short towerShift = optionIndex == 2 ? (short)3 : (short)2;
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							(short)(drawHorizontal - towerShift),
							(short)(this.Processor.PullSignedByte(dataSegment, (ushort)((0xf * nextLevel) + (5 * optionIndex) + PalacePartHeightTableFifthAddress)) + 1),
							PalacePicture(optionIndex, 1));
						break;
				}

				EmptyMessage()
					.Number((short)optionIndex);
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), ((short)optionIndex * 100) - 44, 12, Hues.White);
			}

			this.Owner.Helpers.ReleaseResource(Palace.FirstPicture, PalaceCastleResourceAddress);
			this.Owner.TurnLogic.WaitForKey();
			this.Owner.Startup.ShowMouseCursor();

			ushort chosenStyle;
			while (true)
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.MenuPointerState == 0 && this.Owner.Runtime.KeyAvailable() == 0);

				if (this.Owner.Runtime.KeyAvailable() == 0)
				{
					chosenStyle = (ushort)CentralProcessor.SignedQuotient((short)this.Owner.MenuPointerHorizontal, 0x6b);
					break;
				}

				chosenStyle = (ushort)(this.Owner.Dialogs.ReadNavigationKey() - 0x31);
				if ((short)chosenStyle >= 0 && (short)chosenStyle <= 2)
				{
					break;
				}
			}

			this.Owner.Startup.HideMouseCursor();
			DrawingBox.DisplayIndex = 0x1;
			LoadPalaceGraphics(0);

			ushort primaryScreenBox = Screen.Box;
			this.Processor.EmitWord(dataSegment, primaryScreenBox, 0x0);
			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				primaryScreenBox,
				0, 0);

			return chosenStyle;
		}

		private ushort PalacePart(ushort sectionIndex)
		{
			return PalaceSectionLevel(sectionIndex);
		}

		private ushort PalaceStyle(ushort sectionIndex)
		{
			return PalaceSectionStyle(sectionIndex);
		}

		private ushort DrawPalaceBush(ushort sizeAddress, byte first, int horizontalLocation)
		{
			char bushCharacter = (char)((this.Processor.PullGlobalByte(sizeAddress) << 1) + first);
			string pictureName = $"cbrush{bushCharacter}.pic";

			ushort picture = this.Owner.ImageToolkit.LoadIcon(pictureName);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation,
				(short)this.Processor.PullGlobalWord(sizeAddress) <= 1 ? 105 : 94, picture);

			return picture;
		}

		private bool LeftNeighbourHigher(ushort sectionIndex)
		{
			return (short)PalaceSectionLevel((ushort)(sectionIndex - 1)) > (short)PalaceSectionLevel(sectionIndex);
		}
	}
}
