using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Chooses where computer settlers found new cities. Pure logic, independent of the game engine.
	/// </summary>
	/// <remarks>
	/// Cities keep at least <see cref="DefaultSpacing"/> tiles (Chebyshev distance) from every other city,
	/// <see cref="SmallLandSpacing"/> on a small landmass where land is scarce. A site is scored by the map's site
	/// value, the yield of its 21 tiles, coast and river, and the distance from the settler.
	/// </remarks>
	public static class CitySitePlanner
	{
		public const int DefaultSpacing = 5;
		public const int SmallLandSpacing = 4;
		public const int SmallLandTiles = 80;
		public const int MinimumSiteValue = 8;
		public const int RiverTerrain = 11;

		private const int SiteValueWeight = 10;
		private const int CoastBonus = 8;
		private const int RiverBonus = 6;
		private const int SpecialBonus = 3;
		private const int DistancePenalty = 4;

		public static int Spacing(int landMassSize) => landMassSize < SmallLandTiles ? SmallLandSpacing : DefaultSpacing;

		public static int Distance(Point first, Point second)
		{
			int horizontal = Math.Abs(first.X - second.X);
			horizontal = Math.Min(horizontal, MapRules.Width - horizontal);

			return Math.Max(horizontal, Math.Abs(first.Y - second.Y));
		}

		/// <summary>Whether a city may be founded on the tile.</summary>
		public static bool IsValidSite(ICitySiteMap map, Point tile, IEnumerable<Point> cities)
		{
			if (tile.Y <= 1 || tile.Y >= MapRules.Height - 2 || map.Terrain(tile.X, tile.Y) == MapRules.OceanTerrain ||
				map.SiteValue(tile.X, tile.Y) < MinimumSiteValue)
			{
				return false;
			}

			int spacing = Spacing(map.LandMassSize(tile.X, tile.Y));

			foreach (Point city in cities)
			{
				if (Distance(city, tile) < spacing)
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsCoastal(ICitySiteMap map, Point tile)
		{
			foreach (Point next in Around(tile, 1))
			{
				if (map.Terrain(next.X, next.Y) == MapRules.OceanTerrain)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Value of a site regardless of where the settler stands.</summary>
		public static int SiteScore(ICitySiteMap map, Point tile)
		{
			int score = map.SiteValue(tile.X, tile.Y) * SiteValueWeight;

			for (int vertical = -2; vertical <= 2; vertical++)
			{
				int row = tile.Y + vertical;

				if (row < 0 || row >= MapRules.Height)
				{
					continue;
				}

				for (int horizontal = -2; horizontal <= 2; horizontal++)
				{
					if (Math.Abs(horizontal) == 2 && Math.Abs(vertical) == 2)
					{
						continue;
					}

					int column = Wrap(tile.X + horizontal);
					(int food, int shields, int trade) = map.TerrainYield(map.Terrain(column, row));
					score += (3 * food) + (2 * shields) + trade;

					if (map.HasSpecialResource(column, row))
					{
						score += SpecialBonus;
					}
				}
			}

			if (IsCoastal(map, tile))
			{
				score += CoastBonus;
			}

			if (map.Terrain(tile.X, tile.Y) == RiverTerrain)
			{
				score += RiverBonus;
			}

			return score;
		}

		/// <summary>Best site on the settler's own landmass within <paramref name="range"/> tiles.</summary>
		public static Point? FindSite(ICitySiteMap map, Point settler, IReadOnlyCollection<Point> cities, ICollection<Point> reserved, int range)
		{
			int continent = map.Continent(settler.X, settler.Y);
			Point? best = null;
			int bestScore = int.MinValue;

			foreach (Point tile in Around(settler, range))
			{
				if (map.Continent(tile.X, tile.Y) != continent || reserved.Contains(tile) || !IsValidSite(map, tile, cities))
				{
					continue;
				}

				int score = SiteScore(map, tile) - (DistancePenalty * Distance(settler, tile));

				if (score > bestScore)
				{
					best = tile;
					bestScore = score;
				}
			}

			return best;
		}

		/// <summary>
		/// Best coastal site on another landmass than <paramref name="homeContinent"/>, for settlers carried by ship.
		/// </summary>
		public static Point? FindOverseasSite(ICitySiteMap map, Point harbour, int homeContinent, IReadOnlyCollection<Point> cities,
			ICollection<Point> reserved, int range)
		{
			Point? best = null;
			int bestScore = int.MinValue;

			foreach (Point tile in Around(harbour, range))
			{
				if (map.Terrain(tile.X, tile.Y) == MapRules.OceanTerrain || map.Continent(tile.X, tile.Y) == homeContinent ||
					reserved.Contains(tile) || !IsCoastal(map, tile) || !IsValidSite(map, tile, cities))
				{
					continue;
				}

				int score = SiteScore(map, tile) - (DistancePenalty * Distance(harbour, tile));

				if (score > bestScore)
				{
					best = tile;
					bestScore = score;
				}
			}

			return best;
		}

		/// <summary>An ocean tile next to <paramref name="site"/> where a ship can drop its settlers.</summary>
		public static Point? LandingWater(ICitySiteMap map, Point site, Point harbour)
		{
			Point? best = null;

			foreach (Point next in Around(site, 1))
			{
				if (map.Terrain(next.X, next.Y) == MapRules.OceanTerrain &&
					(best == null || Distance(next, harbour) < Distance(best.Value, harbour)))
				{
					best = next;
				}
			}

			return best;
		}

		/// <summary>Tiles within <paramref name="range"/> (Chebyshev) of a tile, the tile itself included, wrapping around the world.</summary>
		public static IEnumerable<Point> Around(Point tile, int range)
		{
			for (int vertical = -range; vertical <= range; vertical++)
			{
				int row = tile.Y + vertical;

				if (row < 0 || row >= MapRules.Height)
				{
					continue;
				}

				int reach = Math.Min(range, MapRules.Width / 2);
				int last = reach * 2 >= MapRules.Width ? reach - 1 : reach;

				for (int horizontal = -reach; horizontal <= last; horizontal++)
				{
					yield return new Point(Wrap(tile.X + horizontal), row);
				}
			}
		}

		private static int Wrap(int horizontalLocation) => ((horizontalLocation % MapRules.Width) + MapRules.Width) % MapRules.Width;
	}
}
