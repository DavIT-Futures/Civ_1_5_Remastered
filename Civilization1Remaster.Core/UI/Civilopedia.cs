using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class Civilopedia : GameModule
	{
		public Civilopedia(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The Civilopedia, at the part chosen in its menu: shows pages of the index until one answers 0xfffe (leave);
		/// 0xffff turns to the next page (78 entries further) when there is one (0x6808), else back to the first
		/// </summary>
		public void DisplayCivilopedia(ushort part)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopedia({part})");

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			ushort firstEntry = 0;
			ushort answer;

			do
			{
				answer = DisplayCivilopediaIndex(firstEntry, part);

				if (answer == 0xffff)
				{
					firstEntry = CivilopediaNextPage != 0 ? (ushort)(firstEntry + 0x4e) : (ushort)0;
				}
			}
			while (answer != 0xfffe);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Helpers.LaunchHueCycling();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayCivilopedia");
		}

		/// <summary>
		/// One page of the Civilopedia index: the entries of the part (0 technologies, 1 improvements and wonders, 2 units,
		/// 3 terrains, 4 concepts, -1 all of them) in alphabetical order, from entry firstEntry on, in columns of 26 (wider for
		/// one part) until the page is full (0x6808 = 1: there is a next page); the entry kind and number of each line go to
		/// 0x3772:0x36fc and 0x1c80. The entry is chosen with the mouse or the arrows (Enter, space); its record is shown and
		/// a key awaited.
		/// </summary>
		/// <returns>0 for no entry under the choice; 0xffff for the next page ("m", a click on the top right); 0xfffe to
		/// leave (Esc, "x", a click on the top left); else the line number; also in AX</returns>
		public ushort DisplayCivilopediaIndex(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopediaIndex({arg1}, {arg2})");

			ushort scratchBase = (ushort)(this.processor.RegStack.Value16 - 0x300);
			ushort lastName = (ushort)(scratchBase + 0x10);
			ushort nextName = (ushort)(scratchBase + 0x30);
			ushort shortName = (ushort)(scratchBase + 0x50);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 14);

			this.owner.Libc.CopyText(lastName, "a");

			this.owner.Services.FloodBox(ScreenBox, 60, 2, 200, 9, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3c90, 161, 4, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3cae, 160, 3, 10);

			this.owner.TextPainter.RenderTextToScreen0(0x3ccc, 286, 4, 12);

			if (arg2 == 0xffff)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x3cd1, 8, 4, 12);
			}

			this.owner.Services.FloodBox(ScreenBox, 2, 14, 316, 184, 15);

			short width = (short)((short)arg2 > 0 ? 0x96 : 0x64);
			ushort entryTally = ListCivilopediaEntries(arg1, arg2, width, lastName, nextName, shortName);

			ushort answer = (ushort)(MousePresent != 0
				? ChooseCivilopediaEntryWithMouse(arg1, width, entryTally)
				: ChooseCivilopediaEntryWithKeys(arg1, width, entryTally));

			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("DisplayCivilopediaIndex");

			return answer;
		}

		/// <summary>
		/// Lists the entries after "a" in alphabetical order (each round the smallest name after the last one), the entries
		/// from firstEntry on in columns of the width from (10, 16), 7 a line, until column 300; 0x6808 = 1 when the page is
		/// full. The number of entries passed (including those before firstEntry).
		/// </summary>
		private ushort ListCivilopediaEntries(ushort firstEntry, ushort part, short width, ushort lastName, ushort nextName, ushort shortName)
		{
			short horizLoc = 0xa;
			short vertLoc = 0x10;
			ushort entryTally = 0;

			while (true)
			{
				this.owner.Libc.CopyText(nextName, "zzz");

				ushort nextIdx = 0xffff;
				ushort nextKind = 0xffff;

				// Technologies, improvements and wonders (1 - 0x2d), units, terrains, concepts (names through 0x3c64)
				FindNextCivilopediaEntry(part, 0, 0, 0x43, idx => TechnologyText(idx), lastName, nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 1, 1, 0x2e, idx => BuildingRecordText(idx), lastName, nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 2, 0, 0x1c, idx => UnitText(idx), lastName, nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 3, 0, 0xc, idx => TerrainText(idx), lastName, nextName, ref nextIdx, ref nextKind);
				FindNextCivilopediaEntry(part, 4, 0, 0x15, idx => this.processor.PullGlobalWord((ushort)((idx << 1) + 0x3c64)), lastName, nextName, ref nextIdx, ref nextKind);

				if (nextKind == 0xffff)
					break;

				if ((short)entryTally >= (short)firstEntry)
				{
					this.owner.Libc.CopyText(shortName, nextName);

					this.owner.BlockText.ShrinkTextToWidth(shortName, (ushort)width);

					this.owner.TextPainter.RenderTextToScreen0(shortName, horizLoc, vertLoc, 0);

					ushort line = (ushort)((entryTally - firstEntry) << 1);

					this.processor.EmitUInt16(0x3772, (ushort)(line + 0x1c80), nextIdx);
					this.processor.EmitUInt16(0x3772, (ushort)(line + 0x36fc), nextKind);

					vertLoc += 7;

					if (vertLoc > 0xc0)
					{
						horizLoc += width;
						vertLoc = 0x10;
					}
				}

				entryTally++;

				this.owner.Libc.CopyText(lastName, nextName);

				if (horizLoc >= 0x12c)
					break;
			}

			CivilopediaNextPage = (ushort)(horizLoc >= 0x12c ? 1 : 0);

			return entryTally;
		}

		/// <summary>
		/// For the part (or all parts, -1): the entry first - end - 1 whose name comes after the last listed name and before the
		/// next found so far becomes the next one
		/// </summary>
		private void FindNextCivilopediaEntry(ushort part, ushort kind, short first, short end, System.Func<short, ushort> nameOf,
			ushort lastName, ushort nextName, ref ushort nextIdx, ref ushort nextKind)
		{
			if (part != kind && part != 0xffff)
				return;

			for (short idx = first; idx < end; idx++)
			{
				if (this.owner.Libc.CompareTextNoCase(nameOf(idx), lastName) <= 0)
					continue;

				if (this.owner.Libc.CompareTextNoCase(nameOf(idx), nextName) >= 0)
					continue;

				nextIdx = (ushort)idx;
				nextKind = kind;

				this.owner.Libc.CopyText(nextName, nameOf(idx));
			}
		}

		/// <summary>
		/// The mouse: waits for a click or a key; the line under the click (above the list: row -1, column 2 for a right click
		/// or a key, which is taken), then ShowCivilopediaChoice
		/// </summary>
		private ushort ChooseCivilopediaEntryWithMouse(ushort firstEntry, short width, ushort entryTally)
		{
			this.owner.Startup.DisplayMouseCursor();

			do
			{
				this.owner.Startup.ObtainMouseState();

				if (this.owner.GlobDb3a != 0)
					break;
			}
			while (this.owner.Libc.KeyAvailable() == 0);

			short column = CPU.SignedQuotient((short)(this.owner.GlobDb3c - 0xa), width);
			short row = (short)this.owner.GlobDb3e < 0x10 ? (short)-1 : CPU.SignedQuotient((short)(this.owner.GlobDb3e - 0x10), 7);
			ushort chosen = (ushort)((0x1a * column) + row);

			this.owner.Startup.ConcealMouseCursor();

			if (this.owner.GlobDb3a == 2)
			{
				row = -1;
				column = 2;
			}

			if (this.owner.Libc.KeyAvailable() != 0)
			{
				this.owner.Dialogs.ObtainNavigationKey();

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

				this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, width - 2, 8, 15, 10);

				ushort key = this.owner.Dialogs.ObtainNavigationKey();

				this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, width - 2, 8, 10, 15);

				switch (key)
				{
					case 0x4800:
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

					case 0x4b00:
						if (column != 0)
						{
							column--;
						}

						break;

					case 0x4d00:
						if ((short)((0x1a * column) + row + 0x1a) < shown)
						{
							column++;
						}

						break;

					case 0x5000:
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

				if (key == 0x6d)
					return 0xffff;

				if (key == 0x1b || key == 0x78)
					return 0xfffe;

				chosen = (ushort)((0x1a * column) + row);

				if (key == 0xd || key == 0x20)
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
				if (DiskInUse != 3)
				{
					this.owner.Startup.QueryForDisk(3, 1);
				}

				ushort line = (ushort)(chosen << 1);

				DisplayCivilopediaRecord(this.processor.PullUInt16(0x3772, (ushort)(line + 0x1c80)),
					this.processor.PullUInt16(0x3772, (ushort)(line + 0x36fc)));
			}
			else
			{
				chosen = (ushort)(column != 0 ? 0xfffe : 0xffff);
			}

			this.owner.TurnLogic.AwaitForKey();

			return chosen;
		}

