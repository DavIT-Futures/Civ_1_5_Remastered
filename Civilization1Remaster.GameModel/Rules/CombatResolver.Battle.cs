using System.Collections.Generic;

namespace Civilization1Remaster.GameModel.Rules
{
	public static partial class CombatResolver
	{
		public class Battle
		{
			public bool AssailantVictories { get; set; }
			public int AssailantBeginStrikeMarks { get; set; }
			public int GuardianBeginStrikeMarks { get; set; }

			public int AssailantStrikeMarks { get; set; }

			public int GuardianStrikeMarks { get; set; }

			public List<bool> Phases { get; set; } = new List<bool>();

			public int GuardianChances { get; set; } = 1;
		}
	}
}
