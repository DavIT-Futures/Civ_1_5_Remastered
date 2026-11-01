namespace Civilization1Remaster.Core.Engine
{
	public class TextPainter : GameComponent, ITextPainter
	{
		public TextPainter(GameEngine owner)
			: base(owner)
		{
		}

		public void RenderText(string text, int horizontalLocation, int verticalLocation, ushort style)
		{
			DrawingBox.ForegroundHues = style;

			if (Writing.DoubleWidthText != 0)
			{
				horizontalLocation <<= 1;
			}

			this.Owner.VideoCard.RenderText(Screen.Box, horizontalLocation, verticalLocation, text);
		}

		public void RenderText(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort style) => RenderText(TextAt(charactersAddress), horizontalLocation, verticalLocation, style);

		public void DrawTextToPrimaryScreen(string text, int horizontalLocation, int verticalLocation, ushort style)
		{
			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizontalLocation, verticalLocation, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawTextToPrimaryScreen(GameText text, int horizontalLocation, int verticalLocation, ushort style) =>
			DrawTextToPrimaryScreen(text.Read(this.Processor), horizontalLocation, verticalLocation, style);

		public void DrawTextWithShadow(string text, int horizontalLocation, int verticalLocation, ushort style)
		{
			DrawTextToPrimaryScreen(text, horizontalLocation, verticalLocation + 1, 0);
			DrawTextToPrimaryScreen(text, horizontalLocation, verticalLocation, style);
		}

		public void DrawTextWithShadow(GameText text, int horizontalLocation, int verticalLocation, ushort style) =>
			DrawTextWithShadow(text.Read(this.Processor), horizontalLocation, verticalLocation, style);

		public void DrawCenteredTextToPrimaryScreen(string text, int horizontalLocation, int verticalLocation, ushort style)
		{
			horizontalLocation -= (ushort)MeasureTextWidth(text) / 2;

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizontalLocation, verticalLocation, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawCenteredTextToPrimaryScreen(GameText text, int horizontalLocation, int verticalLocation, ushort style) =>
			DrawCenteredTextToPrimaryScreen(text.Read(this.Processor), horizontalLocation, verticalLocation, style);

		public void DrawCenteredTextWithShadowToPrimaryScreen(string text, ushort horizontalLocation, ushort verticalLocation, ushort style)
		{
			horizontalLocation -= (ushort)((ushort)MeasureTextWidth(text) >> 1);

			DrawingBox.FlagSet = 0x0;

			RenderText(text, horizontalLocation + 1, verticalLocation + 1, 0);
			RenderText(text, horizontalLocation, verticalLocation, style);

			DrawingBox.FlagSet = 0x1;
		}

		public void DrawCenteredTextWithShadowToPrimaryScreen(GameText text, ushort horizontalLocation, ushort verticalLocation, ushort style) =>
			DrawCenteredTextWithShadowToPrimaryScreen(text.Read(this.Processor), horizontalLocation, verticalLocation, style);

		public int MeasureTextWidth(string text)
		{
			return this.Owner.VideoCard.ObtainRenderTextSize(DrawingBox.TypefaceIndex, text).Width;
		}

		public int MeasureTextWidth(ushort charactersAddress) => MeasureTextWidth(TextAt(charactersAddress));

		public void WaitForTime(short holdMoment)
		{
			this.Owner.Services.RestoreTimer();

			while ((short)this.Owner.Timer.Beats < holdMoment)
			{
				this.Processor.Idle();
			}
		}

		private string TextAt(ushort charactersAddress) => this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersAddress));
	}
}
