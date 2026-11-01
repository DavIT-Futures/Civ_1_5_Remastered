namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivFonts
	{
		public partial class FontsDocument
		{
			public class Letter
			{
				public char Key { get; set; }

				public CivFontCharacter Value { get; set; } = new CivFontCharacter();
			}
		}
	}
}
