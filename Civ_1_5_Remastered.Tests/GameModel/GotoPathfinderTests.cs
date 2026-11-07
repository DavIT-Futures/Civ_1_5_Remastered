using System.Drawing;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class GotoPathfinderTests
	{
		[Fact]
		public void TakesTheStraightRouteOnOpenGround()
		{
			List<Point> route = GotoPathfinder.FindRoute(new Point(10, 10), new Point(15, 10), (from, to) => GotoPathfinder.MovePointCost);

			Assert.Equal(5, route.Count);
			Assert.All(route, tile => Assert.Equal(10, tile.Y));
			Assert.Equal(new Point(15, 10), route[^1]);
		}

		[Fact]
		public void FollowsRailroadDetourInsteadOfCrossingMountains()
		{
			HashSet<Point> rail = new HashSet<Point>();

			for (int column = 10; column <= 20; column++)
			{
				rail.Add(new Point(column, 14));
			}

			rail.Add(new Point(10, 10));
			rail.Add(new Point(10, 11));
			rail.Add(new Point(10, 12));
			rail.Add(new Point(10, 13));
			rail.Add(new Point(20, 13));
			rail.Add(new Point(20, 12));
			rail.Add(new Point(20, 11));
			rail.Add(new Point(20, 10));

			int Cost(Point from, Point to) => rail.Contains(from) && rail.Contains(to)
				? GotoPathfinder.LandStepCost(0x18, 0x18, true, 3)
				: GotoPathfinder.LandStepCost(0, 0, false, 3);

			List<Point> route = GotoPathfinder.FindRoute(new Point(10, 10), new Point(20, 10), Cost);

			Assert.All(route, tile => Assert.Contains(tile, rail));
		}

		[Fact]
		public void PrefersRoadsAndAvoidsImpassableTiles()
		{
			int Cost(Point from, Point to) => to.X == 12 && to.Y != 20 ? GotoPathfinder.Impassable : GotoPathfinder.MovePointCost;

			List<Point> route = GotoPathfinder.FindRoute(new Point(10, 18), new Point(14, 18), Cost);

			Assert.Contains(new Point(12, 20), route);
			Assert.Equal(new Point(14, 18), route[^1]);

			List<Point> around = GotoPathfinder.FindRoute(new Point(10, 18), new Point(14, 18), (from, to) => to.X == 12 ? GotoPathfinder.Impassable : 30);
			Assert.DoesNotContain(around, tile => tile.X == 12);
			Assert.True(around.Count > MapRules.Width / 2);

			Assert.Empty(GotoPathfinder.FindRoute(new Point(10, 18), new Point(14, 18), (from, to) => to.X is 12 or 40 ? GotoPathfinder.Impassable : 30));
		}

		[Fact]
		public void StepCostsFollowMovementRules()
		{
			Assert.Equal(GotoPathfinder.RailroadCost, GotoPathfinder.LandStepCost(0x18, 0x18, true, 3));
			Assert.Equal(GotoPathfinder.RoadCost, GotoPathfinder.LandStepCost(0x8, 0x8, false, 3));
			Assert.Equal(3 * GotoPathfinder.MovePointCost, GotoPathfinder.LandStepCost(0x8, 0, false, 3));
		}

		[Fact]
		public void TriremeRouteHugsTheCoastAndStopsWhereItEnds()
		{
			// Land: a coast along row 10 from column 10 to 30, plus a bay that cuts deep into the sea.
			HashSet<Point> land = new HashSet<Point>();

			for (int column = 10; column <= 30; column++)
			{
				land.Add(new Point(column, 10));
			}

			bool IsLand(Point tile) => land.Contains(tile);

			int Cost(Point from, Point to) => !IsLand(to) && GotoPathfinder.NextToLand(to, IsLand) ? GotoPathfinder.MovePointCost : GotoPathfinder.Impassable;

			List<Point> route = GotoPathfinder.FindRoute(new Point(10, 11), new Point(30, 11), Cost);

			Assert.NotEmpty(route);
			Assert.All(route, tile => Assert.True(GotoPathfinder.NextToLand(tile, IsLand)));

			Assert.Empty(GotoPathfinder.FindRoute(new Point(10, 11), new Point(20, 20), Cost));
			Assert.True(ShipRules.MustHugCoast(ShipRules.Trireme));
			Assert.False(ShipRules.MustHugCoast(ShipRules.Sail));
		}

		[Fact]
		public void RouteWrapsAroundTheWorld()
		{
			List<Point> route = GotoPathfinder.FindRoute(new Point(1, 5), new Point(MapRules.Width - 2, 5), (from, to) => 30);

			Assert.Equal(3, route.Count);
			Assert.Equal(new Point(0, 5), route[0]);
		}
	}
}
