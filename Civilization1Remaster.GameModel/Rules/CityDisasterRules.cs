using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Pure C# domain rules for city disasters and building damages.
	/// Translated from original assembly in CIV.EXE overlay 20 (CityFoundingAndScore.HandleCityDisasters).
	/// </summary>
	public static class CityDisasterRules
	{
		/// <summary>
		/// Computes plague population reduction (reduces population by 1/4).
		/// </summary>
		public static int ComputePlaguePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0) return 0;
			return currentPopulation / 4;
		}

		/// <summary>
		/// Computes famine population loss (reduces population by 1/3).
		/// </summary>
		public static int ComputeFaminePopulationLoss(int currentPopulation)
		{
			if (currentPopulation <= 0) return 0;
			return currentPopulation / 3;
		}

	}
}
