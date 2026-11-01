namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivilizationFonts
	{
		public partial class FontsDocument
		{
			public class Letter
			{
				public char Key { get; set; }

				public CivilizationFontCharacter Value { get; set; } = new CivilizationFontCharacter();
			}
		}
	}
}
