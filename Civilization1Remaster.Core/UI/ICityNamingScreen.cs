
namespace Civilization1Remaster.Core.UI
{
	/// <summary>The operations of CityNamingScreen that the other parts of the game use</summary>
	public interface ICityNamingScreen
	{
		/// <summary>
		/// Asks for the name of the city (question 0x539e, starting from its name) until the answer is not empty; when the
		/// answer is accepted (PullTextInput not 0) its 13 characters become the city's name. The screen is kept in the back
		/// screen 0x19d4 meanwhile.
		/// </summary>
		/// <returns>The answer of PullTextInput, also in AX</returns>
		ushort ShowCityLabelPrompt(short townIdx);

		/// <summary>
		/// Asks the human player for a name in a box with the question at 0x53ab: the answer starts as the text that
		/// 0x19a2 + 2 * player points to, and is written back there
		/// </summary>
		void ShowFoundCityMessage();

		/// <summary>
		/// Asks the human player for the name of their civilization in a box with the question at 0x53b8: the plural is typed
		/// (the text that 0x1992 + 2 * player points to); without its final "s" and cut to 10 characters it is also the name
		/// (0x1982 + 2 * player)
		/// </summary>
		void ShowCityFoundedMessage();

		/// <summary>
		/// Asks for the name of a city (0x53ce) and centres the map on it when the human player knows its tile (ChartSight);
		/// otherwise (or when no city is chosen) shows the message 0x53f3 until a key or click. The screen is kept in the back
		/// screen 0x19d4 meanwhile.
		/// </summary>
		void ShowNewCityLocation();

		/// <summary>
		/// Asks for a city name (the question at charsAddr) and looks for it: the first of the 256 city names that starts with
		/// the answer (any case), and the first city with that name; names without a city are skipped. Each name is copied into
		/// a buffer in the stack frame for the comparison, like in the original.
		/// </summary>
		/// <returns>City index, 0xffff when none (also in AX)</returns>
		ushort ShowLabelInputBox(ushort charsAddr);

		/// <summary>
		/// Edits the text at TextBuffer.Address in a field of breadth characters (padded with spaces): the cursor (0x68cc) blinks until a
		/// key or a click; Left / Right move it, Insert and Delete shift the rest, Backspace deletes before it, a character
		/// overwrites (the first one typed clears the field). Return or a click accepts, Escape cancels; the trailing spaces
		/// are cut off.
		/// </summary>
		/// <returns>1 when accepted, 0 when cancelled, also in AX</returns>
		ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth);

		/// <summary>
		/// Draws the text of an input field (TextBuffer.Address) character by character on white boxes of 8 x 8, the field the width in
		/// characters; at the cursor (0x68cc) the column and the width of its character go to 0x68d0 and 0x68ce
		/// </summary>
		void DrawTextInputField(short horizLoc, short vertLoc, ushort breadth);
	}
}
