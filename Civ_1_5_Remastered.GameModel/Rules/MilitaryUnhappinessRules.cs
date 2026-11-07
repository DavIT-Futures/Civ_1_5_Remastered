namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Which units away from their city make citizens of a Republic or Democracy unhappy.</summary>
	public static class MilitaryUnhappinessRules
	{
		public const int Trireme = 0x10;

		/// <summary>
		/// Units with an attack strength cause unhappiness when they are away. The remastered rule spares the
		/// Trireme, a coastal ferry like the Transport, which never caused unhappiness.
		/// </summary>
		public static bool CausesUnhappinessAway(int kind, int assault) => assault != 0 && kind != Trireme;
	}
}
