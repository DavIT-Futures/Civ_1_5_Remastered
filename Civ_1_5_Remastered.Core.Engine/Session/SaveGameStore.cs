using System;
using System.Collections.Generic;
using System.IO;

namespace Civ_1_5_Remastered.Core.Engine.Session
{
	public partial class SaveGameStore : GameComponent, ISaveGameStore
	{
		public const int HandBayTally = 20;

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

		private int originalFrigatePrice = Civ_1_5_Remastered.Core.Shared.Tables.RemasterUnitTable.OriginalFrigatePrice;

		private bool displayBackups = false;

		public SaveGameStore(IGameEngine owner)
			: base(owner)
		{
		}

		public ushort FetchBay { get; set; } = 0xffff;

		public bool FetchingBackup { get; set; } = false;

		public void PerformBackup()
		{
			int bay = this.Owner.World.RoundTally % BackupBayTally;

			if (bay < 0)
			{
				bay += BackupBayTally;
			}

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
				if (bay < 10)
				{
					this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(0x30 + bay));
				}

				AssignManualFileLabel(bay);

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

		public ushort LoadGame(ushort documentNameAddress, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ImportGame(0x{documentNameAddress:x4}, {marker})");

			string documentPath = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress));
			if (!documentPath.EndsWith(".SVE", StringComparison.OrdinalIgnoreCase))
			{
				int dotIndex = documentPath.LastIndexOf('.');
				if (dotIndex >= 0)
				{
					this.Owner.Runtime.CopyText((ushort)(documentNameAddress + dotIndex + 1), "SVE");
				}
				else
				{
					this.Owner.Runtime.CopyText((ushort)(documentNameAddress + documentPath.Length), ".SVE");
				}
			}

			ushort result = 1;

			if (marker != 0)
			{
				Files.LoadFile = (ushort)this.Owner.Runtime.OpenHandle(documentNameAddress, 0x8000);

				short handle = (short)Files.LoadFile;

				if (handle == -1)
				{
					this.Owner.Words.Append(" (EMPTY)\n");
				}
				else
				{
					this.Owner.Runtime.SeekHandle(handle, 2, 0);
					this.Owner.Runtime.ReadHandle(handle, this.Owner.Globals.Files.SavedHumanPlayerAddress, 2);

					this.Owner.Runtime.SeekHandle(handle, 8, 0);
					this.Owner.Runtime.ReadHandle(handle, this.Owner.Globals.Files.SavedYearAddress, 2);

					this.Owner.Words.Append(" ");

					this.Owner.Runtime.SeekHandle(handle, 10, 0);
					this.Owner.Runtime.ReadHandle(handle, this.Owner.Globals.Files.SavedDifficultyAddress, 2);

					this.Owner.Words.Append(this.Processor.PullGlobalWord((ushort)((Files.SavedDifficulty << 1) + DifficultyNamesTableAddress)));

					this.Owner.Words.Append(" ");

					this.Owner.Runtime.SeekHandle(handle, (14 * (int)(short)Files.SavedHumanPlayer) + 16, 0);
					string leaderName = this.Owner.Runtime.ReadText(handle, 14);

					this.Owner.Words.Append(leaderName);

					this.Owner.Runtime.SeekHandle(handle, (12 * (int)(short)Files.SavedHumanPlayer) + 0x80, 0);
					string civilizationName = this.Owner.Runtime.ReadText(handle, 0xc);

					this.Owner.Words.Append(", ");

					this.Owner.Words.Append(civilizationName);

					this.Owner.Words.Append("/");

					short year = (short)Files.SavedYear;

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(Math.Abs(year), 10));

					this.Owner.Words.Append(year < 0 ? " BC\n" : " AD\n");
				}

				this.Owner.Runtime.CloseHandle(handle);

