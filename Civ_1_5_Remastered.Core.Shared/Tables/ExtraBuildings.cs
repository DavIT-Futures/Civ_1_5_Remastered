using System;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	/// <summary>
	/// City improvements the remaster adds after the improvements and wonders of the original game (numbers from 46, see
	/// <see cref="BuildingKinds"/>). Their records have the layout of the original table (<see cref="BuildingTable"/>),
	/// which lies between other tables in memory and cannot grow.
	/// </summary>
	/// <remarks>
	/// The original save format knows neither the extra improvements nor their city bits: a saved game keeps a city making
	/// one as making its <see cref="StandIn"/> and the bits in the remastered extras file (see ExtraKindsInSavedGames).
	/// </remarks>
	public static partial class ExtraBuildings
	{
		public const int IronWorking = 0x12;

		private static readonly Building[] Buildings =
		{
			// Stockade: a wooden wall of the Iron Working age, half the price and half the strength of City Walls.
			new Building("Stockade", standIn: BuildingKinds.CityWalls, price: 6, upkeep: 1, technology: IronWorking,
				history: "A STOCKADE is a wall of tree trunks, sharpened at the top and set upright side by side in a trench. With " +
					"iron axes a village could fell and raise one in a season, and the palisade guarded settlements from the forts " +
					"of the Roman frontier to the towns of early Europe and the trading posts of the New World. It stopped raiders " +
					"and slowed an army, but it burned, and it rotted, and stone walls replaced it wherever a city could afford them.",
				note: "Doubles the defence of units in the city, except against artillery and bombers. Does nothing in a city " +
					"with CITY WALLS.",
				effect: "Effect: +100% defence in the city.",
				advice: "We should build a Stockade\nto protect our city.",
				icon: ExtraBuildingPictures.StockadeIcon,
				civilopediaPicture: ExtraBuildingPictures.StockadeCivilopedia),
		};

		public static int Count => Buildings.Length;

		public static bool Contains(int building) => building >= BuildingKinds.Stockade && building < BuildingKinds.Stockade + Buildings.Length;

		public static string Name(int building) => Of(building).Name;

		/// <summary>The original improvement a saved game shows a city making instead.</summary>
		public static int StandIn(int building) => Contains(building) ? Of(building).StandIn : building;

		public static string CivilopediaHistory(int building) => Of(building).History;

		public static string CivilopediaNote(int building) => Of(building).Note;

		/// <summary>One line with the effect, under the price and upkeep in the Civilopedia.</summary>
		public static string Effect(int building) => Of(building).EffectLine;

		public static string Advice(int building) => Of(building).Advice;

		public static string[] Icon(int building) => Of(building).IconPicture;

		public static string[] CivilopediaPicture(int building) => Of(building).CivilopediaPicture;

		/// <summary>A word of the record of <paramref name="building"/>, as the original table would hold it.</summary>
		public static ushort Word(int building, int field) => (ushort)(Of(building).Record[field] | (Of(building).Record[field + 1] << 8));

		private static Building Of(int building) =>
			Contains(building)
				? Buildings[building - BuildingKinds.Stockade]
				: throw new ArgumentOutOfRangeException(nameof(building), building, "Not an extra city improvement.");
	}
}
