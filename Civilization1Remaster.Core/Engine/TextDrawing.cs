using Civilization1Remaster.Emulation;
using Microsoft.Win32;
using System.Threading;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1182.
	/// </summary>
	public class TextDrawing : GameModule
	{
		public TextDrawing(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Draws a string at specified coordinates on screen selected by rectangle at 0xaa
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.ForeHues = style;

			if (this.processor.PullGlobalWord(0x6b8c) != 0x0)
			{
				horizLoc <<= 1;
			}

			this.owner.VideoCard.RenderText(ScreenBox, horizLoc, vertLoc, charsAddr);
		}

		/// <summary>
		/// Draws a string at specified coordinates on screen 0
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void RenderTextToScreen0(ushort charsAddr, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(charsAddr, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void RenderTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			this.owner.Words.Set(text);
			RenderTextToScreen0(TextBuffer.Address, horizLoc, vertLoc, style);
		}

		/// <summary>
		/// Draws a string with shadow at specified coordinates on screen 0
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void RenderTextWithShadow(ushort charsAddr, int horizLoc, int vertLoc, ushort style)
		{
			RenderTextToScreen0(charsAddr, horizLoc, vertLoc + 1, 0);
			RenderTextToScreen0(charsAddr, horizLoc, vertLoc, style);
		}

		/// <summary>
		/// Draws a centered string by rectangle defined at 0xaa on screen 0
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void RenderCenteredTextToScreen0(ushort charsAddr, int horizLoc, int vertLoc, ushort style)
		{
			horizLoc -= (ushort)ObtainTextWidth(charsAddr) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(charsAddr, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void RenderCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			this.owner.Words.Set(text);
			RenderCenteredTextToScreen0(TextBuffer.Address, horizLoc, vertLoc, style);
		}

		/// <summary>
		/// Draws a centered string with shaddow by rectangle defined at 0xaa on screen 0
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void RenderCenteredTextWithShadowToScreen0(ushort charsAddr, ushort horizLoc, ushort vertLoc, ushort style)
		{
			horizLoc -= (ushort)((ushort)ObtainTextWidth(charsAddr) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(charsAddr, horizLoc + 1, vertLoc + 1, 0);
			RenderText(charsAddr, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <summary>
		/// Calculates the string width
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <returns></returns>
		public int ObtainTextWidth(ushort charsAddr)
		{
			ushort faceIdent = DrawingBox.TypefaceIdx;
			string words = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr));

			this.processor.RegAcc.Value16 = (ushort)this.owner.VideoCard.ObtainRenderTextSize(faceIdent, words).Width;

			return this.owner.VideoCard.ObtainRenderTextSize(faceIdent, words).Width;
		}

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
