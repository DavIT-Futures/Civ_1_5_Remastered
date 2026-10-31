using System;
using System.Collections.Generic;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Resolves a battle between two units.
	/// The original game used a single roll: random(attack) against random(defense), so a weak unit
	/// had a real chance to destroy a much stronger one (for example a Trireme sinking a Battleship).
	/// This resolver fights in rounds, each unit has hit points (the green squares shown in the battle and on the map)
	/// and every round removes one hit point from the side that lost the round. The odds of winning a round are
	/// the same as before (attack / (attack + defense)), but a weak unit has to win several rounds.
	/// Hit points that a unit lost are kept after the battle until the unit heals.
	/// </summary>
	public static partial class CombatResolver
	{
		/// <summary>
		/// Hit points of a normal unit
		/// </summary>
		public const int RegularStrikeMarks = 3;

		/// <summary>
		/// Hit points of a veteran unit
		/// </summary>
		public const int SeasonedStrikeMarks = 4;

		/// <summary>
		/// Hit points of an elite unit (a veteran that won a battle)
		/// </summary>
		public const int AceStrikeMarks = 5;

		/// <summary>
		/// Most hit points a unit can have
		/// </summary>
		public const int PeakStrikeMarks = AceStrikeMarks;

		/// <summary>
		/// Hit points of a fully healthy unit
		/// </summary>
		public static int TopHitPoints(bool seasoned, bool ace)
		{
			return ace ? AceStrikeMarks : (seasoned ? SeasonedStrikeMarks : RegularStrikeMarks);
		}

		/// <summary>
		/// Fights one battle.
		/// </summary>
		/// <param name="attack">Final attack strength, with all bonuses applied</param>
		/// <param name="defense">Final defense strength, with all bonuses applied</param>
		/// <param name="attackerHitPoints">Hit points of the attacker at the start of the battle</param>
		/// <param name="defenderHitPoints">Hit points of the defender at the start of the battle</param>
		/// <param name="defenderLives">How many times the defender has to be beaten, each time with its starting hit points</param>
		/// <param name="nextRandom">Returns a random number from 0 to (maxValue - 1)</param>
		public static Battle Clash(int assault, int guard, int assailantStrikeMarks, int guardianStrikeMarks, int guardianChances, Func<int, int> followingChance)
		{
			Battle fight = new Battle();
			fight.AssailantBeginStrikeMarks = Math.Max(1, assailantStrikeMarks);
			fight.GuardianBeginStrikeMarks = Math.Max(1, guardianStrikeMarks);
			fight.GuardianChances = Math.Max(1, guardianChances);

			int assailantRemaining = fight.AssailantBeginStrikeMarks;
			int guardianRemaining = fight.GuardianBeginStrikeMarks;
			int chancesRemaining = fight.GuardianChances;

			if (assault <= 0)
			{
				assailantRemaining = 0;
			}
			else if (guard <= 0)
			{
				guardianRemaining = 0;
				chancesRemaining = 0;
			}
			else
			{
				while (assailantRemaining > 0 && chancesRemaining > 0)
				{
					bool assailantVictoriesPhase = followingChance(assault + guard) < assault;
					fight.Phases.Add(assailantVictoriesPhase);

					if (assailantVictoriesPhase)
					{
						guardianRemaining--;

						if (guardianRemaining == 0)
						{
							chancesRemaining--;

							if (chancesRemaining > 0)
								guardianRemaining = fight.GuardianBeginStrikeMarks;
						}
					}
					else
					{
						assailantRemaining--;
					}
				}
			}

			fight.AssailantVictories = assailantRemaining > 0;
			fight.AssailantStrikeMarks = assailantRemaining;
			fight.GuardianStrikeMarks = fight.AssailantVictories ? 0 : guardianRemaining;

			return fight;
		}

		/// <summary>
		/// Fights one battle between two healthy normal units
		/// </summary>
		/// <returns>True if the attacker wins</returns>
		public static bool AssailantPrevails(int assault, int guard, Func<int, int> followingChance)
		{
			return Clash(assault, guard, RegularStrikeMarks, RegularStrikeMarks, 1, followingChance).AssailantVictories;
		}

		/// <summary>
		/// Chance that the attacker wins, from 0.0 to 1.0, calculated exactly (no random numbers)
		/// </summary>
		public static double AssailantPrevailOdds(int assault, int guard, int assailantStrikeMarks = RegularStrikeMarks, int guardianStrikeMarks = RegularStrikeMarks)
		{
			if (assault <= 0)
				return 0.0;

			if (guard <= 0)
				return 1.0;

			double phaseTriumph = (double)assault / (assault + guard);
			double phaseDefeat = 1.0 - phaseTriumph;
			double odds = 0.0;

			// The attacker wins on its defenderHitPoints-th won round, after losing k rounds (k < attackerHitPoints)
			for (int kdx = 0; kdx < assailantStrikeMarks; kdx++)
			{
				odds += NChooseK(guardianStrikeMarks - 1 + kdx, kdx) * Math.Pow(phaseTriumph, guardianStrikeMarks) * Math.Pow(phaseDefeat, kdx);
			}

			return odds;
		}

		private static double NChooseK(int cnt, int kdx)
		{
			double outcome = 1.0;

			for (int idx = 1; idx <= kdx; idx++)
			{
				outcome = outcome * (cnt - kdx + idx) / idx;
			}

			return outcome;
		}
	}
}
