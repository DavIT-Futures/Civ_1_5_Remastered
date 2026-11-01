using System.Collections.Generic;

namespace Civilization1Remaster.Core
{
	public partial class GameEngine : IMachineHost
	{
		Point IMachineHost.PointerSite
		{
			get { return this.VideoCard.DisplayPointerSite; }
		}

		ushort IMachineHost.PointerClicks
		{
			get { return this.VideoCard.DisplayPointerClicks; }
		}

		Queue<int> IMachineHost.InputTokens
		{
			get { return this.VideoCard.Tokens; }
		}

		object IMachineHost.InputLatch
		{
			get { return this.VideoCard.VideoLatch; }
		}

		CallJournal IMachineHost.IrqJournal
		{
			get { return this.IrqJournal; }
		}

		void IMachineHost.OnPointerEvent()
		{
			this.Services.PointerEvent();
		}

		void IMachineHost.OnClockInterrupt()
		{
			this.Services.ClockInterrupt();
		}

		void IMachineHost.ReadKeyBlocking()
		{
			this.Libc.ReadKey();
		}

		bool IMachineHost.ReleasePicture(ushort section)
		{
			return this.VideoCard.Pictures.Remove(section);
		}
	}
}
