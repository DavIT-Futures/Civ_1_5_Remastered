using System.Collections.Generic;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public class CivFont
	{
		public int StrokeGap { get; set; } = 1;

		public int LetterBreadthGap { get; set; } = 1;

		public Dictionary<char, CivFontCharacter> Letters { get; } = new Dictionary<char, CivFontCharacter>();
	}
}
