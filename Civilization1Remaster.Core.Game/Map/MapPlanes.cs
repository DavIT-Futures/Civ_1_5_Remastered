namespace Civilization1Remaster.Core.Game.Map
{
	public sealed class MapPlanes : GameComponent, IMapPlanes
	{
		public const int UnitsPresent = 8;

		private const ushort ChartDisplay = 2;
		private const int KnownOffset = 80;
		private const int OwnerOffset = 160;
		private const int ContinentOffset = 50;
		private const int LowImprovementsOffset = 100;
		private const int HighImprovementsOffset = 150;

		public MapPlanes(IGameEngine owner)
			: base(owner)
		{
		}

		public int Terrain(int horizontalLocation, int verticalLocation)
		{
			int colour = Dot(horizontalLocation, verticalLocation);

			return this.Processor.PullGlobalWord((ushort)(IMapPlanes.TerrainOfColour + (colour << 1)));
		}

		public int Continent(int horizontalLocation, int verticalLocation)
		{
			return Dot(horizontalLocation, verticalLocation + ContinentOffset);
		}

		public int Improvements(int horizontalLocation, int verticalLocation)
		{
			return (Dot(horizontalLocation, verticalLocation + HighImprovementsOffset) << 4) | Dot(horizontalLocation, verticalLocation + LowImprovementsOffset);
		}

		public int LowImprovements(int horizontalLocation, int verticalLocation)
		{
			return Dot(horizontalLocation, verticalLocation + LowImprovementsOffset);
		}

		public int HighImprovements(int horizontalLocation, int verticalLocation)
		{
			return Dot(horizontalLocation, verticalLocation + HighImprovementsOffset);
		}

		public int KnownImprovements(int horizontalLocation, int verticalLocation)
		{
			return (Dot(horizontalLocation + KnownOffset, verticalLocation + HighImprovementsOffset) << 4) +
				Dot(horizontalLocation + KnownOffset, verticalLocation + LowImprovementsOffset);
		}

		public void RevealImprovements(int horizontalLocation, int verticalLocation)
		{
			AssignDot(horizontalLocation + KnownOffset, verticalLocation + LowImprovementsOffset, (byte)Dot(horizontalLocation, verticalLocation + LowImprovementsOffset));
			AssignDot(horizontalLocation + KnownOffset, verticalLocation + HighImprovementsOffset, (byte)Dot(horizontalLocation, verticalLocation + HighImprovementsOffset));
		}

		public void ClearImprovements(int improvements, int horizontalLocation, int verticalLocation)
		{
			if ((improvements & 0xf) != 0)
			{
				int low = Dot(horizontalLocation, verticalLocation + LowImprovementsOffset);
				AssignDot(horizontalLocation, verticalLocation + LowImprovementsOffset, (byte)(~improvements & low));
			}

			if ((improvements & 0xf0) != 0)
			{
				int high = Dot(horizontalLocation, verticalLocation + HighImprovementsOffset);
				AssignDot(horizontalLocation, verticalLocation + HighImprovementsOffset, (byte)(~(improvements >> 4) & high));
			}
		}

		public int DirectionHorizontal(int heading) => (short)Tables.DirectionHorizontal(heading);

		public int DirectionVertical(int heading) => (short)Tables.DirectionVertical(heading);

		public int OwnerBits(int horizontalLocation, int verticalLocation)
		{
			return Dot(horizontalLocation + OwnerOffset, verticalLocation);
		}

		public void AssignOwnerBits(int horizontalLocation, int verticalLocation, int bits)
		{
			AssignDot(horizontalLocation + OwnerOffset, verticalLocation, (byte)bits);
		}

		private int Dot(int horizontalLocation, int verticalLocation)
		{
			return this.Owner.VideoCard.ObtainDot(ChartDisplay, horizontalLocation, verticalLocation);
		}

		private void AssignDot(int horizontalLocation, int verticalLocation, byte hue)
		{
			this.Owner.VideoCard.AssignDot(ChartDisplay, horizontalLocation, verticalLocation, hue);
		}
	}
}
