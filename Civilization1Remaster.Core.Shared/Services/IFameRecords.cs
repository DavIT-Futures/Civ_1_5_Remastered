namespace Civilization1Remaster.Core.Shared.Services
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
