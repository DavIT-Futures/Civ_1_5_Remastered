using System.Collections.Generic;

namespace Civ_1_5_Remastered.Imaging.Typefaces
{
	public static class CivilizationFonts
	{
		public const string FileName = "FONTS.CV";

		private const int HeaderSize = 8;

		public static Dictionary<int, CivilizationFont> Read(byte[] fontsFile)
		{
			Dictionary<int, CivilizationFont> typefaces = new Dictionary<int, CivilizationFont>();
			int fontCount = ReadWord(fontsFile, 0);

			for (int fontIndex = 0; fontIndex < fontCount; fontIndex++)
			{
				int picture = ReadWord(fontsFile, 2 + (fontIndex * 2));
				int header = picture - HeaderSize;
				int firstLetter = fontsFile[header];
				int lastLetter = fontsFile[header + 1];
				int rowBytes = ReadWord(fontsFile, header + 2);
				int height = fontsFile[header + 4];
				int letterCount = lastLetter - firstLetter + 1;
				int widths = header - letterCount;

				CivilizationFont typeface = new CivilizationFont
				{
					StrokeGap = fontsFile[header + 5],
					LetterBreadthGap = fontsFile[header + 6],
				};

				for (int letterIndex = 0; letterIndex < letterCount - 1; letterIndex++)
				{
					int width = fontsFile[widths + letterIndex];
					int[][] shape = new int[height][];

					for (int row = 0; row < shape.Length; row++)
					{
						int at = picture + (((row * letterCount) + letterIndex) * rowBytes);
						int bits = 0;

						for (int part = 0; part < rowBytes; part++)
						{
							bits = (bits << 8) | fontsFile[at + part];
						}

						shape[row] = new int[width];

						for (int column = 0; column < width; column++)
						{
							shape[row][column] = (bits >> ((rowBytes * 8) - 1 - column)) & 1;
						}
					}

					typeface.Letters[(char)(firstLetter + letterIndex)] = new CivilizationFontCharacter
					{
						Breadth = width,
						Altitude = height,
						Picture = shape,
					};
				}

				typefaces[fontIndex + 1] = typeface;
			}

			return typefaces;
		}

		private static int ReadWord(byte[] content, int at) => content[at] | (content[at + 1] << 8);
	}
}
