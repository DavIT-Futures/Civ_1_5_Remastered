namespace Civilization1Remaster.Core.Game
{
	public interface IComputerPlayer
	{
		void OperateAITurn(short participantIdx);

		ushort ManageBarbarianUnit(short participantIdx, short troopIdx);

		void PostCityDefenders(short townIdx);

		void ReimposeStrategicLocations(short participantIdx);

		void SendUnitToStrategicLocation(short participantIdx, short horizLoc, short vertLoc, short live, short strategy);

		void FreeStrategicLocation(short participantIdx, ushort place);

		void ClearStrategicLocations(short participantIdx);

		void AttachStrategicLocation(short participantIdx, ushort horizLoc, ushort vertLoc, ushort live, ushort strategy);

		void AffixStrategicLocation(short participantIdx, ushort place);

		void DisableStrategicLocation(short participantIdx, ushort role, int horizLoc, int vertLoc, ushort reach);

		void RefreshCityProduction(short participantIdx, ushort continent);

		void PickNewCityProduction(short townIdx);

		ushort MayAIEnterTile(short participantIdx, ushort horizLoc, ushort vertLoc);

		ushort ShiftAIUnit(short participantIdx, short troopIdx);
	}
}
