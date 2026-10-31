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
		/// <summary>Defense bonus factor for terrain (byte)</summary>
		public const int DefenseBonus = 0xd;
		/// <summary>Base food yield (byte)</summary>
		public const int Food = 0xe;
		/// <summary>Base shield/production yield (byte)</summary>
		public const int Shields = 0xf;
		/// <summary>Base trade yield (byte)</summary>
		public const int Trade = 0x10;
	}

	/// <summary>The terrain improvement effects/results (records of 12 bytes = 6 words) in the data segment</summary>
	public static class TerrainImprovementTable
	{
		public const ushort Origin = 0x44a;
		public const int Stride = 0xc;
		/// <summary>Irrigation: -1 not possible, -2 the bonus of one food, else the terrain it turns into</summary>
		public const int Irrigation = 0x0;
		/// <summary>A mine: -1 not possible, a negative number the bonus of (-1 - it) shields, else the terrain it turns into</summary>
		public const int Mining = 0x4;
	}

	/// <summary>The technology prerequisite requirements table</summary>
	public static class TechnologyTable
	{
		public const ushort Prerequisite1Address = 0x4ee;
		public const ushort Prerequisite2Address = 0x4ef;
		public const int Stride = 0x16;
	}

	/// <summary>The technology that makes each wonder obsolete (word table at 0x10fe)</summary>
	public static class WonderObsoleteTable
	{
		public const ushort Origin = 0x10fe;
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
		/// <summary>1 aggressive, -1 friendly</summary>
		public const int Aggression = 0x30;
		/// <summary>1 expansionistic, -1 perfectionist</summary>
		public const int Expansion = 0x32;
		/// <summary>The leaning of the leader, 1 civilized, -1 militaristic (ResearchLevyRatio is 3 more)</summary>
		public const int Leaning = 0x34;
		/// <summary>The short tune of the leader (PerformSound with 0)</summary>
		public const int ShortTune = 0x36;
		/// <summary>The tune of the leader (PerformSound with 3)</summary>
		public const int Tune = 0x38;
	}
}
