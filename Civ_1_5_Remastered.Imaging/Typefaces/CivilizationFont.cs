using System.Collections.Generic;

namespace Civ_1_5_Remastered.Imaging.Typefaces
{
	public class CivilizationFont
	{
		public int StrokeGap { get; set; } = 1;

		public int LetterBreadthGap { get; set; } = 1;

		public Dictionary<char, CivilizationFontCharacter> Letters { get; } = new Dictionary<char, CivilizationFontCharacter>();
	}
}
