
namespace Civilization1Remaster.Core.Engine
{
	/// <summary>The operations of DialogPainter that the other parts of the game use</summary>
	public interface IDialogPainter
	{
		/// <summary>
		/// When set, Tab, Left and Right close the menu (ShowMenu) like Escape and set MenuTabKeyPressed,
		/// so a menu with tabs can switch between them
		/// </summary>
		bool ChoicesIndentTokens { get; set; }

		/// <summary>
		/// Set when the last menu was closed with Tab, Left or Right (see MenuTabKeys)
		/// </summary>
		bool ChoicesIndentTokenDown { get; set; }

		/// <summary>
		/// Displays a menu or dialog box from the text at charsAddr at (horizLoc, vertLoc), handles keyboard and mouse navigation,
		/// and returns the chosen option index (or 0xffff when cancelled)
		/// </summary>
		ushort ShowMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort marker);

		/// <summary>ShowMenu of a text in a C# string (the original kept it on the stack, where its changes stay unseen)</summary>
		ushort ShowMenu(string text, int horizLoc, int vertLoc, ushort marker);

		/// <summary>
		/// Layouts and renders the menu or dialog box for charsAddr at (horizLoc, vertLoc): on the first pass (0xd2e2 == 1) measures
		/// the lines, records their offsets (0x654e) and hotkeys (0xdc4a), and draws the frame and advisor portrait (0x2f9e); then
		/// renders the text lines
		/// </summary>
		void RunMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice);

		/// <summary>
		/// Fills the box moved by (1, 1) with the colour (the motif for 7 with BoxMotif), draws its shadow (15 / 8) and a frame
		/// of colour 0 around it
		/// </summary>
		void FillBoxWithDoubleShadow(int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		/// <summary>
		/// Draws a rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		void DrawBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		/// <summary>
		/// Draws a shaddow rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		/// <param name="mode1"></param>
		void DrawShadowBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style, ushort style1);

		/// <summary>
		/// Reads a key and gives the navigation keys one code each (the other codes of the arrows, Home and the shifted keys of
		/// the keypad map to the usual ones); any other key with a character loses its scan code (high byte)
		/// </summary>
		ushort ReadNavigationKey();
	}
}
