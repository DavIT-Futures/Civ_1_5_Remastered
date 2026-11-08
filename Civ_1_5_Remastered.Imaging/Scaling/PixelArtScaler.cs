using System;

namespace Civ_1_5_Remastered.Imaging.Scaling
{
	/// <summary>
	/// Pixel art upscalers of the EPX family (Scale2x, Scale3x). They raise the density of the picture and
	/// follow the diagonal edges of the drawing instead of enlarging every pixel into a block, without blurring
	/// and without inventing colours that are not in the original picture.
	/// </summary>
	public static class PixelArtScaler
	{
		/// <summary>Size factor of the picture produced for <paramref name="sharpness"/>.</summary>
		public static int Factor(PixelArtSharpness sharpness) => sharpness switch
		{
			PixelArtSharpness.Double => 2,
			PixelArtSharpness.Triple => 3,
			PixelArtSharpness.Quadruple => 4,
			_ => 1,
		};

		/// <summary>Upscales <paramref name="source"/> (row after row, <paramref name="width"/> pixels each).</summary>
		/// <returns>A picture <see cref="Factor"/> times wider and higher.</returns>
		public static int[] Upscale(int[] source, int width, int height, PixelArtSharpness sharpness)
		{
			return sharpness switch
			{
				PixelArtSharpness.Double => Double(source, width, height),
				PixelArtSharpness.Triple => Triple(source, width, height),
				PixelArtSharpness.Quadruple => Double(Double(source, width, height), width * 2, height * 2),
				_ => (int[])source.Clone(),
			};
		}

		/// <summary>Scale2x: every pixel becomes 2 x 2 pixels.</summary>
		public static int[] Double(int[] source, int width, int height)
		{
			ArgumentNullException.ThrowIfNull(source);

			int[] target = new int[width * height * 4];
			int targetWidth = width * 2;

			for (int row = 0; row < height; row++)
			{
				int above = Math.Max(row - 1, 0) * width;
				int here = row * width;
				int below = Math.Min(row + 1, height - 1) * width;
				int top = row * 2 * targetWidth;
				int bottom = top + targetWidth;

				for (int column = 0; column < width; column++)
				{
					int left = Math.Max(column - 1, 0);
					int right = Math.Min(column + 1, width - 1);

					int centre = source[here + column];
					int up = source[above + column];
					int down = source[below + column];
					int west = source[here + left];
					int east = source[here + right];

					int spot = column * 2;

					if (up != down && west != east)
					{
						target[top + spot] = west == up ? west : centre;
						target[top + spot + 1] = up == east ? east : centre;
						target[bottom + spot] = west == down ? west : centre;
						target[bottom + spot + 1] = down == east ? east : centre;
					}
					else
					{
						target[top + spot] = centre;
						target[top + spot + 1] = centre;
						target[bottom + spot] = centre;
						target[bottom + spot + 1] = centre;
					}
				}
			}

			return target;
		}

		/// <summary>Scale3x: every pixel becomes 3 x 3 pixels.</summary>
		public static int[] Triple(int[] source, int width, int height)
		{
			ArgumentNullException.ThrowIfNull(source);

			int[] target = new int[width * height * 9];
			int targetWidth = width * 3;

			for (int row = 0; row < height; row++)
			{
				int above = Math.Max(row - 1, 0) * width;
				int here = row * width;
				int below = Math.Min(row + 1, height - 1) * width;
				int first = row * 3 * targetWidth;
				int second = first + targetWidth;
				int third = second + targetWidth;

				for (int column = 0; column < width; column++)
				{
					int left = Math.Max(column - 1, 0);
					int right = Math.Min(column + 1, width - 1);

					int northWest = source[above + left];
					int north = source[above + column];
					int northEast = source[above + right];
					int west = source[here + left];
					int centre = source[here + column];
					int east = source[here + right];
					int southWest = source[below + left];
					int south = source[below + column];
					int southEast = source[below + right];

					int spot = column * 3;

					int topLeft = centre;
					int topMiddle = centre;
					int topRight = centre;
					int middleLeft = centre;
					int middleRight = centre;
					int bottomLeft = centre;
					int bottomMiddle = centre;
					int bottomRight = centre;

					if (north != south && west != east)
					{
						topLeft = west == north ? west : centre;
						topMiddle = (west == north && centre != northEast) || (north == east && centre != northWest) ? north : centre;
						topRight = north == east ? east : centre;
						middleLeft = (west == north && centre != southWest) || (west == south && centre != northWest) ? west : centre;
						middleRight = (north == east && centre != southEast) || (south == east && centre != northEast) ? east : centre;
						bottomLeft = west == south ? west : centre;
						bottomMiddle = (west == south && centre != southEast) || (south == east && centre != southWest) ? south : centre;
						bottomRight = south == east ? east : centre;
					}

					target[first + spot] = topLeft;
					target[first + spot + 1] = topMiddle;
					target[first + spot + 2] = topRight;
					target[second + spot] = middleLeft;
					target[second + spot + 1] = centre;
					target[second + spot + 2] = middleRight;
					target[third + spot] = bottomLeft;
					target[third + spot + 1] = bottomMiddle;
					target[third + spot + 2] = bottomRight;
				}
			}

			return target;
		}
	}
}
