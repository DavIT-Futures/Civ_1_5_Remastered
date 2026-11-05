namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Encapsulates game rules and bitmask evaluations for railroads and movement costs.
	/// </summary>
	public static class RailroadRules
	{
		public const int CityBit = 0x1;
		public const int RailroadBit = 0x10;

		public const int RailroadTechnology = 0x31;

		public const int MaximumFreeStepsPerTurn = 30;

		/// <summary>
		/// Determines whether a tile possesses a functional railroad.
		/// In Civ 1.5 Remastered, cities automatically act as railroad junctions once the player discovers Railroad technology.
		/// </summary>
		/// <param name="improvements">Tile improvements bitmask.</param>
		/// <param name="playerKnowsRailroad">True if the owning player has discovered the Railroad technology.</param>
		/// <returns>True if railroad movement rules apply to the tile.</returns>
		public static bool HasRailroad(int improvements, bool playerKnowsRailroad) =>
			(improvements & RailroadBit) != 0 || (playerKnowsRailroad && (improvements & CityBit) != 0);

		/// <summary>
		/// Evaluates whether the current movement step along a railroad is free of turn movement points.
		/// </summary>
		/// <param name="movedByHand">True if the unit was directed manually by player input.</param>
		/// <param name="freeStepsThisTurn">Current tally of free railroad steps consumed this turn.</param>
		/// <returns>True if step does not cost turn movement points.</returns>
		public static bool IsFreeStep(bool movedByHand, int freeStepsThisTurn) =>
			movedByHand || freeStepsThisTurn < MaximumFreeStepsPerTurn;
	}
}
