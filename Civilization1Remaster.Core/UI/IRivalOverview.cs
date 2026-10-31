using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of RivalOverview that the other parts of the game use</summary>
	public interface IRivalOverview
	{
		/// <summary>
		/// The overview of a civilization (diagnostics): its name and picture, government, population, treasury and rates, the
		/// counts of units / cities / settlers / land, the strategy on each continent with an attack value, the relations with
		/// the others and its technologies, three a row (white the ones it found first), until the screen is full; after a key
		/// or a click the screen is put back.
		/// </summary>
		void ShowCivilizationOverview(short participantIdx);

		/// <summary>
		/// The table of units: a row for every unit type some player 1 - 7 has or builds, a column for every player 0 - 7 (its
		/// picture, and its standing with 0xd806); the human player's column (all columns with 0xd806) shows "active/in
		/// production" of each row. After a key or a click, the table of units destroyed.
		/// </summary>
		void ShowCivilizationScoreList();

		/// <summary>
		/// The table of units destroyed between the players 1 - 7 (TroopsRuined of the row's player against the column's / the
		/// other way round), each player in its colour (0x1946) with its picture (0xd4ce); only the human's row and column
		/// unless all units are shown (0xd806). Until a key or click, with the screen kept in the back screen 0x19d4.
		/// </summary>
		void DrawScoreListRecord();
	}
}
