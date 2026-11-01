using System.Collections.Generic;
using System.Drawing;

namespace Civ_1_5_Remastered.Machine.Hosting
{
	public interface IMachineHost
	{
		Point PointerSite { get; }
		ushort PointerClicks { get; }
		Queue<int> InputTokens { get; }
		object InputLatch { get; }
		CallJournal InterruptJournal { get; }

		void OnPointerEvent();

		void OnClockInterrupt();

		void ReadKeyBlocking();

		bool ReleasePicture(ushort section);
	}
}
