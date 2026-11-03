namespace Civ_1_5_Remastered.Core.UI.Help
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

		public Encyclopedia(IGameEngine owner)
			: base(owner)
		{
		}

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

		public ushort ShowCivilopediaIndex(ushort firstEntry, ushort part)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilopediaIndex({firstEntry}, {part})");

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Yellow);

			this.Owner.Services.FloodBox(Screen.Box, 60, 2, 200, 9, Hues.White);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(GameTexts.TextEncyclopaediaOfCivilization, 161, 4, Hues.Black);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(GameTexts.TextEncyclopaediaOfCivilizationSecond, 160, 3, Hues.LightGreen);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextExitSecond, 286, 4, Hues.LightRed);

			if (part == 0xffff)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextMoreSecond, 8, 4, Hues.LightRed);
			}

			this.Owner.Services.FloodBox(Screen.Box, 2, 14, 316, 184, Hues.White);

			short width = (short)((short)part > 0 ? 0x96 : 0x64);
			ushort entryTally = ListCivilopediaEntries(firstEntry, part, width);

			ushort answer = (ushort)(Mouse.Present != 0
				? ChooseCivilopediaEntryWithMouse(firstEntry, width, entryTally)
				: ChooseCivilopediaEntryWithKeys(firstEntry, width, entryTally));

			return answer;
		}

		public void ShowCivilopediaRecord(ushort scienceIndex, ushort part)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilopediaRecord({scienceIndex}, {part})");

			ushort dataSegment = this.Processor.SegmentData.Word;
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

			ushort backgroundColor = 0xf;
			if (Screen.VideoGraphicsMode != 0)
			{
				if (Turn.CivilopediaWithoutPictures == 0)
				{
					backgroundColor = 0xfd;
					this.Owner.VideoCard.AssignHue(0xfd, VideoGraphicsBitmap.HueFromDigitalLevels(0x3d, 0x3d, 0x3d));
				}
				else
				{
					backgroundColor = 0xff;
				}
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, backgroundColor);
			this.Owner.UnitOperations.DrawBusyIndicator();

			DrawingBox.TypefaceIndex = 0x7;
			this.Owner.Words.Clear();

			short titleHorizontal = part == 0x2 ? (short)0xe0 : (short)0xcc;
			if (part == 0x4 ||
				(part == 0x1 && (short)scienceIndex > 0x18) ||
				Turn.CivilopediaWithoutPictures != 0)
			{
				titleHorizontal = 0xa0;
			}

			this.DrawCivilopediaRecordHeader(dataSegment, scienceIndex, part, titleHorizontal);

			DrawingBox.TypefaceIndex = 0x6;
			this.Owner.Words.Uppercase();
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), titleHorizontal, 20, Hues.Black);
			DrawingBox.TypefaceIndex = 0x7;

			string tag = "*" + this.Owner.Words.ToString();
			this.Owner.Words.Clear();

			short cursorVertical = 0x4c;
			if (diskStatus != 0)
			{
				if (((byte)(this.Owner.World.SessionPreferenceMarkers & 0xff) & 0x40) != 0)
				{
					ushort imported = this.Owner.BlockText.LoadTextFromFile(CivilopediaFileName, tag);
					if ((ushort)(imported + 1) != 0)
					{
						this.Owner.BlockText.DrawTextChunk(37, 12, 76, 1);
						this.Owner.Conquest.WaitForKeyPressOrMouseClick();
						this.Owner.Services.FloodBox(Screen.Box, 8, 76, 304, 116, backgroundColor);
					}
				}

				tag += "2";
				this.Owner.Words.Clear();
				if (part != 0)
				{
					if (part == 1 && scienceIndex == 100)
					{
						this.Owner.Words.Set("Wealth allows a city to convert its industrial production into direct revenue for the treasury. A quarter (25%) of all net shields produced by the city each turn are converted directly into gold.\n\nThis continuous project is unlocked upon discovering Banking, providing financial stability when no new constructions are needed.");
						cursorVertical = (short)((ushort)this.Owner.BlockText.DrawTextChunk(36, 12, cursorVertical, 1) + 8);
					}
					else if (part == 1 && scienceIndex == 101)
					{
						this.Owner.Words.Set("Research allows a city to dedicate its industrial capacity to scientific advancement. A quarter (25%) of all net shields produced by the city each turn are converted directly into scientific inquiry headway.\n\nThis continuous project becomes available with University, accelerating technological discovery.");
						cursorVertical = (short)((ushort)this.Owner.BlockText.DrawTextChunk(36, 12, cursorVertical, 1) + 8);
					}
					else if (part == 1 && scienceIndex == 102)
					{
						this.Owner.Words.Set("Food allows a city to convert its industrial capacity into food production for its citizens. A quarter (25%) of all net shields produced by the city each turn are converted directly into food supplies.\n\nThis continuous project becomes available upon discovering Engineering, sustaining population growth and preventing famine.");
						cursorVertical = (short)((ushort)this.Owner.BlockText.DrawTextChunk(36, 12, cursorVertical, 1) + 8);
					}
					else
					{
						this.Owner.BlockText.LoadTextFromFile(CivilopediaFileName, tag);

						if (part == 1 && WonderRules.IsWonder(scienceIndex))
						{
							this.Owner.Words.Set(WonderRules.CivilopediaEffect(scienceIndex, this.Owner.Words.ToString()));
						}

						cursorVertical = (short)((ushort)this.Owner.BlockText.DrawTextChunk(36, 12, cursorVertical, 1) + 8);
					}
				}
			}

			switch (part)
			{
				case 0:
					this.DrawTechnologyCivilopediaDetails(dataSegment, scienceIndex, titleHorizontal, backgroundColor, cursorVertical);
					break;
				case 1:
					this.DrawImprovementCivilopediaDetails(scienceIndex, cursorVertical);
					break;
				case 2:
					this.DrawUnitCivilopediaDetails(scienceIndex, cursorVertical);
					break;
				case 3:
					this.DrawTerrainCivilopediaDetails(dataSegment, scienceIndex, cursorVertical);
					break;
			}

			DrawingBox.TypefaceIndex = 0x1;
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
				this.Owner.Services.FloodBox(Screen.Box, 8, 8, 304, 184, backgroundColor);
				this.Owner.Helpers.LaunchHueCycling();
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort ObtainFirstPrerequisite(ushort scienceIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainFirstPrerequisite({scienceIndex})");

			ushort found = FindInScienceList(0x0, 0x48, scienceIndex);

			return found;
		}

		public void DrawIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHorizontal, ushort centreVertical)
		{
			using var _ = this.Processor.Journal.Scope($"RenderIcon({left}, {top}, {breadth}, {height}, {centreHorizontal}, {centreVertical})");

			if ((short)(left + breadth) > 0x13f)
			{
				breadth = (ushort)(0x13f - left);
			}

			if ((short)(top + height) > 0xc7)
			{
				height = (ushort)(0xc7 - top);
			}

			short half = (short)((short)breadth / 2);

			if ((short)centreHorizontal < half)
			{
				left += (ushort)(half - centreHorizontal);
				breadth = (ushort)(centreHorizontal << 1);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, left, top, breadth, height,
				Screen.Box, (ushort)(centreHorizontal - (breadth >> 1)), (ushort)(centreVertical - (height >> 1)));
		}

		public ushort ObtainSecondPrerequisite(ushort scienceIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainSecondPrerequisite({scienceIndex})");

			ushort found = FindInScienceList(0x44, 0x1c, scienceIndex);

			return found;
		}

		public ushort ObtainTechnologyCategory(ushort scienceIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTechnologyCategory({scienceIndex})");

			ushort found = FindInScienceList(0x61, 0xc, scienceIndex);

			return found;
		}

		private ushort ListCivilopediaEntries(ushort firstEntry, ushort part, short width)
		{
			short horizontalLocation = 0xa;
			short verticalLocation = 0x10;
			ushort entryTally = 0;

			string lastName = "a";

			while (true)
			{
				string nextName = "zzz";

				ushort nextIndex = 0xffff;
				ushort nextKind = 0xffff;

				FindNextCivilopediaEntry(part, 0, 0, 0x43, index => GameTables.TechnologyText(index), lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextCivilopediaEntry(part, 1, 1, 0x2e, index => GameTables.BuildingRecordText(index), lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextSpecialCivilopediaEntry(part, 1, "Food", 102, lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextSpecialCivilopediaEntry(part, 1, "Research", 101, lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextSpecialCivilopediaEntry(part, 1, "Wealth", 100, lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextCivilopediaEntry(part, 2, 0, 0x1c, index => GameTables.UnitText(index), lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextCivilopediaEntry(part, 3, 0, 0xc, index => GameTables.TerrainText(index), lastName, ref nextName, ref nextIndex, ref nextKind);
				FindNextCivilopediaEntry(part, 4, 0, 0x15, index => this.Processor.PullGlobalWord((ushort)((index << 1) + CivilopediaConceptTitlesTableAddress)), lastName, ref nextName, ref nextIndex, ref nextKind);

				if (nextKind == 0xffff)
				{
					break;
				}

				if ((short)entryTally >= (short)firstEntry)
				{
					string shortName = this.Owner.BlockText.ShrinkTextToWidth(nextName, (ushort)width);

					this.Owner.TextPainter.DrawTextToPrimaryScreen(shortName, horizontalLocation, verticalLocation, Hues.Black);

					ushort line = (ushort)((entryTally - firstEntry) << 1);

					this.Processor.EmitWord(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenIndexTableAddress), nextIndex);
					this.Processor.EmitWord(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenCategoryTableAddress), nextKind);

					verticalLocation += 7;

					if (verticalLocation > 0xc0)
					{
						horizontalLocation += width;
						verticalLocation = 0x10;
					}
				}

				entryTally++;

				lastName = nextName;

				if (horizontalLocation >= 0x12c)
				{
					break;
				}
			}

			Turn.CivilopediaNextPage = (ushort)(horizontalLocation >= 0x12c ? 1 : 0);

			return entryTally;
		}

		private void FindNextSpecialCivilopediaEntry(ushort part, ushort kind, string entryName, ushort entryIndex,
			string lastName, ref string nextName, ref ushort nextIndex, ref ushort nextKind)
		{
			if (part != kind && part != 0xffff)
			{
				return;
			}

			if (string.Compare(lastName, entryName, System.StringComparison.OrdinalIgnoreCase) < 0 &&
				string.Compare(entryName, nextName, System.StringComparison.OrdinalIgnoreCase) < 0)
			{
				nextName = entryName;
				nextIndex = entryIndex;
				nextKind = kind;
			}
		}

		private void FindNextCivilopediaEntry(ushort part, ushort kind, short first, short end, System.Func<short, ushort> nameOf,
			string lastName, ref string nextName, ref ushort nextIndex, ref ushort nextKind)
		{
			if (part != kind && part != 0xffff)
			{
				return;
			}

			for (short index = first; index < end; index++)
			{
				string name = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, nameOf(index)));

				if (this.Owner.Runtime.CompareTextNoCase(name, lastName) <= 0)
				{
					continue;
				}

				if (this.Owner.Runtime.CompareTextNoCase(name, nextName) >= 0)
				{
					continue;
				}

				nextIndex = (ushort)index;
				nextKind = kind;

				nextName = name;
			}
		}

		private ushort ChooseCivilopediaEntryWithMouse(ushort firstEntry, short width, ushort entryTally)
		{
			this.Owner.Startup.ShowMouseCursor();

			do
			{
				this.Owner.Startup.ReadMouseState();

				if (this.Owner.MenuPointerState != 0)
				{
					break;
				}
			}
			while (this.Owner.Runtime.KeyAvailable() == 0);

			short column = CentralProcessor.SignedQuotient((short)(this.Owner.MenuPointerHorizontal - 0xa), width);
			short row = (short)this.Owner.MenuPointerVertical < 0x10 ? (short)-1 : CentralProcessor.SignedQuotient((short)(this.Owner.MenuPointerVertical - 0x10), 7);
			ushort chosen = (ushort)((0x1a * column) + row);

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.MenuPointerState == 2)
			{
				row = -1;
				column = 2;
			}

			if (this.Owner.Runtime.KeyAvailable() != 0)
			{
				this.Owner.Dialogs.ReadNavigationKey();

				row = -1;
				column = 2;
			}

			return ShowCivilopediaChoice(chosen, row, column, firstEntry, entryTally);
		}

		private ushort ChooseCivilopediaEntryWithKeys(ushort firstEntry, short width, ushort entryTally)
		{
			short column = 0;
			short row = 0;
			short shown = (short)(entryTally - firstEntry);
			ushort chosen;

			while (true)
			{
				short horizontalLocation = (short)((column * width) + 8);
				short verticalLocation = (short)((7 * row) + 0xf);

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation, verticalLocation, width - 2, 8, Hues.White, Hues.LightGreen);

				ushort key = this.Owner.Dialogs.ReadNavigationKey();

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation, verticalLocation, width - 2, 8, Hues.LightGreen, Hues.White);

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
				{
					return 0xffff;
				}

				if (key == KeyCodes.Escape || key == 'x')
				{
					return 0xfffe;
				}

				chosen = (ushort)((0x1a * column) + row);

				if (key == KeyCodes.Enter || key == KeyCodes.Space)
				{
					break;
				}
			}

			return ShowCivilopediaChoice(chosen, row, column, firstEntry, entryTally);
		}

		private ushort ShowCivilopediaChoice(ushort chosen, short row, short column, ushort firstEntry, ushort entryTally)
		{
			if (row >= 0 && (short)(entryTally - firstEntry) <= (short)chosen)
			{
				return 0;
			}

			if (row >= 0)
			{
				if (Files.DiskInUse != 3)
				{
					this.Owner.Startup.AskForDisk(3, 1);
				}

				ushort line = (ushort)(chosen << 1);

				ShowCivilopediaRecord(this.Processor.PullWord(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenIndexTableAddress)),
					this.Processor.PullWord(CivilopediaPageHistorySegment, (ushort)(line + CivilopediaChosenCategoryTableAddress)));
			}
			else
			{
				chosen = (ushort)(column != 0 ? 0xfffe : 0xffff);
			}

			this.Owner.TurnLogic.WaitForKey();

			return chosen;
		}

		private void DrawCivilopediaRecordHeader(ushort dataSegment, ushort scienceIndex, ushort part, short titleHorizontal)
		{
			switch (part)
			{
				case 0:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIndex = ObtainFirstPrerequisite(scienceIndex);
						this.Owner.Words.Set("iconpg1.pic");
						byte pageDigit = (byte)CentralProcessor.SignedQuotient((short)iconIndex, 9);
						this.Owner.Words[6] = (byte)(this.Owner.Words[6] + pageDigit);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaSciencePaletteAddress);
						DrawIcon(
							(ushort)(((iconIndex % 3) * 111) + 1),
							(ushort)((((iconIndex % 9) / 3) * 69) + 1),
							110, 68, 62, 42);
					}

					this.Owner.Words.Set(GameTables.TechnologyText((short)scienceIndex));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(CivilopediaScienceCategoryHeaderAddress, titleHorizontal, 36, Hues.LightGray);
					break;

				case 1:
					if (this.Owner.Startup.AskForDisk(1, 0) == 0)
					{
						Turn.CivilopediaWithoutPictures = 0x1;
					}

					if (scienceIndex == 100)
					{
						this.Owner.Words.Set("Wealth");
						this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(
							CivilopediaBuildingCategoryHeaderAddress,
							titleHorizontal, 36, Hues.LightGray);
						break;
					}

					if (scienceIndex == 101)
					{
						this.Owner.Words.Set("Research");
						this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(
							CivilopediaBuildingCategoryHeaderAddress,
							titleHorizontal, 36, Hues.LightGray);
						break;
					}

					if (scienceIndex == 102)
					{
						this.Owner.Words.Set("Food");
						this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(
							CivilopediaBuildingCategoryHeaderAddress,
							titleHorizontal, 36, Hues.LightGray);
						break;
					}

					if ((short)scienceIndex > 1 && (short)scienceIndex <= 0x15 && Turn.CivilopediaWithoutPictures == 0)
					{
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, CivilopediaBuildingsPictureAddress, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaBuildingPaletteAddress);
						ushort iconSlot = (ushort)(scienceIndex - 2);
						ushort pictureHandle = this.Owner.VideoCard.ScreenToPicture(
							1,
							(ushort)(((iconSlot >> 2) * 0x32) + 1),
							(ushort)(((iconSlot & 3) * 0x32) + 1),
							0x31, 0x31);
						this.Owner.Services.DrawPictureToScreen(Screen.Box, 37, 17, pictureHandle);
						this.Owner.Helpers.ReleaseResource(pictureHandle, 0);
					}

					this.Owner.Words.Set(GameTables.BuildingRecordText((short)scienceIndex));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(
						(short)scienceIndex > 0x18 ? CivilopediaWonderCategoryHeaderAddress : CivilopediaBuildingCategoryHeaderAddress,
						titleHorizontal, 36, Hues.LightGray);
					break;

				case 2:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIndex = ObtainSecondPrerequisite(scienceIndex);
						this.Owner.Words.Set("iconpga.pic");
						byte pageCharacter = (byte)CentralProcessor.SignedQuotient((short)iconIndex, 6);
						this.Owner.Words[6] = (byte)(this.Owner.Words[6] + pageCharacter);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.Owner.Helpers.PullAndAssignColors(CivilopediaUnitPaletteAddress);
						DrawIcon(
							(ushort)(((iconIndex & 1) * 160) + 1),
							(ushort)(((iconIndex % 6) >> 1) * 61),
							158, 61, 88, 42);
					}

					this.Owner.Words.Set(GameTables.UnitText((short)scienceIndex));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(CivilopediaUnitsCategoryHeaderAddress, titleHorizontal, 36, Hues.LightGray);
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						216, 48,
						this.Processor.PullWord(dataSegment, (ushort)((scienceIndex << 1) + CivilopediaUnitIconsTableAddress)));
					break;

				case 3:
					if (Turn.CivilopediaWithoutPictures == 0)
					{
						ushort iconIndex = ObtainTechnologyCategory(scienceIndex);
						this.Owner.Words.Set("iconpgt1.pic");
						byte pageDigit = (byte)CentralProcessor.SignedQuotient((short)iconIndex, 6);
						this.Owner.Words[7] = (byte)(this.Owner.Words[7] + pageDigit);
						this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						if (Screen.VideoGraphicsMode != 0)
						{
							this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, CivilopediaTerrainPaletteAddress, TerrainPaletteBufferAddress);
							this.Owner.VideoCard.AssignHuesFromHueStructure(TerrainPaletteBufferAddress);
							this.Owner.VideoCard.AssignHue(0xfd, VideoGraphicsBitmap.HueFromDigitalLevels(0x3d, 0x3d, 0x3d));
						}

						DrawIcon(
							(ushort)((((short)iconIndex % 3) * 107) + 1),
							(ushort)(((((short)iconIndex % 6) / 3) * 87) + 5),
							106, 77, 76, 46);
					}

					EmptyMessage()
						.Text(GameTables.TerrainText(scienceIndex));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(CivilopediaTerrainCategoryHeaderAddress, titleHorizontal, 36, Hues.LightGray);
					break;

				case 4:
					this.Owner.Words.Append(this.Processor.PullWord(dataSegment, (ushort)((scienceIndex << 1) + CivilopediaConceptTitlesTableAddress)));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(CivilopediaConceptsCategoryHeaderAddress, titleHorizontal, 36, Hues.LightGray);
					break;
			}
		}

		private void DrawTechnologyCivilopediaDetails(ushort dataSegment, ushort scienceIndex, short titleHorizontal, ushort backgroundColor, short cursorVertical)
		{
			cursorVertical = (short)(cursorVertical + 8);
			this.Owner.Words.Clear();

			ushort technologySpecificationAddress = (ushort)(0x16 * scienceIndex);
			byte firstRequirement = this.Processor.PullByte(dataSegment, (ushort)(technologySpecificationAddress + TechnologyTable.PrerequisiteFirstAddress));
			if (firstRequirement != 0xff)
			{
				Message("Requires ")
					.Text(GameTables.TechnologyText((short)(sbyte)firstRequirement));
			}

			byte secondRequirement = this.Processor.PullByte(dataSegment, (ushort)(technologySpecificationAddress + TechnologyTable.PrerequisiteSecondAddress));
			if (secondRequirement != 0xff)
			{
				this.Owner.Words.Append(" and ");
				this.Owner.Words.Append(GameTables.TechnologyText((short)(sbyte)secondRequirement));
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 32, cursorVertical, Hues.Blue);
			cursorVertical = (short)(cursorVertical + 0x10);
			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextAllows, 32, cursorVertical, Hues.Blue);
			cursorVertical = (short)(cursorVertical + 8);
			short listStartVertical = cursorVertical;

			for (ushort otherTechnology = 0; (short)otherTechnology < 0x48; otherTechnology++)
			{
				ushort otherAddress = (ushort)(0x16 * otherTechnology);
				ushort firstOtherRequirement = (ushort)(short)(sbyte)this.Processor.PullByte(dataSegment, (ushort)(otherAddress + 0x4ee));
				if (firstOtherRequirement == scienceIndex)
				{
					this.Owner.Words.Set((ushort)(otherAddress + 0x4da));
					byte companion = this.Processor.PullByte(dataSegment, (ushort)(otherAddress + 0x4ef));
					if (companion != 0xff)
					{
						this.Owner.Words.Append(" (with ");
						this.Owner.Words.Append(GameTables.TechnologyText((short)(sbyte)companion));
						this.Owner.Words.Append(")");
					}

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 40, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}

				ushort secondOtherRequirement = (ushort)(short)(sbyte)this.Processor.PullByte(dataSegment, (ushort)(otherAddress + 0x4ef));
				if (secondOtherRequirement == scienceIndex)
				{
					this.Owner.Words.Set((ushort)(otherAddress + 0x4da));
					byte companion = this.Processor.PullByte(dataSegment, (ushort)(otherAddress + 0x4ee));
					if (companion != 0xff)
					{
						this.Owner.Words.Append(" (with ");
						this.Owner.Words.Append(GameTables.TechnologyText((short)(sbyte)companion));
						this.Owner.Words.Append(")");
					}

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 40, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}
			}

			cursorVertical = (short)(cursorVertical + 4);
			for (ushort unitIndex = 0; (short)unitIndex < 0x1c; unitIndex++)
			{
				ushort unitSpecification = (ushort)(0x22 * unitIndex);
				if (Tables.UnitKindWordAt(unitSpecification, UnitKindTable.Technology) == scienceIndex)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						40,
						cursorVertical - 4,
						this.Processor.PullWord(
							dataSegment,
							(ushort)((((unitIndex + (this.Owner.World.PersonParticipantIndex << 5)) + 0x40) << 1) + 0xd4ce)));

					Message(GameTables.UnitTextAt(unitSpecification))
						.Text(" unit");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 60, cursorVertical, Hues.LightRed);
					cursorVertical = (short)(cursorVertical + 0xc);
				}
			}

			for (ushort improvementIndex = 1; (short)improvementIndex <= 0x2d; improvementIndex++)
			{
				ushort improvementSpecification = (ushort)(0x1e * improvementIndex);
				if (Tables.BuildingWordAt(improvementSpecification, BuildingTable.Technology) == scienceIndex)
				{
					this.Owner.CityOperations.DrawResourceIcon((short)improvementIndex, 40, (short)(cursorVertical - 2));
					Message(GameTables.BuildingRecordTextAt(improvementSpecification))
						.Text((short)improvementIndex > 0x18 ? " Wonder" : " improvement");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 60, cursorVertical, Hues.Green);
					cursorVertical = (short)(cursorVertical + 0xc);
				}
			}

			ushort bankingTechnology = Tables.BuildingWord(10, BuildingTable.Technology);
			if (scienceIndex == bankingTechnology)
			{
				this.Owner.CityOperations.DrawResourceIcon(10, 40, (short)(cursorVertical - 2));
				Message("Wealth (converts 25% shields to gold)");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 60, cursorVertical, Hues.Green);
				cursorVertical = (short)(cursorVertical + 0xc);
			}

			ushort universityTechnology = Tables.BuildingWord(12, BuildingTable.Technology);
			if (scienceIndex == universityTechnology)
			{
				this.Owner.CityOperations.DrawResourceIcon(12, 40, (short)(cursorVertical - 2));
				Message("Research (converts 25% shields to science)");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 60, cursorVertical, Hues.Green);
				cursorVertical = (short)(cursorVertical + 0xc);
			}

			ushort engineeringTechnology = SpecialProjects.EngineeringTechnology;
			if (scienceIndex == engineeringTechnology)
			{
				Message("Food (converts 25% shields to food)");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 60, cursorVertical, Hues.Green);
				cursorVertical = (short)(cursorVertical + 0xc);
			}

			if ((short)(listStartVertical + 4) == cursorVertical)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextAllowsSecond, 32, cursorVertical - 12, backgroundColor);
			}

			ushort obtainedFrom = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ScienceObtainedSince[scienceIndex];
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
					.Text(Tables.CivilizationPlural(obtainedFrom))
					.Text(")");
			}

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), titleHorizontal, 48, Hues.LightGray);
		}

		private void DrawImprovementCivilopediaDetails(ushort scienceIndex, short cursorVertical)
		{
			if (scienceIndex == 100)
			{
				ushort bankingTechnology = Tables.BuildingWord(10, BuildingTable.Technology);
				this.Owner.Words.Set("Requires ");
				this.Owner.Words.Append(GameTables.TechnologyText((short)bankingTechnology));
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
				cursorVertical = (short)(cursorVertical + 8);

				this.Owner.Words.Set("Cost: Continuous project (no end).");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
				cursorVertical = (short)(cursorVertical + 8);

				this.Owner.Words.Set("Effect: Converts 50% net shields to Gold each turn.");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.Green);
				return;
			}

			if (scienceIndex == 101)
			{
				ushort universityTechnology = Tables.BuildingWord(12, BuildingTable.Technology);
				this.Owner.Words.Set("Requires ");
				this.Owner.Words.Append(GameTables.TechnologyText((short)universityTechnology));
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
				cursorVertical = (short)(cursorVertical + 8);

				this.Owner.Words.Set("Cost: Continuous project (no end).");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
				cursorVertical = (short)(cursorVertical + 8);

				this.Owner.Words.Set("Effect: Converts 50% net shields to Science each turn.");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.Green);
				return;
			}

			ushort improvementSpecification = (ushort)(0x1e * scienceIndex);
			this.Owner.Words.Set("Requires ");
			ushort requiredTechnology = Tables.BuildingWordAt(improvementSpecification, BuildingTable.Technology);
			this.Owner.Words.Append(GameTables.TechnologyText((short)requiredTechnology));
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Cost: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)(0xa * (short)Tables.BuildingWordAt(improvementSpecification, BuildingTable.Price)), 10));
			this.Owner.Words.Append(" shields.");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Maintenance: $");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)Tables.BuildingWordAt(improvementSpecification, BuildingTable.Upkeep), 10));
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightRed);

			if (scienceIndex == 10)
			{
				cursorVertical = (short)(cursorVertical + 8);
				this.Owner.Words.Set("Allows city Wealth (25% shields -> gold).");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.Green);
			}
			else if (scienceIndex == 12)
			{
				cursorVertical = (short)(cursorVertical + 8);
				this.Owner.Words.Set("Allows city Research (25% shields -> science).");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.Green);
			}
		}

		private void DrawUnitCivilopediaDetails(ushort scienceIndex, short cursorVertical)
		{
			ushort unitSpecification = (ushort)(0x22 * scienceIndex);
			this.Owner.Words.Set("Requires ");
			ushort requiredTechnology = Tables.UnitKindWordAt(unitSpecification, UnitKindTable.Technology);
			this.Owner.Words.Append(GameTables.TechnologyText((short)requiredTechnology));
			if (scienceIndex == 0x19)
			{
				this.Owner.Words.Append(" and Nuclear Fission");
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Cost: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)(0xa * (short)Tables.UnitKindWordAt(unitSpecification, UnitKindTable.Price)), 10));
			this.Owner.Words.Append(" resources.");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Attack Strength: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWordAt(unitSpecification, UnitKindTable.AssaultPower), 10));
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.LightRed);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Defense Strength: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWordAt(unitSpecification, UnitKindTable.GuardPower), 10));
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.LightRed);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Movement Rate: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWordAt(unitSpecification, UnitKindTable.ShiftTally), 10));
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.Black);

			int successor = UnitUpgradeRules.Successor(scienceIndex);

			if (successor != UnitUpgradeRules.NoSuccessor)
			{
				int cost = UnitUpgradeRules.Cost((short)Tables.UnitKindWord(scienceIndex, UnitKindTable.Price), (short)Tables.UnitKindWord(successor, UnitKindTable.Price));

				cursorVertical = (short)(cursorVertical + 12);
				this.Owner.Words.Set("Can be upgraded to ");
				this.Owner.Words.Append(GameTables.UnitText(successor));
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 100, cursorVertical, Hues.LightBlue);
				cursorVertical = (short)(cursorVertical + 8);

				this.Owner.TextPainter.DrawTextToPrimaryScreen($"for ${cost} in a city.", 100, cursorVertical, Hues.LightBlue);
			}
		}

		private void DrawTerrainCivilopediaDetails(ushort dataSegment, ushort scienceIndex, short cursorVertical)
		{
			for (ushort variantIndex = scienceIndex; (short)(scienceIndex + 0xc) >= (short)variantIndex; variantIndex = (ushort)(variantIndex + 0xc))
			{
				if (variantIndex != scienceIndex &&
					Tables.TerrainField(scienceIndex, TerrainTable.Food) == 2)
				{
					continue;
				}

				ushort variableSpecification = (ushort)(0x13 * variantIndex);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTables.TerrainTextAt(variableSpecification), 12, cursorVertical, Hues.Blue);
				cursorVertical = (short)(cursorVertical + 8);

				sbyte foodValue = (sbyte)Tables.TerrainFieldAt(variableSpecification, TerrainTable.Food);
				short irrigationEffect = (short)this.Processor.PullWord(dataSegment, (ushort)(TerrainImprovementTable.Origin + (0xc * scienceIndex) + TerrainImprovementTable.Irrigation));
				if (foodValue != 0 || irrigationEffect < -1)
				{
					Message("Food: ")
						.Number(foodValue);
					if (foodValue >= 3)
					{
						this.Owner.Words.Append("*");
					}

					if (irrigationEffect < -1)
					{
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(foodValue - irrigationEffect - 1), 10));
						if (foodValue >= 2)
						{
							this.Owner.Words.Append("*");
						}

						this.Owner.Words.Append(" with Irrigation)");
					}

					this.Owner.Words.Append(" units.");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}

				sbyte productionValue = (sbyte)Tables.TerrainFieldAt(variableSpecification, TerrainTable.Shields);
				short mineEffect = (short)this.Processor.PullWord(dataSegment, (ushort)(TerrainImprovementTable.Origin + (0xc * scienceIndex) + TerrainImprovementTable.Mining));
				if (productionValue != 0 || mineEffect < -1)
				{
					this.Owner.Words.Set("Production: ");
					if (scienceIndex == 0x2 || scienceIndex == 0xb)
					{
						this.Owner.Words.Append("0/");
					}

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(productionValue, 10));
					if (mineEffect < -1)
					{
						short minedProduction = (short)(productionValue - mineEffect - 1);
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(minedProduction, 10));
						if (minedProduction >= 3)
						{
							this.Owner.Words.Append("*");
						}

						this.Owner.Words.Append(" with Mining)");
					}

					this.Owner.Words.Append(" units.");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}

				sbyte tradeValue = (sbyte)Tables.TerrainFieldAt(variableSpecification, TerrainTable.Trade);
				if (tradeValue != 0 || (short)scienceIndex <= 2)
				{
					Message("Trade: ")
						.Number(tradeValue);
					if (tradeValue != 0)
					{
						this.Owner.Words.Append("%");
					}

					if (tradeValue >= 3)
					{
						this.Owner.Words.Append("*");
					}

					if ((short)scienceIndex <= 2)
					{
						this.Owner.Words.Append(" (");
						this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(tradeValue + 1), 10));
						this.Owner.Words.Append("% with Roads)");
					}

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}

				if (variantIndex == 0x7)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextNothing, 16, cursorVertical, Hues.LightBlue);
					cursorVertical = (short)(cursorVertical + 8);
				}

				cursorVertical = (short)(cursorVertical + 4);
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TopicFirstIfGovernmentIsDespotismAnarchy, 16, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 8);
			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextFirstIfGovernmentIsRepublicDemocracy, 16, cursorVertical, Hues.LightBlue);
			cursorVertical = (short)(cursorVertical + 0xc);

			ushort baseSpecification = (ushort)(0x13 * scienceIndex);
			this.Owner.Words.Set("Movement cost: ");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText((sbyte)Tables.TerrainFieldAt(baseSpecification, TerrainTable.MoveCost), 10));
			this.Owner.Words.Append(" MP");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightRed);
			cursorVertical = (short)(cursorVertical + 8);

			this.Owner.Words.Set("Defense bonus: +");
			this.Owner.Words.Append(
				this.Owner.Runtime.IntegerToText(
					(short)((0x32 * (short)(sbyte)Tables.TerrainFieldAt(baseSpecification, TerrainTable.DefenseBonus)) - 0x64),
					10));
			this.Owner.Words.Append("%");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 12, cursorVertical, Hues.LightRed);
		}

		private ushort FindInScienceList(ushort origin, int tally, ushort scienceIndex)
		{
			ushort found = 0xffff;

			for (int index = 0; index < tally; index++)
			{
				this.Processor.SegmentExtra.Word = 0x371e;

				if ((ushort)(sbyte)this.Processor.PullByte(this.Processor.SegmentExtra.Word, (ushort)(origin + index)) == scienceIndex)
				{
					found = (ushort)index;
					break;
				}
			}

			return found;
		}
	}
}
