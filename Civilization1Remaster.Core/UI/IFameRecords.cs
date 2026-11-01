namespace Civilization1Remaster.Core.UI
{
	public interface IFameRecords
	{
		void ClearHallOfFame();

		void LoadHallOfFame();

		void SaveHallOfFame();

		ushort ShowHallOfFame(ushort newPlace);

		ushort AttachHallOfFameRecord();

		void ShowGameResultsScreen(ushort rankGiven, ushort score);

		void ShowTopCities();
	}
}
