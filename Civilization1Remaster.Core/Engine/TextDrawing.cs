using Civilization1Remaster.Emulation;
using Microsoft.Win32;
using System.Threading;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1182. B6.1: the texts are C# strings; the versions with the address
	/// of a text in the data segment read it there first (for the code that still keeps its texts in the memory).
	/// </summary>
	public class TextDrawing : GameModule
	{
		public TextDrawing(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>The text at the address of the data segment</summary>
		private string TextAt(ushort charsAddr) => this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr));

		/// <summary>Draws the text at (horizLoc, vertLoc) of the box at 0xaa (twice as far right with DoubleWidthText) in the style</summary>
		public void RenderText(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.ForeHues = style;

			if (DoubleWidthText != 0)
			{
				horizLoc <<= 1;
			}

			this.owner.VideoCard.RenderText(ScreenBox, horizLoc, vertLoc, text);
		}

		/// <summary>RenderText of the text at the address</summary>
		public void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style) => RenderText(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <summary>Draws the text at (horizLoc, vertLoc) on screen 0 (the flag 0 of the box meanwhile)</summary>
		public void RenderTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <summary>RenderTextToScreen0 of the text at the address</summary>
		public void RenderTextToScreen0(ushort charsAddr, int horizLoc, int vertLoc, ushort style) =>
			RenderTextToScreen0(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <summary>Draws the text with a shadow (colour 0, one row lower) on screen 0</summary>
		public void RenderTextWithShadow(string text, int horizLoc, int vertLoc, ushort style)
		{
			RenderTextToScreen0(text, horizLoc, vertLoc + 1, 0);
			RenderTextToScreen0(text, horizLoc, vertLoc, style);
		}

		/// <summary>RenderTextWithShadow of the text at the address</summary>
		public void RenderTextWithShadow(ushort charsAddr, int horizLoc, int vertLoc, ushort style) =>
			RenderTextWithShadow(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <summary>Draws the text centered at horizLoc on screen 0</summary>
		public void RenderCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			horizLoc -= (ushort)ObtainTextWidth(text) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <summary>RenderCenteredTextToScreen0 of the text at the address</summary>
		public void RenderCenteredTextToScreen0(ushort charsAddr, int horizLoc, int vertLoc, ushort style) =>
			RenderCenteredTextToScreen0(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <summary>Draws the text centered at horizLoc with a shadow (colour 0, one right and one lower) on screen 0</summary>
		public void RenderCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style)
		{
			horizLoc -= (ushort)((ushort)ObtainTextWidth(text) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc + 1, vertLoc + 1, 0);
			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <summary>RenderCenteredTextWithShadowToScreen0 of the text at the address</summary>
		public void RenderCenteredTextWithShadowToScreen0(ushort charsAddr, ushort horizLoc, ushort vertLoc, ushort style) =>
			RenderCenteredTextWithShadowToScreen0(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <summary>The width of the text in the typeface of the box at 0xaa</summary>
		public int ObtainTextWidth(string text)
		{
			return this.owner.VideoCard.ObtainRenderTextSize(DrawingBox.TypefaceIdx, text).Width;
		}

		/// <summary>ObtainTextWidth of the text at the address</summary>
		public int ObtainTextWidth(ushort charsAddr) => ObtainTextWidth(TextAt(charsAddr));

		/// <summary>
		/// Waits for specified number of main timer ticks
		/// </summary>
		/// <param name="waitTime"></param>
		public void AwaitTime(short holdMoment)
		{
			this.owner.Services.RestoreTimer();

			while ((short)this.owner.Timer.Beats < holdMoment)
			{
				this.processor.Idle();
			}
		}
	}
}
