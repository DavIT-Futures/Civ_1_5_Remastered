using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of UnitRoster that the other parts of the game use</summary>
	public interface IUnitRoster
	{
		/// <summary>
		/// Puts the units kept in a city of the human player (KeepUnitsInCityList) back on the map, fortified
		/// </summary>
		void ConstructUnitsFromCityList(short townIdx);

		/// <summary>
		/// A city of the human player keeps up to two of its fortified home units standing in it: they are taken off the map and
		/// their types (with the veteran bit) are stored in City.Unfamiliar. The first such unit stays on the map.
		/// Losses are not counted meanwhile (0x70d8).
		/// </summary>
		void KeepUnitsInCityList(short townIdx);

		/// <summary>
		/// A unit has come onto the tile. The human player sees the tiles around it (2 tiles far by the sight of the unit type),
		/// cities next to it give back their kept units, and the unit meets the foreign units around: they see each other, wake up,
		/// lose their "go to" orders, a peace treaty may be offered and the AI players remember the places (SendUnitToStrategicLocation).
		/// </summary>
		/// <param name="marker">Not 0: repaint the tiles around when the human player watches</param>
		void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker);

		/// <summary>
		/// Blinks the active unit on the map and its dot on the world map, and the cursor frame on a map tile, until a key is
		/// pressed or the mouse is clicked. A unit index from 128 blinks a text instead (no unit).
		/// </summary>
		/// <param name="cursorHoriz">Map position of the cursor frame, 0xffff for none</param>
		/// <param name="cursorVert">Map position of the cursor frame</param>
		void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert);

		/// <summary>
		/// Creates a unit of the type on the tile in the first free record (0 - 126). Its home city is the nearest city when that
		/// city belongs to the player. When there is no free record, the human player is told once.
		/// </summary>
		/// <param name="kind">Unit type</param>
		/// <returns>Index of the new unit, or 0xffff</returns>
		ushort ConstructUnit(short participantIdx, short kind, int horizLoc, int vertLoc);

		/// <summary>
		/// Removes the unit. A transport sinking on the ocean takes the land units of its stack along, a carrier its planes.
		/// The tile is repainted when the human player saw the unit.
		/// </summary>
		void EliminateUnit(short participantIdx, short troopIdx);

		/// <summary>
		/// The best defender of the tile of the unit: the stack of the first unit of the player standing there is searched with
		/// LocateBestGuardianStep. A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		ushort LocateStackLeader(short participantIdx, short troopIdx);

		/// <summary>
		/// The best defender in the stack of the unit (LocateBestGuardianStep). A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		ushort LocateBestGuardianInStack(short participantIdx, short troopIdx);

		/// <summary>
		/// One unit of the stack in the search for its best defender (the guardian score by kind, terrain, fortification,
		/// veteran)
		/// </summary>
		void LocateBestGuardianStep(short participantIdx, short troopIdx);

		/// <summary>
		/// Sum over the stack of the unit, the mode chooses what (TallyUnitsByDomainStep: 0 price, 1 defence, 2 units,
		/// 3 attack, 4 defensive units before the unit). Leaves SI = 0x600 * player like the translated original,
		/// ComputerPlayer depends on it (see MIGRATION-BACKLOG.md).
		/// </summary>
		ushort TallyUnitsByDomainInStack(short participantIdx, short troopIdx, ushort mode);

		/// <summary>
		/// Step of TallyUnitsByDomainInStack, adds to the total in 0x6532 what the mode in 0x6536 asks for:
		/// 0 the price, 1 the defence, 2 one per unit, 3 the attack, 4 one per defensive unit with an index below 0x6538.
		/// </summary>
		void TallyUnitsByDomainStep(short participantIdx, short troopIdx);

		/// <summary>
		/// Number of the units of the type in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		ushort TallyUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort kind);

		/// <summary>
		/// Step of TallyUnitsOfTypeInStack: counts in 0x653c the units of the type in 0x653a
		/// </summary>
		void TallyUnitsOfTypeStep(short participantIdx, short troopIdx);

		/// <summary>
		/// Number of the units with the TroopGroup in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		ushort TallyUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort group);

		/// <summary>
		/// Step of TallyUnitsWithFlagInStack: counts in 0x653c the units whose TroopGroup is the one in 0x653a
		/// </summary>
		void TallyUnitsWithFlagStep(short participantIdx, short troopIdx);

		/// <summary>
		/// The free room on the transports of the stack of the unit (TallySpecialUnitsStep).
		/// Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		ushort TallySpecialUnitsInStack(short participantIdx, short troopIdx);

		/// <summary>
		/// Step of TallySpecialUnitsInStack, the free room on the transports in 0x653c: a land unit takes one place,
		/// a transport adds its room (FerryRoom)
		/// </summary>
		void TallySpecialUnitsStep(short participantIdx, short troopIdx);

		/// <summary>
		/// Calls a step function for every unit of the stack of the unit, at most for 10 units. The step function is given by
		/// its address in the original code.
		/// </summary>
		void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr);

		/// <summary>
		/// Calls the action for every unit of the stack of the unit, starting with it, at most for 10 units.
		/// The following unit is read before the action, so the action may remove the unit from the stack.
		/// </summary>
		void ForEachUnitInStack(short participantIdx, short troopIdx, Action<short, short> visit);

		/// <summary>
		/// Stack step: wakes the unit (clears Sentry), except a land unit carried on the ocean
		/// </summary>
		void VerifyUnitTerrainFlag(short participantIdx, short troopIdx);

		/// <summary>
		/// Stack step: a sleeping (Sentry) unit wakes up with full moves (and full fuel for a plane); every unit loses its
		/// "go to" order
		/// </summary>
		void WipeUnitOrders(short participantIdx, short troopIdx);

		/// <summary>
		/// Stack step: RestoreUnitStatus for the ships of the stack
		/// </summary>
		void RestoreUnitOrders(short participantIdx, short troopIdx);

		/// <summary>
		/// Gives the unit back to the player without orders: settler work stops, a sleeping, fortified or working unit gets
		/// its full moves, only the Veteran (and 0x10) status stays, the "go to" order is removed
		/// </summary>
		void RestoreUnitStatus(short participantIdx, short troopIdx);

		/// <summary>
		/// Stack step: removes the unit when it is a land unit (cargo of a sinking transport)
		/// </summary>
		void EliminateLandUnit(short participantIdx, short troopIdx);

		/// <summary>
		/// Stack step: removes the unit when it is a plane or a ship
		/// </summary>
		void EliminateNonLandUnit(short participantIdx, short troopIdx);

		/// <summary>
		/// Stack step: removes the unit when it is a plane (LandGroup 1, despite the name: the planes of a sinking carrier)
		/// </summary>
		void EliminateSeaUnit(short participantIdx, short troopIdx);

		/// <summary>
		/// Scrolls the map view (left column 0xd4cc, top row 0xd75e) so that the tile is in its inner part
		/// (columns 2 - 13, rows 2 - 9 of the view); otherwise the view stays
		/// </summary>
		void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Is the move onto the tile blocked by the zone of control: never into a city, otherwise VerifyTileOccupation
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile</returns>
		ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Does a unit of another player stand on one of the 8 tiles around, on the same kind of tile (land or ocean).
		/// In the turn of the human player that unit, and its city, become visible to the human.
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile, 0 otherwise</returns>
		ushort VerifyTileOccupation(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>1 when a unit of another player stands on one of the 8 tiles around (the zone of control), else 0</summary>
		ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// A unit enters a hut (not a barbarian, not a plane). DrawInt(4) chooses:
		/// 0: an advanced tribe founds a city for the player, when no city is 4 tiles near and the hut layer says so; 50 gold
		/// otherwise, mercenaries near a city;
		/// 1: scrolls of ancient wisdom (a technology) after the first turn up to the year 1000, 50 gold otherwise;
		/// 2: 50 gold;
		/// 3: barbarians around the unit when no city is 4 tiles near and the player has a city, mercenaries otherwise.
		/// </summary>
		void VisitHut(short participantIdx, short troopIdx);

		/// <summary>
		/// The attack animation: the unit slides one tile towards the attacked tile in 17 frames, over the next unit of its
		/// stack. Only when the map view shows both tiles; with 0xb278 = 1 the tile of the unit is only drawn.
		/// </summary>
		/// <param name="direction">Direction of the attack (1 - 8)</param>
		void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort direction);

		/// <summary>
		/// The list of the units of the stack (at most 12): the human player wakes up units (the cursor keys and Enter or Space,
		/// or a click) and picks the one to go on with. Woken units lose their orders and a settler at work gets its full moves.
		/// </summary>
		/// <returns>The picked unit, the stack leader (LocateStackLeader) when none; also in AX</returns>
		ushort DisplayBattleScreen(short participantIdx, short troopIdx);

		/// <summary>
		/// The direction (1 - 8) of the step of the unit towards the nearest own city or carrier (a plane flying home). Tiles with
		/// foreign units and foreign cities are avoided, 0 when no step gets nearer.
		/// </summary>
		/// <returns>Direction, also in AX</returns>
		ushort LocateNearestFriendlyCity(short participantIdx, short troopIdx);

		/// <summary>An entry of the replay with 1 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		void AttachReplayContent(ushort rerunIdx, byte content1);

		/// <summary>An entry of the replay with 2 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2);

		/// <summary>An entry of the replay with 3 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3);

		/// <summary>An entry of the replay with 4 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4);

		/// <summary>
		/// Draws the border of the screen (320 x 200) from pieces of 8 x 8 pixels of the picture 0x19e8 (from row 120), in one of
		/// two styles (DrawInt(2)): the four corners, the left and right edges, then the top and bottom edges
		/// </summary>
		void RenderBusyIndicator();
	}
}
