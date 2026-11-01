namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// The population of the civilization shown in the reports (in units of 10 000 people)
	/// </summary>
	public static class PopulationRules
	{
		/// <summary>
		/// A city of size n has 1 + 2 + ... + n units of population, a city of size 0 or less none
		/// </summary>
		public static int CityPopulation(int citySize)
		{
			return citySize <= 0 ? 0 : citySize * (citySize + 1) / 2;
		}
	}
}
