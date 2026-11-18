namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class DefenseStrength
	{
		public const int CannonAssault = 12;

		/// <param name="townStockade">
		/// The city has a Stockade (remastered): twice the defence where City Walls give three times, on the same terms;
		/// with City Walls it adds nothing.
		/// </param>
		public static int Compute(int troopGuard, int landMultiplier, bool stronghold, bool dugIn,
			bool waterTroop, bool skyTroop, bool townRamparts, int assailantAssault, bool seasoned, bool townStockade = false)
		{
			int guard;

			if (stronghold)
			{
				guard = landMultiplier * troopGuard * 8;
			}
			else if (dugIn)
			{
				guard = landMultiplier * troopGuard * 12 / 2;
			}
			else
			{
				guard = landMultiplier * troopGuard * 4;
			}

			if (waterTroop || skyTroop)
			{
				guard = troopGuard * 8;
			}

			if (townRamparts && assailantAssault != CannonAssault && !skyTroop)
			{
				guard = landMultiplier * troopGuard * 12;
			}
			else if (townStockade && assailantAssault != CannonAssault && !skyTroop)
			{
				guard = landMultiplier * troopGuard * 8;
			}

			if (seasoned)
			{
				guard += guard / 2;
			}

			return guard;
		}
	}
}
