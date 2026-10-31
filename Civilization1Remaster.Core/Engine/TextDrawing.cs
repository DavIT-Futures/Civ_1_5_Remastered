using Civilization1Remaster.Emulation;
using Microsoft.Win32;
using System.Threading;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1182. B6.1: the texts are C# strings; the versions with the address
	/// of a text in the data segment read it there first (for the code that still keeps its texts in the memory).
	/// </summary>
	public class TextDrawing : GameModule, ITextDrawing
	{
		public TextDrawing(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void RenderText(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.ForeHues = style;

			if (DoubleWidthText != 0)
			{
				horizLoc <<= 1;
			}

			this.Owner.VideoCard.RenderText(ScreenBox, horizLoc, vertLoc, text);
		}

		/// <inheritdoc/>
		public void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style) => RenderText(TextAt(charsAddr), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void RenderTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void RenderTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			RenderTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void RenderTextWithShadow(string text, int horizLoc, int vertLoc, ushort style)
		{
			RenderTextToScreen0(text, horizLoc, vertLoc + 1, 0);
			RenderTextToScreen0(text, horizLoc, vertLoc, style);
		}

		/// <inheritdoc/>
		public void RenderTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style) =>
			RenderTextWithShadow(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void RenderCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			horizLoc -= (ushort)ObtainTextWidth(text) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void RenderCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			RenderCenteredTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void RenderCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style)
		{
			horizLoc -= (ushort)((ushort)ObtainTextWidth(text) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc + 1, vertLoc + 1, 0);
			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void RenderCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style) =>
			RenderCenteredTextWithShadowToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public int ObtainTextWidth(string text)
		{
			return this.Owner.VideoCard.ObtainRenderTextSize(DrawingBox.TypefaceIdx, text).Width;
		}

		/// <inheritdoc/>
		public int ObtainTextWidth(ushort charsAddr) => ObtainTextWidth(TextAt(charsAddr));

		/// <inheritdoc/>
		public void AwaitTime(short holdMoment)
		{
			this.Owner.Services.RestoreTimer();

			while ((short)this.Owner.Timer.Beats < holdMoment)
			{
				this.Processor.Idle();
			}
		}

		/// <summary>The text at the address of the data segment</summary>
		private string TextAt(ushort charsAddr) => this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, charsAddr));
	}
}
