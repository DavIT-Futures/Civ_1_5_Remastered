namespace Civilization1Remaster.GameModel.Rules
{
	public static class TechnologyNewsRules
	{
		public const int GreatLibrary = -2;

		public static bool NamesSource(int participantIdx, int sourceIdx) => participantIdx != sourceIdx && sourceIdx != 0;
	}
}
