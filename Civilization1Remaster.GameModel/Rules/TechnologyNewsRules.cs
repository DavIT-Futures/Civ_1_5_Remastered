namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// The news of a new technology: "Babylonians discover Mass Transit!" for the player's own research, "Babylonians
	/// acquire Mass Transit from the Romans!" (or "from the Great Library") when it came from somewhere else.
	/// </summary>
	public static class TechnologyNewsRules
	{
		/// <summary>The source of a technology from the Great Library (EarnTechnology)</summary>
		public const int GreatLibrary = -2;

		/// <summary>
		/// Whether the news names where the technology came from: not for the player's own research, whose source is the
		/// player or 0 (UncoverTechnology, the scrolls of a hut)
		/// </summary>
		public static bool NamesSource(int participantIdx, int sourceIdx) => participantIdx != sourceIdx && sourceIdx != 0;
	}
}
