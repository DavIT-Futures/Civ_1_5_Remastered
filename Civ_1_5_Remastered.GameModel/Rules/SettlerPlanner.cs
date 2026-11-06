using System;
using System.Collections.Generic;
using System.Drawing;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Decides where automated settlers work. Pure logic, independent of the game engine.</summary>
	public static class SettlerPlanner
	{
		public const int Desert = 0;
		public const int Plains = 1;
		public const int Grassland = 2;
		public const int Hills = 4;
		public const int Mountains = 5;
		public const int River = 11;

		public const int CityBit = 0x01;
		public const int IrrigationBit = 0x02;
		public const int MineBit = 0x04;
		public const int RoadBit = 0x08;
		public const int RailroadBit = 0x10;
		public const int PollutionBit = 0x40;

		private const int PollutionPriority = 100;
		private const int TerrainPriority = 60;
		private const int ConnectRoadPriority = 50;
		private const int ConnectRailPriority = 40;
		private const int RoadPriority = 30;
		private const int DistancePenalty = 3;
		private const int ImpassableCost = 1000;

		public static SettlerWork DesiredWork(SettlerAutomation mode, ISettlerMap map, int horizontalLocation, int verticalLocation,
			bool knowsBridge, bool knowsRailroad, bool onConnectionPath)
		{
			int terrain = map.Terrain(horizontalLocation, verticalLocation);
			int improvements = map.Improvements(horizontalLocation, verticalLocation);

			if (terrain == MapRules.OceanTerrain)
			{
				return SettlerWork.None;
			}

			bool improve = mode is SettlerAutomation.ImproveAll or SettlerAutomation.ImproveKeepExisting;

			if ((improvements & PollutionBit) != 0 && (improve || mode == SettlerAutomation.CleanPollution))
			{
				return SettlerWork.CleanPollution;
			}

			if (mode == SettlerAutomation.ConnectCities)
			{
				return onConnectionPath ? RoadOrRailroad(terrain, improvements, knowsBridge, knowsRailroad) : SettlerWork.None;
			}

			if (improve && (improvements & CityBit) == 0)
			{
				SettlerWork wanted = WantedTerrainWork(map, terrain, horizontalLocation, verticalLocation);
				bool developed = (improvements & (IrrigationBit | MineBit)) != 0;
				bool done = (wanted == SettlerWork.Irrigate && (improvements & IrrigationBit) != 0) ||
					(wanted == SettlerWork.Mine && (improvements & MineBit) != 0);

				if (wanted != SettlerWork.None && !done && (mode == SettlerAutomation.ImproveAll || !developed))
				{
					return wanted;
				}
			}

			if ((mode == SettlerAutomation.RoadsOnly || improve) && (improvements & RoadBit) == 0 && GainsFromRoad(terrain))
			{
				return SettlerWork.Road;
			}

			return SettlerWork.None;
		}

		public static int Priority(SettlerWork work, SettlerAutomation mode) => work switch
		{
			SettlerWork.CleanPollution => PollutionPriority,
			SettlerWork.Irrigate or SettlerWork.Mine => TerrainPriority,
			SettlerWork.Road => mode == SettlerAutomation.ConnectCities || mode == SettlerAutomation.RoadsOnly ? ConnectRoadPriority : RoadPriority,
			SettlerWork.Railroad => ConnectRailPriority,
			_ => 0,
		};

		/// <summary>Tiles of the 21-tile working area of a city.</summary>
		public static IEnumerable<Point> CityArea(Point city)
		{
			for (int vertical = -2; vertical <= 2; vertical++)
			{
				for (int horizontal = -2; horizontal <= 2; horizontal++)
				{
					if (Math.Abs(horizontal) == 2 && Math.Abs(vertical) == 2)
					{
						continue;
					}

					int row = city.Y + vertical;

					if (row < 0 || row >= MapRules.Height)
					{
						continue;
					}

					yield return new Point(Wrap(city.X + horizontal), row);
				}
			}
		}

		public static int Distance(Point first, Point second)
		{
			int horizontal = Math.Abs(first.X - second.X);
			horizontal = Math.Min(horizontal, MapRules.Width - horizontal);

			return Math.Max(horizontal, Math.Abs(first.Y - second.Y));
		}

		/// <summary>
		/// Picks the tile an automated settler should work on next.
		/// </summary>
		/// <returns>The tile and the work to do there, or <c>null</c> when there is nothing left to do.</returns>
		public static (Point Tile, SettlerWork Work)? Plan(SettlerAutomation mode, ISettlerMap map, Point settler, IReadOnlyList<Point> cities,
			ICollection<Point> reserved, bool knowsBridge, bool knowsRailroad, ICollection<Point>? connectionPath = null)
		{
			int continent = map.Continent(settler.X, settler.Y);
			HashSet<Point> candidates = new HashSet<Point>();

			if (mode == SettlerAutomation.ConnectCities)
			{
				foreach (Point tile in connectionPath ?? ConnectionPath(map, cities, continent, knowsBridge, knowsRailroad))
				{
					candidates.Add(tile);
				}
			}
			else
			{
				foreach (Point city in cities)
				{
					if (map.Continent(city.X, city.Y) != continent)
					{
						continue;
					}

					foreach (Point tile in CityArea(city))
					{
						candidates.Add(tile);
					}
				}
			}

			(Point Tile, SettlerWork Work)? best = null;
			int bestScore = int.MinValue;
			int bestDistance = int.MaxValue;

			foreach (Point tile in candidates)
			{
				if (map.Continent(tile.X, tile.Y) != continent || (tile != settler && reserved.Contains(tile)))
				{
					continue;
				}

				SettlerWork work = DesiredWork(mode, map, tile.X, tile.Y, knowsBridge, knowsRailroad, mode == SettlerAutomation.ConnectCities);

				if (work == SettlerWork.None)
				{
					continue;
				}

				int distance = Distance(settler, tile);
				int score = Priority(work, mode) - (DistancePenalty * distance);

				if (score > bestScore || (score == bestScore && distance < bestDistance))
				{
					best = (tile, work);
					bestScore = score;
					bestDistance = distance;
				}
			}

			return best;
		}

		/// <summary>
		/// Tiles of the road network that links the cities of one continent: a minimum spanning tree
		/// of the cities, each edge following the cheapest land route (existing roads and rails are preferred).
		/// </summary>
		public static List<Point> ConnectionPath(ISettlerMap map, IReadOnlyList<Point> cities, int continent, bool knowsBridge, bool knowsRailroad)
		{
			List<Point> local = new List<Point>();

			foreach (Point city in cities)
			{
				if (map.Continent(city.X, city.Y) == continent)
				{
					local.Add(city);
				}
			}

			List<Point> path = new List<Point>();

			if (local.Count < 2)
			{
				return path;
			}

			bool[] joined = new bool[local.Count];
			joined[0] = true;

			for (int added = 1; added < local.Count; added++)
			{
				int from = -1;
				int to = -1;
				int shortest = int.MaxValue;

				for (int first = 0; first < local.Count; first++)
				{
					if (!joined[first])
					{
						continue;
					}

					for (int second = 0; second < local.Count; second++)
					{
						if (joined[second])
						{
							continue;
						}

						int distance = Distance(local[first], local[second]);

						if (distance < shortest)
						{
							shortest = distance;
							from = first;
							to = second;
						}
					}
				}

				joined[to] = true;

				foreach (Point tile in FindRoute(map, local[from], local[to], continent, knowsBridge))
				{
					path.Add(tile);
				}
			}

			return path;
		}

		/// <summary>Cheapest land route between two tiles (A*), both ends included; empty when there is none.</summary>
		public static List<Point> FindRoute(ISettlerMap map, Point start, Point goal, int continent, bool knowsBridge)
		{
			Dictionary<Point, int> cost = new Dictionary<Point, int> { [start] = 0 };
			Dictionary<Point, Point> previous = new Dictionary<Point, Point>();
			PriorityQueue<Point, int> open = new PriorityQueue<Point, int>();
			open.Enqueue(start, 0);

			while (open.TryDequeue(out Point current, out _))
			{
				if (current == goal)
				{
					break;
				}

				for (int vertical = -1; vertical <= 1; vertical++)
				{
					for (int horizontal = -1; horizontal <= 1; horizontal++)
					{
						if (horizontal == 0 && vertical == 0)
						{
							continue;
						}

						int row = current.Y + vertical;

						if (row < 0 || row >= MapRules.Height)
						{
							continue;
						}

						Point next = new Point(Wrap(current.X + horizontal), row);

						if (map.Continent(next.X, next.Y) != continent)
						{
							continue;
						}

						int step = StepCost(map, next, knowsBridge);

						if (step >= ImpassableCost)
						{
							continue;
						}

						int total = cost[current] + step;

						if (!cost.TryGetValue(next, out int known) || total < known)
						{
							cost[next] = total;
							previous[next] = current;
							open.Enqueue(next, total + (2 * Distance(next, goal)));
						}
					}
				}
			}

			List<Point> route = new List<Point>();

			if (!cost.ContainsKey(goal))
			{
				return route;
			}

			for (Point tile = goal; ; tile = previous[tile])
			{
				route.Add(tile);

				if (tile == start)
				{
					break;
				}
			}

			route.Reverse();
			return route;
		}

		private static SettlerWork RoadOrRailroad(int terrain, int improvements, bool knowsBridge, bool knowsRailroad)
		{
			if (terrain == River && !knowsBridge)
			{
				return SettlerWork.None;
			}

			if ((improvements & RoadBit) == 0)
			{
				return SettlerWork.Road;
			}

			if (knowsRailroad && (improvements & RailroadBit) == 0 && (improvements & CityBit) == 0)
			{
				return SettlerWork.Railroad;
			}

			return SettlerWork.None;
		}

		private static SettlerWork WantedTerrainWork(ISettlerMap map, int terrain, int horizontalLocation, int verticalLocation)
		{
			int irrigation = map.IrrigationEffect(terrain);
			int mining = map.MiningEffect(terrain);
			bool canMine = mining <= -2;
			bool canIrrigate = irrigation <= -2 && (irrigation != -2 || map.HasWater(horizontalLocation, verticalLocation));

			if (canMine && terrain is Hills or Mountains)
			{
				return SettlerWork.Mine;
			}

			if (canIrrigate)
			{
				return SettlerWork.Irrigate;
			}

			if (canMine && terrain == Desert)
			{
				return SettlerWork.Mine;
			}

			return SettlerWork.None;
		}

		private static bool GainsFromRoad(int terrain) => terrain is Desert or Plains or Grassland;

		private static int StepCost(ISettlerMap map, Point tile, bool knowsBridge)
		{
			int terrain = map.Terrain(tile.X, tile.Y);

			if (terrain == MapRules.OceanTerrain)
			{
				return ImpassableCost;
			}

			if (terrain == River && !knowsBridge)
			{
				return 40;
			}

			int improvements = map.Improvements(tile.X, tile.Y);

			if ((improvements & RailroadBit) != 0 || (improvements & CityBit) != 0)
			{
				return 1;
			}

			if ((improvements & RoadBit) != 0)
			{
				return 2;
			}

			return terrain is Desert or Plains or Grassland or River ? 4 : 7;
		}

		private static int Wrap(int horizontalLocation) => ((horizontalLocation % MapRules.Width) + MapRules.Width) % MapRules.Width;
	}
}
