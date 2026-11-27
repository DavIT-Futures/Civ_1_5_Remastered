namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>What a unit gains by pillaging an enemy improvement (remastered).</summary>
	public readonly struct PillageReward
	{
		/// <summary>Gold for the pillaging civilization.</summary>
		public int Gold { get; init; }

		/// <summary>The unit gets all its hit points back (it feeds on the irrigated fields).</summary>
		public bool Heals { get; init; }
	}
}
