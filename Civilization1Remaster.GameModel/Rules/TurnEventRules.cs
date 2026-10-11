using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for global turn events: global warming progression, palace expansion thresholds, etc.
	/// Translated from original assembly in CIV.EXE segment 0x2283 (TurnEvents).
	/// </summary>
	public static class TurnEventRules
	{
		/// <summary>
		/// Calculates the updated smog impact tier (0..99) and checks whether a warning or global warming occurs.
		/// Original CIV.EXE 0x2283:0x1885 (HandleEndOfTurnEvents).
		/// </summary>
		/// <param name="dirtyCellTally">Total polluted tiles across the world map.</param>
		/// <param name="worldHeatingTally">Times global warming has already triggered.</param>
		/// <param name="currentSmogTier">Current accumulated smog impact tier (0..99).</param>
		/// <returns>
		/// NextSmogTier,
		/// TriggerWarning (scientists alarmed newspaper),
		/// TriggerWarming (rising sea levels enactGlobalWarming)
		/// </returns>
		public static (int nextSmogTier, bool triggerWarning, bool triggerWarming) ComputeGlobalWarmingStep(
			int dirtyCellTally,
			int worldHeatingTally,
			int currentSmogTier)
		{
			// Civ1 formula:
			// delta = (dirtyCellTally * 2) - (worldHeatingTally * 4) - currentSmogTier
			// step = Sign(delta)
			// nextSmogTier = clamp(currentSmogTier + step, 0, 99)
			int delta = (dirtyCellTally * 2) - (worldHeatingTally * 4) - currentSmogTier;
			int step = Math.Sign(delta);
			int nextSmogTier = Math.Clamp(currentSmogTier + step, 0, 99);

			// Newspaper warning if nextSmogTier == 12 and dirtyCellTally > 6
			bool triggerWarning = (nextSmogTier == 12 && dirtyCellTally > 6);

			// Catastrophic warming if smog tier exceeds 16
			bool triggerWarming = (nextSmogTier > 16);

			if (triggerWarming)
			{
				// Smog tier resets to 0 after global warming occurs
				nextSmogTier = 0;
			}

			return (nextSmogTier, triggerWarning, triggerWarming);
		}

		/// <summary>
		/// Calculates the score required to reach a specific palace expansion tier.
		/// Original CIV.EXE 0x2283:0x1831 (ObtainPalaceScoreForLevel).
		/// Formula: sum_{i=0..castleTier} (i * (7 - difficultyTier)) / 2 + 1
		/// </summary>
		public static int ComputePalaceScoreForLevel(int castleTier, int difficultyTier)
		{
			int diffFactor = 7 - difficultyTier;
			int sum = 0;
			for (int i = 0; i <= castleTier; i++)
			{
				sum += (i * diffFactor);
			}

			return (sum / 2) + 1;
		}

		/// <summary>
		/// Calculates the palace tier earned from an accumulated approval rating / score.
		/// Original CIV.EXE 0x2283:0x2307 (ObtainPalaceLevel).
		/// </summary>
		public static int ComputePalaceLevel(int rating)
		{
			if (rating <= 0)
			{
				return 0;
			}

			int castleTier = 0;
			int remaining = rating;

			do
			{
				castleTier += 5;
				remaining -= castleTier;
			} while (remaining > 0);

			int level = (castleTier / 5) - 1;
			return Math.Max(0, level);
		}

		/// <summary>
		/// Calculates the new world year/season based on the current season and starship status.
		/// In original Civ1:
		/// Season &lt; 1000: +20
		/// Season &lt; 1500: +10
		/// Season &lt; 1750: +5
		/// Season &lt; 1850 and no enemy starship launched (starshipMarkers &amp; 0xfe == 0): +2
		/// Otherwise: +1
		/// Special adjustments:
		/// If season becomes 0 (transition from BC to AD), changes to 1 (1 AD).
		/// If season is 21 (overshot 20 AD), normalizes to 20.
		/// </summary>
		public static int ComputeNextSeason(int currentSeason, short starshipMarkers)
		{
			int nextSeason;

			if (currentSeason < 1000)
			{
				nextSeason = currentSeason + 20;
			}
			else if (currentSeason < 1500)
			{
				nextSeason = currentSeason + 10;
			}
			else if (currentSeason < 1750)
			{
				nextSeason = currentSeason + 5;
			}
			else if (currentSeason < 1850 && (starshipMarkers & 0xfe) == 0)
			{
				nextSeason = currentSeason + 2;
			}
			else
			{
				nextSeason = currentSeason + 1;
			}

			if (nextSeason == 0)
			{
				nextSeason = 1;
			}
			else if (nextSeason == 21)
			{
				nextSeason = 20;
			}

			return nextSeason;
		}

		/// <summary>
		/// Checks whether bankruptcy warning dialog should trigger.
		/// In original Civ1:
		/// Triggers if human has more than 1 city, current gold &lt; 100, and projected gold &lt; 0.
		/// Projected gold = currentGold + (gold - initialGold) * 10 &lt; 0.
		/// </summary>
		public static bool CheckBankruptcyWarning(int townTally, int currentGold, int turnStartGold)
		{
			if (townTally <= 1 || currentGold >= 100)
			{
				return false;
			}

			int delta = currentGold - turnStartGold;
			int projectedGold = currentGold + (delta * 10);
			return projectedGold < 0;
		}
	}
}

