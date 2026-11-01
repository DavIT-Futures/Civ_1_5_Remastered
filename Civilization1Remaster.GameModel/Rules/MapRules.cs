using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class MapRules
	{
		public const int Width = 80;
		public const int Height = 50;

		public const int OceanTerrain = 10;

		public static bool IsOnMap(int horizLoc, int vertLoc)
		{
			return horizLoc >= 0 && horizLoc < Width && vertLoc >= 0 && vertLoc < Height;
		}

		public static bool HasSpecialResource(int horizLoc, int vertLoc, int worldSeed)
		{
			if (vertLoc <= 1 || vertLoc >= Height - 2)
				return false;

			return PlaceInBlock(horizLoc, vertLoc) == (BlockHash(horizLoc, vertLoc, worldSeed) & 0xf);
		}

		public static bool IsHutPlace(int horizLoc, int vertLoc, int worldSeed)
		{
			if (vertLoc <= 1 || vertLoc >= Height - 2)
				return false;

			return PlaceInBlock(horizLoc, vertLoc) == ((BlockHash(horizLoc, vertLoc, worldSeed) + 8) & 0x1f);
		}

		public static int Distance(int horizLoc1, int vertLoc1, int horizLoc2, int vertLoc2)
		{
			int horizStep = Math.Abs(horizLoc1 - horizLoc2);

			if (horizStep > Width / 2)
				horizStep = Width - horizStep;

			int vertStep = Math.Abs(vertLoc1 - vertLoc2);

			return LongerPlusHalfShorter(horizStep, vertStep);
		}

		public static int LongerPlusHalfShorter(int horizStep, int vertStep)
		{
			horizStep = Math.Abs(horizStep);
			vertStep = Math.Abs(vertStep);

			return horizStep > vertStep ? (short)((vertStep / 2) + horizStep) : (short)((horizStep / 2) + vertStep);
		}

		public static int DoubledDistance(int horizStep, int vertStep)
		{
			horizStep = Math.Abs(horizStep);

			if (horizStep > Width / 2)
				horizStep = Width - horizStep;

			vertStep = Math.Abs(vertStep);

			return horizStep > vertStep ? horizStep * 2 + vertStep : vertStep * 2 + horizStep;
		}

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
