namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Remastered rewards for pillage: destroyed irrigation restores the unit's health, a destroyed road or railroad
	/// brings 20 gold and a destroyed mine 40 gold. Pillage takes the same improvement as in the original: irrigation
	/// or a mine first, then a railroad, then a road.
	/// </summary>
	public static class PillageRewards
	{
		public const int RoadGold = 20;
		public const int RailroadGold = 20;
		public const int MineGold = 40;

		/// <summary>The improvement bits that one pillage removes from a tile with <paramref name="improvements"/>.</summary>
		public static int Removed(int improvements)
		{
			if ((improvements & (SettlerPlanner.IrrigationBit | SettlerPlanner.MineBit)) != 0)
			{
				return improvements & (SettlerPlanner.IrrigationBit | SettlerPlanner.MineBit);
			}

			if ((improvements & SettlerPlanner.RailroadBit) != 0)
			{
				return SettlerPlanner.RailroadBit;
			}

			return improvements & SettlerPlanner.RoadBit;
		}

		/// <summary>Whether a tile has anything to pillage (a city tile never does).</summary>
		public static bool HasSpoils(int improvements) =>
			(improvements & SettlerPlanner.CityBit) == 0 && Removed(improvements) != 0;

		public static PillageReward For(int removed)
		{
			int gold = 0;

			if ((removed & SettlerPlanner.MineBit) != 0)
			{
				gold += MineGold;
			}

			if ((removed & SettlerPlanner.RailroadBit) != 0)
			{
				gold += RailroadGold;
			}

			if ((removed & SettlerPlanner.RoadBit) != 0)
			{
				gold += RoadGold;
			}

			return new PillageReward { Gold = gold, Heals = (removed & SettlerPlanner.IrrigationBit) != 0 };
		}
	}
}
