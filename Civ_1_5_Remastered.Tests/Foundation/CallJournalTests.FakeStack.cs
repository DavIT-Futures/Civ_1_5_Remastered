using Civ_1_5_Remastered.Foundation.Support;

namespace Civ_1_5_Remastered.Tests.Foundation
{
	public partial class CallJournalTests
	{
		private sealed class FakeStack : IStackProbe
		{
			public ushort StackPointer { get; set; } = 0x100;
		}
	}
}
