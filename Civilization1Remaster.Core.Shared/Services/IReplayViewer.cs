namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IReplayViewer
	{
		void ShowReplay();

		void DrawReplayPreamble();

		ushort ObtainReplayYear(ushort round);

		void AssignReplayCityContent(ushort content, ushort horizontalLocation, ushort verticalLocation);

		void AssignReplayCityContentAlternate(ushort content, ushort horizontalLocation, ushort verticalLocation);

		ushort ComputeDistanceToReplayCity(int horizontalLocation, int verticalLocation);

		void DrawReplayMapTile(int horizontalLocation, int verticalLocation);

		void DumpReplayCities();

		void ObtainReplayPlayerLabel(short participantIndex);

		void ShowConquerorScreen();

		void ShowCivilizationDestroyedScreen(short participantIndex, ushort year);
	}
}
