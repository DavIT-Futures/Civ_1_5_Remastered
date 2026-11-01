using System;

namespace Civilization1Remaster.GameModel.State
{
	public sealed class ChartSightGrid
	{
		public const int Width = 80;
		public const int Height = 50;

		private readonly sbyte[,] grid = new sbyte[Width, Height];

		public sbyte this[int horizontal, int vertical]
		{
			get
			{
				if (vertical >= 65500)
				{
					vertical = (short)(ushort)vertical;
				}

				while (horizontal < 0) horizontal += Width;
				while (horizontal >= Width) horizontal -= Width;

				if (vertical < 0 || vertical >= Height)
				{
					return 0;
				}

				return this.grid[horizontal, vertical];
			}
			set
			{
				if (vertical >= 65500)
				{
					vertical = (short)(ushort)vertical;
				}

				while (horizontal < 0) horizontal += Width;
				while (horizontal >= Width) horizontal -= Width;

				if (vertical < 0 || vertical >= Height)
				{
					return;
				}

				this.grid[horizontal, vertical] = value;
			}
		}

		public int GetLength(int dimension)
		{
			return dimension switch
			{
				0 => Width,
				1 => Height,
				_ => throw new ArgumentOutOfRangeException(nameof(dimension))
			};
		}

		public void Clear()
		{
			for (int horizontal = 0; horizontal < Width; horizontal++)
			{
				for (int vertical = 0; vertical < Height; vertical++)
				{
					this.grid[horizontal, vertical] = 0;
				}
			}
		}
	}
}
