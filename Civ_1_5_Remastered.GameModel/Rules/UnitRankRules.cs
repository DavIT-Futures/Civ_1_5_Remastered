namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Names of unit ranks shown in the unit panel and unit lists.</summary>
	public static class UnitRankRules
	{
		public static string Title(bool seasoned, bool elite)
		{
			if (elite)
			{
				return "Elite";
			}

			return seasoned ? "Veteran" : string.Empty;
		}

		public static string Suffix(bool seasoned, bool elite)
		{
			if (elite)
			{
				return " (E)";
			}

			return seasoned ? " (V)" : string.Empty;
		}
	}
}
