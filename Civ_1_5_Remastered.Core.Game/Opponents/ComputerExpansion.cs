using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	/// <summary>
	/// Remastered expansion of the computer civilizations: where settlers found cities, how many cities and settlers
	/// a civilization wants on each difficulty level, and which ship carries colonists to other islands.
	/// </summary>
	/// <remarks>
	/// Every computer civilization chooses city sites with <see cref="CitySitePlanner"/>. From Prince up it also
	/// builds settlers with a high priority until it reaches its city target, and colonizes islands by sea when its
	/// own landmass is full. Chieftain and Warlord keep the original, mild expansion.
	/// </remarks>
	internal static class ComputerExpansion
	{
		public const int SettlersKind = 0;
		public const int SiteRange = 20;
		public const int OverseasRange = 30;

		private const int ExpansionField = 0x32;

		public static bool Controls(IGameEngine owner, short participantIndex) =>
			participantIndex > 0 && participantIndex < 8 && !owner.World.IsHuman(participantIndex);

		/// <summary>Prince, King and Emperor: settlers get priority and islands are colonized.</summary>
		public static bool Expands(IGameEngine owner, short participantIndex) =>
			Controls(owner, participantIndex) && owner.World.HardnessTier >= 2;

		public static int CityTarget(IGameEngine owner, short participantIndex)
		{
			int target = owner.World.HardnessTier switch
			{
				2 => 10,
				3 => 16,
				_ => 24,
			};

			short trait = (short)owner.Tables.LeaderWord(owner.World.Participants[participantIndex].ChiefVisuals, ExpansionField);

			return System.Math.Max(4, target + (2 * System.Math.Clamp((int)trait, -2, 2)));
		}

		public static int SettlerLimit(IGameEngine owner, int towns) => owner.World.HardnessTier switch
		{
			2 => 1 + (towns / 4),
			3 => 1 + (towns / 3),
			_ => 2 + (towns / 3),
		};

		public static List<Point> Cities(IGameEngine owner)
		{
			List<Point> cities = new List<Point>();

			foreach (City town in owner.World.Towns)
			{
				if (town.ConditionMarker != 0xff)
				{
					cities.Add(town.Place);
				}
			}

			return cities;
		}

		/// <summary>Sites already chosen by other settlers or colony ships of the civilization.</summary>
		public static HashSet<Point> Reserved(IGameEngine owner, UnitMemory troops, short participantIndex, short exceptTroop)
		{
			HashSet<Point> reserved = new HashSet<Point>();

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				byte kind = troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);
				byte travel = troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal);

				if (troopIndex == exceptTroop || kind == 0xff || travel == 0xff)
				{
					continue;
				}

				Point target = new Point(travel, troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical));

				if (kind == SettlersKind)
				{
					reserved.Add(target);
				}
				else if (ShipRules.IsCivilianShip(kind))
				{
					foreach (Point near in CitySitePlanner.Around(target, 1))
					{
						reserved.Add(near);
					}
				}
			}

			return reserved;
		}

		public static Point? SiteFrom(IGameEngine owner, UnitMemory troops, short participantIndex, short troopIndex, Point from) =>
			CitySitePlanner.FindSite(new GameCitySiteMap(owner), from, Cities(owner), Reserved(owner, troops, participantIndex, troopIndex), SiteRange);

		public static Point? OverseasSiteFrom(IGameEngine owner, UnitMemory troops, short participantIndex, short troopIndex, Point harbour, int homeContinent) =>
			CitySitePlanner.FindOverseasSite(new GameCitySiteMap(owner), harbour, homeContinent, Cities(owner),
				Reserved(owner, troops, participantIndex, troopIndex), OverseasRange);

		/// <summary>The best civilian ship the civilization can build to carry colonists, or -1.</summary>
		public static int ColonyShipKind(IGameEngine owner, short participantIndex)
		{
			foreach (int kind in new[] { ShipRules.Transport, ShipRules.Sail, ShipRules.Trireme })
			{
				if (owner.Advisors.PlayerKnowsTechnology(participantIndex, (short)owner.Tables.UnitKindWord(kind, UnitKindTable.Technology)) != 0 &&
					owner.Advisors.PlayerKnowsTechnology(participantIndex, (short)owner.Tables.UnitKindWord(kind, 0xc)) == 0)
				{
					return kind;
				}
			}

			return -1;
		}

		public static int CivilianShips(IGameEngine owner, short participantIndex)
		{
			Player player = owner.World.Participants[participantIndex];
			int ships = 0;

			foreach (int kind in new[] { ShipRules.Trireme, ShipRules.Sail, ShipRules.Transport })
			{
				ships += player.LiveTroops[kind] + player.TroopsInsideMaking[kind];
			}

			return ships;
		}

		public static List<short> UnitsAt(UnitMemory troops, short participantIndex, Point place, int kind)
		{
			List<short> found = new List<short>();

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				byte present = troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

				if (present != 0xff && (kind < 0 || present == kind) &&
					troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) == place.X &&
					troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical) == place.Y)
				{
					found.Add(troopIndex);
				}
			}

			return found;
		}

		public static bool HasDefender(IGameEngine owner, UnitMemory troops, short participantIndex, Point city)
		{
			foreach (short troopIndex in UnitsAt(troops, participantIndex, city, -1))
			{
				sbyte kind = troops.TroopKind(participantIndex, troopIndex);

				if (kind != SettlersKind && owner.Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					owner.Tables.UnitKindWord(kind, UnitKindTable.GuardPower) > 0)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Nearest own coastal city on the given landmass (or anywhere when <paramref name="continent"/> is -1).</summary>
		public static short NearestHarbour(IGameEngine owner, short participantIndex, Point from, int continent)
		{
			short nearest = -1;
			int shortest = int.MaxValue;

			for (short townIndex = 0; townIndex < owner.World.Towns.Length; townIndex++)
			{
				City town = owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex || (town.ConditionMarker & 2) == 0 ||
					(continent >= 0 && owner.MapOperations.Chart.Continent(town.Place.X, town.Place.Y) != continent))
				{
					continue;
				}

				int distance = CitySitePlanner.Distance(from, town.Place);

				if (distance < shortest)
				{
					shortest = distance;
					nearest = townIndex;
				}
			}

			return nearest;
		}
	}
}
