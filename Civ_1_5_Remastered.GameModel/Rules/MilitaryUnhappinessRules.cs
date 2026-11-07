namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Which units away from their city make citizens of a Republic or Democracy unhappy.</summary>
	public static class MilitaryUnhappinessRules
	{
		/// <summary>
		/// Units with an attack strength cause unhappiness when they are away. The remastered rule spares the
		/// civilian ships (Trireme, Sail, Transport); the Frigate and later warships still count.
		/// </summary>
		public static bool CausesUnhappinessAway(int kind, int assault) => assault != 0 && !ShipRules.IsCivilianShip(kind);
	}
}
