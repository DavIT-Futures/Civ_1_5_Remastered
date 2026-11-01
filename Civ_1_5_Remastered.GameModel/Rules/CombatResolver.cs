using System;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static partial class CombatResolver
	{
		public const int RegularStrikeMarks = 3;

		public const int SeasonedStrikeMarks = 4;

		public const int AceStrikeMarks = 5;

		public const int PeakStrikeMarks = AceStrikeMarks;

		public static int TopHitPoints(bool seasoned, bool ace)
		{
			return ace ? AceStrikeMarks : (seasoned ? SeasonedStrikeMarks : RegularStrikeMarks);
		}

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
							{
								guardianRemaining = fight.GuardianBeginStrikeMarks;
							}
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
	}
}
