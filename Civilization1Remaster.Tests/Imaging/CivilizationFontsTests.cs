using Civilization1Remaster.Foundation.Squeeze;
using Civilization1Remaster.Imaging.Typefaces;

namespace Civilization1Remaster.Tests.Imaging
{
	public class CivilizationFontsTests
	{
		private const uint ShapesChecksum = 1665267908u;

		[Fact]
		public void TheGameHasNineFonts()
		{
			Dictionary<int, CivilizationFont>? fonts = GameFonts();
			if (fonts == null)
			{
				return;
			}

			Assert.Equal(Enumerable.Range(1, 9), fonts.Keys);
			Assert.Equal(new[] { 95, 95, 95, 90, 93, 95, 95, 95, 94 }, fonts.Values.Select(font => font.Letters.Count));
		}

		[Fact]
		public void TheExclamationMarkOfTheFirstFontIsTwoDotsWide()
		{
			Dictionary<int, CivilizationFont>? fonts = GameFonts();
			if (fonts == null)
			{
				return;
			}

			CivilizationFontCharacter mark = fonts[1].Letters['!'];
			string[] rows = mark.Picture.Select(row => string.Concat(row.Select(dot => dot != 0 ? 'X' : '.'))).ToArray();

			Assert.Equal(2, mark.Breadth);
			Assert.Equal(7, mark.Altitude);
			Assert.Equal(new[] { "..", "XX", "XX", "XX", "..", "XX", ".." }, rows);
		}

		[Fact]
		public void EveryFontHasTheQuestionMarkFallback()
		{
			Dictionary<int, CivilizationFont>? fonts = GameFonts();
			if (fonts == null)
			{
				return;
			}

			Assert.All(fonts.Values, font => Assert.True(font.Letters.ContainsKey('?')));
		}

		[Fact]
		public void AllShapesMatchTheKnownChecksum()
		{
			Dictionary<int, CivilizationFont>? fonts = GameFonts();
			if (fonts == null)
			{
				return;
			}

			List<byte> shapes = new List<byte>();

			foreach (KeyValuePair<int, CivilizationFont> font in fonts)
			{
				shapes.AddRange(new[] { (byte)font.Key, (byte)font.Value.StrokeGap, (byte)font.Value.LetterBreadthGap });

				foreach (KeyValuePair<char, CivilizationFontCharacter> letter in font.Value.Letters)
				{
					shapes.AddRange(new[] { (byte)letter.Key, (byte)letter.Value.Breadth, (byte)letter.Value.Altitude });
					shapes.AddRange(letter.Value.Picture.SelectMany(row => row).Select(dot => (byte)dot));
				}
			}

			Assert.Equal(ShapesChecksum, CyclicRedundancyCheck.ObtainChecksum(shapes.ToArray()));
		}

		private static Dictionary<int, CivilizationFont>? GameFonts() =>
			GameFiles.Folder == null ? null : CivilizationFonts.Read(File.ReadAllBytes(Path.Combine(GameFiles.Folder, CivilizationFonts.FileName)));
	}
}
