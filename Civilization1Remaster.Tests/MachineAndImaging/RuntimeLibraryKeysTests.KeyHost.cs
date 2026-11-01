using System.Drawing;
using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Machine;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public partial class RuntimeLibraryKeysTests
	{
		private sealed class KeyHost : IMachineHost
		{
			public Point PointerSite => Point.Empty;
			public ushort PointerClicks => 0;
			public Queue<int> InputTokens { get; } = new Queue<int>();
			public object InputLatch { get; } = new object();
			public CallJournal InterruptJournal { get; } = new CallJournal((TextWriter?)null);

			public void OnPointerEvent()
			{
			}

			public void OnClockInterrupt()
			{
			}

			public void ReadKeyBlocking()
			{
			}

			public bool ReleasePicture(ushort section) => false;
		}
	}
}
