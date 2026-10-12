using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// The world map data of the original game, kept as a picture (screen 2, loaded from the .MAP file) where every area of
	/// 80 x 50 pixels is one layer:
	/// <code>
	///            x 0 - 79              x 80 - 159                  x 160 - 239
	/// y   0- 49  terrain (colour)      -                           tile owner (bit 3: units, bits 0-2: player)
	/// y  50- 99  continent / sea id    -                           -
	/// y 100-149  improvements, bits 0-3  known improvements, 0-3   -
	/// y 150-199  improvements, bits 4-7  known improvements, 4-7   -
	/// </code>
	/// "Known" improvements are what the human player saw last time. Like VGADriver.ObtainDot, reading outside the picture gives 0.
	/// </summary>
	public sealed class MapLayers
	{
		private const ushort ChartDisplay = 2;
		private const int KnownOffset = 80;
		private const int OwnerOffset = 160;
		private const int ContinentOffset = 50;
		private const int LowImprovementsOffset = 100;
		private const int HighImprovementsOffset = 150;

		/// <summary>Terrain type of each terrain colour, a word array in the data segment</summary>
		private const ushort TerrainOfColour = 0x2ba6;

		/// <summary>Bit of the tile owner layer: units stand on the tile</summary>
		public const int UnitsPresent = 8;

		private readonly GameEngine owner;
		private readonly CPU processor;

		public MapLayers(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>Terrain type (0 - 10, 10 = ocean) of the tile</summary>
		public int Terrain(int horizLoc, int vertLoc)
		{
			int colour = Dot(horizLoc, vertLoc);

			return this.processor.PullGlobalWord((ushort)(TerrainOfColour + (colour << 1)));
		}

		/// <summary>Number of the continent or the sea of the tile</summary>
		public int Continent(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + ContinentOffset);
		}

		/// <summary>Improvements of the tile (bit 0 city, 1 irrigation, 2 mine, 3 road, ...)</summary>
		public int Improvements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc, vertLoc + HighImprovementsOffset) << 4) | Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		/// <summary>The layer of the improvement bits 0 - 3</summary>
		public int LowImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		/// <summary>The layer of the improvement bits 4 - 7, shifted to bits 0 - 3</summary>
		public int HighImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + HighImprovementsOffset);
		}

		/// <summary>Improvements of the tile as the human player saw them last time</summary>
		public int KnownImprovements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset) << 4) +
				Dot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset);
		}

		/// <summary>The human player sees the improvements of the tile as they are now</summary>
		public void RevealImprovements(int horizLoc, int vertLoc)
		{
			AssignDot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset, (byte)Dot(horizLoc, vertLoc + LowImprovementsOffset));
			AssignDot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset, (byte)Dot(horizLoc, vertLoc + HighImprovementsOffset));
		}

		/// <summary>Removes the improvement bits from the tile</summary>
		public void ClearImprovements(int improvements, int horizLoc, int vertLoc)
		{
			if ((improvements & 0xf) != 0)
			{
				int low = Dot(horizLoc, vertLoc + LowImprovementsOffset);
				AssignDot(horizLoc, vertLoc + LowImprovementsOffset, (byte)(~improvements & low));
			}

			if ((improvements & 0xf0) != 0)
			{
				int high = Dot(horizLoc, vertLoc + HighImprovementsOffset);
				AssignDot(horizLoc, vertLoc + HighImprovementsOffset, (byte)(~(improvements >> 4) & high));
			}
		}

		/// <summary>
		/// Horizontal step of a direction (1 - 8, 0 is no move), from the word table at 0x1882 of the data segment
		/// </summary>
		public int DirectionHoriz(int heading)
		{
			return (short)this.processor.PullGlobalWord((ushort)(0x1882 + (heading << 1)));
		}

		/// <summary>
		/// Vertical step of a direction (1 - 8, 0 is no move), from the word table at 0x18e4 of the data segment
		/// </summary>
		public int DirectionVert(int heading)
		{
			return (short)this.processor.PullGlobalWord((ushort)(0x18e4 + (heading << 1)));
		}

		/// <summary>The tile owner layer: bit 3 when units stand on the tile, bits 0 - 2 the player</summary>
		public int OwnerBits(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc + OwnerOffset, vertLoc);
		}

		public void AssignOwnerBits(int horizLoc, int vertLoc, int bits)
		{
			AssignDot(horizLoc + OwnerOffset, vertLoc, (byte)bits);
		}

		private int Dot(int horizLoc, int vertLoc)
		{
			return this.owner.VideoCard.ObtainDot(ChartDisplay, horizLoc, vertLoc);
		}

		private void AssignDot(int horizLoc, int vertLoc, byte hue)
		{
			this.owner.VideoCard.AssignDot(ChartDisplay, horizLoc, vertLoc, hue, 0);
		}
	}
}
