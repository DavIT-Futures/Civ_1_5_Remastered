using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for Civilization 1 scoring calculation.
	/// Translated from original assembly in CIV.EXE segment 0x1f74 (CityFoundingAndScore.DisplayFinalScore).
	/// </summary>
	public static class ScoreRules
	{
		/// <summary>
		/// Computes citizen population score points.
		/// The original adds city size + happy - unhappy for every city, so each citizen contributes:
		/// - Happy citizen: 2 points
		/// - Content citizen: 1 point
		/// - Unhappy citizen: 0 points
		/// - Specialist (Scientist/Taxman/Entertainer): 1 point
		/// </summary>
		public static int ComputeCitizenScore(int happyCitizens, int contentCitizens, int specialists)
		{
			return (happyCitizens * 2) + contentCitizens + specialists;
		}

		/// <summary>
		/// Computes the spaceship arrival bonus score.
		/// Formula: (StarshipVictoryRatio * StarshipInhabitants) / 2
		/// </summary>
		public static int ComputeSpaceshipScore(int victoryRatio, int inhabitants)
		{
			int score = (victoryRatio * inhabitants) / 2;
			return Math.Max(0, score);
		}

		/// <summary>
		/// Computes the peace bonus points.
		/// Formula: 3 points per peaceful round without declaring war, clamped between 0 and 100.
		/// </summary>
		public static int ComputePeaceBonus(int truceRoundTally)
		{
			int bonus = truceRoundTally * 3;
			return Math.Clamp(bonus, 0, 100);
		}

		/// <summary>
		/// Computes future tech bonus points.
		/// Each discovered Future Tech gives exactly 5 points.
		/// </summary>
		public static int ComputeFutureTechScore(int futureTechTally)
		{
			return futureTechTally * 5;
		}

		/// <summary>
		/// Computes pollution penalty points.
		/// Each dirty polluted tile on the world map deducts 10 points.
		/// </summary>
		public static int ComputePollutionPenalty(int pollutedTilesCount)
		{
			return pollutedTilesCount * 10;
		}

		/// <summary>
		/// Computes early conquest bonus score if all rival civilizations were eliminated.
		/// Formula: 400 + (100 * rivalCount) + ((550 - roundTally) * 2).
		/// </summary>
		public static int ComputeConquestBonus(int rivalCount, int roundTally)
		{
			int bonus = 400 + (100 * rivalCount) + ((550 - roundTally) * 2);
			return bonus;
		}

	}
}
