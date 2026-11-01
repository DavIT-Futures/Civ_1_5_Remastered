namespace Civilization1Remaster.GameModel.Rules
{
	public static class ScienceRules
	{
		public static bool DoesTechObsoleteBarracks(int scienceIdx)
		{
			return scienceIdx == 0x22 || scienceIdx == 0x25;
		}

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

		public static ushort CalculateTechnologyInfo(sbyte b0, sbyte b1, short leaderFactor)
		{
			return (ushort)(leaderFactor * b1 + b0);
		}
	}
}

