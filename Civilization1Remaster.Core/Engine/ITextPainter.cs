namespace Civilization1Remaster.Core.Engine
{
	public interface ITextPainter
	{
		void RenderText(string text, int horizontalLocation, int verticalLocation, ushort style);

		void RenderText(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort style);

		void DrawTextToPrimaryScreen(string text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawTextToPrimaryScreen(GameText text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawTextWithShadow(string text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawTextWithShadow(GameText text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawCenteredTextToPrimaryScreen(string text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawCenteredTextToPrimaryScreen(GameText text, int horizontalLocation, int verticalLocation, ushort style);

		void DrawCenteredTextWithShadowToPrimaryScreen(string text, ushort horizontalLocation, ushort verticalLocation, ushort style);

		void DrawCenteredTextWithShadowToPrimaryScreen(GameText text, ushort horizontalLocation, ushort verticalLocation, ushort style);

		int MeasureTextWidth(string text);

		int MeasureTextWidth(ushort charactersAddress);

		void WaitForTime(short holdMoment);
	}
}
