using System.Drawing;
using Civ_1_5_Remastered.Foundation.Support;
using Civ_1_5_Remastered.Machine.Hosting;

namespace Civ_1_5_Remastered.Tests.Machine
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
