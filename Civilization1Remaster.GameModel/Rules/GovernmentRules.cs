using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure domain rules for government types, revolutions, tax rates, and diplomatic treaty evaluation.
	/// </summary>
	public static class GovernmentRules
	{
		public const short RegimeAnarchy = 0;
		public const short RegimeDespotism = 1;
		public const short RegimeMonarchy = 2;
		public const short RegimeCommunism = 3;
		public const short RegimeRepublic = 4;
		public const short RegimeDemocracy = 5;

		/// <summary>
		/// Returns the technology ID required to adopt a specific government regime.
		/// Regime 1 (Despotism): None (always available, returns -1)
		/// Regime 2 (Monarchy): Monarchy (tech 5)
		/// Regime 3 (Communism): Communism (tech 0x2b = 43)
		/// Regime 4 (Republic): The Republic (tech 0x2c = 44)
		/// Regime 5 (Democracy): Democracy (tech 4)
		/// </summary>
		public static short GetRequiredTechForGovernment(short regimeKind)
		{
			return regimeKind switch
			{
				RegimeMonarchy => 5,
				RegimeCommunism => 43,
				RegimeRepublic => 44,
				RegimeDemocracy => 4,
				_ => -1
			};
		}

		/// <summary>
		/// Checks whether a civilization is eligible to adopt a given government type.
		/// </summary>
		public static bool CanAdoptGovernment(short regimeKind, Func<short, bool> playerKnowsTech, bool hasPyramids)
		{
			if (regimeKind == RegimeDespotism)
				return true;

			if (regimeKind < RegimeDespotism || regimeKind > RegimeDemocracy)
				return false;

			if (hasPyramids)
				return true;

			short reqTech = GetRequiredTechForGovernment(regimeKind);
			if (reqTech < 0)
				return true;

			return playerKnowsTech(reqTech);
		}

		/// <summary>
		/// Computes the default tax/levy ratio for an AI or default government change:
		/// LevyRatio = 4 - (regimeKind / 2)
		/// </summary>
		public static short ComputeDefaultLevyRatio(short regimeKind)
		{
			return (short)(4 - (regimeKind / 2));
		}

		/// <summary>
		/// Pure calculation for whether AI player 1 evaluates and agrees to a peace treaty with player 2.
		/// Replicates the mathematical logic of CIV.EXE / GovernmentAndDiplomacy.AIWeighsTreaty.
		/// </summary>
		public static bool EvaluatePeaceTreaty(
			bool diagMarkerBit0,
			int humanPlayerIdx,
			short p1,
			short p2,
			ushort p1HumanRelation,
			ushort p2HumanRelation,
			int p1Might,
			int p2Might,
			int humanMight,
			short p1Nukes,
			short p2Nukes,
			int p1ChiefVisualDisplacement,
			Func<short, ushort> getP1RelationWithPlayer,
			Func<short, (int townTally, int guard, int assault)> getP1Landmass,
			Func<short, (int guard, int assault)> getP2Landmass)
		{
			if (!diagMarkerBit0)
			{
				return true;
			}

			// If p1 is at war with human (relations & 3 == 1) and p2's might < human might and p2 has no contact bit 4
			if ((p1HumanRelation & 3) == 1 && p2Might < humanMight && (p2HumanRelation & 4) == 0)
			{
				return false;
			}

			// If p2 is at war with human and p1's might < human might and p1 has no contact bit 4
			if ((p2HumanRelation & 3) == 1 && p1Might < humanMight && (p1HumanRelation & 4) == 0)
			{
				return false;
			}

			// Compare nukes (LiveTroops[25])
			if (p1Nukes < p2Nukes)
			{
				return false;
			}

			// Count wars with other players
			int warCount = 0;
			for (short other = 1; other < 8; other++)
			{
				if (other == p1 || other == p2)
					continue;

				if ((getP1RelationWithPlayer(other) & 3) == 1)
				{
					warCount++;
				}
			}

			// Add 1 if p1 has peace/contact flag 2 with p2
			if ((getP1RelationWithPlayer(p2) & 2) != 0)
			{
				warCount++;
			}

			// Adjust by leader aggressiveness / visual attribute
			warCount -= p1ChiefVisualDisplacement;

			int totalCities = 0;
			int totalP1Guard = 0;
			int totalP2DefenseScore = 1; // starts at 1

			for (short landmass = 1; landmass < 15; landmass++)
			{
				var p1Lm = getP1Landmass(landmass);
				if (p1Lm.townTally != 0)
				{
					var p2Lm = getP2Landmass(landmass);
					// If p1 guard/2 + p1 assault < p2 guard, AI refuses treaty
					if ((p1Lm.guard / 2) + p1Lm.assault < p2Lm.guard)
					{
						return false;
					}
				}

				if (p1Lm.guard != 0)
				{
					var p2Lm = getP2Landmass(landmass);
					if (p2Lm.assault != 0)
					{
						totalP1Guard += p1Lm.guard;
						totalP2DefenseScore += (p2Lm.guard / 2) + p2Lm.assault;
						totalCities += p1Lm.townTally;
					}
				}
			}

			// Ratio calculation: (totalP1Guard * 4) / totalP2DefenseScore
			int ratio = (totalP1Guard * 4) / totalP2DefenseScore;
			if (ratio >= warCount + 4)
			{
				return true;
			}

			return totalCities == 0;
		}
	}
}
