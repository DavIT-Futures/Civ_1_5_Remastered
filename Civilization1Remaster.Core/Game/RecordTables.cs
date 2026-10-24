namespace Civilization1Remaster.Game
{
	/// <summary>
	/// The buildings and wonders (records of 0x1e bytes) in the data segment. Record n + 1 is the building n of the
	/// city structures (CityStructure), the wonders follow the buildings.
	/// </summary>
	public static class BuildingTable
	{
		public const ushort Origin = 0xb9a;
		public const int Stride = 0x1e;
		/// <summary>The price in tens of shields</summary>
		public const int Price = 0x18;
		/// <summary>The gold a turn</summary>
		public const int Upkeep = 0x1a;
		/// <summary>The technology that allows the building</summary>
		public const int Technology = 0x1c;
	}

	/// <summary>The terrain types (records of 0x13 bytes) in the data segment</summary>
	public static class TerrainTable
	{
		public const ushort Origin = 0x282;
		public const int Stride = 0x13;
		/// <summary>The moves a unit needs to enter the terrain (byte)</summary>
		public const int MoveCost = 0xc;
	}

	/// <summary>The leaders and their civilizations (records of 0x3a bytes) in the data segment</summary>
	public static class LeaderTable
	{
		public const ushort Origin = 0x14e2;
		public const int Stride = 0x3a;
		/// <summary>The text of the name of the leader</summary>
		public const int Name = 0;
		/// <summary>The text of the plural name of the civilization (empty: the name with "s")</summary>
		public const int CivilizationPlural = 0x10;
		/// <summary>The text of the name of the civilization</summary>
		public const int Civilization = 0x20;
		/// <summary>The leaning of the leader (ResearchLevyRatio is 3 more)</summary>
		public const int Leaning = 0x34;
		/// <summary>The short tune of the leader (PerformSound with 0)</summary>
		public const int ShortTune = 0x36;
		/// <summary>The tune of the leader (PerformSound with 3)</summary>
		public const int Tune = 0x38;
	}
}
