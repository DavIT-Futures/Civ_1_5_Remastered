
namespace Civilization1Remaster.Core.UI
{
	/// <summary>The operations of HelpPopups that the other parts of the game use</summary>
	public interface IHelpPopups
	{
		/// <summary>
		/// Shows the help text for a topic from HELP.TXT (0x34b2): prefixes the uppercase topic with '*' in the stack frame, draws the
		/// text on a blue background when found and restores the screen after a key press or mouse click
		/// </summary>
		void ShowHelpTopic(ushort charsAddr);

		/// <summary>
		/// Hints for the unit's tile: a good place for a city (food + shields of the terrain at least 2, map layer 2 value at
		/// least 10 and not lower than around it) shows 0x34bb; else, on a tile next to (distance 1, 0x6c9a) a city of the
		/// player: irrigation for terrain 4 (0x34c6) or a coastal terrain 1 (0x34ce) without irrigation or mine, or a mine
		/// (0x34d8) where the terrain allows it (0x28e) and the player has less than 2 settlers
		/// </summary>
		void ShowTileInfo(short participantIdx, ushort troopIdx);

		/// <summary>
		/// With the help option on (SessionPrefMarkers bit 0) and only before year 0 (Season below 0): a help window with the
		/// text from the help file (0x34de) for the topic, its codes replaced (ReflowText), 6 pixels per line, until a key or
		/// click. The screen is kept in the back screen 0x19d4 meanwhile. Always: whatever the option and the year (the help
		/// key of the remaster).
		/// </summary>
		void ShowHelpPopup(GameText topic, bool always = false);

		/// <summary>
		/// For the human player only (0x6b90): a help window with the text from the help file (0x3501) for the topic, its codes
		/// replaced (ReflowText), 6 pixels per line, until a key or click. The screen is kept in the back screen 0x19d4 meanwhile.
		/// </summary>
		void ShowHelpPopupForHuman(GameText topic);
	}
}
