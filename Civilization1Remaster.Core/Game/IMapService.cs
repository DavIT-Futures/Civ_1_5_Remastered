using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of MapService that the other parts of the game use</summary>
	public interface IMapService
	{
		/// <summary>The map data (terrain, continents, improvements, tile owners)</summary>
		IMapPlanes Chart { get; }

		/// <summary>
		/// Centres the map view on the place (0xd4cc / 0xd75e): the 15 x 12 tiles are repainted in a random order (unseen ones
		/// black, all with 0xd806), the names of the cities on the map (0x6c96 of them) are written, then the small world map
		/// in the corner: the part around the place (from the screen 0x19e8, wrapped around, from the top with the technology 8),
		/// the dots of the known cities and the frame of the view.
		/// </summary>
		void FocusMapView(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Renders the map tile at (`horizLoc`, `vertLoc`): either as a 4x4 block on the world map (`0xb278 == 1`), or as a 16x16
		/// tile in the main map viewport (`0xd4cc`, `0xd75e`) including coastlines, river mouths, terrain overlays, irrigation,
		/// pollution, roads/railroads, mines, special resources, huts, fortresses, fog-of-war edges, and cities.
		/// </summary>
		ushort DrawMapTile(int horizLoc, int vertLoc);

		/// <summary>
		/// Draws the unit: on the world map (0xb278 = 1) as a dot of 3 x 3 pixels in the player's colour with a shadow (colour 6
		/// under settlers, 0 under the others), returning 0xc * troopIdx (left in AX by the address of the unit); else on the map window (16 pixels a tile,
		/// from 0xd4cc, 0xd75e) when it is in view and is not a land unit sleeping at sea (status bit 1), with the shadow of
		/// a stack under it.
		/// </summary>
		/// <returns>On the map window 1 when drawn, 0 when not (also in AX)</returns>
		ushort DrawUnitOnWorldMap(short participantIdx, short troopIdx);

		/// <summary>
		/// Draws the hit points of a unit as a row of flat rectangles at the top of its 16 x 16 map square:
		/// green for each hit point the unit has, gray for each lost hit point.
		/// </summary>
		/// <param name="xPos">Left side of the map square on the screen</param>
		/// <param name="yPos">Top side of the map square on the screen</param>
		void DrawHealthBar(int horizLoc, int vertLoc, int completeStrikeMarks, int strikeMarks);

		/// <summary>
		/// Draws the unit's picture at the position with the marks of its status: the fortification (status bit 8), the
		/// mark 0x2ba0 (bit 4), the "go to" mark 0x2ba2 of the human player's units, a letter in a box for
		/// the orders of a unit that is not a plane (bits 0xc2: "R", "I"/"?" for bit 0x40, "M"/"F" for bit 0x80, "P" for bit 2
		/// with 0x80), faded colours when sleeping (bit 1), and its hit points. AX is left 0x600 * participantIdx (from the address
		/// of the unit), like in the original.
		/// </summary>
		void DrawUnitSprite(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Draws the tile again and, unless units are hidden (0xdcfc), the unit on it: when the human sees it (all units are
		/// shown with 0xd806, the human's own, or the unit's bit of the human), and the tile has no city
		/// </summary>
		void RepaintMapTile(int horizLoc, int vertLoc);

		/// <summary>
		/// Draws the unit shown on the tile of the unit: on terrain 10, a non-ship unit in a stack is drawn as the first ship
		/// of its stack (following the ring from the unit; itself when there is none); otherwise the best defender of the stack
		/// </summary>
		void RepaintUnitTile(short participantIdx, short troopIdx);

		/// <summary>
		/// Is the position on the map
		/// </summary>
		/// <returns>1 on the map, 0 outside</returns>
		ushort CheckMapLimits(int horizLoc, int vertLoc);

		/// <summary>
		/// Terrain type of the tile (MapRules.OceanTerrain is the ocean)
		/// </summary>
		ushort ObtainTerrainType(int horizLoc, int vertLoc);

		/// <summary>
		/// The player of the tile owner layer (bits 0 - 2), also when no units stand there
		/// </summary>
		ushort ObtainTileOwnerBits(int horizLoc, int vertLoc);

		/// <summary>
		/// Sets the player of the tile owner layer (of a city), keeps the units bit
		/// </summary>
		void MapAssignCityOwner(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Puts the unit on the tile: into the stack of the units standing there, and marks the tile as having units of the player
		/// </summary>
		void PlaceUnitOnTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// Takes the unit off the tile: out of its stack, or when it stood alone, the tile has no units any more
		/// </summary>
		void RemoveUnitFromTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// The first unit standing on the tile. Sets the player of the unit into 0xd7f0 and 0xd20a.
		/// When the tile owner layer says that units are there but none is found, the layer is cleared.
		/// </summary>
		/// <returns>Unit index, or 0xffff when no unit stands on the tile</returns>
		ushort ObtainUnitAtTile(int horizLoc, int vertLoc);

		/// <summary>
		/// The player whose units stand on the tile
		/// </summary>
		/// <returns>Player, or 0xffff when no units stand on the tile</returns>
		ushort ObtainTileOwner(int horizLoc, int vertLoc);

		/// <summary>
		/// Marks that units stand on the tile, keeps the player
		/// </summary>
		void TagTileOwned(int horizLoc, int vertLoc);

		/// <summary>
		/// The road bit (8) of the tile improvements
		/// </summary>
		ushort ContainsRoad(int horizLoc, int vertLoc);

		/// <summary>
		/// Improvements of the tile (bit 0 city, 1 irrigation, 2 mine, 3 road, ...)
		/// </summary>
		ushort ObtainTileImprovements(int horizLoc, int vertLoc);

		/// <summary>
		/// Improvements of the tile as the human player saw them last time
		/// </summary>
		ushort ObtainKnownTileImprovements(int horizLoc, int vertLoc);

		/// <summary>
		/// The human player sees the improvements of the tile as they are now
		/// </summary>
		void KeepKnownTileImprovements(int horizLoc, int vertLoc);

		/// <summary>
		/// Adds improvements to the tile: 0 removes all, a value from 0x10 sets the high bits, a smaller one the low bits.
		/// In the turn of the human player the human sees the change at once.
		/// </summary>
		void AssignTileImprovements(ushort improvements, int horizLoc, int vertLoc);

		/// <summary>
		/// Removes the improvement bits from the tile
		/// </summary>
		void RemoveTileImprovements(ushort improvements, int horizLoc, int vertLoc);

		/// <summary>
		/// The city on the tile. Sets the player of the tile owner layer into 0xd7f0 and 0xd20a.
		/// When there is no city, an error message is shown.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		ushort FindCityAtTile(int horizLoc, int vertLoc);

		/// <summary>
		/// Does the tile have a special resource (MapRules.HasSpecialResource with the world seed)
		/// </summary>
		ushort ContainsSpecialResource(int horizLoc, int vertLoc);

		/// <summary>
		/// Is there a hut (tribal village) on the tile: not on the ocean, on a tile nobody has visited and without a city
		/// </summary>
		/// <param name="terrain">Terrain type of the tile</param>
		ushort ContainsHut(ushort terrain, int horizLoc, int vertLoc);

		/// <summary>
		/// Number of the continent or the sea of the tile
		/// </summary>
		ushort ObtainContinentID(int horizLoc, int vertLoc);

		/// <summary>
		/// Size of the continent (land) or the sea (ocean) of the tile, from the word arrays at 0xdcfe and 0xdd7e
		/// </summary>
		ushort ObtainLandMassSize(int horizLoc, int vertLoc);
	}
}
