using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for city happiness, civil disorder, and We Love The President Day.
	/// Translated from original assembly in CIV.EXE segment 0x1e3f (CityManagement).
	/// </summary>
	public static class CityHappinessRules
	{
		/// <summary>
		/// Evaluates whether a city falls into Civil Disorder.
		/// Original CIV.EXE 0x1e3f:0x5ce5 (CityManagement.HandleCity).
		/// Civil disorder occurs if Unhappy citizens strictly exceed Happy citizens (unhappy > happy).
		/// </summary>
		/// <param name="happyCitizens">Count of happy citizens in the city.</param>
		/// <param name="unhappyCitizens">Count of unhappy citizens in the city.</param>
		/// <returns>True if city goes into Civil Disorder.</returns>
		public static bool EvaluatesCivilDisorder(int happyCitizens, int unhappyCitizens)
		{
			// Formula: (happy - unhappy) < 0 => unhappy > happy
			return unhappyCitizens > happyCitizens;
		}

		/// <summary>
		/// Evaluates whether a city celebrates "We Love The President Day".
		/// Original CIV.EXE 0x1e3f:0x606c (CityManagement.HandleCity).
		/// Conditions:
		/// 1. Zero unhappy citizens (unhappy == 0).
		/// 2. City size >= 3 (TrueExtent > 2).
		/// 3. Happy citizens at least (citySize + 1) / 2.
		/// 4. Civilization is not under Anarchy (RegimeKind != 0).
		/// </summary>
		/// <param name="citySize">Population size of the city (TrueExtent).</param>
		/// <param name="happyCitizens">Count of happy citizens.</param>
		/// <param name="unhappyCitizens">Count of unhappy citizens.</param>
		/// <param name="isAnarchy">True if civilization is currently in Anarchy.</param>
		/// <returns>True if celebrating We Love The President Day.</returns>
		public static bool EvaluatesCelebration(int citySize, int happyCitizens, int unhappyCitizens, bool isAnarchy)
		{
			if (isAnarchy)
			{
				return false;
			}

			if (unhappyCitizens > 0)
			{
				return false;
			}

			if (citySize < 3)
			{
				return false;
			}

			int requiredHappy = (citySize + 1) / 2;
			return happyCitizens >= requiredHappy;
		}

		/// <summary>
		/// Calculates the distribution of citizens into Happy, Content, and Unhappy categories
		/// given the city size, specialists, luxuries, and temple/colosseum/cathedral pacification.
		/// </summary>
		/// <param name="citySize">Total city size.</param>
		/// <param name="specialistCount">Count of specialists (Entertainer, Taxman, Scientist).</param>
		/// <param name="baseUnhappy">Initial unhappy citizens due to empire size and difficulty tier.</param>
		/// <param name="pacifiedCitizens">Total citizens made content or happy by buildings, wonders, martial law, or luxuries.</param>
		/// <returns>Tuple of (happy, content, unhappy).</returns>
		public static (int happy, int content, int unhappy) CalculateCitizenMoods(
			int citySize,
			int specialistCount,
			int baseUnhappy,
			int pacifiedCitizens)
		{
			int workingCitizens = Math.Max(0, citySize - specialistCount);
			int initialUnhappy = Math.Min(workingCitizens, Math.Max(0, baseUnhappy));
			int initialContent = workingCitizens - initialUnhappy;

			int remainingPacification = pacifiedCitizens;

			// First pacify unhappy citizens to content
			int unhappyToContent = Math.Min(initialUnhappy, remainingPacification);
			int unhappy = initialUnhappy - unhappyToContent;
			int content = initialContent + unhappyToContent;
			remainingPacification -= unhappyToContent;

			// Extra pacification turns content citizens to happy
			int contentToHappy = Math.Min(content, remainingPacification);
			int happy = contentToHappy;
			content -= contentToHappy;

			return (happy, content, unhappy);
		}
	}
}
