using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of NewGameSetup that the other parts of the game use</summary>
	public interface INewGameSetup
	{
		/// <summary>
		/// The choices of a new game and its start: the difficulty (also by a mouse click on a portrait), the number of
		/// civilizations (3 - 7, the living players 0xff >> (6 - rivals)), the tribe (the first leaders of the playing slots,
		/// then the second ones; the human player gets that slot and leader, or a random slot for none), the world wiped
		/// (cities, wonders, leaders, spaceships, what the players saw), every civilization made (ConstructCivilization), the
		/// human player's rates 5 / 5 (and 50 gold on the easiest level), and the welcome text with the technologies the human
		/// player starts with.
		/// </summary>
		void ConfigureFreshGame();

		/// <summary>
		/// Makes the civilization of the player anew (at the start of a game, for a new civilization in a free slot, or after a
		/// civil war; for the human player only at the start): everything of the player is wiped (units, relations, cities,
		/// money; government 1, rates 4 / 4; in a running game it gets the technologies the human player knows and that were
		/// first found by a third player, each with chance 1/2). A living player other than 0 then gets a leader (a free one
		/// of its colour at the start, else the other leader of its colour), a start place and its names, a settler there, the
		/// barbarian units within 8 tiles removed. The start place is random on land: not too close to cities and earlier
		/// players, on good land (screen 2 value), on a continent with room for cities and without cities of others after the
		/// start of the game, not on a hut; the demands get lower with the attempts (0x34), after 2000 attempts it takes the
		/// last place. EARTH (0xd76a) uses the start place of the leader at the start. A new civilization in a running game
		/// with no place, or too late for the difficulty, does not come: the player is dead (0, and the game ends 0xdc48 when
		/// only the human player is left).
		/// </summary>
		/// <returns>1 when made (also when it was not made anew), 0 when it does not come; also in AX</returns>
		ushort ConstructCivilization(short participantIdx);

		/// <summary>
		/// When the player (not 0, not the human player) has no city left: tells that the civilization was destroyed by
		/// destroyerIdx (dialog style 0x2f9e: 3 when by the human player, else 5 with relations bit 0x40 of the human player
		/// towards it, else 2), takes away its spaceship, units and everything it saw, notes it in the replay and makes a new
		/// civilization in its place
		/// </summary>
		void EliminateCivilization(short participantIdx, short participantIdx1);

		/// <summary>
		/// Rates the start of every player 1 - 7 by his first unit (settlers): the nearest civilization on the same continent
		/// (close under 10, moderate under 20 +1, far away +2, isolated +4), river valley (more than one river tile +2) or
		/// grasslands (3 grassland tiles +1), and the continent size (/ 50: 2 and 3 +1, more +2). The players behind the best
		/// get compensation: more than 3 points behind another settlers (+3), then a technology per point (UncoverTechnology).
		/// Returns the first player with the best start, or the last of them not too close to another civilization. The
		/// descriptions in TextBuffer.Address are never shown. The tables of the continents and the points are in the stack frame and SI / DI
		/// get the values of the original before the calls, like in the original.
		/// </summary>
		ushort CharacterizeStartLocation();

		/// <summary>
		/// Despite the name: cuts the sprites of the game out of the sprite sheets into picture tables (the order of the
		/// ScreenToPicture calls gives the picture numbers, like in the original). The three tribe portraits (0xdf62); with VGA
		/// colours (0xd762) the unit sprites of 0x3709 (10 rows of 16, 0xb886; rows 6 - 9, and all rows with 0x1a3e, only
		/// column 0 cut and copied to the others), the quarter tiles (0xd294) and 4 tiles (0xd2d4), else the EGA unit sprites
		/// of 0x3714 (12 in two rows); with VGA its palette part (0x30 bytes to 0x6b34). Then the terrain and map pieces of
		/// sheet 0x36ff: tiles, rivers, specials, city walls, numbers, cursors and the 8 coloured rows of city sprites (0xd54e),
		/// coloured per player with the colours 0x1946 / 0x1956.
		/// </summary>
		void DisplayTribeSelection();

		/// <summary>
		/// Loads the government picture of the human player, "govtNa.pic": N is the government / 2 (3 for government 3), "m"
		/// instead of "a" with technology 20; the leader part of it goes into the picture 0x19e8. The file name is made in a
		/// buffer in the stack frame, like in the original.
		/// </summary>
		void ImportGovernmentGraphics();

		/// <summary>
		/// After a revolution of the human player (not to government 0): the newspaper "... government changed to ...!", then
		/// the advisor screen with the title 0x375a and the 4 advisors (pictures from 0x19e8, names at 0x2fac without their
		/// last character, every second one lower) until a key or click
		/// </summary>
		void DisplayRevolutionScreen();

		/// <summary>
		/// The player has no spaceship: no arrival, every place of the spaceship layout (15 rows of 12, segment 0x3772) empty
		/// </summary>
		void WipeSpaceshipContent(short participantIdx);
	}
}
