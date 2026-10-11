using System;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class ChartSightGridTests
	{
		[Fact]
		public void NormalAccess_SetsAndGetsCorrectValues()
		{
			var grid = new ChartSightGrid();
			grid[10, 20] = 5;

			Assert.Equal(5, grid[10, 20]);
		}

		[Fact]
		public void XWrapping_WrapsCyclically()
		{
			var grid = new ChartSightGrid();
			grid[0, 10] = 42;
			grid[79, 10] = 99;

			Assert.Equal(42, grid[80, 10]);
			Assert.Equal(42, grid[-80, 10]);
			Assert.Equal(99, grid[-1, 10]);

			grid[-1, 15] = 7;
			Assert.Equal(7, grid[79, 15]);
		}

		[Fact]
		public void NorthPoleOutOfBounds_ReturnsZeroAndIgnoresSet()
		{
			var grid = new ChartSightGrid();

			// Negative Y (North of pole)
			Assert.Equal(0, grid[10, -1]);
			grid[10, -1] = 12;
			Assert.Equal(0, grid[10, -1]);

			// 16-bit ushort wrapping (65535 = -1)
			ushort northY = 65535;
			Assert.Equal(0, grid[10, northY]);
			grid[10, northY] = 15;
			Assert.Equal(0, grid[10, northY]);

			// Compound assignment should not throw
			grid[10, -1] |= 1;
			grid[10, northY] |= 2;
			Assert.Equal(0, grid[10, -1]);
		}

		[Fact]
		public void SouthPoleOutOfBounds_ReturnsZeroAndIgnoresSet()
		{
			var grid = new ChartSightGrid();

			// Y >= 50 (South of Antarctica)
			Assert.Equal(0, grid[10, 50]);
			grid[10, 50] = 12;
			Assert.Equal(0, grid[10, 50]);

			grid[10, 50] |= 4;
			Assert.Equal(0, grid[10, 50]);
		}

		[Fact]
		public void GetLength_ReturnsCorrectDimensions()
		{
			var grid = new ChartSightGrid();

			Assert.Equal(80, grid.GetLength(0));
			Assert.Equal(50, grid.GetLength(1));
			Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetLength(2));
		}

		[Fact]
		public void Clear_ResetsAllCellsToZero()
		{
			var grid = new ChartSightGrid();
			grid[5, 5] = 10;
			grid[70, 40] = 20;

			grid.Clear();

			Assert.Equal(0, grid[5, 5]);
			Assert.Equal(0, grid[70, 40]);
		}
	}
}
