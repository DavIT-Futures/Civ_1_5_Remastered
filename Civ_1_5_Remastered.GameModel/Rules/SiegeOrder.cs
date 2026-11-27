namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>What a computer attacker near an enemy city does in a siege.</summary>
	public enum SiegeOrder
	{
		/// <summary>Not part of a siege: the unit moves as before.</summary>
		None,

		/// <summary>Walk to a free tile next to the city and join the ring around it.</summary>
		Advance,

		/// <summary>Stay fortified next to the city until enough attackers stand around it.</summary>
		Hold,

		/// <summary>Every attacker around the city attacks in the same turn.</summary>
		Assault,

		/// <summary>The city cannot be taken: destroy the improvements around it instead.</summary>
		Raid,
	}
}
