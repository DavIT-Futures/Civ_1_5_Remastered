using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for unit healing per turn.
	/// - Units in friendly cities heal 2 HP per turn (2x faster).
	/// - Land units outside cities heal 1 HP per turn if fortified.
	/// - Ships outside cities heal 1 HP per turn ONLY if resting (sentry or not moved this turn: ships cannot fortify),
	///   adjacent to coast (land) AND within distance &lt;= 3 of a friendly city.
	/// - Air units heal only in cities.
	/// </summary>
	public static class HealingRules
	{
		public const int OceanTerrainType = 10;
		public const int MaxShipCoastDistanceToCity = 3;

		/// <summary>
		/// Determines whether a tile is adjacent to land (at the coast).
		/// Checks all 8 neighboring tiles with cylindrical wrapping on X (0..79) and clipping on Y (0..49).
		/// </summary>
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

		/// <summary>
		/// Calculates the minimum map distance (with cylindrical wrapping) to the nearest friendly city.
		/// Returns int.MaxValue if there are no friendly cities.
		/// </summary>
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

		/// <summary>
		/// Determines the amount of hit points to mend this turn.
		/// </summary>
		/// <param name="landGroup">0 = Land, 1 = Air, 2 = Sea / Naval</param>
		/// <param name="inFriendlyCity">Whether the unit is inside a friendly city</param>
		/// <param name="isResting">Whether the unit rests: fortified for a land unit, sentry or not moved this turn for a ship</param>
		/// <param name="isAdjacentToLand">Whether the sea tile has adjacent land (coast)</param>
		/// <param name="distToNearestFriendlyCity">Distance to nearest friendly city</param>
		/// <returns>HP amount to heal (0, 1, or 2)</returns>
		public static int CalculateHealAmount(
			int landGroup,
			bool inFriendlyCity,
			bool isResting,
			bool isAdjacentToLand,
			int distToNearestFriendlyCity)
		{
			// In friendly city: 2x healing (2 HP per turn)
			if (inFriendlyCity)
			{
				return 2;
			}

			// Outside city, naval unit:
			// Must be resting, adjacent to coast, and within distance <= 3 from a friendly city
			if (landGroup == 2)
			{
				if (isResting && isAdjacentToLand && distToNearestFriendlyCity <= MaxShipCoastDistanceToCity)
				{
					return 1;
				}
				return 0;
			}

			// Land unit outside city: heals 1 HP per turn if fortified
			if (landGroup == 0)
			{
				return isResting ? 1 : 0;
			}

			// Air unit: heals only in city
			return 0;
		}
	}
}
