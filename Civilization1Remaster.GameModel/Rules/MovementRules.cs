using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class MovementRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		public static readonly (sbyte dx, sbyte dy)[] DirectionVectors = new (sbyte, sbyte)[]
		{
			(0, 0),
			(0, -1),
			(1, -1),
			(1, 0),
			(1, 1),
			(0, 1),
			(-1, 1),
			(-1, 0),
			(-1, -1)
		};

		public static int WrapX(int x)
		{
			if (x < 0) return x + WorldWidth;
			if (x >= WorldWidth) return x - WorldWidth;
			return x;
		}

		public static bool HasAdjacentForeignUnit(int horizLoc, int vertLoc, short participantIdx, Func<int, int, int> getTileOwner)
		{
			for (int dir = 1; dir <= 8; dir++)
			{
				int x = WrapX(horizLoc + DirectionVectors[dir].dx);
				int y = vertLoc + DirectionVectors[dir].dy;

				int owner = getTileOwner(x, y);
				if (owner != -1 && owner != 0xffff && owner != participantIdx)
				{
					return true;
				}
			}

			return false;
		}
	}
}
