using System.Text.RegularExpressions;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class WonderRules
	{
		public const int FirstWonderImprovement = 0x19;

		public const int MichelangelosChapel = 0x21;

		private const string ChapelEffect = "Increases the effect of CATHEDRALS.";

		private const string ChapelEffectInFull = "Increases the effect of all your CATHEDRALS by 50%: each Cathedral makes 6 unhappy citizens content instead of 4.";

		private static readonly Regex LastingSentence = new Regex(@"\s*Both of these effects\s+last until the\s+development of [^.]*\.");
		private static readonly Regex UntilClause = new Regex(@",\s*until the\s+development of [^.]*\.");

		public static bool IsWonder(int improvementIndex) => improvementIndex >= FirstWonderImprovement;

		public static string WithoutObsolescence(string effect)
		{
			return UntilClause.Replace(LastingSentence.Replace(effect, string.Empty), ".");
		}

		public static string CivilopediaEffect(int improvementIndex, string effect)
		{
			string lasting = WithoutObsolescence(effect);

			return improvementIndex == MichelangelosChapel ? lasting.Replace(ChapelEffect, ChapelEffectInFull) : lasting;
		}
	}
}
