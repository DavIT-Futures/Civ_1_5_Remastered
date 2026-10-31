using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
	public class SaveGameStore : GameComponent, ISaveGameStore
	{
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

		// The load list remembers which tab was open
		private bool displayBackups = false;

		public SaveGameStore(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public ushort FetchBay { get; set; } = 0xffff;

		/// <inheritdoc/>
		public bool FetchingBackup { get; set; } = false;

		/// <inheritdoc/>
		public void PerformBackup()
		{
			int bay = this.Owner.World.RoundTally % BackupBayTally;

			if (bay < 0)
				bay += BackupBayTally;

			SaveGameContent($"{AutosaveLabel(bay)}.SVE");
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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
					// The human player (Files.SavedHumanPlayer), the year (Files.SavedYear) and the difficulty (Files.SavedDifficulty)
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

				// The original game had 4 manual saves and saved slots 4 - 9 automatically, at the start of a turn.
				// Autosaves now have their own files, all manual saves continue in the middle of the turn.
				Files.MidTurnSave = (ushort)(this.FetchingBackup ? 0x2 : 0x1);
				Files.LoadedGameMark = 0xfffd;
				this.Owner.World.StarshipMarkers &= 0x7ffe;
			}


			return result;
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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
				.Text(LeaderNameAt(personSlot))
				.Text("\n ")
				.Text(CivilizationPluralAt(personSlot))
				.Text("/");

			this.Owner.TurnHooks.FormatCurrentYear();

			this.Owner.Words.Append("\n ... save in progress.\n");

			Menu.AnyKey = 0x1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 64, 86, 1);

			SaveGameContent(this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, docnameAddr)));


			return 0x1;
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public bool LoadGameContent(string route)
		{
			route = route.ToUpper();

			using var _ = this.Processor.Journal.Scope($"ImportGameContent('{route}')");

			bool victory = false;
			string docname = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainDOSFileLabel(route));

			try
			{
				// read map file
				byte[] scratchAlt;
				VGABitmap? chart = VGABitmap.ViaFile(Path.Combine(this.Processor.StandardFolder, $"{docname}.MAP"), out scratchAlt);

				if (chart == null)
					throw new Exception($"Can't read Map file '{docname}.MAP'");

				if (!this.Owner.VideoCard.Displays.ContainsKey(2))
					throw new InvalidOperationException("This collection doesn't contain key '2'");

				this.Owner.VideoCard.Displays[2] = chart;

				// read sve file
				FileStream source = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.SVE"), FileMode.Open);
				this.Owner.World.RoundTally = source.PullInt16OrZero();
				this.Owner.World.PersonParticipantIdx = source.PullInt16OrZero();
				this.Owner.World.ParticipantMarkers = source.PullInt16OrZero();
				this.Owner.World.ChanceRoot = source.PullUInt16OrZero();
				this.Owner.World.Season = source.PullInt16OrZero();
				this.Owner.World.HardnessTier = source.PullInt16OrZero();
				this.Owner.World.LiveRealms = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].PresentInquiryIdx = source.PullInt16OrZero();
				PullContent(source, LeaderNameAt(0), 0x70);
				PullContent(source, CivilizationPluralAt(0), 0x60);
				PullContent(source, CivilizationNameAt(0), 0x58);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Gold = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].InquiryHeadway = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						this.Owner.World.Participants[idx].LiveTroops[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						this.Owner.World.Participants[idx].TroopsInsideMaking[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].FoundScienceTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						this.Owner.World.Participants[idx].FoundScienceMarkers[jdx] = source.PullUInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].RegimeKind = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Approach = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.Owner.World.Participants[idx].Relations[jdx] = source.PullUInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TownTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TroopTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].SoilTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ColonistTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].GrandTownExtent = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].MartialMight = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Standing = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].LevyRatio = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Tally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TouchParticipantTimeleft = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].HorizBegin = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ChiefVisuals = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Assault = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Guard = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].TownTally = source.PullInt16OrZero();
					}
				}

				PullContent(source, ExploredMapSegment, ExploredMapSize);

				for (int idx = 0; idx < 16; idx++)
				{
					this.Owner.World.Landmasses[idx].ConstructionSpotTally = source.PullInt16OrZero();
				}
				
				PullContent(source, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int idx = 0; idx < this.Owner.World.TrucePlotContent.Length; idx++)
				{
					this.Owner.World.TrucePlotContent[idx] = source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Towns.Length; idx++)
				{
					this.Owner.World.Towns[idx] = City.ReadFrom(source);
				}

				PullContent(source, CityNamesStatusAddress, CityNamesStatusSize);

				byte[] troops = new byte[UnitsBufferSize];
				source.ReadExactly(troops, 0, UnitsBufferSize);
				MemoryStream troopSource = new MemoryStream(troops);

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 128; jdx++)
					{
						this.Owner.World.Participants[idx].Troops[jdx] = Unit.ReadFrom(troopSource);
					}
				}

				troopSource.Position = 0;
				PullContent(troopSource, UnitMemory.Origin, UnitsBufferSize);

				troopSource.Close();

				for (int idx = 0; idx < this.Owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.ChartSight.GetLength(1); jdx++)
					{
						this.Owner.World.ChartSight[idx, jdx] = (sbyte)source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Live = (sbyte)source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Strategy = source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Place = new Point((sbyte)source.PullUInt8OrZero(), this.Owner.World.Participants[idx].TacticalSites[jdx].Place.Y);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Place = new Point(this.Owner.World.Participants[idx].TacticalSites[jdx].Place.X, (sbyte)source.PullUInt8OrZero());
					}
				}

				for (int idx = 0; idx < this.Owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					this.Owner.World.ScienceLeadingFoundVia[idx] = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.Owner.World.Participants[idx].TroopsRuined[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.TownLabels.Length; idx++)
				{
					char[] townLabel = new char[13];

					for (int jdx = 0; jdx < 13; jdx++)
					{
						townLabel[jdx] = (char)source.PullUInt8OrZero();
					}
					this.Owner.World.TownLabels[idx] = new string(townLabel);
				}

				this.Owner.World.RerunContentSpan = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.RerunContent.Length; idx++)
				{
					this.Owner.World.RerunContent[idx] = source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.MarvelTownIdx.Length; idx++)
				{
					this.Owner.World.MarvelTownIdx[idx] = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						this.Owner.World.Participants[idx].MissingTroops[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						this.Owner.World.Participants[idx].ScienceObtainedSince[jdx] = (sbyte)source.PullUInt8OrZero();
					}
				}

				this.Owner.World.DirtyCellTally = source.PullInt16OrZero();
				this.Owner.World.SmogImpactTier = source.PullInt16OrZero();
				this.Owner.World.WorldHeatingTally = source.PullInt16OrZero();
				this.Owner.World.SessionPrefMarkers = source.PullInt16OrZero();
				PullContent(source, ReplayTurnsAddress, ReplayTurnsSize);
				this.Owner.World.HighestScienceTally = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ComingScienceTally = source.PullInt16OrZero();
				this.Owner.World.DiagMarkers = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ResearchLevyRatio = source.PullInt16OrZero();
				}
				
				this.Owner.World.FollowingCollectionRound = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].RunningSagaStanding = source.PullInt16OrZero();
				}

				PullContent(source, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				this.Owner.World.StarshipMarkers = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipVictoryRatio = source.PullInt16OrZero();
				this.Owner.World.BotStarshipVictoryRatio = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipArrivalSeason = source.PullInt16OrZero();
				}

				PullContent(source, PalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				PullContent(source, PalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int idx = 0; idx < this.Owner.World.TownPlaces.Length; idx++)
				{
					this.Owner.World.TownPlaces[idx].X = (sbyte)source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.TownPlaces.Length; idx++)
				{
					this.Owner.World.TownPlaces[idx].Y = (sbyte)source.PullUInt8OrZero();
				}

				this.Owner.World.CastleTier = source.PullInt16OrZero();
				this.Owner.World.TruceRoundTally = source.PullInt16OrZero();
				this.Owner.World.BotRivalTally = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipInhabitants = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipLiftoffSeason = source.PullInt16OrZero();
				}

				this.Owner.World.ParticipantIdentMarkers = source.PullInt16OrZero();

				source.Close();

				// Hit points of the units, a saved game from the original game has none
				string vitalityRoute = Path.Combine(this.Processor.StandardFolder, $"{docname}.HP");
				bool vitalityFetched = false;

				if (File.Exists(vitalityRoute))
				{
					using (FileStream vitalitySource = new FileStream(vitalityRoute, FileMode.Open, FileAccess.Read))
					{
						vitalityFetched = this.Owner.World.Vitals.Import(vitalitySource);
					}
				}

				if (!vitalityFetched)
				{
					this.Owner.World.Vitals.WipeAll();
				}

				victory = true;
			}
			catch (Exception extra)
			{
				Message(extra.Message)
					.Dialog(100, 80);

				victory = false;
			}

			return victory;
		}

		/// <inheritdoc/>
		public bool SaveGameContent(string route)
		{
			route = route.ToUpper();

			using var _ = this.Processor.Journal.Scope($"StoreGameContent('{route}')");

			bool victory = false;
			string docname = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainDOSFileLabel(route));

			try
			{
				// write map file
				this.Owner.VideoCard.Displays[2].Store(Path.Combine(this.Processor.StandardFolder, $"{docname}.MAP"), false);

				// write sve file
				FileStream sink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.SVE"), FileMode.Create);
				sink.EmitInt16(this.Owner.World.RoundTally);
				sink.EmitInt16(this.Owner.World.PersonParticipantIdx);
				sink.EmitInt16(this.Owner.World.ParticipantMarkers);
				sink.EmitUInt16(this.Owner.World.ChanceRoot);
				sink.EmitInt16(this.Owner.World.Season);
				sink.EmitInt16(this.Owner.World.HardnessTier);
				sink.EmitInt16(this.Owner.World.LiveRealms);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].PresentInquiryIdx);
				EmitContent(sink, LeaderNameAt(0), 0x70);
				EmitContent(sink, CivilizationPluralAt(0), 0x60);
				EmitContent(sink, CivilizationNameAt(0), 0x58);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Gold);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].InquiryHeadway);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].LiveTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].TroopsInsideMaking[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].FoundScienceTally);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						sink.EmitUInt16(this.Owner.World.Participants[idx].FoundScienceMarkers[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].RegimeKind);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Approach);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						sink.EmitUInt16(this.Owner.World.Participants[idx].Relations[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TownTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TroopTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].SoilTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ColonistTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].GrandTownExtent);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].MartialMight);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Standing);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].LevyRatio);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Tally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TouchParticipantTimeleft);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].HorizBegin);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ChiefVisuals);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Assault);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Guard);
					}
				}
				
				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].TownTally);
					}
				}

				EmitContent(sink, ExploredMapSegment, ExploredMapSize);

				for (int idx = 0; idx < 16; idx++)
				{
					sink.EmitInt16(this.Owner.World.Landmasses[idx].ConstructionSpotTally);
				}

				EmitContent(sink, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int idx = 0; idx < this.Owner.World.TrucePlotContent.Length; idx++)
				{
					sink.WriteByte(this.Owner.World.TrucePlotContent[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.Towns.Length; idx++)
				{
					this.Owner.World.Towns[idx].WriteTo(sink);
				}

				EmitContent(sink, CityNamesStatusAddress, CityNamesStatusSize);

				EmitContent(sink, UnitMemory.Origin, UnitsBufferSize);

				for (int idx = 0; idx < this.Owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.ChartSight.GetLength(1); jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.ChartSight[idx, jdx]);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Live);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte(this.Owner.World.Participants[idx].TacticalSites[jdx].Strategy);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Place.X);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Place.Y);
					}
				}

				for (int idx = 0; idx < this.Owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.ScienceLeadingFoundVia[idx]);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].TroopsRuined[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.TownLabels.Length; idx++)
				{
					for (int jdx = 0; jdx < 13; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.TownLabels[idx][jdx]);
					}
				}

				sink.EmitInt16(this.Owner.World.RerunContentSpan);
				for (int idx = 0; idx < this.Owner.World.RerunContent.Length; idx++)
				{
					sink.WriteByte(this.Owner.World.RerunContent[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.MarvelTownIdx.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.MarvelTownIdx[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].MissingTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						sink.WriteByte((byte)((sbyte)this.Owner.World.Participants[idx].ScienceObtainedSince[jdx]));
					}
				}

				sink.EmitInt16(this.Owner.World.DirtyCellTally);
				sink.EmitInt16(this.Owner.World.SmogImpactTier);
				sink.EmitInt16(this.Owner.World.WorldHeatingTally);
				sink.EmitInt16(this.Owner.World.SessionPrefMarkers);
				EmitContent(sink, ReplayTurnsAddress, ReplayTurnsSize);
				sink.EmitInt16(this.Owner.World.HighestScienceTally);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ComingScienceTally);
				sink.EmitInt16(this.Owner.World.DiagMarkers);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ResearchLevyRatio);
				}
				
				sink.EmitInt16(this.Owner.World.FollowingCollectionRound);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].RunningSagaStanding);
				}

				EmitContent(sink, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				sink.EmitInt16(this.Owner.World.StarshipMarkers);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipVictoryRatio);
				sink.EmitInt16(this.Owner.World.BotStarshipVictoryRatio);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipArrivalSeason);
				}

				EmitContent(sink, PalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				EmitContent(sink, PalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[idx].X));
				}
				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[idx].Y));
				}

				sink.EmitInt16(this.Owner.World.CastleTier);
				sink.EmitInt16(this.Owner.World.TruceRoundTally);
				sink.EmitInt16(this.Owner.World.BotRivalTally);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipInhabitants);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipLiftoffSeason);
				}

				sink.EmitInt16(this.Owner.World.ParticipantIdentMarkers);

				sink.Close();

				// Hit points of the units, in a separate file so the original game can still load the saved game
				using (FileStream vitalitySink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.HP"), FileMode.Create))
				{
					this.Owner.World.Vitals.Store(vitalitySink);
				}

				victory = true;
			}
			catch (Exception extra)
			{
				this.Owner.Words.Set(extra.Message);
				this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 4, 64, 1);

				victory = false;
			}

			return victory;
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

		/// <summary>
		/// Sets the save file name (0x41c6, drive letter, colon, file name) to an autosave.
		/// "AUTO00" has the same length as "CIVIL0", so the rest of the name stays in place.
		/// </summary>
		private void AssignAutosaveFileLabel(int bay)
		{
			string label = AutosaveLabel(bay);

			for (int idx = 0; idx < label.Length; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(SaveFileNamePatternAddress + idx), (byte)label[idx]);
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
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(SaveFileNamePatternAddress + idx), (byte)label[idx]);
			}
		}

		/// <summary>
		/// Shows the list of saved games on two tabs, the manual saves and the autosaves (newest first).
		/// Tab, Left and Right switch the tab, the first line of the list does the same.
		/// The drive must already be chosen (AskForSaveDrive) and the mouse pointer hidden, as before the original list.
		/// </summary>
		/// <returns>Manual saved game (0 - 9), AutosaveSlotFlag + autosave (0 - 19), or 0xffff when cancelled</returns>
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

				// Title, tab line and the saved games, centered on the screen
				strokeTally = bays.Count + 2;
				int vertLoc = Math.Max(4, (100 - (strokeTally * 8 + 8) / 2) & 0xfffe);

				this.Owner.Startup.ShowMouseCursor();

				// Start on the first saved game, not on the tab line
				Menu.DefaultLine = 0x1;
				this.Owner.Dialogs.ChoicesIndentTokens = true;
				this.Owner.Dialogs.ChoicesIndentTokenDown = false;

				ushort option = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 48, vertLoc, 1);

				this.Owner.Dialogs.ChoicesIndentTokens = false;

				this.Owner.Startup.HideMouseCursor();

				if (option == 0 || (option == 0xffff && this.Owner.Dialogs.ChoicesIndentTokenDown))
				{
					// Switch the tab
					this.displayBackups = !this.displayBackups;
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);
					continue;
				}

				if (option == 0xffff)
					return 0xffff;

				if (option <= bays.Count && bays[option - 1] != 0xffff)
					return (ushort)bays[option - 1];

				// An empty slot was chosen, show the list again
			}
		}

		/// <summary>The letter of the drive (0x4384) is written at the end of the text at TextBuffer.Address, place: (ushort)(TextBuffer.Address - 1) the last character</summary>
		private void AppendDriveLetter(ushort place)
		{
			this.Processor.EmitGlobalByte((ushort)(this.Owner.Words.Length + place), (byte)((byte)Files.SaveDrive + 0x41));
		}

		/// <summary>Reads span bytes of the file into the data segment at the address</summary>
		private void PullContent(Stream source, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(this.Processor.SegData.Value16, stagingAddr), span);
		}

		/// <summary>Reads span bytes of the file into the segment at the address</summary>
		private void PullContent(Stream source, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
		}

		/// <summary>Bytes of a file into the memory, also into the ranges kept in C# arrays (B6: the units)</summary>
		private void ReadIntoMemory(Stream source, uint location, ushort span)
		{
			byte[] content = new byte[span];
			source.ReadExactly(content, 0, span);
			this.Processor.Storage.EmitChunk(location, content, 0, span);
		}

		/// <summary>Bytes of the memory into a file, also of the ranges kept in C# arrays</summary>
		private void WriteFromMemory(Stream sink, uint location, ushort span)
		{
			byte[] content = new byte[span];
			this.Processor.Storage.ReadChunk(location, content, 0, span);
			sink.Write(content, 0, span);
		}

		/// <summary>Writes span bytes of the data segment at the address into the file</summary>
		private void EmitContent(Stream sink, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(this.Processor.SegData.Value16, stagingAddr), span);
		}

		/// <summary>Writes span bytes of the segment at the address into the file</summary>
		private void EmitContent(Stream sink, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
		}
	}
}
