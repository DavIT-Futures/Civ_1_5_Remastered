using System.Drawing;
using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Tests.Solution
{
	internal sealed class SilentGameHost : IGameHost
	{
		public Point PointerSite => Point.Empty;

		public PointerButtons PointerClicks => PointerButtons.None;

		public void OnDisplayCountChanged()
		{
		}

		public void ShowMessage(string message, string caption)
		{
		}

		public HostDialogResult ShowRetryCancel(string message, string caption) => HostDialogResult.Cancel;
	}
}
