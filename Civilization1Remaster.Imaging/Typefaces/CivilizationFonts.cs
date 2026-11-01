using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivilizationFonts
	{
		public static Dictionary<int, CivilizationFont> Unpack(Stream source)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(FontsDocument));
			object? thing = serializer.Deserialize(source);
			if (thing == null)
			{
				throw new Exception("Can't deserialize object");
			}

			Dictionary<int, CivilizationFont> typefaces = new Dictionary<int, CivilizationFont>();

			foreach (FontsDocument.Entry entry in ((FontsDocument)thing).Entries)
			{
				CivilizationFont typeface = new CivilizationFont
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
