using Civilization1Remaster.Emulation;
using System;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of CityPanorama that the other parts of the game use</summary>
	public interface ICityPanorama
	{
		/// <summary>
		/// The view of the city: the land, the walls, the buildings, the wonders and the people; the improvement (or 24 + the
		/// wonder) just built is shown too
		/// </summary>
		void ShowCityScreen(short townIdx, short improvement);

		/// <summary>
		/// One picture of the city view (table at 0x6880) into the back screen (0x19d4), in the column and row of the view
		/// </summary>
		void DrawCityPicture(ushort column, ushort row, ushort pictureIdx);

		/// <summary>
		/// One picture of the city view (table at 0x817a by picture and kind) into the back screen (0x19d4), in the column and
		/// row of the view
		/// </summary>
		void DrawCityPictureRegion(ushort column, ushort row, ushort picture, ushort kind);

		/// <summary>
		/// One picture of a row of the city screen into the back screen (0x19d4): pictures below 0x30 from the table at 0x81a2,
		/// the others from the table at 0x68a0, moved up by their height (segment 0x3767)
		/// </summary>
		void DrawCityResourceBar(ushort column, ushort row, ushort picture);

		/// <summary>
		/// The citizens of the city in a row from (rowHoriz, rowVert), at most breadth wide: the 9 pictures 0x23 apart of 0x4ebc (row 0x34
		/// without technology 0x23, else row 1) are cut into the stack frame; then the happy (0x70e2), content, unhappy (0x70e4)
		/// people (clamped, so that with the specialists 0xe8b8 they fit the size) with two alternating pictures each and a gap of
		/// 8 after a non-empty group (12 after the unhappy), then the specialists (picture 5 + their type)
		/// </summary>
		void DrawCityScenery(short townIdx, ushort rowHoriz, ushort rowVert, ushort breadth);

		/// <summary>
		/// Cuts the pictures of the city view out of screen 1: from 0x4ec8 the 5 x 4 land pieces (0x817a) and the 16 wall pieces
		/// (0x6880, the second set with technology 0x3a), from 0x4ed5 (technology 0x25) or 0x4ee2 the buildings 0 - 22 the city has
		/// (StructureMarkers bit index + 1), improvement - 1 and 21 - 22 (0x81a2), and the city's wonders (0x68a0): from 0x4eef those
		/// outside mask 0x808a, from 0x4efb those in it
		/// </summary>
		void DrawCityBuildings(ushort improvement, short participantIdx, short townIdx);

		/// <summary>
		/// Cuts a picture of the city view out of screen 1. The rectangles (segment 0x3767, records of 4 words: left, top, right,
		/// bottom) are turned into left, top, width, height once, the first time (when the right of rectangle 1 is above 300).
		/// </summary>
		ushort SeizeCityScenery(ushort picture);

		/// <summary>
		/// The parade of the player's people over the bottom of the screen until a key or a click (frame by frame, 10 timer
		/// ticks each): with technology 0x22 the 10 pictures of 0x4f08 (with 0x3a) or 0x4f15, else 0x4f22; 8 figures 0x30
		/// apart moving right, all with the same picture. The pictures are cut into the stack frame, like in the original.
		/// </summary>
		void DrawCitySceneryForPlayer(short participantIdx);

		/// <summary>The animation of a riot in the city (10 pictures, others with the technology 13)</summary>
		void RiotAnimation();

		/// <summary>
		/// The parade of the human player's people (the 10 pictures of 0x4f5c with technology 0x23, else 0x4f52): 4 figures
		/// (ParadeFigureColumnsAddress, the rows after it) moving left, each with its own picture, until a key or a click.
		/// </summary>
		void DrawCitySceneryForHuman();
	}
}
