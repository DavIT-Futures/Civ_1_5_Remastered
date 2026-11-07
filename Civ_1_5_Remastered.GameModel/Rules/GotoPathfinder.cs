using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Fastest route search for GoTo orders over the whole map (A* on movement cost).
	/// </summary>
	/// <remarks>
	/// Costs are expressed in tenths of a movement third: a railroad step is almost free,
	/// a road step costs one third of a move and other steps the terrain movement cost.
	/// A tiny per-step surcharge (slightly higher for diagonal steps) makes the route take as few steps
	/// as possible among equally fast ones and keeps it straight instead of zigzagging.
	/// </remarks>
	public static class GotoPathfinder
	{
		public const int Impassable = -1;
		public const int RailroadCost = 1;
		public const int RoadCost = 10;
		public const int MovePointCost = 30;

		private const int StepSurcharge = 2;
		private const int DiagonalSurcharge = 3;

		/// <summary>Cost of a step for the usual movement rules of land units.</summary>
		public static int LandStepCost(int fromImprovements, int toImprovements, bool fromRailroad, int terrainMoveCost)
		{
			if ((fromImprovements & toImprovements & 0x8) != 0)
			{
				return fromRailroad ? RailroadCost : RoadCost;
			}

			return Math.Max(1, terrainMoveCost) * MovePointCost;
		}

		/// <summary>
		/// Finds the fastest route from <paramref name="start"/> to <paramref name="goal"/>.
		/// </summary>
		/// <param name="stepCost">Cost of moving between two neighbouring tiles, or <see cref="Impassable"/>.</param>
		/// <returns>The tiles of the route without the start, or an empty list when the goal cannot be reached.</returns>
		public static List<Point> FindRoute(Point start, Point goal, Func<Point, Point, int> stepCost)
		{
			List<Point> route = new List<Point>();

			if (start == goal)
			{
				return route;
			}

			Dictionary<Point, int> cost = new Dictionary<Point, int> { [start] = 0 };
			Dictionary<Point, Point> previous = new Dictionary<Point, Point>();
			PriorityQueue<Point, int> open = new PriorityQueue<Point, int>();
			open.Enqueue(start, 0);

			while (open.TryDequeue(out Point current, out int estimate))
			{
				if (current == goal)
				{
					break;
				}

				if (estimate - Heuristic(current, goal) > cost[current])
				{
					continue;
				}

				foreach (Point next in Neighbours(current))
				{
					int step = stepCost(current, next);

					if (step < 0)
					{
						continue;
					}

					bool diagonal = next.X != current.X && next.Y != current.Y;
					int total = cost[current] + step + (diagonal ? DiagonalSurcharge : StepSurcharge);

					if (cost.TryGetValue(next, out int known) && known <= total)
					{
						continue;
					}

					cost[next] = total;
					previous[next] = current;
					open.Enqueue(next, total + Heuristic(next, goal));
				}
			}

			if (!previous.ContainsKey(goal))
			{
				return route;
			}

			for (Point tile = goal; tile != start; tile = previous[tile])
			{
				route.Add(tile);
			}

			route.Reverse();
			return route;
		}

		public static IEnumerable<Point> Neighbours(Point tile)
		{
			for (int vertical = -1; vertical <= 1; vertical++)
			{
				for (int horizontal = -1; horizontal <= 1; horizontal++)
				{
					if (horizontal == 0 && vertical == 0)
					{
						continue;
					}

					int row = tile.Y + vertical;

					if (row < 0 || row >= MapRules.Height)
					{
						continue;
					}

					yield return new Point((tile.X + horizontal + MapRules.Width) % MapRules.Width, row);
				}
			}
		}

		/// <summary>Heuristic that never overestimates: every step costs at least a railroad step plus the surcharge.</summary>
		private static int Heuristic(Point tile, Point goal)
		{
			int horizontal = Math.Abs(tile.X - goal.X);
			horizontal = Math.Min(horizontal, MapRules.Width - horizontal);

			return Math.Max(horizontal, Math.Abs(tile.Y - goal.Y)) * (RailroadCost + StepSurcharge);
		}
	}
}
