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
		public void PrefersGoodSiteNearOwnCitiesOverBetterSiteFarAway()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 5, 10, 60, 20);
			map.Values[21, 20] = 12;
			map.Values[27, 20] = 15;
			Point[] own = { new Point(15, 20) };

			// Without the realm in mind the best land wins; with it, the good site next to the capital does.
			Assert.Equal(new Point(27, 20), CitySitePlanner.FindSite(map, new Point(15, 20), own, new HashSet<Point>(), 20));
			Assert.Equal(new Point(21, 20), CitySitePlanner.FindSite(map, new Point(15, 20), own, new HashSet<Point>(), 20, own));
		}

		[Fact]
		public void RiverLandIsWorthMore()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 5, 10, 60, 20);
			int plain = CitySitePlanner.SiteScore(map, new Point(20, 20));
			map.Terrains[20, 20] = CitySitePlanner.RiverTerrain;
			map.Terrains[20, 21] = CitySitePlanner.RiverTerrain;

			Assert.True(CitySitePlanner.SiteScore(map, new Point(20, 20)) > plain);
		}

		[Fact]
		public void NoCityInTheEdgeRows()
		{
			Assert.False(CitySitePlanner.IsCityRow(1));
			Assert.True(CitySitePlanner.IsCityRow(2));
			Assert.True(CitySitePlanner.IsCityRow(47));
			Assert.False(CitySitePlanner.IsCityRow(48));
		}

		[Fact]
		public void SettlersAtTheEdgeGoToTheNearestRowWhereACityStands()
		{
			FakeCitySiteMap map = new FakeCitySiteMap();
			map.AddLand(1, 50, 44, 10, 6);
			map.AddLand(2, 30, 40, 4, 4);

			// From the last but one row (no city there) one step up, to the land just above it.
			Point? inside = CitySitePlanner.NearestCityRowLand(map, new Point(55, 48), 4);
			Assert.NotNull(inside);
			Assert.Equal(47, inside!.Value.Y);
			Assert.Equal(1, CitySitePlanner.Distance(new Point(55, 48), inside.Value));

			// Already in a row of cities, it stays.
			Assert.Equal(new Point(55, 46), CitySitePlanner.NearestCityRowLand(map, new Point(55, 46), 4));

			// Only its own landmass counts: the island nearby is not reached on foot.
			map.AddLand(3, 40, 48, 2, 2);
			Assert.Null(CitySitePlanner.NearestCityRowLand(map, new Point(40, 48), 4));
		}

		[Fact]
		public void ASettlerInAZoneOfControlStepsToTheFreeTileNearestItsSite()
		{
			Point site = new Point(33, 24);
			(int Direction, Point Tile, bool Free)[] steps =
			{
				(1, new Point(29, 25), true),
				(2, new Point(30, 25), false),
				(3, new Point(30, 26), true),
				(4, new Point(30, 27), true),
			};

			// The tile right towards the site is in the zone of control; of the free ones, the nearest.
			Assert.Equal(3, CitySitePlanner.StepTowards(steps, site));

			// No free tile: no step.
			Assert.Equal(0, CitySitePlanner.StepTowards(steps.Select(step => (step.Direction, step.Tile, false)), site));
			Assert.Equal(0, CitySitePlanner.StepTowards(Array.Empty<(int, Point, bool)>(), site));
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
