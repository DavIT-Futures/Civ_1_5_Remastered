namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IPalaceBuilder
	{
		public const ushort PalaceSectionStylesAddress = 0xb222;
		public const ushort PalaceSectionLevelsAddress = 0xd91e;

		void DisplayPalace();

		void LoadPalaceGraphics(ushort marker);

		void DrawPalaceSection(ushort sectionIndex, ushort marker);

		void DrawPalacePart(ushort level, ushort section, ushort horizontalLocation, ushort verticalShift, ushort marker);

		void LoadPalaceCastle(ushort levelMask, ushort onlyBuilt);
	}
}
