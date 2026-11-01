using Civ_1_5_Remastered.Desktop.Settings;

namespace Civ_1_5_Remastered.Tests.Desktop
{
	public class ApplicationSettingsTests
	{
		[Fact]
		public void FreshSettingsUseFourTimesTheScreen()
		{
			Assert.Equal(4, new ApplicationSettings().Scale);
		}
	}
}
