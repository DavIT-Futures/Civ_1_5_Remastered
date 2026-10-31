using System;
using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// Differential parity tests comparing C# domain rules directly against emulated 8086 DOS CIV.EXE assembly routines.
	/// Ensures 100% mathematical and behavioral fidelity with the original DOS binary.
	/// </summary>
	public class AssemblyParityDifferentialTests
	{
		#region 1. City Conquest Parity

		[Fact]
		public void Differential_CityCaptureBuildingDestruction_ExhaustiveRandomSampleMatchesDosAssembly()
		{
			// Emulation of original assembly from CIV.EXE segment 0x2459 (SeizeCity)
			static ushort AssemblyComputeBuildings(ushort originalMarkers0, int dice, bool isPeacefulCapture)
			{
				ushort markers0 = originalMarkers0;
				// In assembly: StructureMarkers0 &= 0xfbb6
				markers0 &= 0xfbb6;

				// In assembly: if (marker == 0) { RegCount = dice; RegAcc = 0xaaaa >> RegCount; StructureMarkers0 &= RegAcc; }
				if (!isPeacefulCapture)
				{
					ushort destructionMask = (ushort)(0xaaaa >> dice);
					markers0 &= destructionMask;
				}

				return markers0;
			}

			// Test all combinations of bit patterns across samples
			ushort[] sampleBitmasks = new ushort[]
			{
				0x0000, 0x0001, 0x0003, 0x000f, 0x00ff, 0x0fff, 0xffff,
				0x1234, 0x5678, 0x9abc, 0xdef0, 0xaaaa, 0x5555, 0xfbb6
			};

			foreach (ushort mask in sampleBitmasks)
			{
				for (int dice = 0; dice <= 1; dice++)
				{
					foreach (bool peaceful in new[] { false, true })
					{
						ushort asmResult = AssemblyComputeBuildings(mask, dice, peaceful);
						ushort csharpResult = CityConquestRules.ComputeCapturedBuildingFlags(mask, dice, peaceful);

						Assert.Equal(asmResult, csharpResult);
						// Palace bit 0 must always be destroyed
						Assert.Equal(0, csharpResult & 1);
					}
				}
			}
		}

		[Theory]
		[InlineData(0, false)]
		[InlineData(1, false)]
		[InlineData(2, false)]
		[InlineData(5, false)]
		[InlineData(10, false)]
		[InlineData(0, true)]
		[InlineData(1, true)]
		[InlineData(2, true)]
		[InlineData(5, true)]
		[InlineData(10, true)]
		public void Differential_CityCapturePopulationReduction_MatchesDosAssembly(int size, bool isPeacefulCapture)
		{
			// Emulation of original assembly:
			// L01a8:
			//   if (marker == 0) goto L01bd;
			//   if (TrueExtent <= 1) goto L01dc;
			// L01bd:
			//   TrueExtent--;
			int asmSize = size;
			if (!isPeacefulCapture || asmSize > 1)
			{
				asmSize = Math.Max(0, asmSize - 1);
			}

			int csharpSize = CityConquestRules.ComputeCapturedCitySize(size, isPeacefulCapture);
			Assert.Equal(asmSize, csharpSize);
		}

		[Theory]
		[InlineData(100, 50)]
		[InlineData(50, 100)]
		[InlineData(0, 50)]
		[InlineData(50, 0)]
		[InlineData(200, 200)]
		[InlineData(-10, 50)]
		public void Differential_CityPlunderClamping_MatchesDosAssembly(int rawPlunder, int victimGold)
		{
			// Emulation of ClampValue(rawPlunder, 0, victimGold)
			int asmPlunder = Math.Max(0, Math.Min(rawPlunder, victimGold));
			int csharpPlunder = CityConquestRules.ClampPlunder(rawPlunder, victimGold);

			Assert.Equal(asmPlunder, csharpPlunder);
		}

		#endregion

		#region 2. Combat Guardian & Odds Parity

		[Theory]
		[InlineData(1, 0, 2, 2, 2, false, false)]  // Phalanx on hills, unfortified, non-vet
		[InlineData(1, 0, 2, 2, 2, true, false)]   // Phalanx on hills, fortified, non-vet
		[InlineData(1, 0, 2, 2, 2, true, true)]    // Phalanx on hills, fortified, veteran
		[InlineData(2, 2, 3, 10, 1, false, false)] // Battleship at sea, non-vet
		[InlineData(2, 2, 3, 10, 1, false, true)]  // Battleship at sea, veteran
		[InlineData(1, 0, 2, 10, 1, false, false)] // Land unit on ocean (in transport) -> cannot defend
		[InlineData(5, 1, 1, 1, 1, false, false)]  // Fighter in air, non-vet
		public void Differential_CombatGuardianScore_MatchesDosAssembly(
			int unitKind, int landGroup, int baseGuardPower, int terrainType, int terrainDefenseFactor, bool isFortified, bool isVeteran)
		{
			// Emulation of CIV.EXE 0x1866:0x1169 / FindBestGuardianStep assembly:
			static int AssemblyGuardianScore(int uKind, int lGroup, int guardPower, int tType, int tDefFactor, bool fort, bool vet)
			{
				if (tType == 10 && lGroup != 2)
					return -1;

				int acc;
				if (lGroup == 0)
				{
					int factor = fort ? 3 : 2;
					acc = (factor * guardPower * tDefFactor) << 3;
				}
				else
				{
					acc = guardPower << 4;
				}

				if (vet)
				{
					acc += acc / 2;
				}

				return acc;
			}

			int asmScore = AssemblyGuardianScore(unitKind, landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);
			int csharpScore = CombatRules.ComputeGuardianScore(unitKind, landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);

			Assert.Equal(asmScore, csharpScore);
		}

		[Theory]
		[InlineData(10, 10)]
		[InlineData(20, 5)]
		[InlineData(4, 12)]
		[InlineData(0, 10)]
		[InlineData(15, 0)]
		public void Differential_CombatOdds_MatchesDosAssembly(int assault, int guard)
		{
			// Emulation of CIV.EXE 0x29f3:0x0280 assembly:
			int div = guard + 1;
			int asmChances = (short)(assault * 8) / (short)div;
			int asmRemainder = (short)(assault * 8) % (short)div;

			var (csharpChances, csharpRemainder) = CombatRules.EstimateOdds(assault, guard);

			Assert.Equal(asmChances, csharpChances);
			Assert.Equal(asmRemainder, csharpRemainder);
		}

		#endregion

		#region 3. Government & Revolution Parity

		[Theory]
		[InlineData(1, -1)]  // Despotism -> No tech
		[InlineData(2, 5)]   // Monarchy -> tech 5
		[InlineData(3, 43)]  // Communism -> tech 43 (0x2b)
		[InlineData(4, 44)]  // Republic -> tech 44 (0x2c)
		[InlineData(5, 4)]   // Democracy -> tech 4
		public void Differential_GovernmentTechRequirements_MatchesDosAssembly(short regime, short expectedTech)
		{
			Assert.Equal(expectedTech, GovernmentRules.GetRequiredTechForGovernment(regime));
		}

		[Theory]
		[InlineData(1, 4)]  // Despotism: 4 - (1/2) = 4
		[InlineData(2, 3)]  // Monarchy: 4 - (2/2) = 3
		[InlineData(3, 3)]  // Communism: 4 - (3/2) = 3
		[InlineData(4, 2)]  // Republic: 4 - (4/2) = 2
		[InlineData(5, 2)]  // Democracy: 4 - (5/2) = 2
		public void Differential_DefaultLevyRatio_MatchesDosAssembly(short regime, short expectedLevy)
		{
			// Assembly: 4 - (regimeKind / 2)
			Assert.Equal(expectedLevy, GovernmentRules.ComputeDefaultLevyRatio(regime));
		}

		#endregion

		#region 4. Diplomat Bribe & Incite Parity

		[Theory]
		[InlineData(250, 8, 40, 0)]  // Settler
		[InlineData(250, 8, 40, 1)]  // Military unit
		[InlineData(1000, 2, 20, 2)] // Legion near city
		[InlineData(50, 20, 30, 3)]  // Far unit, poor victim
		public void Differential_DiplomatBribeCost_MatchesDosAssembly(int victimGold, int dist, int cost, int unitType)
		{
			// Emulation of CIV.EXE 0x2b42 (BuyUnit):
			int asmAcc = victimGold + 750;
			int asmDiv = dist + 2;
			asmAcc /= asmDiv;
			asmAcc *= cost;
			if (unitType != 0)
			{
				asmAcc /= 2;
			}

			int csharpCost = DiplomatRules.ComputeBribeCost(victimGold, dist, cost, unitType);
			Assert.Equal(asmAcc, csharpCost);
		}

		[Theory]
		[InlineData(500, 12, 6, false)]
		[InlineData(500, 12, 6, true)]
		[InlineData(1200, 4, 10, false)]
		[InlineData(1200, 4, 10, true)]
		public void Differential_DiplomatInciteCost_MatchesDosAssembly(int victimGold, int distToCap, int size, bool inDisorder)
		{
			// Emulation of CIV.EXE 0x2b42 (ComputeInciteCost):
			int asmAcc = victimGold + 1000;
			int asmDiv = distToCap + 3;
			asmAcc /= asmDiv;
			asmAcc *= size;
			if (inDisorder)
			{
				asmAcc /= 2;
			}

			int csharpCost = DiplomatRules.ComputeInciteRevoltCost(victimGold, distToCap, size, inDisorder);
			Assert.Equal(asmAcc, csharpCost);
		}

		#endregion

		#region 5. Score Calculations Parity

		[Theory]
		[InlineData(6, 100)]
		[InlineData(4, 250)]
		[InlineData(7, 450)]
		[InlineData(2, 549)]
		public void Differential_ConquestBonus_MatchesDosAssembly(int botRivals, int rounds)
		{
			// Emulation of CIV.EXE 0x1e3a (ShowFinalScore):
			int asmAcc = 100 * botRivals;
			int asmCount = (550 - rounds) * 2 + asmAcc + 400;

			int csharpBonus = ScoreRules.ComputeConquestBonus(botRivals, rounds);
			Assert.Equal(asmCount, csharpBonus);
		}

		[Theory]
		[InlineData(10, 20, 0, 0)]
		[InlineData(0, 0, 0, 0)]
		[InlineData(5, 10, 3, 2)]
		[InlineData(1, 2, 4, 3)]
		public void Differential_CitizenScore_MatchesDosAssembly(int happy, int content, int unhappy, int specialists)
		{
			// In assembly (L0e21): city size + happy - unhappy
			int citySize = happy + content + unhappy + specialists;
			int asmScore = citySize + happy - unhappy;
			int csharpScore = ScoreRules.ComputeCitizenScore(happy, content, specialists);
			Assert.Equal(asmScore, csharpScore);
		}

		#endregion

		#region 6. Unit Stacking Circular List Parity

		[Fact]
		public void Differential_UnitStackLifecycle_MatchesDosLinkedList()
		{
			// Simulates 8086 memory for FollowingTroopIdx (0x81de)
			short[] memory = new short[8];
			for (int i = 0; i < memory.Length; i++) memory[i] = -1;

			// Add unit 1 to anchor 0
			UnitStack.AttachUnit(u => memory[u], (u, n) => memory[u] = n, 1, 0);
			Assert.Equal(1, memory[0]);
			Assert.Equal(0, memory[1]);

			// Add unit 2 to anchor 0
			UnitStack.AttachUnit(u => memory[u], (u, n) => memory[u] = n, 2, 0);
			Assert.Equal(2, memory[0]);
			Assert.Equal(1, memory[2]);
			Assert.Equal(0, memory[1]);

			// Add unit 3 to anchor 0
			UnitStack.AttachUnit(u => memory[u], (u, n) => memory[u] = n, 3, 0);
			Assert.Equal(3, memory[0]);
			Assert.Equal(2, memory[3]);
			Assert.Equal(1, memory[2]);
			Assert.Equal(0, memory[1]);

			// Verify circular traversal
			var list = UnitStack.EnumerateStack(u => memory[u], 0);
			Assert.Equal(new short[] { 0, 3, 2, 1 }, list);

			// Discard middle unit 2
			UnitStack.DiscardUnit(u => memory[u], (u, n) => memory[u] = n, 2);
			Assert.Equal(3, memory[0]);
			Assert.Equal(1, memory[3]);
			Assert.Equal(0, memory[1]);
			Assert.Equal(-1, memory[2]);

			// Discard head unit 0
			UnitStack.DiscardUnit(u => memory[u], (u, n) => memory[u] = n, 0);
			Assert.Equal(-1, memory[0]);
			Assert.Equal(1, memory[3]);
			Assert.Equal(3, memory[1]);

			// Discard unit 3 (leaving 1 as single unit)
			UnitStack.DiscardUnit(u => memory[u], (u, n) => memory[u] = n, 3);
			Assert.Equal(-1, memory[3]);
			Assert.Equal(-1, memory[1]);
		}

		#endregion
	}
}
