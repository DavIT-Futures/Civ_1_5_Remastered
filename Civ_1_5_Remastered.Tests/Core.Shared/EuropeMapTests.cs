using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Imaging.Pictures;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class EuropeMapTests
	{
		private const byte Ocean = 1;

		private static readonly int[] Leaders = [1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17];

		[Fact]
		public void EveryLeaderStartsOnLand()
		{
			using VideoGraphicsBitmap map = Map();

			foreach (int leader in Leaders)
			{
				(short column, short row) = EuropeMap.Start(leader);
				Assert.NotEqual(Ocean, map.ObtainDot(column, row));
			}
		}

		[Fact]
		public void TheStartsAreApart()
		{
			var starts = Leaders.Select(EuropeMap.Start).ToList();

			for (int first = 0; first < starts.Count; first++)
			{
				for (int second = first + 1; second < starts.Count; second++)
				{
					int distance = Math.Max(Math.Abs(starts[first].Column - starts[second].Column), Math.Abs(starts[first].Row - starts[second].Row));
					Assert.True(distance >= 3, $"leaders {Leaders[first]} and {Leaders[second]} start {distance} tiles apart");
				}
			}
		}

		[Fact]
		public void TheNewWorldLiesBeyondTheSea()
		{
			using VideoGraphicsBitmap map = Map();

			for (int row = 0; row < 50; row++)
			{
				// The map wraps: the east edge and the first columns are sea, nobody walks from Russia to the New World.
				Assert.Equal(Ocean, map.ObtainDot(78, row));
				Assert.Equal(Ocean, map.ObtainDot(79, row));
				Assert.Equal(Ocean, map.ObtainDot(0, row));
				Assert.Equal(Ocean, map.ObtainDot(1, row));
			}

			int newWorld = Enumerable.Range(0, 50).SelectMany(row => Enumerable.Range(2, 10).Select(column => map.ObtainDot(column, row))).Count(colour => colour != Ocean);
			Assert.True(newWorld > 100, $"the New World has {newWorld} tiles of land");
		}

		private static VideoGraphicsBitmap Map()
		{
			return VideoGraphicsBitmap.ViaFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Pictures", "EUROPE.PIC"), out _)!;
		}
	}
}
