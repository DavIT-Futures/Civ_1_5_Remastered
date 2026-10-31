using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of CityManagement that the other parts of the game use</summary>
	public interface ICityManagement
	{
		/// <summary>The index (0 - 20) of the offset among the tiles of a city, 0xffff when it is none of them</summary>
		ushort ObtainCityTileIndex(short horizLoc, short vertLoc);

		/// <summary>
		/// A tile of the city (TownDeltas[townTile] from the city): with marker 1 draws it on the city screen with the icons of
		/// its food, shields and trade (MayCityWorkTile 0 - 2; closer together the more there are, a mark when there are none),
		/// else puts the city's player on the tile in map layer 2 (0x2494 set meanwhile). Either way the amounts are added to the
		/// totals at 0x70da.
		/// </summary>
		void RenderCityTile(short townIdx, ushort townTile, short marker);

		/// <summary>
		/// What the tile yields to the city 0x653e of player 0x6548: food (resource 0), shields (1) or trade (2). The terrain's
		/// value (of the terrain, or of the terrain with the special resource) with irrigation (bit 2), mine (4) and road (8, trade on terrain 0 - 2)
		/// — the real improvements only with DiagMarkers bit 2, else irrigation (terrain 0 - 2) or mine and a road (not on terrain
		/// 1) assumed; no shields on every other tile of terrain 2 and 11; the Colossus, the railroad (bit 0x10: 1.5 times), -1 above
		/// 2 under governments 0 - 1 unless the city celebrates (0x40; 0xe3c2 -= 2 while 0x2494), +1 trade under government 2+
		/// celebrating or 4+, and pollution (bit 0x40) halves it. 0 outside the map, never negative.
		/// </summary>
		ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort resource);

		/// <summary>
		/// Does the player have the effect of the wonder: it stands in a city of the player and is not obsolete
		/// </summary>
		/// <returns>1 or 0</returns>
		ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent);

		/// <summary>
		/// The city of the wonder, also when the wonder is not finished (-1)
		/// </summary>
		/// <returns>City index, or 0xffff when the wonder is obsolete</returns>
		ushort ObtainWonderCityID(short miracleIdent);

		/// <summary>
		/// Pollution (improvement bit 0x40) appears on the tile, unless it is there already; the human player sees it
		/// </summary>
		void AttachSmog(int horizLoc, int vertLoc);

		/// <summary>The kind of specialist of the citizen of the shown city (LaborerMarkers2)</summary>
		void AssignCitizenType(ushort citizen, ushort specialist);

		/// <summary>The kind of specialist of the citizen of the shown city</summary>
		ushort ObtainCitizenType(ushort citizen);

		/// <summary>The number of the specialists of the kind in the shown city</summary>
		ushort TallySpecialists(ushort specialist);

		/// <summary>
		/// Keeps the specialists of the city in range: entertainers (0x70e2) and taxmen (0x70e4) are clamped to the city size,
		/// scientists (0x6542) become taxmen while there are fewer taxmen than scientists, then specialists are given up
		/// (scientists first, then entertainers, and a taxman each time) until all of them fit in the size less the workers
		/// (clamped to 0 - 99)
		/// </summary>
		void ReshuffleCitizens(short townIdx, ushort workerTally);

		/// <summary>
		/// The citizens of the city from (horizLoc, vertLoc): the happy (0x70e2), the content, the unhappy (0x70e4, the first
		/// 0x6542 of them marked) and the specialists specialists (ObtainCitizenType), 7 pixels apart or narrower to fit in breadth, with
		/// gaps between the groups.
		/// </summary>
		/// <returns>The distance of the icons</returns>
		ushort RenderCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort specialists, ushort breadth);

		/// <summary>
		/// Draws an icon of the city screen (from the picture screen, the place by its number) at (horizLocTo, vertLocTo)
		/// </summary>
		void RenderResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo);

		/// <summary>
		/// A box from one corner to the other: the background pattern (FloodBoxWithMotif) and the frame (RenderBox, style 1)
		/// </summary>
		void RenderFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1);

		/// <summary>
		/// Fills the box with the motif of the city screen: on VGA tiles of 16 x 16 from (208, 100) of the picture screen, on
		/// EGA colour 9
		/// </summary>
		void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude);

		/// <summary>
		/// A raised box from (horizLoc, vertLoc) to (horizSpot1, vertSpot1) in the colour, with the text at the address
		/// centered in it
		/// </summary>
		void RenderLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, GameText label, ushort style);

		void RenderMiniMap();

		/// <summary>
		/// A dot of 2 x 2 pixels on the small world map (only when 0x2496 = 2), the world centred on the start column of the
		/// human player
		/// </summary>
		void RenderMiniMapDot(int horizLoc, int vertLoc, ushort style);

		/// <summary>
		/// The work of a city: marker -1 only counts (food, shields, trade, the citizens), 0 is the city's turn (growth,
		/// production, disorder, celebration, science, pollution, upkeep), 1 the city screen with its input. It gives back
		/// the happy less the unhappy citizens. The frame of 0x10c bytes holds the locals of all the parts.
		/// </summary>
		ushort HandleCity(short townIdx, short marker);
	}
}
