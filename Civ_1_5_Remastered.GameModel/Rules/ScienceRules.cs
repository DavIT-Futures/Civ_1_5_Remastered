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
			if (wordIndex >= markers.Length) return false;

			return (markers[wordIndex] & (1 << (scienceIndex & 0xf))) != 0;
		}

		public static ushort CalculateTechnologyInformation(sbyte baseAmount, sbyte leaderWeight, short leaderFactor)
		{
			return (ushort)(leaderFactor * leaderWeight + baseAmount);
		}
	}
}

