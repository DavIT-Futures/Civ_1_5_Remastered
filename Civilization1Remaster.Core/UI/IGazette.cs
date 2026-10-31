using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of Newspaper that the other parts of the game use</summary>
	public interface IGazette
	{
		/// <summary>
		/// The newspaper of the city (a negative city: the human player's palace city, for -1 else its first city): the
		/// title (one of four headlines with the city's name, " News" when too wide, ornaments when narrow), "10 cents", the
		/// date and a random story *NEWSA - *NEWSN of the text file 0x5178. Unless townIdx is -2 (drawn on the current screen,
		/// without waiting): a crowd of people under it, the screen kept and put back after a key.
		/// </summary>
		void ShowNewspaper(short townIdx);
	}
}
