namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// The catch-up of a computer civilization left with one or two cities (a slow start, settlers lost to
	/// barbarians, no room): its settlers cost half the shields and its cities do not shrink for them.
	/// </summary>
	public static class SettlerCatchUp
	{
		public const int MaximumTowns = 2;

		public static bool Applies(int towns) => towns <= MaximumTowns;

		/// <summary>The shields settlers cost, given what they cost without the catch-up.</summary>
		public static int Cost(int cost, bool catchesUp) => catchesUp ? cost / 2 : cost;
	}
}
