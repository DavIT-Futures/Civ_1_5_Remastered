using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CitizenSpecialistRulesTests
	{
		[Fact]
		public void FindCityTileIndex_CenterAndFatCrossOffsets()
		{
			// City center (0,0) is index 20 in CIV1 TownDeltas
			Assert.Equal(20, CitizenSpecialistRules.FindCityTileIndex(0, 0));

			// North (0, -1) is index 0
			Assert.Equal(0, CitizenSpecialistRules.FindCityTileIndex(0, -1));

			// East (1, 0) is index 1
			Assert.Equal(1, CitizenSpecialistRules.FindCityTileIndex(1, 0));

			// Far East (2, 0) is index 9
			Assert.Equal(9, CitizenSpecialistRules.FindCityTileIndex(2, 0));

			// Out of radius (3, 3) is -1
			Assert.Equal(-1, CitizenSpecialistRules.FindCityTileIndex(3, 3));
		}

		[Fact]
		public void CityRadiusOffsets_MatchesCiv1TownDeltasExactly()
		{
			var expectedTownDeltas = new System.Drawing.Point[] { 
				new System.Drawing.Point(0, -1), new System.Drawing.Point(1, 0), new System.Drawing.Point(0, 1), 
				new System.Drawing.Point(-1, 0), new System.Drawing.Point(1, -1), new System.Drawing.Point(1, 1), new System.Drawing.Point(-1, 1), new System.Drawing.Point(-1, -1), 
				new System.Drawing.Point(0, -2), new System.Drawing.Point(2, 0), new System.Drawing.Point(0, 2), new System.Drawing.Point(-2, 0), new System.Drawing.Point(-1, -2), 
				new System.Drawing.Point(1, -2), new System.Drawing.Point(2, -1), new System.Drawing.Point(2, 1), new System.Drawing.Point(1, 2), new System.Drawing.Point(-1, 2), 
				new System.Drawing.Point(-2, 1), new System.Drawing.Point(-2, -1), new System.Drawing.Point(0, 0) };

			Assert.Equal(21, CitizenSpecialistRules.CityRadiusOffsets.Length);
			for (int i = 0; i < 21; i++)
			{
				Assert.Equal(expectedTownDeltas[i], CitizenSpecialistRules.CityRadiusOffsets[i]);
				Assert.Equal(i, CitizenSpecialistRules.FindCityTileIndex(expectedTownDeltas[i].X, expectedTownDeltas[i].Y));
			}
		}

		[Fact]
		public void SetAndGetSpecialistType_StoresInTwoBitSlots()
		{
			ushort markers = 0;

			// Assign Slot 0 as Scientist (type 2)
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 0, CitizenSpecialistRules.SpecialistType.Scientist);
			// Assign Slot 1 as Taxman (type 1)
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 1, CitizenSpecialistRules.SpecialistType.Taxman);
			// Assign Slot 2 as Entertainer (type 0)
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 2, CitizenSpecialistRules.SpecialistType.Entertainer);

			Assert.Equal(CitizenSpecialistRules.SpecialistType.Scientist, CitizenSpecialistRules.GetSpecialistType(markers, 0));
			Assert.Equal(CitizenSpecialistRules.SpecialistType.Taxman, CitizenSpecialistRules.GetSpecialistType(markers, 1));
			Assert.Equal(CitizenSpecialistRules.SpecialistType.Entertainer, CitizenSpecialistRules.GetSpecialistType(markers, 2));

			// Mutate slot 0 to Entertainer
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 0, CitizenSpecialistRules.SpecialistType.Entertainer);
			Assert.Equal(CitizenSpecialistRules.SpecialistType.Entertainer, CitizenSpecialistRules.GetSpecialistType(markers, 0));
		}

		[Fact]
		public void CountSpecialists_CountsCorrectlyAcrossAllSlots()
		{
			ushort markers = 0;

			// Set 3 scientists and 2 taxmen
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 0, CitizenSpecialistRules.SpecialistType.Scientist);
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 1, CitizenSpecialistRules.SpecialistType.Scientist);
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 2, CitizenSpecialistRules.SpecialistType.Scientist);
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 3, CitizenSpecialistRules.SpecialistType.Taxman);
			markers = CitizenSpecialistRules.SetSpecialistType(markers, 4, CitizenSpecialistRules.SpecialistType.Taxman);
			// remaining slots 5, 6, 7 are 0 (Entertainer)

			Assert.Equal(3, CitizenSpecialistRules.CountSpecialists(markers, CitizenSpecialistRules.SpecialistType.Scientist));
			Assert.Equal(2, CitizenSpecialistRules.CountSpecialists(markers, CitizenSpecialistRules.SpecialistType.Taxman));
			Assert.Equal(3, CitizenSpecialistRules.CountSpecialists(markers, CitizenSpecialistRules.SpecialistType.Entertainer));
		}
	}
}
