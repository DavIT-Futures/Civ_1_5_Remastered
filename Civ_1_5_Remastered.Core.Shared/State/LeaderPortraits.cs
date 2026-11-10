using System;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// Leader portraits added by the remaster: Casimir of Poland, dressed for each audience scenery (KING14 and
	/// KING16..KING21), and Tokugawa of Japan (KING15). They ship with the application in Assets/Pictures. The
	/// original game knows only the portraits 0..13. Its per-portrait tables (face position, disk, small head on
	/// the wall of fallen leaders) stop there, so the added ones answer here.
	/// </summary>
	public static class LeaderPortraits
	{
		/// <summary>Casimir crowned, in a royal robe: ancient monarchy, and wherever no scenery is known.</summary>
		public const int Casimir = 14;
		public const int Tokugawa = 15;
		public const int CasimirSuit = 16;
		public const int CasimirFieldUniform = 17;
		public const int CasimirTunic = 18;
		public const int CasimirParadeUniform = 19;
		public const int CasimirWarlord = 20;
		public const int CasimirSenator = 21;
		public const int PolishLeader = 16;
		public const int JapaneseLeader = 17;

		public static bool IsAdded(int portrait)
		{
			return portrait >= Casimir && portrait <= CasimirSenator;
		}

		public static bool IsCasimir(int portrait)
		{
			return IsAdded(portrait) && portrait != Tokugawa;
		}

		/// <summary>
		/// The portrait dressed for the audience scenery, which follows the government (regime) and the era.
		/// Casimir: a party tunic under communism (regime 3, one scenery for every era); under anarchy or
		/// despotism (0, 1) a warlord's helmet and furs, later a field uniform; under monarchy (2) a crown and royal
		/// robe, later a parade uniform; under a republic or democracy (4, 5) a senator's toga, later a suit.
		/// </summary>
		public static int ForScenery(int portrait, int regime, bool modern)
		{
			if (portrait != Casimir)
			{
				return portrait;
			}

			return regime switch
			{
				3 => CasimirTunic,
				0 or 1 => modern ? CasimirFieldUniform : CasimirWarlord,
				4 or 5 => modern ? CasimirSuit : CasimirSenator,
				_ => modern ? CasimirParadeUniform : Casimir,
			};
		}

		/// <summary>The face (eyes at <c>Vertical - 2</c>, mouth at <c>Vertical + 18</c>) on the audience screen.</summary>
		public static (short Horizontal, short Vertical) Face(int portrait)
		{
			return portrait switch
			{
				Tokugawa => (123, 27),
				CasimirSuit => (123, 24),
				CasimirFieldUniform or CasimirTunic => (123, 28),
				_ when IsCasimir(portrait) => (123, 27),
				_ => throw new ArgumentOutOfRangeException(nameof(portrait)),
			};
		}

		/// <summary>The original disk holding the portraits; an added one asks for the last of them (always present).</summary>
		public static ushort Disk(int portrait)
		{
			return (ushort)((Math.Min(portrait, 13) / 7) + 4);
		}

		/// <summary>An original portrait standing in where only the originals have pictures (the wall of fallen leaders).</summary>
		public static int StandIn(int portrait)
		{
			if (IsCasimir(portrait))
			{
				return 2;
			}

			return portrait == Tokugawa ? 10 : portrait;
		}
	}
}
