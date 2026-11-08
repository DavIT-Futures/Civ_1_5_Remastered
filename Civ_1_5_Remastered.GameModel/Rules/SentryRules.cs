using System.Collections.Generic;
using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Remastered sentry alarm: at the start of the player's turn a sentried unit wakes up when a unit of a
	/// civilization we are not at peace with (or a barbarian) stands next to it, even if that unit did not move.
	/// Land units carried aboard ships keep sleeping, as in the original game.
	/// </summary>
	public static class SentryRules
	{
		public static bool ShouldWake(Point unit, bool carriedAtSea, ISet<Point> hostileUnits)
		{
			if (carriedAtSea || hostileUnits.Count == 0)
			{
				return false;
			}

			foreach (Point next in GotoPathfinder.Neighbours(unit))
			{
				if (hostileUnits.Contains(next))
				{
					return true;
				}
			}

			return false;
		}
	}
}
