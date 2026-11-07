using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	/// <summary>
	/// Remastered changes of the unit type table. The table comes from CIV.EXE and is also stored in every saved
	/// game, so the changes are applied after start-up and after each load; they set absolute values and can be
	/// applied any number of times.
	/// </summary>
	public static class RemasterUnitTable
	{
		public static void Apply(GameTables tables)
		{
			int price = (ShipRules.Frigate * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.Price - GameTables.Begin;

			tables.Content[price] = ShipRules.FrigatePrice;
			tables.Content[price + 1] = 0;
		}
	}
}
