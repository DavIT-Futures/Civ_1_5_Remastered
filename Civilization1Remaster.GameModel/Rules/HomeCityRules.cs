namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// The home city of a unit (byte 0xb of the unit): the index of a city, or 0xff for a unit without a home city.
	/// </summary>
	public static class HomeCityRules
	{
		/// <summary>The home city of a unit without one</summary>
		public const int NoHome = 0xff;

		/// <summary>
		/// Whether the owner of the home city may be read from the city table: not for a unit without a home city, which
		/// then counts as homed with its own player (DivideCivilization: it stays with the old civilization)
		/// </summary>
		public static bool HasHome(int homeIdx) => homeIdx != NoHome;
	}
}
