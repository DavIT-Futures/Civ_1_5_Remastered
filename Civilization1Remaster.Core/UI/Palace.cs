using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class Palace : GameModule
	{
		public Palace(GameEngine owner)
			: base(owner)
		{
		}

/// <summary>
		/// ?
		/// </summary>
		public void DisplayPalace()
		{
			this.processor.Journal.OpenScope("DisplayPalace()");

			this.processor.PreserveIndexRegisters();

			ushort dataSeg = this.processor.SegData.Value16;
			ushort textBuf = (ushort)(this.processor.RegStack.Value16 - 0x300);

			this.owner.Startup.ConcealMouseCursor();
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x4ade);
			this.owner.Libc.CopyText(textBuf, TextBuffer.Address);
			this.owner.Startup.QueryForDisk(2, 1);

			DrawingBox.DisplayIdx = 0x1;
			ImportPalaceGraphics(0);
			DrawingBox.DisplayIdx = 0x0;

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			this.owner.Helpers.HaltHueCycling();

			ushort chiefVisuals = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals;
			this.owner.Services.PerformSound(LeaderWord(chiefVisuals, LeaderTable.Tune), 3);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4ae6, 1);

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.owner.Dialogs.DisplayMenu(textBuf, 20, 16, 1);

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0x14, 0x10, 0x118, 0x2c,
				ScreenBox,
				0x14, 0x10);

			this.owner.VideoCard.RenderImage(
				ScreenBox,
				0, 0, 0x140, 0xc8,
				BackScreenBox,
				0, 0);

			this.processor.EmitGlobalWord(0xdb38, 0x1);
			this.owner.Dialogs.DisplayMenu(0x4af0, 40, 16, 1);

			ushort hasOptions = 0;
			for (ushort sec = 1; (short)sec < 8; sec++)
			{
				ushort secWordOff = (ushort)(sec << 1);
				short level = (short)this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd91e));
				if (level < 4 &&
					((short)this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd91c)) > 0 ||
					 (short)this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd920)) > 0 ||
					 level >= 0))
				{
					short labelX = (short)(0x30 * (short)sec - 0x24);
					EmptyMessage()
						.Number((short)sec);
					this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, labelX, 144, 14);
					hasOptions = 1;
				}
			}

			for (ushort gardenIdx = 0; (short)gardenIdx < 3; gardenIdx++)
			{
				ushort gardenWordOff = (ushort)(gardenIdx << 1);
				if ((short)this.processor.PullUInt16(dataSeg, (ushort)(gardenWordOff + 0xd930)) < 3)
				{
					this.owner.Words.Set("A");
					this.processor.EmitGlobalByte(TextBuffer.Address, (byte)(this.owner.Words[0] + (byte)gardenIdx));
					this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, (short)gardenIdx * 120 + 40, 160, 14);
					hasOptions = 1;
				}
			}

			if (hasOptions != 0)
			{
				this.UpgradePalaceSectionInteractive(dataSeg);
			}

			this.owner.Services.DisplayScreen(0);
			this.owner.Services.PerformSound(1, 0);
			this.owner.Startup.QueryForDisk(0, 1);

			if (VgaMode == 0)
			{
				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			}

			this.owner.TurnHooks.RepaintMainScreen();
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.Journal.CloseScope("DisplayPalace");
		}

		private void UpgradePalaceSectionInteractive(ushort dataSeg)
		{
			this.owner.Startup.DisplayMouseCursor();

			ushort selectedSection;
			while (true)
			{
				do
				{
					this.owner.Startup.ObtainMouseState();
				}
				while (this.owner.GlobDb3a == 0 && this.owner.Libc.KeyAvailable() == 0);

				if (this.owner.Libc.KeyAvailable() != 0)
				{
					ushort navKey = this.owner.Dialogs.ObtainNavigationKey();
					selectedSection = (short)navKey >= 0x38
						? (ushort)(navKey - 0x58)
						: (ushort)(navKey - 0x30);
					if ((short)selectedSection < 1 || (short)selectedSection > 0xb)
					{
						continue;
					}
				}
				else if ((short)this.owner.GlobDb3e >= 0x9a)
				{
					selectedSection = (ushort)(CPU.SignedQuotient((short)this.owner.GlobDb3c, 0x6b) + 9);
				}
				else
				{
					selectedSection = (ushort)(CPU.SignedQuotient((short)(this.owner.GlobDb3c + 0xc), 0x30) + 1);
				}

				ushort selWordOff = (ushort)(selectedSection << 1);
				if ((short)this.processor.PullUInt16(dataSeg, (ushort)(selWordOff + 0xd91e)) >= 4)
				{
					continue;
				}

				if ((short)selectedSection >= 9 &&
					(short)this.processor.PullUInt16(dataSeg, (ushort)(selWordOff + 0xd91e)) >= 3)
				{
					continue;
				}

				break;
			}

			if ((short)selectedSection < 9)
			{
				while ((short)selectedSection < 4)
				{
					ushort secWordOff = (ushort)(selectedSection << 1);
					if ((short)this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd920)) > 0 ||
						this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd91e)) != 0xffff)
					{
						break;
					}

					selectedSection++;
				}

				while ((short)selectedSection > 4)
				{
					ushort secWordOff = (ushort)(selectedSection << 1);
					if ((short)this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd91c)) > 0 ||
						this.processor.PullUInt16(dataSeg, (ushort)(secWordOff + 0xd91e)) != 0xffff)
					{
						break;
					}

					selectedSection--;
				}
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			ushort nextLevel = (ushort)this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(dataSeg, (ushort)((selectedSection << 1) + 0xd91e)) + 1,
				1, 4);

			ushort selectedStyle = 0;
			if ((short)selectedSection < 9)
			{
				selectedStyle = this.SelectPalaceArchitectureStyle(dataSeg, selectedSection, nextLevel);
			}

			ushort finalWordOff = (ushort)(selectedSection << 1);
			this.processor.EmitUInt16(dataSeg, (ushort)(finalWordOff + 0xd91e), nextLevel);
			this.processor.EmitUInt16(dataSeg, (ushort)(finalWordOff + 0xb222), selectedStyle);

			DrawingBox.DisplayIdx = 0x1;
			ImportPalaceGraphics(0);
			DrawingBox.DisplayIdx = 0x0;

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x14);
			this.owner.TurnLogic.AwaitForKey();
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();
		}

		private ushort SelectPalaceArchitectureStyle(ushort dataSeg, ushort selectedSection, ushort nextLevel)
		{
			for (ushort sec = 1; (short)sec < 8; sec++)
			{
				ushort secWordOff = (ushort)(sec << 1);
				this.processor.EmitUInt8(dataSeg, (ushort)(secWordOff + 0xb25f), 0xff);
				this.processor.EmitUInt8(dataSeg, (ushort)(secWordOff + 0xb25e), 0xff);
			}

			RenderPalaceSection(selectedSection, 0);

			ushort selWordOff = (ushort)(selectedSection << 1);
			for (ushort styleIdx = 1; (short)styleIdx <= 3; styleIdx++)
			{
				ushort styleWordOff = (ushort)(styleIdx << 1);
				this.processor.EmitUInt8(dataSeg, (ushort)(styleWordOff + 0xb25e), this.processor.PullUInt8(dataSeg, (ushort)(selWordOff + 0xb25e)));
				this.processor.EmitUInt8(dataSeg, (ushort)(styleWordOff + 0xb25f), this.processor.PullUInt8(dataSeg, (ushort)(selWordOff + 0xb25f)));
			}

			this.processor.RegSource.Value16 = selWordOff;
			this.processor.RegDest.Value16 = 6;
			PalaceFirstPicture = 0xffff;

			ImportPalaceCastle((ushort)(1 << (byte)nextLevel), 0);

			short boxHeight = 0x5c;
			this.owner.Services.FloodBox(ScreenBox, 16, 2, 288, 102, 9);
			this.owner.TextPainter.RenderCenteredTextToScreen0(0x4b13, 160, 3, 15);

			for (ushort optIdx = 1; (short)optIdx <= 3; optIdx++)
			{
				short drawX = (short)(0x64 * (short)optIdx - 0x50);
				ushort boxColor = VgaMode == 0 ? (ushort)3 : (ushort)1;
				this.owner.Services.FloodBox(ScreenBox, drawX, 10, 80, boxHeight, boxColor);

				drawX = (short)(drawX + 0x10);
				ushort look = 2;
				if ((short)selectedSection < 4)
				{
					look = (this.processor.PullUInt16(dataSeg, (ushort)(selWordOff + 0xd91c)) == 0xffff || selectedSection == 1)
						? (ushort)0
						: (ushort)1;
				}

				if ((short)selectedSection > 4)
				{
					look = (this.processor.PullUInt16(dataSeg, (ushort)(selWordOff + 0xd920)) == 0xffff || selectedSection == 7)
						? (ushort)3
						: (ushort)1;
				}

				switch (look)
				{
					case 0:
						if (selectedSection != 1)
						{
							this.owner.Services.RenderPictureToScreen(
								ScreenBox,
								drawX + (short)PalaceWingShift,
								this.processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + 0x4b32)) + 1,
								this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23e)));
						}

						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							drawX,
							this.processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + 0x4b34)) + 1,
							this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23c)));
						break;

					case 1:
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							drawX,
							this.processor.PullInt8(dataSeg, (ushort)(5 * optIdx + 0xf * nextLevel + 0x4b32)) + 1,
							this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23c)));

						drawX = (short)(drawX + (short)PalacePartWidth);
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							drawX,
							(short)(this.processor.PullInt8(dataSeg, (ushort)(this.processor.RegSource.Value16 + 0x4b33)) + 1),
							this.processor.PullUInt16(dataSeg, (ushort)(this.processor.RegDest.Value16 + 0xb23e)));
						break;

					case 2:
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							drawX,
							(short)(this.processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + 0x4b31)) + 1),
							this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23c)));
						break;

					case 3:
						if (selectedSection != 7)
						{
							this.owner.Services.RenderPictureToScreen(
								ScreenBox,
								drawX,
								this.processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + 0x4b32)) + 1,
								this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23c)));
							drawX = (short)(drawX + (short)PalacePartWidth);
						}

						short towerShift = optIdx == 2 ? (short)3 : (short)2;
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							(short)(drawX - towerShift),
							(short)(this.processor.PullInt8(dataSeg, (ushort)(0xf * nextLevel + 5 * optIdx + 0x4b35)) + 1),
							this.processor.PullUInt16(dataSeg, (ushort)((optIdx << 2) + 0xb23e)));
						break;
				}

				EmptyMessage()
					.Number((short)optIdx);
				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, (short)optIdx * 100 - 44, 12, 15);
			}

			this.owner.Helpers.ReleaseResource(PalaceFirstPicture, 0x4b27);
			this.owner.TurnLogic.AwaitForKey();
			this.owner.Startup.DisplayMouseCursor();

			ushort chosenStyle;
			while (true)
			{
				do
				{
					this.owner.Startup.ObtainMouseState();
				}
				while (this.owner.GlobDb3a == 0 && this.owner.Libc.KeyAvailable() == 0);

				if (this.owner.Libc.KeyAvailable() == 0)
				{
					chosenStyle = (ushort)CPU.SignedQuotient((short)this.owner.GlobDb3c, 0x6b);
					break;
				}

				chosenStyle = (ushort)(this.owner.Dialogs.ObtainNavigationKey() - 0x31);
				if ((short)chosenStyle >= 0 && (short)chosenStyle <= 2)
				{
					break;
				}
			}

			this.owner.Startup.ConcealMouseCursor();
			DrawingBox.DisplayIdx = 0x1;
			ImportPalaceGraphics(0);

			ushort screen0Ptr = ScreenBox;
			this.processor.EmitUInt16(dataSeg, screen0Ptr, 0x0);
			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				screen0Ptr,
				0, 0);

			return chosenStyle;
		}

		/// <summary>
		/// Draws the palace on screen 1 (the parts of sections 0 - 8 in the words at 0xd91e, 0xffff not built): the looks of
		/// sections 1 - 7, the castle, the background (0x4b81 for the palace screen, 0x4b89), the garden "cbacksN.pic" (N =
		/// 0xd932), the sections from x 0x10 (8 further left without section 2, one with 0xb22a = 1): the left ones with their
		/// wings, the middle one, the right ones with their towers, each joined to a neighbour of the same style (0xb222), and
		/// the bushes "cbrushN.pic" left (0xd930) and right (0xd934). For the palace screen (marker) screen 1 is shown with the
		/// palette 0x4bb0. The file name is in the stack frame, like in the original, and the rightmost built section (0x18)
		/// and the last left section (0x12) are read from the frame even when this call didn't set them, like in the original.
		/// </summary>
		public void ImportPalaceGraphics(ushort marker)
		{
			this.processor.Journal.OpenScope($"ImportPalaceGraphics({marker})");

			// Locals 0x12 and 0x18 of the original (B3: in C#), read before written on some paths: they start at 0
			ushort lastLeftEnd = 0;
			ushort builtRightmost = 0;

			this.owner.Startup.QueryForDisk(2, 1);

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
				this.processor.EmitGlobalByte((ushort)((sectionIdx << 1) + 0xb25f), 0xff);
				this.processor.EmitGlobalByte((ushort)((sectionIdx << 1) + 0xb25e), 0xff);

				if (PalacePart(sectionIdx) != 0xffff)
					RenderPalaceSection(sectionIdx, 1);
			}

			ImportPalaceCastle(built, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, marker != 0 ? (ushort)0x4b81 : (ushort)0x4b89, 0);

			if ((short)this.processor.PullGlobalWord(0xd932) > 0)
			{
				string gardenPicture = $"cbacks{(char)(this.processor.PullGlobalByte(0xd932) + '0')}.pic";
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x87, gardenPicture, 0);
			}

			ushort horizLoc = 0x10;

			if (this.processor.PullGlobalWord(0xd922) == 0xffff)
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
							RenderPalacePart(part, section, (ushort)(horizLoc + PalaceWingShift), vertLoc, 1);

						if (PalaceStyle(section) != 1 || section == 3)
							RenderPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += PalaceWingShift;

						if (section == 3)
							horizLoc += PalacePartWidth;

						lastLeftEnd = section;
						break;

					case 1:
						// A section between two, joined to the left end before it and to the rightmost one after it
						ushort leftEnd = lastLeftEnd;

						if ((ushort)(leftEnd + 1) == section && PalaceStyle(section) == PalaceStyle(leftEnd))
							RenderPalacePart(part, leftEnd, (ushort)(horizLoc + PalacePartWidth - 17), vertLoc - 2, 0);

						ushort rightEnd = builtRightmost;

						if ((ushort)(rightEnd - 1) == section && PalaceStyle(section) == PalaceStyle(rightEnd))
							RenderPalacePart(part, rightEnd, (ushort)(horizLoc + PalacePartWidth - 17), vertLoc - 2, 1);

						RenderPalacePart(part, section, horizLoc, vertLoc, 0);

						horizLoc += PalacePartWidth;

						RenderPalacePart(part, section, horizLoc, vertLoc, 1);

						horizLoc += PalacePartWidth;
						break;

					case 2:
						// The middle
						if (PalaceLayout == 1)
							horizLoc--;

						RenderPalacePart(part, section, (ushort)(horizLoc - 2), vertLoc, 0);

						horizLoc += this.processor.PullGlobalWord(0x4b2c);

						if (PalaceLayout == 1)
							horizLoc--;
						break;

					case 3:
						// A right end with its tower
						if (section == 5)
						{
							RenderPalacePart(part, section, horizLoc, vertLoc, 0);

							horizLoc += PalacePartWidth;
						}

						ushort level = PalacePart((ushort)(section - 1));

						if (PalaceStyle(section) == 1 && (short)level > (short)PalacePart(section))
						{
							RenderPalacePart(part, section, (ushort)(horizLoc - 3), (ushort)(level - PalacePart(section) + vertLoc + 1), 1);
						}
						else
						{
							RenderPalacePart(part, section, (ushort)(horizLoc - (PalaceStyle(section) == 1 ? 3 : 2)), vertLoc, 1);
						}

						horizLoc += this.processor.PullGlobalWord(0x4b34);
						break;
				}
			}

			// The wing of the last left end, unless it is section 3
			ushort lastLeft = lastLeftEnd;

			if (PalaceStyle(lastLeft) == 1 && lastLeft != 3)
			{
				RenderPalacePart(PalacePart(lastLeft), lastLeft, (ushort)((lastLeft * 48) - 36),
					(ushort)(PalacePart((ushort)(lastLeft + 1)) - PalacePart(lastLeft) + vertLoc), 0);
			}

			this.owner.Helpers.ReleaseResource(PalaceFirstPicture, 0x4b9f);

			if (PalaceLeftBushes != 0 || PalaceRightBushes != 0)
			{
				ushort leftBush = 0, rightBush = 0;

				if (PalaceLeftBushes != 0)
				{
					leftBush = RenderPalaceBush(0xd930, 0x2e, 0);
				}

				if (PalaceRightBushes != 0)
				{
					rightBush = RenderPalaceBush(0xd934, 0x2f, 184);
				}

				this.owner.Helpers.ReleaseResource(PalaceLeftBushes != 0 ? leftBush : rightBush, 0);
			}

			if (marker != 0)
			{
				DrawingBox.DisplayIdx = 0;

				if (VgaMode != 0 && (short)FirstMissingDisk > 7)
				{
					this.owner.Services.FloodColors(5, 0, 0, 0);
				}

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4bb0, 1);

				this.owner.Startup.QueryForDisk(0, 1);
			}

			// Far return
			this.processor.Journal.CloseScope("ImportPalaceGraphics");
		}

		/// <summary>The part built in the palace section (0xd91e + 2 * section), 0xffff for none</summary>
		private ushort PalacePart(ushort sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xd91e));
		}

		/// <summary>The style of the palace section (0xb222 + 2 * section)</summary>
		private ushort PalaceStyle(ushort sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xb222));
		}

		/// <summary>A bush of the palace garden: cbrush picture 2 * size + first from the word sizeAddr, at x horizLoc</summary>
		private ushort RenderPalaceBush(ushort sizeAddr, byte first, int horizLoc)
		{
			char bushChar = (char)((this.processor.PullGlobalByte(sizeAddr) << 1) + first);
			string pictureName = $"cbrush{bushChar}.pic";

			ushort picture = this.owner.ImageToolkit.ImportIcon(pictureName);

			this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc,
				(short)this.processor.PullGlobalWord(sizeAddr) <= 1 ? 105 : 94, picture);

			return picture;
		}

		/// <summary>
		/// The look of the palace section arg1 (0 - 7, levels in the words at 0xd91c...) by its neighbours: the style (0xb25e
		/// + 2 * section) and the shape (0xb25f + 2 * section). Section 4 is the middle; a left section (0 - 3) without a
		/// built neighbour to the left (or section 1) is the left end, a right section (5 - 7) without one to the right (or
		/// section 7) the right end.
		/// </summary>
		public void RenderPalaceSection(ushort arg1, ushort marker)
		{
			this.processor.Journal.OpenScope($"RenderPalaceSection({arg1}, {marker})");

			short section = (short)arg1;
			ushort levels = (ushort)(arg1 << 1);
			ushort style = (ushort)(levels + 0xb25e);
			ushort shape = (ushort)(levels + 0xb25f);
			ushort level = this.processor.PullGlobalWord((ushort)(levels + 0xd91e));

			// 0: left end, 1: inner section, 2: middle, 3: right end
			int place = 2;

			if (section < 4)
			{
				place = 1;

				if (this.processor.PullGlobalWord((ushort)(levels + 0xd91c)) == 0xffff || arg1 == 1)
				{
					place = 0;
				}
			}

			if (section > 4)
			{
				place = 1;

				if (this.processor.PullGlobalWord((ushort)(levels + 0xd920)) == 0xffff || arg1 == 7)
				{
					place = 3;
				}
			}

			switch (place)
			{
				case 0:
					this.processor.EmitGlobalByte(style, 0x3);

					if (arg1 == 3 || marker == 0)
					{
						this.processor.EmitGlobalByte(shape, (byte)(level != 0 ? 1 : 2));
					}
					break;

				case 1:
					this.processor.EmitGlobalByte(style, (byte)(LeftNeighbourHigher(levels) || arg1 == 5 ||
						this.processor.PullGlobalWord((ushort)(levels + 0xd91a)) == 0xffff ? 1 : 2));
					this.processor.EmitGlobalByte(shape, 0x2);
					break;

				case 2:
					this.processor.EmitGlobalByte(style, 0x0);
					break;

				case 3:
					if (arg1 == 5 || marker == 0)
					{
						this.processor.EmitGlobalByte(style, (byte)(LeftNeighbourHigher(levels) || arg1 == 5 ? 1 : 2));
					}

					this.processor.EmitGlobalByte(shape, 0x4);
					break;
			}

			// Far return
			this.processor.Journal.CloseScope("RenderPalaceSection");
		}

		/// <summary>The section to the left (levels at 0xd91c) is higher than the section (0xd91e)</summary>
		private bool LeftNeighbourHigher(ushort levels)
		{
			return (short)this.processor.PullGlobalWord((ushort)(levels + 0xd91c)) > (short)this.processor.PullGlobalWord((ushort)(levels + 0xd91e));
		}

		/// <summary>
		/// One part of the palace: picture (table at 0xb23c, by the part arg2 and its style marker) at column arg3; the row comes
		/// from the height table at 0x4b36 (records of 15 bytes by arg1, by the place of the part) plus arg4 and arg1
		/// </summary>
		public void RenderPalacePart(ushort arg1, ushort arg2, ushort arg3, ushort arg4, ushort marker)
		{
			this.processor.Journal.OpenScope($"RenderPalacePart({arg1}, {arg2}, {arg3}, {arg4}, {marker})");

			ushort partPlace = this.processor.PullGlobalWord((ushort)((arg2 << 1) + 0xb222));
			sbyte styleShift = (sbyte)this.processor.PullGlobalByte((ushort)(marker + (arg2 << 1) + 0xb25e));
			ushort heightIdx = (ushort)(styleShift + (5 * partPlace));
			sbyte height = (sbyte)this.processor.PullGlobalByte((ushort)((0xf * arg1) + heightIdx + 0x4b36));
			ushort vertLoc = (ushort)(height + arg4 + arg1);

			this.owner.Services.RenderPictureToScreen(ScreenBox,
				(short)arg3,
				(short)vertLoc,
				this.processor.PullGlobalWord((ushort)((marker << 1) + (arg2 << 2) + 0xb23c)));

			this.processor.Journal.CloseScope("RenderPalacePart");
		}

		/// <summary>
		/// Cuts the pictures of the palace sections 1 - 7 from the castle pictures castle0 - castle4.pic (each one in the mask
		/// arg1): with arg2 the sections of that level (0xd91e) from the place of the section (0xb222: bit 0 the column, bit 1 the
		/// row), without arg2 all sections from the place of the section before. Each section has two parts (styles at 0xb25e:
		/// 0 the whole wall, 1 and 2 left and right pieces, 3 and 4 the ends, cut from the middle picture for section 2 or a
		/// section at the place 1), the picture goes to 0xb23c + 4 * section + 2 * part, the first one also to 0x6828 when unset.
		/// </summary>
		public void ImportPalaceCastle(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ImportPalaceCastle({arg1}, {arg2})");

			ushort level;

			for (level = 0; level < 5; level++)
			{
				if (((1 << level) & arg1) == 0)
					continue;

				string docname = $"castle{level}.pic";

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docname, 0);

				ushort section;

				for (section = 1; section < 8; section++)
				{
					if (this.processor.PullGlobalWord((ushort)((section << 1) + 0xd91e)) != level && arg2 != 0)
						continue;

					ushort place = this.processor.PullGlobalWord((ushort)((section << 1) + 0xb222));

					if (arg2 == 0)
					{
						place = (ushort)(section - 1);
					}

					ushort horizLoc = (ushort)((place & 1) * 0xa0);
					ushort vertLoc = (ushort)((place & 2) * 0x32);

					for (ushort partIdx = 0; partIdx < 2; partIdx++)
					{
						ushort picture = (ushort)((section << 2) + (partIdx << 1) + 0xb23c);
						sbyte style = (sbyte)this.processor.PullGlobalByte((ushort)((section << 1) + 0xb25e + partIdx));
						bool middle = arg2 != 0 ? this.processor.PullGlobalWord((ushort)((section << 1) + 0xb222)) == 1 : section == 2;

						switch (style)
						{
							case 0:
								this.owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 1), 0x34, 0x63);
								break;

							case 1:
								this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x35), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 2:
								this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x4e), (ushort)(vertLoc + 1), 0x18, 0x63);
								break;

							case 3:
								if (middle)
									this.owner.VideoCard.ScreenToPicture(1, 0xa0, 0x65, 0x23, 0x63);
								else
									this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x67), (ushort)(vertLoc + 1), 0x1c, 0x63);
								break;

							case 4:
								if (middle)
									this.owner.VideoCard.ScreenToPicture(1, 0xc4, 0x65, 0x23, 0x63);
								else
									this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x84), (ushort)(vertLoc + 1), 0x1b, 0x63);
								break;
						}

						if (style is >= 0 and <= 4)
						{
							this.processor.EmitGlobalWord(picture, this.processor.RegAcc.Value16);
						}

						if (style != -1 && PalaceFirstPicture == 0xffff)
						{
							PalaceFirstPicture = this.processor.PullGlobalWord(picture);
						}
					}

				}
			}

			// Far return
			this.processor.Journal.CloseScope("ImportPalaceCastle");
		}
	}
}
