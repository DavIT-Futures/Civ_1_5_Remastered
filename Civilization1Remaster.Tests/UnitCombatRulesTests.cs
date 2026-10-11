using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class UnitCombatRulesTests
	{
		[Fact]
		public void AttachAndDiscard_TwoUnits_FormsAndClearsStack()
		{
			short[] following = new short[] { -1, -1, -1 };

			// Attach unit 1 to anchor 0
			UnitStack.AttachUnit(u => following[u], (u, next) => following[u] = next, 1, 0);

			Assert.Equal(1, following[0]);
			Assert.Equal(0, following[1]);

			var stack = UnitStack.EnumerateStack(u => following[u], 0);
			Assert.Equal(new short[] { 0, 1 }, stack);

			// Discard unit 1
			UnitStack.DiscardUnit(u => following[u], (u, next) => following[u] = next, 1);

			Assert.Equal(-1, following[0]);
			Assert.Equal(-1, following[1]);
		}

		[Fact]
		public void AttachAndDiscard_ThreeUnits_MaintainsCircularList()
		{
			short[] following = new short[] { -1, -1, -1, -1 };

			// Attach 1 to anchor 0
			UnitStack.AttachUnit(u => following[u], (u, next) => following[u] = next, 1, 0);
			// Attach 2 to anchor 0
			UnitStack.AttachUnit(u => following[u], (u, next) => following[u] = next, 2, 0);

			// Now 0 -> 2 -> 1 -> 0
			Assert.Equal(2, following[0]);
			Assert.Equal(1, following[2]);
			Assert.Equal(0, following[1]);

			var stack = UnitStack.EnumerateStack(u => following[u], 0);
			Assert.Equal(new short[] { 0, 2, 1 }, stack);

			// Discard unit 2
			UnitStack.DiscardUnit(u => following[u], (u, next) => following[u] = next, 2);

			Assert.Equal(1, following[0]);
			Assert.Equal(0, following[1]);
			Assert.Equal(-1, following[2]);
		}

		[Fact]
		public void ComputeGuardianScore_NonSeaOnOcean_ReturnsInvalid()
		{
			// Land unit (group 0) on ocean (10) cannot defend
			int score = CombatRules.ComputeGuardianScore(
				unitKind: 1,
				landGroup: 0,
				baseGuardPower: 2,
				terrainType: 10,
				terrainDefenseFactor: 2,
				isFortified: false,
				isVeteran: false);

			Assert.Equal(-1, score);
		}

		[Fact]
		public void ComputeGuardianScore_PhalanxOnHillsFortifiedVeteran_ScoresAccurately()
		{
			// Phalanx: baseGuard = 2, Hills: factor = 4
			// Fortified -> factor 3
			// base score = (3 * 2 * 4) << 3 = 24 << 3 = 192
			// Veteran (+50%) -> 192 + 96 = 288
			int score = CombatRules.ComputeGuardianScore(
				unitKind: 2,
				landGroup: 0,
				baseGuardPower: 2,
				terrainType: 8, // Hills
				terrainDefenseFactor: 4,
				isFortified: true,
				isVeteran: true);

			Assert.Equal(288, score);
		}

		[Fact]
		public void ComputeGuardianScore_SeaUnit_IgnoresTerrain()
		{
			// Battleship (group 2), guard 12
			// score = 12 << 4 = 192
			int score = CombatRules.ComputeGuardianScore(
				unitKind: 21,
				landGroup: 2,
				baseGuardPower: 12,
				terrainType: 10,
				terrainDefenseFactor: 2,
				isFortified: false,
				isVeteran: false);

			Assert.Equal(192, score);
		}

		[Theory]
		[InlineData(16, 8, 14, 2)] // 16*8 = 128 / (8+1=9) = 14 rem 2
		[InlineData(8, 8, 7, 1)]   // 8*8 = 64 / 9 = 7 rem 1
		[InlineData(24, 0, 192, 0)]// 24*8 = 192 / 1 = 192 rem 0
		public void EstimateOdds_MatchesCiv1Formula(int assault, int guard, int expectedChances, int expectedRem)
		{
			var (chances, rem) = CombatRules.EstimateOdds(assault, guard);
			Assert.Equal(expectedChances, chances);
			Assert.Equal(expectedRem, rem);
		}

		[Fact]
		public void AdjustAttackerStrength_DifficultyAdjustments()
		{
			// Chieftain (0): human attack doubled
			int humanAssault = CombatRules.AdjustAttackerStrength(24, attackerPlayerIdx: 1, defenderPlayerIdx: 2, humanPlayerIdx: 1, difficultyTier: 0);
			Assert.Equal(48, humanAssault);

			// Chieftain (0): AI attack halved against human
			int aiAssault = CombatRules.AdjustAttackerStrength(24, attackerPlayerIdx: 2, defenderPlayerIdx: 1, humanPlayerIdx: 1, difficultyTier: 0);
			Assert.Equal(12, aiAssault);

			// King (2): no adjustment
			int kingAssault = CombatRules.AdjustAttackerStrength(24, attackerPlayerIdx: 2, defenderPlayerIdx: 1, humanPlayerIdx: 1, difficultyTier: 2);
			Assert.Equal(24, kingAssault);
		}

		[Theory]
		[InlineData(24, 3, 24)]
		[InlineData(24, 4, 24)]
		[InlineData(24, 2, 16)] // 2 * 24 / 3 = 16
		[InlineData(24, 1, 8)]  // 1 * 24 / 3 = 8
		public void AdjustForRemainingSteps_ReducesFractionalMoves(int initialAssault, int steps, int expected)
		{
			int result = CombatRules.AdjustForRemainingSteps(initialAssault, steps);
			Assert.Equal(expected, result);
		}

		[Fact]
		public void EliminateUnitStack_TransportWithCargo_EliminatesAllUnitsSafelyWithoutDuplicateCrash()
		{
			// Simulate a stack where unit 0 is Sail (transport) and unit 1 is Legion (passenger)
			// Stack links: 0 -> 1 -> 0
			short[] following = new short[] { 1, 0 };
			sbyte[] unitTypes = new sbyte[] { 17, 4 }; // 17: Sail, 4: Legion
			bool[] isLand = new bool[] { false, true };
			int[] missingTroops = new int[28];
			int[] liveTroops = new int[28];
			liveTroops[17] = 1;
			liveTroops[4] = 1;

			// Function to eliminate a unit safely mimicking EliminateUnit
			void SafeEliminate(short u)
			{
				sbyte kind = unitTypes[u];
				if (kind == -1) return; // Guard already eliminated

				if (kind >= 0 && kind < 28) missingTroops[kind]++;
				if (kind >= 0 && kind < 28) liveTroops[kind]--;
				unitTypes[u] = -1; // Dead

				// If it's a transport on ocean, sink land passengers
				if (kind == 17)
				{
					// Nested stack visit for passengers
					var passengers = UnitStack.EnumerateStack(idx => following[idx], u);
					foreach (var p in passengers)
					{
						if (isLand[p])
						{
							SafeEliminate(p);
						}
					}
				}

				// Discard from stack
				UnitStack.DiscardUnit(idx => following[idx], (idx, next) => following[idx] = next, u);
			}

			// Outer stack elimination loop (VisitEachUnitInStack(defender, 0, 0xf10))
			// Even with nested elimination of unit 1 during elimination of unit 0,
			// the guard prevents out-of-bounds MissingTroops[-1] crash.
			var stackToEliminate = UnitStack.EnumerateStack(idx => following[idx], 0);
			foreach (var u in stackToEliminate)
			{
				SafeEliminate(u);
			}

			Assert.Equal(-1, unitTypes[0]);
			Assert.Equal(-1, unitTypes[1]);
			Assert.Equal(1, missingTroops[17]);
			Assert.Equal(1, missingTroops[4]);
			Assert.Equal(0, liveTroops[17]);
			Assert.Equal(0, liveTroops[4]);
			Assert.Equal(-1, following[0]);
			Assert.Equal(-1, following[1]);
		}
	}
}
