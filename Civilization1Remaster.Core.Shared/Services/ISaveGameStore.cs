namespace Civilization1Remaster.Core.Shared.Services
{
	public interface ISaveGameStore
	{
		public const ushort FetchSessionAsk = 3;

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
