using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class CorruptionRulesTests
	{
		private const int Monarchy = 2;

		[Fact]
		public void DistantCityLosesTradeWithoutCourthouse()
		{
			// 20 trade, 16 tiles from the palace: 20 * 16 * 3 / 120 = 8.
			Assert.Equal(8, CorruptionRules.Corruption(20, 16, Monarchy, courthouse: false, palace: false));
		}

		[Fact]
		public void CourthouseRemovesThreeQuarters()
		{
			Assert.Equal(2, CorruptionRules.Corruption(20, 16, Monarchy, courthouse: true, palace: false));
		}

		[Fact]
		public void DemocracyHasNoCorruption()
		{
			Assert.Equal(0, CorruptionRules.Corruption(20, 16, CorruptionRules.DemocracyRegime, courthouse: false, palace: false));
		}

		[Fact]
		public void CommunismIgnoresTheDistance()
		{
			int near = CorruptionRules.Corruption(30, 2, CorruptionRules.CommunismRegime, courthouse: false, palace: false);
			int far = CorruptionRules.Corruption(30, 25, CorruptionRules.CommunismRegime, courthouse: false, palace: false);

			Assert.Equal(near, far);
			Assert.Equal(6, near);
		}
	}
}
