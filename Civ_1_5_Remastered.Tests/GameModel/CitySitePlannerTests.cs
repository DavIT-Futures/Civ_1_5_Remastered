using System.Drawing;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class CitySitePlannerTests
	{
		[Fact]
		public void SpacingIsFiveTilesAndFourOnSmallIslands()
		{
			Assert.Equal(5, CitySitePlanner.Spacing(200));
			Assert.Equal(4, CitySitePlanner.Spacing(40));

			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 10, 10, 20, 15);
			Point[] cities = { new Point(15, 15) };

			Assert.False(CitySitePlanner.IsValidSite(map, new Point(19, 15), cities));
			Assert.True(CitySitePlanner.IsValidSite(map, new Point(20, 15), cities));

			map.AddLand(2, 40, 20, 6, 6);
			Point[] island = { new Point(41, 21) };
			Assert.True(CitySitePlanner.IsValidSite(map, new Point(45, 21), island));
			Assert.False(CitySitePlanner.IsValidSite(map, new Point(44, 21), island));
		}

		[Fact]
		public void RejectsOceanPolesAndPoorSites()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 10, 0, 20, 20);
			map.Values[20, 10] = 5;

			Assert.False(CitySitePlanner.IsValidSite(map, new Point(5, 5), Array.Empty<Point>()));
			Assert.False(CitySitePlanner.IsValidSite(map, new Point(15, 1), Array.Empty<Point>()));
			Assert.False(CitySitePlanner.IsValidSite(map, new Point(20, 10), Array.Empty<Point>()));
		}

		[Fact]
		public void FindsBestNearbySiteOnOwnContinentAndHonoursReservations()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 10, 10, 30, 20);
			map.Values[30, 20] = 15;
			Point[] cities = { new Point(15, 15) };

			Point? site = CitySitePlanner.FindSite(map, new Point(25, 20), cities, new HashSet<Point>(), 12);
			Assert.Equal(new Point(30, 20), site);

			Point? other = CitySitePlanner.FindSite(map, new Point(25, 20), cities, new HashSet<Point> { new Point(30, 20) }, 12);
			Assert.NotNull(other);
			Assert.NotEqual(new Point(30, 20), other);
			Assert.True(CitySitePlanner.Distance(other!.Value, cities[0]) >= 5);
		}

		[Fact]
		public void NoSiteWhenContinentIsFull()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 10, 10, 9, 9);
			Point[] cities = { new Point(14, 14) };

			Assert.Null(CitySitePlanner.FindSite(map, new Point(14, 14), cities, new HashSet<Point>(), 20));
		}

		[Fact]
		public void FindsCoastalOverseasSiteAndLandingWater()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 10, 10, 9, 9);
			map.AddLand(2, 30, 12, 8, 8);
			Point harbour = new Point(18, 14);

			Point? site = CitySitePlanner.FindOverseasSite(map, harbour, 1, new[] { new Point(14, 14) }, new HashSet<Point>(), 30);
			Assert.NotNull(site);
			Assert.Equal(2, map.Continent(site!.Value.X, site.Value.Y));
			Assert.True(CitySitePlanner.IsCoastal(map, site.Value));

			Point? water = CitySitePlanner.LandingWater(map, site.Value, harbour);
			Assert.NotNull(water);
			Assert.Equal(MapRules.OceanTerrain, map.Terrain(water!.Value.X, water.Value.Y));
			Assert.Equal(1, CitySitePlanner.Distance(water.Value, site.Value));
		}

		[Fact]
		public void AroundWrapsWithoutDuplicates()
		{
			List<Point> tiles = CitySitePlanner.Around(new Point(0, 25), 45).ToList();

			Assert.Equal(tiles.Count, tiles.Distinct().Count());
			Assert.Equal(MapRules.Width * MapRules.Height, tiles.Count);
			Assert.Equal(9, CitySitePlanner.Around(new Point(0, 25), 1).Count());
		}
	}
}
