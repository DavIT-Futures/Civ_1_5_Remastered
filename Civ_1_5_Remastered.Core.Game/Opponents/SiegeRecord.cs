namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	/// <summary>What a computer civilization remembers about its siege of one city (see ComputerPlayer.Siege).</summary>
	internal sealed class SiegeRecord
	{
		public int CityOwner { get; set; } = -1;

		public int FirstRound { get; set; } = -1;

		public int LastRound { get; set; } = -1;

		public int AssaultRound { get; set; } = -1;
	}
}
