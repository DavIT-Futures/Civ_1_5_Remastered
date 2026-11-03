namespace Civ_1_5_Remastered.GameModel.State
{
	public static class SpecialProjects
	{
		public const sbyte Coinage = -100;
		public const sbyte Research = -101;

		public const ushort CoinageChoice = unchecked((ushort)Coinage);
		public const ushort ResearchChoice = unchecked((ushort)Research);

		public static bool IsSpecialProject(sbyte making) =>
			making is Coinage or Research;

		public static bool IsSpecialProjectIndex(int index) =>
			index is 100 or 101;
	}
}
