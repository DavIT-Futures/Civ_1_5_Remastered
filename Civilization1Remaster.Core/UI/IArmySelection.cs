using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of ArmySelection that the other parts of the game use</summary>
	public interface IArmySelection
	{
		/// <summary>
		/// Asks which civilization's army to show (the names at 0x1982 + 2 * player, players 1 - 7) and shows its list;
		/// the screen is kept in the back screen 0x19d4 and put back
		/// </summary>
		void DisplayWhichArmy();

		/// <summary>
		/// The strategic map of a player (diagnostics), each step after a key: the tiles it sees (all with 0xd806) as 4 x 4
		/// boxes (sea 1, land 2), cities and the owners of the tiles in their colours; then its strategic locations (S, A, D, N by
		/// the role) and its tactical sites; then its strategy on each continent (S, D, A, T) with defence / attack.
		/// </summary>
		void DisplayArmyList(short participantIdx);

		/// <summary>
		/// A window about the unit: civilization, number, unit type, home city, position, the target of its "go to" order, then
		/// the types of the other units of its stack (following the ring, while the text is shorter than 512)
		/// </summary>
		void DisplayUnitInfo(short participantIdx, short troopIdx);
	}
}
