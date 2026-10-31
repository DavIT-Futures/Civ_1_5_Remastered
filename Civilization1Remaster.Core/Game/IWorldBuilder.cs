using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of WorldBuilder that the other parts of the game use</summary>
	public interface IWorldBuilder
	{
		/// <summary>
		/// Generates a new world map (or loads EARTH.MP when 0xd76a != 0) while advancing the opening creation story on screen 0:
		/// builds cloud layers, smooths diagonal coastlines, assigns initial elevation and latitude terrain, runs west-to-east
		/// and east-to-west moisture passes, ages terrain with random walks, carves rivers (ProduceElevation), and finalizes
		/// continents, oceans, and special resources (ProduceTerrain).
		/// </summary>
		void OpeningSequence();

		/// <summary>
		/// On the map being made (screen 1) the tile becomes land (colour 1) unless one of its four diagonal neighbours is land
		/// </summary>
		void BlendTerrainTile(short horizLoc, short vertLoc);

		/// <summary>
		/// Rivers of the new map (in fact not the elevation): up to 256 attempts, until there are more than 2 * (0x7ef6 + 0x7efa)
		/// + 6 rivers. An attempt starts at a random hill (screen 1 value 12) and flows (colour 9) in one of 4 directions, turning
		/// by 45 degrees at random, until it reaches a tile next to the ocean (1), a river (9) or a value 13 or 1; it stays when
		/// it got next to the ocean or into a river after at least 5 steps (and the grassland, 2, around its spring becomes 11),
		/// else the map is put back from the copy at column 160. The story of the intro goes on after each attempt.
		/// </summary>
		void ProduceElevation();

		/// <summary>
		/// A cloud layer for the intro: one cloud at a random place (columns 4 - 75, rows 8 - 41) drawn in the area at column 160
		/// of screen 1, then every pixel of the 80 x 50 area at column 0 under a cloud pixel gets one colour lighter (0x67fe
		/// counts them)
		/// </summary>
		void ProduceCloudLayer();

		/// <summary>Draws a cloud on screen 1: 1 - 64 steps of a random walk from the place, 3 white dots a step</summary>
		void ProduceCloud(short horizLoc, short vertLoc);

		/// <summary>
		/// Numbers the continents (pass 0) and the seas (pass 1) of the screen map: the tiles of the kind are labelled row by row
		/// in ChartSight (joined with the touching labels of the row above, a label's size in 0xdcfe + 0x80 * pass + 2 * label),
		/// the 15 largest get the numbers 1 - 15 by size (drawn at y + 50, the others 15) and their sizes; label 0 has size 0
		/// and 15 size 1. Then the terrain map, the block links, the special resources and the polar rows (lines and 4 * 20
		/// random tundra dots). The terrain counts of pass 0 and the ranking are in the words of the frame (B3: in C#); the
		/// story goes on after every row and every join.
		/// </summary>
		void ProduceTerrain();

		/// <summary>
		/// The value of every land tile as a city site (in the map screen): the weights of the 12 terrains and their specials
		/// (3 * food + trade, + 2 * shields except on 2 and 11, + the bonus of a mine or twice that of irrigation), summed
		/// over the 21 tiles of a city (the inner 8 twice, the centre four times, +2 for rivers on terrains 2 and 11), less 16 off
		/// the river pattern unless on terrain 1; the value 8 - 15 is drawn at (x + 80, y) and (x + 80, y + 50), the other tiles
		/// get 0. Every site counts for its continent (ConstructionSpotTally).
		/// </summary>
		void PlaceSpecialTerrain();

		/// <summary>
		/// The links between the 20 x 13 blocks of 4 x 4 tiles (bits at 0xdb44 + 13 * column + row): a block with land (the
		/// first of its tiles (1, 1), (2, 1), (1, 2), (2, 2) that isn't ocean on the screen map) gets bit direction - 1, and its
		/// neighbour in the direction 1 - 4 (0x1882 / 0x18e4) bit (direction + 3) &amp; 7, when the neighbour's land is the same
		/// continent and a route of less than 20 (ComputeRouteCost) joins them. The final values of the locals stay in the
		/// stack frame, like in the original.
		/// </summary>
		void LabelLandmasses();

		/// <summary>
		/// The sea links between the 20 x 12 blocks of 4 x 4 tiles (bits at 0x7f38 + 13 * column + row): a block with sea (the
		/// first ocean tile of (1, 1), (2, 1), (1, 2), (2, 2) on the screen map) gets bit direction - 1, and its neighbour in the
		/// direction 1 - 8 (0x1882 / 0x18e4) bit (direction + 3) &amp; 7, when the neighbour's sea is the same and a passage of
		/// 5 steps from the block's corner has at least 2 of 4 ocean tiles inside the map at each step (always for direction 3
		/// in the last row). The neighbour's sea is compared from the stack frame (0xe), so a neighbour without sea compares
		/// the last one found, like in the original. With the marker the intro story goes on after each column; the edge
		/// columns are then linked around the map (0xe0, 0x0e) except at the corners.
		/// </summary>
		void ReckonContinentSizes(ushort marker);

		/// <summary>
		/// Terrain type of the pixel of the current screen (the screen of the box at 0xaa), by the terrain of the colour (MapPlanes.TerrainOfColour)
		/// </summary>
		/// <returns>Terrain type, also in AX</returns>
		ushort ObtainTerrainTypeFromScreen(short horizLoc, short vertLoc);

		/// <summary>The continent number of the place, from the map layer of the drawing screen (row + 50)</summary>
		ushort ObtainContinentIDFromScreen(short horizLoc, short vertLoc);

		/// <summary>
		/// One step of the story told while the new world is made (called again and again by the map generator); the stage
		/// 0x3b62: 0 = not started (opens story.txt, handle 0x6804), 1 = telling, 2 - 4 = told (the line under the picture is
		/// wiped at 5), 5 = "press a key" (0x3b28) blinking with the clock (0x5c / 60). While telling, a step waits until the
		/// music (0x6800, song position from the second line on) reaches the time of the line (0x6806), then shows the next
		/// line of story.txt; a short line (at most 3 characters) instead shows the next picture birthN.pic (0x3b64) and,
		/// with VGA colours (0xd762), its palette and colour cycles (records of first colour, last colour and speed in
		/// 0x3710:0x3b66, cycle count 0x3b68). The end of the file or a key (when 0x67fc allows it) ends the story.
		/// The screen is marked as changed ([0xaa] = 1) and AX is 1 while waiting for the music, else 0.
		/// </summary>
		ushort ShowStoryIntro();

		/// <summary>
		/// Global warming (worldHeatingTally times so far): after the newspaper, changes the land tiles of terrain 0 - 3 that have
		/// at least 7 - worldHeatingTally ocean tiles around them (map layer 2 colour 11 for terrain 3, else 3; improvement bits 6
		/// discarded) and, of the others, those where (11 * column + 13 * row) mod 8 is worldHeatingTally
		/// (colour 14 for terrain 0 - 1, else 6). A changed tile becomes visible to player 0 and is repainted when the human
		/// player sees it.
		/// </summary>
		void EnactGlobalWarming(short worldHeatingTally);
	}
}
