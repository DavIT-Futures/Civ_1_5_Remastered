using System;
using System.Xml.Serialization;
using Civilization1Original.Containers;

namespace Civilization1Original.Video
{
	[Serializable]
	public class CivFont
	{
		private readonly BDictionary<char, CivFontCharacter> letters = new BDictionary<char, CivFontCharacter>();
		private int letterBreadthGap = 1;
		private int strokeGap = 1;

		public CivFont()
		{ }

		[XmlElement("LineSpacing")]
		public int StrokeGap
		{
			get { return this.strokeGap; }
			set { this.strokeGap = value; }
		}

		[XmlElement("CharacterWidthSpacing")]
		public int LetterBreadthGap
		{
			get { return this.letterBreadthGap; }
			set { this.letterBreadthGap = value; }
		}

		[XmlArray("Characters")]
		public BDictionary<char, CivFontCharacter> Letters
		{
			get { return this.letters; }
		}
	}
}
