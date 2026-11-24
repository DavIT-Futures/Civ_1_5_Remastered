using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class ScienceRules
	{
		public static bool DoesTechnologyObsoleteBarracks(int scienceIndex)
		{
			return scienceIndex == 0x22 || scienceIndex == 0x25;
		}

		public static bool PlayerKnowsTechnology(ushort[] markers, short participantIndex, short scienceIndex)
		{
			if (scienceIndex < 0)
			{
				return true;
			}

			if (scienceIndex >= 71 || participantIndex == 0 || markers == null)
			{
				return false;
			}

			int wordIndex = scienceIndex >> 4;
			if (wordIndex >= markers.Length)
			{
				return false;
			}

			return (markers[wordIndex] & (1 << (scienceIndex & 0xf))) != 0;
		}

		/// <summary>
		/// A technology may be researched when it is not known yet and both prerequisites are known (a negative
		/// prerequisite means none).
		/// </summary>
		public static bool MayResearch(ushort[] markers, short participantIndex, short scienceIndex, sbyte firstPrerequisite, sbyte secondPrerequisite)
		{
			return !PlayerKnowsTechnology(markers, participantIndex, scienceIndex) &&
				PlayerKnowsTechnology(markers, participantIndex, firstPrerequisite) &&
				PlayerKnowsTechnology(markers, participantIndex, secondPrerequisite);
		}

		/// <summary>
		/// Every technology (Future Technology excluded) the player may research, in technology order. Nothing is left
		/// out: the original game hid about a third of the choices above Chieftain.
		/// </summary>
		public static List<short> ResearchChoices(ushort[] markers, short participantIndex, Func<short, (sbyte First, sbyte Second)> prerequisites)
		{
			List<short> choices = new List<short>();

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				(sbyte first, sbyte second) = prerequisites(scienceIndex);

				if (MayResearch(markers, participantIndex, scienceIndex, first, second))
				{
					choices.Add(scienceIndex);
				}
			}

			return choices;
		}

		/// <summary>
		/// The top of the "Which discovery" menu: 64 as in the original game, moved up when the full list of choices
		/// (three heading lines plus one line per technology, 8 pixels each) would not fit on the 200 pixel screen.
		/// </summary>
		public static int ResearchMenuTop(int choices)
		{
			const int Original = 64;
			int height = ((choices + 3) * 8) + 16;
			int top = Math.Min(Original, 200 - height);

			return Math.Max(0, top & ~1);
		}

		public static ushort CalculateTechnologyInformation(sbyte baseAmount, sbyte leaderWeight, short leaderFactor)
		{
			return (ushort)((leaderFactor * leaderWeight) + baseAmount);
		}
	}
}
