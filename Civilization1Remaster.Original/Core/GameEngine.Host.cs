using System.Drawing;
using Civilization1Original.Emulation;

namespace Civilization1Original
{
	/// <summary>
	/// The game engine is the host of the emulated processor.
	/// </summary>
	public partial class GameEngine : IMachineHost
	{
		Point IMachineHost.PointerSite { get { return this.VideoCard.DisplayPointerSite; } }

		ushort IMachineHost.PointerClicks { get { return this.VideoCard.DisplayPointerClicks; } }

		Queue<int> IMachineHost.InputTokens { get { return this.VideoCard.Tokens; } }

		object IMachineHost.InputLatch { get { return this.VideoCard.VideoLatch; } }

		LogWrapper IMachineHost.IrqJournal { get { return this.IrqJournal; } }

		void IMachineHost.OnPointerEvent() { this.Services.PointerEvent(); }

		void IMachineHost.OnClockInterrupt() { this.Services.ClockInterrupt(); }

		void IMachineHost.ReadKeyBlocking() { this.Libc.ReadKey(); }
	}
}
