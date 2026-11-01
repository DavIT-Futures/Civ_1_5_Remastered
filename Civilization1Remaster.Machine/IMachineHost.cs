using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Machine
{
	public interface IMachineHost
	{
		Point PointerSite { get; }
		ushort PointerClicks { get; }
		Queue<int> InputTokens { get; }
		object InputLatch { get; }
		CallJournal IrqJournal { get; }

		void OnPointerEvent();

		void OnClockInterrupt();

		void ReadKeyBlocking();

		bool ReleasePicture(ushort section);
	}
}
