using System.Drawing;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SentryRulesTests
	{
		[Fact]
		public void EnemyNextToTheSentryWakesIt()
		{
			Assert.True(SentryRules.ShouldWake(new Point(10, 10), false, new HashSet<Point> { new Point(11, 11) }));
		}

		[Fact]
		public void DistantEnemyLetsItSleep()
		{
			Assert.False(SentryRules.ShouldWake(new Point(10, 10), false, new HashSet<Point> { new Point(12, 10) }));
		}

		[Fact]
		public void CargoAboardShipKeepsSleeping()
		{
			Assert.False(SentryRules.ShouldWake(new Point(10, 10), true, new HashSet<Point> { new Point(10, 11) }));
		}

		[Fact]
		public void EnemyAcrossTheDateLineCounts()
		{
			Assert.True(SentryRules.ShouldWake(new Point(0, 20), false, new HashSet<Point> { new Point(MapRules.Width - 1, 20) }));
		}
	}
}
