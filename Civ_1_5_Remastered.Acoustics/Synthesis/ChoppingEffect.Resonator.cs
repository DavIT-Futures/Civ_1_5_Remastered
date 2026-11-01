using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public static partial class ChoppingEffect
	{
		private class Resonator
		{
			private readonly double firstLeft;
			private readonly double secondLeft;
			private readonly double boost;

			private double firstVertical;
			private double secondVertical;

			public Resonator(int sliceRatio, double hertz, double reach)
			{
				this.firstLeft = 2.0 * reach * Math.Cos(2.0 * Math.PI * hertz / sliceRatio);
				this.secondLeft = -reach * reach;
				this.boost = 1.0 - reach;
			}

			public double Handle(double incoming)
			{
				double emitted = incoming + (this.firstLeft * this.firstVertical) + (this.secondLeft * this.secondVertical);
				this.secondVertical = this.firstVertical;
				this.firstVertical = emitted;

				return emitted * this.boost;
			}
		}
	}
}
