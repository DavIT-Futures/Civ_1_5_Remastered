using Civ_1_5_Remastered.Imaging.Scaling;

namespace Civ_1_5_Remastered.Tests.Imaging
{
	public class PixelArtScalerTests
	{
		private const int Ink = 1;
		private const int Paper = 0;

		[Fact]
		public void FlatAreaStaysFlat()
		{
			int[] flat = Enumerable.Repeat(Ink, 9).ToArray();

			foreach (PixelArtSharpness sharpness in Enum.GetValues<PixelArtSharpness>())
			{
				int factor = PixelArtScaler.Factor(sharpness);
				int[] scaled = PixelArtScaler.Upscale(flat, 3, 3, sharpness);

				Assert.Equal(9 * factor * factor, scaled.Length);
				Assert.All(scaled, pixel => Assert.Equal(Ink, pixel));
			}
		}

		[Fact]
		public void DoubleRoundsTheStepOfADiagonal()
		{
			// A staircase diagonal: the corner pixel next to both inked neighbours takes their colour.
			int[] stairs =
			{
				Ink, Paper, Paper,
				Ink, Ink, Paper,
				Ink, Ink, Ink,
			};

			int[] scaled = PixelArtScaler.Double(stairs, 3, 3);

			// Pixel (1, 0) is paper with ink to the west and below: its bottom-left quarter becomes ink.
			Assert.Equal(Ink, scaled[(1 * 6) + 2]);
			Assert.Equal(Paper, scaled[3]);
		}

		[Fact]
		public void TripleKeepsSinglePixels()
		{
			int[] dot =
			{
				Paper, Paper, Paper,
				Paper, Ink, Paper,
				Paper, Paper, Paper,
			};

			int[] scaled = PixelArtScaler.Triple(dot, 3, 3);

			Assert.Equal(9, scaled.Count(pixel => pixel == Ink));
		}

		[Fact]
		public void NoNewColoursAppear()
		{
			int[] picture = { 5, 7, 9, 7, 5, 7, 9, 7, 5, 1, 2, 3 };

			int[] scaled = PixelArtScaler.Upscale(picture, 4, 3, PixelArtSharpness.Quadruple);

			Assert.All(scaled, pixel => Assert.Contains(pixel, picture));
		}
	}
}
