namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>What one attacker knows about the siege of one enemy city.</summary>
	public readonly struct SiegeView
	{
		/// <summary>Tiles between the attacker and the city.</summary>
		public int Distance { get; init; }

		/// <summary>Defenders in the city.</summary>
		public int Defenders { get; init; }

		/// <summary>Own attackers standing next to the city.</summary>
		public int RingAttackers { get; init; }

		/// <summary>Expected number of defenders killed by the attackers next to the city.</summary>
		public double RingKills { get; init; }

		/// <summary>Expected number of defenders killed by every own attacker near the city (the ring included).</summary>
		public double ForceKills { get; init; }

		/// <summary>Free land tiles next to the city (a coastal city has fewer).</summary>
		public int RingTiles { get; init; }

		/// <summary>Turns since the first attacker reached the city.</summary>
		public int RoundsWaiting { get; init; }

		/// <summary>The attackers around this city already started the assault this turn.</summary>
		public bool AssaultUnderway { get; init; }
	}
}
