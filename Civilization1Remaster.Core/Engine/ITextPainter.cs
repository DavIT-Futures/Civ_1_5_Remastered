using Civilization1Remaster.Emulation;
using Microsoft.Win32;
using System.Threading;

namespace Civilization1Remaster.Engine
{
	/// <summary>The operations of TextPainter that the other parts of the game use</summary>
	public interface ITextPainter
	{
		/// <summary>Draws the text at (horizLoc, vertLoc) of the box at 0xaa (twice as far right with DoubleWidthText) in the style</summary>
		void RenderText(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>RenderText of the text at the address</summary>
		void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text at (horizLoc, vertLoc) on screen 0 (the flag 0 of the box meanwhile)</summary>
		void RenderTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>RenderTextToScreen0 of the text at the address</summary>
		void RenderTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text with a shadow (colour 0, one row lower) on screen 0</summary>
		void RenderTextWithShadow(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>RenderTextWithShadow of the text at the address</summary>
		void RenderTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text centered at horizLoc on screen 0</summary>
		void RenderCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		/// <summary>RenderCenteredTextToScreen0 of the text at the address</summary>
		void RenderCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		/// <summary>Draws the text centered at horizLoc with a shadow (colour 0, one right and one lower) on screen 0</summary>
		void RenderCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style);

		/// <summary>RenderCenteredTextWithShadowToScreen0 of the text at the address</summary>
		void RenderCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style);

		/// <summary>The width of the text in the typeface of the box at 0xaa</summary>
		int ObtainTextWidth(string text);

		/// <summary>ObtainTextWidth of the text at the address</summary>
		int ObtainTextWidth(ushort charsAddr);

		/// <summary>
		/// Waits for specified number of main timer ticks
		/// </summary>
		/// <param name="waitTime"></param>
		void AwaitTime(short holdMoment);
	}
}
