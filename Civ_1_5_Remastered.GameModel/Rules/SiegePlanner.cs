using System;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Remastered sieges of the computer civilizations. The original computer sent its attackers to an enemy city one
	/// by one, each attacking as soon as it arrived. Now attackers near an enemy city first gather on the tiles around
	/// it, spreading to every side, and attack together once enough of them stand there. When even all of them cannot
	/// take the city, they pillage the land around it instead.
	/// </summary>
	public static class SiegePlanner
	{
		/// <summary>Attackers this close to an enemy city take part in its siege.</summary>
		public const int GatherRange = 5;

		/// <summary>The fewest attackers that start an assault (unless the ring has fewer free tiles).</summary>
		public const int MinimumRing = 3;

		/// <summary>The most attackers a siege waits for.</summary>
		public const int MaximumRing = 8;

		/// <summary>After this many turns around the city the attackers stop waiting for more.</summary>
		public const int PatienceRounds = 5;

		/// <summary>The ring attacks when it expects to kill this share of the defenders.</summary>
		public const double AssaultShare = 1.0;

		/// <summary>After waiting, the ring attacks when it expects to kill this share of the defenders.</summary>
		public const double LateAssaultShare = 0.6;

		/// <summary>The siege is hopeless when all attackers together expect to kill less than this share.</summary>
		public const double HopelessShare = 0.5;

		/// <summary>How many attackers a siege waits for: two more than the defenders, within the limits.</summary>
		public static int WantedRing(int defenders, int ringTiles)
		{
			int wanted = Math.Clamp(defenders + 2, MinimumRing, MaximumRing);

			return Math.Max(1, Math.Min(wanted, Math.Max(1, ringTiles)));
		}

		public static SiegeOrder Decide(SiegeView view)
		{
			if (view.Distance > GatherRange)
			{
				return SiegeOrder.None;
			}

			if (view.AssaultUnderway || view.Defenders == 0)
			{
				return view.Distance <= 1 ? SiegeOrder.Assault : SiegeOrder.Advance;
			}

			if (view.ForceKills < view.Defenders * HopelessShare)
			{
				return SiegeOrder.Raid;
			}

			if (view.Distance > 1)
			{
				return SiegeOrder.Advance;
			}

			if (view.RingAttackers >= WantedRing(view.Defenders, view.RingTiles) && view.RingKills >= view.Defenders * AssaultShare)
			{
				return SiegeOrder.Assault;
			}

			if (view.RoundsWaiting >= PatienceRounds)
			{
				return view.RingKills >= view.Defenders * LateAssaultShare ? SiegeOrder.Assault : SiegeOrder.Raid;
			}

			return SiegeOrder.Hold;
		}

		/// <summary>
		/// The chance that an attacker wins a battle fought in rounds: each round it wins with
		/// <c>assault / (assault + guard)</c>, and the battle ends when one side has lost all its hit points.
		/// </summary>
		public static double WinChance(int assault, int guard, int assailantHitPoints, int guardianHitPoints)
		{
			if (assault <= 0 || assailantHitPoints <= 0)
			{
				return 0;
			}

			if (guard <= 0 || guardianHitPoints <= 0)
			{
				return 1;
			}

			double round = (double)assault / (assault + guard);

			// chance[lost, won]: the chance of reaching a state where the attacker lost and won so many rounds.
			double[,] chance = new double[assailantHitPoints + 1, guardianHitPoints + 1];
			chance[0, 0] = 1;
			double victory = 0;

			for (int lost = 0; lost < assailantHitPoints; lost++)
			{
				for (int won = 0; won < guardianHitPoints; won++)
				{
					double here = chance[lost, won];

					if (here == 0)
					{
						continue;
					}

					if (won + 1 == guardianHitPoints)
					{
						victory += here * round;
					}
					else
					{
						chance[lost, won + 1] += here * round;
					}

					if (lost + 1 < assailantHitPoints)
					{
						chance[lost + 1, won] += here * (1 - round);
					}
				}
			}

			return victory;
		}
	}
}
