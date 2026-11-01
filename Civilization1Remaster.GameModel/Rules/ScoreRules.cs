using System;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class ScoreRules
	{
		public static int ComputeCitizenScore(int happyCitizens, int contentCitizens, int specialists)
		{
			return (happyCitizens * 2) + contentCitizens + specialists;
		}

		public static int ComputeSpaceshipScore(int victoryRatio, int inhabitants)
		{
			int score = (victoryRatio * inhabitants) / 2;
			return Math.Max(0, score);
		}

		public static int ComputePeaceBonus(int truceRoundTally)
		{
			int bonus = truceRoundTally * 3;
			return Math.Clamp(bonus, 0, 100);
		}

		public static int ComputeFutureTechnologyScore(int futureTechnologyTally)
		{
			return futureTechnologyTally * 5;
		}

		public static int ComputePollutionPenalty(int pollutedTilesCount)
		{
			return pollutedTilesCount * 10;
		}

		public static int ComputeConquestBonus(int rivalCount, int roundTally)
		{
			int bonus = 400 + (100 * rivalCount) + ((550 - roundTally) * 2);
			return bonus;
		}
	}
}
