using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of MapPlanes that the other parts of the game use</summary>
	public interface IMapPlanes
	{
		/// <summary>Terrain type (0 - 10, 10 = ocean) of the tile</summary>
		int Terrain(int horizLoc, int vertLoc);

		/// <summary>Number of the continent or the sea of the tile</summary>
		int Continent(int horizLoc, int vertLoc);

		/// <summary>Improvements of the tile (bit 0 city, 1 irrigation, 2 mine, 3 road, ...)</summary>
		int Improvements(int horizLoc, int vertLoc);

		/// <summary>The layer of the improvement bits 0 - 3</summary>
		int LowImprovements(int horizLoc, int vertLoc);

		/// <summary>The layer of the improvement bits 4 - 7, shifted to bits 0 - 3</summary>
		int HighImprovements(int horizLoc, int vertLoc);

		/// <summary>Improvements of the tile as the human player saw them last time</summary>
		int KnownImprovements(int horizLoc, int vertLoc);

		/// <summary>The human player sees the improvements of the tile as they are now</summary>
		void RevealImprovements(int horizLoc, int vertLoc);

		/// <summary>Removes the improvement bits from the tile</summary>
		void ClearImprovements(int improvements, int horizLoc, int vertLoc);

		/// <summary>
		/// Horizontal step of a direction (1 - 8, 0 is no move)
		/// </summary>
		int DirectionHoriz(int heading);

		/// <summary>
		/// Vertical step of a direction (1 - 8, 0 is no move)
		/// </summary>
		int DirectionVert(int heading);

		/// <summary>The tile owner layer: bit 3 when units stand on the tile, bits 0 - 2 the player</summary>
		int OwnerBits(int horizLoc, int vertLoc);

		void AssignOwnerBits(int horizLoc, int vertLoc, int bits);
	}
}
