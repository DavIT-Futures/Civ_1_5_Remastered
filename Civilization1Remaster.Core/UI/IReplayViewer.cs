namespace Civilization1Remaster.Core.UI
{
	public interface IReplayViewer
	{
		void ShowReplay();

		void DrawReplayPreamble();

		ushort ObtainReplayYear(ushort round);

		void AssignReplayCityContent(ushort content, ushort horizLoc, ushort vertLoc);

		void AssignReplayCityContentAlt(ushort content, ushort horizLoc, ushort vertLoc);

		ushort ComputeDistanceToReplayCity(int horizLoc, int vertLoc);

		void DrawReplayMapTile(int horizLoc, int vertLoc);

		void DumpReplayCities();

		void ObtainReplayPlayerLabel(short participantIdx);

		void ShowConquerorScreen();

		void ShowCivilizationDestroyedScreen(short participantIdx, ushort year);
	}
}
