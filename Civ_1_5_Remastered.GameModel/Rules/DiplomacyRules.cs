using System;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Remastered diplomacy memory: vendetta (grudge) countdown, peace duration counter,
	/// reparations and the attitude estimate shown by the War Advisor.
	/// </summary>
	/// <remarks>
	/// The original save format keeps one 16-bit relation word per ordered pair of civilizations
	/// and only uses bits 0x0001-0x0200. The remastered counters live in the six spare high bits,
	/// so saves stay compatible with the original layout:
	/// <list type="bullet">
	/// <item>bits 10-13 of Relations[x][y]: how long x still holds a vendetta against y (units of 4 turns);</item>
	/// <item>bits 14-15: two bits of the shared peace counter of the pair (low half in the word of the lower
	/// index, high half in the word of the higher index), also in units of 4 turns.</item>
	/// </list>
	/// </remarks>
	public static class DiplomacyRules
	{
		public const ushort ContactRelation = 0x0001;
		public const ushort PeaceRelation = 0x0002;
		public const ushort VendettaRelation = 0x0008;
		public const ushort RecentCombatRelation = 0x0020;
		public const ushort EmbassyRelation = 0x0040;
		public const ushort BreakPeacePlanRelation = 0x0100;
		public const ushort SneakAttackRelation = 0x0200;

		public const int TurnsPerUnit = 4;
		public const int MaximumUnits = 15;

		public const int SneakAttackGrudge = 12;
		public const int InciteRevoltGrudge = 12;
		public const int AllianceTargetGrudge = 6;
		public const int CivilWarGrudge = 15;
		public const int LegacyGrudge = 12;

		public const int MinimumTreatyUnits = 4;
		public const int TrustedPeaceUnits = 8;

		public const int ReparationsPerUnit = 100;
		public const int MinimumReparations = 300;
		public const int MaximumReparations = 3000;

		private const int GrudgeShift = 10;
		private const ushort GrudgeMask = 0x3c00;
		private const int PeaceShift = 14;
		private const ushort PeaceMask = 0xc000;

		public static int GrudgeUnits(ushort relation)
		{
			if ((relation & VendettaRelation) == 0)
			{
				return 0;
			}

			int stored = (relation & GrudgeMask) >> GrudgeShift;

			return stored == 0 ? LegacyGrudge : stored;
		}

		public static int GrudgeTurns(ushort relation) => GrudgeUnits(relation) * TurnsPerUnit;

		public static ushort WithGrudgeUnits(ushort relation, int units)
		{
			units = Math.Clamp(units, 0, MaximumUnits);

			relation = (ushort)(relation & ~GrudgeMask);

			if (units == 0)
			{
				return (ushort)(relation & ~VendettaRelation);
			}

			return (ushort)(relation | VendettaRelation | (units << GrudgeShift));
		}

		/// <summary>Starts (or deepens, for repeat offences) a vendetta of <paramref name="holder"/> against <paramref name="target"/>.</summary>
		public static void BeginVendetta(Player[] participants, int holder, int target, int severityUnits)
		{
			ushort relation = participants[holder].Relations[target];

			participants[holder].Relations[target] = WithGrudgeUnits(relation, ProjectedVendettaUnits(relation, severityUnits));
		}

		/// <summary>Length of the vendetta that a new offence would cause; repeat offenders get a longer one.</summary>
		public static int ProjectedVendettaUnits(ushort relation, int severityUnits)
		{
			int current = GrudgeUnits(relation);
			int units = current == 0 ? severityUnits : Math.Max(current, severityUnits) + (severityUnits / 2);

			return Math.Clamp(units, 1, MaximumUnits);
		}

		public static void EndVendetta(Player[] participants, int holder, int target)
		{
			participants[holder].Relations[target] = WithGrudgeUnits(participants[holder].Relations[target], 0);
		}

		/// <summary>Halves the remaining vendetta, e.g. after tribute was paid.</summary>
		public static void SoftenVendetta(Player[] participants, int holder, int target)
		{
			ushort relation = participants[holder].Relations[target];

			if ((relation & VendettaRelation) != 0)
			{
				participants[holder].Relations[target] = WithGrudgeUnits(relation, GrudgeUnits(relation) / 2);
			}
		}

		public static int PeaceUnits(Player[] participants, int first, int second)
		{
			int low = Math.Min(first, second);
			int high = Math.Max(first, second);

			int lowBits = (participants[low].Relations[high] & PeaceMask) >> PeaceShift;
			int highBits = (participants[high].Relations[low] & PeaceMask) >> PeaceShift;

			return lowBits | (highBits << 2);
		}

		public static int PeaceTurns(Player[] participants, int first, int second) => PeaceUnits(participants, first, second) * TurnsPerUnit;

		public static void SetPeaceUnits(Player[] participants, int first, int second, int units)
		{
			units = Math.Clamp(units, 0, MaximumUnits);

			int low = Math.Min(first, second);
			int high = Math.Max(first, second);

			participants[low].Relations[high] = (ushort)((participants[low].Relations[high] & ~PeaceMask) | ((units & 3) << PeaceShift));
			participants[high].Relations[low] = (ushort)((participants[high].Relations[low] & ~PeaceMask) | ((units >> 2) << PeaceShift));
		}

		public static bool AtPeace(Player[] participants, int first, int second) =>
			(participants[first].Relations[second] & PeaceRelation) != 0 && (participants[second].Relations[first] & PeaceRelation) != 0;

		public static bool FoughtRecently(Player[] participants, int first, int second) =>
			((participants[first].Relations[second] | participants[second].Relations[first]) & RecentCombatRelation) != 0;

		/// <summary>
		/// Advances the counters of one ordered pair. Called once per turn for every pair;
		/// counters only move on turns divisible by <see cref="TurnsPerUnit"/>.
		/// </summary>
		/// <returns><c>true</c> when the vendetta of <paramref name="holder"/> against <paramref name="target"/> expired on this call.</returns>
		public static bool AdvanceCounters(Player[] participants, int holder, int target, int roundTally)
		{
			if (holder == target || (roundTally % TurnsPerUnit) != 0)
			{
				return false;
			}

			if (holder < target)
			{
				int peace = PeaceUnits(participants, holder, target);
				SetPeaceUnits(participants, holder, target, AtPeace(participants, holder, target) ? peace + 1 : 0);
			}

			ushort relation = participants[holder].Relations[target];

			if ((relation & VendettaRelation) == 0)
			{
				return false;
			}

			bool fought = (relation & RecentCombatRelation) != 0;
			bool atPeace = AtPeace(participants, holder, target);

			if (fought || (!atPeace && (roundTally % (2 * TurnsPerUnit)) != 0))
			{
				participants[holder].Relations[target] = WithGrudgeUnits(relation, GrudgeUnits(relation));
				return false;
			}

			int units = GrudgeUnits(relation) - 1;
			participants[holder].Relations[target] = WithGrudgeUnits(relation, units);

			return units <= 0;
		}

		/// <summary>
		/// Whether the Senate of a Republic or Democracy lets the player break a treaty: only when the other
		/// civilization holds a vendetta against us, or when our embassy has exposed its plan to break the peace.
		/// </summary>
		/// <param name="theirRelation">Their relation word towards us.</param>
		/// <param name="embassy">Whether we have an embassy with them.</param>
		public static bool SenateConsents(ushort theirRelation, bool embassy) =>
			(theirRelation & VendettaRelation) != 0 ||
			(embassy && (theirRelation & (BreakPeacePlanRelation | SneakAttackRelation)) != 0);

		/// <summary>Gold demanded to forget a vendetta. Deliberately expensive to discourage sneak attacks.</summary>
		public static int ComputeReparations(int grudgeUnits, int hardnessTier)
		{
			if (grudgeUnits <= 0)
			{
				return 0;
			}

			int gold = grudgeUnits * ReparationsPerUnit * (Math.Clamp(hardnessTier, 0, 4) + 2) / 3;

			return Math.Clamp(gold / 50 * 50, MinimumReparations, MaximumReparations);
		}

		/// <summary>
		/// How far a long peace eases the envy rulers feel for the world leader without nuclear weapons:
		/// 0 none, 1 (32+ turns) a good attitude can outweigh it, 2 (60+ turns) the envy is gone.
		/// </summary>
		public static int EnvyEase(bool atPeace, int peaceUnits)
		{
			if (!atPeace)
			{
				return 0;
			}

			if (peaceUnits >= MaximumUnits)
			{
				return 2;
			}

			return peaceUnits >= TrustedPeaceUnits ? 1 : 0;
		}

		public static int PeaceAttitudeBonus(int peaceUnits)
		{
			if (peaceUnits >= MaximumUnits)
			{
				return 2;
			}

			return peaceUnits >= TrustedPeaceUnits ? 1 : 0;
		}

		/// <summary>
		/// Decides whether a civilization that planned to break the peace (bit 0x100) actually goes ahead.
		/// </summary>
		/// <returns>1 to break the peace now (subject to the dice), 0 to wait, -1 to abandon the plan.</returns>
		public static int WeighBreakingPeace(int peaceUnits, int ownMight, int targetMight, bool vendetta)
		{
			if (!vendetta && 2 * targetMight >= 3 * ownMight)
			{
				return -1;
			}

			return peaceUnits < MinimumTreatyUnits ? 0 : 1;
		}

		/// <summary>
		/// Estimates the attitude of <paramref name="holder"/> towards <paramref name="target"/> with the same ingredients
		/// the ruler audience uses (vendetta, wars with strong rivals, leader aggression, relative might, length of peace).
		/// </summary>
		public static int ComputeAttitude(Player[] participants, int holder, int target, int aggression)
		{
			Player leader = participants[holder];
			ushort relation = leader.Relations[target];
			int attitude = (relation & VendettaRelation) != 0 ? -2 : 0;

			for (int other = 1; other < participants.Length; other++)
			{
				if (other == target || other == holder || (leader.Relations[other] & 3) != 1)
				{
					continue;
				}

				if (leader.MartialMight < participants[other].MartialMight * 4)
				{
					attitude++;
				}

				if (participants[other].MartialMight > leader.MartialMight)
				{
					attitude++;
				}
			}

			attitude -= aggression;

			if (participants[target].MartialMight < leader.MartialMight)
			{
				attitude--;
			}

			if (AtPeace(participants, holder, target))
			{
				attitude += PeaceAttitudeBonus(PeaceUnits(participants, holder, target));
			}

			return attitude;
		}

		public static string AttitudeName(int attitude, bool vendetta)
		{
			if (vendetta)
			{
				return "Vengeful";
			}

			if (attitude >= 2)
			{
				return "Friendly";
			}

			if (attitude >= 0)
			{
				return "Neutral";
			}

			return attitude == -1 ? "Wary" : "Hostile";
		}
	}
}
