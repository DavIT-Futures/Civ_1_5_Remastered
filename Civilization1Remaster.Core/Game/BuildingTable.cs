namespace Civilization1Remaster.Core.Game
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
}
