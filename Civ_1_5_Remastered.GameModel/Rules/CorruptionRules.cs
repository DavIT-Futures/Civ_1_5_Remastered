namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Corruption of city trade. The original formula grows with the distance to the palace; the remastered
	/// Courthouse removes three quarters of it (the original only halved it), so distant cities are worth governing.
	/// </summary>
	public static class CorruptionRules
	{
		public const int CommunismRegime = 3;
		public const int DemocracyRegime = 5;
		public const int CommunismDistance = 10;

		/// <summary>Corruption of a city producing <paramref name="trade"/> at <paramref name="palaceDistance"/> from the palace.</summary>
		public static int Corruption(int trade, int palaceDistance, int regime, bool courthouse, bool palace)
		{
			if (regime == DemocracyRegime)
			{
				return 0;
			}

			if (regime == CommunismRegime)
			{
				palaceDistance = CommunismDistance;
			}

			short raw = (short)(short)((short)(trade * palaceDistance) * 3);
			int corruption = raw / (short)((20 * regime) + 0x50);

			if (palace)
			{
				return corruption / 2;
			}

			return courthouse ? corruption / 4 : corruption;
		}
	}
}
