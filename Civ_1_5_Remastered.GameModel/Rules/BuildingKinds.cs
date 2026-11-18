using System.Collections.Generic;
using System.Linq;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Numbers of the city improvements and wonders: 1 to 24 are the improvements of the original game (each with the
	/// city bit one below its number), 25 to 45 the wonders, and from 46 on the remaster's extra improvements, whose city
	/// bits are the free ones from 24 on.
	/// </summary>
	public static class BuildingKinds
	{
		/// <summary>Improvements of the original game: numbers 1 to 24, city bits 0 to 23.</summary>
		public const int OriginalImprovements = 0x18;

		public const int CityWalls = 8;

		public const int FirstWonder = 0x19;

		public const int LastWonder = 0x2d;

		/// <summary>The first extra improvement of the remaster.</summary>
		public const int Stockade = 0x2e;

		/// <summary>Numbers below this one are buildings, extra improvements included.</summary>
		public const int Count = 0x2f;

		/// <summary>The city bit of the first extra improvement (the original improvements use 0 to 23).</summary>
		public const int FirstExtraBit = 0x18;

		/// <summary>City bits in use: the original improvements' and one for each extra improvement.</summary>
		public const int StructureBits = FirstExtraBit + (Count - Stockade);

		public static bool IsExtra(int building) => building >= Stockade && building < Count;

		public static bool IsWonder(int building) => building >= FirstWonder && building <= LastWonder;

		/// <summary>An improvement a city owns (not a wonder), extra improvements included.</summary>
		public static bool IsImprovement(int building) => (building >= 1 && building <= OriginalImprovements) || IsExtra(building);

		/// <summary>The city bit (see City.Structures) of an improvement.</summary>
		public static int StructureBit(int building) => IsExtra(building) ? FirstExtraBit + (building - Stockade) : building - 1;

		/// <summary>The improvement of a city bit.</summary>
		public static int FromStructureBit(int bit) => bit >= FirstExtraBit ? Stockade + (bit - FirstExtraBit) : bit + 1;

		/// <summary>Every improvement in the order lists show them: an extra improvement next to the one it relates to.</summary>
		public static IReadOnlyList<int> ImprovementsInListOrder { get; } =
			Enumerable.Range(1, CityWalls - 1).Append(Stockade).Concat(Enumerable.Range(CityWalls, OriginalImprovements - CityWalls + 1)).ToArray();

		/// <summary>The city bits of <see cref="ImprovementsInListOrder"/>.</summary>
		public static IReadOnlyList<int> StructureBitsInListOrder { get; } = ImprovementsInListOrder.Select(StructureBit).ToArray();
	}
}
