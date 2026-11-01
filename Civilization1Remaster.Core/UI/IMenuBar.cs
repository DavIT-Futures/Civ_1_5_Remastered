namespace Civilization1Remaster.Core.UI
{
	public interface IMenuBar
	{
		void ManageMenuBar(short participantIdx, short troopIdx, short marker);

		void ShowGameMenu();

		void ShowOrdersMenu(short participantIdx, short troopIdx);

		void ShowAdvisorMenu();

		void ShowWorldMenu();

		void ShowCivilopediaMenu();
	}
}
