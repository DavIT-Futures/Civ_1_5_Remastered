using Civilization1Remaster.Core.Composition;
using Civilization1Remaster.Core.Game.Map;

namespace Civilization1Remaster.Tests.Core.Game
{
	public class MapPlanesTests
	{
		private const ushort MapDisplay = 2;

		[Fact]
		public void OwnerBitsReadBackWhatWasAssigned()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());
			engine.VideoCard.ReserveScreen(MapDisplay);
			MapPlanes chart = new MapPlanes(engine);

			chart.AssignOwnerBits(10, 20, 5);
			chart.AssignOwnerBits(11, 20, 3);

			Assert.Equal(5, chart.OwnerBits(10, 20));
			Assert.Equal(3, chart.OwnerBits(11, 20));
		}
	}
}
