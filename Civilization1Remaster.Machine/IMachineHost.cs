using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// Everything the emulated processor needs from the program that hosts it: the mouse and keyboard state,
	/// the interrupt handlers of the game and the journals.
	/// </summary>
	public interface IMachineHost
	{
		Point PointerSite { get; }
		ushort PointerClicks { get; }
		Queue<int> InputTokens { get; }
		object InputLatch { get; }
		LogWrapper IrqJournal { get; }
		void OnPointerEvent();
		void OnClockInterrupt();
		void ReadKeyBlocking();

		/// <summary>Frees the picture of the driver at the segment (0xb000 and above); false when there is none</summary>
		bool ReleasePicture(ushort section);
	}
}
