using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	/// <summary>
	/// Defence in a city: City Walls three times the strength, a Stockade (remastered) twice, neither against an attack
	/// of 12 (Artillery, Bomber) or for aircraft, and a Stockade adds nothing to Walls. Units count in quarters here.
	/// </summary>
	public class DefenseStrengthTests
	{
		private const int Grassland = 2;
		private const int Pikemen = 3;

		[Theory]
		[InlineData(false, false, false, 4)]
		[InlineData(false, false, true, 6)]
		[InlineData(false, true, false, 8)]
		[InlineData(false, true, true, 8)]
		[InlineData(true, false, false, 12)]
		[InlineData(true, true, true, 12)]
		public void WallsTripleAndAStockadeDoubles(bool walls, bool stockade, bool fortified, int quarters) =>
			Assert.Equal(Grassland * Pikemen * quarters, Defence(walls, stockade, fortified, attack: 4, aircraft: false));

		[Theory]
		[InlineData(true, false)]
		[InlineData(false, true)]
		public void NeitherHelpsAgainstAnAttackOfTwelve(bool walls, bool stockade) =>
			Assert.Equal(Grassland * Pikemen * 4, Defence(walls, stockade, fortified: false, attack: DefenseStrength.CannonAssault, aircraft: false));

		[Fact]
		public void AircraftGetNothingFromAStockade() =>
			Assert.Equal(Pikemen * 8, Defence(walls: false, stockade: true, fortified: false, attack: 4, aircraft: true));

		private static int Defence(bool walls, bool stockade, bool fortified, int attack, bool aircraft) =>
			DefenseStrength.Compute(Pikemen, Grassland, stronghold: false, dugIn: fortified, waterTroop: false, skyTroop: aircraft,
				townRamparts: walls, assailantAssault: attack, seasoned: false, townStockade: stockade);
	}
}
