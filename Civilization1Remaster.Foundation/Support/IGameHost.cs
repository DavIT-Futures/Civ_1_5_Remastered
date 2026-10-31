using System;
using System.Drawing;

namespace Civilization1Remaster.Foundation.Support
{
	public interface IGameHost
	{
		Point PointerSite { get; }
		PointerButtons PointerClicks { get; }
		void OnDisplayCountChanged();
		void ShowMessage(string message, string caption);
		HostDialogResult ShowRetryCancel(string message, string caption);
	}
}
