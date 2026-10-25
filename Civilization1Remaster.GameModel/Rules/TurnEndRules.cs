namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Small rules of the end of a turn and of the start of a game that the translated code got wrong; each one with the
	/// classic variant kept for the tests (a switch of TurnEvents, see the methods).
	/// </summary>
	public static class TurnEndRules
	{
		/// <summary>The random cities that may suffer a disaster each turn after turn 50</summary>
		public const int DisasterDraws = 2;

		/// <summary>
		/// The high byte of the population of the human player in the replay entry 11 (every 50 turns). classic: as the
		/// translated code wrote it, (byte &amp; 0xff00) &gt;&gt; 8 of a byte, so always 0 (TurnEvents.ClassicReplayPopulation).
		/// </summary>
		public static byte ReplayPopulationHigh(ushort population, bool classic) => classic ? (byte)0 : (byte)(population >> 8);

		/// <summary>
		/// The offset (into the pointers of the plural names, 0x1992) of the civilization a threatening emissary names: the
		/// enemy it wants to crush ("rid the world of the evil $THEM", like the tribute offer). classic: as the translated
		/// code read it, DI = player &lt;&lt; 4 left by the relations, a pointer past the table (TurnEvents.ClassicEmissaryThreat).
		/// </summary>
		public static ushort EmissaryEnemyName(int participantIdx, int enemyIdx, bool classic) =>
			classic ? (ushort)(participantIdx << 4) : (ushort)(enemyIdx << 1);

		/// <summary>
		/// The player whose ruined units are cleared after each civilization is made at the start of a game. classic: as the
		/// translated code did it, always player 6, the counter of an earlier loop (TurnEvents.ClassicFreshGameRuins).
		/// </summary>
		public static int RuinedUnitsCleared(int participantIdx, bool classic) => classic ? 6 : participantIdx;
	}
}
