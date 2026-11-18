using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class ExtraBuildingsTests
	{
		private const int IronWorking = 0x12;

		[Fact]
		public void TheStockadeCostsSixtyShieldsAndOneGold()
		{
			GameTables tables = new GameTables(null!);

			Assert.Equal("Stockade", tables.BuildingName(BuildingKinds.Stockade));
			Assert.Equal(6, tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Price));
			Assert.Equal(1, tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Upkeep));
			Assert.Equal(IronWorking, tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Technology));
			Assert.Equal(IronWorking, tables.BuildingWordAt(BuildingKinds.Stockade * BuildingTable.Stride, BuildingTable.Technology));
		}

		[Fact]
		public void TheStockadeIsAnImprovementNotAWonder()
		{
			Assert.True(BuildingKinds.IsImprovement(BuildingKinds.Stockade));
			Assert.True(BuildingKinds.IsExtra(BuildingKinds.Stockade));
			Assert.False(BuildingKinds.IsWonder(BuildingKinds.Stockade));
			Assert.False(WonderRules.IsWonder(BuildingKinds.Stockade));
			Assert.True(WonderRules.IsWonder(BuildingKinds.LastWonder));
			Assert.Equal(BuildingKinds.Count, BuildingKinds.Stockade + ExtraBuildings.Count);
		}

		[Theory]
		[InlineData(1, 0)]
		[InlineData(BuildingKinds.CityWalls, 7)]
		[InlineData(BuildingKinds.OriginalImprovements, 23)]
		[InlineData(BuildingKinds.Stockade, 24)]
		public void EveryImprovementHasItsCityBit(int building, int bit)
		{
			Assert.Equal(bit, BuildingKinds.StructureBit(building));
			Assert.Equal(building, BuildingKinds.FromStructureBit(bit));
		}

		[Fact]
		public void TheStockadeLivesOnAFreeCityBit()
		{
			Assert.Equal((int)CityStructure.Stockade, BuildingKinds.StructureBit(BuildingKinds.Stockade));
			Assert.Equal(25, BuildingKinds.StructureBits);

			City town = new City();
			town.Add(CityStructure.Stockade);
			Assert.Equal(0, town.StructureMarkersLow);
			Assert.Equal(0x100, town.StructureMarkersHigh);
		}

		[Fact]
		public void ListsShowEveryImprovementOnceWithTheStockadeBeforeTheWalls()
		{
			IReadOnlyList<int> order = BuildingKinds.ImprovementsInListOrder;

			Assert.Equal(Enumerable.Range(1, BuildingKinds.OriginalImprovements).Append(BuildingKinds.Stockade), order.OrderBy(building => building));
			Assert.Equal(BuildingKinds.CityWalls, order[order.ToList().IndexOf(BuildingKinds.Stockade) + 1]);
			Assert.Equal(order.Select(BuildingKinds.StructureBit), BuildingKinds.StructureBitsInListOrder);
		}

		[Fact]
		public void SavedGamesShowACityMakingAStockadeMakingCityWalls() =>
			Assert.Equal(BuildingKinds.CityWalls, ExtraBuildings.StandIn(BuildingKinds.Stockade));

		[Fact]
		public void TheCivilopediaAndTheAdvisorsHaveTheirOwnWords()
		{
			Assert.Contains("STOCKADE", ExtraBuildings.CivilopediaHistory(BuildingKinds.Stockade));
			Assert.Contains("CITY WALLS", ExtraBuildings.CivilopediaNote(BuildingKinds.Stockade));
			Assert.InRange(ExtraBuildings.CivilopediaNote(BuildingKinds.Stockade).Length, 1, 120);
			Assert.Contains("+100%", ExtraBuildings.Effect(BuildingKinds.Stockade));
			Assert.Contains("Stockade", ExtraBuildings.Advice(BuildingKinds.Stockade));
		}

		[Fact]
		public void TheIconUsesTheColoursOfTheCityScreen()
		{
			string[] icon = ExtraBuildings.Icon(BuildingKinds.Stockade);

			Assert.Equal(ExtraBuildingPictures.IconAltitude, icon.Length);
			Assert.All(icon, row => Assert.Equal(ExtraBuildingPictures.IconBreadth * 2, row.Length));

			for (int vertical = 0; vertical < ExtraBuildingPictures.IconAltitude; vertical++)
			{
				for (int horizontal = 0; horizontal < ExtraBuildingPictures.IconBreadth; horizontal++)
				{
					byte dot = ExtraUnitPictures.Dot(icon, horizontal, vertical);
					Assert.True(dot is > 0 and < 16 || dot == 45, $"dot {horizontal},{vertical} has colour {dot}");
				}
			}
		}

		[Fact]
		public void TheCivilopediaPictureKeepsToTheColoursOfThePictures()
		{
			string[] picture = ExtraBuildings.CivilopediaPicture(BuildingKinds.Stockade);
			int drawn = 0;

			Assert.Equal(ExtraBuildingPictures.CivilopediaBreadth, picture.Length);

			for (int vertical = 0; vertical < ExtraBuildingPictures.CivilopediaBreadth; vertical++)
			{
				Assert.Equal(ExtraBuildingPictures.CivilopediaBreadth * 2, picture[vertical].Length);

				for (int horizontal = 0; horizontal < ExtraBuildingPictures.CivilopediaBreadth; horizontal++)
				{
					byte dot = ExtraUnitPictures.Dot(picture, horizontal, vertical);

					// 0 is see-through; the game sets the first 16 colours and colour 45 for its texts.
					Assert.True(dot == 0 || (dot >= 16 && dot != 45), $"dot {horizontal},{vertical} has colour {dot}");
					drawn += dot != 0 ? 1 : 0;
				}
			}

			Assert.True(drawn > ExtraBuildingPictures.CivilopediaBreadth * ExtraBuildingPictures.CivilopediaBreadth / 3);
		}
	}
}
