using System;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// Leader portraits added by the remaster: Casimir of Poland (KING14, in his royal robe) and Tokugawa of Japan
	/// (KING15); Casimir also dresses for the modern sceneries (KING16..KING19). They
	/// ship with the application in Assets/Pictures. The original game knows only the portraits 0..13. Its
	/// per-portrait tables (face position, disk, small head on the wall of fallen leaders) stop there, so the
	/// added ones answer here.
	/// </summary>
	public static class LeaderPortraits
	{
		public const int Casimir = 14;
		public const int Tokugawa = 15;
		public const int CasimirParadeUniform = 16;
		public const int CasimirFieldUniform = 17;
		public const int CasimirTunic = 18;
		public const int CasimirSuit = 19;
		public const int PolishLeader = 16;
		public const int JapaneseLeader = 17;

		public static bool IsAdded(int portrait)
		{
			return portrait >= Casimir && portrait <= CasimirSuit;
		}

		public static bool IsCasimir(int portrait)
		{
			return portrait == Casimir || (portrait >= CasimirParadeUniform && portrait <= CasimirSuit);
		}

		/// <summary>
		/// Casimir dressed to match the audience scenery, which follows the government and the era: a field uniform
		/// under modern despotism, a parade uniform under modern monarchy, a party tunic under communism, a suit
		/// under a modern republic or democracy, and his royal robe in the ancient sceneries.
		/// </summary>
		public static int ForScenery(int portrait, int regime, bool modern)
		{
			if (portrait != Casimir)
			{
				return portrait;
			}

			if (regime == 3)
			{
				return CasimirTunic;
			}

			if (!modern)
			{
				return Casimir;
			}

			return (regime / 2) switch
			{
				0 => CasimirFieldUniform,
				1 => CasimirParadeUniform,
				_ => CasimirSuit,
			};
		}

		/// <summary>The face (eyes at <c>Vertical - 2</c>, mouth at <c>Vertical + 18</c>) on the audience screen.</summary>
		public static (short Horizontal, short Vertical) Face(int portrait)
		{
			if (IsCasimir(portrait))
			{
				return (131, 16);
			}

			return portrait == Tokugawa ? ((short)123, (short)27) : throw new ArgumentOutOfRangeException(nameof(portrait));
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
