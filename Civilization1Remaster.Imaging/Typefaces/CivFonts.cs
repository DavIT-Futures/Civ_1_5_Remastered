using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	/// <summary>
	/// Loads the built-in fonts (the Fonts.xml.gz resource) into fonts by number.
	/// </summary>
	public static class CivFonts
	{
		public static Dictionary<int, CivFont> Unpack(Stream source)
		{
			XmlSerializer xml = new XmlSerializer(typeof(FontsDocument));
			object? thing = xml.Deserialize(source);
			if (thing == null)
				throw new Exception("Can't deserialize object");

			Dictionary<int, CivFont> typefaces = new Dictionary<int, CivFont>();

			foreach (FontsDocument.Entry entry in ((FontsDocument)thing).Entries)
			{
				CivFont typeface = new CivFont
				{
					StrokeGap = entry.Value.LineSpacing,
					LetterBreadthGap = entry.Value.CharacterWidthSpacing,
				};

				foreach (FontsDocument.Letter letter in entry.Value.Characters)
					typeface.Letters[letter.Key] = letter.Value;

				typefaces[entry.Key] = typeface;
			}

			return typefaces;
		}

		/// <summary>
		/// The layout of Fonts.xml. The element names are those the resource was written with.
		/// </summary>
		[XmlRoot("ArrayOfBKeyValuePairOfInt32CivFont")]
		public class FontsDocument
		{
			[XmlElement("BKeyValuePairOfInt32CivFont")]
			public List<Entry> Entries { get; set; } = new List<Entry>();

			public class Entry
			{
				public int Key { get; set; }

				public Font Value { get; set; } = new Font();
			}

			public class Font
			{
				public int LineSpacing { get; set; } = 1;

				public int CharacterWidthSpacing { get; set; } = 1;

				[XmlArray("Characters")]
				[XmlArrayItem("BKeyValuePairOfCharCivFontCharacter")]
				public List<Letter> Characters { get; set; } = new List<Letter>();
			}

			public class Letter
			{
				public char Key { get; set; }

				public CivFontCharacter Value { get; set; } = new CivFontCharacter();
			}
		}
	}
}
