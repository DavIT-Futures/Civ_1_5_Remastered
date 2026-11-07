using System.Drawing;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SettlerPlannerTests
	{
		[Fact]
		public void RoadsOnlyBuildsRoadsOnTradeTerrain()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Grassland);

			Assert.Equal(SettlerWork.Road, Desired(SettlerAutomation.RoadsOnly, map, 5, 5));

			map.Terrain[5, 5] = SettlerPlanner.Hills;
			Assert.Equal(SettlerWork.None, Desired(SettlerAutomation.RoadsOnly, map, 5, 5));
		}

		[Fact]
		public void ImproveAllReplacesButKeepExistingDoesNot()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Grassland);
			map.Improvements[5, 5] = SettlerPlanner.MineBit | SettlerPlanner.RoadBit;

			Assert.Equal(SettlerWork.Irrigate, Desired(SettlerAutomation.ImproveAll, map, 5, 5));
			Assert.Equal(SettlerWork.None, Desired(SettlerAutomation.ImproveKeepExisting, map, 5, 5));

			map.Improvements[5, 5] = SettlerPlanner.IrrigationBit;
			Assert.Equal(SettlerWork.Road, Desired(SettlerAutomation.ImproveAll, map, 5, 5));
		}

		[Fact]
		public void HillsGetMinesAndIrrigationNeedsWater()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Hills);
			Assert.Equal(SettlerWork.Mine, Desired(SettlerAutomation.ImproveAll, map, 5, 5));

			map.Terrain[5, 5] = SettlerPlanner.Plains;
			map.Water = false;
			Assert.Equal(SettlerWork.Road, Desired(SettlerAutomation.ImproveAll, map, 5, 5));
		}

		[Fact]
		public void PollutionComesFirstInImproveAndCleanModes()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Grassland);
			map.Improvements[5, 5] = SettlerPlanner.PollutionBit;

			Assert.Equal(SettlerWork.CleanPollution, Desired(SettlerAutomation.ImproveKeepExisting, map, 5, 5));
			Assert.Equal(SettlerWork.CleanPollution, Desired(SettlerAutomation.CleanPollution, map, 5, 5));
			Assert.Equal(SettlerWork.Road, Desired(SettlerAutomation.RoadsOnly, map, 5, 5));

			map.Improvements[5, 5] = 0;
			Assert.Equal(SettlerWork.None, Desired(SettlerAutomation.CleanPollution, map, 5, 5));
		}

		[Fact]
		public void ConnectBuildsRailroadOverRoadOnceKnown()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Hills);
			map.Improvements[5, 5] = SettlerPlanner.RoadBit;

			Assert.Equal(SettlerWork.None, SettlerPlanner.DesiredWork(SettlerAutomation.ConnectCities, map, 5, 5, true, false, true));
			Assert.Equal(SettlerWork.Railroad, SettlerPlanner.DesiredWork(SettlerAutomation.ConnectCities, map, 5, 5, true, true, true));

			map.Terrain[6, 6] = SettlerPlanner.River;
			Assert.Equal(SettlerWork.None, SettlerPlanner.DesiredWork(SettlerAutomation.ConnectCities, map, 6, 6, false, true, true));
		}

		[Fact]
		public void PlanPrefersNearbyWorkAndSkipsReservedTiles()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Hills);
			map.Terrain[11, 10] = SettlerPlanner.Grassland;
			map.Terrain[12, 10] = SettlerPlanner.Grassland;
			Point[] cities = { new Point(10, 10) };
			map.Improvements[10, 10] = SettlerPlanner.CityBit | SettlerPlanner.RoadBit;

			var plan = SettlerPlanner.Plan(SettlerAutomation.RoadsOnly, map, new Point(10, 10), cities, new HashSet<Point>(), true, false);
			Assert.Equal(new Point(11, 10), plan!.Value.Tile);

			plan = SettlerPlanner.Plan(SettlerAutomation.RoadsOnly, map, new Point(10, 10), cities, new HashSet<Point> { new Point(11, 10) }, true, false);
			Assert.Equal(new Point(12, 10), plan!.Value.Tile);
		}

		[Fact]
		public void JungleAndSwampAreDrainedButBonusesAreKept()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Jungle);

			Assert.Equal(SettlerWork.Irrigate, Desired(SettlerAutomation.ImproveKeepExisting, map, 5, 5));

			map.Specials.Add(new Point(5, 5));
			Assert.Equal(SettlerWork.Mine, Desired(SettlerAutomation.ImproveAll, map, 5, 5));

			map.Terrain[5, 5] = SettlerPlanner.Swamp;
			Assert.Equal(SettlerWork.Road, Desired(SettlerAutomation.ImproveAll, map, 5, 5));

			map.Improvements[5, 5] = SettlerPlanner.RoadBit;
			Assert.Equal(SettlerWork.None, Desired(SettlerAutomation.ImproveAll, map, 5, 5));

			map.Terrain[6, 6] = SettlerPlanner.Swamp;
			Assert.Equal(SettlerWork.Irrigate, Desired(SettlerAutomation.ImproveAll, map, 6, 6));
			Assert.Equal(SettlerWork.None, Desired(SettlerAutomation.RoadsOnly, map, 6, 6));
		}

		[Fact]
		public void StandingSettlerBuildsRoadFirstOnQuickTerrain()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Grassland);
			Point[] cities = { new Point(10, 10) };
			map.Improvements[10, 10] = SettlerPlanner.CityBit | SettlerPlanner.RoadBit;

			var plan = SettlerPlanner.Plan(SettlerAutomation.ImproveAll, map, new Point(11, 11), cities, new HashSet<Point>(), true, false);
			Assert.Equal((new Point(11, 11), SettlerWork.Road), plan!.Value);

			map.Terrain[11, 11] = SettlerPlanner.River;
			Assert.False(SettlerPlanner.QuickRoadHere(SettlerAutomation.ImproveAll, map, new Point(11, 11), false));
			Assert.True(SettlerPlanner.QuickRoadHere(SettlerAutomation.ImproveAll, map, new Point(11, 11), true));
			Assert.False(SettlerPlanner.QuickRoadHere(SettlerAutomation.CleanPollution, map, new Point(11, 11), true));

			map.Terrain[11, 11] = SettlerPlanner.Grassland;
			map.Improvements[11, 11] = SettlerPlanner.PollutionBit;
			plan = SettlerPlanner.Plan(SettlerAutomation.ImproveAll, map, new Point(11, 11), cities, new HashSet<Point>(), true, false);
			Assert.Equal(SettlerWork.CleanPollution, plan!.Value.Work);

			map.Improvements[11, 11] = 0;
			plan = SettlerPlanner.Plan(SettlerAutomation.ImproveAll, map, new Point(30, 30), cities, new HashSet<Point>(), true, false);
			Assert.NotEqual(new Point(30, 30), plan!.Value.Tile);
		}

		[Fact]
		public void CityLandIsDevelopedShieldGrasslandFirstThenPlainsThenGrassland()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Hills);
			Point city = new Point(20, 20);
			map.Improvements[20, 20] = SettlerPlanner.CityBit | SettlerPlanner.RoadBit;

			for (int row = 18; row <= 22; row++)
			{
				for (int column = 18; column <= 22; column++)
				{
					if (column != 20 || row != 20)
					{
						map.Improvements[column, row] = SettlerPlanner.MineBit | SettlerPlanner.RoadBit;
					}
				}
			}

			Point shield = new Point(18, 21);
			Point plain = new Point(22, 19);
			Point bare = new Point(21, 20);
			Assert.True(SettlerPlanner.HasGrasslandShield(shield.X, shield.Y));
			Assert.False(SettlerPlanner.HasGrasslandShield(bare.X, bare.Y));

			foreach (Point tile in new[] { shield, plain, bare })
			{
				map.Improvements[tile.X, tile.Y] = 0;
			}

			map.Terrain[shield.X, shield.Y] = SettlerPlanner.Grassland;
			map.Terrain[plain.X, plain.Y] = SettlerPlanner.Plains;
			map.Terrain[bare.X, bare.Y] = SettlerPlanner.Grassland;

			List<Point> order = new List<Point>();

			for (int step = 0; step < 3; step++)
			{
				var plan = SettlerPlanner.Plan(SettlerAutomation.ImproveKeepExisting, map, city, new[] { city }, new HashSet<Point>(order), true, false);
				Assert.Equal(SettlerWork.Road, plan!.Value.Work);
				order.Add(plan.Value.Tile);
			}

			Assert.Equal(new[] { shield, plain, bare }, order);

			map.Improvements[shield.X, shield.Y] = SettlerPlanner.RoadBit;
			Assert.Equal(SettlerWork.Irrigate, Desired(SettlerAutomation.ImproveAll, map, shield.X, shield.Y));
		}

		[Fact]
		public void PlanReturnsNullWhenNothingLeft()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Hills);
			Point[] cities = { new Point(10, 10) };

			Assert.Null(SettlerPlanner.Plan(SettlerAutomation.RoadsOnly, map, new Point(10, 10), cities, new HashSet<Point>(), true, false));
		}

		[Fact]
		public void RouteAvoidsOceanAndConnectionLinksAllCities()
		{
			FakeSettlerMap map = new FakeSettlerMap(SettlerPlanner.Grassland);

			for (int row = 0; row < 8; row++)
			{
				map.Terrain[15, row] = MapRules.OceanTerrain;
			}

			List<Point> route = SettlerPlanner.FindRoute(map, new Point(10, 3), new Point(20, 3), 1, true);

			Assert.Equal(new Point(10, 3), route[0]);
			Assert.Equal(new Point(20, 3), route[^1]);
			Assert.DoesNotContain(route, tile => map.Terrain[tile.X, tile.Y] == MapRules.OceanTerrain);
			Assert.All(route.Zip(route.Skip(1)), pair => Assert.Equal(1, SettlerPlanner.Distance(pair.First, pair.Second)));

			Point[] cities = { new Point(10, 3), new Point(20, 3), new Point(10, 20) };
			List<Point> network = SettlerPlanner.ConnectionPath(map, cities, 1, true, false);

			Assert.Contains(new Point(20, 3), network);
			Assert.Contains(new Point(10, 20), network);
		}

		[Fact]
		public void DistanceWrapsAroundTheWorld()
		{
			Assert.Equal(2, SettlerPlanner.Distance(new Point(0, 5), new Point(MapRules.Width - 2, 5)));
			Assert.Equal(21, SettlerPlanner.CityArea(new Point(10, 10)).Count());
		}

		private static SettlerWork Desired(SettlerAutomation mode, FakeSettlerMap map, int column, int row) =>
			SettlerPlanner.DesiredWork(mode, map, column, row, true, false, false);
	}
}
