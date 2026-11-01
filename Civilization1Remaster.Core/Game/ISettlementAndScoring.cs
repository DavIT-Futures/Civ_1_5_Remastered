namespace Civilization1Remaster.Core.Game
{
	public interface ISettlementAndScoring
	{
		void ClearCitySquare(int horizontalLocation, int verticalLocation);

		ushort EstablishCity(short participantIndex, ushort unused, int horizontalLocation, int verticalLocation, ushort townExtent);

		void HandleCityDisasters(short townIndex);

		ushort ShowFinalScore(short participantIndex, bool marker);

		void DrawScoreIcon(ushort icon);
	}
}
