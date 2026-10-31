namespace Civilization1Remaster.Core.Game
{
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
}
