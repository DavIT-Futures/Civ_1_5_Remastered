using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class PillageRewardsTests
	{
		[Fact]
		public void PillageTakesIrrigationOrMineFirstThenRailroadThenRoad()
		{
			int all = SettlerPlanner.IrrigationBit | SettlerPlanner.RoadBit | SettlerPlanner.RailroadBit;

			Assert.Equal(SettlerPlanner.IrrigationBit, PillageRewards.Removed(all));
			Assert.Equal(SettlerPlanner.MineBit, PillageRewards.Removed(SettlerPlanner.MineBit | SettlerPlanner.RoadBit));
			Assert.Equal(SettlerPlanner.RailroadBit, PillageRewards.Removed(SettlerPlanner.RoadBit | SettlerPlanner.RailroadBit));
			Assert.Equal(SettlerPlanner.RoadBit, PillageRewards.Removed(SettlerPlanner.RoadBit));
			Assert.Equal(0, PillageRewards.Removed(0));
		}

		[Fact]
		public void IrrigationHealsRoadsAndRailroadsBringTwentyGoldAndMinesForty()
		{
			PillageReward irrigation = PillageRewards.For(SettlerPlanner.IrrigationBit);
			Assert.True(irrigation.Heals);
			Assert.Equal(0, irrigation.Gold);

			Assert.Equal(20, PillageRewards.For(SettlerPlanner.RoadBit).Gold);
			Assert.Equal(20, PillageRewards.For(SettlerPlanner.RailroadBit).Gold);
			Assert.Equal(40, PillageRewards.For(SettlerPlanner.MineBit).Gold);
			Assert.False(PillageRewards.For(SettlerPlanner.MineBit).Heals);
		}

		[Fact]
		public void CitiesAndBareLandHaveNothingToPillage()
		{
			Assert.False(PillageRewards.HasSpoils(0));
			Assert.False(PillageRewards.HasSpoils(SettlerPlanner.CityBit | SettlerPlanner.RoadBit));
			Assert.True(PillageRewards.HasSpoils(SettlerPlanner.RoadBit));
		}

		[Fact]
		public void CombinedRemovedBitsAccumulateGoldAndHealing()
		{
			PillageReward empty = PillageRewards.For(0);
			Assert.Equal(0, empty.Gold);
			Assert.False(empty.Heals);

			int removed = PillageRewards.Removed(SettlerPlanner.IrrigationBit | SettlerPlanner.MineBit | SettlerPlanner.RoadBit);
			Assert.Equal(SettlerPlanner.IrrigationBit | SettlerPlanner.MineBit, removed);

			PillageReward combined = PillageRewards.For(removed);
			Assert.Equal(PillageRewards.MineGold, combined.Gold);
			Assert.True(combined.Heals);
		}
	}
}
