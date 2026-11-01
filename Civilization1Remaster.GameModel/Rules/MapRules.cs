using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class MapRules
	{
		public const int Width = 80;
		public const int Height = 50;

		public const int OceanTerrain = 10;

		public static bool IsOnMap(int horizontalLocation, int verticalLocation)
		{
			return horizontalLocation >= 0 && horizontalLocation < Width && verticalLocation >= 0 && verticalLocation < Height;
		}

		public static bool HasSpecialResource(int horizontalLocation, int verticalLocation, int worldSeed)
		{
			if (verticalLocation <= 1 || verticalLocation >= Height - 2)
				return false;

			return PlaceInBlock(horizontalLocation, verticalLocation) == (BlockHash(horizontalLocation, verticalLocation, worldSeed) & 0xf);
		}

		public static bool IsHutPlace(int horizontalLocation, int verticalLocation, int worldSeed)
		{
			if (verticalLocation <= 1 || verticalLocation >= Height - 2)
				return false;

			return PlaceInBlock(horizontalLocation, verticalLocation) == ((BlockHash(horizontalLocation, verticalLocation, worldSeed) + 8) & 0x1f);
		}

		public static int Distance(int firstHorizontalLocation, int firstVerticalLocation, int secondHorizontalLocation, int secondVerticalLocation)
		{
			int horizontalStep = Math.Abs(firstHorizontalLocation - secondHorizontalLocation);

			if (horizontalStep > Width / 2)
				horizontalStep = Width - horizontalStep;

			int verticalStep = Math.Abs(firstVerticalLocation - secondVerticalLocation);

			return LongerPlusHalfShorter(horizontalStep, verticalStep);
		}

		public static int LongerPlusHalfShorter(int horizontalStep, int verticalStep)
		{
			horizontalStep = Math.Abs(horizontalStep);
			verticalStep = Math.Abs(verticalStep);

			return horizontalStep > verticalStep ? (short)((verticalStep / 2) + horizontalStep) : (short)((horizontalStep / 2) + verticalStep);
		}

		public static int DoubledDistance(int horizontalStep, int verticalStep)
		{
			horizontalStep = Math.Abs(horizontalStep);

			if (horizontalStep > Width / 2)
				horizontalStep = Width - horizontalStep;

			verticalStep = Math.Abs(verticalStep);

			return horizontalStep > verticalStep ? horizontalStep * 2 + verticalStep : verticalStep * 2 + horizontalStep;
		}

		private static int PlaceInBlock(int horizontalLocation, int verticalLocation)
		{
			return ((horizontalLocation & 3) * 4) + (verticalLocation & 3);
		}

		private static int BlockHash(int horizontalLocation, int verticalLocation, int worldSeed)
		{
			return ((horizontalLocation / 4) * 13) + ((verticalLocation / 4) * 11) + worldSeed;
		}
	}
}
