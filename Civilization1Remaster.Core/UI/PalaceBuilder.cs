
namespace Civilization1Remaster.Core.UI
{
	public class PalaceBuilder : GameComponent, IPalaceBuilder
	{
		public const ushort PalaceSectionStylesAddress = 0xb222;
		public const ushort PalacePicturesTableAddress = 0xb23c;
		public const ushort PalaceSectionStyleMarkersAddress = 0xb25e;
		public const ushort PalaceSectionLevelsAddress = 0xd91e;
		public const ushort PalaceLeftBushesAddress = 0xd930;
		public const ushort PalaceGardenLevelsAddress = 0xd930;
		public const ushort PalaceRightBushesAddress = 0xd934;

		private const ushort PalaceUpgradeOptionsPromptAddress = 0x4af0;
		private const ushort PalaceCastleResourceAddress = 0x4b27;
		private const ushort PalacePartHeightTable1Address = 0x4b31;
		private const ushort PalacePartHeightTable2Address = 0x4b32;
		private const ushort PalacePartHeightTable3Address = 0x4b33;
		private const ushort PalacePartHeightTable4Address = 0x4b34;
		private const ushort PalacePartHeightTable5Address = 0x4b35;
		private const ushort PalacePartHeightTableAddress = 0x4b36;
		private const ushort PalaceGraphicsResourceAddress = 0x4b9f;

		private static readonly GameText PalaceUpgradeAnnouncementDialogAddress = new(0x4ade, "*PALACE");
		private static readonly GameText PalaceInitialScreenPictureAddress = new(0x4ae6, "cback.pic");
		private static readonly GameText PalaceStyleChoiceHeaderAddress = new(0x4b13, "Select new section.");
		private static readonly GameText PalaceScreenBackgroundPictureAddress = new(0x4b81, "sad.pic");
		private static readonly GameText PalaceDefaultBackgroundPictureAddress = new(0x4b89, "cback.pic");
		private static readonly GameText PalacePaletteAddress = new(0x4bb0, "sad.pic");

		public PalaceBuilder(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void DisplayPalace()
		{
			this.Processor.Journal.OpenScope("DisplayPalace()");


			ushort dataSeg = this.Processor.SegData.Value16;

			this.Owner.Startup.HideMouseCursor();
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(PalaceUpgradeAnnouncementDialogAddress);
			string announcement = this.Owner.Words.ToString();
			this.Owner.Startup.AskForDisk(2, 1);

			DrawingBox.DisplayIdx = 0x1;
			LoadPalaceGraphics(0);
			DrawingBox.DisplayIdx = 0x0;

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			this.Owner.Helpers.HaltHueCycling();

			ushort chiefVisuals = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals;
			this.Owner.Services.PerformSound(LeaderWord(chiefVisuals, LeaderTable.Tune), 3);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, PalaceInitialScreenPictureAddress, 1);

			this.Owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.Owner.Dialogs.ShowMenu(announcement, 20, 16, 1);

			this.Owner.VideoCard.RenderImage(
				BackScreenBox,
				0x14, 0x10, 0x118, 0x2c,
				ScreenBox,
				0x14, 0x10);

			this.Owner.VideoCard.RenderImage(
				ScreenBox,
				0, 0, 0x140, 0xc8,
				BackScreenBox,
				0, 0);

			DialogNeedsRedraw = 1;
			this.Owner.Dialogs.ShowMenu(PalaceUpgradeOptionsPromptAddress, 40, 16, 1);

			ushort hasOptions = 0;
			for (ushort sec = 1; (short)sec < 8; sec++)
			{
				short level = (short)PalaceSectionLevel(sec);
				if (level < 4 &&
					((short)PalaceSectionLevel((ushort)(sec - 1)) > 0 ||
					 (short)PalaceSectionLevel((ushort)(sec + 1)) > 0 ||
					 level >= 0))
				{
					short labelX = (short)(0x30 * (short)sec - 0x24);
					EmptyMessage()
						.Number((short)sec);
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), labelX, 144, Hues.Yellow);
					hasOptions = 1;
				}
			}

