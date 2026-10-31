using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of IntroSequence that the other parts of the game use</summary>
	public interface IIntroSequence
	{
		/// <summary>
		/// The intro of the game: finds the disks and the graphics driver, reserves the screens, starts the sound and the
		/// timer, then scrolls the credits (credits.txt) and the planet in time with the music, shows the title logo and
		/// takes the choice of the main menu from the key pressed during the intro (0x6b32: 0 - 3, 0xffff without a choice)
		/// </summary>
		void ShowMainIntro();

		/// <summary>
		/// The end of the space race: the ship of the player (the human player or another one) lands on Alpha Centauri. The
		/// pictures of the flying ship come from 0x32d8, the text (file 0x3301, part 0x32f2 for the human player, else 0x32f9)
		/// scrolls in parts with the ship, then the landing (or the palace of another player) is shown with the closing text,
		/// line after line, and a key closes the screen
		/// </summary>
		void ShowCivilizationScreen(short participantIdx);

		/// <summary>
		/// The opening news of a new game: the picture 0x332e and, with the human player's leader name in capitals (code text
		/// 0x30b8) and the last technology the player knows ("Irrigation" without any, code text 0x30be), the text 0x3342 of
		/// file 0x3348 shown part after part ("^" ends a part; the last part is not shown), each in a band at row 8 (colour 14
		/// with a shadow when 0xd762 is set, else 9), waiting 200 between parts unless a part ends with "!" or a key is waiting;
		/// then the leader's sound and the screen of the civilization
		/// </summary>
		void ShowTerrainTutorial();

		/// <summary>
		/// The tutorial of the screen: points at the 8 tiles around the centre of the map window (0xd4cc + 7, 0xd75e + 6) with
		/// the names of their terrain (of the special resource, or "Village"), then at the menu bar, the map window and the
		/// active unit, and waits for a key or a click. SI holds 2 * direction at the pointers, like in the original: ShowMenu
		/// leaves a value made from it in DI, which goes back to the caller.
		/// </summary>
		void ShowInterfaceTutorial();

		/// <summary>
		/// A tutorial hint: a line (colour 15) from the pointed place to the text, then the text at TextBuffer.Address as a dialog
		/// </summary>
		void DrawTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2);
	}
}
