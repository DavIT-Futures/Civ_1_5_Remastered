namespace Civilization1Remaster.Core.Engine
{
	public interface ISaveGameStore
	{
		ushort FetchBay { get; set; }

		bool FetchingBackup { get; set; }

		void PerformBackup();

		ushort ChooseGameToImport();

		ushort LoadGameFromSlot(ushort bay);

		ushort LoadGameFromMenu(ushort marker);

		ushort LoadGame(ushort docnameAddr, ushort marker);

		void ShowStoreGameDialog(ushort slot);

		ushort SaveGame(ushort docnameAddr);

		ushort AskForSaveDrive();

		ushort CheckDrive(ushort driveIdx);

		bool LoadGameContent(string route);

		bool SaveGameContent(string route);
	}
}
