namespace Civilization1Remaster.Core.Game
{
	public interface ISettlementAndScoring
	{
		void ClearCitySquare(int horizLoc, int vertLoc);

		ushort EstablishCity(short participantIdx, ushort unused, int horizLoc, int vertLoc, ushort townExtent);

		void HandleCityDisasters(short townIdx);

		ushort ShowFinalScore(short participantIdx, bool marker);

		void DrawScoreIcon(ushort icon);
	}
}
