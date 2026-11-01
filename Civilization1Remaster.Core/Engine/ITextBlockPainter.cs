namespace Civilization1Remaster.Core.Engine
{
	public interface ITextBlockPainter
	{
		void ArrangeTextRow(short breadth);

		int DrawTextChunk(int peakBreadth, int horizLoc, int vertLoc, ushort style);

		ushort LoadTextFromFile(GameText docname, GameText topic);

		ushort LoadTextFromFile(GameText docname, string topic);

		void ShowTextDialog(GameText topic);

		void ReflowText();

		string ShrinkTextToWidth(string text, ushort peakBreadth);

		void ShrinkTextToWidth(ushort charsAddr, ushort peakBreadth);
	}
}
