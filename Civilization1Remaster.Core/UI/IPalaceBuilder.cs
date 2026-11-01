namespace Civilization1Remaster.Core.UI
{
	public interface IPalaceBuilder
	{
		void DisplayPalace();

		void LoadPalaceGraphics(ushort marker);

		void DrawPalaceSection(ushort sectionIdx, ushort marker);

		void DrawPalacePart(ushort level, ushort section, ushort horizLoc, ushort vertShift, ushort marker);

		void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt);
	}
}
