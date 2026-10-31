namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// The city of a wonder (GameState.MarvelTownIdx): the index of the city it stands in, -1 while nobody has built it and
	/// 128 after its city was destroyed (FinalizeProduction).
	/// </summary>
	public static class WonderCityRules
	{
		/// <summary>The city of a wonder nobody has built</summary>
		public const short NotBuilt = -1;

		/// <summary>The city of a wonder whose city was destroyed</summary>
		public const short Destroyed = 128;

		/// <summary>
		/// Whether the city of the wonder may be read from the city table: not for a wonder nobody has built or whose city
		/// was destroyed, which then belongs to nobody. classic: as the translated code did it, which read the city for
		/// these too; DOS read the memory before or after the table (an owner that almost never matched), C# throws
		/// IndexOutOfRangeException. Kept for the tests (TurnEvents.ClassicWonderCities).
		/// </summary>
		public static bool ReadsCity(int townIdx, bool classic) =>
			classic || (townIdx >= 0 && townIdx < Destroyed);
	}
}
