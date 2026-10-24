using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2e31.
	/// </summary>
	public class Pathfinding : GameModule
	{
		public Pathfinding(GameEngine owner)
			: base(owner)
		{
		}

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
		public ushort CarryOutGoto(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"CarryOutGoto({participantIdx}, {troopIdx})");

			this.processor.EnterFrame(0x2a);
			this.processor.PreserveIndexRegisters();

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			ushort direction;

			if (participantIdx != this.owner.World.PersonParticipantIdx || !StepToNearGoal(troop, troopIdx, out direction))
				direction = StepTowardsGoal(participantIdx, troopIdx, troop);

			this.processor.RegAcc.Value16 = direction;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("CarryOutGoto");

			return direction;
		}

		/// <summary>The human player's unit next to its goal: the direction there (0 for its own tile); false when not near</summary>
		private bool StepToNearGoal(ushort troop, short troopIdx, out ushort direction)
		{
			this.processor.RegSource.Value16 = troop;

			short shiftHoriz = troopIdx < 128 ? (short)(this.processor.PullGlobalByte((ushort)(troop + 0x81da)) - this.processor.PullGlobalByte((ushort)(troop + 0x81d5))) : (short)0;
			short shiftVert = troopIdx < 128 ? (short)(this.processor.PullGlobalByte((ushort)(troop + 0x81db)) - this.processor.PullGlobalByte((ushort)(troop + 0x81d6))) : (short)0;

			this.processor.EmitLocalWord(0x1e, (ushort)shiftHoriz);
			this.processor.EmitLocalWord(0x20, (ushort)shiftVert);

			direction = 0;

			if (Math.Abs(shiftHoriz) > 1 || Math.Abs(shiftVert) > 1)
				return false;

			shiftHoriz = (short)Math.Sign(shiftHoriz);
			shiftVert = (short)Math.Sign(shiftVert);

			this.processor.EmitLocalWord(0x1e, (ushort)shiftHoriz);
			this.processor.EmitLocalWord(0x20, (ushort)shiftVert);

			if (troopIdx < 128)
			{
				this.processor.RegSource.Value16 = (ushort)(troop - (0xc * troopIdx));
				this.processor.EmitGlobalByte((ushort)(troop + 0x81da), 0xff);
			}

			ushort candidate;

			for (candidate = 1; candidate <= 8; candidate++)
			{
				this.processor.RegSource.Value16 = (ushort)(candidate << 1);

				if ((short)this.processor.PullGlobalWord((ushort)((candidate << 1) + 0x1882)) == shiftHoriz &&
					(short)this.processor.PullGlobalWord((ushort)((candidate << 1) + 0x18e4)) == shiftVert)
				{
					direction = candidate;
					break;
				}
			}

			this.processor.EmitLocalWord(0x18, candidate);

			return true;
		}

		/// <summary>Where the unit heads (0x6590 / 0x6592) and its step there (see CarryOutGoto)</summary>
		private ushort StepTowardsGoal(short participantIdx, short troopIdx, ushort troop)
		{
			this.processor.RegSource.Value16 = troop;

			ushort kind = (ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)));

			if (this.processor.PullGlobalWord((ushort)(kind + 0x1138)) == 1)
			{
				GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
				GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));
			}
			else
			{
				this.processor.EmitLocalWord(0xe, 0);

				bool windowFailed = false;
				short columns = (short)Math.Abs((short)(this.processor.PullGlobalByte((ushort)(troop + 0x81da)) - this.processor.PullGlobalByte((ushort)(troop + 0x81d5))));

				if (columns < 7 || columns > 0x49)
				{
					if (Math.Abs((short)(this.processor.PullGlobalByte((ushort)(troop + 0x81db)) - this.processor.PullGlobalByte((ushort)(troop + 0x81d6)))) < 7)
					{
						GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
						GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));

						if (TryShiftToward(participantIdx, troopIdx, out ushort step))
							return step;

						this.processor.EmitLocalWord(0xe, 1);
						windowFailed = true;
					}
				}

				if (ChartRoute(participantIdx, troopIdx) != 0 || !windowFailed)
				{
					if (TryShiftToward(participantIdx, troopIdx, out ushort step))
						return step;
				}
			}

			return ChooseGotoStep(participantIdx, troopIdx, troop);
		}

		/// <summary>ShiftToward with the limit 0x3e7 (into the local 0x14); true with a direction</summary>
		private bool TryShiftToward(short participantIdx, short troopIdx, out ushort step)
		{
			step = ShiftToward(participantIdx, troopIdx, 0x3e7);
			this.processor.EmitLocalWord(0x14, step);

			return step != 0xffff;
		}

		/// <summary>The distance measure of CarryOutGoto: the larger of |x| and |y| plus both</summary>
		private static short GotoDistance(short shiftHoriz, short shiftVert)
		{
			short horiz = (short)Math.Abs(shiftHoriz), vert = (short)Math.Abs(shiftVert);

			return (short)((horiz > vert ? horiz : vert) + horiz + vert);
		}

		/// <summary>The cheapest neighbour towards 0x6590 / 0x6592 (see CarryOutGoto)</summary>
		private ushort ChooseGotoStep(short participantIdx, short troopIdx, ushort troop)
		{
			this.processor.RegSource.Value16 = troop;

			short unitHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81d5));
			short unitVert = this.processor.PullGlobalByte((ushort)(troop + 0x81d6));
			short shiftHoriz = (short)(GoalHoriz - unitHoriz);
			short shiftVert = (short)(GoalVert - unitVert);

			this.processor.EmitLocalWord(0x1e, (ushort)shiftHoriz);
			this.processor.EmitLocalWord(0x20, (ushort)shiftVert);
			this.processor.EmitLocalWord(0x26, (ushort)Math.Abs(shiftHoriz));
			this.processor.EmitLocalWord(0x28, (ushort)Math.Abs(shiftVert));

			short distance = GotoDistance(shiftHoriz, shiftVert);
			this.processor.EmitLocalWord(0xa, (ushort)distance);

			if (shiftHoriz == 0 && shiftVert == 0)
			{
				this.processor.EmitGlobalByte((ushort)(troop + 0x81dc), 0xff);
				this.processor.EmitGlobalByte((ushort)(troop + 0x81da), 0xff);
				this.processor.EmitGlobalByte((ushort)(troop + 0x81d8), 0);

				return 0xffff;
			}

			short lowest = 0x270f;
			short best = 0;

			this.processor.EmitLocalWord(0x24, (ushort)lowest);
			this.processor.EmitLocalWord(0x14, 0);

			ushort road = (ushort)(this.owner.MapOps.ObtainTileImprovements((sbyte)unitHoriz, (sbyte)unitVert) & 8);
			this.processor.EmitLocalWord(0xc, road);

			ushort occupied = this.owner.UnitOps.MayOccupyTile(participantIdx, (sbyte)unitHoriz, (sbyte)unitVert);
			this.processor.EmitLocalWord(0x1c, occupied);

			ushort direction;

			for (direction = 1; direction < 9; direction++)
			{
				this.processor.EmitLocalWord(0x18, direction);

				short stepHoriz = (short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882));
				short stepVert = (short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4));
				short nextHoriz = (short)(this.processor.PullGlobalByte((ushort)(troop + 0x81d5)) + stepHoriz);
				short nextVert = (short)(this.processor.PullGlobalByte((ushort)(troop + 0x81d6)) + stepVert);
				short leftHoriz = (short)(shiftHoriz - stepHoriz);
				short leftVert = (short)(shiftVert - stepVert);

				this.processor.RegSource.Value16 = (ushort)(direction << 1);
				this.processor.RegDest.Value16 = troop;
				this.processor.EmitLocalWord(0x10, (ushort)nextHoriz);
				this.processor.EmitLocalWord(0x12, (ushort)nextVert);
				this.processor.EmitLocalWord(0x2, (ushort)leftHoriz);
				this.processor.EmitLocalWord(0x4, (ushort)leftVert);
				this.processor.EmitLocalWord(0x28, (ushort)Math.Abs(leftVert));
				this.processor.EmitLocalWord(0x26, (ushort)Math.Abs(leftHoriz));
				this.processor.EmitLocalWord(0x2a, (ushort)Math.Abs(leftVert));

				short nextDistance = GotoDistance(leftHoriz, leftVert);
				this.processor.EmitLocalWord(0x16, (ushort)nextDistance);

				if (participantIdx == this.owner.World.PersonParticipantIdx && nextDistance > distance)
					continue;

				if (!MayGotoTile(participantIdx, troopIdx, troop, nextHoriz, nextVert, occupied, out ushort terrain))
					continue;

				short cost;

				if (road != 0 && (this.owner.MapOps.ObtainTileImprovements(nextHoriz, nextVert) & 8) != 0)
				{
					cost = 1;
				}
				else
				{
					ushort unitKind = (ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)));

					cost = (short)this.processor.PullGlobalWord((ushort)(unitKind + 0x113a)) <= 1 ? (short)3 :
						(short)(3 * (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0x13 * terrain) + 0x28e)));
				}

				cost += (short)((nextDistance << 2) + Math.Abs(leftHoriz) + Math.Abs(leftVert));

				byte last = this.processor.PullGlobalByte((ushort)(troop + 0x81dc));
				this.processor.EmitLocalByte(0x2a, last);

				if (last != 0xff)
				{
					short turn = (short)Math.Abs((short)((sbyte)last - direction));

					if (turn > 4)
						turn = (short)(8 - turn);

					this.processor.EmitLocalWord(0x6, (ushort)turn);

					cost += (short)(turn * turn);
				}

				this.processor.EmitLocalWord(0x8, (ushort)cost);

				if (cost < lowest)
				{
					best = (short)direction;
					lowest = cost;

					this.processor.EmitLocalWord(0x14, (ushort)best);
					this.processor.EmitLocalWord(0x24, (ushort)lowest);
				}
			}

			this.processor.EmitLocalWord(0x18, direction);

			this.processor.RegSource.Value16 = troop;

			byte previous = this.processor.PullGlobalByte((ushort)(troop + 0x81dc));

			// Straight back: the unit stops for this turn
			if (previous != 0xff && (ushort)((ushort)(sbyte)previous ^ 4) == (ushort)best)
			{
				this.processor.EmitGlobalByte((ushort)(troop + 0x81d8), 0);
				best = 0;
				this.processor.EmitLocalWord(0x14, 0);
			}

			if (best == 0)
			{
				this.processor.EmitGlobalByte((ushort)(troop + 0x81dc), 0xff);
				this.processor.EmitGlobalByte((ushort)(troop + 0x81da), 0xff);
				this.processor.EmitLocalWord(0x14, 0xffff);

				return 0xffff;
			}

			this.processor.EmitGlobalByte((ushort)(troop + 0x81dc), (byte)best);

			return (ushort)best;
		}

		/// <summary>
		/// Whether the unit may step on the tile: own or free tiles of its kind it may occupy (or of an air unit), an own city,
		/// not an ocean of less than 5 tiles; the terrain for the cost (see CarryOutGoto)
		/// </summary>
		private bool MayGotoTile(short participantIdx, short troopIdx, ushort troop, short horizLoc, short vertLoc, ushort occupied, out ushort terrain)
		{
			terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x1a, terrain);

			ushort ownerIdx = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x22, ownerIdx);

			bool allowed = false;

			if (ownerIdx == 0xffff || ownerIdx == (ushort)participantIdx)
			{
				ushort kind = (ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)));
				this.processor.RegSource.Value16 = terrain == 0xa ? (ushort)1 : (ushort)0;

				if ((this.processor.PullGlobalWord((ushort)(kind + 0x1138)) == 2) == (terrain == 0xa) &&
					(occupied == 0 || this.owner.UnitOps.MayOccupyTile(participantIdx, horizLoc, vertLoc) == 0))
				{
					allowed = true;
				}
				else
				{
					this.processor.RegSource.Value16 = (ushort)(0x600 * participantIdx);

					if (this.processor.PullGlobalWord((ushort)((ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7))) + 0x1138)) == 1)
						allowed = true;
				}
			}

			if (!allowed)
			{
				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0 ||
					this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) != (ushort)participantIdx)
					return false;
			}

			if (terrain == 0xa && (short)this.owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 5)
				return false;

			return true;
		}

		/// <summary>
		/// The next step of a unit's long way (0x6590 / 0x6592): from the block of 4 x 4 tiles of its goal (LocateRouteStep), a
		/// search in width over the linked blocks (land 0xdb44 or, for a ship, sea 0x7f38; steps in 0xd816, queue 0x6594 /
		/// 0x6694, columns wrap at 20) until the unit's block. Found: the neighbour of the unit's block with the fewest steps
		/// (the nearest to the goal among equal ones), its tile (1, 1) or the next one of the right kind (land or sea), else
		/// the goal itself. Returns 1 when found, 0 when not, and the goal row when the unit's block has no step (like the
		/// original's AX). The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		public ushort ChartRoute(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ChartRoute({participantIdx}, {troopIdx})");

			this.processor.EnterFrame(0x3a);
			this.processor.PreserveIndexRegisters();

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			ushort unitHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81d5));
			ushort unitVert = this.processor.PullGlobalByte((ushort)(troop + 0x81d6));

			this.processor.EmitLocalWord(0x36, unitHoriz);
			this.processor.EmitLocalWord(0x3a, unitVert);
			GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
			GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));

			ushort sea = UnitMovesBySea(troop) ? (ushort)1 : (ushort)0;

			ushort step = LocateRouteStep((short)unitHoriz, (short)unitVert, sea);

			ushort result;

			if (step == 0)
			{
				GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
				GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));

				// The goal row is still in AX in the original
				result = this.processor.PullGlobalByte((ushort)(troop + 0x81db));
			}
			else
			{
				short unitColumn = (short)GoalHoriz, unitRow = (short)GoalVert;

				this.processor.EmitLocalWord(0x36, (ushort)unitColumn);
				this.processor.EmitLocalWord(0x3a, (ushort)unitRow);

				LocateRouteStep(this.processor.PullGlobalByte((ushort)(troop + 0x81da)), this.processor.PullGlobalByte((ushort)(troop + 0x81db)),
					UnitMovesBySea(troop) ? (ushort)1 : (ushort)0);

				result = SearchRouteBlocks(troop, unitColumn, unitRow) ? (ushort)1 : (ushort)0;
			}

			this.processor.RegAcc.Value16 = result;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChartRoute");

			return result;
		}

		/// <summary>Whether the unit (offset from 0x81d4) is of a sea kind (domain 2, 0x1138 + 0x22 * kind)</summary>
		private bool UnitMovesBySea(ushort troop)
		{
			return this.processor.PullGlobalWord((ushort)((ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7))) + 0x1138)) == 2;
		}

		/// <summary>The links of the block (the bits of directions 1 - 8): sea blocks 0x7f38 or land blocks 0xdb44</summary>
		private short RouteBlockLinks(bool sea, short column, short row)
		{
			return (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * column) + row + (sea ? 0x7f38 : 0xdb44)));
		}

		/// <summary>The column of the block in the direction, wrapping at 20</summary>
		private short RouteNeighbourColumn(short direction, short column)
		{
			short nextColumn = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + column);

			if (nextColumn == 0x14)
				nextColumn = 0;

			if (nextColumn == -1)
				nextColumn = 0x13;

			return nextColumn;
		}

		/// <summary>
		/// The search in width from the goal's block (0x6590 / 0x6592) to the unit's block and the step there (see ChartRoute);
		/// true when the unit's block was reached
		/// </summary>
		private bool SearchRouteBlocks(ushort troop, short unitColumn, short unitRow)
		{
			this.owner.Libc.FillBytes(0xd816, 0, 0x104);

			byte tail = 0, head = 0;

			this.processor.EmitGlobalByte((ushort)(tail + 0x6594), (byte)GoalHoriz);
			this.processor.EmitGlobalByte((ushort)(tail + 0x6694), (byte)GoalVert);
			tail++;
			this.processor.EmitGlobalByte((ushort)((ushort)(0xd * GoalHoriz) + GoalVert + 0xd816), 1);

			bool sea = UnitMovesBySea(troop);
			bool found = false;
			// The last values of the locals 0xa (steps, then the distance), 0x10, 0x2e, 0x30, 0x34 (unset: never written)
			bool stepsSet = false, linksSet = false, neighbourSet = false;
			short steps = 0, links = 0, nextColumn = 0, nextRow = 0;
			short column, row;

			this.processor.EmitLocalWord(0x16, sea ? (ushort)1 : (ushort)0);

			do
			{
				column = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6594));
				row = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6694));

				if (column == unitColumn && row == unitRow)
				{
					found = true;
					break;
				}

				steps = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * column) + row + 0xd816));
				stepsSet = true;
				head++;
				links = RouteBlockLinks(sea, column, row);
				linksSet = true;

				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
						continue;

					nextColumn = RouteNeighbourColumn(direction, column);
					nextRow = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + row);
					neighbourSet = true;

					ushort block = (ushort)((ushort)(0xd * nextColumn) + nextRow + 0xd816);

					if (this.processor.PullGlobalByte(block) != 0)
						continue;

					this.processor.EmitGlobalByte(block, (byte)(steps + 1));
					this.processor.EmitGlobalByte((ushort)(tail + 0x6594), (byte)nextColumn);
					this.processor.EmitGlobalByte((ushort)(tail + 0x6694), (byte)nextRow);
					tail++;
				}
			}
			while (head != tail);

			this.processor.EmitLocalWord(0x2, tail);
			this.processor.EmitLocalWord(0x4, head);
			this.processor.EmitLocalWord(0xc, found ? (ushort)1 : (ushort)0);
			this.processor.EmitLocalWord(0x14, (ushort)column);
			this.processor.EmitLocalWord(0x2c, (ushort)row);

			GoalHoriz = 0xffff;

			if (found)
			{
				short fewest = 0x63;
				short bestDirection = -1;

				links = RouteBlockLinks(UnitMovesBySea(troop), unitColumn, unitRow);
				linksSet = true;

				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
						continue;

					nextColumn = RouteNeighbourColumn(direction, unitColumn);
					nextRow = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + unitRow);
					neighbourSet = true;

					short blockSteps = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xd * nextColumn) + nextRow + 0xd816));
					this.processor.EmitLocalWord(0x6, (ushort)blockSteps);

					if (blockSteps == 0 || blockSteps > fewest)
						continue;

					short distance = (short)(ushort)this.owner.Helpers.ObtainDistance((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81da)), (nextColumn * 4) + 1,
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81db)), (nextRow * 4) + 1);

					if (blockSteps < fewest)
					{
						fewest = blockSteps;
						bestDirection = direction;
					}
					else
					{
						this.processor.EmitLocalWord(0x12, (ushort)distance);

						// Compared with 0xa: the last distance, or the steps of the search (unwritten: what is in the frame)
						if (distance >= (stepsSet ? steps : (short)this.processor.PullLocalWord(0xa)))
							continue;

						bestDirection = direction;
					}

					steps = distance;
					stepsSet = true;
				}

				this.processor.EmitLocalWord(0x8, (ushort)fewest);
				this.processor.EmitLocalWord(0xe, (ushort)bestDirection);
				this.processor.EmitLocalWord(0x2e, 9);

				if (bestDirection != -1)
					AimRouteStep(bestDirection, unitColumn, unitRow, sea);
			}
			else if (linksSet)
			{
				this.processor.EmitLocalWord(0x2e, 9);
			}

			if (stepsSet)
				this.processor.EmitLocalWord(0xa, (ushort)steps);

			if (linksSet)
				this.processor.EmitLocalWord(0x10, (ushort)links);

			if (neighbourSet)
			{
				this.processor.EmitLocalWord(0x30, (ushort)nextColumn);
				this.processor.EmitLocalWord(0x34, (ushort)nextRow);
			}

			if (GoalHoriz == 0xffff)
			{
				GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
				GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));
			}

			return found;
		}

		/// <summary>
		/// The step into the neighbouring block: its tile (1, 1), else (2, 1), else (2, 2) when the tile isn't of the unit's kind
		/// (land or sea)
		/// </summary>
		private void AimRouteStep(short direction, short unitColumn, short unitRow, bool sea)
		{
			GoalHoriz = (ushort)((((short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + unitColumn) << 2) + 1);

			GoalHoriz = (ushort)TunePosition((short)GoalHoriz);
			GoalVert = (ushort)((((short)this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + unitRow) << 2) + 1);

			if ((this.owner.MapOps.ObtainTerrainType((short)GoalHoriz, (short)GoalVert) == 0xa) == sea)
				return;

			GoalHoriz = (ushort)(GoalHoriz + 1);

			if ((this.owner.MapOps.ObtainTerrainType((short)GoalHoriz, (short)GoalVert) == 0xa) == sea)
				return;

			GoalVert = (ushort)(GoalVert + 1);
		}

		/// <summary>
		/// The next block of 4 x 4 tiles on the way to the place (land blocks 0xdb44, marker 0, or sea blocks 0x7f38, marker 1;
		/// 13 bytes a column): the block of the place itself when it is marked, else the marked neighbouring block (directions
		/// 1 - 8) nearest to the place that has a corner tile of the right kind (ocean for the marker 1) with a route
		/// (ComputeRouteCost) to the place. The block goes to 0x6590 / 0x6592.
		/// </summary>
		/// <returns>1 when a block was found, else 0, also in AX</returns>
		public ushort LocateRouteStep(int horizLoc, int vertLoc, ushort marker)
		{
			this.processor.Journal.OpenScope($"LocateRouteStep({horizLoc}, {vertLoc}, {marker})");

			short blockHoriz = DivideByFour((short)horizLoc);
			short blockVert = DivideByFour((short)vertLoc);
			ushort blocks = marker != 0 ? (ushort)0x7f38 : (ushort)0xdb44;
			int found = -1;

			if (BlockMarked(blocks, blockHoriz, blockVert))
			{
				found = 0;
			}
			else
			{
				short nearest = 0x63;

				for (int directionIdx = 1; directionIdx <= 8; directionIdx++)
				{
					short nextHoriz = (short)(this.processor.PullGlobalWord((ushort)((directionIdx << 1) + 0x1882)) + blockHoriz);
					short nextVert = (short)(this.processor.PullGlobalWord((ushort)((directionIdx << 1) + 0x18e4)) + blockVert);

					if (!BlockMarked(blocks, nextHoriz, nextVert))
						continue;

					short distance = (short)this.owner.Helpers.MergeCoordinates(horizLoc - (nextHoriz * 4) - 1, vertLoc - (nextVert * 4) - 1);

					if (distance >= nearest)
						continue;

					// The corners of the block: (1, 1), (2, 1), (2, 2), (1, 2)
					short cornerHoriz = (short)((nextHoriz << 2) + 1);
					short cornerVert = (short)((nextVert << 2) + 1);

					if (!CornerMatches(cornerHoriz, cornerVert, marker) && !CornerMatches(++cornerHoriz, cornerVert, marker) &&
						!CornerMatches(cornerHoriz, ++cornerVert, marker) && !CornerMatches(--cornerHoriz, cornerVert, marker))
						continue;

					if (ComputeRouteCost(cornerHoriz, cornerVert, horizLoc, vertLoc, marker, 18) != 0xffff)
					{
						nearest = distance;
						found = directionIdx;
					}
				}
			}

			ushort result = 0;

			if (found != -1)
			{
				GoalHoriz = (ushort)(this.processor.PullGlobalWord((ushort)((found << 1) + 0x1882)) + blockHoriz);
				GoalVert = (ushort)(this.processor.PullGlobalWord((ushort)((found << 1) + 0x18e4)) + blockVert);
				result = 1;
			}

			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("LocateRouteStep");

			return result;
		}

		/// <summary>The block is marked in the table of blocks (13 bytes a column)</summary>
		private bool BlockMarked(ushort blocks, short blockHoriz, short blockVert)
		{
			return this.processor.PullGlobalByte((ushort)(blockVert + (0xd * blockHoriz) + blocks)) != 0;
		}

		/// <summary>The tile is ocean for the marker 1, land for the marker 0</summary>
		private bool CornerMatches(short horizLoc, short vertLoc, ushort marker)
		{
			return (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa ? 1 : 0) == marker;
		}

		/// <summary>Signed division by 4 toward zero, like the original (-32768 gives a positive quotient)</summary>
		private static short DivideByFour(short amount)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> 2);

			return amount < 0 ? (short)-quotient : quotient;
		}

		/// <summary>
		/// The unit's next step towards the place 0x6590 / 0x6592 inside the 16 x 16 window around it: the move costs from the
		/// place outwards (0xb780 + 16 * column + row of the window, queue 0x6594 / 0x6694; 1 on a road from a road, 3 for an
		/// air unit, else 3 * the terrain's cost; tiles of the other kind than the unit only with a city), kept while the
		/// place stays the same (0x6796 / 0x6798) and the unit's tile is reached; 0x6794 is the cost to the unit. Below the
		/// limit arg3, the direction to the neighbour with the lowest cost (equal: fewer own units there * 4 + distance to the
		/// place), else -1 and the unit's goal becomes the place. The locals of the frame are written as in the original.
		/// </summary>
		public ushort ShiftToward(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ShiftToward({participantIdx}, {troopIdx}, {arg3})");

			this.processor.EnterFrame(0x4a);
			this.processor.PreserveIndexRegisters();

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			this.processor.RegSource.Value16 = troop;

			short unitHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81d5));
			short unitVert = this.processor.PullGlobalByte((ushort)(troop + 0x81d6));
			short windowHoriz = (short)(GoalHoriz - 8);
			short windowVert = (short)(GoalVert - 8);

			this.processor.EmitLocalWord(0x46, (ushort)unitHoriz);
			this.processor.EmitLocalWord(0x4a, (ushort)unitVert);
			this.processor.EmitLocalWord(0x20, (ushort)windowHoriz);
			this.processor.EmitLocalWord(0x3a, (ushort)windowVert);

			GotoCost = 0;

			if (GoalHoriz != RoutePlaceHoriz ||
				GoalVert != RoutePlaceVert ||
				this.processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, unitHoriz, unitVert)) == 0)
			{
				ChartWindowCosts(participantIdx, troopIdx, arg3, unitHoriz, unitVert, windowHoriz, windowVert);
			}

			this.processor.EmitLocalWord(0x16, 0xffff);

			short step = -1;

			if ((short)arg3 > (short)GotoCost)
				step = ChooseCheapestStep(participantIdx, troopIdx, unitHoriz, unitVert, windowHoriz, windowVert);

			if (step == -1)
			{
				this.processor.RegSource.Value16 = troop;

				GoalHoriz = this.processor.PullGlobalByte((ushort)(troop + 0x81da));
				GoalVert = this.processor.PullGlobalByte((ushort)(troop + 0x81db));
			}

			this.processor.RegAcc.Value16 = (ushort)step;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftToward");

			return (ushort)step;
		}

		/// <summary>The cell of the tile in the window of move costs (0xb780 + 16 * column + row)</summary>
		private static ushort WindowCell(short windowHoriz, short windowVert, short horizLoc, short vertLoc)
		{
			return (ushort)((ushort)((horizLoc - windowHoriz) << 4) + vertLoc - windowVert + 0xb780);
		}

		/// <summary>The move costs of the window from the place outwards, until the unit's tile (see ShiftToward)</summary>
		private void ChartWindowCosts(short participantIdx, short troopIdx, ushort arg3, short unitHoriz, short unitVert, short windowHoriz, short windowVert)
		{
			RoutePlaceHoriz = GoalHoriz;
			RoutePlaceVert = GoalVert;

			this.owner.Libc.FillBytes(0xb780, 0, 0x100);

			byte tail = 0, head = 0;

			this.processor.EmitGlobalByte((ushort)(tail + 0x6594), (byte)GoalHoriz);
			this.processor.EmitGlobalByte((ushort)(tail + 0x6694), (byte)GoalVert);
			tail++;
			this.processor.EmitGlobalByte(WindowCell(windowHoriz, windowVert, (short)GoalHoriz, (short)GoalVert), 1);

			this.processor.EmitLocalWord(0x14, 0);
			GotoCost = arg3;

			ushort kind = (ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx) + 0x81d7)));
			this.processor.RegSource.Value16 = kind;

			bool sea = this.processor.PullGlobalWord((ushort)(kind + 0x1138)) == 2;
			bool air = this.processor.PullGlobalWord((ushort)(kind + 0x113a)) == 1;

			this.processor.EmitLocalWord(0x1e, sea ? (ushort)1 : (ushort)0);
			this.processor.EmitLocalWord(0x10, air ? (ushort)1 : (ushort)0);

			do
			{
				short horizLoc = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6594));
				short vertLoc = (sbyte)this.processor.PullGlobalByte((ushort)(head + 0x6694));
				head++;

				this.processor.EmitLocalWord(0x1c, (ushort)horizLoc);
				this.processor.EmitLocalWord(0x36, (ushort)vertLoc);
				this.processor.EmitLocalWord(0x4, head);

				short cost = (sbyte)this.processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, horizLoc, vertLoc));
				this.processor.EmitLocalWord(0xe, (ushort)cost);

				if (cost > (short)GotoCost)
					continue;

				if ((short)TunePosition(horizLoc) == unitHoriz && vertLoc == unitVert)
				{
					GotoCost = (ushort)cost;
					continue;
				}

				tail = ExpandWindowCell(horizLoc, vertLoc, cost, tail, sea, air, windowHoriz, windowVert);
			}
			while (head != tail && (short)head < 0xe1);

			this.processor.EmitLocalWord(0x2, tail);
		}

		/// <summary>The neighbours of a tile of the window get its cost plus the move cost (see ShiftToward); the new tail</summary>
		private byte ExpandWindowCell(short horizLoc, short vertLoc, short cost, byte tail, bool sea, bool air, short windowHoriz, short windowVert)
		{
			ushort road = this.owner.MapOps.ContainsRoad((short)(ushort)TunePosition(horizLoc), vertLoc);
			this.processor.EmitLocalWord(0x8, road);

			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				this.processor.EmitLocalWord(0x3c, (ushort)direction);

				short nextHoriz = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + horizLoc);
				this.processor.EmitLocalWord(0x40, (ushort)nextHoriz);

				if (this.owner.Libc.AbsoluteValue((short)(nextHoriz - GoalHoriz)) >= 8)
					continue;

				short tileHoriz = (short)TunePosition(nextHoriz);
				this.processor.EmitLocalWord(0xc, (ushort)tileHoriz);

				short nextVert = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + vertLoc);
				this.processor.EmitLocalWord(0x44, (ushort)nextVert);

				if (this.owner.Libc.AbsoluteValue((short)(nextVert - GoalVert)) >= 8)
					continue;

				if (this.owner.MapOps.VerifyMapLimits(tileHoriz, nextVert) == 0)
					continue;

				ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, nextVert);
				this.processor.EmitLocalWord(0x3e, terrain);

				if ((terrain == 0xa) != sea && (this.owner.MapOps.ObtainTileImprovements(tileHoriz, nextVert) & 1) == 0)
					continue;

				short nextCost;

				if (road != 0 && this.owner.MapOps.ContainsRoad(tileHoriz, nextVert) != 0)
				{
					nextCost = (short)(cost + 1);
				}
				else if (air)
				{
					nextCost = (short)(cost + 3);
				}
				else
				{
					nextCost = (short)((3 * (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0x13 * terrain) + 0x28e))) + cost);
				}

				this.processor.EmitLocalWord(0x1a, (ushort)nextCost);

				ushort cell = WindowCell(windowHoriz, windowVert, nextHoriz, nextVert);
				short known = (sbyte)this.processor.PullGlobalByte(cell);
				this.processor.EmitLocalWord(0x38, (ushort)known);

				if (known != 0 && known <= nextCost)
					continue;

				this.processor.EmitGlobalByte(cell, (byte)nextCost);
				this.processor.EmitGlobalByte((ushort)(tail + 0x6594), (byte)nextHoriz);
				this.processor.EmitGlobalByte((ushort)(tail + 0x6694), (byte)nextVert);
				tail++;
				this.processor.EmitLocalWord(0x2, tail);
			}

			this.processor.EmitLocalWord(0x3c, (ushort)direction);

			return tail;
		}

		/// <summary>
		/// The direction to the unit's neighbour with the lowest cost in the window (equal ones: 4 * own units there + distance
		/// to the place), -1 when there is none (see ShiftToward)
		/// </summary>
		private short ChooseCheapestStep(short participantIdx, short troopIdx, short unitHoriz, short unitVert, short windowHoriz, short windowVert)
		{
			short lowest = 0x63;
			short best = -1;
			short direction;

			this.processor.EmitLocalWord(0xa, (ushort)lowest);

			for (direction = 1; direction <= 8; direction++)
			{
				this.processor.EmitLocalWord(0x3c, (ushort)direction);

				short nextHoriz = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + unitHoriz);
				short place = (short)GoalHoriz;

				if (this.owner.Libc.AbsoluteValue((short)(nextHoriz - place)) >= 0x48)
					nextHoriz = (short)(nextHoriz <= place ? nextHoriz + 0x50 : nextHoriz - 0x50);

				this.processor.EmitLocalWord(0x40, (ushort)nextHoriz);

				if (this.owner.Libc.AbsoluteValue((short)(nextHoriz - GoalHoriz)) >= 8)
					continue;

				short tileHoriz = (short)TunePosition(nextHoriz);
				this.processor.EmitLocalWord(0xc, (ushort)tileHoriz);

				short nextVert = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + unitVert);
				this.processor.EmitLocalWord(0x44, (ushort)nextVert);

				if (this.owner.Libc.AbsoluteValue((short)(nextVert - GoalVert)) >= 8)
					continue;

				bool seaTile = this.owner.MapOps.ObtainTerrainType(tileHoriz, nextVert) == 0xa;

				ushort kind = (ushort)(0x22 * (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx) + 0x81d7)));
				this.processor.RegSource.Value16 = seaTile ? (ushort)1 : (ushort)0;

				if ((this.processor.PullGlobalWord((ushort)(kind + 0x1138)) == 2) != seaTile &&
					(this.owner.MapOps.ObtainTileImprovements(tileHoriz, nextVert) & 1) == 0)
					continue;

				short cost = (sbyte)this.processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, nextHoriz, nextVert));
				this.processor.EmitLocalWord(0x6, (ushort)cost);

				if (cost == 0)
					continue;

				if (cost < lowest)
				{
					lowest = cost;
					best = direction;

					this.processor.EmitLocalWord(0xa, (ushort)lowest);
					this.processor.EmitLocalWord(0x16, (ushort)best);
					this.processor.EmitLocalWord(0xe, (ushort)CrowdAndDistance(participantIdx, tileHoriz, nextVert, 0xe));
				}

				if (cost != lowest)
					continue;

				short alternative = CrowdAndDistance(participantIdx, tileHoriz, nextVert, 0x1a);

				if (alternative < (short)this.processor.PullLocalWord(0xe))
				{
					best = direction;

					this.processor.EmitLocalWord(0x16, (ushort)best);
					this.processor.EmitLocalWord(0xe, (ushort)alternative);
				}
			}

			this.processor.EmitLocalWord(0x3c, (ushort)direction);

			return best;
		}

		/// <summary>
		/// 4 * the units of the player in the stack on the tile (none: 0) plus the distance to the place; the units go into the
		/// local first, like in the original
		/// </summary>
		private short CrowdAndDistance(short participantIdx, short horizLoc, short vertLoc, int local)
		{
			ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x12, troopIdx);

			if (troopIdx != 0xffff)
			{
				this.processor.EmitLocalWord(local, (ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)troopIdx, 2) << 2));
			}
			else
			{
				this.processor.EmitLocalWord(local, 0);
			}

			ushort total = (ushort)(this.processor.PullLocalWord(local) + (ushort)this.owner.Helpers.ObtainDistance((short)GoalHoriz, horizLoc, (short)GoalVert, vertLoc));
			this.processor.EmitLocalWord(local, total);

			return (short)total;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos1"></param>
		/// <param name="flag"></param>
		/// <param name="param6"></param>
		/// <returns></returns>
		public ushort ComputeRouteCost(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort marker, ushort arg6)
		{
			this.processor.Journal.OpenScope($"ComputeRouteCost({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, {marker}, {arg6})");

			// function body
			if (Math.Abs(horizLoc - horizSpot1) <= 7 && Math.Abs(vertLoc - vertSpot1) <= 7)
			{
				this.processor.EmitGlobalByte(0x87c9, (byte)((sbyte)horizLoc));
				this.processor.EmitGlobalByte(0x87ca, (byte)((sbyte)vertLoc));				
				this.processor.EmitGlobalByte(0x87cb, (byte)((marker == 0) ? 1 : 0x10));
				GoalHoriz = (ushort)((short)horizSpot1);
				GoalVert = (ushort)((short)vertSpot1);

				ushort direction = ShiftToward(0, 127, arg6);

				this.processor.EmitGlobalByte(0x87cb, 0xff);

				if (direction == 0xffff)
				{
					this.processor.RegAcc.Value16 = 0xffff;
				}
				else
				{
					this.processor.RegAcc.Value16 = GotoCost;
				}
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}

			// Far return
			this.processor.Journal.CloseScope("ComputeRouteCost");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Adjust X position
		/// </summary>
		/// <param name="xPos"></param>
		/// <returns></returns>
		public int TunePosition(int horizLoc)
		{
			int backAmountInt = horizLoc;

			// function body
			if (horizLoc < 0)
			{
				backAmountInt = horizLoc + 80;
			}
			else if (horizLoc >= 80)
			{
				backAmountInt = horizLoc - 80;
			}

			this.processor.RegAcc.Value16 = (ushort)((short)backAmountInt);

			return backAmountInt;
		}
	}
}
