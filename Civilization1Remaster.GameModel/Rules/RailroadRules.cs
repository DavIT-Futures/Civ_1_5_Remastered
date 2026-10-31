namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Moving on railroads. A land unit pays nothing for a step along a road when the tile it leaves has a railroad.
	/// A city has a railroad once the player knows Railroad, and every unit rides for free, but a unit not moved by hand
	/// (GoTo or a computer player) at most <see cref="MaxFreeStepsPerTurn"/> times a turn, so that no unit goes round in
	/// circles for ever; after that a step costs as on a road. (The original let only the human player's units moved by
	/// hand ride, and a city had only a road, so a train lost a move in every city it passed.)
	/// </summary>
	public static class RailroadRules
	{
		/// <summary>The improvement bits of a tile</summary>
		public const int CityBit = 0x1;
		public const int RailroadBit = 0x10;

		/// <summary>The technology Railroad</summary>
		public const int RailroadTechnology = 0x31;

		/// <summary>The free steps on railroads a turn of a unit not moved by hand (GoTo or a computer player)</summary>
		public const int MaxFreeStepsPerTurn = 30;

		/// <summary>The tile has a railroad: built by settlers, or a city of a player who knows Railroad</summary>
		public static bool HasRailroad(int improvements, bool playerKnowsRailroad) =>
			(improvements & RailroadBit) != 0 || (playerKnowsRailroad && (improvements & CityBit) != 0);

		/// <summary>
		/// A step along a road from a tile with a railroad is free: for a unit moved by hand always, for the others while
		/// they have made fewer than <see cref="MaxFreeStepsPerTurn"/> free steps this turn
		/// </summary>
		public static bool IsFreeStep(bool movedByHand, int freeStepsThisTurn) =>
			movedByHand || freeStepsThisTurn < MaxFreeStepsPerTurn;
	}
}
