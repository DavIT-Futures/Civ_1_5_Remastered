namespace Civ_1_5_Remastered.Tests.Foundation
{
	public partial class CallJournalTests
	{
		private sealed class Counter
		{
			public int Formatted { get; private set; }

			public override string ToString()
			{
				this.Formatted++;
				return "counter";
			}
		}
	}
}
