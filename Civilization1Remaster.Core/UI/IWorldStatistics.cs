namespace Civilization1Remaster.Core.UI
{
	public interface IWorldStatistics
	{
		void ShowWorldMap(ushort marker);

		void ShowGameFigures();

		void ShowCivilizationGraph();

		void ShowWondersOfTheWorld();

		void ShowEpicChronicle();

		void ShowCensus(short participantIdx);

		void DrawCensusLine(ushort valuesAddr, short vertLoc, ushort charsAddr);

		void DrawCensusLine(short[] values, short vertLoc, ushort charsAddr);
	}
}
