namespace Civilization1Remaster.Core.Game
{
	public sealed class MapPlanes : GameComponent, IMapPlanes
	{
		public const ushort TerrainOfColour = 0x2ba6;

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

		public int Terrain(int horizLoc, int vertLoc)
		{
			int colour = Dot(horizLoc, vertLoc);

			return this.Processor.PullGlobalWord((ushort)(TerrainOfColour + (colour << 1)));
		}

		public int Continent(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + ContinentOffset);
		}

		public int Improvements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc, vertLoc + HighImprovementsOffset) << 4) | Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		public int LowImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + LowImprovementsOffset);
		}

		public int HighImprovements(int horizLoc, int vertLoc)
		{
			return Dot(horizLoc, vertLoc + HighImprovementsOffset);
		}

		public int KnownImprovements(int horizLoc, int vertLoc)
		{
			return (Dot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset) << 4) +
				Dot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset);
		}

		public void RevealImprovements(int horizLoc, int vertLoc)
		{
			AssignDot(horizLoc + KnownOffset, vertLoc + LowImprovementsOffset, (byte)Dot(horizLoc, vertLoc + LowImprovementsOffset));
			AssignDot(horizLoc + KnownOffset, vertLoc + HighImprovementsOffset, (byte)Dot(horizLoc, vertLoc + HighImprovementsOffset));
		}

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

		public int DirectionHoriz(int heading) => (short)Tables.DirectionHoriz(heading);

		public int DirectionVert(int heading) => (short)Tables.DirectionVert(heading);

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
			this.Owner.VideoCard.AssignDot(ChartDisplay, horizLoc, vertLoc, hue);
		}
	}
}
