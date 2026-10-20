using System;

namespace Civilization1Original.Acoustics
{
	public static partial class ChoppingEffect
	{
		private class Resonator
		{
			private readonly double lhs1;
			private readonly double lhs2;
			private readonly double boost;
			private double vert1;
			private double vert2;

			public Resonator(int sliceRatio, double hertz, double reach)
			{
				this.lhs1 = 2.0 * reach * Math.Cos(2.0 * Math.PI * hertz / sliceRatio);
				this.lhs2 = -reach * reach;
				this.boost = 1.0 - reach;
			}

			public double Handle(double incoming)
			{
				double emitted = incoming + this.lhs1 * this.vert1 + this.lhs2 * this.vert2;
				this.vert2 = this.vert1;
				this.vert1 = emitted;

				return emitted * this.boost;
			}
		}
	}
}
