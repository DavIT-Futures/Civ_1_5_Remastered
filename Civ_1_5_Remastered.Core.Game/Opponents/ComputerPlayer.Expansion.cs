namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private const ushort FoundCityOrder = 0x62;
		private const ushort FollowGotoOrder = 0;
		private const ushort WaitOrder = 0x20;

		/// <summary>
		/// Remastered settler behaviour of the computer: found a city on the best free site nearby, walk there,
		/// or wait in a harbour for a colony ship when the own landmass is full.
		/// </summary>
		/// <returns>The order, or <c>null</c> to fall back to the original settler behaviour (terrain work, joining a city).</returns>
		private ushort? PlanSettlerExpansion(short participantIndex, short troopIndex)
		{
			if (!ComputerExpansion.Controls(this.Owner, participantIndex))
			{
				return null;
			}

			Player player = this.Owner.World.Participants[participantIndex];
			Point here = new Point((short)this.look.Horizontal, (short)this.look.Vertical);
			GameCitySiteMap map = new GameCitySiteMap(this.Owner);

			if (map.Terrain(here.X, here.Y) == MapRules.OceanTerrain)
			{
				return LandFromShip(map, here);
			}

			if (player.TownTally == 0)
			{
				if (CitySitePlanner.IsCityRow(here.Y))
				{
					return FoundCityOrder;
				}

				// No city stands in the first or last two rows of the map: settlers that begin there
				// (a start at the very edge) first step to the nearest land where one does.
				Point? inside = CitySitePlanner.NearestCityRowLand(map, here, 4);

				return inside == null ? null : Travel(participantIndex, troopIndex, inside.Value);
			}

			Point? site = ComputerExpansion.SiteFrom(this.Owner, this.troops, participantIndex, troopIndex, here);

			if (site == here)
			{
				return FoundCityOrder;
			}

			if (site != null)
			{
				return StepOutOfZoneOfControl(participantIndex, map, here, site.Value) ?? Travel(participantIndex, troopIndex, site.Value);
			}

			if (!ComputerExpansion.Expands(this.Owner, participantIndex) || ComputerExpansion.ColonyShipKind(this.Owner, participantIndex) < 0)
			{
				return null;
			}

			short harbourIndex = ComputerExpansion.NearestHarbour(this.Owner, participantIndex, here, map.Continent(here.X, here.Y));

			if (harbourIndex < 0)
			{
				return null;
			}

			Point harbour = this.Owner.World.Towns[harbourIndex].Place;

			if (ComputerExpansion.OverseasSiteFrom(this.Owner, this.troops, participantIndex, troopIndex, harbour, map.Continent(here.X, here.Y)) == null)
			{
				return null;
			}

			if (harbour == here)
			{
				// Stay unfortified, so the colony ship leaving the city takes the settlers along.
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status,
					(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & ~(int)(UnitStatus.Fortifying | UnitStatus.Fortified)));

				return WaitOrder;
			}

			return Travel(participantIndex, troopIndex, harbour);
		}

		/// <summary>
		/// A settler next to a foreign unit is in its zone of control: it may not step onto another tile next to a
		/// foreign unit, and the way to its site (GoTo knows nothing of zones) tried that step every turn, forever. It
		/// steps first onto the free tile around it nearest the site: one out of every zone, a city, or one with its
		/// own units. With no such tile it founds its city where it stands, when it may.
		/// </summary>
		/// <returns>The direction to step, the order to found a city, or <c>null</c> when the settler is free to go.</returns>
		private ushort? StepOutOfZoneOfControl(short participantIndex, GameCitySiteMap map, Point here, Point site)
		{
			if (this.Owner.UnitOperations.MayOccupyTile(participantIndex, here.X, here.Y) == 0)
			{
				return null;
			}

			bool europe = EuropeMap.Played(this.Owner.World.Extras.World);
			System.Collections.Generic.List<(int Direction, Point Tile, bool Free)> steps = new();

			for (ushort direction = 1; direction <= 8; direction++)
			{
				int column = here.X + (short)Tables.DirectionHorizontal(direction);
				Point next = new Point(this.Owner.Routing.TunePosition(column), here.Y + (short)Tables.DirectionVertical(direction));

				if (next.Y < 0 || next.Y >= MapRules.Height || (europe && EuropeMap.CrossesEdge(here.X, next.X)) ||
					map.Terrain(next.X, next.Y) == MapRules.OceanTerrain)
				{
					continue;
				}

				ushort unit = this.Owner.MapOperations.ObtainUnitAtTile(next.X, next.Y);
				bool own = unit != 0xffff && Search.FoundPlayer == (ushort)participantIndex;
				bool free = own || (unit == 0xffff && this.Owner.UnitOperations.MayOccupyTile(participantIndex, next.X, next.Y) == 0);

				steps.Add((direction, next, free));
			}

			int best = CitySitePlanner.StepTowards(steps, site);

			if (best != 0)
			{
				return (ushort)best;
			}

			return CitySitePlanner.IsValidSite(map, here, ComputerExpansion.Cities(this.Owner)) ? FoundCityOrder : null;
		}

		/// <summary>A settler woken on a ship next to land steps ashore, preferring a free city site.</summary>
		private ushort LandFromShip(GameCitySiteMap map, Point here)
		{
			System.Collections.Generic.List<Point> cities = ComputerExpansion.Cities(this.Owner);
			ushort fallback = WaitOrder;

			for (ushort direction = 1; direction <= 8; direction++)
			{
				Point next = new Point(
					this.Owner.Routing.TunePosition(here.X + (short)Tables.DirectionHorizontal(direction)),
					here.Y + (short)Tables.DirectionVertical(direction));

				if (next.Y < 0 || next.Y >= MapRules.Height || map.Terrain(next.X, next.Y) == MapRules.OceanTerrain)
				{
					continue;
				}

				if (CitySitePlanner.IsValidSite(map, next, cities))
				{
					return direction;
				}

				fallback = direction;
			}

			return fallback;
		}

		/// <summary>
		/// Remastered colony ship of the computer (Prince and up): carries waiting settlers from a harbour to a free
		/// coastal site on another island, lets them land and sails back.
		/// </summary>
		private ushort? DirectColonyShip(short participantIndex, short troopIndex)
		{
			if (!ComputerExpansion.Expands(this.Owner, participantIndex) || !ShipRules.IsCivilianShip(this.look.Kind))
			{
				return null;
			}

			GameCitySiteMap map = new GameCitySiteMap(this.Owner);
			Point here = new Point((short)this.look.Horizontal, (short)this.look.Vertical);
			System.Collections.Generic.List<short> settlers = ComputerExpansion.UnitsAt(this.troops, participantIndex, here, ComputerExpansion.SettlersKind);
			bool atSea = map.Terrain(here.X, here.Y) == MapRules.OceanTerrain;
			bool travelling = this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff;

			if (atSea && settlers.Count > 0)
			{
				if (NextToFreeSite(map, here))
				{
					foreach (short settler in settlers)
					{
						this.troops.Assign(participantIndex, settler, UnitMemory.Status,
							(byte)(this.troops.Pull(participantIndex, settler, UnitMemory.Status) & ~(int)UnitStatus.Sentry));
					}

					return WaitOrder;
				}

				if (travelling)
				{
					return FollowGotoOrder;
				}

				Point? site = ComputerExpansion.OverseasSiteFrom(this.Owner, this.troops, participantIndex, troopIndex, here, -1);
				Point? water = site == null ? null : CitySitePlanner.LandingWater(map, site.Value, here);

				return water == null ? null : Travel(participantIndex, troopIndex, water.Value);
			}

			if (atSea)
			{
				if (travelling)
				{
					return FollowGotoOrder;
				}

				// An empty ship sails to colonists waiting in a harbour; otherwise (also troop transports of an
				// invasion) it keeps the original behaviour.
				Point? waiting = HarbourWithColonists(participantIndex, here);

				return waiting == null ? null : Travel(participantIndex, troopIndex, waiting.Value);
			}

			int continent = map.Continent(here.X, here.Y);

			if (settlers.Count > 0)
			{
				Point? site = ComputerExpansion.OverseasSiteFrom(this.Owner, this.troops, participantIndex, troopIndex, here, continent);
				Point? water = site == null ? null : CitySitePlanner.LandingWater(map, site.Value, here);

				if (water != null)
				{
					return Travel(participantIndex, troopIndex, water.Value);
				}
			}

			// Wait in the harbour for colonists while the landmass is full and islands are free.
			if ((this.Owner.MapOperations.ObtainTileImprovements(here.X, here.Y) & 1) != 0 &&
				ComputerExpansion.SiteFrom(this.Owner, this.troops, participantIndex, -1, here) == null &&
				ComputerExpansion.OverseasSiteFrom(this.Owner, this.troops, participantIndex, troopIndex, here, continent) != null)
			{
				return WaitOrder;
			}

			return null;
		}

		private Point? HarbourWithColonists(short participantIndex, Point from)
		{
			Point? best = null;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex || (town.ConditionMarker & 2) == 0 ||
					ComputerExpansion.UnitsAt(this.troops, participantIndex, town.Place, ComputerExpansion.SettlersKind).Count == 0)
				{
					continue;
				}

				if (best == null || CitySitePlanner.Distance(from, town.Place) < CitySitePlanner.Distance(from, best.Value))
				{
					best = town.Place;
				}
			}

			return best;
		}

		private bool NextToFreeSite(GameCitySiteMap map, Point here)
		{
			System.Collections.Generic.List<Point> cities = ComputerExpansion.Cities(this.Owner);

			foreach (Point next in CitySitePlanner.Around(here, 1))
			{
				if (map.Terrain(next.X, next.Y) != MapRules.OceanTerrain && CitySitePlanner.IsValidSite(map, next, cities))
				{
					return true;
				}
			}

			return false;
		}

		private ushort Travel(short participantIndex, short troopIndex, Point target)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)target.X);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)target.Y);

			return FollowGotoOrder;
		}
	}
}
