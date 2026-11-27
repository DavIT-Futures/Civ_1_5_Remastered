namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	/// <summary>Switches for the remastered sieges of the computer civilizations (see <see cref="SiegePlanner"/>).</summary>
	internal static class ComputerSiege
	{
		/// <summary>Switches the remastered sieges off, for measuring them against the original behaviour.</summary>
		public static bool Enabled { get; set; } = true;

		/// <summary>How many times each <see cref="SiegeOrder"/> was given, for measuring the sieges.</summary>
		public static int[] Orders { get; } = new int[5];

		public static bool Controls(IGameEngine owner, short participantIndex) =>
			Enabled && participantIndex > 0 && participantIndex < 8 && !owner.World.IsHuman(participantIndex);
	}
}
