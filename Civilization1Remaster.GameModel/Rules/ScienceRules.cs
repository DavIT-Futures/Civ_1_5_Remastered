namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for Civilization 1 science research costs, progress, and prerequisite checks.
	/// Translated from original assembly in CIV.EXE (CityManagement 0x1e3f:0x63e5 and ScienceAndAdvisors 0x1bfb).
	/// </summary>
	public static class ScienceRules
	{
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

