using System;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// Size of the map window of the main screen. The original shows 15 x 12 tiles on a 320 x 200 screen; a larger
	/// viewport enlarges the main screen (the tiles keep their size), so more of the world is visible at once.
	/// </summary>
	/// <remarks>
	/// The left panel (80 pixels) and the menu bar (8 pixels) keep their place. The host asks for a size with
	/// <see cref="Request"/>; the game switches to it the next time it repaints the main screen.
	/// </remarks>
	public sealed class MapViewport
	{
		public const int OriginalColumns = 15;
		public const int OriginalRows = 12;
		public const int Left = 80;
		public const int Top = 8;
		public const int TileSize = 16;
		public const int WorldRows = 50;
		public const int MaximumColumns = 60;
		public const int MaximumRows = 46;

		private volatile int requestedColumns = OriginalColumns;
		private volatile int requestedRows = OriginalRows;

		public int Columns { get; private set; } = OriginalColumns;

		public int Rows { get; private set; } = OriginalRows;

		public int ScreenWidth => Left + (Columns * TileSize);

		public int ScreenHeight => Top + (Rows * TileSize);

		/// <summary>Bottom edge of the map window in pixels (200 in the original).</summary>
		public int Bottom => ScreenHeight;

		/// <summary>Column of the tile in the middle of the window (7 in the original).</summary>
		public int MiddleColumn => Columns / 2;

		/// <summary>Row of the tile in the middle of the window (6 in the original).</summary>
		public int MiddleRow => Rows / 2;

		/// <summary>Highest first row of the window (38 in the original).</summary>
		public int LastTopRow => WorldRows - Rows;

		public bool Enlarged => Columns != OriginalColumns || Rows != OriginalRows;

		public bool Pending => this.requestedColumns != Columns || this.requestedRows != Rows;

		public static int ScreenWidthFor(int columns) => Left + (columns * TileSize);

		public static int ScreenHeightFor(int rows) => Top + (rows * TileSize);

		public void Request(int columns, int rows)
		{
			this.requestedColumns = Math.Clamp(columns, OriginalColumns, MaximumColumns);
			this.requestedRows = Math.Clamp(rows, OriginalRows, MaximumRows);
		}

		/// <summary>Takes the requested size; returns whether it changed.</summary>
		public bool Apply()
		{
			if (!Pending)
			{
				return false;
			}

			Columns = this.requestedColumns;
			Rows = this.requestedRows;

			return true;
		}

		public bool Contains(int column, int row) => column >= 0 && row >= 0 && column < Columns && row < Rows;
	}
}
