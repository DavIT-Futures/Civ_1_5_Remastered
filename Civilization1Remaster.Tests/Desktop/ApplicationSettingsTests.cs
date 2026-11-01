using Civilization1Remaster.Desktop.Settings;

namespace Civilization1Remaster.Tests.Desktop
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
