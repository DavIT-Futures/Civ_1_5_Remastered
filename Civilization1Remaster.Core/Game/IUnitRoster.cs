using System;

namespace Civilization1Remaster.Core.Game
{
	public interface IUnitRoster
	{
		void CreateUnitsFromCityList(short townIdx);

		void KeepUnitsInCityList(short townIdx);

		void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker);

		void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert);

		ushort CreateUnit(short participantIdx, short kind, int horizLoc, int vertLoc);

		void DestroyUnit(short participantIdx, short troopIdx);

		ushort FindStackLeader(short participantIdx, short troopIdx);

		ushort FindBestGuardianInStack(short participantIdx, short troopIdx);

		void FindBestGuardianStep(short participantIdx, short troopIdx);

		ushort CountUnitsByDomainInStack(short participantIdx, short troopIdx, ushort mode);

		void CountUnitsByDomainStep(short participantIdx, short troopIdx);

		ushort CountUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort kind);

		void CountUnitsOfTypeStep(short participantIdx, short troopIdx);

		ushort CountUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort group);

		void CountUnitsWithFlagStep(short participantIdx, short troopIdx);

		ushort CountSpecialUnitsInStack(short participantIdx, short troopIdx);

		void CountSpecialUnitsStep(short participantIdx, short troopIdx);

		void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr);

		void ForEachUnitInStack(short participantIdx, short troopIdx, Action<short, short> visit);

		void CheckUnitTerrainFlag(short participantIdx, short troopIdx);

		void ClearUnitOrders(short participantIdx, short troopIdx);

		void RestoreUnitOrders(short participantIdx, short troopIdx);

		void RestoreUnitStatus(short participantIdx, short troopIdx);

		void DestroyLandUnit(short participantIdx, short troopIdx);

		void DestroyNonLandUnit(short participantIdx, short troopIdx);

		void DestroySeaUnit(short participantIdx, short troopIdx);

		void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc);

		ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc);

		ushort CheckTileOccupation(short participantIdx, int horizLoc, int vertLoc);

		ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc);

		void VisitHut(short participantIdx, short troopIdx);

		void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort direction);

		ushort ShowBattleScreen(short participantIdx, short troopIdx);

		ushort FindNearestFriendlyCity(short participantIdx, short troopIdx);

		void AttachReplayContent(ushort rerunIdx, byte content1);

		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2);

		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3);

		void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4);

		void DrawBusyIndicator();
	}
}
