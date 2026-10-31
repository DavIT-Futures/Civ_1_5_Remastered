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

	}
}
