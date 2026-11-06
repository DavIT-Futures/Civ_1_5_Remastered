namespace Civ_1_5_Remastered.GameModel.State
{
	/// <summary>Standing order of an automated settler.</summary>
	public enum SettlerAutomation
	{
		None = 0,
		RoadsOnly = 1,
		ImproveAll = 2,
		ImproveKeepExisting = 3,
		CleanPollution = 4,
		ConnectCities = 5,
	}
}
