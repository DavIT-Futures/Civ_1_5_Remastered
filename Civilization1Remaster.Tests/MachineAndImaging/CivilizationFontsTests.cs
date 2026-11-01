using System.Xml.Linq;
using Civilization1Remaster.Imaging.Typefaces;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class CivilizationFontsTests
	{
		[Fact]
		public void BuiltInFontsMatchTheMarkupResource()
		{
			Dictionary<int, CivilizationFont> fonts = CivilizationFonts.Unpack(GameFiles.FontsMarkup());
			XElement root = XDocument.Load(GameFiles.FontsMarkup()).Root!;
			XElement[] entries = root.Elements("BKeyValuePairOfInt32CivFont").ToArray();

			Assert.Equal(9, entries.Length);
			Assert.Equal(entries.Select(entry => (int)entry.Element("Key")!), fonts.Keys);

			foreach (XElement entry in entries)
			{
				CivilizationFont font = fonts[(int)entry.Element("Key")!];
				XElement value = entry.Element("Value")!;
				XElement[] letters = value.Element("Characters")!.Elements("BKeyValuePairOfCharCivFontCharacter").ToArray();

				Assert.Equal((int)value.Element("LineSpacing")!, font.StrokeGap);
				Assert.Equal((int)value.Element("CharacterWidthSpacing")!, font.LetterBreadthGap);
				Assert.Equal(letters.Length, font.Letters.Count);

				foreach (XElement letter in letters)
				{
					CivilizationFontCharacter shape = font.Letters[(char)(int)letter.Element("Key")!];
					XElement shapeValue = letter.Element("Value")!;
					int[][] bitmap = shapeValue.Element("Bitmap")!.Elements("ArrayOfInt")
						.Select(row => row.Elements("int").Select(dot => (int)dot).ToArray()).ToArray();

					Assert.Equal((int)shapeValue.Element("Width")!, shape.Breadth);
					Assert.Equal((int)shapeValue.Element("Height")!, shape.Altitude);
					Assert.Equal(bitmap, shape.Picture);
				}
			}
		}

		[Fact]
		public void EveryFontHasTheQuestionMarkFallback()
		{
			foreach (CivilizationFont font in CivilizationFonts.Unpack(GameFiles.FontsMarkup()).Values)
			{
				Assert.True(font.Letters.ContainsKey('?'));
			}
		}
	}
}
