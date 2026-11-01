namespace Civilization1Remaster.Core.Engine
{
	/// <summary>The operations of SaveGameStore that the other parts of the game use</summary>
	public interface ISaveGameStore
	{
		/// <summary>
		/// Saved game chosen with Load Game in the GAME menu, 0xffff when none
		/// </summary>
		ushort FetchBay { get; set; }

		/// <summary>An autosave is being loaded (LoadGame continues it like the original autosaves)</summary>
		bool FetchingBackup { get; set; }

		/// <summary>
		/// Saves the game into the next autosave, called once every turn
		/// </summary>
		void PerformBackup();

		/// <summary>
		/// Shows the list of saved games for Load Game in the GAME menu, without loading anything.
		/// The screen is restored afterwards.
		/// </summary>
		/// <returns>The chosen saved game (see ShowLoadList), or 0xffff when cancelled</returns>
		ushort ChooseGameToImport();

		/// <summary>
		/// Loads a saved game chosen with ShowLoadList, from the same drive
		/// </summary>
		/// <param name="slot">Manual saved game (0 - 9) or AutosaveSlotFlag + autosave (0 - 19)</param>
		/// <returns>1 when the game was loaded</returns>
		ushort LoadGameFromSlot(ushort bay);

		/// <summary>
		/// Load a Saved Game from the start menu
		/// </summary>
		/// <param name="flag">0xffff to choose the saved game from the list, otherwise the manual saved game to load</param>
		/// <returns>The loaded game (see ShowLoadList), or 0xffff when nothing was loaded</returns>
		ushort LoadGameFromMenu(ushort marker);

		/// <summary>
		/// Marker 0: loads the saved game (the extension of the file name becomes SVE), the continents are counted again and
		/// the game continues in the middle of the turn (0xdf60: 2 for an autosave, 1 for a manual save). Otherwise appends the
		/// line of the file to the list of saved games at TextBuffer.Address: " (EMPTY)", or the difficulty, the leader and the civilization
		/// of the human player and the year.
		/// </summary>
		/// <returns>1, 0 for a missing file in the list, also in AX</returns>
		ushort LoadGame(ushort docnameAddr, ushort marker);

		/// <summary>
		/// Save Game: the drive, the list of the 10 save files (the slot -1 asks the player, starting at the last one, 0xe168),
		/// the game stored into the chosen file and the message whether it was saved (with write access denied, 0xd, or disk
		/// full, 0x1c, from the error 0x681c).
		/// </summary>
		void ShowStoreGameDialog(ushort slot);

		/// <summary>
		/// Saves the game into the file: first a message with the file name, the difficulty (0x33a2 + 2 * level), the leader
		/// (0x19a2 + 2 * player), the civilization (0x1992 + 2 * player) and the year, "... save in progress."
		/// </summary>
		/// <returns>1, also in AX</returns>
		ushort SaveGame(ushort docnameAddr);

		/// <summary>
		/// Asks for the drive of the saved games (0x4384, the current drive the first time): a letter A - F chooses it, Return
		/// takes it, Escape cancels. A floppy drive without a disk gives the message "No Disk in Drive ...". The drive letter
		/// goes into the file name at 0x41c6.
		/// </summary>
		/// <returns>The drive (0 = A), or 0xffff when cancelled or without a disk, also in AX</returns>
		ushort AskForSaveDrive();

		/// <summary>
		/// Whether there is a disk in a floppy drive (0 = A, 1 = B; other drives are always ready): the BIOS verifies one sector
		/// (head 0, track 0, sector 2), up to 3 times with a reset of the disk system after each failure. The request is in the
		/// stack frame, like in the original.
		/// </summary>
		/// <returns>1 when the drive is ready, else 0 (also in AX)</returns>
		ushort CheckDrive(ushort driveIdx);

		/// <summary>
		/// Loads the saved game of the path: the map (.MAP into screen 2) and the state (.SVE); false when a file cannot be
		/// read
		/// </summary>
		bool LoadGameContent(string route);

		/// <summary>
		/// Saves the game as the path: the map (screen 2 into .MAP) and the state (.SVE); false when a file cannot be written
		/// </summary>
		bool SaveGameContent(string route);
	}
}
