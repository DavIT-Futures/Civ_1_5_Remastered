
namespace Civilization1Remaster.Core.UI
{
	public class Encyclopedia : GameComponent, IEncyclopedia
	{
		private const ushort CivilopediaFileName = 0x3fa4;
		private const ushort CivilopediaFileNumberOffset = 0x3fa9;
		private const ushort CivilopediaConceptTitlesTableAddress = 0x3c64;
		private const ushort CivilopediaNoneStringAddress = 0x3dee;
		private const ushort CivilopediaAllowsStringAddress = 0x3e03;
		private const ushort CivilopediaPageHistorySegment = 0x3772;
		private const ushort CivilopediaChosenIndexTableAddress = 0x1c80;
		private const ushort CivilopediaChosenCategoryTableAddress = 0x36fc;
		private const ushort CivilopediaSciencePaletteAddress = 0x3ce6;
		private const ushort CivilopediaBuildingPaletteAddress = 0x3d14;
		private const ushort CivilopediaUnitPaletteAddress = 0x3d4e;
		private const ushort CivilopediaUnitIconsTableAddress = 0xd58e;
		private const ushort TerrainPaletteBufferAddress = 0xc5be;

		private static readonly GameText CivilopediaScienceCategoryHeaderAddress = new(0x3cf2, "Civilization Advance");
		private static readonly GameText CivilopediaBuildingsPictureAddress = new(0x3d07, "citypix2.pic");
		private static readonly GameText CivilopediaBuildingCategoryHeaderAddress = new(0x3d1d, "City Improvement");
		private static readonly GameText CivilopediaWonderCategoryHeaderAddress = new(0x3d2e, "Wonder of the World");
		private static readonly GameText CivilopediaUnitsCategoryHeaderAddress = new(0x3d5a, "Military Unit");
		private static readonly GameText CivilopediaTerrainPaletteAddress = new(0x3d75, "sp256.pal");
		private static readonly GameText CivilopediaTerrainCategoryHeaderAddress = new(0x3d7f, "Terrain Type");
		private static readonly GameText CivilopediaConceptsCategoryHeaderAddress = new(0x3d8c, "Game Concept");

		public Encyclopedia(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ShowCivilopedia(ushort part)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilopedia({part})");

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.HideMouseCursor();

			ushort firstEntry = 0;
			ushort answer;

			do
			{
				answer = ShowCivilopediaIndex(firstEntry, part);

				if (answer == 0xffff)
				{
					firstEntry = Turn.CivilopediaNextPage != 0 ? (ushort)(firstEntry + 0x4e) : (ushort)0;
				}
			}
			while (answer != 0xfffe);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.TurnHooks.RepaintMainScreen();

			this.Owner.Helpers.LaunchHueCycling();

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public ushort ShowCivilopediaIndex(ushort firstEntry, ushort part)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilopediaIndex({firstEntry}, {part})");

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Yellow);

			this.Owner.Services.FloodBox(Screen.Box, 60, 2, 200, 9, Hues.White);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(GameTexts.TextEncyclopaediaOfCivilization, 161, 4, Hues.Black);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(GameTexts.TextEncyclopaediaOfCivilizationB, 160, 3, Hues.LightGreen);

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextExitB, 286, 4, Hues.LightRed);

			if (part == 0xffff)
			{
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextMoreB, 8, 4, Hues.LightRed);
			}

			this.Owner.Services.FloodBox(Screen.Box, 2, 14, 316, 184, Hues.White);

			short width = (short)((short)part > 0 ? 0x96 : 0x64);
			ushort entryTally = ListCivilopediaEntries(firstEntry, part, width);

			ushort answer = (ushort)(Mouse.Present != 0
				? ChooseCivilopediaEntryWithMouse(firstEntry, width, entryTally)
				: ChooseCivilopediaEntryWithKeys(firstEntry, width, entryTally));

			return answer;
		}

