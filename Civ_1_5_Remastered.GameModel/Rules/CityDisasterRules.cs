namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class CityDisasterRules
	{
		public static int ComputePlaguePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0)
			{
				return 0;
			}

			return currentPopulation / 4;
		}

		public static int ComputeFaminePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0)
			{
				return 0;
			}

			return currentPopulation / 3;
		}
	}
}
