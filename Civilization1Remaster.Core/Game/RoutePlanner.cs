using System;

namespace Civilization1Remaster.Core.Game
{
	public class RoutePlanner : GameComponent, IRoutePlanner
	{
		private const ushort QueueHorizAddress = 0x6594;

		private const ushort QueueVertAddress = 0x6694;

		private const ushort BlockStepsAddress = 0xd816;

		private const ushort SeaBlockLinksAddress = 0x7f38;

		private const ushort LandBlockLinksAddress = 0xdb44;

		private const ushort CostWindowAddress = 0xb780;

		private readonly UnitMemory troops;

		public RoutePlanner(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public ushort CarryOutGoto(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"CarryOutGoto({participantIdx}, {troopIdx})");

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			ushort direction;

			if (participantIdx != this.Owner.World.PersonParticipantIdx || !StepToNearGoal(troop, troopIdx, out direction))
			{
				direction = StepTowardsGoal(participantIdx, troopIdx, troop);
			}

			return direction;
		}

		public ushort ChartRoute(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ChartRoute({participantIdx}, {troopIdx})");

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
			ushort unitHoriz = this.Troops.PullAt(troop, UnitMemory.Horiz);
			ushort unitVert = this.Troops.PullAt(troop, UnitMemory.Vert);

			Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
			Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);

			ushort sea = UnitMovesBySea(troop) ? (ushort)1 : (ushort)0;

			ushort step = FindRouteStep((short)unitHoriz, (short)unitVert, sea);

			ushort result;

			if (step == 0)
			{
				Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
				Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);

				result = this.Troops.PullAt(troop, UnitMemory.TravelVert);
			}
			else
			{
				short unitColumn = (short)Search.GoalHoriz, unitRow = (short)Search.GoalVert;

				FindRouteStep(this.Troops.PullAt(troop, UnitMemory.TravelHoriz), this.Troops.PullAt(troop, UnitMemory.TravelVert),
					UnitMovesBySea(troop) ? (ushort)1 : (ushort)0);

				result = SearchRouteBlocks(troop, unitColumn, unitRow) ? (ushort)1 : (ushort)0;
			}

