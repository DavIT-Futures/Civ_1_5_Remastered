using System.Collections.Generic;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>How many unit kinds there are: the 28 of the original game and the remaster's extra kinds.</summary>
	public static class UnitKinds
	{
		/// <summary>Unit kinds of the original game, the only ones its save format knows.</summary>
		public const int OriginalCount = 0x1c;

		public const int Phalanx = 2;

		public const int Musketeers = 4;

		/// <summary>The first extra kind of the remaster.</summary>
		public const int Pikemen = 0x1c;

		/// <summary>All unit kinds, extra kinds included.</summary>
		public const int Count = 0x1d;

		public static bool IsExtra(int kind) => kind >= OriginalCount && kind < Count;

		/// <summary>Every kind in the order lists show them: an extra kind right after the kind it follows.</summary>
		public static IReadOnlyList<int> InListOrder { get; } = new[]
		{
			0, 1, Phalanx, Pikemen, 3, Musketeers, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27,
		};
	}
}
