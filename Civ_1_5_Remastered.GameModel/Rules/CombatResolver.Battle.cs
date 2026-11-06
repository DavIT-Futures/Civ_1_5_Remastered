using System.Collections.Generic;

namespace Civ_1_5_Remastered.GameModel.Rules
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

			public int AssailantSecondChancesUsed { get; set; }

			public int GuardianSecondChancesUsed { get; set; }
		}
	}
}