			for (ushort gardenIdx = 0; (short)gardenIdx < 3; gardenIdx++)
			{
				if ((short)PalaceGardenLevel(gardenIdx) < 3)
				{
					this.Owner.Words.Set("A");
					this.Processor.EmitGlobalByte(TextBuffer.Address, (byte)(this.Owner.Words[0] + (byte)gardenIdx));
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), (short)gardenIdx * 120 + 40, 160, Hues.Yellow);
					hasOptions = 1;
				}
			}

			if (hasOptions != 0)
			{
				this.UpgradePalaceSectionInteractive(dataSeg);
			}

			this.Owner.Services.ShowScreen(0);
			this.Owner.Services.PerformSound(1, 0);
			this.Owner.Startup.AskForDisk(0, 1);

			if (VgaMode == 0)
			{
				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			}

			this.Owner.TurnHooks.RepaintMainScreen();
			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayPalace");
		}

		/// <inheritdoc/>
		public void LoadPalaceGraphics(ushort marker)
		{
			this.Processor.Journal.OpenScope($"ImportPalaceGraphics({marker})");

			// Locals 0x12 and 0x18 of the original (B3: in C#), read before written on some paths: they start at 0
			ushort lastLeftEnd = 0;
			ushort builtRightmost = 0;

			this.Owner.Startup.AskForDisk(2, 1);

			DrawingBox.DisplayIdx = 1;
			PalaceFirstPicture = 0xffff;

			ushort built = 0;

			for (ushort sectionIdx = 0; sectionIdx < 9; sectionIdx++)
			{
				ushort part = PalacePart(sectionIdx);

				if (part != 0xffff)
					built |= (ushort)(1 << (byte)part);
			}

			for (ushort sectionIdx = 1; sectionIdx < 8; sectionIdx++)
			{
				SetPalaceSectionStyleMarkers(sectionIdx, 0xff, 0xff);

				if (PalacePart(sectionIdx) != 0xffff)
					DrawPalaceSection(sectionIdx, 1);
			}

			LoadPalaceCastle(built, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, marker != 0 ? PalaceScreenBackgroundPictureAddress : PalaceDefaultBackgroundPictureAddress, 0);

			if ((short)PalaceGardenBackground > 0)
			{
				string gardenPicture = $"cbacks{(char)(PalaceGardenBackground + '0')}.pic";
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x87, gardenPicture, 0);
			}

			ushort horizLoc = 0x10;

			if (PalaceStyleShift == 0xffff)
				horizLoc -= 0x18;

			if (PalaceLayout == 1)
				horizLoc++;

			const ushort vertLoc = 0x24;

			// The rightmost built section (0x18 is written only when section 8 isn't built)
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
					horizLoc += 0x30;
					continue;
				}

				ushort look = 2;

				if (section < 4)
					look = PalacePart((ushort)(section - 1)) == 0xffff || section == 1 ? (ushort)0 : (ushort)1;

				if (section > 4)
					look = PalacePart((ushort)(section + 1)) == 0xffff || section == 7 ? (ushort)3 : (ushort)1;

				switch (look)
				{
					case 0:
						// A left end with its wing
						if (section == 3)
							DrawPalacePart(part, section, (ushort)(horizLoc + PalaceWingShift), vertLoc, 1);

						if (PalaceStyle(section) != 1 || section == 3)
							DrawPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += PalaceWingShift;

						if (section == 3)
							horizLoc += PalacePartWidth;

						lastLeftEnd = section;
						break;

					case 1:
						// A section between two, joined to the left end before it and to the rightmost one after it
						ushort leftEnd = lastLeftEnd;

						if ((ushort)(leftEnd + 1) == section && PalaceStyle(section) == PalaceStyle(leftEnd))
							DrawPalacePart(part, leftEnd, (ushort)(horizLoc + PalacePartWidth - 17), vertLoc - 2, 0);

						ushort rightEnd = builtRightmost;

						if ((ushort)(rightEnd - 1) == section && PalaceStyle(section) == PalaceStyle(rightEnd))
							DrawPalacePart(part, rightEnd, (ushort)(horizLoc + PalacePartWidth - 17), vertLoc - 2, 1);

						DrawPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += PalacePartWidth;

						DrawPalacePart(part, section, horizLoc, vertLoc, 1);

						horizLoc += PalacePartWidth;
						break;

					case 2:
						// The middle
						if (PalaceLayout == 1)
							horizLoc--;

						DrawPalacePart(part, section, (ushort)(horizLoc - 2), vertLoc, 0);

						horizLoc += PalaceLeftWingWidth;

						if (PalaceLayout == 1)
							horizLoc--;
						break;

					case 3:
						// A right end with its tower
						if (section == 5)
						{
							DrawPalacePart(part, section, horizLoc, vertLoc, 0);

							horizLoc += PalacePartWidth;
						}

						ushort level = PalacePart((ushort)(section - 1));

						if (PalaceStyle(section) == 1 && (short)level > (short)PalacePart(section))
						{
							DrawPalacePart(part, section, (ushort)(horizLoc - 3), (ushort)(level - PalacePart(section) + vertLoc + 1), 1);
						}
						else
						{
							DrawPalacePart(part, section, (ushort)(horizLoc - (PalaceStyle(section) == 1 ? 3 : 2)), vertLoc, 1);
						}

						horizLoc += PalaceRightWingWidth;
						break;
				}
			}

			// The wing of the last left end, unless it is section 3
			ushort lastLeft = lastLeftEnd;

			if (PalaceStyle(lastLeft) == 1 && lastLeft != 3)
			{
				DrawPalacePart(PalacePart(lastLeft), lastLeft, (ushort)((lastLeft * 48) - 36),
					(ushort)(PalacePart((ushort)(lastLeft + 1)) - PalacePart(lastLeft) + vertLoc), 0);
			}

			this.Owner.Helpers.ReleaseResource(PalaceFirstPicture, PalaceGraphicsResourceAddress);

			if (PalaceLeftBushes != 0 || PalaceRightBushes != 0)
			{
				ushort leftBush = 0, rightBush = 0;

				if (PalaceLeftBushes != 0)
				{
					leftBush = DrawPalaceBush(PalaceLeftBushesAddress, 0x2e, 0);
				}

				if (PalaceRightBushes != 0)
				{
					rightBush = DrawPalaceBush(PalaceRightBushesAddress, 0x2f, 184);
				}

				this.Owner.Helpers.ReleaseResource(PalaceLeftBushes != 0 ? leftBush : rightBush, 0);
			}

			if (marker != 0)
			{
				DrawingBox.DisplayIdx = 0;

				if (VgaMode != 0 && (short)FirstMissingDisk > 7)
				{
					this.Owner.Services.FillColors(5, 0, 0, 0);
				}

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, PalacePaletteAddress, 1);

				this.Owner.Startup.AskForDisk(0, 1);
			}

			this.Processor.Journal.CloseScope("ImportPalaceGraphics");
		}

		/// <inheritdoc/>
		public void DrawPalaceSection(ushort sectionIdx, ushort marker)
		{
			this.Processor.Journal.OpenScope($"RenderPalaceSection({sectionIdx}, {marker})");

			short section = (short)sectionIdx;
			ushort level = PalaceSectionLevel(sectionIdx);

			// 0: left end, 1: inner section, 2: middle, 3: right end
			int place = 2;

			if (section < 4)
			{
				place = 1;

				if (PalaceSectionLevel((ushort)(sectionIdx - 1)) == 0xffff || sectionIdx == 1)
				{
					place = 0;
				}
			}

			if (section > 4)
			{
				place = 1;

				if (PalaceSectionLevel((ushort)(sectionIdx + 1)) == 0xffff || sectionIdx == 7)
				{
					place = 3;
				}
			}

			switch (place)
			{
				case 0:
					SetPalaceSectionStyleMarker(sectionIdx, 0, 0x3);

					if (sectionIdx == 3 || marker == 0)
					{
						SetPalaceSectionStyleMarker(sectionIdx, 1, (byte)(level != 0 ? 1 : 2));
					}
					break;

				case 1:
					SetPalaceSectionStyleMarker(sectionIdx, 0, (byte)(LeftNeighbourHigher(sectionIdx) || sectionIdx == 5 ||
						PalaceSectionLevel((ushort)(sectionIdx - 2)) == 0xffff ? 1 : 2));
					SetPalaceSectionStyleMarker(sectionIdx, 1, 0x2);
					break;

				case 2:
					SetPalaceSectionStyleMarker(sectionIdx, 0, 0x0);
					break;

				case 3:
					if (sectionIdx == 5 || marker == 0)
					{
						SetPalaceSectionStyleMarker(sectionIdx, 0, (byte)(LeftNeighbourHigher(sectionIdx) || sectionIdx == 5 ? 1 : 2));
					}

					SetPalaceSectionStyleMarker(sectionIdx, 1, 0x4);
					break;
			}

			this.Processor.Journal.CloseScope("RenderPalaceSection");
		}

		/// <inheritdoc/>
		public void DrawPalacePart(ushort level, ushort section, ushort horizLoc, ushort vertShift, ushort marker)
		{
			this.Processor.Journal.OpenScope($"RenderPalacePart({level}, {section}, {horizLoc}, {vertShift}, {marker})");

			ushort partPlace = PalaceSectionStyle(section);
			sbyte styleShift = (sbyte)PalaceSectionStyleMarker(section, marker);
			ushort heightIdx = (ushort)(styleShift + (5 * partPlace));
			sbyte height = (sbyte)this.Processor.PullGlobalByte((ushort)((0xf * level) + heightIdx + PalacePartHeightTableAddress));
			ushort vertLoc = (ushort)(height + vertShift + level);

			this.Owner.Services.DrawPictureToScreen(ScreenBox,
				(short)horizLoc,
				(short)vertLoc,
				PalacePicture(section, marker));

			this.Processor.Journal.CloseScope("RenderPalacePart");
		}

		/// <inheritdoc/>
		public void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt)
		{
			this.Processor.Journal.OpenScope($"ImportPalaceCastle({levelMask}, {onlyBuilt})");

			ushort level;

			for (level = 0; level < 5; level++)
			{
				if (((1 << level) & levelMask) == 0)
					continue;

				string docname = $"castle{level}.pic";

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, docname, 0);

				ushort section;

				for (section = 1; section < 8; section++)
				{
					if (PalaceSectionLevel(section) != level && onlyBuilt != 0)
						continue;

					ushort place = PalaceSectionStyle(section);

					if (onlyBuilt == 0)
					{
						place = (ushort)(section - 1);
					}

					ushort horizLoc = (ushort)((place & 1) * 0xa0);
					ushort vertLoc = (ushort)((place & 2) * 0x32);

					for (ushort partIdx = 0; partIdx < 2; partIdx++)
					{
						ushort picture = PalacePictureAddress(section, partIdx);
						sbyte style = (sbyte)PalaceSectionStyleMarker(section, partIdx);
						bool middle = onlyBuilt != 0 ? PalaceSectionStyle(section) == 1 : section == 2;

						ushort picHandle = 0;
						switch (style)
						{
							case 0:
								picHandle = this.Owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 1), 0x34, 0x63);
								break;

							case 1:
								picHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x35), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 2:
								picHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x4e), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 3:
								if (middle)
									picHandle = this.Owner.VideoCard.ScreenToPicture(1, 0xa0, 0x65, 0x23, 0x63);
								else
									picHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x67), (ushort)(vertLoc + 1), 0x1c, 0x63);
								break;

							case 4:
								if (middle)
									picHandle = this.Owner.VideoCard.ScreenToPicture(1, 0xc4, 0x65, 0x23, 0x63);
								else
									picHandle = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x84), (ushort)(vertLoc + 1), 0x1b, 0x63);
								break;
						}

						if (style is >= 0 and <= 4)
						{
							SetPalacePicture(section, partIdx, picHandle);
						}

						if (style != -1 && PalaceFirstPicture == 0xffff)
						{
							PalaceFirstPicture = PalacePicture(section, partIdx);
						}
					}

				}
			}

			this.Processor.Journal.CloseScope("ImportPalaceCastle");
		}

		private static ushort PalacePictureAddress(ushort sectionIdx, ushort partIdx)
		{
			return (ushort)(PalacePicturesTableAddress + (sectionIdx << 2) + (partIdx << 1));
		}

		private ushort PalaceSectionLevel(ushort sectionIdx)
		{
			return this.Processor.PullGlobalWord((ushort)(PalaceSectionLevelsAddress + (sectionIdx << 1)));
		}

		private void SetPalaceSectionLevel(ushort sectionIdx, ushort level)
		{
			this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (sectionIdx << 1)), level);
		}

		private ushort PalaceGardenLevel(ushort gardenIdx)
		{
			return this.Processor.PullGlobalWord((ushort)(PalaceGardenLevelsAddress + (gardenIdx << 1)));
		}

		private ushort PalaceSectionStyle(ushort sectionIdx)
		{
			return this.Processor.PullGlobalWord((ushort)(PalaceSectionStylesAddress + (sectionIdx << 1)));
		}

		private void SetPalaceSectionStyle(ushort sectionIdx, ushort style)
		{
			this.Processor.EmitGlobalWord((ushort)(PalaceSectionStylesAddress + (sectionIdx << 1)), style);
		}

		private byte PalaceSectionStyleMarker(ushort sectionIdx, ushort markerIdx)
		{
			return this.Processor.PullGlobalByte((ushort)(PalaceSectionStyleMarkersAddress + (sectionIdx << 1) + markerIdx));
		}

		private void SetPalaceSectionStyleMarker(ushort sectionIdx, ushort markerIdx, byte marker)
		{
			this.Processor.EmitGlobalByte((ushort)(PalaceSectionStyleMarkersAddress + (sectionIdx << 1) + markerIdx), marker);
		}

		private void SetPalaceSectionStyleMarkers(ushort sectionIdx, byte marker0, byte marker1)
		{
			ushort addr = (ushort)(PalaceSectionStyleMarkersAddress + (sectionIdx << 1));
			this.Processor.EmitGlobalByte(addr, marker0);
			this.Processor.EmitGlobalByte((ushort)(addr + 1), marker1);
		}

		private ushort PalacePicture(ushort sectionIdx, ushort partIdx)
		{
			return this.Processor.PullGlobalWord(PalacePictureAddress(sectionIdx, partIdx));
		}

		private void SetPalacePicture(ushort sectionIdx, ushort partIdx, ushort pictureHandle)
		{
			this.Processor.EmitGlobalWord(PalacePictureAddress(sectionIdx, partIdx), pictureHandle);
		}

		private void UpgradePalaceSectionInteractive(ushort dataSeg)
		{
			this.Owner.Startup.ShowMouseCursor();

			ushort selectedSection;
			while (true)
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.GlobDb3a == 0 && this.Owner.Libc.KeyAvailable() == 0);

				if (this.Owner.Libc.KeyAvailable() != 0)
				{
					ushort navKey = this.Owner.Dialogs.ReadNavigationKey();
					selectedSection = (short)navKey >= 0x38
						? (ushort)(navKey - 0x58)
						: (ushort)(navKey - 0x30);
					if ((short)selectedSection < 1 || (short)selectedSection > 0xb)
					{
						continue;
					}
				}
				else if ((short)this.Owner.GlobDb3e >= 0x9a)
				{
					selectedSection = (ushort)(CPU.SignedQuotient((short)this.Owner.GlobDb3c, 0x6b) + 9);
				}
				else
				{
					selectedSection = (ushort)(CPU.SignedQuotient((short)(this.Owner.GlobDb3c + 0xc), 0x30) + 1);
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
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			ushort nextLevel = (ushort)this.Owner.Helpers.ClampValue(
				(short)PalaceSectionLevel(selectedSection) + 1,
				1, 4);

			ushort selectedStyle = 0;
			if ((short)selectedSection < 9)
			{
				selectedStyle = this.SelectPalaceArchitectureStyle(dataSeg, selectedSection, nextLevel);
			}

			SetPalaceSectionLevel(selectedSection, nextLevel);
			SetPalaceSectionStyle(selectedSection, selectedStyle);

			DrawingBox.DisplayIdx = 0x1;
			LoadPalaceGraphics(0);
			DrawingBox.DisplayIdx = 0x0;

			this.Owner.VideoCard.DrawScreenToMainScreen(1, 0x14);
			this.Owner.TurnLogic.WaitForKey();
			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		private ushort SelectPalaceArchitectureStyle(ushort dataSeg, ushort selectedSection, ushort nextLevel)
		{
			for (ushort sec = 1; (short)sec < 8; sec++)
			{
				SetPalaceSectionStyleMarkers(sec, 0xff, 0xff);
			}

			DrawPalaceSection(selectedSection, 0);

			ushort selWordOff = (ushort)(selectedSection << 1);
			byte marker0 = PalaceSectionStyleMarker(selectedSection, 0);
			byte marker1 = PalaceSectionStyleMarker(selectedSection, 1);
			for (ushort styleIdx = 1; (short)styleIdx <= 3; styleIdx++)
			{
				SetPalaceSectionStyleMarkers(styleIdx, marker0, marker1);
			}

			PalaceFirstPicture = 0xffff;

			LoadPalaceCastle((ushort)(1 << (byte)nextLevel), 0);

			short boxHeight = 0x5c;
			this.Owner.Services.FloodBox(ScreenBox, 16, 2, 288, 102, Hues.LightBlue);
			this.Owner.TextPainter.DrawCenteredTextToScreen0(PalaceStyleChoiceHeaderAddress, 160, 3, Hues.White);

			for (ushort optIdx = 1; (short)optIdx <= 3; optIdx++)
			{
				short drawX = (short)(0x64 * (short)optIdx - 0x50);
				ushort boxColor = VgaMode == 0 ? (ushort)3 : (ushort)1;
				this.Owner.Services.FloodBox(ScreenBox, drawX, 10, 80, boxHeight, boxColor);

				drawX = (short)(drawX + 0x10);
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
								ScreenBox,
								drawX + (short)PalaceWingShift,
								this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable2Address)) + 1,
								PalacePicture(optIdx, 1));
						}

						this.Owner.Services.DrawPictureToScreen(
							ScreenBox,
							drawX,
							this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable4Address)) + 1,
							PalacePicture(optIdx, 0));
						break;

					case 1:
						this.Owner.Services.DrawPictureToScreen(
							ScreenBox,
							drawX,
							this.Processor.PullInt8(dataSeg, (ushort)(5 * optIdx + 0xf * nextLevel + PalacePartHeightTable2Address)) + 1,
							PalacePicture(optIdx, 0));

						drawX = (short)(drawX + (short)PalacePartWidth);
						if (this.Owner.TurnHooks.ClassicPalaceStyleGlitch)
						{
							this.Owner.Services.DrawPictureToScreen(
								ScreenBox,
								drawX,
								(short)(this.Processor.PullInt8(dataSeg, (ushort)(selWordOff + PalacePartHeightTable3Address)) + 1),
								this.Processor.PullUInt16(dataSeg, (ushort)(6 + PalacePicturesTableAddress + 2)));
						}
						else
						{
							this.Owner.Services.DrawPictureToScreen(
								ScreenBox,
								drawX,
								(short)(this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable3Address)) + 1),
								PalacePicture(optIdx, 1));
						}
						break;

					case 2:
						this.Owner.Services.DrawPictureToScreen(
							ScreenBox,
							drawX,
							(short)(this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable1Address)) + 1),
							PalacePicture(optIdx, 0));
						break;

					case 3:
						if (selectedSection != 7)
						{
							this.Owner.Services.DrawPictureToScreen(
								ScreenBox,
								drawX,
								this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable2Address)) + 1,
								PalacePicture(optIdx, 0));
							drawX = (short)(drawX + (short)PalacePartWidth);
						}

						short towerShift = optIdx == 2 ? (short)3 : (short)2;
						this.Owner.Services.DrawPictureToScreen(
							ScreenBox,
							(short)(drawX - towerShift),
							(short)(this.Processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + PalacePartHeightTable5Address)) + 1),
							PalacePicture(optIdx, 1));
						break;
				}

				EmptyMessage()
					.Number((short)optIdx);
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), (short)optIdx * 100 - 44, 12, Hues.White);
			}

			this.Owner.Helpers.ReleaseResource(PalaceFirstPicture, PalaceCastleResourceAddress);
			this.Owner.TurnLogic.WaitForKey();
			this.Owner.Startup.ShowMouseCursor();

			ushort chosenStyle;
			while (true)
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.GlobDb3a == 0 && this.Owner.Libc.KeyAvailable() == 0);

				if (this.Owner.Libc.KeyAvailable() == 0)
				{
					chosenStyle = (ushort)CPU.SignedQuotient((short)this.Owner.GlobDb3c, 0x6b);
					break;
				}

				chosenStyle = (ushort)(this.Owner.Dialogs.ReadNavigationKey() - 0x31);
				if ((short)chosenStyle >= 0 && (short)chosenStyle <= 2)
				{
					break;
				}
			}

			this.Owner.Startup.HideMouseCursor();
			DrawingBox.DisplayIdx = 0x1;
			LoadPalaceGraphics(0);

			ushort screen0Ptr = ScreenBox;
			this.Processor.EmitUInt16(dataSeg, screen0Ptr, 0x0);
			this.Owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				screen0Ptr,
				0, 0);

			return chosenStyle;
		}

		/// <summary>The part built in the palace section (0xd91e + 2 * section), 0xffff for none</summary>
		private ushort PalacePart(ushort sectionIdx)
		{
			return PalaceSectionLevel(sectionIdx);
		}

		/// <summary>The style of the palace section (0xb222 + 2 * section)</summary>
		private ushort PalaceStyle(ushort sectionIdx)
		{
			return PalaceSectionStyle(sectionIdx);
		}

		/// <summary>A bush of the palace garden: cbrush picture 2 * size + first from the word sizeAddr, at x horizLoc</summary>
		private ushort DrawPalaceBush(ushort sizeAddr, byte first, int horizLoc)
		{
			char bushChar = (char)((this.Processor.PullGlobalByte(sizeAddr) << 1) + first);
			string pictureName = $"cbrush{bushChar}.pic";

			ushort picture = this.Owner.ImageToolkit.LoadIcon(pictureName);

			this.Owner.Services.DrawPictureToScreen(ScreenBox, horizLoc,
				(short)this.Processor.PullGlobalWord(sizeAddr) <= 1 ? 105 : 94, picture);

			return picture;
		}

		/// <summary>The section to the left is higher than the section</summary>
		private bool LeftNeighbourHigher(ushort sectionIdx)
		{
			return (short)PalaceSectionLevel((ushort)(sectionIdx - 1)) > (short)PalaceSectionLevel(sectionIdx);
		}
	}
}
