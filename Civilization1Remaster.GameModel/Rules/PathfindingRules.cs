using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for Civilization 1 pathfinding, quadrant routing, and movement cost calculations.
	/// Translated from CIV.EXE segment 0x2e31 (Pathfinding).
	/// </summary>
	public static class PathfindingRules
	{
		public const int QuadrantGridWidth = 20;  // 80 / 4
		public const int QuadrantGridHeight = 13; // ceil(50 / 4)
		public const int LocalWindowSize = 16;    // 16x16 local flood fill search window

		/// <summary>
		/// Converts world X coordinate to quadrant grid X (0..19).
		/// </summary>
		public static int ToQuadrantX(int x)
		{
			return (x >> 2);
		}

		/// <summary>
		/// Converts world Y coordinate to quadrant grid Y (0..12).
		/// </summary>
		public static int ToQuadrantY(int y)
		{
			return (y >> 2);
		}

		/// <summary>
		/// Wraps quadrant horizontal coordinate around the cylindrical world (width = 20).
		/// </summary>
		public static int WrapQuadrantX(int qx)
		{
			if (qx >= QuadrantGridWidth) return qx - QuadrantGridWidth;
			if (qx < 0) return qx + QuadrantGridWidth;
			return qx;
		}

		/// <summary>
		/// Calculates the step movement cost between two tiles in 1/3-movement points:
		/// - Both tiles have road: 1 point (1/3 move)
		/// - Unit is flat-rate / railroad unit: 3 points (1 full move)
		/// - Standard terrain: baseMoveCost * 3
		/// </summary>
		public static int CalculateStepMoveCost(bool fromRoad, bool toRoad, bool isRailroadUnit, int baseMoveCost)
		{
			if (fromRoad && toRoad)
			{
				return 1;
			}
			if (isRailroadUnit)
			{
				return 3;
			}
			return Math.Max(1, baseMoveCost * 3);
		}

		/// <summary>
		/// Calculates the steering / turn penalty when changing movement direction.
		/// Original CIV.EXE 0x2e31:0x0535:
		/// diff = abs(lastDir - newDir);
		/// if (diff > 4) diff = 8 - diff;
		/// return diff * diff;
		/// </summary>
		public static int CalculateTurnPenalty(int lastDir, int newDir)
		{
			if (lastDir == 0xff || lastDir <= 0 || newDir <= 0)
			{
				return 0;
			}

			int diff = Math.Abs(lastDir - newDir);
			if (diff > 4)
			{
				diff = 8 - diff;
			}
			return diff * diff;
		}

		/// <summary>
		/// Tests if a direction bit is set in a quadrant connectivity mask.
		/// Direction 1..8 maps to bit (dir - 1).
		/// </summary>
		public static bool CanStepQuadrant(byte connectivityMask, int dir)
		{
			if (dir < 1 || dir > 8) return false;
			return (connectivityMask & (1 << (dir - 1))) != 0;
		}

		/// <summary>
		/// Computes Chebyshev / diagonal distance metric as used in Civ1 CommonUtilities.
		/// </summary>
		public static int DiagonalDistance(int dx, int dy)
		{
			dx = Math.Abs(dx);
			dy = Math.Abs(dy);
			return dx > dy ? (dy / 2) + dx : (dx / 2) + dy;
		}
	}
}
