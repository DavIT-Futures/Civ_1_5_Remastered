using System;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// Leader portraits added by the remaster: Casimir of Poland (KING14) and Tokugawa of Japan (KING15). They
	/// ship with the application in Assets/Pictures. The original game knows only the portraits 0..13. Its
	/// per-portrait tables (face position, disk, small head on the wall of fallen leaders) stop there, so the
	/// added ones answer here.
	/// </summary>
	public static class LeaderPortraits
	{
		public const int Casimir = 14;
		public const int Tokugawa = 15;
		public const int PolishLeader = 16;
		public const int JapaneseLeader = 17;

		public static bool IsAdded(int portrait)
		{
			return portrait == Casimir || portrait == Tokugawa;
		}

		/// <summary>The face (eyes at <c>Vertical - 2</c>, mouth at <c>Vertical + 18</c>) on the audience screen.</summary>
		public static (short Horizontal, short Vertical) Face(int portrait)
		{
			return portrait switch
			{
				Casimir => (131, 16),
				Tokugawa => (123, 27),
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
				Casimir => 2,
				Tokugawa => 10,
				_ => portrait,
			};
		}
	}
}
