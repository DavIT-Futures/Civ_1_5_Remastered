namespace Civ_1_5_Remastered.Core.Game.Research
{
	public partial class ResearchOffice
	{
		private sealed class KindTallies
		{
			public ushort[] MakingElsewhere { get; } = new ushort[UnitKindTable.Count];

			public ushort[] UnitsOnContinent { get; } = new ushort[UnitKindTable.Count];

			/// <summary>Counts one more of <paramref name="kind"/>; a kind outside the table is left out.</summary>
			public void Count(ushort[] tally, int kind)
			{
				if (kind >= 0 && kind < tally.Length)
				{
					tally[kind]++;
				}
			}
		}
	}
}
