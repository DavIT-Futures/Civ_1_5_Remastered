namespace Civilization1Remaster.Core.Engine
{
	public class TextPainter : GameComponent, ITextPainter
	{
		public TextPainter(GameEngine owner)
			: base(owner)
		{
		}

		public void RenderText(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.ForeHues = style;

			if (Writing.DoubleWidthText != 0)
			{
				horizLoc <<= 1;
			}

			this.Owner.VideoCard.RenderText(Screen.Box, horizLoc, vertLoc, text);
		}

		public void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style) => RenderText(TextAt(charsAddr), horizLoc, vertLoc, style);

		public void DrawTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		public void DrawTextWithShadow(string text, int horizLoc, int vertLoc, ushort style)
		{
			DrawTextToScreen0(text, horizLoc, vertLoc + 1, 0);
			DrawTextToScreen0(text, horizLoc, vertLoc, style);
		}

		public void DrawTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawTextWithShadow(text.Read(this.Processor), horizLoc, vertLoc, style);

		public void DrawCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style)
		{
			horizLoc -= (ushort)MeasureTextWidth(text) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style) =>
			DrawCenteredTextToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		public void DrawCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style)
		{
			horizLoc -= (ushort)((ushort)MeasureTextWidth(text) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizLoc + 1, vertLoc + 1, 0);
			RenderText(text, horizLoc, vertLoc, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style) =>
			DrawCenteredTextWithShadowToScreen0(text.Read(this.Processor), horizLoc, vertLoc, style);

		public int MeasureTextWidth(string text)
		{
			return this.Owner.VideoCard.ObtainRenderTextSize(DrawingBox.TypefaceIdx, text).Width;
		}

		public int MeasureTextWidth(ushort charsAddr) => MeasureTextWidth(TextAt(charsAddr));

		public void WaitForTime(short holdMoment)
		{
			this.Owner.Services.RestoreTimer();

			while ((short)this.Owner.Timer.Beats < holdMoment)
			{
				this.Processor.Idle();
			}
		}

		private string TextAt(ushort charsAddr) => this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, charsAddr));
	}
}
