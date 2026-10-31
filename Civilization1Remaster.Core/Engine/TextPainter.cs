using Microsoft.Win32;
using System.Threading;

namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1182. B6.1: the texts are C# strings; the versions with the address
	/// of a text in the data segment read it there first (for the code that still keeps its texts in the memory).
	/// </summary>
	public class TextPainter : GameComponent, ITextPainter
	{
		public TextPainter(GameEngine owner)
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
		public void DrawTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void DrawTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void DrawTextWithShadow(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawTextToScreen0(text, horizLoc, vertLoc + 1, 0);
			DrawTextToScreen0(text, horizLoc, vertLoc, style);
		}

		/// <inheritdoc/>
		public void DrawTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawTextWithShadow(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void DrawCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			horizLoc -= (ushort)MeasureTextWidth(text) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void DrawCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawCenteredTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public void DrawCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style)
		{
			horizLoc -= (ushort)((ushort)MeasureTextWidth(text) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc + 1, vertLoc + 1, 0);
			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		/// <inheritdoc/>
		public void DrawCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style) =>
			DrawCenteredTextWithShadowToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		/// <inheritdoc/>
		public int MeasureTextWidth(string text)
		{
			return this.Owner.VideoCard.ObtainRenderTextSize(DrawingBox.TypefaceIdx, text).Width;
		}

		/// <inheritdoc/>
		public int MeasureTextWidth(ushort charsAddr) => MeasureTextWidth(TextAt(charsAddr));

		/// <inheritdoc/>
		public void WaitForTime(short holdMoment)
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
