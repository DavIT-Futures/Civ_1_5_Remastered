using Civ_1_5_Remastered.Core.Engine.Painting;

namespace Civ_1_5_Remastered.Tests.Core.Engine
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

		[Fact]
		public void AddressesAreStrictlyAscendingAndMatchBoxCount()
		{
			ScreenBoxes boxes = new ScreenBoxes();

			Assert.Equal(boxes.Content.Length, ScreenBoxes.Addresses.Length);
			for (int i = 1; i < ScreenBoxes.Addresses.Length; i++)
			{
				Assert.True(ScreenBoxes.Addresses[i] > ScreenBoxes.Addresses[i - 1]);
			}
		}

		[Fact]
		public void BoxBuffersStartZeroedAndRemainIndependent()
		{
			ScreenBoxes boxes = new ScreenBoxes();

			Assert.All(boxes.Content, box => Assert.All(box, value => Assert.Equal((byte)0, value)));

			boxes.Content[0][0] = 0x7f;

			Assert.Equal((byte)0x7f, boxes.Content[0][0]);
			Assert.Equal((byte)0, boxes.Content[1][0]);
			Assert.Equal((byte)0, boxes.Content[2][0]);
			Assert.Equal((byte)0, boxes.Content[3][0]);
		}
	}
}
