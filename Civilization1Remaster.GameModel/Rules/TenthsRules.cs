namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// A number in tenths written with one decimal, "4.5" for 45: the census (children of a family), the mass of a spaceship
	/// ("4,500 tons" from 45 hundreds) and its flight time ("4.5 Years").
	/// </summary>
	public static class TenthsRules
	{
		/// <summary>The part before the decimal point</summary>
		public static short Whole(short tenths) => (short)(tenths / 10);

		/// <summary>
		/// The digit after the decimal point. classic: as the translated code wrote it, the whole part again ("4.4" for 45,
		/// value / 10 where the original had value % 10); kept for the tests (TurnEvents.ClassicTenths).
		/// </summary>
		public static short Tenth(short tenths, bool classic) => (short)(classic ? tenths / 10 : tenths % 10);
	}
}
