namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>Small rules of the end of a turn and of the start of a game that the translated code got wrong</summary>
	public static class TurnEndRules
	{
		/// <summary>The random cities that may suffer a disaster each turn after turn 50</summary>
		public const int DisasterDraws = 2;

		/// <summary>The high byte of the population of the human player in the replay entry 11 (every 50 turns)</summary>
		public static byte ReplayPopulationHigh(ushort population) => (byte)(population >> 8);

		/// <summary>
		/// The offset (into the pointers of the plural names, 0x1992) of the civilization a threatening emissary names: the
		/// enemy it wants to crush ("rid the world of the evil $THEM", like the tribute offer)
		/// </summary>
		public static ushort EmissaryEnemyName(int enemyIdx) => (ushort)(enemyIdx << 1);
	}
}
