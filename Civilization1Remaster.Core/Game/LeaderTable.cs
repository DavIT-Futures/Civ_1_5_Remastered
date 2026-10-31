namespace Civilization1Remaster.Core.Game
{
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
