namespace Civilization1Remaster.Core.Engine
{
	public interface ITextPainter
	{
		void RenderText(string text, int horizLoc, int vertLoc, ushort style);

		void RenderText(ushort charsAddr, int horizLoc, int vertLoc, ushort style);

		void DrawTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		void DrawTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		void DrawTextWithShadow(string text, int horizLoc, int vertLoc, ushort style);

		void DrawTextWithShadow(GameText text, int horizLoc, int vertLoc, ushort style);

		void DrawCenteredTextToScreen0(string text, int horizLoc, int vertLoc, ushort style);

		void DrawCenteredTextToScreen0(GameText text, int horizLoc, int vertLoc, ushort style);

		void DrawCenteredTextWithShadowToScreen0(string text, ushort horizLoc, ushort vertLoc, ushort style);

		void DrawCenteredTextWithShadowToScreen0(GameText text, ushort horizLoc, ushort vertLoc, ushort style);

		int MeasureTextWidth(string text);

		int MeasureTextWidth(ushort charsAddr);

		void WaitForTime(short holdMoment);
	}
}
