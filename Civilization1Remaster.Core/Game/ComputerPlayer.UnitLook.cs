namespace Civilization1Remaster.Core.Game
{
	partial class ComputerPlayer
	{
		/// <summary>
		/// The locals of ShiftAIUnit, shared by the methods it calls as in the original (where they were the words 0x2 - 0x5e
		/// of its frame; the comments give the offsets). B3 (2026-10-26): a C# object made for every call, every value 0 at
		/// the start like the zeroed frame of the remaster (SurveyStack reads 0xc before anything writes it).
		/// </summary>
		private sealed class UnitLook
		{
			/// <summary>0x2: the approach of the player on the continent of the unit</summary>
			public ushort Approach { get; set; }

			/// <summary>0x4: the nearest unit (of the player, or the bomber of the human player for a fighter)</summary>
			public ushort NearestUnit { get; set; }

			/// <summary>0x6: the city size per guard the city wants (3 or 4, one more or less by the approach)</summary>
			public ushort GuardShare { get; set; }

			/// <summary>0x8: the kind of the unit</summary>
			public ushort Kind { get; set; }

			/// <summary>0xa: how far the direction is from the heading of the unit (written only)</summary>
			public ushort Away { get; set; }

			/// <summary>0xc: the nearest coastal city of the player for a transport; SurveyStack reads it before anything writes it</summary>
			public ushort Harbour { get; set; }

			/// <summary>0xe: the column of the tile around the tile of the direction</summary>
			public ushort AroundHoriz { get; set; }

			/// <summary>0x10: the score of the direction; the bits of the roads in the directions</summary>
			public ushort Score { get; set; }

			/// <summary>0x12: the distance to the coastal city</summary>
			public ushort HarbourDistance { get; set; }

			/// <summary>0x14: the best distance so far; the distance of the nearest bomber</summary>
			public ushort BestDistance { get; set; }

			/// <summary>0x16: the row of the tile around the tile of the direction</summary>
			public ushort AroundVert { get; set; }

			/// <summary>0x1a: the continent of the unit</summary>
			public ushort Continent { get; set; }

			/// <summary>0x1c: a foreign unit at the tile of the direction (an enemy next to the unit)</summary>
			public ushort Foreign { get; set; }

			/// <summary>0x1e: the bits of the roles of the other units of the stack</summary>
			public ushort StackRoles { get; set; }

			/// <summary>0x20: the owner of a land tile around a transport</summary>
			public ushort TileOwner { get; set; }

			/// <summary>0x22: the city looked at by HasCityNear</summary>
			public ushort CityLoop { get; set; }

			/// <summary>0x24: the unit at the tile of the direction</summary>
			public ushort TileUnit { get; set; }

			/// <summary>0x26: the column of the tile of the direction</summary>
			public ushort TileHoriz { get; set; }

			/// <summary>0x28: the best score so far (-999 at the start)</summary>
			public ushort BestScore { get; set; }

			/// <summary>0x2a: the distance to the nearest city</summary>
			public ushort CityDistance { get; set; }

			/// <summary>0x2c: the best direction so far; a count</summary>
			public ushort BestDirection { get; set; }

			/// <summary>0x2e: the row of the tile of the direction</summary>
			public ushort TileVert { get; set; }

			/// <summary>0x30: the column of the unit</summary>
			public ushort Horiz { get; set; }

			/// <summary>0x32: the distance to the nearest unit</summary>
			public ushort UnitDistance { get; set; }

			/// <summary>0x34: the nearest city (the target of a nuclear weapon, the city to land in)</summary>
			public ushort NearestCity { get; set; }

			/// <summary>0x36: the row of the unit</summary>
			public ushort Vert { get; set; }

			/// <summary>0x38: the terrain of the tile of the direction</summary>
			public ushort TileTerrain { get; set; }

			/// <summary>0x3a: the direction from the round; a road two tiles away; the approach at the coastal city</summary>
			public ushort Round { get; set; }

			/// <summary>0x3c: the load of the transport is wanted on the continent</summary>
			public ushort LoadWanted { get; set; }

			/// <summary>0x3e: HasCityNear found a city</summary>
			public ushort CityNear { get; set; }

			/// <summary>0x40: the direction (the try)</summary>
			public ushort Direction { get; set; }

			/// <summary>0x42: the terrain of the unit</summary>
			public ushort Terrain { get; set; }

			/// <summary>0x44: the tile around the tile of the direction</summary>
			public ushort Around { get; set; }

			/// <summary>0x4a: the continent of the tile of the direction</summary>
			public ushort TileContinent { get; set; }

			/// <summary>0x4c: the unit may stay on its tile</summary>
			public ushort MayStay { get; set; }

			/// <summary>0x50: the bit of the player</summary>
			public ushort PlayerBit { get; set; }

			/// <summary>0x52: the best direction was next to an enemy</summary>
			public ushort BestNextToEnemy { get; set; }

			/// <summary>0x54: the land units of the stack; the roads around</summary>
			public ushort LandUnits { get; set; }

			/// <summary>0x56: the role of the unit</summary>
			public ushort Role { get; set; }

			/// <summary>0x58: the player of the unit at the tile of the direction</summary>
			public ushort TileUnitOwner { get; set; }

			/// <summary>0x5a: the weight of the battle with the unit at the tile</summary>
			public ushort BattleScore { get; set; }

			/// <summary>0x5c: no enemy stands next to the unit</summary>
			public ushort NoEnemyNear { get; set; }

			/// <summary>0x5e: the heading of the unit</summary>
			public ushort Heading { get; set; }

			/// <summary>The low byte of the heading alone (mov byte [bp - 0x5e]), the high byte stays</summary>
			public byte HeadingLow { set => this.Heading = (ushort)((this.Heading & 0xff00) | value); }
		}
	}
}
