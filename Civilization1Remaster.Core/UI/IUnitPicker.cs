namespace Civilization1Remaster.Core.UI
{
	public interface IUnitPicker
	{
		void ShowWhichArmy();

		void ShowArmyList(short participantIdx);

		void ShowUnitInfo(short participantIdx, short troopIdx);
	}
}
