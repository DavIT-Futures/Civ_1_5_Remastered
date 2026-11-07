using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Map
{
	public class RoutePlanner : GameComponent, IRoutePlanner
	{
		private const ushort QueueHorizontalAddress = 0x6594;

		private const ushort QueueVerticalAddress = 0x6694;

		private const ushort BlockStepsAddress = 0xd816;

		private const ushort SeaBlockLinksAddress = 0x7f38;

		private const ushort LandBlockLinksAddress = 0xdb44;

		private const ushort CostWindowAddress = 0xb780;

		private readonly UnitMemory troops;

		public RoutePlanner(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public ushort CarryOutGoto(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"CarryOutGoto({participantIndex}, {troopIndex})");

			ushort troop = (ushort)((ushort)(0x600 * participantIndex) + (ushort)(0xc * troopIndex));
			ushort direction;

			if (participantIndex != this.Owner.World.PersonParticipantIndex || !StepToNearGoal(troop, troopIndex, out direction))
			{
				if (participantIndex == this.Owner.World.PersonParticipantIndex && FastestGotoStep(participantIndex, troopIndex, troop, out direction))
				{
					return direction;
				}

				direction = StepTowardsGoal(participantIndex, troopIndex, troop);
			}

			return direction;
		}

		public ushort ChartRoute(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ChartRoute({participantIndex}, {troopIndex})");

			ushort troop = (ushort)((ushort)(0x600 * participantIndex) + (ushort)(0xc * troopIndex));
			ushort unitHorizontal = this.Troops.PullAt(troop, UnitMemory.Horizontal);
			ushort unitVertical = this.Troops.PullAt(troop, UnitMemory.Vertical);

			Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
			Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);

			ushort sea = UnitMovesBySea(troop) ? (ushort)1 : (ushort)0;

			ushort step = FindRouteStep((short)unitHorizontal, (short)unitVertical, sea);

			ushort result;

			if (step == 0)
			{
				Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
				Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);

				result = this.Troops.PullAt(troop, UnitMemory.TravelVertical);
			}
			else
			{
				short unitColumn = (short)Search.GoalHorizontal, unitRow = (short)Search.GoalVertical;

				FindRouteStep(this.Troops.PullAt(troop, UnitMemory.TravelHorizontal), this.Troops.PullAt(troop, UnitMemory.TravelVertical),
					UnitMovesBySea(troop) ? (ushort)1 : (ushort)0);

				result = SearchRouteBlocks(troop, unitColumn, unitRow) ? (ushort)1 : (ushort)0;
			}

			return result;
		}

		public ushort FindRouteStep(int horizontalLocation, int verticalLocation, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"LocateRouteStep({horizontalLocation}, {verticalLocation}, {marker})");

			short blockHorizontal = DivideByFour((short)horizontalLocation);
			short blockVertical = DivideByFour((short)verticalLocation);
			ushort blocks = marker != 0 ? SeaBlockLinksAddress : LandBlockLinksAddress;
			int found = -1;

			if (BlockMarked(blocks, blockHorizontal, blockVertical))
			{
				found = 0;
			}
			else
			{
				short nearest = 0x63;

				for (int directionIndex = 1; directionIndex <= 8; directionIndex++)
				{
					short nextHorizontal = (short)(Tables.DirectionHorizontal(directionIndex) + blockHorizontal);
					short nextVertical = (short)(Tables.DirectionVertical(directionIndex) + blockVertical);

					if (!BlockMarked(blocks, nextHorizontal, nextVertical))
					{
						continue;
					}

					short distance = (short)this.Owner.Helpers.MergeCoordinates(horizontalLocation - (nextHorizontal * 4) - 1, verticalLocation - (nextVertical * 4) - 1);

					if (distance >= nearest)
					{
						continue;
					}

					short cornerHorizontal = (short)((nextHorizontal << 2) + 1);
					short cornerVertical = (short)((nextVertical << 2) + 1);

					if (!CornerMatches(cornerHorizontal, cornerVertical, marker) && !CornerMatches(++cornerHorizontal, cornerVertical, marker) &&
						!CornerMatches(cornerHorizontal, ++cornerVertical, marker) && !CornerMatches(--cornerHorizontal, cornerVertical, marker))
					{
						continue;
					}

					if (ComputeRouteCost(cornerHorizontal, cornerVertical, horizontalLocation, verticalLocation, marker, 18) != 0xffff)
					{
						nearest = distance;
						found = directionIndex;
					}
				}
			}

			ushort result = 0;

			if (found != -1)
			{
				Search.GoalHorizontal = (ushort)(Tables.DirectionHorizontal(found) + blockHorizontal);
				Search.GoalVertical = (ushort)(Tables.DirectionVertical(found) + blockVertical);
				result = 1;
			}

			return result;
		}

		public ushort ShiftToward(short participantIndex, short troopIndex, ushort costLimit)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftToward({participantIndex}, {troopIndex}, {costLimit})");

			ushort troop = (ushort)((ushort)(0x600 * participantIndex) + (ushort)(0xc * troopIndex));

			short unitHorizontal = this.Troops.PullAt(troop, UnitMemory.Horizontal);
			short unitVertical = this.Troops.PullAt(troop, UnitMemory.Vertical);
			short windowHorizontal = (short)(Search.GoalHorizontal - 8);
			short windowVertical = (short)(Search.GoalVertical - 8);

			Search.GotoCost = 0;

			if (Search.GoalHorizontal != Search.RoutePlaceHorizontal ||
				Search.GoalVertical != Search.RoutePlaceVertical ||
				this.Processor.PullGlobalByte(WindowCell(windowHorizontal, windowVertical, unitHorizontal, unitVertical)) == 0)
			{
				ChartWindowCosts(participantIndex, troopIndex, costLimit, unitHorizontal, unitVertical, windowHorizontal, windowVertical);
			}

			short step = -1;

			if ((short)costLimit > (short)Search.GotoCost)
			{
				step = ChooseCheapestStep(participantIndex, troopIndex, unitHorizontal, unitVertical, windowHorizontal, windowVertical);
			}

			if (step == -1)
			{
				Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
				Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);
			}

			return (ushort)step;
		}

		public ushort ComputeRouteCost(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot, ushort marker, ushort costLimit)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeRouteCost({horizontalLocation}, {verticalLocation}, {firstHorizontalSpot}, {firstVerticalSpot}, {marker}, {costLimit})");

			ushort cost;

			if (Math.Abs(horizontalLocation - firstHorizontalSpot) <= 7 && Math.Abs(verticalLocation - firstVerticalSpot) <= 7)
			{
				this.troops.Assign(0, 127, UnitMemory.Horizontal, (byte)((sbyte)horizontalLocation));
				this.troops.Assign(0, 127, UnitMemory.Vertical, (byte)((sbyte)verticalLocation));
				this.troops.Assign(0, 127, UnitMemory.Kind, (byte)((marker == 0) ? 1 : 0x10));
				Search.GoalHorizontal = (ushort)((short)firstHorizontalSpot);
				Search.GoalVertical = (ushort)((short)firstVerticalSpot);

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

		public int TunePosition(int horizontalLocation)
		{
			int backAmount = horizontalLocation;

			if (horizontalLocation < 0)
			{
				backAmount = horizontalLocation + 80;
			}
			else if (horizontalLocation >= 80)
			{
				backAmount = horizontalLocation - 80;
			}

			return backAmount;
		}

		private static short GotoDistance(short shiftHorizontal, short shiftVertical)
		{
			short horizontal = (short)Math.Abs(shiftHorizontal), vertical = (short)Math.Abs(shiftVertical);

			return (short)((horizontal > vertical ? horizontal : vertical) + horizontal + vertical);
		}

		private static ushort BlockStepsCell(int column, int row) => (ushort)((ushort)(0xd * column) + row + BlockStepsAddress);

		private static short DivideByFour(short amount)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> 2);

			return amount < 0 ? (short)-quotient : quotient;
		}

		private static ushort WindowCell(short windowHorizontal, short windowVertical, short horizontalLocation, short verticalLocation)
		{
			return (ushort)((ushort)((horizontalLocation - windowHorizontal) << 4) + verticalLocation - windowVertical + CostWindowAddress);
		}

		/// <summary>
		/// Remastered GoTo for the player's land and sea units: the first step of the fastest route over the whole map,
		/// preferring railroads (almost free) and roads. Falls back to the original search when no route is known.
		/// </summary>
		private bool FastestGotoStep(short participantIndex, short troopIndex, ushort troop, out ushort direction)
		{
			direction = 0;

			if (troopIndex >= 128)
			{
				return false;
			}

			sbyte kind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);
			ushort group = Tables.UnitKindWord(kind, UnitKindTable.LandGroup);

			if (group != UnitKindTable.LandUnits && group != UnitKindTable.SeaUnits)
			{
				return false;
			}

			bool sea = group == UnitKindTable.SeaUnits;
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RailroadRules.RailroadTechnology) != 0;
			System.Drawing.Point start = new System.Drawing.Point((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));
			System.Drawing.Point goal = new System.Drawing.Point(this.Troops.PullAt(troop, UnitMemory.TravelHorizontal), this.Troops.PullAt(troop, UnitMemory.TravelVertical));

			if (start == goal || goal.X >= MapRules.Width || goal.Y >= MapRules.Height)
			{
				return false;
			}

			IMapPlanes chart = this.Owner.MapOperations.Chart;

			int GotoStepCost(System.Drawing.Point from, System.Drawing.Point to)
			{
				bool known = MapView.Revealed != 0 ||
					((sbyte)this.Owner.World.ChartSight[to.X, to.Y] & (1 << participantIndex)) != 0;
				ushort improvements = this.Owner.MapOperations.ObtainTileImprovements(to.X, to.Y);
				bool city = (improvements & 1) != 0;
				ushort holder = this.Owner.MapOperations.ObtainTileOwner(to.X, to.Y);

				if (to != goal && holder != 0xffff && holder != (ushort)participantIndex)
				{
					return GotoPathfinder.Impassable;
				}

				if (!known)
				{
					return GotoPathfinder.MovePointCost;
				}

				if (city && to != goal && this.Owner.MapOperations.ObtainTileOwnerBits(to.X, to.Y) != (ushort)participantIndex)
				{
					return GotoPathfinder.Impassable;
				}

				int terrain = chart.Terrain(to.X, to.Y);

				if (sea)
				{
					return terrain == MapRules.OceanTerrain || city ? GotoPathfinder.MovePointCost : GotoPathfinder.Impassable;
				}

				if (terrain == MapRules.OceanTerrain && !city)
				{
					return GotoPathfinder.Impassable;
				}

				ushort fromImprovements = this.Owner.MapOperations.ObtainTileImprovements(from.X, from.Y);

				return GotoPathfinder.LandStepCost(fromImprovements, improvements, RailroadRules.HasRailroad(fromImprovements, knowsRailroad),
					(sbyte)Tables.TerrainField(terrain, TerrainTable.MoveCost));
			}

			List<System.Drawing.Point> route = GotoPathfinder.FindRoute(start, goal, GotoStepCost);

			if (route.Count == 0)
			{
				return false;
			}

			System.Drawing.Point next = route[0];
			int shiftHorizontal = next.X - start.X;

			if (shiftHorizontal > 1)
			{
				shiftHorizontal -= MapRules.Width;
			}
			else if (shiftHorizontal < -1)
			{
				shiftHorizontal += MapRules.Width;
			}

			int shiftVertical = next.Y - start.Y;

			for (ushort candidate = 1; candidate <= 8; candidate++)
			{
				if ((short)Tables.DirectionHorizontal(candidate) != shiftHorizontal || (short)Tables.DirectionVertical(candidate) != shiftVertical)
				{
					continue;
				}

				ushort occupied = this.Owner.UnitOperations.MayOccupyTile(participantIndex, (sbyte)start.X, (sbyte)start.Y);

				if (!MayGotoTile(participantIndex, troop, (short)(start.X + shiftHorizontal), (short)next.Y, occupied, out _))
				{
					return false;
				}

				this.Troops.AssignAt(troop, UnitMemory.TravelHeading, (byte)candidate);
				direction = candidate;

				return true;
			}

			return false;
		}

		private bool StepToNearGoal(ushort troop, short troopIndex, out ushort direction)
		{
			short shiftHorizontal = troopIndex < 128 ? (short)(this.Troops.PullAt(troop, UnitMemory.TravelHorizontal) - this.Troops.PullAt(troop, UnitMemory.Horizontal)) : (short)0;
			short shiftVertical = troopIndex < 128 ? (short)(this.Troops.PullAt(troop, UnitMemory.TravelVertical) - this.Troops.PullAt(troop, UnitMemory.Vertical)) : (short)0;

			direction = 0;

			if (Math.Abs(shiftHorizontal) > 1 || Math.Abs(shiftVertical) > 1)
			{
				return false;
			}

			shiftHorizontal = (short)Math.Sign(shiftHorizontal);
			shiftVertical = (short)Math.Sign(shiftVertical);

			if (troopIndex < 128)
			{
				this.Troops.AssignAt(troop, UnitMemory.TravelHorizontal, 0xff);
			}

			ushort candidate;

			for (candidate = 1; candidate <= 8; candidate++)
			{
				if ((short)Tables.DirectionHorizontal(candidate) == shiftHorizontal &&
					(short)Tables.DirectionVertical(candidate) == shiftVertical)
				{
					direction = candidate;
					break;
				}
			}

			return true;
		}

		private ushort StepTowardsGoal(short participantIndex, short troopIndex, ushort troop)
		{
			ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

			if (Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 1)
			{
				Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
				Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);
			}
			else
			{
				bool windowFailed = false;
				short columns = (short)Math.Abs((short)(this.Troops.PullAt(troop, UnitMemory.TravelHorizontal) - this.Troops.PullAt(troop, UnitMemory.Horizontal)));

				if (columns < 7 || columns > 0x49)
				{
					if (Math.Abs((short)(this.Troops.PullAt(troop, UnitMemory.TravelVertical) - this.Troops.PullAt(troop, UnitMemory.Vertical))) < 7)
					{
						Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
						Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);

						if (TryShiftToward(participantIndex, troopIndex, out ushort step))
						{
							return step;
						}

						windowFailed = true;
					}
				}

				if (ChartRoute(participantIndex, troopIndex) != 0 || !windowFailed)
				{
					if (TryShiftToward(participantIndex, troopIndex, out ushort step))
					{
						return step;
					}
				}
			}

			return ChooseGotoStep(participantIndex, troop);
		}

		private bool TryShiftToward(short participantIndex, short troopIndex, out ushort step)
		{
			step = ShiftToward(participantIndex, troopIndex, 0x3e7);

			return step != 0xffff;
		}

		private ushort ChooseGotoStep(short participantIndex, ushort troop)
		{
			short unitHorizontal = this.Troops.PullAt(troop, UnitMemory.Horizontal);
			short unitVertical = this.Troops.PullAt(troop, UnitMemory.Vertical);
			short shiftHorizontal = (short)(Search.GoalHorizontal - unitHorizontal);
			short shiftVertical = (short)(Search.GoalVertical - unitVertical);

			short distance = GotoDistance(shiftHorizontal, shiftVertical);

			if (shiftHorizontal == 0 && shiftVertical == 0)
			{
				this.Troops.AssignAt(troop, UnitMemory.TravelHeading, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.TravelHorizontal, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);

				return 0xffff;
			}

			short lowest = 0x270f;
			short best = 0;

			ushort road = (ushort)(this.Owner.MapOperations.ObtainTileImprovements((sbyte)unitHorizontal, (sbyte)unitVertical) & 8);

			ushort occupied = this.Owner.UnitOperations.MayOccupyTile(participantIndex, (sbyte)unitHorizontal, (sbyte)unitVertical);

			ushort direction;

			for (direction = 1; direction < 9; direction++)
			{
				short stepHorizontal = (short)Tables.DirectionHorizontal(direction);
				short stepVertical = (short)Tables.DirectionVertical(direction);
				short nextHorizontal = (short)(this.Troops.PullAt(troop, UnitMemory.Horizontal) + stepHorizontal);
				short nextVertical = (short)(this.Troops.PullAt(troop, UnitMemory.Vertical) + stepVertical);
				short leftHorizontal = (short)(shiftHorizontal - stepHorizontal);
				short leftVertical = (short)(shiftVertical - stepVertical);

				short nextDistance = GotoDistance(leftHorizontal, leftVertical);

				if (participantIndex == this.Owner.World.PersonParticipantIndex && nextDistance > distance)
				{
					continue;
				}

				if (!MayGotoTile(participantIndex, troop, nextHorizontal, nextVertical, occupied, out ushort terrain))
				{
					continue;
				}

				short cost;

				if (road != 0 && (this.Owner.MapOperations.ObtainTileImprovements(nextHorizontal, nextVertical) & 8) != 0)
				{
					cost = 1;
				}
				else
				{
					ushort unitKind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

					cost = (short)Tables.UnitKindWordAt(unitKind, UnitKindTable.ShiftTally) <= 1 ? (short)3 : (short)(3 * (sbyte)Tables.TerrainField(terrain, TerrainTable.MoveCost));
				}

				cost += (short)((nextDistance << 2) + Math.Abs(leftHorizontal) + Math.Abs(leftVertical));

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
				this.Troops.AssignAt(troop, UnitMemory.TravelHorizontal, 0xff);

				return 0xffff;
			}

			this.Troops.AssignAt(troop, UnitMemory.TravelHeading, (byte)best);

			return (ushort)best;
		}

		private bool MayGotoTile(short participantIndex, ushort troop, short horizontalLocation, short verticalLocation, ushort occupied, out ushort terrain)
		{
			terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			ushort ownerIndex = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

			bool allowed = false;

			if (ownerIndex == 0xffff || ownerIndex == (ushort)participantIndex)
			{
				ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind));

				if ((Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2) == (terrain == 0xa) &&
					(occupied == 0 || this.Owner.UnitOperations.MayOccupyTile(participantIndex, horizontalLocation, verticalLocation) == 0))
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
				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0 ||
					this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation) != (ushort)participantIndex)
				{
					return false;
				}
			}

			if (terrain == 0xa && (short)this.Owner.MapOperations.ObtainLandMassSize(horizontalLocation, verticalLocation) < 5)
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
			short nextColumn = (short)(Tables.DirectionHorizontal(direction) + column);

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

		private void Enqueue(byte tail, int horizontalLocation, int verticalLocation)
		{
			this.Processor.EmitGlobalByte((ushort)(tail + QueueHorizontalAddress), (byte)horizontalLocation);
			this.Processor.EmitGlobalByte((ushort)(tail + QueueVerticalAddress), (byte)verticalLocation);
		}

		private short QueuedHorizontal(byte head) => (sbyte)this.Processor.PullGlobalByte((ushort)(head + QueueHorizontalAddress));

		private short QueuedVertical(byte head) => (sbyte)this.Processor.PullGlobalByte((ushort)(head + QueueVerticalAddress));

		private bool SearchRouteBlocks(ushort troop, short unitColumn, short unitRow)
		{
			this.Owner.Runtime.FillBytes(BlockStepsAddress, 0, 0x104);

			byte tail = 0, head = 0;

			Enqueue(tail, Search.GoalHorizontal, Search.GoalVertical);
			tail++;
			this.Processor.EmitGlobalByte(BlockStepsCell(Search.GoalHorizontal, Search.GoalVertical), 1);

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
				column = QueuedHorizontal(head);
				row = QueuedVertical(head);

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
					nextRow = (short)(Tables.DirectionVertical(direction) + row);
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

			Search.GoalHorizontal = 0xffff;

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
					nextRow = (short)(Tables.DirectionVertical(direction) + unitRow);
					neighbourSet = true;

					short blockSteps = (sbyte)this.Processor.PullGlobalByte(BlockStepsCell(nextColumn, nextRow));

					if (blockSteps == 0 || blockSteps > fewest)
					{
						continue;
					}

					short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance((sbyte)this.Troops.PullAt(troop, UnitMemory.TravelHorizontal), (nextColumn * 4) + 1,
						(sbyte)this.Troops.PullAt(troop, UnitMemory.TravelVertical), (nextRow * 4) + 1);

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

			if (Search.GoalHorizontal == 0xffff)
			{
				Search.GoalHorizontal = this.Troops.PullAt(troop, UnitMemory.TravelHorizontal);
				Search.GoalVertical = this.Troops.PullAt(troop, UnitMemory.TravelVertical);
			}

			return found;
		}

		private void AimRouteStep(short direction, short unitColumn, short unitRow, bool sea)
		{
			Search.GoalHorizontal = (ushort)((((short)Tables.DirectionHorizontal(direction) + unitColumn) << 2) + 1);

			Search.GoalHorizontal = (ushort)TunePosition((short)Search.GoalHorizontal);
			Search.GoalVertical = (ushort)((((short)Tables.DirectionVertical(direction) + unitRow) << 2) + 1);

			if ((this.Owner.MapOperations.ObtainTerrainType((short)Search.GoalHorizontal, (short)Search.GoalVertical) == 0xa) == sea)
			{
				return;
			}

			Search.GoalHorizontal = (ushort)(Search.GoalHorizontal + 1);

			if ((this.Owner.MapOperations.ObtainTerrainType((short)Search.GoalHorizontal, (short)Search.GoalVertical) == 0xa) == sea)
			{
				return;
			}

			Search.GoalVertical = (ushort)(Search.GoalVertical + 1);
		}

		private bool BlockMarked(ushort blocks, short blockHorizontal, short blockVertical)
		{
			return this.Processor.PullGlobalByte((ushort)(blockVertical + (0xd * blockHorizontal) + blocks)) != 0;
		}

		private bool CornerMatches(short horizontalLocation, short verticalLocation, ushort marker)
		{
			return (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 0xa ? 1 : 0) == marker;
		}

		private void ChartWindowCosts(short participantIndex, short troopIndex, ushort costLimit, short unitHorizontal, short unitVertical, short windowHorizontal, short windowVertical)
		{
			Search.RoutePlaceHorizontal = Search.GoalHorizontal;
			Search.RoutePlaceVertical = Search.GoalVertical;

			this.Owner.Runtime.FillBytes(CostWindowAddress, 0, 0x100);

			byte tail = 0, head = 0;

			Enqueue(tail, Search.GoalHorizontal, Search.GoalVertical);
			tail++;
			this.Processor.EmitGlobalByte(WindowCell(windowHorizontal, windowVertical, (short)Search.GoalHorizontal, (short)Search.GoalVertical), 1);

			Search.GotoCost = costLimit;

			ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt((ushort)(0xc * troopIndex) + (ushort)(0x600 * participantIndex), UnitMemory.Kind));

			bool sea = Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2;
			bool air = Tables.UnitKindWordAt(kind, UnitKindTable.ShiftTally) == 1;

			do
			{
				short horizontalLocation = QueuedHorizontal(head);
				short verticalLocation = QueuedVertical(head);
				head++;

				short cost = (sbyte)this.Processor.PullGlobalByte(WindowCell(windowHorizontal, windowVertical, horizontalLocation, verticalLocation));

				if (cost > (short)Search.GotoCost)
				{
					continue;
				}

				if ((short)TunePosition(horizontalLocation) == unitHorizontal && verticalLocation == unitVertical)
				{
					Search.GotoCost = (ushort)cost;
					continue;
				}

				tail = ExpandWindowCell(horizontalLocation, verticalLocation, cost, tail, sea, air, windowHorizontal, windowVertical);
			}
			while (head != tail && (short)head < 0xe1);
		}

		private byte ExpandWindowCell(short horizontalLocation, short verticalLocation, short cost, byte tail, bool sea, bool air, short windowHorizontal, short windowVertical)
		{
			ushort road = this.Owner.MapOperations.ContainsRoad((short)(ushort)TunePosition(horizontalLocation), verticalLocation);

			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				short nextHorizontal = (short)(Tables.DirectionHorizontal(direction) + horizontalLocation);

				if (this.Owner.Runtime.AbsoluteValue((short)(nextHorizontal - Search.GoalHorizontal)) >= 8)
				{
					continue;
				}

				short tileHorizontal = (short)TunePosition(nextHorizontal);

				short nextVertical = (short)(Tables.DirectionVertical(direction) + verticalLocation);

				if (this.Owner.Runtime.AbsoluteValue((short)(nextVertical - Search.GoalVertical)) >= 8)
				{
					continue;
				}

				if (this.Owner.MapOperations.CheckMapLimits(tileHorizontal, nextVertical) == 0)
				{
					continue;
				}

				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, nextVertical);

				if ((terrain == 0xa) != sea && (this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, nextVertical) & 1) == 0)
				{
					continue;
				}

				short nextCost;

				if (road != 0 && this.Owner.MapOperations.ContainsRoad(tileHorizontal, nextVertical) != 0)
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

				ushort cell = WindowCell(windowHorizontal, windowVertical, nextHorizontal, nextVertical);
				short known = (sbyte)this.Processor.PullGlobalByte(cell);

				if (known != 0 && known <= nextCost)
				{
					continue;
				}

				this.Processor.EmitGlobalByte(cell, (byte)nextCost);
				Enqueue(tail, nextHorizontal, nextVertical);
				tail++;
			}

			return tail;
		}

		private short ChooseCheapestStep(short participantIndex, short troopIndex, short unitHorizontal, short unitVertical, short windowHorizontal, short windowVertical)
		{
			short lowest = 0x63;
			short best = -1;
			short bestCrowdAndDistance = 0;
			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				short nextHorizontal = (short)(Tables.DirectionHorizontal(direction) + unitHorizontal);
				short place = (short)Search.GoalHorizontal;

				if (this.Owner.Runtime.AbsoluteValue((short)(nextHorizontal - place)) >= 0x48)
				{
					nextHorizontal = (short)(nextHorizontal <= place ? nextHorizontal + 0x50 : nextHorizontal - 0x50);
				}

				if (this.Owner.Runtime.AbsoluteValue((short)(nextHorizontal - Search.GoalHorizontal)) >= 8)
				{
					continue;
				}

				short tileHorizontal = (short)TunePosition(nextHorizontal);

				short nextVertical = (short)(Tables.DirectionVertical(direction) + unitVertical);

				if (this.Owner.Runtime.AbsoluteValue((short)(nextVertical - Search.GoalVertical)) >= 8)
				{
					continue;
				}

				bool seaTile = this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, nextVertical) == 0xa;

				ushort kind = (ushort)(0x22 * (sbyte)this.Troops.PullAt((ushort)(0xc * troopIndex) + (ushort)(0x600 * participantIndex), UnitMemory.Kind));

				if ((Tables.UnitKindWordAt(kind, UnitKindTable.LandGroup) == 2) != seaTile &&
					(this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, nextVertical) & 1) == 0)
				{
					continue;
				}

				short cost = (sbyte)this.Processor.PullGlobalByte(WindowCell(windowHorizontal, windowVertical, nextHorizontal, nextVertical));

				if (cost == 0)
				{
					continue;
				}

				if (cost < lowest)
				{
					lowest = cost;
					best = direction;
					bestCrowdAndDistance = CrowdAndDistance(participantIndex, tileHorizontal, nextVertical);
				}

				if (cost != lowest)
				{
					continue;
				}

				short alternative = CrowdAndDistance(participantIndex, tileHorizontal, nextVertical);

				if (alternative < bestCrowdAndDistance)
				{
					best = direction;
					bestCrowdAndDistance = alternative;
				}
			}

			return best;
		}

		private short CrowdAndDistance(short participantIndex, short horizontalLocation, short verticalLocation)
		{
			ushort troopIndex = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

			short crowd = troopIndex != 0xffff
				? (short)(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)troopIndex, 2) << 2)
				: (short)0;

			return (short)(crowd + this.Owner.Helpers.ObtainDistance((short)Search.GoalHorizontal, horizontalLocation, (short)Search.GoalVertical, verticalLocation));
		}
	}
}
