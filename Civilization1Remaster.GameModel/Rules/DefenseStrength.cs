namespace Civilization1Remaster.GameModel.Rules
{
	public static class DefenseStrength
	{
		public const int CannonAssault = 12;

		public static int Compute(int troopGuard, int landMultiplier, bool stronghold, bool dugin,
			bool waterTroop, bool skyTroop, bool townRamparts, int assailantAssault, bool seasoned)
		{
			int guard;

			if (stronghold)
			{
				guard = landMultiplier * troopGuard * 8;
			}
			else if (dugin)
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

			if (seasoned)
			{
				guard += guard / 2;
			}

			return guard;
		}
	}
}
