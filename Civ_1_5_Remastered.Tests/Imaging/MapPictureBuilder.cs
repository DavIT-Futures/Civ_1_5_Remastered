using Civ_1_5_Remastered.Imaging.Pictures;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Imaging
{
	/// <summary>
	/// Builds the map picture of a fixed world (EUROPE.PIC) from a grid of terrain letters, in the format of the
	/// original MAP.PIC: the 80 x 50 tiles at the top left, one colour per terrain, and its palette
	/// (opt-in tool: CIV_MAP_GRID=europe.txt; tools/maps makes the grid).
	/// </summary>
	public class MapPictureBuilder
	{
		/// <summary>The colour the game reads as each terrain (see IMapPlanes.TerrainOfColour).</summary>
		private static readonly Dictionary<char, byte> Colours = new()
		{
			['o'] = 1, ['d'] = 14, ['p'] = 6, ['g'] = 10, ['f'] = 2, ['h'] = 12,
			['m'] = 13, ['t'] = 7, ['a'] = 15, ['s'] = 3, ['j'] = 11, ['r'] = 9,
		};

		[Fact]
		public void BuildMapPicture()
		{
			string? grid = Environment.GetEnvironmentVariable("CIV_MAP_GRID");

			if (GameFiles.Folder == null || grid == null)
			{
				return;
			}

			string[] rows = File.ReadAllLines(grid);
			using VideoGraphicsBitmap original = VideoGraphicsBitmap.ViaFile(Path.Combine(GameFiles.Folder, "MAP.PIC"), out _)!;
			using VideoGraphicsBitmap map = new VideoGraphicsBitmap(320, 200);

			for (int colour = 0; colour < 256; colour++)
			{
				map.AssignHue(colour, original.ObtainHue(colour));
			}

			for (int row = 0; row < 50; row++)
			{
				for (int column = 0; column < 80; column++)
				{
					map.AssignDot(column, row, Colours[rows[row][column]]);
				}
			}

			string target = Path.Combine(Path.GetDirectoryName(grid)!, Path.GetFileNameWithoutExtension(grid).ToUpperInvariant() + ".PIC");
			map.Store(target, true);

			using VideoGraphicsBitmap check = VideoGraphicsBitmap.ViaFile(target, out byte[] swatch)!;
			Assert.Equal(Colours[rows[20][40]], check.ObtainDot(40, 20));
			Assert.NotEmpty(swatch);
		}
	}
}
