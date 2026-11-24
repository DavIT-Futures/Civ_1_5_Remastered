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

		/// <summary>
		/// Calculates additional sea movement points granted by naval world wonders.
		/// </summary>
		/// <param name="lighthouse">Whether the Great Lighthouse wonder is active.</param>
		/// <param name="magellan">Whether Magellan's Expedition wonder is active.</param>
		/// <returns>Bonus sea movement points.</returns>
		public static int SeaWonderMoves(bool lighthouse, bool magellan) => (lighthouse ? 1 : 0) + (magellan ? 1 : 0);

		/// <summary>
		/// Wraps a horizontal map coordinate across the cylindrical world boundary.
		/// </summary>
		/// <param name="horizontal">Raw horizontal coordinate.</param>
		/// <returns>Normalized coordinate within [0, WorldWidth).</returns>
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

		/// <summary>
		/// Checks whether any adjacent tile contains a unit belonging to a foreign player.
		/// </summary>
		/// <param name="horizontalLocation">Origin horizontal tile coordinate.</param>
		/// <param name="verticalLocation">Origin vertical tile coordinate.</param>
		/// <param name="participantIndex">Active player index.</param>
		/// <param name="getTileOwner">Accessor delegate resolving tile occupant ownership.</param>
		/// <returns><c>true</c> if a foreign unit is adjacent; otherwise, <c>false</c>.</returns>
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
