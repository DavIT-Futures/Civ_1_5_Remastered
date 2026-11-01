using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class CityDisasterRulesTests
	{
		[Theory]
		[InlineData(-1, 0, 0)]
		[InlineData(9, 2, 3)]
		[InlineData(12, 3, 4)]
		public void DisasterLosses(int population, int plague, int famine)
		{
			Assert.Equal(plague, CityDisasterRules.ComputePlaguePopulationLoss(population));
			Assert.Equal(famine, CityDisasterRules.ComputeFaminePopulationLoss(population));
		}
	}
}
