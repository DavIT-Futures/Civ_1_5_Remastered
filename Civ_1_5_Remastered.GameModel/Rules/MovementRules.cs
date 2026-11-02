using System;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class MovementRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		public static readonly (sbyte DeltaHorizontal, sbyte DeltaVertical)[] DirectionVectors = new (sbyte, sbyte)[]
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

		public static int SeaWonderMoves(bool lighthouse, bool magellan) => (lighthouse ? 1 : 0) + (magellan ? 1 : 0);

		public static int WrapHorizontal(int horizontal)
		{
			if (horizontal < 0)
			{
				return horizontal + WorldWidth;
			}

			if (horizontal >= WorldWidth)
			{
				return horizontal - WorldWidth;
			}

			return horizontal;
		}

		public static bool HasAdjacentForeignUnit(int horizontalLocation, int verticalLocation, short participantIndex, Func<int, int, int> getTileOwner)
		{
			for (int direction = 1; direction <= 8; direction++)
			{
				int horizontal = WrapHorizontal(horizontalLocation + DirectionVectors[direction].DeltaHorizontal);
				int vertical = verticalLocation + DirectionVectors[direction].DeltaVertical;

				int owner = getTileOwner(horizontal, vertical);
				if (owner != -1 && owner != 0xffff && owner != participantIndex)
				{
					return true;
				}
			}

			return false;
		}
	}
}
