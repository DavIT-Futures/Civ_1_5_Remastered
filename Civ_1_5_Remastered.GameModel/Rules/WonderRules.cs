using System.Text.RegularExpressions;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class WonderRules
	{
		public const int FirstWonderImprovement = 0x19;

		private static readonly Regex LastingSentence = new Regex(@"\s*Both of these effects\s+last until the\s+development of [^.]*\.");
		private static readonly Regex UntilClause = new Regex(@",\s*until the\s+development of [^.]*\.");

		public static bool IsWonder(int improvementIndex) => improvementIndex >= FirstWonderImprovement;

		public static string WithoutObsolescence(string effect)
		{
			return UntilClause.Replace(LastingSentence.Replace(effect, string.Empty), ".");
		}
	}
}
