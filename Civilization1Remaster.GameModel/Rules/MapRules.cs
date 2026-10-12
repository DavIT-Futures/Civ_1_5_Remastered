using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Rules of the world map that don't need the map data: its size and where special resources and huts are.
	/// </summary>
	public static class MapRules
	{
		public const int Width = 80;
		public const int Height = 50;

		/// <summary>Terrain type of the ocean</summary>
		public const int OceanTerrain = 10;

		/// <summary>True when the position is on the map (the map doesn't wrap here)</summary>
		public static bool IsOnMap(int horizLoc, int vertLoc)
		{
			return horizLoc >= 0 && horizLoc < Width && vertLoc >= 0 && vertLoc < Height;
		}

		/// <summary>
		/// The special resource (fish, oasis, coal, ...) of a tile. In every block of 4 x 4 tiles one tile has it, chosen by the
		/// world seed, but not in the two rows at each pole.
		/// </summary>
		public static bool HasSpecialResource(int horizLoc, int vertLoc, int worldSeed)
		{
			if (vertLoc <= 1 || vertLoc >= Height - 2)
				return false;

			return PlaceInBlock(horizLoc, vertLoc) == (BlockHash(horizLoc, vertLoc, worldSeed) & 0xf);
		}

		/// <summary>
		/// The place of a hut (tribal village) before the terrain and the map are checked: in every block of 4 x 4 tiles at most
		/// one tile, chosen by the world seed (half of the blocks have none), not in the two rows at each pole.
		/// </summary>
		public static bool IsHutPlace(int horizLoc, int vertLoc, int worldSeed)
		{
			if (vertLoc <= 1 || vertLoc >= Height - 2)
				return false;

			return PlaceInBlock(horizLoc, vertLoc) == ((BlockHash(horizLoc, vertLoc, worldSeed) + 8) & 0x1f);
		}

		/// <summary>Index of the tile in its block of 4 x 4 tiles, 0 - 15</summary>
		private static int PlaceInBlock(int horizLoc, int vertLoc)
		{
			return ((horizLoc & 3) * 4) + (vertLoc & 3);
		}

		private static int BlockHash(int horizLoc, int vertLoc, int worldSeed)
		{
			return ((horizLoc / 4) * 13) + ((vertLoc / 4) * 11) + worldSeed;
		}
	}
}
