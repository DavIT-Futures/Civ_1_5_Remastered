namespace Civ_1_5_Remastered.Imaging.Scaling
{
	/// <summary>How the 320 x 200 picture is turned into the window picture.</summary>
	public enum PixelArtSharpness
	{
		/// <summary>Original pixels, enlarged as blocks.</summary>
		Original = 1,

		/// <summary>Twice the density, edges smoothed (Scale2x).</summary>
		Double = 2,

		/// <summary>Three times the density, edges smoothed (Scale3x).</summary>
		Triple = 3,

		/// <summary>Four times the density, edges smoothed twice (Scale2x applied twice).</summary>
		Quadruple = 4,
	}
}
