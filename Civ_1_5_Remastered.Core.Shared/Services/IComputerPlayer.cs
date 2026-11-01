namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IComputerPlayer
	{
		void OperateComputerTurn(short participantIndex);

		ushort ManageBarbarianUnit(short participantIndex, short troopIndex);

		void PostCityDefenders(short townIndex);

		void ReimposeStrategicLocations(short participantIndex);

		void SendUnitToStrategicLocation(short participantIndex, short horizontalLocation, short verticalLocation, short live, short strategy);

		void FreeStrategicLocation(short participantIndex, ushort place);

		void ClearStrategicLocations(short participantIndex);

		void AttachStrategicLocation(short participantIndex, ushort horizontalLocation, ushort verticalLocation, ushort live, ushort strategy);

		void AffixStrategicLocation(short participantIndex, ushort place);

		void DisableStrategicLocation(short participantIndex, ushort role, int horizontalLocation, int verticalLocation, ushort reach);

		void RefreshCityProduction(short participantIndex, ushort continent);

		void PickNewCityProduction(short townIndex);

		ushort MayComputerEnterTile(short participantIndex, ushort horizontalLocation, ushort verticalLocation);

		ushort ShiftComputerUnit(short participantIndex, short troopIndex);
	}
}
