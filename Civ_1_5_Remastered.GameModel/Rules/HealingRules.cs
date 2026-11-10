using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class HealingRules
	{
		public const int MaximumShipCoastDistanceToCity = 3;

		/// <summary>
		/// Determines whether the specified map coordinate is adjacent to a land tile.
		/// </summary>
		/// <param name="horizontal">The horizontal coordinate on the world grid.</param>
		/// <param name="vertical">The vertical coordinate on the world grid.</param>
		/// <param name="isOceanTile">Predicate delegate evaluating whether a tile is ocean.</param>
		/// <returns><c>true</c> if at least one neighboring tile is land; otherwise, <c>false</c>.</returns>
		public static bool IsAdjacentToLand(int horizontal, int vertical, Func<int, int, bool> isOceanTile)
		{
			for (int deltaVertical = -1; deltaVertical <= 1; deltaVertical++)
			{
				int nextVertical = vertical + deltaVertical;
				if (nextVertical < 0 || nextVertical >= TradeRules.WorldHeight)
				{
					continue;
				}

				for (int deltaHorizontal = -1; deltaHorizontal <= 1; deltaHorizontal++)
				{
					if (deltaHorizontal == 0 && deltaVertical == 0)
					{
						continue;
					}

					int nextHorizontal = (horizontal + deltaHorizontal + TradeRules.WorldWidth) % TradeRules.WorldWidth;
					if (!isOceanTile(nextHorizontal, nextVertical))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static int ComputeDistanceToNearestFriendlyCity(int horizontal, int vertical, IEnumerable<Point> friendlyCityLocations)
		{
			int minimumDistance = int.MaxValue;
			foreach (Point cityPosition in friendlyCityLocations)
			{
				int distance = TradeRules.ComputeDistance(horizontal, vertical, cityPosition.X, cityPosition.Y);
				if (distance < minimumDistance)
				{
					minimumDistance = distance;
				}
			}

			return minimumDistance;
		}

		public static int CalculateHealAmount(
			int landGroup,
			bool inFriendlyCity,
			bool isResting,
			bool isAdjacentToLand,
			int distanceToNearestFriendlyCity)
		{
			if (inFriendlyCity)
			{
				return 2;
			}

			if (landGroup == 2)
			{
				if (isResting && isAdjacentToLand && distanceToNearestFriendlyCity <= MaximumShipCoastDistanceToCity)
				{
					return 1;
				}

				return 0;
			}

			if (landGroup == 0)
			{
				return isResting ? 1 : 0;
			}

			return 0;
		}
	}
}
