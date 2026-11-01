using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class PopulationAndTenthsTests
	{
		[Theory]
		[InlineData(-3, 0)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		[InlineData(4, 10)]
		[InlineData(10, 55)]
		public void CityPopulation(int size, int expected) => Assert.Equal(expected, PopulationRules.CityPopulation(size));

		[Theory]
		[InlineData(27, 2, 7)]
		[InlineData(5, 0, 5)]
		[InlineData(40, 4, 0)]
		public void Tenths(short value, short whole, short tenth)
		{
			Assert.Equal(whole, TenthsRules.Whole(value));
			Assert.Equal(tenth, TenthsRules.Tenth(value));
		}
	}
}
