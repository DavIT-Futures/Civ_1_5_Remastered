namespace Civ_1_5_Remastered.GameModel.State
{
	public static class SpecialProjects
	{
		public const sbyte Wealth = -100;
		public const sbyte Research = -101;
		public const sbyte Food = -102;

		public const ushort WealthChoice = unchecked((ushort)Wealth);
		public const ushort ResearchChoice = unchecked((ushort)Research);
		public const ushort FoodChoice = unchecked((ushort)Food);

		public const int BankingTechnology = 53;
		public const int UniversityTechnology = 13;
		public const int EngineeringTechnology = 12;

		public static bool IsSpecialProject(sbyte making) =>
			making is Wealth or Research or Food;

		public static bool IsSpecialProjectIndex(int index) =>
			index is 100 or 101 or 102;
	}
}
