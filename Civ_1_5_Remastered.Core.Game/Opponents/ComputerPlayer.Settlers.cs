using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private const int BlockedWorkRounds = 16;

		private readonly Dictionary<(short Participant, int Continent), List<Point>> connectionPaths = new Dictionary<(short Participant, int Continent), List<Point>>();
		private readonly Dictionary<(short Participant, short Troop), (Point From, Point Target, int Round)> workTargets =
			new Dictionary<(short Participant, short Troop), (Point From, Point Target, int Round)>();

		private readonly Dictionary<(short Participant, Point Tile), int> blockedWork = new Dictionary<(short Participant, Point Tile), int>();
		private int workRound = -1;

		private ushort? DirectSettlers(short participantIndex, short troopIndex)
		{
			bool worker = ComputerExpansion.Controls(this.Owner, participantIndex) &&
				ComputerExpansion.IsWorker(this.Owner, this.troops, participantIndex, troopIndex);
			ushort? work = worker ? PlanSettlerWork(participantIndex, troopIndex) : null;

			if (work != null)
			{
				return work;
			}

			ushort? expansion = PlanSettlerExpansion(participantIndex, troopIndex);

			// Colonists waiting in a harbour for a ship that is not afloat yet work the land meanwhile.
			if (expansion == WaitOrder && !worker && ComputerExpansion.CivilianShipsAfloat(this.Owner, participantIndex) == 0)
			{
				expansion = PlanSettlerWork(participantIndex, troopIndex) ?? expansion;
			}

			if (expansion != null)
			{
				return expansion;
			}

			// No city site left for these settlers (only foreign land around): they join a city whose land is
			// already well developed, or else improve the land.
			ushort? join = PlanSettlerJoin(participantIndex, troopIndex);

			if (join != null)
			{
				return join;
			}

			work = worker ? null : PlanSettlerWork(participantIndex, troopIndex);

			if (work != null)
			{
				return work;
			}

			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if (this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance >= 2)
				{
					SendUnitToStrategicLocation(participantIndex, horizontalLocation, verticalLocation, 2, 2);
				}

				ushort? improvement = ImproveSettlersTile(participantIndex, troopIndex);

				if (improvement != null)
				{
					return improvement;
				}

				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x40) != 0)
				{
					return 0x70;
				}

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
				{
					ushort continent = this.look.Continent;
					short wish = (short)(Tables.LeaderWord(player.ChiefVisuals, LeaderTable.Aggression + 2) + 1);
					short wanted = (short)MultiplyWords((ushort)wish, (ushort)this.Owner.World.Landmasses[continent].ConstructionSpotTally);
					short maximumTownsScore = (short)(player.Landmasses[continent].TownTally << 4);

					if (wanted > maximumTownsScore)
					{
						return 0;
					}
				}

				if ((short)this.look.CityDistance <= 1 && this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex == participantIndex &&
					(this.Owner.World.DiagonalMarkers & 2) != 0 && ChooseSettlersTile(participantIndex, troopIndex))
				{
					return 0;
				}

				ushort? road = BuildSettlersRoad(participantIndex);

				if (road != null)
				{
					return road;
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				return 0;
			}

			this.look.Round = (ushort)(this.Owner.Runtime.Dice.DrawInteger(20) + 1);
			ushort direction = (ushort)(this.look.Round << 1);
			this.look.TileHorizontal = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) >> 2) +
				Tables.DirectionHorizontalAt(direction));
			this.look.TileVertical = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) >> 2) +
				Tables.DirectionVerticalAt(direction));
			this.Processor.SegmentExtra.Word = 0x3772;

			if ((this.Processor.PullByte(this.Processor.SegmentExtra.Word,
				(ushort)((0xd * this.look.TileHorizontal) + this.look.TileVertical + 0x2438)) & 3) != 0)
			{
				return null;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal,
				(byte)((this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(direction + 0x1882)) << 2) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal)));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical,
				(byte)((this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(direction + 0x18e4)) << 2) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical)));

			short travelHorizontal = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal);
			short travelVertical = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical);

			if (this.Owner.MapOperations.CheckMapLimits(travelHorizontal, travelVertical) != 0)
			{
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(travelHorizontal, travelVertical);

				if (this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((terrain * 0x13) + 0x291)) != 0 &&
					this.Owner.MapOperations.ObtainContinentIdentifier(travelHorizontal, travelVertical) == this.look.Continent)
				{
					return 0;
				}
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			return null;
		}

		/// <summary>
		/// Remastered land work of computer settlers: irrigation, mines and roads around the civilization's cities,
		/// and roads (later railroads) that connect its cities, chosen with <see cref="SettlerPlanner.PlanComputer"/>.
		/// </summary>
		/// <returns>The order (work on this tile, or GoTo to the next tile worth working), or <c>null</c> when there is nothing to do.</returns>
		private ushort? PlanSettlerWork(short participantIndex, short troopIndex)
		{
			if (!ComputerExpansion.Workers || !ComputerExpansion.Controls(this.Owner, participantIndex) ||
				this.Owner.World.Participants[participantIndex].TownTally == 0)
			{
				return null;
			}

			Point here = new Point((short)this.look.Horizontal, (short)this.look.Vertical);
			GameSettlerMap map = new GameSettlerMap(this.Owner, this.Tables);

			if (map.Terrain(here.X, here.Y) == MapRules.OceanTerrain)
			{
				return null;
			}

			int continent = map.Continent(here.X, here.Y);
			List<Point> cities = ComputerExpansion.CitiesOf(this.Owner, participantIndex);
			bool knowsBridge = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, ComputerExpansion.BridgeTechnology) != 0;
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RailroadRules.RailroadTechnology) != 0;

			if (this.workRound != this.Owner.World.RoundTally)
			{
				this.workRound = this.Owner.World.RoundTally;
				this.connectionPaths.Clear();

				foreach (KeyValuePair<(short Participant, Point Tile), int> expired in new List<KeyValuePair<(short Participant, Point Tile), int>>(this.blockedWork))
				{
					if (expired.Value <= this.workRound)
					{
						this.blockedWork.Remove(expired.Key);
					}
				}
			}

			if (!this.connectionPaths.TryGetValue((participantIndex, continent), out List<Point>? path))
			{
				path = SettlerPlanner.ConnectionPath(map, cities, continent, knowsBridge, knowsRailroad);
				this.connectionPaths[(participantIndex, continent)] = path;
			}

			HashSet<Point> reserved = WorkedSettlerTiles(participantIndex, troopIndex);

			foreach (KeyValuePair<(short Participant, Point Tile), int> blocked in this.blockedWork)
			{
				if (blocked.Key.Participant == participantIndex && blocked.Value > this.Owner.World.RoundTally)
				{
					reserved.Add(blocked.Key.Tile);
				}
			}

			ForeignUnitTiles(participantIndex, reserved);

			bool hemmedIn = this.Owner.UnitOperations.MayOccupyTile(participantIndex, here.X, here.Y) != 0;
			(Point Tile, SettlerWork Work)? plan = null;

			for (int attempt = 0; attempt < 3; attempt++)
			{
				plan = SettlerPlanner.PlanComputer(map, here, cities, reserved, knowsBridge, knowsRailroad, path, (troopIndex & 1) != 0);

				if (plan == null || plan.Value.Tile == here)
				{
					break;
				}

				ushort step = StepTo(here, plan.Value.Tile);

				// Next to a foreign unit the settlers may not step onto another tile next to one (zone of control).
				bool closed = step != 0 && hemmedIn && this.Owner.UnitOperations.MayOccupyTile(participantIndex, plan.Value.Tile.X, plan.Value.Tile.Y) != 0 &&
					this.Owner.MapOperations.ObtainUnitAtTile(plan.Value.Tile.X, plan.Value.Tile.Y) == 0xffff;

				if (!closed && !WorkTargetBlocked(participantIndex, troopIndex, here, plan.Value.Tile))
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

					return step != 0 ? step : Travel(participantIndex, troopIndex, plan.Value.Tile);
				}

				reserved.Add(plan.Value.Tile);
				plan = null;
			}

			if (plan == null)
			{
				return null;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			return plan.Value.Work switch
			{
				SettlerWork.Irrigate => 0x69,
				SettlerWork.Mine => 0x6d,
				SettlerWork.CleanPollution => 0x70,
				_ => 0x72,
			};
		}

		/// <summary>
		/// Settlers with no free city site join the nearest own city on their landmass (below size 10) whose land has at
		/// least <see cref="ComputerExpansion.JoinMargin"/> more irrigated or mined tiles than the city has citizens.
		/// </summary>
		/// <returns>The order to join the city, the GoTo there, or <c>null</c> when no city is ready for them.</returns>
		private ushort? PlanSettlerJoin(short participantIndex, short troopIndex)
		{
			if (!ComputerExpansion.Workers || !ComputerExpansion.Controls(this.Owner, participantIndex))
			{
				return null;
			}

			Point here = new Point((short)this.look.Horizontal, (short)this.look.Vertical);
			int continent = this.Owner.MapOperations.ObtainContinentIdentifier(here.X, here.Y);
			Point? best = null;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex ||
					this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) != continent ||
					!ComputerExpansion.ReadyToJoin(this.Owner, town))
				{
					continue;
				}

				if (best == null || CitySitePlanner.Distance(here, town.Place) < CitySitePlanner.Distance(here, best.Value))
				{
					best = town.Place;
				}
			}

			if (best == null)
			{
				return null;
			}

			if (best.Value == here)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

				return FoundCityOrder;
			}

			ushort step = StepTo(here, best.Value);

			if (step != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

				return step;
			}

			return Travel(participantIndex, troopIndex, best.Value);
		}

		/// <summary>The direction of a neighbouring tile, or 0 when the tile is farther.</summary>
		private ushort StepTo(Point here, Point tile)
		{
			for (ushort direction = 1; direction <= 8; direction++)
			{
				if (this.Owner.Routing.TunePosition(here.X + (short)Tables.DirectionHorizontal(direction)) == tile.X &&
					here.Y + (short)Tables.DirectionVertical(direction) == tile.Y)
				{
					return direction;
				}
			}

			return 0;
		}

		/// <summary>Adds the tiles where units of other civilizations stand: settlers cannot work there.</summary>
		private void ForeignUnitTiles(short participantIndex, HashSet<Point> tiles)
		{
			for (short other = 0; other < 8; other++)
			{
				if (other == participantIndex)
				{
					continue;
				}

				for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (this.troops.Pull(other, troopIndex, UnitMemory.Kind) != 0xff)
					{
						tiles.Add(new Point(this.troops.PullSigned(other, troopIndex, UnitMemory.Horizontal), this.troops.PullSigned(other, troopIndex, UnitMemory.Vertical)));
					}
				}
			}
		}

		/// <summary>
		/// Whether the settlers set out for the same tile from the same place in an earlier round too: the way there is
		/// blocked (a foreign unit, its zone of control), so the tile is left alone for a while.
		/// </summary>
		private bool WorkTargetBlocked(short participantIndex, short troopIndex, Point here, Point target)
		{
			int round = this.Owner.World.RoundTally;
			(short, short) key = (participantIndex, troopIndex);

			if (this.workTargets.TryGetValue(key, out (Point From, Point Target, int Round) last) && last.From == here && last.Target == target)
			{
				if (last.Round == round)
				{
					return false;
				}

				this.workTargets.Remove(key);
				this.blockedWork[(participantIndex, target)] = round + BlockedWorkRounds;

				return true;
			}

			this.workTargets[key] = (here, target, round);

			return false;
		}

		/// <summary>Tiles other settlers of the civilization work on or walk to.</summary>
		private HashSet<Point> WorkedSettlerTiles(short participantIndex, short troopIndex)
		{
			HashSet<Point> worked = new HashSet<Point>();

			for (short other = 0; other < UnitMemory.TroopTally; other++)
			{
				if (other == troopIndex || this.troops.Pull(participantIndex, other, UnitMemory.Kind) != ComputerExpansion.SettlersKind)
				{
					continue;
				}

				byte travel = this.troops.Pull(participantIndex, other, UnitMemory.TravelHorizontal);

				if (travel != 0xff)
				{
					worked.Add(new Point(travel, this.troops.Pull(participantIndex, other, UnitMemory.TravelVertical)));
				}
				else if ((this.troops.Pull(participantIndex, other, UnitMemory.Status) & 0xc2) != 0)
				{
					worked.Add(new Point(this.troops.PullSigned(participantIndex, other, UnitMemory.Horizontal), this.troops.PullSigned(participantIndex, other, UnitMemory.Vertical)));
				}
			}

			return worked;
		}

		private ushort SettlersTerrainColumn(short participantIndex)
		{
			return (ushort)((this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 5) != 0 ? 5 : 4) << 1);
		}

		private ushort? ImproveSettlersTile(short participantIndex, short troopIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;
			ushort terrain = this.look.Terrain;

			if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIndex)) == 0 &&
				this.look.Approach != 5)
			{
				return null;
			}

			short distance = (short)this.look.CityDistance;
			GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

			if (distance <= 0 || distance > 2 || town.ParticipantIndex != participantIndex)
			{
				return null;
			}

			if ((sbyte)town.TrueExtent < 3 && terrain == 4 && this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) == 0)
			{
				return null;
			}

			if ((this.Owner.World.DiagonalMarkers & 2) == 0)
			{
				return null;
			}

			ushort cost = this.Owner.TurnLogic.ObtainTileMoveCost(horizontalLocation, verticalLocation);

			if (cost == 1 || cost == 2)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
				return (ushort)(cost == 1 ? 0x69 : 0x6d);
			}

			this.look.Round = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);
			byte improvements = (byte)this.look.Round;

			if ((improvements & 6) != 0 && (improvements & 8) == 0 && (short)terrain <= 2)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
				return 0x72;
			}

			if ((improvements & 0x18) == 0x18 || this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x31) == 0)
			{
				return null;
			}

			short worked = (short)this.Owner.CityOperations.MayCityWorkTile(horizontalLocation, verticalLocation, 1);

			if ((short)((improvements & 8) != 0 ? 1 : 2) > worked)
			{
				return null;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			return 0x72;
		}

		private bool ChooseSettlersTile(short participantIndex, short troopIndex)
		{
			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)(((this.look.Direction + this.Owner.World.RoundTally) & 7) + 1);
				ushort direction = (ushort)(this.look.Round << 1);
				this.look.TileHorizontal = (ushort)(Tables.DirectionHorizontalAt(direction) + this.look.Horizontal);
				this.look.TileVertical = (ushort)(Tables.DirectionVerticalAt(direction) + this.look.Vertical);

				short horizontalLocation = (short)this.look.TileHorizontal;
				short verticalLocation = (short)this.look.TileVertical;

				this.look.TileOwner = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

				if (this.look.TileOwner != 0xffff &&
					((ushort)participantIndex != Search.FoundPlayer ||
						this.Owner.UnitOperations.CountUnitsOfTypeInStack(participantIndex, (short)this.look.TileOwner, 0) != 0))
				{
					continue;
				}

				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

				if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIndex)) == 0 ||
					this.Owner.TurnLogic.ObtainTileMoveCost(horizontalLocation, verticalLocation) == 0)
				{
					continue;
				}

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if ((sbyte)town.TrueExtent < 3 && this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 4 &&
					this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) == 0)
				{
					continue;
				}

				if ((short)(ushort)this.Owner.Helpers.MergeCoordinates((short)(horizontalLocation - town.Place.X), (short)(verticalLocation - town.Place.Y)) > 2)
				{
					continue;
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)this.look.TileHorizontal);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)this.look.TileVertical);

				return true;
			}

			return false;
		}

		private ushort? BuildSettlersRoad(short participantIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 8) != 0)
			{
				return null;
			}

			if (this.look.Terrain == 0xb && this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x13) == 0)
			{
				return null;
			}

			ushort city = this.look.NearestCity;

			if ((city < 128 ? (ushort)this.Owner.World.Towns[city].ParticipantIndex : (ushort)0xffff) != (ushort)participantIndex)
			{
				return null;
			}

			if ((short)this.look.CityDistance <= 2 && (this.look.Terrain == 1 || this.look.Terrain == 2))
			{
				return 0x72;
			}

			this.look.Round = 0;
			this.look.LandUnits = 0;
			this.look.Score = 0;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				short stepHorizontal = (short)Tables.DirectionHorizontalAt(direction);
				short stepVertical = (short)Tables.DirectionVerticalAt(direction);

				if ((this.Owner.MapOperations.ObtainTileImprovements(stepHorizontal + horizontalLocation, stepVertical + verticalLocation) & 8) == 0)
				{
					continue;
				}

				this.look.Score = (ushort)(this.look.Score | (ushort)(1 << (byte)((byte)this.look.Direction - 1)));
				this.look.LandUnits = this.Processor.IncrementWord(this.look.LandUnits);

				if ((this.Owner.MapOperations.ObtainTileImprovements((stepHorizontal * 2) + horizontalLocation, (stepVertical * 2) + verticalLocation) & 8) != 0)
				{
					this.look.Round = 1;
				}
			}

			this.look.Score = (ushort)(this.look.Score & (ushort)((short)this.look.Score >> 4));

			if ((short)this.look.LandUnits < 4)
			{
				if (this.look.Score != 0)
				{
					return 0x72;
				}

				if (this.look.CityDistance == 1 && (short)this.look.Terrain <= 2)
				{
					return 0x72;
				}
			}

			if (this.look.LandUnits == 1 && this.look.Round != 0 &&
				Tables.TerrainField(this.look.Terrain, TerrainTable.MoveCost) == 1)
			{
				return 0x72;
			}

			return null;
		}
	}
}
