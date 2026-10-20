using System;
using System.Collections.Generic;

namespace Civilization1Original.Game.Rules
{
	public static partial class CombatResolver
	{
		/// <summary>
		/// Result of one battle
		/// </summary>
		public class Battle
		{
			public bool AssailantVictories;

			public int AssailantBeginStrikeMarks;
			public int GuardianBeginStrikeMarks;

			/// <summary>
			/// Hit points left after the battle, the loser has 0
			/// </summary>
			public int AssailantStrikeMarks;
			public int GuardianStrikeMarks;

			/// <summary>
			/// Winner of each round, true when the attacker won the round
			/// </summary>
			public List<bool> Phases = new List<bool>();

			/// <summary>
			/// Number of times the defender has to be beaten (barbarians have to win twice against a city)
			/// </summary>
			public int GuardianChances = 1;
		}
	}
}
