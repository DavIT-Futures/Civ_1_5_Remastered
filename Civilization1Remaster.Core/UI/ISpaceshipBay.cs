using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of SpaceshipBay that the other parts of the game use</summary>
	public interface ISpaceshipBay
	{
		/// <summary>
		/// The space background: a black screen with the picture 0x4bc4, its 10 parts cut out (rectangles of 13 words at 0x4be2,
		/// the pictures go to 0x682a), 20 stars at random places (0x683e, 0x6852, 0x6866), the colour cycles 1 - 5 set up and,
		/// when 0xd762 is set, started
		/// </summary>
		void RenderSpaceScenery();

		/// <summary>
		/// Leaves the spaceship screen: frees its picture (0x682a), stops the colour cycles 1 - 5 when 0xd762 is set, loads the
		/// palette of the human player's government, "backNa.pal" (N is the government / 2, "m" instead of "a" with technology
		/// 20), and the palette 0x4cea, clears the screen and draws the map screen again. The file name is made in a buffer in
		/// the stack frame, like in the original.
		/// </summary>
		void ImportSpaceScenery();

		/// <summary>
		/// The spaceship of the player: its parts on the layout (segment 0x3772, 0x16e0 + 0xb4 * player; places in the table
		/// 0x375a), counted per kind in 0xdea2 when connected to a neighbour (structures always), the mass (part masses 0x4be0),
		/// the colonists (0xdeae), support, energy, fuel, flight time and the chance of success; the arrival year of a ship
		/// not launched yet. Shown (with a key awaited and the stars moving until a key, a click or frames frames) for the human
		/// player, or for all with the spaceship option (0x20); the human player may launch it ("l" or the button) when launchable
		/// and frames &lt; 0.
		/// </summary>
		/// <returns>The chance of success in percent, also in AX</returns>
		ushort RenderSpaceship(short participantIdx, short frames, short launchable);

		/// <summary>
		/// Is the place of the spaceship of the player taken by a part: the byte of the spaceship layout (segment 0x3772,
		/// 0x16e0 + 0xb4 * player + 0xc * row + column) is not empty (0xff) and below 3
		/// </summary>
		/// <returns>1 or 0, also in AX</returns>
		ushort ObtainSpaceshipPartValue(short participantIdx, ushort row, ushort column);

		/// <summary>
		/// Moves the 20 stars of the space background one step to the right (0x683e: column, 0x6852: row, 0x6866: drawn):
		/// every 4th star 2 pixels (colour 7), the next 1 pixel (colour 3), the others 1 pixel every second frame (colour 8,
		/// frame counter 0x687a); a star past column 239 starts again at column 1 on a random row. A star is drawn only on
		/// black. The positions are read as signed bytes, like in the original.
		/// </summary>
		void RenderStarField();

		/// <summary>
		/// The AI decides about a spaceship part of the kind partKind (0 structure, 1 component, 2 module): the places of the table
		/// 0x375a are searched from the first one (from 0x21 for modules, or when the player's ship arrives within 15 years and
		/// before the human player's) for an empty place of a part of this kind; the first empty place that would take a part
		/// of the kind is remembered, and the search gives up 4 places after it (not for modules) when the player knows the
		/// technology of the part (0xe68 + 0x1e * kind). An empty place of this kind is built (shown with the space scenery,
		/// the ship redrawn, its chance of success in BotStarshipVictoryRatio, the ship bit 8 + player set).
		/// </summary>
		/// <returns>0 when a part was built, 1 when not (none found but a later place exists), 2 when no place at all; also in AX</returns>
		ushort ComputeSpaceshipSuccess(short participantIdx, short partKind);

		/// <summary>
		/// A new part of the player's spaceship: partKind 0 a structure, 1 a component (dialog 0x4df9: the kind 0 or 1 becomes 3 or 4,
		/// unless all 3 are there, 0x687c), 2 a module (dialog 0x4e12: kind 6 - 8, unless all 7 are there, 0x687e); a cancelled
		/// dialog leaves without a part. The part goes into a free slot of its kind in the grid 0x3772:0x16e0 + 0xb4 * player
		/// (slots: table 0x375a of row, column and kind, ending with row 0xff); a structure into the first free one of the inner
		/// 0x21 slots or of the outer ones, whichever side has the shorter span to the last built part. The first spaceship
		/// part of the player (StarshipMarkers bit 8 + player) clears TouchParticipantTimeleft of every player 1 - 7 with one.
		/// </summary>
		void AttachSpaceshipPart(short participantIdx, ushort partKind);

		/// <summary>
		/// When some player 1 - 7 has a spaceship (StarshipMarkers bits 9 - 15), asks which nation's spaceship to show (the
		/// names at 0x1982 + 2 * player; 0xb276 masks the ones without a spaceship) and shows it
		/// </summary>
		void ChooseNationToView();

		/// <summary>
		/// The player launches its spaceship (StarshipMarkers bit player): a black screen with "... LAUNCH space ship!" and the
		/// estimated arrival, the launch year is noted, then the spaceship is shown (on the space background, which is left
		/// again afterwards, for another player than the human)
		/// </summary>
		void FireSpaceship(short participantIdx);
	}
}
