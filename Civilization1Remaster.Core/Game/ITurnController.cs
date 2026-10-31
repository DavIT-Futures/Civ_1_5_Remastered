
namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of TurnController that the other parts of the game use</summary>
	public interface ITurnController
	{
		/// <summary>
		/// The turn of a player: passes over his units (another pass while some unit waited or was off the view), each unit
		/// gets its orders (from ShiftAIUnit, its standing orders or the keys and clicks of the human) and carries them out
		/// or moves; the human gets a last look without a unit at the end of the turn. The locals (TurnLocals) are shared by
		/// the methods it calls, as in the original; the frame (0x52) stays so that the called functions run at the same
		/// depth of the stack.
		/// </summary>
		void HandlePlayerTurn(short participantIdx);

		/// <summary>
		/// The remaster's railroads (RailroadRules): a step along a road from a tile with a railroad (or a city, once the player
		/// knows Railroad) is free for every unit; a unit not moved by hand (GoTo or a computer player) rides for free at most
		/// 30 times a turn, counted here.
		/// </summary>
		bool RideForFree(short participantIdx, short troopIdx, short horizLoc, short vertLoc);

		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		void RepaintTileIfVisible(int horizLoc, int vertLoc);

		/// <summary>
		/// Repaints the tile of the unit when the human player sees it (own unit, or the unit is shown to the human)
		/// </summary>
		void RepaintUnitIfVisible(short participantIdx, short troopIdx);

		/// <summary>
		/// 0 on a tile with a city, irrigation or a mine (improvement bits 0 - 2). Otherwise from the terrain table at 0x44a
		/// (12 bytes a terrain): 2 when the word at +4 is -3 or less, 1 when the word at +0 is -2 and the tile is at the coast
		/// (CheckCoastalTile), else 0.
		/// </summary>
		ushort ObtainTileMoveCost(int horizLoc, int vertLoc);

		/// <summary>
		/// Whether the tile is on the coast: it is terrain 11 itself, or one of the 4 tiles next to it in a straight line
		/// (headings 1, 3, 5, 7; the column is not wrapped) is terrain 10 or 11 or has improvement bit 1 - but tiles with a city
		/// do not count
		/// </summary>
		/// <returns>1 on the coast, else 0 (also in AX)</returns>
		ushort CheckCoastalTile(int horizLoc, int vertLoc);

		/// <summary>
		/// The panel of the active unit (left, under the world map): the civilization, the type (Veteran), the moves (with the
		/// fuel of aircraft), the home city, the terrain and its improvements, then the units of the city on the tile (the two
		/// supported at 0x1c * city ... Unfamiliar, drawn through the unit 127) and the stack (up to row 0xb8, "more" after).
		/// Unit 0x80: no unit.
		/// </summary>
		void DrawUnitInfo(short participantIdx, ushort troopIdx);

		/// <summary>1 when the place is inside the map view (15 x 12 tiles, also across the edge of the map), else 0</summary>
		ushort CheckPositionOnScreen(int horizLoc, int vertLoc);

		/// <summary>
		/// Throws away the keys waiting and waits until the mouse buttons are released
		/// </summary>
		void WaitForKey();

		/// <summary>
		/// The next unit to activate in the stack of the unit: following the ring (at most 16 steps), the non-ship unit with the
		/// smallest index after the unit, counting round past 127
		/// </summary>
		/// <returns>That unit, the unit itself when it stands alone or nothing comes (also in AX)</returns>
		ushort FindBestGuardian(short participantIdx, ushort troopIdx);

		/// <summary>The help popup of the topic, only in the turn of the human player</summary>
		void ShowHelpForHuman(GameText topic);
	}
}
