using System;
using System.Collections.Generic;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	public class GameLoadAndSave : GameModule
	{
		public GameLoadAndSave(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Value of the game end flag (0xdc48) that asks the main loop to load the game in LoadSlot
		/// </summary>
		public const ushort FetchSessionAsk = 3;

		/// <summary>
		/// Number of manual saved games, CIVIL0 - CIVIL9
		/// </summary>
		public const int HandBayTally = 10;

		/// <summary>
		/// Number of autosaves, AUTO00 - AUTO19, one for each turn, the oldest is overwritten
		/// </summary>
		public const int BackupBayTally = 20;

		/// <summary>
		/// Added to the autosave number in the slot values returned by the load list
		/// </summary>
		public const ushort BackupBayMarker = 0x100;

		/// <summary>
		/// Saved game chosen with Load Game in the GAME menu, 0xffff when none
		/// </summary>
		public ushort FetchBay = 0xffff;

		// The load list remembers which tab was open
		private bool displayBackups = false;
		/// <summary>An autosave is being loaded (ImportGame continues it like the original autosaves)</summary>
		public bool FetchingBackup { get; set; } = false;

		/// <summary>
		/// Saves the game into the next autosave, called once every turn
		/// </summary>
		public void PerformBackup()
		{
			int bay = this.owner.World.RoundTally % BackupBayTally;

			if (bay < 0)
				bay += BackupBayTally;

			StoreGameContent($"{AutosaveLabel(bay)}.SVE");
		}

		private static string AutosaveLabel(int bay)
		{
			return $"AUTO{bay:00}";
		}

		/// <summary>
		/// Turn of the game saved in an autosave, or -1 when the autosave doesn't exist
		/// </summary>
		private int PullAutosaveTurn(int bay)
		{
			string route = Path.Combine(this.processor.StandardFolder, $"{AutosaveLabel(bay)}.SVE");

			try
			{
				if (File.Exists(route) && File.Exists(Path.ChangeExtension(route, ".MAP")))
				{
					using (FileStream source = new FileStream(route, FileMode.Open, FileAccess.Read))
					{
						int lower = source.ReadByte();
						int upper = source.ReadByte();

						if (lower >= 0 && upper >= 0)
							return (short)(lower | (upper << 8));
					}
				}
			}
			catch
			{ }

			return -1;
		}

		/// <summary>
		/// Sets the save file name (0x41c6, drive letter, colon, file name) to an autosave.
		/// "AUTO00" has the same length as "CIVIL0", so the rest of the name stays in place.
		/// </summary>
		private void AssignAutosaveFileLabel(int bay)
		{
			string label = AutosaveLabel(bay);

			for (int idx = 0; idx < label.Length; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(0x41c8 + idx), (byte)label[idx]);
			}
		}

		/// <summary>
		/// Sets the save file name (0x41c6) back to a manual saved game
		/// </summary>
		private void AssignManualFileLabel(int bay)
		{
			string label = $"CIVIL{bay}";

			for (int idx = 0; idx < label.Length; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(0x41c8 + idx), (byte)label[idx]);
			}
		}

		/// <summary>
		/// Shows the list of saved games on two tabs, the manual saves and the autosaves (newest first).
		/// Tab, Left and Right switch the tab, the first line of the list does the same.
		/// The drive must already be chosen (AskForSaveDrive) and the mouse pointer hidden, as before the original list.
		/// </summary>
		/// <returns>Manual saved game (0 - 9), AutosaveSlotFlag + autosave (0 - 19), or 0xffff when cancelled</returns>
		private ushort DisplayImportList()
		{
			while (true)
			{
				List<int> bays = new List<int>();
				int strokeTally;

				if (this.displayBackups)
				{
					this.owner.Words.Set("\x008cLoad Game: Autosaves\n Show Saved Games...\n");

					List<(int Bay, int Round)> backups = new List<(int Bay, int Round)>();

					for (int idx = 0; idx < BackupBayTally; idx++)
					{
						int round = PullAutosaveTurn(idx);

						if (round >= 0)
							backups.Add((idx, round));
					}

					backups.Sort((px, py) => py.Round.CompareTo(px.Round));

					for (int idx = 0; idx < backups.Count; idx++)
					{
						AssignAutosaveFileLabel(backups[idx].Bay);
						ImportGame(0x41c6, 1);
						bays.Add(BackupBayMarker + backups[idx].Bay);
					}

					if (backups.Count == 0)
					{
						this.owner.Words.Append(" (no autosaves yet)\n");
						bays.Add(0xffff);
					}

					AssignManualFileLabel(Math.Clamp((int)LastSaveSlot, 0, HandBayTally - 1));
				}
				else
				{
					this.owner.Words.Set("\x008cLoad Game: Saved Games\n Show Autosaves...\n");

					for (int idx = 0; idx < HandBayTally; idx++)
					{
						this.processor.EmitGlobalByte(0x41cd, (byte)(0x30 + idx));

						bays.Add(ImportGame(0x41c6, 1) != 0 ? idx : 0xffff);
					}
				}

				// Title, tab line and the saved games, centered on the screen
				strokeTally = bays.Count + 2;
				int vertLoc = Math.Max(4, (100 - (strokeTally * 8 + 8) / 2) & 0xfffe);

				this.owner.Startup.DisplayMouseCursor();

				// Start on the first saved game, not on the tab line
				DefaultLine = 0x1;
				this.owner.Dialogs.ChoicesIndentTokens = true;
				this.owner.Dialogs.ChoicesIndentTokenDown = false;

				ushort option = this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 48, vertLoc, 1);

				this.owner.Dialogs.ChoicesIndentTokens = false;

				this.owner.Startup.ConcealMouseCursor();

				if (option == 0 || (option == 0xffff && this.owner.Dialogs.ChoicesIndentTokenDown))
				{
					// Switch the tab
					this.displayBackups = !this.displayBackups;
					this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);
					continue;
				}

				if (option == 0xffff)
					return 0xffff;

				if (option <= bays.Count && bays[option - 1] != 0xffff)
					return (ushort)bays[option - 1];

				// An empty slot was chosen, show the list again
			}
		}

		/// <summary>
		/// Shows the list of saved games for Load Game in the GAME menu, without loading anything.
		/// The screen is restored afterwards.
		/// </summary>
		/// <returns>The chosen saved game (see ShowLoadList), or 0xffff when cancelled</returns>
		public ushort ChooseGameToImport()
		{
			this.processor.Journal.OpenScope("ChooseGameToImport()");

			ushort bay = 0xffff;
			LoadingGame = 0x1;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				ScreenBox,
				0, 0, 0x140, 0xc8,
				BackScreenBox,
				0, 0);

			if (QueryForStoreDrive() != 0xffff)
			{
				bay = DisplayImportList();
			}

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("ChooseGameToImport");

			return bay;
		}

		/// <summary>
		/// Loads a saved game chosen with ShowLoadList, from the same drive
		/// </summary>
		/// <param name="slot">Manual saved game (0 - 9) or AutosaveSlotFlag + autosave (0 - 19)</param>
		/// <returns>1 when the game was loaded</returns>
		public ushort ImportGameFromSlot(ushort bay)
		{
			ushort outcome;

			if ((bay & BackupBayMarker) != 0)
			{
				AssignAutosaveFileLabel(bay & 0xff);
				this.FetchingBackup = true;

				outcome = ImportGame(0x41c6, 0);

				this.FetchingBackup = false;
				AssignManualFileLabel(Math.Clamp((int)LastSaveSlot, 0, HandBayTally - 1));
			}
			else
			{
				LastSaveSlot = bay;
				this.processor.EmitGlobalByte(0x41cd, (byte)(0x30 + bay));

				outcome = ImportGame(0x41c6, 0);
			}

			return outcome;
		}

		/// <summary>
		/// Load a Saved Game from the start menu
		/// </summary>
		/// <param name="flag">0xffff to choose the saved game from the list, otherwise the manual saved game to load</param>
		/// <returns>The loaded game (see ShowLoadList), or 0xffff when nothing was loaded</returns>
		public ushort ImportGameFromMenu(ushort marker)
		{
			this.processor.Journal.OpenScope($"ImportGameFromMenu({marker})");

			// function body
			ushort bay = 0xffff;
			LoadingGame = 0x1;

			this.owner.Startup.ConcealMouseCursor();

			if (QueryForStoreDrive() == 0xffff)
			{
				this.owner.Startup.DisplayMouseCursor();
			}
			else
			{
				bay = (marker == 0xffff) ? DisplayImportList() : marker;

				if (bay != 0xffff && ImportGameFromSlot(bay) == 0)
				{
					bay = 0xffff;
				}

				this.owner.Startup.QueryForDisk(0, 1);

				this.owner.Startup.DisplayMouseCursor();
			}

			// Far return
			this.processor.Journal.CloseScope("ImportGameFromMenu");

			this.processor.RegAcc.Value16 = bay;

			return bay;
		}

		/// <summary>
		/// Marker 0: loads the saved game (the extension of the file name becomes SVE), the continents are counted again and
		/// the game continues in the middle of the turn (0xdf60: 2 for an autosave, 1 for a manual save). Otherwise appends the
		/// line of the file to the list of saved games at TextBuffer.Address: " (EMPTY)", or the difficulty, the leader and the civilization
		/// of the human player and the year.
		/// </summary>
		/// <returns>1, 0 for a missing file in the list, also in AX</returns>
		public ushort ImportGame(ushort docnameAddr, ushort marker)
		{
			this.processor.Journal.OpenScope($"ImportGame(0x{docnameAddr:x4}, {marker})");

			// The names are read into a buffer in the stack frame, like in the original
			this.processor.EnterFrame(0x10);

			this.owner.Libc.CopyText((ushort)(docnameAddr + 9), "SVE");

			ushort result = 1;

			if (marker != 0)
			{
				LoadFile = (ushort)this.owner.Libc.OpenHandle(docnameAddr, 0x8000);

				short handle = (short)LoadFile;

				if (handle == -1)
				{
					this.owner.Words.Append(" (EMPTY)\n");
				}
				else
				{
					ushort names = (ushort)(this.processor.RegFrame.Value16 - 0x10);

					// The human player (0x6822), the year (0x6826) and the difficulty (0x6824)
					this.owner.Libc.SeekHandle(handle, 2, 0);
					this.owner.Libc.ReadHandle(handle, 0x6822, 2);

					this.owner.Libc.SeekHandle(handle, 8, 0);
					this.owner.Libc.ReadHandle(handle, 0x6826, 2);

					this.owner.Words.Append(" ");

					this.owner.Libc.SeekHandle(handle, 10, 0);
					this.owner.Libc.ReadHandle(handle, 0x6824, 2);

					this.owner.Words.Append(this.processor.PullGlobalWord((ushort)((SavedDifficulty << 1) + 0x33a2)));

					this.owner.Words.Append(" ");

					this.owner.Libc.SeekHandle(handle, (14 * (int)(short)SavedHumanPlayer) + 16, 0);
					this.owner.Libc.ReadHandle(handle, names, 14);

					this.owner.Words.Append(names);

					this.owner.Libc.SeekHandle(handle, (12 * (int)(short)SavedHumanPlayer) + 0x80, 0);
					this.owner.Libc.ReadHandle(handle, names, 0xc);

					this.owner.Words.Append(", ");

					this.owner.Words.Append(names);

					this.owner.Words.Append("/");

					short year = (short)SavedYear;

					this.owner.Words.Append(this.owner.Libc.IntegerToText(Math.Abs(year), 10));

					this.owner.Words.Append(year < 0 ? " BC\n" : " AD\n");
				}

				this.owner.Libc.CloseHandle(handle);

				if (handle == -1)
				{
					result = 0;
				}
			}
			else
			{
				ImportGameContent(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)));

				this.owner.Startup.QueryForDisk(0, 1);

				this.processor.EmitGlobalWord(ScreenBox, 0x2);

				this.owner.WorldSetup.ReckonContinentSizes(0);

				this.processor.EmitGlobalWord(ScreenBox, 0x0);
				this.processor.EmitGlobalWord(0x6820, 0x8);

				// The original game had 4 manual saves and saved slots 4 - 9 automatically, at the start of a turn.
				// Autosaves now have their own files, all manual saves continue in the middle of the turn.
				MidTurnSave = (ushort)(this.FetchingBackup ? 0x2 : 0x1);
				this.processor.EmitGlobalWord(0x3484, 0xfffd);
				this.owner.World.StarshipMarkers &= 0x7ffe;
			}

			this.processor.RegAcc.Value16 = result;

			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("ImportGame");

			return result;
		}

		/// <summary>
		/// Save Game: the drive, the list of the 10 save files (the slot -1 asks the player, starting at the last one, 0xe168),
		/// the game stored into the chosen file and the message whether it was saved (with write access denied, 0xd, or disk
		/// full, 0x1c, from the error 0x681c).
		/// </summary>
		public void DisplayStoreGameDialog(ushort arg1)
		{
			this.processor.Journal.OpenScope($"DisplayStoreGameDialog({arg1})");

			FileError = 0;
			LoadingGame = 0;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			if (QueryForStoreDrive() != 0xffff)
			{
				this.owner.Words.Set("\x008cSelect Save File...\n");

				for (int bayIdx = 0; bayIdx < HandBayTally; bayIdx++)
				{
					this.processor.EmitGlobalByte(0x41cd, (byte)(bayIdx + 0x30));

					ImportGame(0x41c6, 1);
				}

				DefaultLine = (ushort)this.owner.Helpers.VerifyValueSpan((short)LastSaveSlot, 0, HandBayTally - 1);

				if (arg1 == 0xffff)
				{
					this.owner.Startup.DisplayMouseCursor();

					arg1 = this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 48, 33, 1);

					this.owner.Startup.ConcealMouseCursor();
				}

				if (arg1 != 0xffff)
				{
					LastSaveSlot = arg1;
					this.processor.EmitGlobalByte(0x41cd, (byte)(arg1 + 0x30));

					if (StoreGame(0x41c6) != 0)
					{
						if (FileError == 0)
						{
							this.owner.Words.Set(" Game has been saved.\n");
						}
						else
						{
							this.owner.Words.Set(" Game NOT saved.\n");

							this.owner.Services.FloodBox(ScreenBox, 64, 127, 192, 34, 12);
						}

						if (FileError == 0xd)
						{
							this.owner.Words.Append(" Write access denied.\n");
						}

						if (FileError == 0x1c)
						{
							this.owner.Words.Append(" Disk Full.\n");
						}

						this.owner.Words.Append(" Press key to continue.\n");

						MenuAnyKey = 0x1;

						this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 64, 127, 1);
					}
				}
			}

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("DisplayStoreGameDialog");
		}

		/// <summary>
		/// Saves the game into the file: first a message with the file name, the difficulty (0x33a2 + 2 * level), the leader
		/// (0x19a2 + 2 * player), the civilization (0x1992 + 2 * player) and the year, "... save in progress."
		/// </summary>
		/// <returns>1, also in AX</returns>
		public ushort StoreGame(ushort docnameAddr)
		{
			this.processor.Journal.OpenScope($"StoreGame(0x{docnameAddr:x4})");

			ushort personSlot = (ushort)(this.owner.World.PersonParticipantIdx << 1);

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			Message(" ")
				.Text(docnameAddr)
				.Text("\n ")
				.Text(this.processor.PullGlobalWord((ushort)((this.owner.World.HardnessTier << 1) + 0x33a2)))
				.Text(" ")
				.Text(LeaderNameAt(personSlot))
				.Text("\n ")
				.Text(CivilizationPluralAt(personSlot))
				.Text("/");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.Words.Append("\n ... save in progress.\n");

			MenuAnyKey = 0x1;

			this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 64, 86, 1);

			StoreGameContent(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)));

			this.processor.RegAcc.Value16 = 0x1;

			this.processor.Journal.CloseScope("StoreGame");

			return 0x1;
		}

		/// <summary>
		/// Asks for the drive of the saved games (0x4384, the current drive the first time): a letter A - F chooses it, Return
		/// takes it, Escape cancels. A floppy drive without a disk gives the message "No Disk in Drive ...". The drive letter
		/// goes into the file name at 0x41c6.
		/// </summary>
		/// <returns>The drive (0 = A), or 0xffff when cancelled or without a disk, also in AX</returns>
		public ushort QueryForStoreDrive()
		{
			this.processor.Journal.OpenScope("QueryForStoreDrive()");

			if (SaveDrive == 0xffff)
			{
				this.owner.Libc.DosQueryDrive(0x4384);

				SaveDrive = (ushort)(SaveDrive - 1);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

			if (LoadingGame == 0)
			{
				this.owner.UnitOps.RenderBusyIndicator();
			}

			ushort key;

			do
			{
				this.owner.Words.Set(LoadingGame != 0 ?
					"  Which drive contains your\n    saved game files?\n\n            " :
					"  Which drive contains your\n     Save Game disk?\n\n            ");

				AppendDriveLetter((ushort)(TextBuffer.Address - 1));

				this.owner.Words.Append(":\n\n    Press drive letter and\nReturn when disk is inserted.\n");

				this.owner.Words.Append("    Press Escape to cancel.\n");

				this.owner.BlockText.RenderTextChunk(99, 80, 72, 0);

				key = this.owner.Dialogs.ObtainNavigationKey();

				for (ushort driveIdx = 0; driveIdx < 6; driveIdx++)
				{
					if (key == 'A' + driveIdx || key == 'a' + driveIdx)
					{
						SaveDrive = driveIdx;
					}
				}

				if (key == 0x1b)
				{
					SaveDrive = 0xffff;
				}

				this.owner.Services.FloodBox(ScreenBox, 80, 88, 160, 24, 15);
			}
			while (key != 0xd && key != 0x1b);

			this.owner.Services.FloodBox(ScreenBox, 8, 8, 304, 184, 15);

			ushort drive = SaveDrive;

			if (drive != 0xffff)
			{
				if (VerifyDrive(drive) == 0)
				{
					this.owner.Words.Set("No Disk in Drive A.\n");

					AppendDriveLetter((ushort)(TextBuffer.Address - 3));

					this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 100, 80, 1);

					this.processor.RegAcc.Value16 = 0xffff;
					this.processor.Journal.CloseScope("QueryForStoreDrive");

					return 0xffff;
				}
			}

			this.processor.EmitGlobalByte(0x41c6, (byte)((byte)SaveDrive + 0x61));

			drive = SaveDrive;
			this.processor.RegAcc.Value16 = drive;

			// Far return
			this.processor.Journal.CloseScope("QueryForStoreDrive");

			return drive;
		}

		/// <summary>The letter of the drive (0x4384) is written at the end of the text at TextBuffer.Address, place: (ushort)(TextBuffer.Address - 1) the last character</summary>
		private void AppendDriveLetter(ushort place)
		{
			this.processor.EmitGlobalByte((ushort)(this.owner.Words.Length + place), (byte)((byte)SaveDrive + 0x41));
		}

		/// <summary>
		/// Whether there is a disk in a floppy drive (0 = A, 1 = B; other drives are always ready): the BIOS verifies one sector
		/// (head 0, track 0, sector 2), up to 3 times with a reset of the disk system after each failure. The request is in the
		/// stack frame, like in the original.
		/// </summary>
		/// <returns>1 when the drive is ready, else 0 (also in AX)</returns>
		public ushort VerifyDrive(ushort driveIdx)
		{
			this.processor.Journal.OpenScope($"VerifyDrive({driveIdx})");

			this.processor.EnterFrame(0x12);

			ushort ready = 1;

			if ((short)driveIdx <= 1)
			{
				ushort request = (ushort)(this.processor.RegFrame.Value16 - 0x12);

				this.processor.EmitGlobalWord(request, driveIdx);
				this.processor.EmitGlobalWord((ushort)(request + 2), 0);
				this.processor.EmitGlobalWord((ushort)(request + 4), 0);
				this.processor.EmitGlobalWord((ushort)(request + 6), 2);
				this.processor.EmitGlobalWord((ushort)(request + 8), 1);

				ready = 0;

				for (int attempt = 0; attempt < 3 && ready == 0; attempt++)
				{
					if (this.owner.Libc.BiosDiskCall(4, request))
					{
						ready = 1;
					}
					else
					{
						this.owner.Libc.BiosDiskCall(0, 0);
					}
				}
			}

			this.processor.RegAcc.Value16 = ready;

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("VerifyDrive");

			return ready;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool ImportGameContent(string route)
		{
			route = route.ToUpper();

			this.processor.Journal.OpenScope($"ImportGameContent('{route}')");

			// function body
			bool victory = false;
			string docname = Path.GetFileNameWithoutExtension(CRuntime.ObtainDOSFileLabel(route));

			try
			{
				// read map file
				byte[] scratchAlt;
				VGABitmap? chart = VGABitmap.ViaFile(Path.Combine(this.processor.StandardFolder, $"{docname}.MAP"), out scratchAlt);

				if (chart == null)
					throw new Exception($"Can't read Map file '{docname}.MAP'");

				this.owner.VideoCard.Displays.AssignValueByKey(2, chart);

				// read sve file
				FileStream source = new FileStream(Path.Combine(this.processor.StandardFolder, $"{docname}.SVE"), FileMode.Open);
				this.owner.World.RoundTally = PullInt16(source);
				this.owner.World.PersonParticipantIdx = PullInt16(source);
				this.owner.World.ParticipantMarkers = PullInt16(source);
				this.owner.World.ChanceRoot = PullUInt16(source);
				this.owner.World.Season = PullInt16(source);
				this.owner.World.HardnessTier = PullInt16(source);
				this.owner.World.LiveRealms = PullInt16(source);
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = PullInt16(source);
				PullContent(source, this.processor.PullGlobalWord(0x19a2), 0x70);
				PullContent(source, this.processor.PullGlobalWord(0x1992), 0x60);
				PullContent(source, this.processor.PullGlobalWord(0x1982), 0x58);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].Gold = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].InquiryHeadway = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						this.owner.World.Participants[idx].LiveTroops[jdx] = PullInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						this.owner.World.Participants[idx].TroopsInsideMaking[jdx] = PullInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].FoundScienceTally = PullInt16(source);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						this.owner.World.Participants[idx].FoundScienceMarkers[jdx] = PullUInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].RegimeKind = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.owner.World.Participants[idx].Landmasses[jdx].Approach = PullInt16(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.owner.World.Participants[idx].Relations[jdx] = PullUInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].TownTally = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].TroopTally = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].SoilTally = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].ColonistTally = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].GrandTownExtent = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].MartialMight = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].Standing = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].LevyRatio = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].Tally = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].TouchParticipantTimeleft = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].HorizBegin = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].ChiefVisuals = PullInt16(source);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].Landmasses[jdx].Assault = PullInt16(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].Landmasses[jdx].Guard = PullInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.owner.World.Participants[idx].Landmasses[jdx].TownTally = PullInt16(source);
					}
				}

				PullContent(source, 0xdcfe, 0x100);

				for (int idx = 0; idx < 16; idx++)
				{
					this.owner.World.Landmasses[idx].ConstructionSpotTally = PullInt16(source);
				}
				
				PullContent(source, 0x3772, 0, 0x4b0);

				for (int idx = 0; idx < this.owner.World.TrucePlotContent.Length; idx++)
				{
					this.owner.World.TrucePlotContent[idx] = PullUInt8(source);
				}

				for (int idx = 0; idx < this.owner.World.Towns.Length; idx++)
				{
					this.owner.World.Towns[idx] = City.ReadFrom(source);
				}

				PullContent(source, 0x112a, 0x3b8);

				byte[] troops = new byte[0x3000];
				source.ReadExactly(troops, 0, 0x3000);
				MemoryStream troopSource = new MemoryStream(troops);

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 128; jdx++)
					{
						this.owner.World.Participants[idx].Troops[jdx] = Unit.ReadFrom(troopSource);
					}
				}

				troopSource.Position = 0;
				PullContent(troopSource, 0x81d4, 0x3000);

				troopSource.Close();

				for (int idx = 0; idx < this.owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.ChartSight.GetLength(1); jdx++)
					{
						this.owner.World.ChartSight[idx, jdx] = (sbyte)PullUInt8(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].TacticalSites[jdx].Live = (sbyte)PullUInt8(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].TacticalSites[jdx].Strategy = PullUInt8(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].TacticalSites[jdx].Place.X = (sbyte)PullUInt8(source);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.owner.World.Participants[idx].TacticalSites[jdx].Place.Y = (sbyte)PullUInt8(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					this.owner.World.ScienceLeadingFoundVia[idx] = PullInt16(source);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.owner.World.Participants[idx].TroopsRuined[jdx] = PullInt16(source);
					}
				}

				for (int idx = 0; idx < this.owner.World.TownLabels.Length; idx++)
				{
					char[] townLabel = new char[13];

					for (int jdx = 0; jdx < 13; jdx++)
					{
						townLabel[jdx] = (char)GameLoadAndSave.PullUInt8(source);
					}
					this.owner.World.TownLabels[idx] = new string(townLabel);
				}

				this.owner.World.RerunContentSpan = PullInt16(source);

				for (int idx = 0; idx < this.owner.World.RerunContent.Length; idx++)
				{
					this.owner.World.RerunContent[idx] = PullUInt8(source);
				}

				for (int idx = 0; idx < this.owner.World.MarvelTownIdx.Length; idx++)
				{
					this.owner.World.MarvelTownIdx[idx] = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						this.owner.World.Participants[idx].MissingTroops[jdx] = PullInt16(source);

					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						this.owner.World.Participants[idx].ScienceObtainedSince[jdx] = (sbyte)PullUInt8(source);
					}
				}

				this.owner.World.DirtyCellTally = PullInt16(source);
				this.owner.World.SmogImpactTier = PullInt16(source);
				this.owner.World.WorldHeatingTally = PullInt16(source);
				this.owner.World.SessionPrefMarkers = PullInt16(source);
				PullContent(source, 0xdb44, 0x104);
				this.owner.World.HighestScienceTally = PullInt16(source);
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally = PullInt16(source);
				this.owner.World.DiagMarkers = PullInt16(source);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].ResearchLevyRatio = PullInt16(source);
				}
				
				this.owner.World.FollowingCollectionRound = PullInt16(source);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].RunningSagaStanding = PullInt16(source);
				}

				PullContent(source, 0x3772, 0x16e0, 0x5a0);
				this.owner.World.StarshipMarkers = PullInt16(source);
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio = PullInt16(source);
				this.owner.World.BotStarshipVictoryRatio = PullInt16(source);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].StarshipArrivalSeason = PullInt16(source);
				}

				PullContent(source, 0xd91e, 0x18);
				PullContent(source, 0xb222, 0x18);

				for (int idx = 0; idx < this.owner.World.TownPlaces.Length; idx++)
				{
					this.owner.World.TownPlaces[idx].X = (sbyte)PullUInt8(source);
				}

				for (int idx = 0; idx < this.owner.World.TownPlaces.Length; idx++)
				{
					this.owner.World.TownPlaces[idx].Y = (sbyte)PullUInt8(source);
				}

				this.owner.World.CastleTier = PullInt16(source);
				this.owner.World.TruceRoundTally = PullInt16(source);
				this.owner.World.BotRivalTally = PullInt16(source);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].StarshipInhabitants = PullInt16(source);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					this.owner.World.Participants[idx].StarshipLiftoffSeason = PullInt16(source);
				}

				this.owner.World.ParticipantIdentMarkers = PullInt16(source);

				source.Close();

				// Hit points of the units, a saved game from the original game has none
				string vitalityRoute = Path.Combine(this.processor.StandardFolder, $"{docname}.HP");
				bool vitalityFetched = false;

				if (File.Exists(vitalityRoute))
				{
					using (FileStream vitalitySource = new FileStream(vitalityRoute, FileMode.Open, FileAccess.Read))
					{
						vitalityFetched = this.owner.World.Vitals.Import(vitalitySource);
					}
				}

				if (!vitalityFetched)
				{
					this.owner.World.Vitals.WipeAll();
				}

				victory = true;
			}
			catch (Exception extra)
			{
				Message(extra.Message)
					.Dialog(100, 80);

				victory = false;
			}

			// Far return
			this.processor.Journal.CloseScope("ImportGameContent");

			return victory;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="bufferPtr"></param>
		/// <param name="length"></param>
		private void PullContent(Stream source, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(this.processor.SegData.Value16, stagingAddr), span);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="bufferSeg"></param>
		/// <param name="bufferPtr"></param>
		/// <param name="length"></param>
		private void PullContent(Stream source, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
		}

		/// <summary>Bytes of a file into the memory, also into the ranges kept in C# arrays (B6: the units)</summary>
		private void ReadIntoMemory(Stream source, uint location, ushort span)
		{
			byte[] content = new byte[span];
			source.ReadExactly(content, 0, span);
			this.processor.Storage.EmitChunk(location, content, 0, span);
		}

		/// <summary>Bytes of the memory into a file, also of the ranges kept in C# arrays</summary>
		private void WriteFromMemory(Stream sink, uint location, ushort span)
		{
			byte[] content = new byte[span];
			this.processor.Storage.ReadChunk(location, content, 0, span);
			sink.Write(content, 0, span);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool StoreGameContent(string route)
		{
			route = route.ToUpper();

			this.processor.Journal.OpenScope($"StoreGameContent('{route}')");

			// function body
			bool victory = false;
			string docname = Path.GetFileNameWithoutExtension(CRuntime.ObtainDOSFileLabel(route));

			try
			{
				// write map file
				this.owner.VideoCard.Displays.ObtainValueByKey(2).Store(Path.Combine(this.processor.StandardFolder, $"{docname}.MAP"), false);

				// write sve file
				FileStream sink = new FileStream(Path.Combine(this.processor.StandardFolder, $"{docname}.SVE"), FileMode.Create);
				EmitInt16(sink, this.owner.World.RoundTally);
				EmitInt16(sink, this.owner.World.PersonParticipantIdx);
				EmitInt16(sink, this.owner.World.ParticipantMarkers);
				EmitUInt16(sink, this.owner.World.ChanceRoot);
				EmitInt16(sink, this.owner.World.Season);
				EmitInt16(sink, this.owner.World.HardnessTier);
				EmitInt16(sink, this.owner.World.LiveRealms);
				EmitInt16(sink, this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
				EmitContent(sink, this.processor.PullGlobalWord(0x19a2), 0x70);
				EmitContent(sink, this.processor.PullGlobalWord(0x1992), 0x60);
				EmitContent(sink, this.processor.PullGlobalWord(0x1982), 0x58);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].Gold);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].InquiryHeadway);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].LiveTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].TroopsInsideMaking[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].FoundScienceTally);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						EmitUInt16(sink, this.owner.World.Participants[idx].FoundScienceMarkers[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].RegimeKind);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].Landmasses[jdx].Approach);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						EmitUInt16(sink, this.owner.World.Participants[idx].Relations[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].TownTally);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].TroopTally);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].SoilTally);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].ColonistTally);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].GrandTownExtent);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].MartialMight);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].Standing);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].LevyRatio);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].Tally);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].TouchParticipantTimeleft);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].HorizBegin);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].ChiefVisuals);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].Landmasses[jdx].Assault);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].Landmasses[jdx].Guard);
					}
				}
				
				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].Landmasses[jdx].TownTally);
					}
				}

				EmitContent(sink, 0xdcfe, 0x100);

				for (int idx = 0; idx < 16; idx++)
				{
					EmitInt16(sink, this.owner.World.Landmasses[idx].ConstructionSpotTally);
				}

				EmitContent(sink, 0x3772, 0, 0x4b0);

				for (int idx = 0; idx < this.owner.World.TrucePlotContent.Length; idx++)
				{
					sink.WriteByte(this.owner.World.TrucePlotContent[idx]);
				}

				for (int idx = 0; idx < this.owner.World.Towns.Length; idx++)
				{
					this.owner.World.Towns[idx].WriteTo(sink);
				}

				EmitContent(sink, 0x112a, 0x3b8);

				EmitContent(sink, 0x81d4, 0x3000);

				for (int idx = 0; idx < this.owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.ChartSight.GetLength(1); jdx++)
					{
						sink.WriteByte((byte)this.owner.World.ChartSight[idx, jdx]);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.owner.World.Participants[idx].TacticalSites[jdx].Live);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte(this.owner.World.Participants[idx].TacticalSites[jdx].Strategy);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.owner.World.Participants[idx].TacticalSites[jdx].Place.X);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.owner.World.Participants[idx].TacticalSites[jdx].Place.Y);
					}
				}

				for (int idx = 0; idx < this.owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.ScienceLeadingFoundVia[idx]);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].TroopsRuined[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.TownLabels.Length; idx++)
				{
					for (int jdx = 0; jdx < 13; jdx++)
					{
						sink.WriteByte((byte)this.owner.World.TownLabels[idx][jdx]);
					}
				}

				EmitInt16(sink, this.owner.World.RerunContentSpan);
				for (int idx = 0; idx < this.owner.World.RerunContent.Length; idx++)
				{
					sink.WriteByte(this.owner.World.RerunContent[idx]);
				}

				for (int idx = 0; idx < this.owner.World.MarvelTownIdx.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.MarvelTownIdx[idx]);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						EmitInt16(sink, this.owner.World.Participants[idx].MissingTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						sink.WriteByte((byte)((sbyte)this.owner.World.Participants[idx].ScienceObtainedSince[jdx]));
					}
				}

				EmitInt16(sink, this.owner.World.DirtyCellTally);
				EmitInt16(sink, this.owner.World.SmogImpactTier);
				EmitInt16(sink, this.owner.World.WorldHeatingTally);
				EmitInt16(sink, this.owner.World.SessionPrefMarkers);
				EmitContent(sink, 0xdb44, 0x104);
				EmitInt16(sink, this.owner.World.HighestScienceTally);
				EmitInt16(sink, this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally);
				EmitInt16(sink, this.owner.World.DiagMarkers);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].ResearchLevyRatio);
				}
				
				EmitInt16(sink, this.owner.World.FollowingCollectionRound);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].RunningSagaStanding);
				}

				EmitContent(sink, 0x3772, 0x16e0, 0x5a0);
				EmitInt16(sink, this.owner.World.StarshipMarkers);
				EmitInt16(sink, this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio);
				EmitInt16(sink, this.owner.World.BotStarshipVictoryRatio);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].StarshipArrivalSeason);
				}

				EmitContent(sink, 0xd91e, 0x18);
				EmitContent(sink, 0xb222, 0x18);

				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.owner.World.TownPlaces[idx].X));
				}
				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.owner.World.TownPlaces[idx].Y));
				}

				EmitInt16(sink, this.owner.World.CastleTier);
				EmitInt16(sink, this.owner.World.TruceRoundTally);
				EmitInt16(sink, this.owner.World.BotRivalTally);

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].StarshipInhabitants);
				}

				for (int idx = 0; idx < this.owner.World.Participants.Length; idx++)
				{
					EmitInt16(sink, this.owner.World.Participants[idx].StarshipLiftoffSeason);
				}

				EmitInt16(sink, this.owner.World.ParticipantIdentMarkers);

				sink.Close();

				// Hit points of the units, in a separate file so the original game can still load the saved game
				using (FileStream vitalitySink = new FileStream(Path.Combine(this.processor.StandardFolder, $"{docname}.HP"), FileMode.Create))
				{
					this.owner.World.Vitals.Store(vitalitySink);
				}

				victory = true;
			}
			catch (Exception extra)
			{
				this.owner.Words.Set(extra.Message);
				this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 4, 64, 1);

				victory = false;
			}

			// Far return
			this.processor.Journal.CloseScope("StoreGameContent");

			return victory;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bufferPtr"></param>
		/// <param name="length"></param>
		private void EmitContent(Stream sink, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(this.processor.SegData.Value16, stagingAddr), span);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bufferSeg"></param>
		/// <param name="bufferPtr"></param>
		/// <param name="length"></param>
		private void EmitContent(Stream sink, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
		}

		public static byte PullUInt8(Stream source) { return StreamIo.PullUInt8(source); }

		public static ushort PullUInt16(Stream source) { return StreamIo.PullUInt16(source); }

		public static short PullInt16(Stream source) { return StreamIo.PullInt16(source); }

		public static void EmitUInt16(Stream sink, ushort amount) { StreamIo.EmitUInt16(sink, amount); }

		public static void EmitInt16(Stream sink, short amount) { StreamIo.EmitInt16(sink, amount); }
	}
}
