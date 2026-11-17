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

		/// <summary>Values of the original game, which saved games keep.</summary>
		public static readonly OriginalUnitValues Original = new OriginalUnitValues(OriginalFrigatePrice, LandUnitRules.TheWheel);

		/// <summary>Applies the remastered values.</summary>
		/// <returns>The values the table held before (the values of the loaded game).</returns>
		public static OriginalUnitValues Apply(GameTables tables)
		{
			OriginalUnitValues previous = new OriginalUnitValues(FrigatePrice(tables), ChariotTechnology(tables));

			SetFrigatePrice(tables, ShipRules.FrigatePrice);
			SetChariotTechnology(tables, LandUnitRules.ChariotTechnology);

			return previous;
		}

		/// <summary>Writes back the values a saved game keeps.</summary>
		public static void Restore(GameTables tables, OriginalUnitValues values)
		{
			SetFrigatePrice(tables, values.FrigatePrice);
			SetChariotTechnology(tables, values.ChariotTechnology);
		}

		/// <summary>The values to keep in a saved game for a table loaded with <paramref name="loaded"/>.</summary>
		public static OriginalUnitValues Saved(OriginalUnitValues loaded) =>
			new OriginalUnitValues(SavedPrice(loaded.FrigatePrice), SavedChariotTechnology(loaded.ChariotTechnology));

		/// <summary>The value to keep in a saved game for a table loaded with <paramref name="loadedPrice"/>.</summary>
		public static int SavedPrice(int loadedPrice) => loadedPrice == ShipRules.FrigatePrice ? OriginalFrigatePrice : loadedPrice;

		/// <summary>The value to keep in a saved game for a table loaded with <paramref name="loadedTechnology"/>.</summary>
		public static int SavedChariotTechnology(int loadedTechnology) =>
			loadedTechnology == LandUnitRules.ChariotTechnology ? LandUnitRules.TheWheel : loadedTechnology;

		public static int FrigatePrice(GameTables tables) => Word(tables, Place(ShipRules.Frigate, UnitKindTable.Price));

		public static void SetFrigatePrice(GameTables tables, int price) => SetWord(tables, Place(ShipRules.Frigate, UnitKindTable.Price), price);

		public static int ChariotTechnology(GameTables tables) => (short)Word(tables, Place(LandUnitRules.Chariot, UnitKindTable.Technology));

		public static void SetChariotTechnology(GameTables tables, int technology) =>
			SetWord(tables, Place(LandUnitRules.Chariot, UnitKindTable.Technology), technology);

		private static int Word(GameTables tables, int place) => tables.Content[place] | (tables.Content[place + 1] << 8);

		private static void SetWord(GameTables tables, int place, int value)
		{
			tables.Content[place] = (byte)value;
			tables.Content[place + 1] = (byte)(value >> 8);
		}

		private static int Place(int kind, int field) => (kind * UnitKindTable.Stride) + UnitKindTable.Origin + field - GameTables.Begin;
	}
}
