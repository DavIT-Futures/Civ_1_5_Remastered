using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>The operations of GameStartup that the other parts of the game use</summary>
	public interface IGameStartup
	{
		/// <summary>
		/// Main game entry: the intro, the check of the free memory (a warning below 8000 paragraphs with VGA, 4000 without;
		/// 0x1a3e set below 9150 with VGA), the mouse cursor, the main menu and the new world, then the turns of the game
		/// until it ends (a game loaded during play starts the turns again)
		/// </summary>
		void RunGame();

		/// <summary>
		/// Loads the game chosen with Load Game in the GAME menu, after the main loop has left the turn.
		/// It does the same steps as Load a Saved Game in the start menu.
		/// </summary>
		/// <param name="pointerLevel">Mouse pointer show/hide level at the start of the game</param>
		void ImportGameDuringPlay(ushort arrowTier);

		/// <summary>
		/// The mouse: GlobDb3a the clicks since the last call and the buttons held, GlobDb3c / GlobDb3e the place; all 0
		/// without a mouse. The interrupts are served first.
		/// </summary>
		void ObtainMouseState();

		/// <summary>
		/// Raises the show level of the mouse pointer (0xdeea); the pointer is drawn when the level reaches 1 and there is a
		/// mouse (0x1a3c)
		/// </summary>
		void DisplayMouseCursor();

		/// <summary>
		/// Hides the mouse pointer one level more (PointerShowLevel); it is blanked when it goes from shown, level 1
		/// </summary>
		void ConcealMouseCursor();

		/// <summary>
		/// Remembers the show level of the mouse pointer (0xdeea) in 0x652e and shows the pointer until the level is at least 1
		/// </summary>
		void DisplayMouseCursorFully();

		/// <summary>
		/// Hides the mouse pointer back to the show level remembered by DisplayMouseCursorFully (0x652e)
		/// </summary>
		void ConcealMouseCursorToSavedLevel();

		/// <summary>
		/// Makes sure the file that marks game disk "diskIdx" (path at 0x1ace + 2 * disk) is there, asking "Please insert
		/// Disk ..." until it is; the disk in use goes to 0xe17c. Marker 0: disk 0 instead when the disk is missing, without
		/// asking; marker 0xffff: ask once even when it is there (then marker 1). The message is made in a buffer in the stack
		/// frame, like in the original.
		/// </summary>
		/// <returns>1 when found at once with marker 0, else the marker (also left in AX)</returns>
		ushort QueryForDisk(ushort diskIdx, ushort marker);

		/// <summary>
		/// The main menu after the title (the choice in 0x6b32, asked when it is -1): 0 = a new game (all world options 1,
		/// the world is made with the story), 1 = a saved game, 2 = EARTH (0xd76a = 1), 3 = a new game with the world options
		/// asked (land mass, temperature, climate, age in 0x7ef6 - 0x7efc), 4 = the Hall of Fame and the menu again. The choice
		/// is played at a measure of the title music. Then the tribe is chosen and the game screen prepared.
		/// </summary>
		void TitleLogoAndMainChoices();

		/// <summary>
		/// The choices of a new game (only before the first turn, after the picture 0x1c16) and the government pictures
		/// </summary>
		void FreshWorldChoices();
	}
}
