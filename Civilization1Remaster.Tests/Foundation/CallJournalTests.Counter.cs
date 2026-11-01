namespace Civilization1Remaster.Tests.Foundation
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
