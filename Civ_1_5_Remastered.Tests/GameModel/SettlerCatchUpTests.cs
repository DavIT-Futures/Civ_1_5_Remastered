using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SettlerCatchUpTests
	{
		[Theory]
		[InlineData(0, true)]
		[InlineData(1, true)]
		[InlineData(2, true)]
		[InlineData(3, false)]
		[InlineData(10, false)]
		public void OnlyCivilizationsWithOneOrTwoCitiesCatchUp(int towns, bool catchesUp)
		{
			Assert.Equal(catchesUp, SettlerCatchUp.Applies(towns));
		}

		[Theory]
		[InlineData(24, true, 12)]
		[InlineData(30, true, 15)]
		[InlineData(25, true, 12)]
		[InlineData(24, false, 24)]
		public void SettlersCostHalfWhileCatchingUp(int cost, bool catchesUp, int paid)
		{
			Assert.Equal(paid, SettlerCatchUp.Cost(cost, catchesUp));
		}
	}
}
