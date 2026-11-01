using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class HealingRules
	{
		public const int MaxShipCoastDistanceToCity = 3;

		public static bool IsAdjacentToLand(int x, int y, Func<int, int, bool> isOceanTile)
		{
			for (int dy = -1; dy <= 1; dy++)
			{
				int ny = y + dy;
				if (ny < 0 || ny >= TradeRules.WorldHeight)
					continue;

				for (int dx = -1; dx <= 1; dx++)
				{
					if (dx == 0 && dy == 0)
						continue;

					int nx = (x + dx + TradeRules.WorldWidth) % TradeRules.WorldWidth;
					if (!isOceanTile(nx, ny))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static int ComputeDistanceToNearestFriendlyCity(int x, int y, IEnumerable<Point> friendlyCityLocations)
		{
			int minDistance = int.MaxValue;
			foreach (Point cityPos in friendlyCityLocations)
			{
				int dist = TradeRules.ComputeDistance(x, y, cityPos.X, cityPos.Y);
				if (dist < minDistance)
				{
					minDistance = dist;
				}
			}

			return minDistance;
		}

		public static int CalculateHealAmount(
			int landGroup,
			bool inFriendlyCity,
			bool isResting,
			bool isAdjacentToLand,
			int distToNearestFriendlyCity)
		{
			if (inFriendlyCity)
			{
				return 2;
			}

			if (landGroup == 2)
			{
				if (isResting && isAdjacentToLand && distToNearestFriendlyCity <= MaxShipCoastDistanceToCity)
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
