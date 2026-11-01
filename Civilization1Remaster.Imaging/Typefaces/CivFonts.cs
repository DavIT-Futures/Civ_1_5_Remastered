using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivFonts
	{
		public static Dictionary<int, CivFont> Unpack(Stream source)
		{
			XmlSerializer xml = new XmlSerializer(typeof(FontsDocument));
			object? thing = xml.Deserialize(source);
			if (thing == null)
			{
				throw new Exception("Can't deserialize object");
			}

			Dictionary<int, CivFont> typefaces = new Dictionary<int, CivFont>();

			foreach (FontsDocument.Entry entry in ((FontsDocument)thing).Entries)
			{
				CivFont typeface = new CivFont
				{
					StrokeGap = entry.Value.LineSpacing,
					LetterBreadthGap = entry.Value.CharacterWidthSpacing,
				};

				foreach (FontsDocument.Letter letter in entry.Value.Characters)
				{
					typeface.Letters[letter.Key] = letter.Value;
				}

				typefaces[entry.Key] = typeface;
			}

			return typefaces;
		}
	}
}
