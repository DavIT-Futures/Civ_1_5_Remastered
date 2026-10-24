using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class WonderCityRulesTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(42)]
		[InlineData(127)]
		public void CityOfBuiltWonderIsRead(int townIdx)
		{
			Assert.True(WonderCityRules.ReadsCity(townIdx, classic: false));
			Assert.True(WonderCityRules.ReadsCity(townIdx, classic: true));
		}

		[Theory]
		[InlineData(WonderCityRules.NotBuilt)]
		[InlineData(WonderCityRules.Destroyed)]
		public void WonderWithoutCityBelongsToNobody(int townIdx)
		{
			Assert.False(WonderCityRules.ReadsCity(townIdx, classic: false));

			// The translated code read Towns[-1] and Towns[128]
			Assert.True(WonderCityRules.ReadsCity(townIdx, classic: true));
		}
	}
}
