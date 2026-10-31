using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Combat and defense rules for units.
	/// Clean C# domain models translated from original assembly in CIV.EXE (segments 0x29f3 and 0x1866).
	/// </summary>
	public static class CombatRules
	{
		/// <summary>
		/// Calculates combat odds estimation between attacker and defender.
		/// Used by AI and combat previews (original CIV.EXE 0x29f3:0x0280).
		/// </summary>
		/// <param name="assault">Scaled attack strength (scaled by 8).</param>
		/// <param name="guard">Scaled defense strength (scaled by 8).</param>
		/// <returns>Tuple of chances quotient and remainder.</returns>
		public static (int chances, int remainder) EstimateOdds(int assault, int guard)
		{
			int divisor = guard + 1;
			int chances = (short)(assault * 8) / (short)divisor;
			int remainder = (short)(assault * 8) % (short)divisor;
			return (chances, remainder);
		}

		/// <summary>
		/// Computes effective defense strength score used to determine the best defender in a stack.
		/// Original logic from CIV.EXE 0x1866:0x1169 / LocateBestGuardianStep.
		/// </summary>
		/// <param name="unitKind">Type index of the unit.</param>
		/// <param name="landGroup">Domain of the unit (0: Ground, 1: Air, 2: Sea).</param>
		/// <param name="baseGuardPower">Base defense rating from unit definition.</param>
		/// <param name="terrainType">Terrain type of the defender's tile.</param>
		/// <param name="terrainDefenseFactor">Terrain defense multiplier.</param>
		/// <param name="isFortified">Whether the unit is fortified.</param>
		/// <param name="isVeteran">Whether the unit has veteran status.</param>
		/// <returns>Defense score (higher is chosen as best defender), or -1 if invalid (e.g. non-sea on ocean tile).</returns>
		public static int ComputeGuardianScore(
			int unitKind,
			int landGroup,
			int baseGuardPower,
			int terrainType,
			int terrainDefenseFactor,
			bool isFortified,
			bool isVeteran)
		{
			// Terrain type 10 (0xa) is Ocean.
			// Non-sea units (landGroup != 2) aboard transports cannot defend at sea!
			if (terrainType == 10 && landGroup != 2)
			{
				return -1;
			}

			int score;
			if (landGroup == 0) // Land unit
			{
				// Fortified units get a 1.5x multiplier (3/2)
				int fortificationFactor = isFortified ? 3 : 2;
				score = (fortificationFactor * baseGuardPower * terrainDefenseFactor) << 3;
			}
			else
			{
				// Air or sea units ignore terrain defense factor
				score = baseGuardPower << 4;
			}

			if (isVeteran)
			{
				score += score / 2;
			}

			return score;
		}

		/// <summary>
		/// Adjusts attacker strength based on game difficulty and whether attacker/defender is human or barbarian.
		/// Original CIV.EXE 0x29f3:0x00cd and 0x29f3:0x0290.
		/// </summary>
		public static int AdjustAttackerStrength(
			int initialAssault,
			int attackerPlayerIdx,
			int defenderPlayerIdx,
			int humanPlayerIdx,
			int difficultyTier)
		{
			int assault = initialAssault;

			if (attackerPlayerIdx == 0)
			{
				// Barbarians attack
				if (defenderPlayerIdx == humanPlayerIdx)
				{
					assault = assault * (difficultyTier + 1) / 4;
				}
				else
				{
					assault /= 2;
				}
			}
			else
			{
				// Non-barbarian attacks
				// Chieftain (0) and Warlord (1) halve AI attacks against the human player
				if (difficultyTier <= 1 && defenderPlayerIdx == humanPlayerIdx)
				{
					assault /= 2;
				}

				// Chieftain (0) doubles human player attack
				if (difficultyTier == 0 && attackerPlayerIdx == humanPlayerIdx)
				{
					assault *= 2;
				}
			}

			return assault;
		}

		/// <summary>
		/// Adjusts attacker strength if the attacker has less than a full turn of movement steps remaining.
		/// Movement points are stored multiplied by 3 (so 1 full turn move = 3).
		/// </summary>
		public static int AdjustForRemainingSteps(int assault, int remainingSteps)
		{
			if (remainingSteps < 3)
			{
				return remainingSteps * assault / 3;
			}
			return assault;
		}

		/// <summary>
		/// Determines whether a city has active SDI Defense protecting it against nuclear strikes.
		/// In original Civ1: StructureMarkers1 &amp; 0x1.
		/// </summary>
		public static bool IsSDIProtected(ushort structureMarkers1)
		{
			return (structureMarkers1 & 1) != 0;
		}

		/// <summary>
		/// Computes the remaining population of a city after being struck by a nuclear blast.
		/// Halves population rounded up (i.e. loses size / 2).
		/// </summary>
		public static int CalculateNuclearCityPopulation(int currentSize)
		{
			return currentSize - (currentSize / 2);
		}

		/// <summary>
		/// Checks whether a tile hit by nuclear fallout receives a smog / pollution counter.
		/// Never attaches to ocean tiles (terrainType 10).
		/// In original Civ1: dice(3) != 0 (2 out of 3 chance).
		/// </summary>
		public static bool ShouldAttachFallout(int terrainType, int diceThreeOutcome)
		{
			return terrainType != 10 && diceThreeOutcome != 0;
		}
	}
}
