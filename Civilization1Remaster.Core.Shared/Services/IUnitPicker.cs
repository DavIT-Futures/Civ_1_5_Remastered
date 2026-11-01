namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IUnitPicker
	{
		public const ushort StrategicLocationRoleTableAddress = 0xe2c2;
		public const ushort StrategicLocationHorizontalTableAddress = 0xe598;
		public const ushort StrategicLocationVerticalTableAddress = 0xe798;
		public const ushort StrategicLocationPriorityTableAddress = 0xb2be;

		void ShowWhichArmy();

		void ShowArmyList(short participantIndex);

		void ShowUnitInformation(short participantIndex, short troopIndex);
	}
}
