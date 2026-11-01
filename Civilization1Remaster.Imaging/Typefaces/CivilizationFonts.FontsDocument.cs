using System.Collections.Generic;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public static partial class CivilizationFonts
	{
		[XmlRoot("ArrayOfBKeyValuePairOfInt32CivFont")]
		public partial class FontsDocument
		{
			[XmlElement("BKeyValuePairOfInt32CivFont")]
			public List<Entry> Entries { get; set; } = new List<Entry>();
		}
	}
}
