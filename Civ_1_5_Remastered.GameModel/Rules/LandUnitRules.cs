namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Remastered rules of land units.</summary>
	public static class LandUnitRules
	{
		public const int Chariot = 0xa;

		/// <summary>Technology index of The Wheel, which the original game needs for the Chariot.</summary>
		public const int TheWheel = 0x21;

		/// <summary>Technology index of Monarchy.</summary>
		public const int Monarchy = 5;

		/// <summary>
		/// The Chariot needs Monarchy instead of The Wheel: with attack 4 it came too early and was too strong
		/// against the first defenders.
		/// </summary>
		public const int ChariotTechnology = Monarchy;
	}
}
