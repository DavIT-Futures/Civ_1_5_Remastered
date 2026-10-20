using System;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Unit movement, direction calculations, and Zone of Control rules.
	/// </summary>
	public static class MovementRules
	{
		public const int WorldWidth = 80;
		public const int WorldHeight = 50;

		/// <summary>
		/// The 8 compass directions indexed from 1 to 8:
		/// 1: North (0, -1)
		/// 2: North-East (1, -1)
		/// 3: East (1, 0)
		/// 4: South-East (1, 1)
		/// 5: South (0, 1)
		/// 6: South-West (-1, 1)
		/// 7: West (-1, 0)
		/// 8: North-West (-1, -1)
		/// Index 0 represents no direction (0, 0).
		/// </summary>
		public static readonly (sbyte dx, sbyte dy)[] DirectionVectors = new (sbyte, sbyte)[]
		{
			(0, 0),   // 0: None
			(0, -1),  // 1: North
			(1, -1),  // 2: North-East
			(1, 0),   // 3: East
			(1, 1),   // 4: South-East
			(0, 1),   // 5: South
			(-1, 1),  // 6: South-West
			(-1, 0),  // 7: West
			(-1, -1)  // 8: North-West
		};

		/// <summary>
		/// Adjusts the horizontal coordinate (X) to wrap around the world cylinder (0..79).
		/// </summary>
		public static int WrapX(int x)
		{
			if (x < 0) return x + WorldWidth;
			if (x >= WorldWidth) return x - WorldWidth;
			return x;
		}

		/// <summary>
		/// Calculates delta X taking into account the cylindrical world wrap (width = 80).
		/// </summary>
		public static int WrappedDeltaX(int fromX, int targetX)
		{
			int dx = targetX - fromX;
			if (dx >= WorldWidth / 2) dx -= WorldWidth;
			else if (dx < -WorldWidth / 2) dx += WorldWidth;
			return dx;
		}

		/// <summary>
		/// Determines if target tile (targetX, targetY) is directly adjacent to (fromX, fromY).
		/// </summary>
		public static bool IsAdjacent(int fromX, int fromY, int targetX, int targetY)
		{
			int dx = Math.Abs(WrappedDeltaX(fromX, targetX));
			int dy = Math.Abs(targetY - fromY);
			return (dx <= 1 && dy <= 1) && (dx != 0 || dy != 0);
		}

		/// <summary>
		/// Finds the direction index (1..8) to an adjacent tile. Returns 0 if not adjacent or same tile.
		/// </summary>
		public static int DirectionTo(int fromX, int fromY, int targetX, int targetY)
		{
			int dx = WrappedDeltaX(fromX, targetX);
			int dy = targetY - fromY;

			if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
				return 0;

			for (int dir = 1; dir <= 8; dir++)
			{
				if (DirectionVectors[dir].dx == dx && DirectionVectors[dir].dy == dy)
					return dir;
			}

			return 0;
		}

		/// <summary>
		/// Checks whether any of the 8 adjacent tiles contains an enemy/foreign unit.
		/// </summary>
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
