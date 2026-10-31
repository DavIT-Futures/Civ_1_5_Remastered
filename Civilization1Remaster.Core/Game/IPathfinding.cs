using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of Pathfinding that the other parts of the game use</summary>
	public interface IPathfinding
	{
		/// <summary>
		/// The direction (1 - 8) of a unit's next step towards its goal (0x81da / 0x81db), -1 when it is there (the goal is
		/// cleared), 0 when the human player's goal next to the unit is the unit's own tile. The human player's unit next to its
		/// goal steps there; an air unit heads straight for it; others first look in the 16 x 16 window (ShiftToward) or follow
		/// the long way (ChartRoute) to 0x6590 / 0x6592. Then the neighbour that costs least: distance to 0x6590 / 0x6592 * 4
		/// plus its move cost (1 along a road, 3 for units of one move, else 3 * the terrain's cost) plus the square of the turn
		/// from the last direction (0x81dc); only tiles the unit may enter (own or free, of its kind or an own city, not small
		/// lakes), for the human player only not farther away. Going straight back stops the unit's moves. The locals of the
		/// frame are written as in the original.
		/// </summary>
		ushort CarryOutGoto(short participantIdx, short troopIdx);

		/// <summary>
		/// The next step of a unit's long way (0x6590 / 0x6592): from the block of 4 x 4 tiles of its goal (LocateRouteStep), a
		/// search in width over the linked blocks (land or, for a ship, sea; steps in BlockStepsAddress, the queue;
		/// columns wrap at 20) until the unit's block. Found: the neighbour of the unit's block with the fewest steps
		/// (the nearest to the goal among equal ones), its tile (1, 1) or the next one of the right kind (land or sea), else
		/// the goal itself. Returns 1 when found, 0 when not, and the goal row when the unit's block has no step (like the
		/// original's AX). The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		ushort ChartRoute(short participantIdx, short troopIdx);

		/// <summary>
		/// The next block of 4 x 4 tiles on the way to the place (land blocks, marker 0, or sea blocks, marker 1;
		/// 13 bytes a column): the block of the place itself when it is marked, else the marked neighbouring block (directions
		/// 1 - 8) nearest to the place that has a corner tile of the right kind (ocean for the marker 1) with a route
		/// (ComputeRouteCost) to the place. The block goes to 0x6590 / 0x6592.
		/// </summary>
		/// <returns>1 when a block was found, else 0, also in AX</returns>
		ushort LocateRouteStep(int horizLoc, int vertLoc, ushort marker);

		/// <summary>
		/// The unit's next step towards the place 0x6590 / 0x6592 inside the 16 x 16 window around it: the move costs from the
		/// place outwards (16 * column + row of the window, the queue; 1 on a road from a road, 3 for an
		/// air unit, else 3 * the terrain's cost; tiles of the other kind than the unit only with a city), kept while the
		/// place stays the same (0x6796 / 0x6798) and the unit's tile is reached; 0x6794 is the cost to the unit. Below the
		/// limit costLimit, the direction to the neighbour with the lowest cost (equal: fewer own units there * 4 + distance to the
		/// place), else -1 and the unit's goal becomes the place. The locals of the frame are written as in the original.
		/// </summary>
		ushort ShiftToward(short participantIdx, short troopIdx, ushort costLimit);

		/// <summary>
		/// The move cost from (horizLoc, vertLoc) to (horizSpot1, vertSpot1) within 7 tiles: the unit 127 of the barbarians (a
		/// land unit, a ship with the marker) at the start, ShiftToward up to the cost limit; 0xffff without a route or farther
		/// away
		/// </summary>
		ushort ComputeRouteCost(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort marker, ushort costLimit);

		/// <summary>
		/// Adjust X position
		/// </summary>
		/// <param name="xPos"></param>
		/// <returns></returns>
		int TunePosition(int horizLoc);
	}
}
