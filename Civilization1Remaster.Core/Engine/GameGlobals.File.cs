namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The saved games and the disks (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class FileVariables : VariableGroup
		{
			private readonly GlobalWord saveDrive = new GlobalWord(0x4384);
			private readonly GlobalWord firstMissingDisk = new GlobalWord(0xdeba);
			private readonly GlobalWord lastSaveSlot = new GlobalWord(0xe168);
			private readonly GlobalWord midTurnSave = new GlobalWord(0xdf60);
			private readonly GlobalWord diskInUse = new GlobalWord(0xe17c);
			private readonly GlobalWord fileError = new GlobalWord(0x681c);
			private readonly GlobalWord savedHumanPlayer = new GlobalWord(0x6822);
			private readonly GlobalWord savedDifficulty = new GlobalWord(0x6824);
			private readonly GlobalWord savedYear = new GlobalWord(0x6826);
			private readonly GlobalWord loadingGame = new GlobalWord(0x681a);
			private readonly GlobalWord loadFile = new GlobalWord(0x681e);
			private readonly GlobalWord loadedGameMark = new GlobalWord(0x3484);
			private readonly GlobalWord importPlayerIndex = new GlobalWord(0x6820);

			/// <summary>0x4384: the letter of the drive of the saved games</summary>
			public ushort SaveDrive { get => this.saveDrive.Value; set => this.saveDrive.Value = value; }

			/// <summary>0xdeba: the first disk of the game that is missing</summary>
			public ushort FirstMissingDisk { get => this.firstMissingDisk.Value; set => this.firstMissingDisk.Value = value; }

			/// <summary>0xe168: the save slot used last</summary>
			public ushort LastSaveSlot { get => this.lastSaveSlot.Value; set => this.lastSaveSlot.Value = value; }

			/// <summary>0xdf60: the game continues in the middle of the turn (2 an autosave, 1 a manual save)</summary>
			public ushort MidTurnSave { get => this.midTurnSave.Value; set => this.midTurnSave.Value = value; }

			/// <summary>0xe17c: the disk of the game in use</summary>
			public ushort DiskInUse { get => this.diskInUse.Value; set => this.diskInUse.Value = value; }

			/// <summary>0x681c: the error of the last file operation</summary>
			public ushort FileError { get => this.fileError.Value; set => this.fileError.Value = value; }

			/// <summary>0x6822: the human player of the saved game</summary>
			public ushort SavedHumanPlayer { get => this.savedHumanPlayer.Value; set => this.savedHumanPlayer.Value = value; }

			/// <summary>0x6824: the difficulty of the saved game</summary>
			public ushort SavedDifficulty { get => this.savedDifficulty.Value; set => this.savedDifficulty.Value = value; }

			/// <summary>0x6826: the year of the saved game</summary>
			public ushort SavedYear { get => this.savedYear.Value; set => this.savedYear.Value = value; }

			/// <summary>The addresses the start screen reads the saved games into</summary>
			public ushort SavedHumanPlayerAddress => this.savedHumanPlayer.Address;
			public ushort SavedYearAddress => this.savedYear.Address;
			public ushort SavedDifficultyAddress => this.savedDifficulty.Address;

			/// <summary>0x681a: the game is loaded, not saved ("Which drive contains your saved game files?")</summary>
			public ushort LoadingGame { get => this.loadingGame.Value; set => this.loadingGame.Value = value; }

			/// <summary>0x681e: the handle of the file of the game loaded</summary>
			public ushort LoadFile { get => this.loadFile.Value; set => this.loadFile.Value = value; }

			/// <summary>0x3484: a game was just loaded (0xfffd, then 0xfffe in the first score of the turn, then 0)</summary>
			public ushort LoadedGameMark { get => this.loadedGameMark.Value; set => this.loadedGameMark.Value = value; }

			/// <summary>0x6820: loop counter of player index during game import</summary>
			public ushort ImportPlayerIndex { get => this.importPlayerIndex.Value; set => this.importPlayerIndex.Value = value; }
		}
	}
}
