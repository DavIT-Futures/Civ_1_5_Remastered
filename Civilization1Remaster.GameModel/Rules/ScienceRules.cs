using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for Civilization 1 science research costs, progress, and prerequisite checks.
	/// Translated from original assembly in CIV.EXE (CityManagement 0x1e3f:0x63e5 and ScienceAndAdvisors 0x1bfb).
	/// </summary>
	public static class ScienceRules
	{
		/// <summary>
		/// Computes the research cost (in beakers) required to discover the next technology.
		/// Original CIV.EXE 0x1e3f:0x63e5 (CityManagement).
		/// </summary>
		/// <param name="isHuman">True if player is human, false for AI bot.</param>
		/// <param name="difficultyTier">Difficulty level (0=Chieftain, 1=Warlord, 2=Prince, 3=King, 4=Emperor).</param>
		/// <param name="foundScienceTally">Number of technologies already discovered by this civilization.</param>
		/// <param name="yearBC">True if the current game year is BC (year &lt; 0), false if AD.</param>
		/// <returns>Beakers required to discover the current tech.</returns>
		public static int ComputeTechnologyCost(bool isHuman, int difficultyTier, int foundScienceTally, bool yearBC)
		{
			int baseFactor;

			if (isHuman)
			{
				// Human player formula:
				// baseFactor = 6 + (difficultyTier * 2)
				baseFactor = 6 + (difficultyTier * 2);

				// Early game assistance: if (11 - techCount) > baseFactor, baseFactor = 11 - techCount
				int earlyCap = 11 - foundScienceTally;
				if (earlyCap > baseFactor)
				{
					baseFactor = earlyCap;
				}
			}
			else
			{
				// AI player formula:
				// baseFactor = 14 - difficultyTier
				baseFactor = 14 - difficultyTier;
			}

			// In AD years (year >= 0), research requirement is doubled (divided by 2 on inquiry headway, or multiplied by 2 here)
			int yearMultiplier = yearBC ? 1 : 2;

			// Total required beakers = baseFactor * foundScienceTally * yearMultiplier
			int totalCost = baseFactor * foundScienceTally * yearMultiplier;
			return Math.Max(1, totalCost);
		}

		/// <summary>
		/// Checks whether a civilization satisfies prerequisites to research a given technology.
		/// </summary>
		/// <param name="prereq1">First prerequisite tech ID (-1 if none).</param>
		/// <param name="prereq2">Second prerequisite tech ID (-1 if none).</param>
		/// <param name="knowsTech">Predicate checking if player already knows a tech ID.</param>
		/// <returns>True if both prerequisites are met and the tech can be researched.</returns>
		public static bool CanResearch(int prereq1, int prereq2, Func<int, bool> knowsTech)
		{
			if (prereq1 >= 0 && !knowsTech(prereq1))
			{
				return false;
			}
			if (prereq2 >= 0 && !knowsTech(prereq2))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Evaluates whether current science progress is sufficient to complete the technology.
		/// </summary>
		public static bool IsResearchComplete(int inquiryHeadway, int requiredCost)
		{
			return inquiryHeadway >= requiredCost;
		}

		/// <summary>
		/// Determines whether discovering a technology makes existing barracks obsolete across cities.
		/// Gunpowder (0x22 = 34) and Combustion (0x25 = 37) obsolete existing barracks in Civ1.
		/// </summary>
		public static bool DoesTechObsoleteBarracks(int scienceIdx)
		{
			return scienceIdx == 0x22 || scienceIdx == 0x25;
		}

		/// <summary>
		/// Checks whether a civilization has researched or acquired a given technology.
		/// A negative technology (-1, "no technology needed") counts as known, as in the original.
		/// </summary>
		public static bool PlayerKnowsTechnology(ushort[] markers, short participantIdx, short scienceIdx)
		{
			if (scienceIdx < 0)
			{
				return true;
			}

			if (scienceIdx >= 71 || participantIdx == 0 || markers == null)
			{
				return false;
			}

			int wordIdx = scienceIdx >> 4;
			if (wordIdx >= markers.Length) return false;

			return (markers[wordIdx] & (1 << (scienceIdx & 0xf))) != 0;
		}

		/// <summary>
		/// Computes the AI leader technology preference value.
		/// </summary>
		public static ushort CalculateTechnologyInfo(sbyte b0, sbyte b1, short leaderFactor)
		{
			return (ushort)(leaderFactor * b1 + b0);
		}
	}
}

