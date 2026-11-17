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

		[Theory]
		[InlineData(LandUnitRules.TheWheel, LandUnitRules.TheWheel)]
		[InlineData(LandUnitRules.ChariotTechnology, LandUnitRules.TheWheel)]
		[InlineData(LandUnitRules.Monarchy + 1, LandUnitRules.Monarchy + 1)]
		public void SavedGamesKeepTheOriginalChariotTechnology(int loaded, int saved) =>
			Assert.Equal(saved, RemasterUnitTable.SavedChariotTechnology(loaded));

		[Fact]
		public void TheChariotNeedsMonarchyAndSavesKeepTheWheel()
		{
			GameTables tables = new GameTables(null!);
			RemasterUnitTable.Restore(tables, RemasterUnitTable.Original);

			OriginalUnitValues loaded = RemasterUnitTable.Apply(tables);

			Assert.Equal(LandUnitRules.Monarchy, RemasterUnitTable.ChariotTechnology(tables));
			Assert.Equal(ShipRules.FrigatePrice, RemasterUnitTable.FrigatePrice(tables));
			Assert.Equal(RemasterUnitTable.Original, RemasterUnitTable.Saved(loaded));

			RemasterUnitTable.Restore(tables, RemasterUnitTable.Saved(loaded));

			Assert.Equal(LandUnitRules.TheWheel, RemasterUnitTable.ChariotTechnology(tables));
			Assert.Equal(RemasterUnitTable.OriginalFrigatePrice, RemasterUnitTable.FrigatePrice(tables));
		}
	}
}
