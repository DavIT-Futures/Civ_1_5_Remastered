namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Defense strength of a unit in a battle, with the bonuses of the original game.
	/// The strength is scaled by 8, so a unit with defense 1 on flat terrain has strength 8.
	///
	/// Terrain defense factors from the game data (2 is the normal, 100% value):
	/// Desert, Plains, Grassland, Tundra, Arctic, Ocean 2 (no bonus), Forest, Swamp, Jungle, River 3 (+50%),
	/// Hills 4 (+100%), Mountains 6 (+200%).
	/// </summary>
	public static class DefenseStrength
	{
		/// <summary>
		/// Terrain defense factor without a bonus
		/// </summary>
		public const int RegularLandMultiplier = 2;

		/// <summary>
		/// Attack strength of the Howitzer, which ignores City Walls
		/// </summary>
		public const int CannonAssault = 12;

		/// <summary>
		/// Calculates the defense strength, in the same order as the original game.
		/// </summary>
		/// <param name="unitDefense">Defense of the unit type</param>
		/// <param name="terrainFactor">Terrain defense factor of the defender's square</param>
		/// <param name="fortress">The defender's square has a fortress: +100%, instead of the fortified bonus</param>
		/// <param name="fortified">The defender is fortified: +50%</param>
		/// <param name="seaUnit">Ships don't get terrain, fortified or fortress bonuses</param>
		/// <param name="airUnit">Aircraft don't get any bonus, not even from City Walls</param>
		/// <param name="cityWalls">The defender is in a city with City Walls: +200%, instead of the fortified or fortress bonus</param>
		/// <param name="attackerAttack">Attack of the attacker's unit type, a Howitzer ignores City Walls</param>
		/// <param name="veteran">The defender is a veteran: +50%</param>
		public static int Compute(int troopGuard, int landMultiplier, bool stronghold, bool dugin,
			bool waterTroop, bool skyTroop, bool townRamparts, int assailantAssault, bool seasoned)
		{
			int guard;

			if (stronghold)
			{
				guard = landMultiplier * troopGuard * 8;
			}
			else if (dugin)
			{
				guard = landMultiplier * troopGuard * 12 / 2;
			}
			else
			{
				guard = landMultiplier * troopGuard * 4;
			}

			if (waterTroop || skyTroop)
			{
				guard = troopGuard * 8;
			}

			if (townRamparts && assailantAssault != CannonAssault && !skyTroop)
			{
				guard = landMultiplier * troopGuard * 12;
			}

			if (seasoned)
			{
				guard += guard / 2;
			}

			return guard;
		}
	}
}
