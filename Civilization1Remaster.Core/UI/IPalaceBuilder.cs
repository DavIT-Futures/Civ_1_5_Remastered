namespace Civilization1Remaster.Core.UI
{
	public interface IPalaceBuilder
	{
		void DisplayPalace();

		void LoadPalaceGraphics(ushort marker);

		void DrawPalaceSection(ushort sectionIndex, ushort marker);

		void DrawPalacePart(ushort level, ushort section, ushort horizontalLocation, ushort verticalShift, ushort marker);

		void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt);
	}
}
