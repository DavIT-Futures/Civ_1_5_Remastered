using Civ_1_5_Remastered.Core.Shared.State;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class MapViewportTests
	{
		[Fact]
		public void OriginalWindowMatchesTheOriginalScreen()
		{
			MapViewport viewport = new MapViewport();

			Assert.Equal(320, viewport.ScreenWidth);
			Assert.Equal(200, viewport.ScreenHeight);
			Assert.Equal(7, viewport.MiddleColumn);
			Assert.Equal(6, viewport.MiddleRow);
			Assert.Equal(38, viewport.LastTopRow);
			Assert.False(viewport.Enlarged);
		}

		[Fact]
		public void RequestedSizeAppliesOnce()
		{
			MapViewport viewport = new MapViewport();

			viewport.Request(30, 24);

			Assert.True(viewport.Pending);
			Assert.Equal(15, viewport.Columns);
			Assert.True(viewport.Apply());
			Assert.False(viewport.Apply());
			Assert.Equal(560, viewport.ScreenWidth);
			Assert.Equal(392, viewport.ScreenHeight);
			Assert.Equal(15, viewport.MiddleColumn);
			Assert.Equal(12, viewport.MiddleRow);
			Assert.Equal(26, viewport.LastTopRow);
			Assert.True(viewport.Enlarged);
		}

		[Fact]
		public void SizeStaysWithinTheWorld()
		{
			MapViewport viewport = new MapViewport();

			viewport.Request(5, 200);
			viewport.Apply();

			Assert.Equal(MapViewport.OriginalColumns, viewport.Columns);
			Assert.Equal(MapViewport.MaximumRows, viewport.Rows);
			Assert.True(viewport.LastTopRow >= 0);
		}
	}
}
