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

			this.Owner.World.Extras.AssignAutomation(participantIndex, troopIndex, 0, (SettlerAutomation)(choice + 1));
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

			bool knowsBridge = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x13) != 0;
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RailroadRules.RailroadTechnology) != 0;
			List<Point>? path = null;

			if (mode == SettlerAutomation.ConnectCities)
			{
				int continent = map.Continent(settler.X, settler.Y);
				int key = (participantIndex << 8) | continent;

				if (!this.connectionPaths.TryGetValue(key, out path))
				{
					path = SettlerPlanner.ConnectionPath(map, cities, continent, knowsBridge, knowsRailroad);
					this.connectionPaths[key] = path;
				}
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
