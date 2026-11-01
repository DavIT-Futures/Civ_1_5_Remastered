using Civilization1Remaster.Core.Engine.Painting;

namespace Civilization1Remaster.Tests.Core.Engine
{
	public class ScreenBoxesTests
	{
		[Fact]
		public void FourBoxesStartWithTheMainOne()
		{
			ScreenBoxes boxes = new ScreenBoxes();

			Assert.Equal(new ushort[] { 0x98, 0x19c2, 0x19d6, 0x19ea }, ScreenBoxes.Addresses);
			Assert.Equal(4, boxes.Content.Length);
			Assert.All(boxes.Content, box => Assert.Equal(ScreenBoxes.Size, box.Length));
		}
	}
}
