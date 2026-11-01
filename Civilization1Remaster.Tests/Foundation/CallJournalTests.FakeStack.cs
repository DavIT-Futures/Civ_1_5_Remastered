using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Tests.Foundation
{
	public partial class CallJournalTests
	{
		private sealed class FakeStack : IStackProbe
		{
			public ushort StackPointer { get; set; } = 0x100;
		}
	}
}
