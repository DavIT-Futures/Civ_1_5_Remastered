namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IMenuBar
	{
		void ManageMenuBar(short participantIndex, short troopIndex, short marker);

		void ShowGameMenu();

		void ShowOrdersMenu(short participantIndex, short troopIndex);

		void ShowAdvisorMenu();

		void ShowWorldMenu();

		void ShowCivilopediaMenu();
	}
}