				if (handle == -1)
				{
					result = 0;
				}
			}
			else
			{
				LoadGameContent(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)));

				this.Owner.Startup.AskForDisk(0, 1);

				DrawingBox.DisplayIndex = 0x2;

				this.Owner.WorldSetup.ReckonContinentSizes(0);

				DrawingBox.DisplayIndex = 0x0;
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

				for (int bayIndex = 0; bayIndex < HandBayTally; bayIndex++)
				{
					if (bayIndex < 10)
					{
						this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(bayIndex + 0x30));
					}

					AssignManualFileLabel(bayIndex);

					LoadGame(SaveFileNameAddress, 1);
				}

				Menu.DefaultLine = (ushort)this.Owner.Helpers.ClampValue((short)Files.LastSaveSlot, 0, HandBayTally - 1);

				if (slot == 0xffff)
				{
					this.Owner.Startup.ShowMouseCursor();

					int strokeTally = HandBayTally + 1;
					int verticalLocation = Math.Max(4, (100 - (((strokeTally * 8) + 8) / 2)) & 0xfffe);

					slot = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 48, verticalLocation, 1);

					this.Owner.Startup.HideMouseCursor();
				}

				if (slot != 0xffff)
				{
					Files.LastSaveSlot = slot;
					if (slot < 10)
					{
						this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(slot + 0x30));
					}

					AssignManualFileLabel(slot);

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

		public ushort SaveGame(ushort documentNameAddress)
		{
			using var _ = this.Processor.Journal.Scope($"StoreGame(0x{documentNameAddress:x4})");

			ushort personSlot = (ushort)(this.Owner.World.PersonParticipantIndex << 1);

			Screen.DialogNeedsRedraw = 1;

			Message(" ")
				.Text(documentNameAddress)
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

			SaveGameContent(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)));

			return 0x1;
		}

		public ushort AskForSaveDrive()
		{
			using var _ = this.Processor.Journal.Scope("QueryForStoreDrive()");

			if (Files.SaveDrive == 0xffff)
			{
				this.Owner.Runtime.OperatingSystemQueryDrive(SavedGameDriveLetterAddress);

				Files.SaveDrive = (ushort)(Files.SaveDrive - 1);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

			if (Files.LoadingGame == 0)
			{
				this.Owner.UnitOperations.DrawBusyIndicator();
			}

			ushort key;

			while (true)
			{
				this.Owner.Words.Set(Files.LoadingGame != 0 ? "  Which drive contains your\n    saved game files?\n\n            " : "  Which drive contains your\n     Save Game disk?\n\n            ");

				AppendDriveLetter((ushort)(TextBuffer.Address - 1));

				this.Owner.Words.Append(":\n\n    Press drive letter and\nReturn when disk is inserted.\n");

				this.Owner.Words.Append("    Press Escape to cancel.\n");

				this.Owner.BlockText.DrawTextChunk(99, 80, 72, 0);

				key = this.Owner.Dialogs.ReadNavigationKey();

				for (ushort driveIndex = 0; driveIndex < 6; driveIndex++)
				{
					if (key == 'A' + driveIndex || key == 'a' + driveIndex)
					{
						Files.SaveDrive = driveIndex;
					}
				}

				if (key == KeyCodes.Escape)
				{
					Files.SaveDrive = 0xffff;
				}

				this.Owner.Services.FloodBox(Screen.Box, 80, 88, 160, 24, Hues.White);

				if (key == KeyCodes.Enter || key == KeyCodes.Escape)
				{
					break;
				}
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

		public ushort CheckDrive(ushort driveIndex)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyDrive({driveIndex})");

			ushort ready = 1;

			if ((short)driveIndex <= 1)
			{
				ready = 0;

				for (int attempt = 0; attempt < 3 && ready == 0; attempt++)
				{
					if (this.Owner.Runtime.FirmwareDiskCall(4, (int)driveIndex))
					{
						ready = 1;
					}
					else
					{
						this.Owner.Runtime.FirmwareDiskCall(0, 0);
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
						{
							return (short)(lower | (upper << 8));
						}
					}
				}
			}
			catch
			{ }

			return -1;
		}

		private void AssignAutosaveFileLabel(int bay)
		{
			string label = $"{AutosaveLabel(bay)}.SVE\0";

			for (int index = 0; index < label.Length; index++)
			{
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(SaveFileNamePatternAddress + index), (byte)label[index]);
			}
		}

		private void AssignManualFileLabel(int bay)
		{
			string label = $"CIVIL{bay}.SVE\0";

			for (int index = 0; index < label.Length; index++)
			{
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(SaveFileNamePatternAddress + index), (byte)label[index]);
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

					for (int index = 0; index < BackupBayTally; index++)
					{
						int round = PullAutosaveTurn(index);

						if (round >= 0)
						{
							backups.Add((index, round));
						}
					}

					backups.Sort((pixelHorizontal, pixelVertical) => pixelVertical.Round.CompareTo(pixelHorizontal.Round));

					for (int index = 0; index < backups.Count; index++)
					{
						AssignAutosaveFileLabel(backups[index].Bay);
						LoadGame(SaveFileNameAddress, 1);
						bays.Add(BackupBayMarker + backups[index].Bay);
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

					for (int index = 0; index < HandBayTally; index++)
					{
						if (index < 10)
						{
							this.Processor.EmitGlobalByte(SaveSlotDigitAddress, (byte)(0x30 + index));
						}

						AssignManualFileLabel(index);

						bays.Add(LoadGame(SaveFileNameAddress, 1) != 0 ? index : 0xffff);
					}
				}

				strokeTally = bays.Count + 2;
				int verticalLocation = Math.Max(4, (100 - (((strokeTally * 8) + 8) / 2)) & 0xfffe);

				this.Owner.Startup.ShowMouseCursor();

				Menu.DefaultLine = 0x1;
				this.Owner.Dialogs.ChoicesIndentTokens = true;
				this.Owner.Dialogs.ChoicesIndentTokenDown = false;

				ushort option = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 48, verticalLocation, 1);

				this.Owner.Dialogs.ChoicesIndentTokens = false;

				this.Owner.Startup.HideMouseCursor();

				if (option == 0 || (option == 0xffff && this.Owner.Dialogs.ChoicesIndentTokenDown))
				{
					this.displayBackups = !this.displayBackups;
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);
					continue;
				}

				if (option == 0xffff)
				{
					return 0xffff;
				}

				if (option <= bays.Count && bays[option - 1] != 0xffff)
				{
					return (ushort)bays[option - 1];
				}
			}
		}

		private void AppendDriveLetter(ushort place)
		{
			this.Processor.EmitGlobalByte((ushort)(this.Owner.Words.Length + place), (byte)((byte)Files.SaveDrive + 0x41));
		}
	}
}
