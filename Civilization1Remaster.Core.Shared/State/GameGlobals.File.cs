namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
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

			public ushort SaveDrive { get => this.saveDrive.Value; set => this.saveDrive.Value = value; }

			public ushort FirstMissingDisk { get => this.firstMissingDisk.Value; set => this.firstMissingDisk.Value = value; }

			public ushort LastSaveSlot { get => this.lastSaveSlot.Value; set => this.lastSaveSlot.Value = value; }

			public ushort MidTurnSave { get => this.midTurnSave.Value; set => this.midTurnSave.Value = value; }

			public ushort DiskInUse { get => this.diskInUse.Value; set => this.diskInUse.Value = value; }

			public ushort FileError { get => this.fileError.Value; set => this.fileError.Value = value; }

			public ushort SavedHumanPlayer { get => this.savedHumanPlayer.Value; set => this.savedHumanPlayer.Value = value; }

			public ushort SavedDifficulty { get => this.savedDifficulty.Value; set => this.savedDifficulty.Value = value; }

			public ushort SavedYear { get => this.savedYear.Value; set => this.savedYear.Value = value; }

			public ushort SavedHumanPlayerAddress => this.savedHumanPlayer.Address;
			public ushort SavedYearAddress => this.savedYear.Address;
			public ushort SavedDifficultyAddress => this.savedDifficulty.Address;

			public ushort LoadingGame { get => this.loadingGame.Value; set => this.loadingGame.Value = value; }

			public ushort LoadFile { get => this.loadFile.Value; set => this.loadFile.Value = value; }

			public ushort LoadedGameMark { get => this.loadedGameMark.Value; set => this.loadedGameMark.Value = value; }

			public ushort ImportPlayerIndex { get => this.importPlayerIndex.Value; set => this.importPlayerIndex.Value = value; }
		}
	}
}
