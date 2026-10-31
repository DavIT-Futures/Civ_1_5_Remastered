using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of FameRecords that the other parts of the game use</summary>
	public interface IFameRecords
	{
		/// <summary>
		/// Empties the 6 entries of the Hall of Fame (records of 0x2e bytes at 0x3772:37d2, the first word -1)
		/// </summary>
		void ClearHallOfFame();

		/// <summary>
		/// Wipes the Hall of Fame and reads it from fame.dta (0x114 bytes, through the buffer TextBuffer.Address) into 0x3772:0x37d2
		/// </summary>
		void LoadHallOfFame();

		/// <summary>
		/// Writes the Hall of Fame (0x114 bytes at 0x3772:0x37d2, through the buffer TextBuffer.Address) to fame.dta
		/// </summary>
		void SaveHallOfFame();

		/// <summary>
		/// The Hall of Fame (records of 0x2e bytes at 0x3772:0x37d2): places 1 - 5 (the 6th instead of the 5th when newPlace is 5),
		/// each with the leader, title, civilization, years, population, score and rating; the place newPlace is highlighted, and
		/// unless newPlace is -1 "C" may be pressed (the hint at the bottom).
		/// </summary>
		/// <returns>1 after the key C (c), else 0, also in AX</returns>
		ushort ShowHallOfFame(ushort newPlace);

		/// <summary>
		/// Puts the human player's game into the Hall of Fame (6 records of 0x2e bytes at 0x3772:0x37d2, ordered by score 0xb77e
		/// times difficulty + 2; a game not better than the first 5 takes the last place), moving the worse records down, and
		/// shows it. When ShowHallOfFame returns non-zero the Hall of Fame is wiped and the game put in again.
		/// </summary>
		/// <returns>The place of the game (also in AX)</returns>
		ushort AttachHallOfFameRecord();

		/// <summary>
		/// The ranking of the ruler among the 20 titles of 0x36d4 (0x19 bytes each, from the last): rank rankGiven, or with a score
		/// percentage score * (difficulty + 1) / 50 the highest k - 1 (k = 1 - 20) with k * k / 3 below it ("...% exceeds even");
		/// nothing for a negative rank. The titles up to the rank are listed upwards, the rank highlighted, until a key or a click.
		/// </summary>
		void ShowGameResultsScreen(ushort rankGiven, ushort score);

		/// <summary>
		/// The 5 top cities (after HandleCity of each: size + happy - unhappy citizens, + 10 per wonder), best first, each in a box
		/// of its player's colour with its citizens and wonders, until a key or a click. The list (scores at BP - 0x20, cities at
		/// BP - 0x10) stays in the stack frame like in the original, and DrawCitizenIcons, which reads what is left below the
		/// stack, is called at the same depth.
		/// </summary>
		void ShowTopCities();
	}
}
