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
	public sealed class MapPlanes : GameComponent, IMapPlanes
	{
		/// <summary>Terrain type of each terrain colour, a word array in the data segment</summary>
		public const ushort TerrainOfColour = 0x2ba6;

		/// <summary>Bit of the tile owner layer: units stand on the tile</summary>
		public const int UnitsPresent = 8;

		private const ushort ChartDisplay = 2;
		private const int KnownOffset = 80;
		private const int OwnerOffset = 160;
		private const int ContinentOffset = 50;
		private const int LowImprovementsOffset = 100;
		private const int HighImprovementsOffset = 150;

		public MapPlanes(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public int Terrain(int horizLoc, int vertLoc)
		{
			int colour = Dot(horizLoc, vertLoc);

			return this.Processor.PullGlobalWord((ushort)(TerrainOfColour + (colour << 1)));
		}

		/// <inheritdoc/>
		public int Continent(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + ContinentOffset);
		}

		/// <inheritdoc/>
		public int Improvements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc, vertLoc + HighImprovementsOffset) << 4) | Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		/// <inheritdoc/>
		public int LowImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		/// <inheritdoc/>
		public int HighImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + HighImprovementsOffset);
		}

		/// <inheritdoc/>
		public int KnownImprovements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset) << 4) +
				Dot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset);
		}

		/// <inheritdoc/>
		public void RevealImprovements(int horizLoc, int vertLoc)
		{
			AssignDot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset, (byte)Dot(horizLoc, vertLoc + LowImprovementsOffset));
			AssignDot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset, (byte)Dot(horizLoc, vertLoc + HighImprovementsOffset));
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public new int DirectionHoriz(int heading) => (short)base.DirectionHoriz(heading);

		/// <inheritdoc/>
		public new int DirectionVert(int heading) => (short)base.DirectionVert(heading);

		/// <inheritdoc/>
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
			return this.Owner.VideoCard.ObtainDot(ChartDisplay, horizLoc, vertLoc);
		}

		private void AssignDot(int horizLoc, int vertLoc, byte hue)
		{
			this.Owner.VideoCard.AssignDot(ChartDisplay, horizLoc, vertLoc, hue, 0);
		}
	}
}
