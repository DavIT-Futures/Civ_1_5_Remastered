using System;

namespace Civilization1Remaster.Core.Game
{
	public interface IUnitRoster
	{
		void CreateUnitsFromCityList(short townIndex);

		void KeepUnitsInCityList(short townIndex);

		void ShiftUnit(int horizontalLocation, int verticalLocation, short participantIndex, short troopIndex, ushort marker);

		void ChoreographUnitMove(short participantIndex, short troopIndex, ushort cursorHorizontal, ushort cursorVertical);

		ushort CreateUnit(short participantIndex, short kind, int horizontalLocation, int verticalLocation);

		void DestroyUnit(short participantIndex, short troopIndex);

		ushort FindStackLeader(short participantIndex, short troopIndex);

		ushort FindBestGuardianInStack(short participantIndex, short troopIndex);

		void FindBestGuardianStep(short participantIndex, short troopIndex);

		ushort CountUnitsByDomainInStack(short participantIndex, short troopIndex, ushort mode);

		void CountUnitsByDomainStep(short participantIndex, short troopIndex);

		ushort CountUnitsOfTypeInStack(short participantIndex, short troopIndex, ushort kind);

		void CountUnitsOfTypeStep(short participantIndex, short troopIndex);

		ushort CountUnitsWithFlagInStack(short participantIndex, short troopIndex, ushort group);

		void CountUnitsWithFlagStep(short participantIndex, short troopIndex);

		ushort CountSpecialUnitsInStack(short participantIndex, short troopIndex);

		void CountSpecialUnitsStep(short participantIndex, short troopIndex);

		void VisitEachUnitInStack(short participantIndex, short troopIndex, int functionAddress);

		void ForEachUnitInStack(short participantIndex, short troopIndex, Action<short, short> visit);

		void CheckUnitTerrainFlag(short participantIndex, short troopIndex);

		void ClearUnitOrders(short participantIndex, short troopIndex);

		void RestoreUnitOrders(short participantIndex, short troopIndex);

		void RestoreUnitStatus(short participantIndex, short troopIndex);

		void DestroyLandUnit(short participantIndex, short troopIndex);

		void DestroyNonLandUnit(short participantIndex, short troopIndex);

		void DestroySeaUnit(short participantIndex, short troopIndex);

		void PanMapToPosition(short participantIndex, int horizontalLocation, int verticalLocation);

		ushort MayOccupyTile(short participantIndex, int horizontalLocation, int verticalLocation);

		ushort CheckTileOccupation(short participantIndex, int horizontalLocation, int verticalLocation);

		ushort ContainsAdjacentForeignUnit(short participantIndex, int horizontalLocation, int verticalLocation);

		void VisitHut(short participantIndex, short troopIndex);

		void ChoreographUnitCombat(short participantIndex, short troopIndex, ushort direction);

		ushort ShowBattleScreen(short participantIndex, short troopIndex);

		ushort FindNearestFriendlyCity(short participantIndex, short troopIndex);

		void AttachReplayContent(ushort rerunIndex, byte firstContent);

		void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent);

		void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent, byte thirdContent);

		void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent, byte thirdContent, byte fourthContent);

		void DrawBusyIndicator();
	}
}
