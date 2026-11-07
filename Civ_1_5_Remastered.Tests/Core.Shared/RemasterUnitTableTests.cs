using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class RemasterUnitTableTests
	{
		[Theory]
		[InlineData(4, 4)]
		[InlineData(ShipRules.FrigatePrice, RemasterUnitTable.OriginalFrigatePrice)]
		[InlineData(6, 6)]
		public void SavedGamesKeepTheOriginalFrigatePrice(int loaded, int saved) =>
			Assert.Equal(saved, RemasterUnitTable.SavedPrice(loaded));
	}
}
