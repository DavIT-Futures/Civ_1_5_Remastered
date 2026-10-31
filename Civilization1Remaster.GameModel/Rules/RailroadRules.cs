namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Moving on railroads. A land unit pays nothing for a step along a road when the tile it leaves has a railroad.
	/// The original game (classic) let only the human player's units moved by hand do so (GoTo and the computer players
	/// paid 1 as on a road), and a city had no railroad (only a road), so a train lost a move in every city it passed.
	/// The remaster: a city has a railroad once the player knows Railroad, and every unit rides for free, but a unit not moved
	/// by hand (GoTo or a computer player) at most <see cref="MaxFreeStepsPerTurn"/> times a turn, so that no unit goes
	/// round in circles for ever; after that a step costs as on a road.
	/// </summary>
	public static class RailroadRules
	{
		/// <summary>The improvement bits of a tile</summary>
		public const int CityBit = 0x1;

		public const int RoadBit = 0x8;
		public const int RailroadBit = 0x10;

		/// <summary>The technology Railroad</summary>
		public const int RailroadTechnology = 0x31;

		/// <summary>The free steps on railroads a turn of a unit not moved by hand (GoTo or a computer player)</summary>
		public const int MaxFreeStepsPerTurn = 30;

		/// <summary>
		/// The tile has a railroad: built by settlers, or (the remaster) a city of a player who knows Railroad.
		/// </summary>
		public static bool HasRailroad(int improvements, bool playerKnowsRailroad, bool classic) =>
			(improvements & RailroadBit) != 0 ||
			(!classic && playerKnowsRailroad && (improvements & CityBit) != 0);

		/// <summary>
		/// A step along a road from a tile with a railroad is free: classic only for the human player's unit moved by hand;
		/// the remaster for every unit, for one not moved by hand only while it has made fewer than
		/// <see cref="MaxFreeStepsPerTurn"/> free steps this turn.
		/// </summary>
		public static bool IsFreeStep(bool movedByHand, int freeStepsThisTurn, bool classic) =>
			classic ? movedByHand : movedByHand || freeStepsThisTurn < MaxFreeStepsPerTurn;
	}
}
