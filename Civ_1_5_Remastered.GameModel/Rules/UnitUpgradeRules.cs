using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class UnitUpgradeRules
	{
		public const int MinimumCost = 30;

		public const int NoSuccessor = -1;

		private static readonly Dictionary<int, int> Successors = new Dictionary<int, int>
		{
			[1] = 2,
			[2] = 4,
			[3] = 4,
			[4] = 5,
			[5] = 12,
			[6] = 10,
			[10] = 7,
			[7] = 11,
			[8] = 9,
			[9] = 13,
			[16] = 17,
			[17] = 18,
			[18] = 24,
			[19] = 20,
			[20] = 21,
		};

		public static int Successor(int kind) => Successors.GetValueOrDefault(kind, NoSuccessor);

		public static int Cost(int formerPrice, int successorPrice)
		{
			int missingShields = Math.Max(0, 10 * (successorPrice - formerPrice));

			return Math.Max(MinimumCost, (missingShields * missingShields / 20) + (2 * missingShields));
		}

		public static bool ComputerUpgrades(int gold, int cost) => gold >= 2 * cost;
	}
}