/// <summary>
/// The Civilopedia page of the entry of the part (0 technologies, 1 improvements and wonders, 2 units, 3 terrains, 4
/// concepts): the title, the picture and the texts of the file BLURBn
/// </summary>
		public void ShowCivilopediaRecord(ushort scienceIdx, ushort part)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilopediaRecord({scienceIdx}, {part})");

			ushort dataSeg = this.Processor.SegData.Value16;
			this.Processor.EmitGlobalByte(CivilopediaFileNumberOffset, (byte)((byte)part + 0x30));

			this.Owner.Helpers.HaltHueCycling();
			this.Owner.Startup.HideMouseCursor();

			ushort diskStatus = 0xffff;
			if (Files.DiskInUse != 0x3)
			{
				diskStatus = this.Owner.Startup.AskForDisk(3, 0);
				if (diskStatus == 0)
				{
					Turn.CivilopediaWithoutPictures = 0x1;
				}
			}

			ushort bgColor = 0xf;
			if (Screen.VgaMode != 0)
			{
				if (Turn.CivilopediaWithoutPictures == 0)
				{
					bgColor = 0xfd;
					this.Owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));
				}
				else
				{
					bgColor = 0xff;
				}
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, bgColor);
			this.Owner.UnitOps.DrawBusyIndicator();

			DrawingBox.TypefaceIdx = 0x7;
			this.Owner.Words.Clear();

			short titleX = part == 0x2 ? (short)0xe0 : (short)0xcc;
			if (part == 0x4 ||
				(part == 0x1 && (short)scienceIdx > 0x18) ||
				Turn.CivilopediaWithoutPictures != 0)
			{
				titleX = 0xa0;
			}

			this.DrawCivilopediaRecordHeader(dataSeg, scienceIdx, part, titleX);

			DrawingBox.TypefaceIdx = 0x6;
			this.Owner.Words.Uppercase();
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), titleX, 20, Hues.Black);
			DrawingBox.TypefaceIdx = 0x7;

			string tag = "*" + this.Owner.Words.ToString();
			this.Owner.Words.Clear();

			short cursorY = 0x4c;
			if (diskStatus != 0)
			{
				if (((byte)(this.Owner.World.SessionPrefMarkers & 0xff) & 0x40) != 0)
				{
					ushort imported = this.Owner.BlockText.LoadTextFromFile(CivilopediaFileName, tag);
					if ((ushort)(imported + 1) != 0)
					{
						this.Owner.BlockText.DrawTextChunk(37, 12, 76, 1);
						this.Owner.Conquest.WaitForKeyPressOrMouseClick();
						this.Owner.Services.FloodBox(Screen.Box, 8, 76, 304, 116, bgColor);
					}
				}

				tag += "2";
				this.Owner.Words.Clear();
				if (part != 0)
				{
					this.Owner.BlockText.LoadTextFromFile(CivilopediaFileName, tag);
					cursorY = (short)((ushort)this.Owner.BlockText.DrawTextChunk(36, 12, cursorY, 1) + 8);
				}
			}

			switch (part)
			{
				case 0:
					this.DrawTechnologyCivilopediaDetails(dataSeg, scienceIdx, titleX, bgColor, cursorY);
					break;
				case 1:
					this.DrawImprovementCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
				case 2:
					this.DrawUnitCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
				case 3:
					this.DrawTerrainCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
			}

			DrawingBox.TypefaceIdx = 0x1;
			this.Owner.Startup.ShowMouseCursor();
			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
			this.Owner.Startup.HideMouseCursor();

			if ((short)diskStatus == 1)
			{
				this.Owner.Startup.AskForDisk(0, 1);
			}

			Turn.CivilopediaWithoutPictures = 0x0;
			if (King.Answer == 0xffff)
			{
				this.Owner.Services.FloodBox(Screen.Box, 8, 8, 304, 184, bgColor);
				this.Owner.Helpers.LaunchHueCycling();
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public ushort ObtainFirstPrerequisite(ushort scienceIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainFirstPrerequisite({scienceIdx})");

			ushort found = FindInScienceList(0x0, 0x48, scienceIdx);

			return found;
		}

		/// <inheritdoc/>
		public void DrawIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHoriz, ushort centreVert)
		{
			using var _ = this.Processor.Journal.Scope($"RenderIcon({left}, {top}, {breadth}, {height}, {centreHoriz}, {centreVert})");

			if ((short)(left + breadth) > 0x13f)
			{
				breadth = (ushort)(0x13f - left);
			}

			if ((short)(top + height) > 0xc7)
			{
				height = (ushort)(0xc7 - top);
			}

			short half = (short)((short)breadth / 2);

			if ((short)centreHoriz < half)
			{
				left += (ushort)(half - centreHoriz);
				breadth = (ushort)(centreHoriz << 1);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, left, top, breadth, height,
				Screen.Box, (ushort)(centreHoriz - (breadth >> 1)), (ushort)(centreVert - (height >> 1)));
		}

		/// <inheritdoc/>
		public ushort ObtainSecondPrerequisite(ushort scienceIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainSecondPrerequisite({scienceIdx})");

			ushort found = FindInScienceList(0x44, 0x1c, scienceIdx);

			return found;
		}

		/// <inheritdoc/>
		public ushort ObtainTechnologyCategory(ushort scienceIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTechnologyCategory({scienceIdx})");

			ushort found = FindInScienceList(0x61, 0xc, scienceIdx);

			return found;
		}

		/// <summary>
		/// Lists the entries after "a" in alphabetical order (each round the smallest name after the last one), the entries
		/// from firstEntry on in columns of the width from (10, 16), 7 a line, until column 300; 0x6808 = 1 when the page is
		/// full. The number of entries passed (including those before firstEntry).
		/// </summary>
		private ushort ListCivilopediaEntries(ushort firstEntry, ushort part, short width)
		{
			short horizLoc = 0xa;
			short vertLoc = 0x10;
			ushort entryTally = 0;

			// The last name listed and the next one (the original kept them under SP)
			string lastName = "a";

			while (true)
			{
				string nextName = "zzz";

				ushort nextIdx = 0xffff;
				ushort nextKind = 0xffff;

				// Technologies, improvements and wonders (1 - 0x2d), units, terrains, concepts (names through 0x3c64)
				FindNextCivilopediaEntry(part, 0, 0, 0x43, idx => TechnologyText(idx), lastName, ref nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 1, 1, 0x2e, idx => BuildingRecordText(idx), lastName, ref nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 2, 0, 0x1c, idx => UnitText(idx), lastName, ref nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 3, 0, 0xc, idx => TerrainText(idx), lastName, ref nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 4, 0, 0x15, idx => this.Processor.PullGlobalWord((ushort)((idx << 1) + CivilopediaConceptTitlesTableAddress)), lastName, ref nextName, ref nextIdx, ref nextKind);

				if (nextKind == 0xffff)
					break;

				if ((short)entryTally >= (short)firstEntry)
				{
					string shortName = this.Owner.BlockText.ShrinkTextToWidth(nextName, (ushort)width);

					this.Owner.TextPainter.DrawTextToScreen0(shortName, horizLoc, vertLoc, Hues.Black);

					ushort line = (ushort)((entryTally - firstEntry) << 1);

					this.Processor.EmitUInt16(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenIndexTableAddress), nextIdx);
					this.Processor.EmitUInt16(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenCategoryTableAddress), nextKind);

					vertLoc += 7;

					if (vertLoc > 0xc0)
					{
						horizLoc += width;
						vertLoc = 0x10;
					}
				}

				entryTally++;

				lastName = nextName;

				if (horizLoc >= 0x12c)
					break;
			}

			Turn.CivilopediaNextPage = (ushort)(horizLoc >= 0x12c ? 1 : 0);

			return entryTally;
		}

		/// <summary>
		/// For the part (or all parts, -1): the entry first - end - 1 whose name comes after the last listed name and before the
		/// next found so far becomes the next one
		/// </summary>
		private void FindNextCivilopediaEntry(ushort part, ushort kind, short first, short end, System.Func<short, ushort> nameOf,
			string lastName, ref string nextName, ref ushort nextIdx, ref ushort nextKind)
		{
			if (part != kind && part != 0xffff)
				return;

			for (short idx = first; idx < end; idx++)
			{
				string name = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, nameOf(idx)));

				if (this.Owner.Libc.CompareTextNoCase(name, lastName) <= 0)
					continue;

				if (this.Owner.Libc.CompareTextNoCase(name, nextName) >= 0)
					continue;

				nextIdx = (ushort)idx;
				nextKind = kind;

				nextName = name;
			}
		}

		/// <summary>
		/// The mouse: waits for a click or a key; the line under the click (above the list: row -1, column 2 for a right click
		/// or a key, which is taken), then ShowCivilopediaChoice
		/// </summary>
		private ushort ChooseCivilopediaEntryWithMouse(ushort firstEntry, short width, ushort entryTally)
		{
			this.Owner.Startup.ShowMouseCursor();

			do
			{
				this.Owner.Startup.ReadMouseState();

				if (this.Owner.GlobDb3a != 0)
					break;
			}
			while (this.Owner.Libc.KeyAvailable() == 0);

			short column = CPU.SignedQuotient((short)(this.Owner.GlobDb3c - 0xa), width);
			short row = (short)this.Owner.GlobDb3e < 0x10 ? (short)-1 : CPU.SignedQuotient((short)(this.Owner.GlobDb3e - 0x10), 7);
			ushort chosen = (ushort)((0x1a * column) + row);

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.GlobDb3a == 2)
			{
				row = -1;
				column = 2;
			}

			if (this.Owner.Libc.KeyAvailable() != 0)
			{
				this.Owner.Dialogs.ReadNavigationKey();

				row = -1;
				column = 2;
			}

			return ShowCivilopediaChoice(chosen, row, column, firstEntry, entryTally);
		}

		/// <summary>
		/// The keys: the line is highlighted (colour 15 to 10 while waiting for a key) and moved with the arrows; "m" = the next
		/// page, Esc or "x" = leave, Enter or space = ShowCivilopediaChoice
		/// </summary>
		private ushort ChooseCivilopediaEntryWithKeys(ushort firstEntry, short width, ushort entryTally)
		{
			short column = 0;
			short row = 0;
			short shown = (short)(entryTally - firstEntry);
			ushort chosen;

			while (true)
			{
				short horizLoc = (short)((column * width) + 8);
				short vertLoc = (short)((7 * row) + 0xf);

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizLoc, vertLoc, width - 2, 8, Hues.White, Hues.LightGreen);

				ushort key = this.Owner.Dialogs.ReadNavigationKey();

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizLoc, vertLoc, width - 2, 8, Hues.LightGreen, Hues.White);

				switch (key)
				{
					case KeyCodes.Up:
						if (row != 0)
						{
							row--;
						}
						else if (column != 0)
						{
							column--;
							row = 0x19;
						}

						break;

					case KeyCodes.Left:
						if (column != 0)
						{
							column--;
						}

						break;

					case KeyCodes.Right:
						if ((short)((0x1a * column) + row + 0x1a) < shown)
						{
							column++;
						}

						break;

					case KeyCodes.Down:
						if ((short)((0x1a * column) + row + 1) < shown)
						{
							if (row >= 0x19)
							{
								row = 0;
								column++;
							}
							else
							{
								row++;
							}
						}

						break;
				}

				if (key == 'm')
					return 0xffff;

				if (key == KeyCodes.Escape || key == 'x')
					return 0xfffe;

				chosen = (ushort)((0x1a * column) + row);

				if (key == KeyCodes.Enter || key == KeyCodes.Space)
					break;
			}

			return ShowCivilopediaChoice(chosen, row, column, firstEntry, entryTally);
		}

		/// <summary>
		/// The chosen line: 0 when no entry is there; above the list 0xfffe (column not 0) or 0xffff; else the record of the
		/// entry (the Civilopedia disk 3 first). Then a key is awaited.
		/// </summary>
		private ushort ShowCivilopediaChoice(ushort chosen, short row, short column, ushort firstEntry, ushort entryTally)
		{
			if (row >= 0 && (short)(entryTally - firstEntry) <= (short)chosen)
				return 0;

			if (row >= 0)
			{
				if (Files.DiskInUse != 3)
				{
					this.Owner.Startup.AskForDisk(3, 1);
				}

				ushort line = (ushort)(chosen << 1);

				ShowCivilopediaRecord(this.Processor.PullUInt16(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenIndexTableAddress)),
					this.Processor.PullUInt16(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenCategoryTableAddress)));
			}
			else
			{
				chosen = (ushort)(column != 0 ? 0xfffe : 0xffff);
			}

			this.Owner.TurnLogic.WaitForKey();

			return chosen;
		}

		private void DrawCivilopediaRecordHeader(ushort dataSeg, ushort scienceIdx, ushort part, short titleX)
		{
			switch (part)
			{
				case 0:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainFirstPrerequisite(scienceIdx);
						this.Owner.Words.Set("iconpg1.pic");
						byte pageDigit = (byte)CPU.SignedQuotient((short)iconIdx, 9);
						this.Owner.Words[6] = (byte)(this.Owner.Words[6] + pageDigit);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaSciencePaletteAddress);
						DrawIcon(
							(ushort)(((iconIdx % 3) * 111) + 1),
							(ushort)((((iconIdx % 9) / 3) * 69) + 1),
							110, 68, 62, 42);
					}

					this.Owner.Words.Set(TechnologyText((short)scienceIdx));
					this.Owner.TextPainter.DrawCenteredTextToScreen0(CivilopediaScienceCategoryHeaderAddress, titleX, 36, Hues.LightGray);
					break;

				case 1:
					if (this.Owner.Startup.AskForDisk(1, 0) == 0)
					{
						Turn.CivilopediaWithoutPictures = 0x1;
					}

					if ((short)scienceIdx > 1 && (short)scienceIdx <= 0x15 && Turn.CivilopediaWithoutPictures == 0)
					{
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, CivilopediaBuildingsPictureAddress, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaBuildingPaletteAddress);
						ushort iconSlot = (ushort)(scienceIdx - 2);
						ushort picHandle = this.Owner.VideoCard.ScreenToPicture(
							1,
							(ushort)(((iconSlot >> 2) * 0x32) + 1),
							(ushort)(((iconSlot & 3) * 0x32) + 1),
							0x31, 0x31);
						this.Owner.Services.DrawPictureToScreen(Screen.Box, 37, 17, picHandle);
						this.Owner.Helpers.ReleaseResource(picHandle, 0);
					}

					this.Owner.Words.Set(BuildingRecordText((short)scienceIdx));
					this.Owner.TextPainter.DrawCenteredTextToScreen0(
						(short)scienceIdx > 0x18 ? CivilopediaWonderCategoryHeaderAddress : CivilopediaBuildingCategoryHeaderAddress,
						titleX, 36, Hues.LightGray);
					break;

				case 2:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainSecondPrerequisite(scienceIdx);
						this.Owner.Words.Set("iconpga.pic");
						byte pageChar = (byte)CPU.SignedQuotient((short)iconIdx, 6);
						this.Owner.Words[6] = (byte)(this.Owner.Words[6] + pageChar);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaUnitPaletteAddress);
						DrawIcon(
							(ushort)(((iconIdx & 1) * 160) + 1),
							(ushort)(((iconIdx % 6) >> 1) * 61),
							158, 61, 88, 42);
					}

					this.Owner.Words.Set(UnitText((short)scienceIdx));
					this.Owner.TextPainter.DrawCenteredTextToScreen0(CivilopediaUnitsCategoryHeaderAddress, titleX, 36, Hues.LightGray);
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						216, 48,
						this.Processor.PullUInt16(dataSeg, (ushort)((scienceIdx << 1) + CivilopediaUnitIconsTableAddress)));
					break;

				case 3:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainTechnologyCategory(scienceIdx);
						this.Owner.Words.Set("iconpgt1.pic");
						byte pageDigit = (byte)CPU.SignedQuotient((short)iconIdx, 6);
						this.Owner.Words[7] = (byte)(this.Owner.Words[7] + pageDigit);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						if (Screen.VgaMode != 0)
						{
							this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, CivilopediaTerrainPaletteAddress, TerrainPaletteBufferAddress);
							this.Owner.VideoCard.AssignHuesFromHueStruct(TerrainPaletteBufferAddress);
							this.Owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));
						}

						DrawIcon(
							(ushort)((((short)iconIdx % 3) * 107) + 1),
							(ushort)(((((short)iconIdx % 6) / 3) * 87) + 5),
							106, 77, 76, 46);
					}

					EmptyMessage()
						.Text(TerrainText(scienceIdx));
					this.Owner.TextPainter.DrawCenteredTextToScreen0(CivilopediaTerrainCategoryHeaderAddress, titleX, 36, Hues.LightGray);
					break;

				case 4:
					this.Owner.Words.Append(this.Processor.PullUInt16(dataSeg, (ushort)((scienceIdx << 1) + CivilopediaConceptTitlesTableAddress)));
					this.Owner.TextPainter.DrawCenteredTextToScreen0(CivilopediaConceptsCategoryHeaderAddress, titleX, 36, Hues.LightGray);
					break;
			}
		}

		private void DrawTechnologyCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short titleX, ushort bgColor, short cursorY)
		{
			cursorY = (short)(cursorY + 8);
			this.Owner.Words.Clear();

			ushort techSpecAddr = (ushort)(0x16 * scienceIdx);
			byte req1 = this.Processor.PullUInt8(dataSeg, (ushort)(techSpecAddr + TechnologyTable.Prerequisite1Address));
			if (req1 != 0xff)
			{
				Message("Requires ")
					.Text(TechnologyText((short)(sbyte)req1));
			}

			byte req2 = this.Processor.PullUInt8(dataSeg, (ushort)(techSpecAddr + TechnologyTable.Prerequisite2Address));
			if (req2 != 0xff)
			{
				this.Owner.Words.Append(" and ");
				this.Owner.Words.Append(TechnologyText((short)(sbyte)req2));
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 32, cursorY, Hues.Blue);
			cursorY = (short)(cursorY + 0x10);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextAllows, 32, cursorY, Hues.Blue);
			cursorY = (short)(cursorY + 8);
			short listStartY = cursorY;

			for (ushort otherTech = 0; (short)otherTech < 0x48; otherTech++)
			{
				ushort otherAddr = (ushort)(0x16 * otherTech);
				ushort otherReq1 = (ushort)(short)(sbyte)this.Processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ee));
				if (otherReq1 == scienceIdx)
				{
					this.Owner.Words.Set((ushort)(otherAddr + 0x4da));
					byte companion = this.Processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ef));
					if (companion != 0xff)
					{
						this.Owner.Words.Append(" (with ");
						this.Owner.Words.Append(TechnologyText((short)(sbyte)companion));
						this.Owner.Words.Append(")");
					}

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 40, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}

				ushort otherReq2 = (ushort)(short)(sbyte)this.Processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ef));
				if (otherReq2 == scienceIdx)
				{
					this.Owner.Words.Set((ushort)(otherAddr + 0x4da));
					byte companion = this.Processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ee));
					if (companion != 0xff)
					{
						this.Owner.Words.Append(" (with ");
						this.Owner.Words.Append(TechnologyText((short)(sbyte)companion));
						this.Owner.Words.Append(")");
					}

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 40, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}
			}

			cursorY = (short)(cursorY + 4);
			for (ushort unitIdx = 0; (short)unitIdx < 0x1c; unitIdx++)
			{
				ushort unitSpec = (ushort)(0x22 * unitIdx);
				if (UnitKindWordAt(unitSpec, UnitKindTable.Technology) == scienceIdx)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						40,
						cursorY - 4,
						this.Processor.PullUInt16(
							dataSeg,
							(ushort)((((unitIdx + (this.Owner.World.PersonParticipantIdx << 5)) + 0x40) << 1) + 0xd4ce)));

					Message(UnitTextAt(unitSpec))
						.Text(" unit");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 60, cursorY, Hues.LightRed);
					cursorY = (short)(cursorY + 0xc);
				}
			}

			for (ushort impIdx = 1; (short)impIdx <= 0x2d; impIdx++)
			{
				ushort impSpec = (ushort)(0x1e * impIdx);
				if (BuildingWordAt(impSpec, BuildingTable.Technology) == scienceIdx)
				{
					this.Owner.CityOps.DrawResourceIcon((short)impIdx, 40, (short)(cursorY - 2));
					Message(BuildingRecordTextAt(impSpec))
						.Text((short)impIdx > 0x18 ? " Wonder" : " improvement");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 60, cursorY, Hues.Green);
					cursorY = (short)(cursorY + 0xc);
				}
			}

			if ((short)(listStartY + 4) == cursorY)
			{
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextAllowsB, 32, cursorY - 12, bgColor);
			}

			ushort obtainedFrom = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ScienceObtainedSince[scienceIdx];
			if (obtainedFrom == 0xfffe)
			{
				this.Owner.Words.Set(CivilopediaNoneStringAddress);
			}
			else if (obtainedFrom == 0xffff)
			{
				this.Owner.Words.Clear();
			}
			else if (obtainedFrom == 0)
			{
				this.Owner.Words.Set(CivilopediaAllowsStringAddress);
			}
			else
			{
				Message("(Taken from ")
					.Text(CivilizationPlural(obtainedFrom))
					.Text(")");
			}

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), titleX, 48, Hues.LightGray);
		}

		private void DrawImprovementCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			ushort impSpec = (ushort)(0x1e * scienceIdx);
			this.Owner.Words.Set("Requires ");
			ushort reqTech = BuildingWordAt(impSpec, BuildingTable.Technology);
			this.Owner.Words.Append(TechnologyText((short)reqTech));
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 12, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Cost: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)(0xa * (short)BuildingWordAt(impSpec, BuildingTable.Price)), 10));
			this.Owner.Words.Append(" shields.");
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 12, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Maintenance: $");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)BuildingWordAt(impSpec, BuildingTable.Upkeep), 10));
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 12, cursorY, Hues.LightRed);
		}

		private void DrawUnitCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			ushort unitSpec = (ushort)(0x22 * scienceIdx);
			this.Owner.Words.Set("Requires ");
			ushort reqTech = UnitKindWordAt(unitSpec, UnitKindTable.Technology);
			this.Owner.Words.Append(TechnologyText((short)reqTech));
			if (scienceIdx == 0x19)
			{
				this.Owner.Words.Append(" and Nuclear Fission");
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 100, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Cost: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)(0xa * (short)UnitKindWordAt(unitSpec, UnitKindTable.Price)), 10));
			this.Owner.Words.Append(" resources.");
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 100, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Attack Strength: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.AssaultPower), 10));
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 100, cursorY, Hues.LightRed);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Defense Strength: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.GuardPower), 10));
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 100, cursorY, Hues.LightRed);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Movement Rate: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.ShiftTally), 10));
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 100, cursorY, Hues.Black);
		}

		private void DrawTerrainCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			for (ushort variantIdx = scienceIdx; (short)(scienceIdx + 0xc) >= (short)variantIdx; variantIdx = (ushort)(variantIdx + 0xc))
			{
				if (variantIdx != scienceIdx &&
					TerrainField(scienceIdx, TerrainTable.Food) == 2)
				{
					continue;
				}

				ushort varSpec = (ushort)(0x13 * variantIdx);
				this.Owner.TextPainter.DrawTextToScreen0(TerrainTextAt(varSpec), 12, cursorY, Hues.Blue);
				cursorY = (short)(cursorY + 8);

				sbyte foodVal = (sbyte)TerrainFieldAt(varSpec, TerrainTable.Food);
				short irrigEffect = (short)this.Processor.PullUInt16(dataSeg, (ushort)(TerrainImprovementTable.Origin + (0xc * scienceIdx) + TerrainImprovementTable.Irrigation));
				if (foodVal != 0 || irrigEffect < -1)
				{
					Message("Food: ")
						.Number(foodVal);
					if (foodVal >= 3)
					{
						this.Owner.Words.Append("*");
					}

					if (irrigEffect < -1)
					{
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(foodVal - irrigEffect - 1), 10));
						if (foodVal >= 2)
						{
							this.Owner.Words.Append("*");
						}

						this.Owner.Words.Append(" with Irrigation)");
					}

					this.Owner.Words.Append(" units.");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}

				sbyte prodVal = (sbyte)TerrainFieldAt(varSpec, TerrainTable.Shields);
				short mineEffect = (short)this.Processor.PullUInt16(dataSeg, (ushort)(TerrainImprovementTable.Origin + (0xc * scienceIdx) + TerrainImprovementTable.Mining));
				if (prodVal != 0 || mineEffect < -1)
				{
					this.Owner.Words.Set("Production: ");
					if (scienceIdx == 0x2 || scienceIdx == 0xb)
					{
						this.Owner.Words.Append("0/");
					}

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(prodVal, 10));
					if (mineEffect < -1)
					{
						short minedProd = (short)(prodVal - mineEffect - 1);
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Libc.IntegerToText(minedProd, 10));
						if (minedProd >= 3)
						{
							this.Owner.Words.Append("*");
						}

						this.Owner.Words.Append(" with Mining)");
					}

					this.Owner.Words.Append(" units.");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}

				sbyte tradeVal = (sbyte)TerrainFieldAt(varSpec, TerrainTable.Trade);
				if (tradeVal != 0 || (short)scienceIdx <= 2)
				{
					Message("Trade: ")
						.Number(tradeVal);
					if (tradeVal != 0)
					{
						this.Owner.Words.Append("%");
					}

					if (tradeVal >= 3)
					{
						this.Owner.Words.Append("*");
					}

					if ((short)scienceIdx <= 2)
					{
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(tradeVal + 1), 10));
						this.Owner.Words.Append("% with Roads)");
					}

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}

				if (variantIdx == 0x7)
				{
					this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextNothing, 16, cursorY, Hues.LightBlue);
					cursorY = (short)(cursorY + 8);
				}

				cursorY = (short)(cursorY + 4);
			}

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.Topic1IfGovernmentIsDespotismAnarchy, 16, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 8);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.Text1IfGovernmentIsRepublicDemocracy, 16, cursorY, Hues.LightBlue);
			cursorY = (short)(cursorY + 0xc);

			ushort baseSpec = (ushort)(0x13 * scienceIdx);
			this.Owner.Words.Set("Movement cost: ");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText((sbyte)TerrainFieldAt(baseSpec, TerrainTable.MoveCost), 10));
			this.Owner.Words.Append(" MP");
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 12, cursorY, Hues.LightRed);
			cursorY = (short)(cursorY + 8);

			this.Owner.Words.Set("Defense bonus: +");
			this.Owner.Words.Append(
				this.Owner.Libc.IntegerToText(
					(short)((0x32 * (short)(sbyte)TerrainFieldAt(baseSpec, TerrainTable.DefenseBonus)) - 0x64),
					10));
			this.Owner.Words.Append("%");
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 12, cursorY, Hues.LightRed);
		}

		/// <summary>
		/// The first index of the technology in a list of signed bytes in the segment 0x371e. Like the original, ES is left
		/// at the segment and BX at the last index looked at.
		/// </summary>
		/// <returns>Index, or 0xffff when the technology is not in the list; also in AX</returns>
		private ushort FindInScienceList(ushort origin, int tally, ushort scienceIdx)
		{
			ushort found = 0xffff;

			for (int idx = 0; idx < tally; idx++)
			{
				this.Processor.SegExtra.Value16 = 0x371e;

				if ((ushort)(sbyte)this.Processor.PullUInt8(this.Processor.SegExtra.Value16, (ushort)(origin + idx)) == scienceIdx)
				{
					found = (ushort)idx;
					break;
				}
			}

			return found;
		}
	}
}
