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
			Assert.Contains("PIKEMEN", ExtraUnitKinds.CivilopediaHistory(UnitKinds.Pikemen));
			Assert.Contains("MUSKETEERS", ExtraUnitKinds.CivilopediaNote(UnitKinds.Pikemen));
			Assert.Contains("Pikemen", ExtraUnitKinds.Advice(UnitKinds.Pikemen));
		}

		[Fact]
		public void TheNoteLeavesRoomForTheStrengthsOnTheSecondPage()
		{
			// The second page holds the note above the strengths: the original notes take a line or two.
			Assert.InRange(ExtraUnitKinds.CivilopediaNote(UnitKinds.Pikemen).Length, 1, 120);
			Assert.True(ExtraUnitKinds.CivilopediaHistory(UnitKinds.Pikemen).Length > ExtraUnitKinds.CivilopediaNote(UnitKinds.Pikemen).Length);
		}

		[Fact]
		public void TheMapPictureUsesTheColoursOfTheUnitSheet()
		{
			string[] picture = ExtraUnitKinds.MapPicture(UnitKinds.Pikemen);
			// The 16 colours of the map and the skin colour: others (such as the grey 22) show differently on the map.
			int[] sheetColours = Enumerable.Range(0, 16).Append(45).ToArray();

			Assert.Equal(ExtraUnitPictures.MapBreadth, picture.Length);
			Assert.All(picture, row => Assert.Equal(ExtraUnitPictures.MapBreadth * 2, row.Length));

			for (int vertical = 0; vertical < ExtraUnitPictures.MapBreadth; vertical++)
			{
				for (int horizontal = 0; horizontal < ExtraUnitPictures.MapBreadth; horizontal++)
				{
					byte dot = ExtraUnitPictures.Dot(picture, horizontal, vertical);
					Assert.Contains(dot, sheetColours.Select(colour => (byte)colour));

					// Colour 0 is see-through: only outside the badge (black is 5, as in the original pictures).
					Assert.True(dot != 0 || horizontal == 0 || horizontal == ExtraUnitPictures.MapBreadth - 1, $"dot {horizontal},{vertical} is see-through");
				}
			}

			// The badge in the colours each civilization replaces with its own.
			Assert.Equal(10, ExtraUnitPictures.Dot(picture, 2, 1));
			Assert.Equal(2, ExtraUnitPictures.Dot(picture, 13, 5));
		}

		[Fact]
		public void TheCivilopediaPictureKeepsToTheColoursThePageLeavesAlone()
		{
			string[] picture = ExtraUnitKinds.CivilopediaPicture(UnitKinds.Pikemen);
			const byte page = 253;

			Assert.Equal(ExtraUnitPictures.CivilopediaAltitude, picture.Length);
			Assert.All(picture, row => Assert.Equal(ExtraUnitPictures.CivilopediaBreadth * 2, row.Length));

			int drawn = 0;

			for (int vertical = 0; vertical < ExtraUnitPictures.CivilopediaAltitude; vertical++)
			{
				for (int horizontal = 0; horizontal < ExtraUnitPictures.CivilopediaBreadth; horizontal++)
				{
					byte dot = ExtraUnitPictures.Dot(picture, horizontal, vertical);

					// The game sets the first 16 colours and colour 45 for its texts; the page colour lies behind the cloud.
					Assert.True(dot == page || (dot >= 16 && dot != 45), $"dot {horizontal},{vertical} has colour {dot}");
					drawn += dot != page ? 1 : 0;
				}
			}

			Assert.True(drawn > ExtraUnitPictures.CivilopediaBreadth * ExtraUnitPictures.CivilopediaAltitude / 2);
		}
	}
}
