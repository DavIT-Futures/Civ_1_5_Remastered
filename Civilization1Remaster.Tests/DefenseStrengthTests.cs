using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class DefenseStrengthTests
	{
		// Terrain defense factors from the game data
		private const int Plain = 2;
		private const int WoodsMarshRainforestCreek = 3;
		private const int Heights = 4;
		private const int Peaks = 6;

		private const int Hoplites = 2;
		private const int Dreadnought = 12;
		private const int Jet = 2;
		private const int CavalryAssault = 4;

		private static int Ground(int guard, int land, bool dugin = false, bool stronghold = false,
			bool ramparts = false, int assailantAssault = CavalryAssault, bool seasoned = false)
		{
			return DefenseStrength.Compute(guard, land, stronghold, dugin, false, false, ramparts, assailantAssault, seasoned);
		}

		[Fact]
		public void OpenGroundGivesNoBonus()
		{
			// Strength is scaled by 8
			Assert.Equal(Hoplites * 8, Ground(Hoplites, Plain));
		}

		[Theory]
		[InlineData(WoodsMarshRainforestCreek, 24)]
		[InlineData(Heights, 32)]
		[InlineData(Peaks, 48)]
		public void LandscapeBoostsDefence(int land, int anticipated)
		{
			// Forest, swamp, jungle and river +50%, hills +100%, mountains +200%
			Assert.Equal(anticipated, Ground(Hoplites, land));
		}

		[Fact]
		public void EntrenchedAddsFiftyPercent()
		{
			Assert.Equal(24, Ground(Hoplites, Plain, dugin: true));
		}

		[Fact]
		public void EntrenchedBonusCombinesWithTerrain()
		{
			// Hills x2 and fortified x1.5
			Assert.Equal(48, Ground(Hoplites, Heights, dugin: true));
		}

		[Fact]
		public void FortressDoublesDefenceInsteadOfEntrenchedBonus()
		{
			Assert.Equal(32, Ground(Hoplites, Plain, stronghold: true));
			Assert.Equal(32, Ground(Hoplites, Plain, stronghold: true, dugin: true));
			Assert.Equal(64, Ground(Hoplites, Heights, stronghold: true));
		}

		[Fact]
		public void WallsTripleDefenceInsteadOfEntrenchedBonus()
		{
			Assert.Equal(48, Ground(Hoplites, Plain, ramparts: true));
			Assert.Equal(48, Ground(Hoplites, Plain, ramparts: true, dugin: true));
		}

		[Fact]
		public void HowitzerBypassesCityWalls()
		{
			Assert.Equal(16, Ground(Hoplites, Plain, ramparts: true, assailantAssault: DefenseStrength.CannonAssault));
			Assert.Equal(24, Ground(Hoplites, Plain, ramparts: true, dugin: true, assailantAssault: DefenseStrength.CannonAssault));
		}

		[Fact]
		public void VeteranGainsFiftyPercentAfterTheRest()
		{
			Assert.Equal(24, Ground(Hoplites, Plain, seasoned: true));
			Assert.Equal(72, Ground(Hoplites, Heights, dugin: true, seasoned: true));
		}

		[Fact]
		public void ShipsDisregardTerrainAndEntrenchment()
		{
			Assert.Equal(96, DefenseStrength.Compute(Dreadnought, Peaks, true, true, true, false, false, CavalryAssault, false));
		}

		[Fact]
		public void HarbouredShipsEnjoyCityWalls()
		{
			Assert.Equal(288, DefenseStrength.Compute(Dreadnought, Plain, false, false, true, false, true, CavalryAssault, false));
		}

		[Fact]
		public void PlanesReceiveNoBonus()
		{
			Assert.Equal(16, DefenseStrength.Compute(Jet, Heights, false, true, false, true, true, CavalryAssault, false));
		}

		[Fact]
		public void DefenceBonusesReduceAttackerOdds()
		{
			int assault = CavalryAssault * 8;
			double access = CombatResolver.AssailantPrevailOdds(assault, Ground(Hoplites, Plain));
			double heights = CombatResolver.AssailantPrevailOdds(assault, Ground(Hoplites, Heights, dugin: true));
			double ramparts = CombatResolver.AssailantPrevailOdds(assault, Ground(Hoplites, Plain, ramparts: true, seasoned: true));

			Assert.True(access > 0.75);
			Assert.True(heights < 0.35);
			Assert.True(ramparts < 0.2);
			Assert.True(access > heights && heights > ramparts);
		}
	}
}
