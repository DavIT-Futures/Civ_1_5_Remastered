using System.Collections.Generic;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivilizationFonts
	{
		public partial class FontsDocument
		{
			public class Font
			{
				public int LineSpacing { get; set; } = 1;

				public int CharacterWidthSpacing { get; set; } = 1;

				[XmlArray("Characters")]
				[XmlArrayItem("BKeyValuePairOfCharCivFontCharacter")]
				public List<Letter> Characters { get; set; } = new List<Letter>();
			}
		}
	}
}