/// <summary>
		/// ?
		/// </summary>
		/// <param name="technologyID"></param>
		/// <param name="param2"></param>
		public void DisplayCivilopediaRecord(ushort scienceIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopediaRecord({scienceIdx}, {arg2})");

			ushort dataSeg = this.processor.SegData.Value16;
			this.processor.EmitGlobalByte(0x3fa9, (byte)((byte)arg2 + 0x30));

			this.owner.Helpers.HaltHueCycling();
			this.owner.Startup.ConcealMouseCursor();

			ushort diskStatus = 0xffff;
			if (DiskInUse != 0x3)
			{
				diskStatus = this.owner.Startup.QueryForDisk(3, 0);
				if (diskStatus == 0)
				{
					CivilopediaWithoutPictures = 0x1;
				}
			}

			ushort bgColor = 0xf;
			if (VgaMode != 0)
			{
				if (CivilopediaWithoutPictures == 0)
				{
					bgColor = 0xfd;
					this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));
				}
				else
				{
					bgColor = 0xff;
				}
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, bgColor);
			this.owner.UnitOps.RenderBusyIndicator();

			DrawingBox.TypefaceIdx = 0x7;
			this.owner.Words.Clear();

			short titleX = arg2 == 0x2 ? (short)0xe0 : (short)0xcc;
			if (arg2 == 0x4 ||
				(arg2 == 0x1 && (short)scienceIdx > 0x18) ||
				CivilopediaWithoutPictures != 0)
			{
				titleX = 0xa0;
			}

			this.RenderCivilopediaRecordHeader(dataSeg, scienceIdx, arg2, titleX);

			DrawingBox.TypefaceIdx = 0x6;
			this.owner.Libc.UppercaseText(TextBuffer.Address);
			this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, titleX, 20, 0);
			DrawingBox.TypefaceIdx = 0x7;

			ushort tagBuf = (ushort)(this.processor.RegStack.Value16 - 0x300);
			this.owner.Libc.CopyText(tagBuf, "*");
			this.owner.Libc.AppendText(tagBuf, TextBuffer.Address);
			this.owner.Words.Clear();

			short cursorY = 0x4c;
			if (diskStatus != 0)
			{
				if (((byte)(this.owner.World.SessionPrefMarkers & 0xff) & 0x40) != 0)
				{
					ushort imported = this.owner.BlockText.ImportTextFromFile(0x3fa4, tagBuf);
					if ((ushort)(imported + 1) != 0)
					{
						this.owner.BlockText.RenderTextChunk(37, 12, 76, 1);
						this.owner.Conquest.AwaitForKeyPressOrMouseClick();
						this.owner.Services.FloodBox(ScreenBox, 8, 76, 304, 116, bgColor);
					}
				}

				this.owner.Libc.AppendText(tagBuf, "2");
				this.owner.Words.Clear();
				if (arg2 != 0)
				{
					this.owner.BlockText.ImportTextFromFile(0x3fa4, tagBuf);
					cursorY = (short)((ushort)this.owner.BlockText.RenderTextChunk(36, 12, cursorY, 1) + 8);
				}
			}

			switch (arg2)
			{
				case 0:
					this.RenderTechnologyCivilopediaDetails(dataSeg, scienceIdx, titleX, bgColor, cursorY);
					break;
				case 1:
					this.RenderImprovementCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
				case 2:
					this.RenderUnitCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
				case 3:
					this.RenderTerrainCivilopediaDetails(dataSeg, scienceIdx, cursorY);
					break;
			}

			DrawingBox.TypefaceIdx = 0x1;
			this.owner.Startup.DisplayMouseCursor();
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();
			this.owner.Startup.ConcealMouseCursor();

			if ((short)diskStatus == 1)
			{
				this.owner.Startup.QueryForDisk(0, 1);
			}

			CivilopediaWithoutPictures = 0x0;
			if (KingAnswer == 0xffff)
			{
				this.owner.Services.FloodBox(ScreenBox, 8, 8, 304, 184, bgColor);
				this.owner.Helpers.LaunchHueCycling();
			}

			this.owner.Startup.DisplayMouseCursor();
			this.processor.Journal.CloseScope("DisplayCivilopediaRecord");
		}

		private void RenderCivilopediaRecordHeader(ushort dataSeg, ushort scienceIdx, ushort arg2, short titleX)
		{
			switch (arg2)
			{
				case 0:
					if (CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainFirstPrerequisite(scienceIdx);
						this.owner.Words.Set("iconpg1.pic");
						byte pageDigit = (byte)CPU.SignedQuotient((short)iconIdx, 9);
						this.owner.Words[6] = (byte)(this.owner.Words[6] + pageDigit);
						this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.owner.Helpers.PullAndAssignColors(0x3ce6);
						RenderIcon(
							(ushort)(((iconIdx % 3) * 111) + 1),
							(ushort)((((iconIdx % 9) / 3) * 69) + 1),
							110, 68, 62, 42);
					}

					this.owner.Words.Set(TechnologyText((short)scienceIdx));
					this.owner.TextPainter.RenderCenteredTextToScreen0(0x3cf2, titleX, 36, 7);
					break;

				case 1:
					if (this.owner.Startup.QueryForDisk(1, 0) == 0)
					{
						CivilopediaWithoutPictures = 0x1;
					}

					if ((short)scienceIdx > 1 && (short)scienceIdx <= 0x15 && CivilopediaWithoutPictures == 0)
					{
						this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3d07, 0);
						this.owner.Helpers.PullAndAssignColors(0x3d14);
						ushort iconSlot = (ushort)(scienceIdx - 2);
						ushort picHandle = this.owner.VideoCard.ScreenToPicture(
							1,
							(ushort)(((iconSlot >> 2) * 0x32) + 1),
							(ushort)(((iconSlot & 3) * 0x32) + 1),
							0x31, 0x31);
						this.owner.Services.RenderPictureToScreen(ScreenBox, 37, 17, picHandle);
						this.owner.Helpers.ReleaseResource(picHandle, 0);
					}

					this.owner.Words.Set(BuildingRecordText((short)scienceIdx));
					this.owner.TextPainter.RenderCenteredTextToScreen0(
						(short)scienceIdx > 0x18 ? (ushort)0x3d2e : (ushort)0x3d1d,
						titleX, 36, 7);
					break;

				case 2:
					if (CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainSecondPrerequisite(scienceIdx);
						this.owner.Words.Set("iconpga.pic");
						byte pageChar = (byte)CPU.SignedQuotient((short)iconIdx, 6);
						this.owner.Words[6] = (byte)(this.owner.Words[6] + pageChar);
						this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						this.owner.Helpers.PullAndAssignColors(0x3d4e);
						RenderIcon(
							(ushort)(((iconIdx & 1) * 160) + 1),
							(ushort)(((iconIdx % 6) >> 1) * 61),
							158, 61, 88, 42);
					}

					this.owner.Words.Set(UnitText((short)scienceIdx));
					this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d5a, titleX, 36, 7);
					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						216, 48,
						this.processor.PullUInt16(dataSeg, (ushort)((scienceIdx << 1) + 0xd58e)));
					break;

				case 3:
					if (CivilopediaWithoutPictures == 0)
					{
						ushort iconIdx = ObtainTechnologyCategory(scienceIdx);
						this.owner.Words.Set("iconpgt1.pic");
						byte pageDigit = (byte)CPU.SignedQuotient((short)iconIdx, 6);
						this.owner.Words[7] = (byte)(this.owner.Words[7] + pageDigit);
						this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, TextBuffer.Address, 0);
						if (VgaMode != 0)
						{
							this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x3d75, 0xc5be);
							this.owner.VideoCard.AssignHuesFromHueStruct(0xc5be);
							this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));
						}

						RenderIcon(
							(ushort)((((short)iconIdx % 3) * 107) + 1),
							(ushort)(((((short)iconIdx % 6) / 3) * 87) + 5),
							106, 77, 76, 46);
					}

					EmptyMessage()
						.Text(TerrainText(scienceIdx));
					this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d7f, titleX, 36, 7);
					break;

				case 4:
					this.owner.Words.Append(this.processor.PullUInt16(dataSeg, (ushort)((scienceIdx << 1) + 0x3c64)));
					this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d8c, titleX, 36, 7);
					break;
			}
		}

		private void RenderTechnologyCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short titleX, ushort bgColor, short cursorY)
		{
			cursorY = (short)(cursorY + 8);
			this.owner.Words.Clear();

			ushort techSpecAddr = (ushort)(0x16 * scienceIdx);
			byte req1 = this.processor.PullUInt8(dataSeg, (ushort)(techSpecAddr + 0x4ee));
			if (req1 != 0xff)
			{
				Message("Requires ")
					.Text(TechnologyText((short)(sbyte)req1));
			}

			byte req2 = this.processor.PullUInt8(dataSeg, (ushort)(techSpecAddr + 0x4ef));
			if (req2 != 0xff)
			{
				this.owner.Words.Append(" and ");
				this.owner.Words.Append(TechnologyText((short)(sbyte)req2));
			}

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 32, cursorY, 1);
			cursorY = (short)(cursorY + 0x10);
			this.owner.TextPainter.RenderTextToScreen0(0x3dad, 32, cursorY, 1);
			cursorY = (short)(cursorY + 8);
			short listStartY = cursorY;

			for (ushort otherTech = 0; (short)otherTech < 0x48; otherTech++)
			{
				ushort otherAddr = (ushort)(0x16 * otherTech);
				ushort otherReq1 = (ushort)(short)(sbyte)this.processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ee));
				if (otherReq1 == scienceIdx)
				{
					this.owner.Words.Set((ushort)(otherAddr + 0x4da));
					byte companion = this.processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ef));
					if (companion != 0xff)
					{
						this.owner.Words.Append(" (with ");
						this.owner.Words.Append(TechnologyText((short)(sbyte)companion));
						this.owner.Words.Append(")");
					}

					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 40, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}

				ushort otherReq2 = (ushort)(short)(sbyte)this.processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ef));
				if (otherReq2 == scienceIdx)
				{
					this.owner.Words.Set((ushort)(otherAddr + 0x4da));
					byte companion = this.processor.PullUInt8(dataSeg, (ushort)(otherAddr + 0x4ee));
					if (companion != 0xff)
					{
						this.owner.Words.Append(" (with ");
						this.owner.Words.Append(TechnologyText((short)(sbyte)companion));
						this.owner.Words.Append(")");
					}

					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 40, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}
			}

			cursorY = (short)(cursorY + 4);
			for (ushort unitIdx = 0; (short)unitIdx < 0x1c; unitIdx++)
			{
				ushort unitSpec = (ushort)(0x22 * unitIdx);
				if (UnitKindWordAt(unitSpec, UnitKindTable.Technology) == scienceIdx)
				{
					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						40,
						cursorY - 4,
						this.processor.PullUInt16(
							dataSeg,
							(ushort)((((unitIdx + (this.owner.World.PersonParticipantIdx << 5)) + 0x40) << 1) + 0xd4ce)));

					Message(UnitTextAt(unitSpec))
						.Text(" unit");
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 60, cursorY, 12);
					cursorY = (short)(cursorY + 0xc);
				}
			}

			for (ushort impIdx = 1; (short)impIdx <= 0x2d; impIdx++)
			{
				ushort impSpec = (ushort)(0x1e * impIdx);
				if (BuildingWordAt(impSpec, BuildingTable.Technology) == scienceIdx)
				{
					this.owner.CityOps.RenderResourceIcon((short)impIdx, 40, (short)(cursorY - 2));
					Message(BuildingRecordTextAt(impSpec))
						.Text((short)impIdx > 0x18 ? " Wonder" : " improvement");
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 60, cursorY, 2);
					cursorY = (short)(cursorY + 0xc);
				}
			}

			if ((short)(listStartY + 4) == cursorY)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x3de5, 32, cursorY - 12, bgColor);
			}

			ushort obtainedFrom = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ScienceObtainedSince[scienceIdx];
			if (obtainedFrom == 0xfffe)
			{
				this.owner.Words.Set(0x3dee);
			}
			else if (obtainedFrom == 0xffff)
			{
				this.owner.Words.Clear();
			}
			else if (obtainedFrom == 0)
			{
				this.owner.Words.Set(0x3e03);
			}
			else
			{
				Message("(Taken from ")
					.Text(CivilizationPlural(obtainedFrom))
					.Text(")");
			}

			this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, titleX, 48, 7);
		}

		private void RenderImprovementCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			ushort impSpec = (ushort)(0x1e * scienceIdx);
			this.owner.Words.Set("Requires ");
			ushort reqTech = BuildingWordAt(impSpec, BuildingTable.Technology);
			this.owner.Words.Append(TechnologyText((short)reqTech));
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 12, cursorY, 9);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Cost: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)(0xa * (short)BuildingWordAt(impSpec, BuildingTable.Price)), 10));
			this.owner.Words.Append(" shields.");
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 12, cursorY, 9);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Maintenance: $");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)BuildingWordAt(impSpec, BuildingTable.Upkeep), 10));
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 12, cursorY, 12);
		}

		private void RenderUnitCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			ushort unitSpec = (ushort)(0x22 * scienceIdx);
			this.owner.Words.Set("Requires ");
			ushort reqTech = UnitKindWordAt(unitSpec, UnitKindTable.Technology);
			this.owner.Words.Append(TechnologyText((short)reqTech));
			if (scienceIdx == 0x19)
			{
				this.owner.Words.Append(" and Nuclear Fission");
			}

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 100, cursorY, 9);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Cost: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)(0xa * (short)UnitKindWordAt(unitSpec, UnitKindTable.Price)), 10));
			this.owner.Words.Append(" resources.");
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 100, cursorY, 9);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Attack Strength: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.AssaultPower), 10));
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 100, cursorY, 12);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Defense Strength: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.GuardPower), 10));
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 100, cursorY, 12);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Movement Rate: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((short)UnitKindWordAt(unitSpec, UnitKindTable.ShiftTally), 10));
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 100, cursorY, 0);
		}

		private void RenderTerrainCivilopediaDetails(ushort dataSeg, ushort scienceIdx, short cursorY)
		{
			for (ushort variantIdx = scienceIdx; (short)(scienceIdx + 0xc) >= (short)variantIdx; variantIdx = (ushort)(variantIdx + 0xc))
			{
				if (variantIdx != scienceIdx &&
					this.processor.PullUInt8(dataSeg, (ushort)(0x13 * scienceIdx + 0x290)) == 0x2)
				{
					continue;
				}

				ushort varSpec = (ushort)(0x13 * variantIdx);
				this.owner.TextPainter.RenderTextToScreen0(TerrainTextAt(varSpec), 12, cursorY, 1);
				cursorY = (short)(cursorY + 8);

				sbyte foodVal = (sbyte)this.processor.PullUInt8(dataSeg, (ushort)(varSpec + 0x290));
				short irrigEffect = (short)this.processor.PullUInt16(dataSeg, (ushort)(0xc * scienceIdx + 0x44a));
				if (foodVal != 0 || irrigEffect < -1)
				{
					Message("Food: ")
						.Number(foodVal);
					if (foodVal >= 3)
					{
						this.owner.Words.Append("*");
					}

					if (irrigEffect < -1)
					{
						this.owner.Words.Append(" (");
						this.owner.Words.Append(this.owner.Libc.IntegerToText((short)(foodVal - irrigEffect - 1), 10));
						if (foodVal >= 2)
						{
							this.owner.Words.Append("*");
						}

						this.owner.Words.Append(" with Irrigation)");
					}

					this.owner.Words.Append(" units.");
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 16, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}

				sbyte prodVal = (sbyte)this.processor.PullUInt8(dataSeg, (ushort)(varSpec + 0x291));
				short mineEffect = (short)this.processor.PullUInt16(dataSeg, (ushort)(0xc * scienceIdx + 0x44e));
				if (prodVal != 0 || mineEffect < -1)
				{
					this.owner.Words.Set("Production: ");
					if (scienceIdx == 0x2 || scienceIdx == 0xb)
					{
						this.owner.Words.Append("0/");
					}

					this.owner.Words.Append(this.owner.Libc.IntegerToText(prodVal, 10));
					if (mineEffect < -1)
					{
						short minedProd = (short)(prodVal - mineEffect - 1);
						this.owner.Words.Append(" (");
						this.owner.Words.Append(this.owner.Libc.IntegerToText(minedProd, 10));
						if (minedProd >= 3)
						{
							this.owner.Words.Append("*");
						}

						this.owner.Words.Append(" with Mining)");
					}

					this.owner.Words.Append(" units.");
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 16, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}

				sbyte tradeVal = (sbyte)this.processor.PullUInt8(dataSeg, (ushort)(varSpec + 0x292));
				if (tradeVal != 0 || (short)scienceIdx <= 2)
				{
					Message("Trade: ")
						.Number(tradeVal);
					if (tradeVal != 0)
					{
						this.owner.Words.Append("%");
					}

					if (tradeVal >= 3)
					{
						this.owner.Words.Append("*");
					}

					if ((short)scienceIdx <= 2)
					{
						this.owner.Words.Append(" (");
						this.owner.Words.Append(this.owner.Libc.IntegerToText((short)(tradeVal + 1), 10));
						this.owner.Words.Append("% with Roads)");
					}

					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 16, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}

				if (variantIdx == 0x7)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x3f20, 16, cursorY, 9);
					cursorY = (short)(cursorY + 8);
				}

				cursorY = (short)(cursorY + 4);
			}

			this.owner.TextPainter.RenderTextToScreen0(0x3f28, 16, cursorY, 9);
			cursorY = (short)(cursorY + 8);
			this.owner.TextPainter.RenderTextToScreen0(0x3f52, 16, cursorY, 9);
			cursorY = (short)(cursorY + 0xc);

			ushort baseSpec = (ushort)(0x13 * scienceIdx);
			this.owner.Words.Set("Movement cost: ");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText((sbyte)TerrainFieldAt(baseSpec, TerrainTable.MoveCost), 10));
			this.owner.Words.Append(" MP");
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 12, cursorY, 12);
			cursorY = (short)(cursorY + 8);

			this.owner.Words.Set("Defense bonus: +");
			this.owner.Libc.AppendText(
				TextBuffer.Address,
				this.owner.Libc.IntegerToText(
					(short)((0x32 * (short)(sbyte)this.processor.PullUInt8(dataSeg, (ushort)(baseSpec + 0x28f))) - 0x64),
					10));
			this.owner.Words.Append("%");
			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 12, cursorY, 12);
		}

		/// <summary>
		/// Index of the technology in the list of the technologies that lead to others first (72 bytes at 0x371e:0000)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainFirstPrerequisite(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainFirstPrerequisite({scienceIdx})");

			ushort found = LocateInScienceList(0x0, 0x48, scienceIdx);

			this.processor.Journal.CloseScope("ObtainFirstPrerequisite");

			return found;
		}

		/// <summary>
		/// Copies a picture part (left, top, width, height in the back screen 0x19d4) centred at (centreHoriz, centreVert) on the
		/// screen: the part is cut at the screen edge (319, 199), and narrowed so that it does not start left of the screen
		/// </summary>
		public void RenderIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHoriz, ushort centreVert)
		{
			this.processor.Journal.OpenScope($"RenderIcon({left}, {top}, {breadth}, {height}, {centreHoriz}, {centreVert})");

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

			this.owner.VideoCard.RenderImage(BackScreenBox, left, top, breadth, height,
				ScreenBox, (ushort)(centreHoriz - (breadth >> 1)), (ushort)(centreVert - (height >> 1)));

			this.processor.Journal.CloseScope("RenderIcon");
		}

		/// <summary>
		/// Index of the technology in the second list of the Civilopedia (28 bytes at 0x371e:0044)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainSecondPrerequisite(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainSecondPrerequisite({scienceIdx})");

			ushort found = LocateInScienceList(0x44, 0x1c, scienceIdx);

			this.processor.Journal.CloseScope("ObtainSecondPrerequisite");

			return found;
		}

		/// <summary>
		/// Index of the technology in the list of the technology categories of the Civilopedia (12 bytes at 0x371e:0061)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainTechnologyCategory(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainTechnologyCategory({scienceIdx})");

			ushort found = LocateInScienceList(0x61, 0xc, scienceIdx);

			this.processor.Journal.CloseScope("ObtainTechnologyCategory");

			return found;
		}

		/// <summary>
		/// The first index of the technology in a list of signed bytes in the segment 0x371e. Like the original, ES is left
		/// at the segment and BX at the last index looked at.
		/// </summary>
		/// <returns>Index, or 0xffff when the technology is not in the list; also in AX</returns>
		private ushort LocateInScienceList(ushort origin, int tally, ushort scienceIdx)
		{
			ushort found = 0xffff;

			for (int idx = 0; idx < tally; idx++)
			{
				this.processor.RegBase.Value16 = (ushort)idx;
				this.processor.SegExtra.Value16 = 0x371e;

				if ((ushort)(sbyte)this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(origin + idx)) == scienceIdx)
				{
					found = (ushort)idx;
					break;
				}
			}

			this.processor.RegAcc.Value16 = found;

			return found;
		}
	}
}
