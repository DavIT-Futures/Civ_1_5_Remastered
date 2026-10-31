using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Game.Rules
{
	public static partial class CombatResolver
	{
		/// <summary>
		/// Result of one battle
		/// </summary>
		public class Battle
		{
			public bool AssailantVictories { get; set; }

			public int AssailantBeginStrikeMarks { get; set; }
			public int GuardianBeginStrikeMarks { get; set; }

			/// <summary>
			/// Hit points left after the battle, the loser has 0
			/// </summary>
			public int AssailantStrikeMarks { get; set; }
			public int GuardianStrikeMarks { get; set; }

			/// <summary>
			/// Winner of each round, true when the attacker won the round
			/// </summary>
			public List<bool> Phases { get; set; } = new List<bool>();

			/// <summary>
			/// Number of times the defender has to be beaten (barbarians have to win twice against a city)
			/// </summary>
			public int GuardianChances { get; set; } = 1;
		}
	}
}
