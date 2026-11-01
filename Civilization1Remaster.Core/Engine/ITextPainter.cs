namespace Civilization1Remaster.Core.Engine
{
	/// <summary>The operations of TextPainter that the other parts of the game use</summary>
	public interface ITextPainter
	{
		/// <summary>Draws the text at (horizLoc, vertLoc) of the box at 0xaa (twice as far right with DoubleWidthText) in the style</summary>
		void RenderText(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>RenderText of the text at the address</summary>
		void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text at (horizLoc, vertLoc) on screen 0 (the flag 0 of the box meanwhile)</summary>
		void DrawTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>DrawTextToScreen0 of the text at the address</summary>
		void DrawTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text with a shadow (colour 0, one row lower) on screen 0</summary>
		void DrawTextWithShadow(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>DrawTextWithShadow of the text at the address</summary>
		void DrawTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text centered at horizLoc on screen 0</summary>
		void DrawCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>DrawCenteredTextToScreen0 of the text at the address</summary>
		void DrawCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text centered at horizLoc with a shadow (colour 0, one right and one lower) on screen 0</summary>
		void DrawCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style);

		/// <summary>DrawCenteredTextWithShadowToScreen0 of the text at the address</summary>
		void DrawCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style);

		/// <summary>The width of the text in the typeface of the box at 0xaa</summary>
		int MeasureTextWidth(string text);

		/// <summary>MeasureTextWidth of the text at the address</summary>
		int MeasureTextWidth(ushort charsAddr);

		/// <summary>
		/// Waits for specified number of main timer ticks
		/// </summary>
		/// <param name="waitTime"></param>
		void WaitForTime(short holdMoment);
	}
}
