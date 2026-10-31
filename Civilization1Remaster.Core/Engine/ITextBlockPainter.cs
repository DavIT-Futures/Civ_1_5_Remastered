using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>The operations of TextBlockPainter that the other parts of the game use</summary>
	public interface ITextBlockPainter
	{
		/// <summary>
		/// Breaks the text at TextBuffer.Address into lines of the width (in characters): a space after the width becomes a line break
		/// (unless a space follows), "^" becomes a space with a line break after it (over the next character); a line break
		/// is added at the end of a text that does not end with one
		/// </summary>
		void ArrangeTextRow(short breadth);

		/// <summary>
		/// Draws the text at TextBuffer.Address in lines of at most peakBreadth * 8 pixels, from vertLoc down while the lines start at most at
		/// row 192: a line ends at the last space, line break or "^" before it gets too wide (or at a line break or "^", which also
		/// skips the character after a "^"). Returns the row below the last drawn line.
		/// </summary>
		int RenderTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style);

		/// <summary>
		/// Appends the text of the topic charsAddr from the text file (lines after the "*TOPIC" line up to the next "*" line,
		/// joined with spaces, without trailing spaces and '^') to TextBuffer.Address. The file starts with an index of 256 words, by a
		/// hash of the topic (the low byte of the sum of character * position): a word above 0x1ff (not "00") is the offset of the
		/// topic line, a wrong topic tries the next hash. Without an index entry, the topic is searched from 0x200 and its offset
		/// written into the index.
		/// </summary>
		/// <returns>The number of lines of the text, 0xffff when the topic is missing, also in AX</returns>
		/// <remarks>
		/// The index is written at ((hash >> (5 + hash)) * 2, not at the place it is read from ((hash >> 5) + hash) * 2: an
		/// operator precedence mistake of the translation, kept (see MIGRATION-BACKLOG.md).
		/// </remarks>
		ushort ImportTextFromFile(GameText docname, GameText topic);

		/// <summary>ImportTextFromFile of the topic in a C# string</summary>
		ushort ImportTextFromFile(GameText docname, string topic);

		/// <summary>
		/// The text of the topic at the address from the text file 0x30a5 into the message buffer, its codes replaced, arranged
		/// in rows of 0x50 for a dialog
		/// </summary>
		void DisplayTextDialog(GameText topic);

		/// <summary>
		/// Replaces in the text at TextBuffer.Address every occurrence of the 5 codes (texts pointed to by 0x30ae + 2 * code) by their
		/// texts (0x30b8 + 2 * code), one code after the other.
		/// </summary>
		void ReflowText();

		/// <summary>
		/// Shortens the text until it fits the width: the last character goes, and the one before becomes a dot (unless the
		/// character two before the end is a space)
		/// </summary>
		/// <summary>ShrinkTextToWidth of a text in a C# string (the original kept it on the stack): the shortened text</summary>
		string ShrinkTextToWidth(string text, ushort peakBreadth);

		void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth);
	}
}
