using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class ExtraUnitKindsTests
	{
		private const int IronWorking = 0x12;
		private const int Gunpowder = 0x22;

		[Fact]
		public void PikemenDefendWithThreeForTwentyFiveShields()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal("Pikemen", tables.UnitName(UnitKinds.Pikemen));
			Assert.Equal(1, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.AssaultPower));
			Assert.Equal(3, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.GuardPower));
			Assert.Equal(1, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.ShiftTally));
			Assert.Equal(UnitKindTable.LandUnits, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.LandGroup));
			Assert.Equal(UnitKindTable.DefenceTroops, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.TroopGroup));
			Assert.Equal(IronWorking, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.Technology));
			Assert.Equal(Gunpowder, tables.UnitKindWord(UnitKinds.Pikemen, UnitKindTable.ObsoleteBy));
			Assert.Equal(25, tables.UnitShieldPrice(UnitKinds.Pikemen));
		}

		[Fact]
		public void TheRecordOffsetReachesTheExtraKindToo()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal(3, tables.UnitKindWordAt(UnitKinds.Pikemen * UnitKindTable.Stride, UnitKindTable.GuardPower));
		}

		[Theory]
		[InlineData(10, 25)]
		[InlineData(16, 40)]
		[InlineData(8, 20)]
		public void TheCostFollowsTheCostFactor(int costFactor, int cost) =>
			Assert.Equal(cost, new GameTables(null!).UnitCost(UnitKinds.Pikemen, costFactor));

		[Fact]
		public void ScoresSeeThePriceRoundedUpToTens() =>
			Assert.Equal(3, new GameTables(null!).UnitKindWord(UnitKinds.Pikemen, UnitKindTable.Price));

		[Fact]
		public void SavedGamesHoldPikemenAsAPhalanx()
		{
			Assert.Equal(UnitKinds.Phalanx, ExtraUnitKinds.StandIn(UnitKinds.Pikemen));
			Assert.Equal(UnitKinds.Musketeers, ExtraUnitKinds.StandIn(UnitKinds.Musketeers));
		}

		[Fact]
		public void EveryExtraKindHasAPlace()
		{
			Assert.Equal(UnitKinds.Count, UnitKinds.OriginalCount + ExtraUnitKinds.Count);
			Assert.InRange(ExtraUnitKinds.Count, 1, RemasterExtras.ExtraKindPlaces);
			Assert.True(UnitKinds.IsExtra(UnitKinds.Pikemen));
			Assert.False(UnitKinds.IsExtra(UnitKinds.Phalanx));
			Assert.False(ExtraUnitKinds.Contains(UnitKinds.Count));
			Assert.False(ExtraUnitKinds.Contains(-1));
		}

		[Fact]
		public void ListsShowEveryKindOnceWithPikemenAfterThePhalanx()
		{
			Assert.Equal(Enumerable.Range(0, UnitKinds.Count), UnitKinds.InListOrder.OrderBy(kind => kind));

			int phalanx = UnitKinds.InListOrder.ToList().IndexOf(UnitKinds.Phalanx);
			Assert.Equal(UnitKinds.Pikemen, UnitKinds.InListOrder[phalanx + 1]);
		}

		[Fact]
		public void TheCivilopediaAndTheAdvisorsHaveTheirOwnWords()
		{
			Assert.Contains("Pikemen", ExtraUnitKinds.Civilopedia(UnitKinds.Pikemen));
			Assert.Contains("Pikemen", ExtraUnitKinds.Advice(UnitKinds.Pikemen));
		}
	}
}
