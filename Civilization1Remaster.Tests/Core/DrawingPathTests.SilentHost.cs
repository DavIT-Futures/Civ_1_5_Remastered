using System.Drawing;
using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Tests.Core
{
	public partial class DrawingPathTests
	{
		private sealed class SilentHost : IGameHost
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
}
