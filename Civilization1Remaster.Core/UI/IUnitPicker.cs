namespace Civilization1Remaster.Core.UI
{
	public interface IUnitPicker
	{
		void ShowWhichArmy();

		void ShowArmyList(short participantIndex);

		void ShowUnitInformation(short participantIndex, short troopIndex);
	}
}
