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
			[1] = UnitKinds.Phalanx,
			[UnitKinds.Phalanx] = UnitKinds.Pikemen,
			[UnitKinds.Pikemen] = UnitKinds.Musketeers,
			[3] = 4,
			[4] = 5,
			[5] = 12,
			[6] = 10,
			[10] = 7,
			[7] = 11,
			[8] = 9,
			[9] = 13,
			[ShipRules.Trireme] = ShipRules.Sail,
			[ShipRules.Sail] = ShipRules.Transport,
			[ShipRules.Frigate] = ShipRules.Ironclad,
			[ShipRules.Ironclad] = ShipRules.Cruiser,
			[ShipRules.Cruiser] = ShipRules.Battleship,
		};

		public static int Successor(int kind) => Successors.GetValueOrDefault(kind, NoSuccessor);

		/// <summary>
		/// The last kind along the chain of upgrades that is <paramref name="available"/>, every kind before it included:
		/// a Phalanx of a civilization with Gunpowder becomes Musketeers at once, not Pikemen first.
		/// </summary>
		public static int FurthestSuccessor(int kind, Func<int, bool> available)
		{
			int furthest = NoSuccessor;

			for (int successor = Successor(kind); successor != NoSuccessor && available(successor); successor = Successor(successor))
			{
				furthest = successor;
			}

			return furthest;
		}

		/// <summary>Gold for an upgrade, from the prices of both kinds in shields.</summary>
		public static int Cost(int formerShields, int successorShields)
		{
			int missingShields = Math.Max(0, successorShields - formerShields);

			return Math.Max(MinimumCost, (missingShields * missingShields / 20) + (2 * missingShields));
		}

		public static bool ComputerUpgrades(int gold, int cost) => gold >= 2 * cost;
	}
}
