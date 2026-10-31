namespace Civilization1Remaster.Game
{
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
}
