namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// The strength of an own stack a computer unit weighs when it scores a tile (AIPlayer.ScoreTile): a defender
	/// (role 1) adds 4 x the defence of the stack / (its units + 1), an attacker (role 2) 2 x its attack / (its defence + 1).
	/// </summary>
	public static class StackStrengthRules
	{
		/// <summary>
		/// The divisor of the share: the tally counted first plus 1. classic: as the translated code did it, which kept the
		/// divisor in SI and lost it in the second tally (TallyUnitsByDomainInStack leaves SI = 0x600 * player, CIV.EXE
		/// kept SI), so the share was almost always 0. Kept for the tests (TurnEvents.ClassicStackStrength).
		/// </summary>
		public static ushort Divisor(ushort tallyPlusOne, ushort leftSource, bool classic) =>
			classic ? leftSource : tallyPlusOne;
	}
}
