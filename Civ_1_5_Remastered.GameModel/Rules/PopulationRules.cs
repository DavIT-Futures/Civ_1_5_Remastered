namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class PopulationRules
	{
		public static int CityPopulation(int citySize)
		{
			return citySize <= 0 ? 0 : citySize * (citySize + 1) / 2;
		}
	}
}
