namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class CombatRules
	{
		public static int ComputeGuardianScore(int landGroup,
			int baseGuardPower,
			int terrainType,
			int terrainDefenseFactor,
			bool isFortified,
			bool isVeteran)
		{
			if (terrainType == 10 && landGroup != 2)
			{
				return -1;
			}

			int score;
			if (landGroup == 0)
			{
				int fortificationFactor = isFortified ? 3 : 2;
				score = (fortificationFactor * baseGuardPower * terrainDefenseFactor) << 3;
			}
			else
			{
				score = baseGuardPower << 4;
			}

			if (isVeteran)
			{
				score += score / 2;
			}

			return score;
		}
	}
}
