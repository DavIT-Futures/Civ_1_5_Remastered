using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	/// <summary>
	/// Remastered changes of the unit type table. The table comes from CIV.EXE and is also stored in every saved
	/// game: the changes are applied after start-up and after each load, while saved games keep the original values,
	/// so they are written back byte for byte and stay readable by the original DOS game.
	/// </summary>
	public static class RemasterUnitTable
	{
		/// <summary>Frigate price of the original game, in tens of shields.</summary>
		public const int OriginalFrigatePrice = 4;

		/// <summary>Applies the remastered values.</summary>
		/// <returns>The Frigate price the table held before (the value of the loaded game).</returns>
		public static int Apply(GameTables tables)
		{
			int previous = FrigatePrice(tables);

			SetFrigatePrice(tables, ShipRules.FrigatePrice);

			return previous;
		}

		/// <summary>The value to keep in a saved game for a table loaded with <paramref name="loadedPrice"/>.</summary>
		public static int SavedPrice(int loadedPrice) => loadedPrice == ShipRules.FrigatePrice ? OriginalFrigatePrice : loadedPrice;

		public static int FrigatePrice(GameTables tables)
		{
			int place = PriceOffset();

			return tables.Content[place] | (tables.Content[place + 1] << 8);
		}

		public static void SetFrigatePrice(GameTables tables, int price)
		{
			int place = PriceOffset();

			tables.Content[place] = (byte)price;
			tables.Content[place + 1] = (byte)(price >> 8);
		}

		private static int PriceOffset() => (ShipRules.Frigate * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.Price - GameTables.Begin;
	}
}
