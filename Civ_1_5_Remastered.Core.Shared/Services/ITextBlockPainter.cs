namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface ITextBlockPainter
	{
		void ArrangeTextRow(short breadth);

		int DrawTextChunk(int peakBreadth, int horizontalLocation, int verticalLocation, ushort style);

		ushort LoadTextFromFile(GameText documentName, GameText topic);

		ushort LoadTextFromFile(GameText documentName, string topic);

		void ShowTextDialog(GameText topic);

		void ReflowText();

		string ShrinkTextToWidth(string text, ushort peakBreadth);

		void ShrinkTextToWidth(ushort charactersAddress, ushort peakBreadth);
	}
}
