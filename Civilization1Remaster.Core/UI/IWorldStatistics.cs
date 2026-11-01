namespace Civilization1Remaster.Core.UI
{
	public interface IWorldStatistics
	{
		void ShowWorldMap(ushort marker);

		void ShowGameFigures();

		void ShowCivilizationGraph();

		void ShowWondersOfTheWorld();

		void ShowEpicChronicle();

		void ShowCensus(short participantIndex);

		void DrawCensusLine(ushort valuesAddress, short verticalLocation, ushort charactersAddress);

		void DrawCensusLine(short[] values, short verticalLocation, ushort charactersAddress);
	}
}