			return result;
		}

		public ushort FindRouteStep(int horizLoc, int vertLoc, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"LocateRouteStep({horizLoc}, {vertLoc}, {marker})");

			short blockHoriz = DivideByFour((short)horizLoc);
			short blockVert = DivideByFour((short)vertLoc);
			ushort blocks = marker != 0 ? SeaBlockLinksAddress : LandBlockLinksAddress;
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
					short nextHoriz = (short)(Tables.DirectionHoriz(directionIdx) + blockHoriz);
					short nextVert = (short)(Tables.DirectionVert(directionIdx) + blockVert);

					if (!BlockMarked(blocks, nextHoriz, nextVert))
					{
						continue;
					}

					short distance = (short)this.Owner.Helpers.MergeCoordinates(horizLoc - (nextHoriz * 4) - 1, vertLoc - (nextVert * 4) - 1);

					if (distance >= nearest)
					{
						continue;
					}

					short cornerHoriz = (short)((nextHoriz << 2) + 1);
					short cornerVert = (short)((nextVert << 2) + 1);

					if (!CornerMatches(cornerHoriz, cornerVert, marker) && !CornerMatches(++cornerHoriz, cornerVert, marker) &&
						!CornerMatches(cornerHoriz, ++cornerVert, marker) && !CornerMatches(--cornerHoriz, cornerVert, marker))
					{
						continue;
					}

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
				Search.GoalHoriz = (ushort)(Tables.DirectionHoriz(found) + blockHoriz);
				Search.GoalVert = (ushort)(Tables.DirectionVert(found) + blockVert);
				result = 1;
			}

			return result;
		}

		public ushort ShiftToward(short participantIdx, short troopIdx, ushort costLimit)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftToward({participantIdx}, {troopIdx}, {costLimit})");

			ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));

			short unitHoriz = this.Troops.PullAt(troop, UnitMemory.Horiz);
			short unitVert = this.Troops.PullAt(troop, UnitMemory.Vert);
			short windowHoriz = (short)(Search.GoalHoriz - 8);
			short windowVert = (short)(Search.GoalVert - 8);

			Search.GotoCost = 0;

			if (Search.GoalHoriz != Search.RoutePlaceHoriz ||
				Search.GoalVert != Search.RoutePlaceVert ||
				this.Processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, unitHoriz, unitVert)) == 0)
			{
				ChartWindowCosts(participantIdx, troopIdx, costLimit, unitHoriz, unitVert, windowHoriz, windowVert);
			}

			short step = -1;

			if ((short)costLimit > (short)Search.GotoCost)
			{
				step = ChooseCheapestStep(participantIdx, troopIdx, unitHoriz, unitVert, windowHoriz, windowVert);
			}

			if (step == -1)
			{
				Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
				Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);
			}

			return (ushort)step;
		}

		public ushort ComputeRouteCost(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort marker, ushort costLimit)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeRouteCost({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, {marker}, {costLimit})");

			ushort cost;

			if (Math.Abs(horizLoc - horizSpot1) <= 7 && Math.Abs(vertLoc - vertSpot1) <= 7)
			{
				this.troops.Assign(0, 127, UnitMemory.Horiz, (byte)((sbyte)horizLoc));
				this.troops.Assign(0, 127, UnitMemory.Vert, (byte)((sbyte)vertLoc));
				this.troops.Assign(0, 127, UnitMemory.Kind, (byte)((marker == 0) ? 1 : 0x10));
				Search.GoalHoriz = (ushort)((short)horizSpot1);
				Search.GoalVert = (ushort)((short)vertSpot1);

				ushort direction = ShiftToward(0, 127, costLimit);

				this.troops.Assign(0, 127, UnitMemory.Kind, 0xff);

				if (direction == 0xffff)
				{
					cost = 0xffff;
				}
				else
				{
					cost = Search.GotoCost;
				}
			}
			else
			{
				cost = 0xffff;
			}

			return cost;
		}

		public int TunePosition(int horizLoc)
		{
			int backAmountInt = horizLoc;

			if (horizLoc < 0)
			{
				backAmountInt = horizLoc + 80;
			}
			else if (horizLoc >= 80)
			{
				backAmountInt = horizLoc - 80;
			}

			return backAmountInt;
		}

		private static short GotoDistance(short shiftHoriz, short shiftVert)
		{
			short horiz = (short)Math.Abs(shiftHoriz), vert = (short)Math.Abs(shiftVert);

			return (short)((horiz > vert ? horiz : vert) + horiz + vert);
		}

		private static ushort BlockStepsCell(int column, int row) => (ushort)((ushort)(0xd * column) + row + BlockStepsAddress);

		private static short DivideByFour(short amount)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> 2);

			return amount < 0 ? (short)-quotient : quotient;
		}

		private static ushort WindowCell(short windowHoriz, short windowVert, short horizLoc, short vertLoc)
		{
			return (ushort)((ushort)((horizLoc - windowHoriz) << 4) + vertLoc - windowVert + CostWindowAddress);
		}

		private bool StepToNearGoal(ushort troop, short troopIdx, out ushort direction)
		{
			short shiftHoriz = troopIdx < 128 ? (short)(this.Troops.PullAt(troop, UnitMemory.TravelHoriz) - this.Troops.PullAt(troop, UnitMemory.Horiz)) : (short)0;
			short shiftVert = troopIdx < 128 ? (short)(this.Troops.PullAt(troop, UnitMemory.TravelVert) - this.Troops.PullAt(troop, UnitMemory.Vert)) : (short)0;

			direction = 0;

			if (Math.Abs(shiftHoriz) > 1 || Math.Abs(shiftVert) > 1)
			{
				return false;
			}

			shiftHoriz = (short)Math.Sign(shiftHoriz);
			shiftVert = (short)Math.Sign(shiftVert);

			if (troopIdx < 128)
			{
				this.Troops.AssignAt(troop, UnitMemory.TravelHoriz, 0xff);
			}

			ushort candidate;

			for (candidate = 1; candidate <= 8; candidate++)
			{
				if ((short)Tables.DirectionHoriz(candidate) == shiftHoriz &&
					(short)Tables.DirectionVert(candidate) == shiftVert)
				{
					direction = candidate;
					break;
				}
			}

			return true;
		}

		private ushort StepTowardsGoal(short participantIdx, short troopIdx, ushort troop)
		{
			ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

			if (Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 1)
			{
				Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
				Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);
			}
			else
			{
				bool windowFailed = false;
				short columns = (short)Math.Abs((short)(this.Troops.PullAt(troop, UnitMemory.TravelHoriz) - this.Troops.PullAt(troop, UnitMemory.Horiz)));

				if (columns < 7 || columns > 0x49)
				{
					if (Math.Abs((short)(this.Troops.PullAt(troop, UnitMemory.TravelVert) - this.Troops.PullAt(troop, UnitMemory.Vert))) < 7)
					{
						Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
						Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);

						if (TryShiftToward(participantIdx, troopIdx, out ushort step))
						{
							return step;
						}

						windowFailed = true;
					}
				}

				if (ChartRoute(participantIdx, troopIdx) != 0 || !windowFailed)
				{
					if (TryShiftToward(participantIdx, troopIdx, out ushort step))
					{
						return step;
					}
				}
			}

			return ChooseGotoStep(participantIdx, troop);
		}

		private bool TryShiftToward(short participantIdx, short troopIdx, out ushort step)
		{
			step = ShiftToward(participantIdx, troopIdx, 0x3e7);

			return step != 0xffff;
		}

		private ushort ChooseGotoStep(short participantIdx, ushort troop)
		{
			short unitHoriz = this.Troops.PullAt(troop, UnitMemory.Horiz);
			short unitVert = this.Troops.PullAt(troop, UnitMemory.Vert);
			short shiftHoriz = (short)(Search.GoalHoriz - unitHoriz);
			short shiftVert = (short)(Search.GoalVert - unitVert);

			short distance = GotoDistance(shiftHoriz, shiftVert);

			if (shiftHoriz == 0 && shiftVert == 0)
			{
				this.Troops.AssignAt(troop, UnitMemory.TravelHeading, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.TravelHoriz, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);

				return 0xffff;
			}

			short lowest = 0x270f;
			short best = 0;

			ushort road = (ushort)(this.Owner.MapOps.ObtainTileImprovements((sbyte)unitHoriz, (sbyte)unitVert) & 8);

			ushort occupied = this.Owner.UnitOps.MayOccupyTile(participantIdx, (sbyte)unitHoriz, (sbyte)unitVert);

			ushort direction;

			for (direction = 1; direction < 9; direction++)
			{
				short stepHoriz = (short)Tables.DirectionHoriz(direction);
				short stepVert = (short)Tables.DirectionVert(direction);
				short nextHoriz = (short)(this.Troops.PullAt(troop, UnitMemory.Horiz) + stepHoriz);
				short nextVert = (short)(this.Troops.PullAt(troop, UnitMemory.Vert) + stepVert);
				short leftHoriz = (short)(shiftHoriz - stepHoriz);
				short leftVert = (short)(shiftVert - stepVert);

				short nextDistance = GotoDistance(leftHoriz, leftVert);

				if (participantIdx == this.Owner.World.PersonParticipantIdx && nextDistance > distance)
				{
					continue;
				}

				if (!MayGotoTile(participantIdx, troop, nextHoriz, nextVert, occupied, out ushort terrain))
				{
					continue;
				}

				short cost;

				if (road != 0 && (this.Owner.MapOps.ObtainTileImprovements(nextHoriz, nextVert) & 8) != 0)
				{
					cost = 1;
				}
				else
				{
					ushort unitKind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

					cost = (short)Tables.UnitKindWordAt(unitKind, UnitKindTable.ShiftTally) <= 1 ? (short)3 : (short)(3 * (sbyte)Tables.TerrainField(terrain, TerrainTable.MoveCost));
				}

				cost += (short)((nextDistance << 2) + Math.Abs(leftHoriz) + Math.Abs(leftVert));

				byte last = this.Troops.PullAt(troop, UnitMemory.TravelHeading);

				if (last != 0xff)
				{
					short turn = (short)Math.Abs((short)((sbyte)last - direction));

					if (turn > 4)
					{
						turn = (short)(8 - turn);
					}

					cost += (short)(turn * turn);
				}

				if (cost < lowest)
				{
					best = (short)direction;
					lowest = cost;
				}
			}

			byte previous = this.Troops.PullAt(troop, UnitMemory.TravelHeading);

			if (previous != 0xff && (ushort)((ushort)(sbyte)previous ^ 4) == (ushort)best)
			{
				this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);
				best = 0;
			}

			if (best == 0)
			{
				this.Troops.AssignAt(troop, UnitMemory.TravelHeading, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.TravelHoriz, 0xff);

				return 0xffff;
			}

			this.Troops.AssignAt(troop, UnitMemory.TravelHeading, (byte)best);

			return (ushort)best;
		}

		private bool MayGotoTile(short participantIdx, ushort troop, short horizLoc, short vertLoc, ushort occupied, out ushort terrain)
		{
			terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			ushort ownerIdx = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

			bool allowed = false;

			if (ownerIdx == 0xffff || ownerIdx == (ushort)participantIdx)
			{
				ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

				if ((Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2) == (terrain == 0xa) &&
					(occupied == 0 || this.Owner.UnitOps.MayOccupyTile(participantIdx, horizLoc, vertLoc) == 0))
				{
					allowed = true;
				}
				else
				{
					if (Tables.UnitKindWord((sbyte)this.Troops.PullAt(troop, UnitMemory.Kind), UnitKindTable.LandGroup) == 1)
					{
						allowed = true;
					}
				}
			}

			if (!allowed)
			{
				if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0 ||
					this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) != (ushort)participantIdx)
				{
					return false;
				}
			}

			if (terrain == 0xa && (short)this.Owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 5)
			{
				return false;
			}

			return true;
		}

		private bool UnitMovesBySea(ushort troop)
		{
			return Tables.UnitKindWord((sbyte)this.Troops.PullAt(troop, UnitMemory.Kind), UnitKindTable.LandGroup) == 2;
		}

		private short RouteBlockLinks(bool sea, short column, short row)
		{
			return (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)(0xd * column) + row + (sea ? SeaBlockLinksAddress : LandBlockLinksAddress)));
		}

		private short RouteNeighbourColumn(short direction, short column)
		{
			short nextColumn = (short)(Tables.DirectionHoriz(direction) + column);

			if (nextColumn == 0x14)
			{
				nextColumn = 0;
			}

			if (nextColumn == -1)
			{
				nextColumn = 0x13;
			}

			return nextColumn;
		}

		private void Enqueue(byte tail, int horizLoc, int vertLoc)
		{
			this.Processor.EmitGlobalByte((ushort)(tail + QueueHorizAddress), (byte)horizLoc);
			this.Processor.EmitGlobalByte((ushort)(tail + QueueVertAddress), (byte)vertLoc);
		}

		private short QueuedHoriz(byte head) => (sbyte)this.Processor.PullGlobalByte((ushort)(head + QueueHorizAddress));

		private short QueuedVert(byte head) => (sbyte)this.Processor.PullGlobalByte((ushort)(head + QueueVertAddress));

		private bool SearchRouteBlocks(ushort troop, short unitColumn, short unitRow)
		{
			this.Owner.Libc.FillBytes(BlockStepsAddress, 0, 0x104);

			byte tail = 0, head = 0;

			Enqueue(tail, Search.GoalHoriz, Search.GoalVert);
			tail++;
			this.Processor.EmitGlobalByte(BlockStepsCell(Search.GoalHoriz, Search.GoalVert), 1);

			bool sea = UnitMovesBySea(troop);
			bool found = false;
			bool stepsSet = false, linksSet = false, neighbourSet = false;
			short steps = 0;
			short column, row;
			short links;
			short nextColumn;
			short nextRow;
			do
			{
				column = QueuedHoriz(head);
				row = QueuedVert(head);

				if (column == unitColumn && row == unitRow)
				{
					found = true;
					break;
				}

				steps = (sbyte)this.Processor.PullGlobalByte(BlockStepsCell(column, row));
				stepsSet = true;
				head++;
				links = RouteBlockLinks(sea, column, row);
				linksSet = true;

				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
					{
						continue;
					}

					nextColumn = RouteNeighbourColumn(direction, column);
					nextRow = (short)(Tables.DirectionVert(direction) + row);
					neighbourSet = true;

					ushort block = BlockStepsCell(nextColumn, nextRow);

					if (this.Processor.PullGlobalByte(block) != 0)
					{
						continue;
					}

					this.Processor.EmitGlobalByte(block, (byte)(steps + 1));
					Enqueue(tail, nextColumn, nextRow);
					tail++;
				}
			}
			while (head != tail);

			Search.GoalHoriz = 0xffff;

			if (found)
			{
				short fewest = 0x63;
				short bestDirection = -1;

				links = RouteBlockLinks(UnitMovesBySea(troop), unitColumn, unitRow);
				for (short direction = 1; direction <= 8; direction++)
				{
					if (((1 << (byte)(direction - 1)) & links) == 0)
					{
						continue;
					}

					nextColumn = RouteNeighbourColumn(direction, unitColumn);
					nextRow = (short)(Tables.DirectionVert(direction) + unitRow);
					neighbourSet = true;

					short blockSteps = (sbyte)this.Processor.PullGlobalByte(BlockStepsCell(nextColumn, nextRow));

					if (blockSteps == 0 || blockSteps > fewest)
					{
						continue;
					}

					short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance((sbyte)this.Troops.PullAt(troop, UnitMemory.TravelHoriz), (nextColumn * 4) + 1,
						(sbyte)this.Troops.PullAt(troop, UnitMemory.TravelVert), (nextRow * 4) + 1);

					if (blockSteps < fewest)
					{
						fewest = blockSteps;
						bestDirection = direction;
					}
					else
					{
						if (distance >= (stepsSet ? steps : (short)0))
						{
							continue;
						}

						bestDirection = direction;
					}

					steps = distance;
					stepsSet = true;
				}

				if (bestDirection != -1)
				{
					AimRouteStep(bestDirection, unitColumn, unitRow, sea);
				}
			}
			else if (linksSet)
			{
			}

			if (neighbourSet)
			{
			}

			if (Search.GoalHoriz == 0xffff)
			{
				Search.GoalHoriz = this.Troops.PullAt(troop, UnitMemory.TravelHoriz);
				Search.GoalVert = this.Troops.PullAt(troop, UnitMemory.TravelVert);
			}

			return found;
		}

		private void AimRouteStep(short direction, short unitColumn, short unitRow, bool sea)
		{
			Search.GoalHoriz = (ushort)((((short)Tables.DirectionHoriz(direction) + unitColumn) << 2) + 1);

			Search.GoalHoriz = (ushort)TunePosition((short)Search.GoalHoriz);
			Search.GoalVert = (ushort)((((short)Tables.DirectionVert(direction) + unitRow) << 2) + 1);

			if ((this.Owner.MapOps.ObtainTerrainType((short)Search.GoalHoriz, (short)Search.GoalVert) == 0xa) == sea)
			{
				return;
			}

			Search.GoalHoriz = (ushort)(Search.GoalHoriz + 1);

			if ((this.Owner.MapOps.ObtainTerrainType((short)Search.GoalHoriz, (short)Search.GoalVert) == 0xa) == sea)
			{
				return;
			}

			Search.GoalVert = (ushort)(Search.GoalVert + 1);
		}

		private bool BlockMarked(ushort blocks, short blockHoriz, short blockVert)
		{
			return this.Processor.PullGlobalByte((ushort)(blockVert + (0xd * blockHoriz) + blocks)) != 0;
		}

		private bool CornerMatches(short horizLoc, short vertLoc, ushort marker)
		{
			return (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa ? 1 : 0) == marker;
		}

		private void ChartWindowCosts(short participantIdx, short troopIdx, ushort costLimit, short unitHoriz, short unitVert, short windowHoriz, short windowVert)
		{
			Search.RoutePlaceHoriz = Search.GoalHoriz;
			Search.RoutePlaceVert = Search.GoalVert;

			this.Owner.Libc.FillBytes(CostWindowAddress, 0, 0x100);

			byte tail = 0, head = 0;

			Enqueue(tail, Search.GoalHoriz, Search.GoalVert);
			tail++;
			this.Processor.EmitGlobalByte(WindowCell(windowHoriz, windowVert, (short)Search.GoalHoriz, (short)Search.GoalVert), 1);

			Search.GotoCost = costLimit;

			ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx), UnitMemory.Kind));

			bool sea = Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2;
			bool air = Tables.UnitKindWordAt(kind, UnitKindTable.ShiftTally) == 1;

			do
			{
				short horizLoc = QueuedHoriz(head);
				short vertLoc = QueuedVert(head);
				head++;

				short cost = (sbyte)this.Processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, horizLoc, vertLoc));

				if (cost > (short)Search.GotoCost)
				{
					continue;
				}

				if ((short)TunePosition(horizLoc) == unitHoriz && vertLoc == unitVert)
				{
					Search.GotoCost = (ushort)cost;
					continue;
				}

				tail = ExpandWindowCell(horizLoc, vertLoc, cost, tail, sea, air, windowHoriz, windowVert);
			}
			while (head != tail && (short)head < 0xe1);
		}

		private byte ExpandWindowCell(short horizLoc, short vertLoc, short cost, byte tail, bool sea, bool air, short windowHoriz, short windowVert)
		{
			ushort road = this.Owner.MapOps.ContainsRoad((short)(ushort)TunePosition(horizLoc), vertLoc);

			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				short nextHoriz = (short)(Tables.DirectionHoriz(direction) + horizLoc);

				if (this.Owner.Libc.AbsoluteValue((short)(nextHoriz - Search.GoalHoriz)) >= 8)
				{
					continue;
				}

				short tileHoriz = (short)TunePosition(nextHoriz);

				short nextVert = (short)(Tables.DirectionVert(direction) + vertLoc);

				if (this.Owner.Libc.AbsoluteValue((short)(nextVert - Search.GoalVert)) >= 8)
				{
					continue;
				}

				if (this.Owner.MapOps.CheckMapLimits(tileHoriz, nextVert) == 0)
				{
					continue;
				}

				ushort terrain = this.Owner.MapOps.ObtainTerrainType(tileHoriz, nextVert);

				if ((terrain == 0xa) != sea && (this.Owner.MapOps.ObtainTileImprovements(tileHoriz, nextVert) & 1) == 0)
				{
					continue;
				}

				short nextCost;

				if (road != 0 && this.Owner.MapOps.ContainsRoad(tileHoriz, nextVert) != 0)
				{
					nextCost = (short)(cost + 1);
				}
				else if (air)
				{
					nextCost = (short)(cost + 3);
				}
				else
				{
					nextCost = (short)((3 * (sbyte)Tables.TerrainField(terrain, TerrainTable.MoveCost)) + cost);
				}

				ushort cell = WindowCell(windowHoriz, windowVert, nextHoriz, nextVert);
				short known = (sbyte)this.Processor.PullGlobalByte(cell);

				if (known != 0 && known <= nextCost)
				{
					continue;
				}

				this.Processor.EmitGlobalByte(cell, (byte)nextCost);
				Enqueue(tail, nextHoriz, nextVert);
				tail++;
			}

			return tail;
		}

		private short ChooseCheapestStep(short participantIdx, short troopIdx, short unitHoriz, short unitVert, short windowHoriz, short windowVert)
		{
			short lowest = 0x63;
			short best = -1;
			short bestCrowdAndDistance = 0;
			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				short nextHoriz = (short)(Tables.DirectionHoriz(direction) + unitHoriz);
				short place = (short)Search.GoalHoriz;

				if (this.Owner.Libc.AbsoluteValue((short)(nextHoriz - place)) >= 0x48)
				{
					nextHoriz = (short)(nextHoriz <= place ? nextHoriz + 0x50 : nextHoriz - 0x50);
				}

				if (this.Owner.Libc.AbsoluteValue((short)(nextHoriz - Search.GoalHoriz)) >= 8)
				{
					continue;
				}

				short tileHoriz = (short)TunePosition(nextHoriz);

				short nextVert = (short)(Tables.DirectionVert(direction) + unitVert);

				if (this.Owner.Libc.AbsoluteValue((short)(nextVert - Search.GoalVert)) >= 8)
				{
					continue;
				}

				bool seaTile = this.Owner.MapOps.ObtainTerrainType(tileHoriz, nextVert) == 0xa;

				ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx), UnitMemory.Kind));

				if ((Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2) != seaTile &&
					(this.Owner.MapOps.ObtainTileImprovements(tileHoriz, nextVert) & 1) == 0)
				{
					continue;
				}

				short cost = (sbyte)this.Processor.PullGlobalByte(WindowCell(windowHoriz, windowVert, nextHoriz, nextVert));

				if (cost == 0)
				{
					continue;
				}

				if (cost < lowest)
				{
					lowest = cost;
					best = direction;
					bestCrowdAndDistance = CrowdAndDistance(participantIdx, tileHoriz, nextVert);
				}

				if (cost != lowest)
				{
					continue;
				}

				short alternative = CrowdAndDistance(participantIdx, tileHoriz, nextVert);

				if (alternative < bestCrowdAndDistance)
				{
					best = direction;
					bestCrowdAndDistance = alternative;
				}
			}

			return best;
		}

		private short CrowdAndDistance(short participantIdx, short horizLoc, short vertLoc)
		{
			ushort troopIdx = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			short crowd = troopIdx != 0xffff
				? (short)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)troopIdx, 2) << 2)
				: (short)0;

			return (short)(crowd + this.Owner.Helpers.ObtainDistance((short)Search.GoalHoriz, horizLoc, (short)Search.GoalVert, vertLoc));
		}
	}
}
