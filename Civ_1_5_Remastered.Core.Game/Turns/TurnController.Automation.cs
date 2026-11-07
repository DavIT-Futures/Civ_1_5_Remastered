using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private const int AutomationTriesPerTurn = 6;

		private readonly Dictionary<(short Participant, short Troop), int> automationTries = new Dictionary<(short Participant, short Troop), int>();
		private readonly Dictionary<int, List<Point>> connectionPaths = new Dictionary<int, List<Point>>();
		private int automationRound = -1;

		private bool IsAutomatedSettler(short participantIndex, short troopIndex) =>
			troopIndex < UnitMemory.TroopTally &&
			this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0 &&
			this.Owner.World.Extras.AutomationMode(participantIndex, troopIndex, 0) != SettlerAutomation.None;

		/// <summary>Asks how a settler should be automated (order 'a').</summary>
		private void AutomateSettler(short participantIndex)
		{
			ChooseSettlerAutomation(participantIndex);

			// The menus are drawn over the map; repaint it so they disappear at once.
			this.Owner.TurnHooks.RepaintMainScreen();
		}

		private void ChooseSettlerAutomation(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
			{
				return;
			}

			string text = "Automate Settlers:\n Build roads only\n Improve everything\n Improve, keep existing work\n" +
				" Clean up pollution\n Connect cities (roads, railroads)\n";

			Menu.DisabledLines = 0;
			ushort choice = this.Owner.Dialogs.ShowMenu(text, 72, 40, 1);

			if (choice == 0xffff || choice > 4)
			{
				return;
			}

			int townIndex = RemasterExtras.Anywhere;
			short nearest = NearestOwnTown(participantIndex, troopIndex);

			if (nearest >= 0)
			{
				string city = this.Owner.World.TownLabels[this.Owner.World.Towns[nearest].LabelIndex].Split('\0')[0].Trim();
				string where = "Where should they work?\n Around all our cities\n Only around " + city + "\n";

				Menu.DisabledLines = 0;
				ushort area = this.Owner.Dialogs.ShowMenu(where, 72, 40, 1);

				if (area == 0xffff)
				{
					return;
				}

				townIndex = area == 1 ? nearest : RemasterExtras.Anywhere;
			}

			this.Owner.World.Extras.AssignAutomation(participantIndex, troopIndex, 0, (SettlerAutomation)(choice + 1), townIndex);
			this.automationTries.Remove((participantIndex, troopIndex));
		}

		/// <summary>
		/// Gives an automated settler its next order: work on the current tile, or a GoTo to the next tile worth working.
		/// </summary>
		/// <returns><c>false</c> when there is nothing left to do and the settler is handed back to the player.</returns>
		private bool DirectAutomatedSettler(short participantIndex, short troopIndex)
		{
			SettlerAutomation mode = this.Owner.World.Extras.AutomationMode(participantIndex, troopIndex, 0);

			if (this.automationRound != this.Owner.World.RoundTally)
			{
				this.automationRound = this.Owner.World.RoundTally;
				this.automationTries.Clear();
				this.connectionPaths.Clear();
			}

			int tries = this.automationTries.GetValueOrDefault((participantIndex, troopIndex)) + 1;
			this.automationTries[(participantIndex, troopIndex)] = tries;

			Point settler = new Point(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			if (tries > AutomationTriesPerTurn || this.Owner.MapOperations.ObtainTerrainType(settler.X, settler.Y) == MapRules.OceanTerrain)
			{
				this.turn.Order = KeyCodes.Space;
				return true;
			}

			GameSettlerMap map = new GameSettlerMap(this.Owner, this.Tables);
			List<Point> cities = new List<Point>();

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker != 0xff && town.ParticipantIndex == participantIndex)
				{
					cities.Add(town.Place);
				}
			}

			int homeIndex = this.Owner.World.Extras.AutomationTown(participantIndex, troopIndex, 0);
			Point? home = null;

			if (homeIndex != RemasterExtras.Anywhere)
			{
				City homeTown = this.Owner.World.Towns[homeIndex];

				if (homeTown.ConditionMarker == 0xff || homeTown.ParticipantIndex != participantIndex)
				{
					this.Owner.World.Extras.ClearAutomation(participantIndex, troopIndex);

					Message("Automated Settlers lost\ntheir city. Awaiting orders.\n")
						.Style(0x1)
						.Dialog(100, 80);

					return false;
				}

				home = homeTown.Place;
			}

			bool knowsBridge = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x13) != 0;
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RailroadRules.RailroadTechnology) != 0;
			List<Point>? path = null;

			if (mode == SettlerAutomation.ConnectCities)
			{
				int continent = map.Continent(settler.X, settler.Y);
				int key = (participantIndex << 16) | (continent << 8) | (homeIndex & 0xff);

				if (!this.connectionPaths.TryGetValue(key, out path))
				{
					path = home == null
						? SettlerPlanner.ConnectionPath(map, cities, continent, knowsBridge, knowsRailroad)
						: SettlerPlanner.ConnectionPath(map, NearestPair(home.Value, cities, map, continent), continent, knowsBridge, knowsRailroad);
					this.connectionPaths[key] = path;
				}
			}
			else if (home != null)
			{
				cities = new List<Point> { home.Value };
			}

			(Point Tile, SettlerWork Work)? plan = SettlerPlanner.Plan(mode, map, settler, cities, ReservedSettlerTiles(participantIndex, troopIndex),
				knowsBridge, knowsRailroad, path);

			if (plan == null)
			{
				this.Owner.World.Extras.ClearAutomation(participantIndex, troopIndex);
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, settler.X, settler.Y);

				Message("Automated Settlers\nhave no more work.\nAwaiting orders.\n")
					.Style(0x1)
					.Dialog(100, 80);

				return false;
			}

			if (plan.Value.Tile == settler)
			{
				this.turn.Order = plan.Value.Work switch
				{
					SettlerWork.Irrigate => 'i',
					SettlerWork.Mine => 'm',
					SettlerWork.CleanPollution => 'p',
					_ => 'r',
				};

				return true;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)plan.Value.Tile.X);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)plan.Value.Tile.Y);
			this.turn.Order = KeyCodes.Space;

			return true;
		}

		/// <summary>A city and its nearest neighbour on the same continent: the route a city-bound settler connects.</summary>
		private List<Point> NearestPair(Point home, List<Point> cities, GameSettlerMap map, int continent)
		{
			List<Point> pair = new List<Point> { home };
			Point? nearest = null;

			foreach (Point city in cities)
			{
				if (city == home || map.Continent(city.X, city.Y) != continent)
				{
					continue;
				}

				if (nearest == null || SettlerPlanner.Distance(home, city) < SettlerPlanner.Distance(home, nearest.Value))
				{
					nearest = city;
				}
			}

			if (nearest != null)
			{
				pair.Add(nearest.Value);
			}

			return pair;
		}

		private short NearestOwnTown(short participantIndex, short troopIndex)
		{
			Point settler = new Point(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));
			int continent = this.Owner.MapOperations.ObtainContinentIdentifier(settler.X, settler.Y);
			short nearest = -1;
			int shortest = int.MaxValue;

			for (short townIndex = 0; townIndex < RemasterExtras.TownTally; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex ||
					this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) != continent)
				{
					continue;
				}

				int distance = SettlerPlanner.Distance(settler, town.Place);

				if (distance < shortest)
				{
					shortest = distance;
					nearest = townIndex;
				}
			}

			return nearest;
		}

		private HashSet<Point> ReservedSettlerTiles(short participantIndex, short troopIndex)
		{
			HashSet<Point> reserved = new HashSet<Point>();

			for (short otherIndex = 0; otherIndex < UnitMemory.TroopTally; otherIndex++)
			{
				if (otherIndex == troopIndex || !IsAutomatedSettler(participantIndex, otherIndex))
				{
					continue;
				}

				byte travelHorizontal = this.troops.Pull(participantIndex, otherIndex, UnitMemory.TravelHorizontal);

				reserved.Add(travelHorizontal != 0xff
					? new Point(travelHorizontal, this.troops.Pull(participantIndex, otherIndex, UnitMemory.TravelVertical))
					: new Point(this.troops.PullSigned(participantIndex, otherIndex, UnitMemory.Horizontal), this.troops.PullSigned(participantIndex, otherIndex, UnitMemory.Vertical)));
			}

			return reserved;
		}
	}
}
