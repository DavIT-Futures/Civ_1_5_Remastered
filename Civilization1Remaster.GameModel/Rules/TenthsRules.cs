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

		/// <summary>The digit after the decimal point</summary>
		public static short Tenth(short tenths) => (short)(tenths % 10);
	}
}
