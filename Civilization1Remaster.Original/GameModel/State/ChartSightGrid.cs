using System;

namespace Civilization1Original
{
	/// <summary>
	/// 80x50 map visibility grid with safe coordinate wrapping and pole bounds clamping.
	/// Prevents out-of-bounds crashes at north and south poles (Y &lt; 0 or Y &gt;= 50)
	/// while supporting cyclic X wrapping.
	/// </summary>
	public sealed class ChartSightGrid
	{
		public const int Width = 80;
		public const int Height = 50;

		private readonly sbyte[,] grid = new sbyte[Width, Height];

		public sbyte this[int x, int y]
		{
			get
			{
				if (y >= 65500)
				{
					y = (short)(ushort)y;
				}

				while (x < 0) x += Width;
				while (x >= Width) x -= Width;

				if (y < 0 || y >= Height)
				{
					return 0;
				}

				return this.grid[x, y];
			}
			set
			{
				if (y >= 65500)
				{
					y = (short)(ushort)y;
				}

				while (x < 0) x += Width;
				while (x >= Width) x -= Width;

				if (y < 0 || y >= Height)
				{
					return;
				}

				this.grid[x, y] = value;
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
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					this.grid[x, y] = 0;
				}
			}
		}
	}
}
