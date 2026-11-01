namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of WorldEvents that the other parts of the game use</summary>
	public interface IWorldEvents
	{
		/// <summary>
		/// Units heal lost hit points every turn (HealingRules): 2 in an own city; outside cities 1 for a fortified land unit
		/// and for a resting ship (sentry or not moved this turn) next to land, at most 3 tiles from an own city.
		/// Runs at the end of the game turn, so the moves left (reset at the start of each player's turn) show whether the
		/// unit moved. Reads the memory without journal entries or registers, like the rest of the hit points (host only).
		/// </summary>
		void MendFortifiedUnits();

		/// <summary>
		/// Shows the text as a dialog (ShowMenu) and puts back the screen as it was before (kept in the back screen 0x19d4);
		/// the answer of ShowMenu given back (and left in AX)
		/// </summary>
		ushort ShowDialog(ushort charsAddr, int horizLoc, int vertLoc);

		/// <summary>
		/// One turn of the game: every living player in order (the cities, peace treaties, the AI, the human player's turn;
		/// after a loaded game 0xdf60 the players before the human player are skipped), the end of turn events, the year, the
		/// autosave, every 50 turns the status and the ranking into the replay, the chronicle, the scores of the wonders and
		/// the pollution, the palace, and the end of the game (a spaceship arrived, conquest, the year of retirement).
		/// The frame like the original: HandleCity (through HandleAllCities) reads the stack below it, and the ranking of the
		/// replay is made in the frame (bp - 0x14).
		/// </summary>
		void HandleGameEnd();

		/// <summary>
		/// The end of the game: the civilization graph, the replay (not when the game was quit) and, without the landing of the
		/// player, the results, the final score and the Hall of Fame
		/// </summary>
		void ShowEndGameSequence();

		/// <summary>The title picture of the end of the game, until a key or a click</summary>
		void ShowTitleScreen();

		/// <summary>
		/// Barbarians, from turn 30 * (5 - level): every 8th turn, while nobody knows technology 58, a landing ship (0x11, from
		/// turn 300 0x12) on an ocean tile nobody owns of a sea of at least 16 tiles with 1 - 3 units (3, with technology 56: 7)
		/// and a leader (0x1a), all on board (flag 1). Then a random city of the human player is attacked with a chance of its
		/// plunder %: up to 0x63 tries for a free land tile nobody owns, at least 6 from its nearest city and on its continent
		/// (the nearest city becomes the target; the occupation test asks for the player in the never written local 0x2 of the
		/// frame, like in the original); a human city the player sees gets the travellers' report, then size / 2 (2 - 99) units
		/// (4 / 9 or 6 / 10 with technology 35) and a leader land there, seen like the tile. The final values of the locals
		/// stay in the stack frame, like in the original.
		/// </summary>
		void ShowTravellersReport();

		/// <summary>
		/// The power of every player 1 - 7 (gold / 32 + 8 * citizens + technologies + the price of all units, 0 for a destroyed
		/// civilization): before turn 600 it goes (/ 8, at most 255) into the power graph (segment 0x3772) and the players' peace
		/// treaties into the peace graph (TrucePlotContent), both at 8 * (turn / 4 % 150) + player. The players are ranked by
		/// power (Standing 7 - 1). A human player first in power, with more than 4 cities, no spaceship part (unit 25) and after
		/// turn 200 is feared (0x8078): the others with more than one city make peace, or with a treaty get 0x100.
		/// </summary>
		void RefreshPeaceGraphContent();

		/// <summary>Clears the top line and draws the menu bar (Game, Orders, Advisors, World, Civilopedia)</summary>
		void ClearStatusLine();

		/// <summary>
		/// The status panel (not on the world map, 0xb278 = 1): peace bar, palace, free memory (with 0xd806), population, year,
		/// research bulb (progress * 4 / (cost + 1), 0 - 3), pollution sun, gold and the tax / luxury / science rates; then, while
		/// 0x1c28 is 0, tells when the population passes the next mark (0x1c24: tens below 100, then hundreds + 9). The stack
		/// frame, the saved SI, the return address pushed for ObtainFreeStorage and SI = the new mark before ShowDialog stay
		/// like in the original (ShowDialog leaves a value made from SI in DI).
		/// </summary>
		void DrawStatusBar();

		/// <summary>
		/// The small palace of a player with cities, on a line at (horizLoc, vertLoc): the human player's from its palace
		/// (levels 0xd91e, styles 0xb222), an AI player's built from castleTier + 3 parts (3 - 36) by a generator seeded with
		/// player * ChanceRoot (levels up to 3 and parts / 5, a section only next to a higher one, its style player % 3). Each built
		/// section is drawn from the pictures at 0xd21c by its style, level and place (ends and middle).
		/// </summary>
		void DrawPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc);

		/// <summary>
		/// Score needed for the palace level: the sum of level * (7 - difficulty) for the levels 0 up to the given one, halved,
		/// plus 1
		/// </summary>
		/// <returns>Score, also in AX</returns>
		ushort ObtainPalaceScoreForLevel(short castleTier);

		/// <summary>
		/// Gets current year as a String, appends buffer at TextBuffer.Address
		/// </summary>
		void FormatCurrentYear();

		/// <summary>
		/// The end of a turn: the pollution level SmogImpactTier moves one step towards 2 * DirtyCellTally - 4 * WorldHeatingTally
		/// (0 - 99; the newspaper warns at 12 with more than 6 polluted tiles, above 16 global warming comes). Then up to level + 1
		/// tries for a wonder 1 - 21 built by a rival: on the easiest level only a wonder the human player could build, on the
		/// two easiest the one a human city builds becomes 0x11 (the Manhattan Project with technology 0x2e, also in the first try
		/// when the rival has 0x2e and the human player not, unless built), not stronger rivals than him for 0x11 once he has
		/// 0x2e; a city of size above cost / 10 of a player at peace with the barbarians and with the wonder's technology builds
		/// it with up to a third of the treasury (announced unless 0xb278 is 1), the human cities building it get a new
		/// production. The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		void HandleEndOfTurnEvents();

		/// <summary>
		/// Draws the main screen again: the map view, the box of the left column and the civilization panels, with the colour
		/// cycling stopped meanwhile (0x1c28 = 1 while drawing)
		/// </summary>
		void RepaintMainScreen();

		/// <summary>Fills the box with the motif and draws its shadow</summary>
		void FillBoxWithShadow(int horizLoc, int vertLoc, int breadth, int altitude);

		/// <summary>
		/// When 0xd762 is set: loads the palette of the human player's government, "backNa.pal" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" with technology 20), and the palette 0x1e56, and puts colours 5 on. The file name is
		/// made in a buffer in the stack frame, like in the original.
		/// </summary>
		void ShowCivilizationScenery();

		/// <summary>The level of the palace for the rating: how many of the steps 5, 10, 15, ... it takes, less one</summary>
		short ObtainPalaceLevel(short rating);
	}
}
