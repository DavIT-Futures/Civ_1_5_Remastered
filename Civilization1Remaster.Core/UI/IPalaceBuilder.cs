using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of Palace that the other parts of the game use</summary>
	public interface IPalaceBuilder
	{
		/// <summary>
		/// The palace screen: the announcement of the upgrade, the palace with its sections, the player picks the part to
		/// improve
		/// </summary>
		void DisplayPalace();

		/// <summary>
		/// Draws the palace on screen 1 (the parts of sections 0 - 8 in the words at 0xd91e, 0xffff not built): the looks of
		/// sections 1 - 7, the castle, the background (0x4b81 for the palace screen, 0x4b89), the garden "cbacksN.pic" (N =
		/// 0xd932), the sections from x 0x10 (8 further left without section 2, one with 0xb22a = 1): the left ones with their
		/// wings, the middle one, the right ones with their towers, each joined to a neighbour of the same style (0xb222), and
		/// the bushes "cbrushN.pic" left (0xd930) and right (0xd934). For the palace screen (marker) screen 1 is shown with the
		/// palette 0x4bb0. The file name is in the stack frame, like in the original, and the rightmost built section (0x18)
		/// and the last left section (0x12) are read from the frame even when this call didn't set them, like in the original.
		/// </summary>
		void LoadPalaceGraphics(ushort marker);

		/// <summary>
		/// The look of the palace section sectionIdx (0 - 7, levels in the words at 0xd91c...) by its neighbours: the style (0xb25e
		/// + 2 * section) and the shape (0xb25f + 2 * section). Section 4 is the middle; a left section (0 - 3) without a
		/// built neighbour to the left (or section 1) is the left end, a right section (5 - 7) without one to the right (or
		/// section 7) the right end.
		/// </summary>
		void DrawPalaceSection(ushort sectionIdx, ushort marker);

		/// <summary>
		/// One part of the palace: picture (table at 0xb23c, by the part section and its style marker) at column horizLoc; the row comes
		/// from the height table at 0x4b36 (records of 15 bytes by level, by the place of the part) plus vertShift and level
		/// </summary>
		void DrawPalacePart(ushort level, ushort section, ushort horizLoc, ushort vertShift, ushort marker);

		/// <summary>
		/// Cuts the pictures of the palace sections 1 - 7 from the castle pictures castle0 - castle4.pic (each one in the mask
		/// levelMask): with onlyBuilt the sections of that level (0xd91e) from the place of the section (0xb222: bit 0 the column, bit 1 the
		/// row), without onlyBuilt all sections from the place of the section before. Each section has two parts (styles at 0xb25e:
		/// 0 the whole wall, 1 and 2 left and right pieces, 3 and 4 the ends, cut from the middle picture for section 2 or a
		/// section at the place 1), the picture goes to 0xb23c + 4 * section + 2 * part, the first one also to 0x6828 when unset.
		/// </summary>
		void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt);
	}
}
