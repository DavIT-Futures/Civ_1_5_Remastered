namespace Civilization1Remaster.GameModel.Rules
{
	public static class RailroadRules
	{
		public const int CityBit = 0x1;
		public const int RailroadBit = 0x10;

		public const int RailroadTechnology = 0x31;

		public const int MaximumFreeStepsPerTurn = 30;

		public static bool HasRailroad(int improvements, bool playerKnowsRailroad) =>
			(improvements & RailroadBit) != 0 || (playerKnowsRailroad && (improvements & CityBit) != 0);

		public static bool IsFreeStep(bool movedByHand, int freeStepsThisTurn) =>
			movedByHand || freeStepsThisTurn < MaximumFreeStepsPerTurn;
	}
}
