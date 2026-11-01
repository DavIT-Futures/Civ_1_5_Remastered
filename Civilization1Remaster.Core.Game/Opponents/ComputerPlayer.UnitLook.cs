namespace Civilization1Remaster.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private sealed class UnitLook
		{
			public ushort Approach { get; set; }

			public ushort NearestUnit { get; set; }

			public ushort GuardShare { get; set; }

			public ushort Kind { get; set; }

			public ushort Away { get; set; }

			public ushort Harbour { get; set; }

			public ushort AroundHorizontal { get; set; }

			public ushort Score { get; set; }

			public ushort HarbourDistance { get; set; }

			public ushort BestDistance { get; set; }

			public ushort AroundVertical { get; set; }

			public ushort Continent { get; set; }

			public ushort Foreign { get; set; }

			public ushort StackRoles { get; set; }

			public ushort TileOwner { get; set; }

			public ushort CityLoop { get; set; }

			public ushort TileUnit { get; set; }

			public ushort TileHorizontal { get; set; }

			public ushort BestScore { get; set; }

			public ushort CityDistance { get; set; }

			public ushort BestDirection { get; set; }

			public ushort TileVertical { get; set; }

			public ushort Horizontal { get; set; }

			public ushort UnitDistance { get; set; }

			public ushort NearestCity { get; set; }

			public ushort Vertical { get; set; }

			public ushort TileTerrain { get; set; }

			public ushort Round { get; set; }

			public ushort LoadWanted { get; set; }

			public ushort CityNear { get; set; }

			public ushort Direction { get; set; }

			public ushort Terrain { get; set; }

			public ushort Around { get; set; }

			public ushort TileContinent { get; set; }

			public ushort MayStay { get; set; }

			public ushort PlayerBit { get; set; }

			public ushort BestNextToEnemy { get; set; }

			public ushort LandUnits { get; set; }

			public ushort Role { get; set; }

			public ushort TileUnitOwner { get; set; }

			public ushort BattleScore { get; set; }

			public ushort NoEnemyNear { get; set; }

			public ushort Heading { get; set; }

			public byte HeadingLow { set => this.Heading = (ushort)((this.Heading & 0xff00) | value); }
		}
	}
}
