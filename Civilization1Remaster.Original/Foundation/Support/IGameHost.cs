using System;
using System.Drawing;

namespace Civilization1Original
{
	[Flags]
	public enum PointerButtons : ushort
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4
	}

	public enum HostDialogResult
	{
		OK,
		Cancel,
		Retry
	}

	public interface IGameHost
	{
		Point PointerSite { get; }
		PointerButtons PointerClicks { get; }
		void OnDisplayCountChanged();
		void ShowMessage(string message, string caption);
		HostDialogResult ShowRetryCancel(string message, string caption);
	}
}
