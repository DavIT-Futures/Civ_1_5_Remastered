using System.Drawing;
using Civ_1_5_Remastered.Foundation.Support;

namespace Civ_1_5_Remastered.Tests.Core
{
	public partial class SaveGameRoundTripTests
	{
		private sealed class SilentHost : IGameHost
		{
			public List<string> Messages { get; } = new List<string>();
			public Point PointerSite => Point.Empty;
			public PointerButtons PointerClicks => PointerButtons.None;

			public void OnDisplayCountChanged()
			{
			}

			public void ShowMessage(string message, string caption) => this.Messages.Add($"{caption}: {message}");

			public HostDialogResult ShowRetryCancel(string message, string caption) => HostDialogResult.Cancel;
		}
	}
}
