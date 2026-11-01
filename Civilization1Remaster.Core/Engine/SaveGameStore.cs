using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
	public partial class SaveGameStore : GameComponent, ISaveGameStore
	{
		public const ushort FetchSessionAsk = 3;

		public const int HandBayTally = 10;

		public const int BackupBayTally = 20;

		public const ushort BackupBayMarker = 0x100;

		private const ushort SaveFileNameAddress = 0x41c6;
		private const ushort SaveFileNamePatternAddress = 0x41c8;
		private const ushort SaveSlotDigitAddress = 0x41cd;
		private const ushort DifficultyNamesTableAddress = 0x33a2;
		private const ushort SavedGameDriveLetterAddress = 0x4384;
		private const ushort ExploredMapSegment = 0xdcfe;
		private const ushort ExploredMapSize = 0x100;
		private const ushort CivilizationHistorySegment = 0x3772;
		private const ushort CivilizationHistoryBaseAddress = 0x0;
		private const ushort CivilizationHistorySize = 0x4b0;
		private const ushort CityNamesStatusAddress = 0x112a;
		private const ushort CityNamesStatusSize = 0x3b8;
		private const ushort UnitsBufferSize = 0x3000;
		private const ushort ReplayTurnsAddress = 0xdb44;
		private const ushort ReplayTurnsSize = 0x104;
		private const ushort SpaceshipGridSegment = 0x3772;
		private const ushort SpaceshipGridBaseAddress = 0x16e0;
		private const ushort SpaceshipGridTotalSize = 0x5a0;
		private const ushort PalaceSectionRecordSize = 0x18;

		private bool displayBackups = false;

		public SaveGameStore(GameEngine owner)
			: base(owner)
		{
		}

		public ushort FetchBay { get; set; } = 0xffff;

		public bool FetchingBackup { get; set; } = false;

		public void PerformBackup()
		{
			int bay = this.Owner.World.RoundTally % BackupBayTally;

			if (bay < 0)
				bay += BackupBayTally;

			SaveGameContent($"{AutosaveLabel(bay)}.SVE");
		}

		public ushort ChooseGameToImport()
		{
			using var _ = this.Processor.Journal.Scope("ChooseGameToImport()");

			ushort bay = 0xffff;
			Files.LoadingGame = 0x1;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.BackScreenBox,
				0, 0);

			if (AskForSaveDrive() != 0xffff)
			{
				bay = ShowImportList();
			}

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Startup.ShowMouseCursor();

			return bay;
		}

		public ushort LoadGameFromSlot(ushort bay)
		{
			ushort outcome;

			if ((bay & BackupBayMarker) != 0)
			{
				AssignAutosaveFileLabel(bay & 0xff);
				this.FetchingBackup = true;

				outcome = LoadGame(SaveFileNameAddress, 0);

				this.FetchingBackup = false;
				AssignManualFileLabel(Math.Clamp((int)Files.LastSaveSlot, 0, HandBayTally - 1));
			}
			else
			{
				Files.LastSaveSlot = bay;
				this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(0x30 + bay));

				outcome = LoadGame(SaveFileNameAddress, 0);
			}

			return outcome;
		}

		public ushort LoadGameFromMenu(ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ImportGameFromMenu({marker})");

			ushort bay = 0xffff;
			Files.LoadingGame = 0x1;

			this.Owner.Startup.HideMouseCursor();

			if (AskForSaveDrive() == 0xffff)
			{
				this.Owner.Startup.ShowMouseCursor();
			}
			else
			{
				bay = (marker == 0xffff) ? ShowImportList() : marker;

				if (bay != 0xffff && LoadGameFromSlot(bay) == 0)
				{
					bay = 0xffff;
				}

				this.Owner.Startup.AskForDisk(0, 1);

				this.Owner.Startup.ShowMouseCursor();
			}

			return bay;
		}

		public ushort LoadGame(ushort docnameAddr, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ImportGame(0x{docnameAddr:x4}, {marker})");

			this.Owner.Libc.CopyText((ushort)(docnameAddr + 9), "SVE");

			ushort result = 1;

			if (marker != 0)
			{
				Files.LoadFile = (ushort)this.Owner.Libc.OpenHandle(docnameAddr, 0x8000);

				short handle = (short)Files.LoadFile;

				if (handle == -1)
				{
					this.Owner.Words.Append(" (EMPTY)\n");
				}
				else
				{
					this.Owner.Libc.SeekHandle(handle, 2, 0);
					this.Owner.Libc.ReadHandle(handle, this.Owner.Globals.Files.SavedHumanPlayerAddress, 2);

					this.Owner.Libc.SeekHandle(handle, 8, 0);
					this.Owner.Libc.ReadHandle(handle, this.Owner.Globals.Files.SavedYearAddress, 2);

					this.Owner.Words.Append(" ");

					this.Owner.Libc.SeekHandle(handle, 10, 0);
					this.Owner.Libc.ReadHandle(handle, this.Owner.Globals.Files.SavedDifficultyAddress, 2);

					this.Owner.Words.Append(this.Processor.PullGlobalWord((ushort)((Files.SavedDifficulty << 1) + DifficultyNamesTableAddress)));

					this.Owner.Words.Append(" ");

					this.Owner.Libc.SeekHandle(handle, (14 * (int)(short)Files.SavedHumanPlayer) + 16, 0);
					string leaderName = this.Owner.Libc.ReadText(handle, 14);

					this.Owner.Words.Append(leaderName);

					this.Owner.Libc.SeekHandle(handle, (12 * (int)(short)Files.SavedHumanPlayer) + 0x80, 0);
					string civName = this.Owner.Libc.ReadText(handle, 0xc);

					this.Owner.Words.Append(", ");

					this.Owner.Words.Append(civName);

					this.Owner.Words.Append("/");

					short year = (short)Files.SavedYear;

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs(year), 10));

					this.Owner.Words.Append(year < 0 ? " BC\n" : " AD\n");
				}

				this.Owner.Libc.CloseHandle(handle);

				if (handle == -1)
				{
					result = 0;
				}
			}
			else
			{
				LoadGameContent(this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, docnameAddr)));

				this.Owner.Startup.AskForDisk(0, 1);

				DrawingBox.DisplayIdx = 0x2;

				this.Owner.WorldSetup.ReckonContinentSizes(0);

				DrawingBox.DisplayIdx = 0x0;
				Files.ImportPlayerIndex = 8;

				Files.MidTurnSave = (ushort)(this.FetchingBackup ? 0x2 : 0x1);
				Files.LoadedGameMark = 0xfffd;
				this.Owner.World.StarshipMarkers &= 0x7ffe;
			}

			return result;
		}

		public void ShowStoreGameDialog(ushort slot)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayStoreGameDialog({slot})");

			Files.FileError = 0;
			Files.LoadingGame = 0;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			if (AskForSaveDrive() != 0xffff)
			{
				this.Owner.Words.Set("\x008cSelect Save File...\n");

				for (int bayIdx = 0; bayIdx < HandBayTally; bayIdx++)
				{
					this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(bayIdx + 0x30));

					LoadGame(SaveFileNameAddress, 1);
				}

				Menu.DefaultLine = (ushort)this.Owner.Helpers.ClampValue((short)Files.LastSaveSlot, 0, HandBayTally - 1);

				if (slot == 0xffff)
				{
					this.Owner.Startup.ShowMouseCursor();

					slot = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 48, 33, 1);

					this.Owner.Startup.HideMouseCursor();
				}

				if (slot != 0xffff)
				{
					Files.LastSaveSlot = slot;
					this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(slot + 0x30));

					if (SaveGame(SaveFileNameAddress) != 0)
					{
						if (Files.FileError == 0)
						{
							this.Owner.Words.Set(" Game has been saved.\n");
						}
						else
						{
							this.Owner.Words.Set(" Game NOT saved.\n");

							this.Owner.Services.FloodBox(Screen.Box, 64, 127, 192, 34, Hues.LightRed);
						}

						if (Files.FileError == 0xd)
						{
							this.Owner.Words.Append(" Write access denied.\n");
						}

						if (Files.FileError == 0x1c)
						{
							this.Owner.Words.Append(" Disk Full.\n");
						}

						this.Owner.Words.Append(" Press key to continue.\n");

						Menu.AnyKey = 0x1;

						this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 64, 127, 1);
					}
				}
			}

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort SaveGame(ushort docnameAddr)
		{
			using var _ = this.Processor.Journal.Scope($"StoreGame(0x{docnameAddr:x4})");

			ushort personSlot = (ushort)(this.Owner.World.PersonParticipantIdx << 1);

			Screen.DialogNeedsRedraw = 1;

			Message(" ")
				.Text(docnameAddr)
				.Text("\n ")
				.Text(this.Processor.PullGlobalWord((ushort)((this.Owner.World.HardnessTier << 1) + DifficultyNamesTableAddress)))
				.Text(" ")
				.Text(Tables.LeaderNameAt(personSlot))
				.Text("\n ")
				.Text(Tables.CivilizationPluralAt(personSlot))
				.Text("/");

			this.Owner.TurnHooks.FormatCurrentYear();

			this.Owner.Words.Append("\n ... save in progress.\n");

			Menu.AnyKey = 0x1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 64, 86, 1);

			SaveGameContent(this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, docnameAddr)));

			return 0x1;
		}

		public ushort AskForSaveDrive()
		{
			using var _ = this.Processor.Journal.Scope("QueryForStoreDrive()");

			if (Files.SaveDrive == 0xffff)
			{
				this.Owner.Libc.DosQueryDrive(SavedGameDriveLetterAddress);

				Files.SaveDrive = (ushort)(Files.SaveDrive - 1);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

			if (Files.LoadingGame == 0)
			{
				this.Owner.UnitOps.DrawBusyIndicator();
			}

			ushort key;

			while (true)
			{
				this.Owner.Words.Set(Files.LoadingGame != 0 ?
					"  Which drive contains your\n    saved game files?\n\n            " :
					"  Which drive contains your\n     Save Game disk?\n\n            ");

				AppendDriveLetter((ushort)(TextBuffer.Address - 1));

				this.Owner.Words.Append(":\n\n    Press drive letter and\nReturn when disk is inserted.\n");

				this.Owner.Words.Append("    Press Escape to cancel.\n");

				this.Owner.BlockText.DrawTextChunk(99, 80, 72, 0);

				key = this.Owner.Dialogs.ReadNavigationKey();

				for (ushort driveIdx = 0; driveIdx < 6; driveIdx++)
				{
					if (key == 'A' + driveIdx || key == 'a' + driveIdx)
					{
						Files.SaveDrive = driveIdx;
					}
				}

				if (key == KeyCodes.Escape)
				{
					Files.SaveDrive = 0xffff;
				}

				this.Owner.Services.FloodBox(Screen.Box, 80, 88, 160, 24, Hues.White);

				if (key == KeyCodes.Enter || key == KeyCodes.Escape)
					break;
			}

			this.Owner.Services.FloodBox(Screen.Box, 8, 8, 304, 184, Hues.White);

			ushort drive = Files.SaveDrive;

			if (drive != 0xffff)
			{
				if (CheckDrive(drive) == 0)
				{
					this.Owner.Words.Set("No Disk in Drive A.\n");

					AppendDriveLetter((ushort)(TextBuffer.Address - 3));

					this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 100, 80, 1);

					return 0xffff;
				}
			}

			this.Processor.EmitGlobalByte(SaveFileNameAddress, (byte)((byte)Files.SaveDrive + 0x61));

			drive = Files.SaveDrive;

			return drive;
		}

		public ushort CheckDrive(ushort driveIdx)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyDrive({driveIdx})");

			ushort ready = 1;

			if ((short)driveIdx <= 1)
			{
				ready = 0;

				for (int attempt = 0; attempt < 3 && ready == 0; attempt++)
				{
					if (this.Owner.Libc.BiosDiskCall(4, (int)driveIdx))
					{
						ready = 1;
					}
					else
					{
						this.Owner.Libc.BiosDiskCall(0, 0);
					}
				}
			}

			return ready;
		}

		private static string AutosaveLabel(int bay)
		{
			return $"AUTO{bay:00}";
		}

		private int PullAutosaveTurn(int bay)
		{
			string route = Path.Combine(this.Processor.StandardFolder, $"{AutosaveLabel(bay)}.SVE");

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

		private void AssignAutosaveFileLabel(int bay)
		{
			string label = AutosaveLabel(bay);

			for (int idx = 0; idx < label.Length; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(SaveFileNamePatternAddress + idx), (byte)label[idx]);
			}
		}

		private void AssignManualFileLabel(int bay)
		{
			string label = $"CIVIL{bay}";

			for (int idx = 0; idx < label.Length; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(SaveFileNamePatternAddress + idx), (byte)label[idx]);
			}
		}

		private ushort ShowImportList()
		{
			while (true)
			{
				List<int> bays = new List<int>();
				int strokeTally;

				if (this.displayBackups)
				{
					this.Owner.Words.Set("\x008cLoad Game: Autosaves\n Show Saved Games...\n");

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
						LoadGame(SaveFileNameAddress, 1);
						bays.Add(BackupBayMarker + backups[idx].Bay);
					}

					if (backups.Count == 0)
					{
						this.Owner.Words.Append(" (no autosaves yet)\n");
						bays.Add(0xffff);
					}

					AssignManualFileLabel(Math.Clamp((int)Files.LastSaveSlot, 0, HandBayTally - 1));
				}
				else
				{
					this.Owner.Words.Set("\x008cLoad Game: Saved Games\n Show Autosaves...\n");

					for (int idx = 0; idx < HandBayTally; idx++)
					{
						this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(0x30 + idx));

						bays.Add(LoadGame(SaveFileNameAddress, 1) != 0 ? idx : 0xffff);
					}
				}

				strokeTally = bays.Count + 2;
				int vertLoc = Math.Max(4, (100 - (strokeTally * 8 + 8) / 2) & 0xfffe);

				this.Owner.Startup.ShowMouseCursor();

				Menu.DefaultLine = 0x1;
				this.Owner.Dialogs.ChoicesIndentTokens = true;
				this.Owner.Dialogs.ChoicesIndentTokenDown = false;

				ushort option = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 48, vertLoc, 1);

				this.Owner.Dialogs.ChoicesIndentTokens = false;

				this.Owner.Startup.HideMouseCursor();

				if (option == 0 || (option == 0xffff && this.Owner.Dialogs.ChoicesIndentTokenDown))
				{
					this.displayBackups = !this.displayBackups;
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);
					continue;
				}

				if (option == 0xffff)
					return 0xffff;

				if (option <= bays.Count && bays[option - 1] != 0xffff)
					return (ushort)bays[option - 1];
			}
		}

		private void AppendDriveLetter(ushort place)
		{
			this.Processor.EmitGlobalByte((ushort)(this.Owner.Words.Length + place), (byte)((byte)Files.SaveDrive + 0x41));
		}
	}
}
