
namespace Civilization1Remaster.Core.UI
{
	/// <summary>The operations of Civilopedia that the other parts of the game use</summary>
	public interface IEncyclopedia
	{
		/// <summary>
		/// The Civilopedia, at the part chosen in its menu: shows pages of the index until one answers 0xfffe (leave);
		/// 0xffff turns to the next page (78 entries further) when there is one (0x6808), else back to the first
		/// </summary>
		void ShowCivilopedia(ushort part);

		/// <summary>
		/// One page of the Civilopedia index: the entries of the part (0 technologies, 1 improvements and wonders, 2 units,
		/// 3 terrains, 4 concepts, -1 all of them) in alphabetical order, from entry firstEntry on, in columns of 26 (wider for
		/// one part) until the page is full (0x6808 = 1: there is a next page); the entry kind and number of each line go to
		/// 0x3772:0x36fc and 0x1c80. The entry is chosen with the mouse or the arrows (Enter, space); its record is shown and
		/// a key awaited.
		/// </summary>
		/// <returns>0 for no entry under the choice; 0xffff for the next page ("m", a click on the top right); 0xfffe to
		/// leave (Esc, "x", a click on the top left); else the line number; also in AX</returns>
		ushort ShowCivilopediaIndex(ushort firstEntry, ushort part);

		void ShowCivilopediaRecord(ushort scienceIdx, ushort part);

		/// <summary>
		/// Index of the technology in the list of the technologies that lead to others first (72 bytes at 0x371e:0000)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		ushort ObtainFirstPrerequisite(ushort scienceIdx);

		/// <summary>
		/// Copies a picture part (left, top, width, height in the back screen 0x19d4) centred at (centreHoriz, centreVert) on the
		/// screen: the part is cut at the screen edge (319, 199), and narrowed so that it does not start left of the screen
		/// </summary>
		void DrawIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHoriz, ushort centreVert);

		/// <summary>
		/// Index of the technology in the second list of the Civilopedia (28 bytes at 0x371e:0044)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		ushort ObtainSecondPrerequisite(ushort scienceIdx);

		/// <summary>
		/// Index of the technology in the list of the technology categories of the Civilopedia (12 bytes at 0x371e:0061)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		ushort ObtainTechnologyCategory(ushort scienceIdx);
	}
}
