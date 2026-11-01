namespace Civ_1_5_Remastered.Core.Shared.Services
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
