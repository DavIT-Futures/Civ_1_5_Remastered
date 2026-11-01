using System.Collections.Generic;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public class CivilizationFont
	{
		public int StrokeGap { get; set; } = 1;

		public int LetterBreadthGap { get; set; } = 1;

		public Dictionary<char, CivilizationFontCharacter> Letters { get; } = new Dictionary<char, CivilizationFontCharacter>();
	}
}
