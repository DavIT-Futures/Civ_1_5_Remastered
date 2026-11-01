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

		ushort LoadGame(ushort documentNameAddress, ushort marker);

		void ShowStoreGameDialog(ushort slot);

		ushort SaveGame(ushort documentNameAddress);

		ushort AskForSaveDrive();

		ushort CheckDrive(ushort driveIndex);

		bool LoadGameContent(string route);

		bool SaveGameContent(string route);
	}
}
