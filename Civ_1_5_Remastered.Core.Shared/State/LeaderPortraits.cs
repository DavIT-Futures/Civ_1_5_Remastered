using System;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// Leader portraits added by the remaster: Casimir of Poland (KING14, royal robe; KING16, suit for the modern
	/// republic and democracy sceneries; KING17, field uniform for modern despotism and anarchy) and Tokugawa of Japan (KING15). They
	/// ship with the application in Assets/Pictures. The original game knows only the portraits 0..13. Its
	/// per-portrait tables (face position, disk, small head on the wall of fallen leaders) stop there, so the
	/// added ones answer here.
	/// </summary>
	public static class LeaderPortraits
	{
		public const int Casimir = 14;
		public const int Tokugawa = 15;
		public const int CasimirSuit = 16;
		public const int CasimirFieldUniform = 17;
		public const int PolishLeader = 16;
		public const int JapaneseLeader = 17;

		public static bool IsAdded(int portrait)
		{
			return portrait >= Casimir && portrait <= CasimirFieldUniform;
		}

		/// <summary>
		/// The portrait dressed for the audience scenery, which follows the government and the era: Casimir wears
		/// a field uniform under modern anarchy or despotism (regimes 0 and 1), a suit under a modern republic or
		/// democracy (regimes 4 and 5) and his royal robe elsewhere.
		/// </summary>
		public static int ForScenery(int portrait, int regime, bool modern)
		{
			if (portrait != Casimir || !modern)
			{
				return portrait;
			}

			return regime switch
			{
				0 or 1 => CasimirFieldUniform,
				4 or 5 => CasimirSuit,
				_ => portrait,
			};
		}

		/// <summary>The face (eyes at <c>Vertical - 2</c>, mouth at <c>Vertical + 18</c>) on the audience screen.</summary>
		public static (short Horizontal, short Vertical) Face(int portrait)
		{
			return portrait switch
			{
				Casimir => (131, 16),
				Tokugawa => (123, 27),
				CasimirSuit => (123, 24),
				CasimirFieldUniform => (123, 28),
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
			return portrait switch
			{
				Casimir or CasimirSuit or CasimirFieldUniform => 2,
				Tokugawa => 10,
				_ => portrait,
			};
		}
	}
}
