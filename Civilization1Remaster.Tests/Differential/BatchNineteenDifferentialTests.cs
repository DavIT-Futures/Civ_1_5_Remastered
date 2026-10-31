using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The World and Advisors menus and the table of destroyed units against their original code</summary>
	public class BatchNineteenDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>Enter takes the line the menu starts at (0x2f9a, set by the scenario), the others close the report</summary>
		private static readonly int[] MenuKeys = { 0x0d, 0x1b, 0x0d, 0x1b, 0x1b };

		private static readonly string[] WorldReports = { "DisplayWondersOfTheWorld", "DisplayTopCities", "DisplayFinalScore",
			"DisplayWorldMap", "DisplayCensus", "ChooseNationToView" };

		private static readonly string[] AdvisorScreens = { "DisplayCityStatusReport", "RenderUnitCountsChart",
			"DisplayCivilizationScoreList", "DisplayMilitaryReport", "DisplayAttitudeReport", "DisplayTradeReport", "DisplayScienceReport" };

		private static string Opened(Civilization1Remaster.GameEngine game, DifferentialPair pair, string[] reports, int[] before)
		{
			for (int idx = 0; idx < reports.Length; idx++)
			{
				if (Calls(game, pair, reports[idx]) > before[idx])
					return reports[idx];
			}

			return "nothing";
		}

		[Fact]
		public void DisplayWorldMenuMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				short markers = (short)(chance.Next(3) == 0 ? 0 : chance.Next(0x10000));
				ushort line = (ushort)chance.Next(6);
				int[] before = WorldReports.Select(report => Calls(pair.Rewritten.Engine, pair, report)).ToArray();

				return new Scenario($"DisplayWorldMenu(), spaceships 0x{(ushort)markers:x4}, line {line}",
					game =>
					{
						game.World.StarshipMarkers = markers;
						game.Processor.EmitGlobalWord(0x2f9a, line);
					},
					game => new MenuBarReference(game).ShowWorldMenu(),
					game => game.MenuSystem.ShowWorldMenu(),
					Register.Kept, game => Opened(game, pair, WorldReports, before), [(0x3772, 0, 0x8000)]);
			}, output, WorldReports, MenuKeys);
		}

		[Fact]
		public void DisplayAdvisorMenuMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort allShown = (ushort)chance.Next(2);
				ushort line = (ushort)(chance.Next(3) == 0 ? 1 : chance.Next(6)); // line 1 has two reports
				int[] before = AdvisorScreens.Select(report => Calls(pair.Rewritten.Engine, pair, report)).ToArray();

				return new Scenario($"DisplayAdvisorMenu(), all units shown {allShown}, line {line}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd806, allShown);
						game.Processor.EmitGlobalWord(0x2f9a, line);
					},
					game => new MenuBarReference(game).ShowAdvisorMenu(),
					game => game.MenuSystem.ShowAdvisorMenu(),
					Register.Kept, game => Opened(game, pair, AdvisorScreens, before));
			}, output, AdvisorScreens, MenuKeys);
		}

		[Fact]
		public void RenderScoreListRecordMatchesOriginalCode()
		{
			Check(3, 12, (chance, pair) =>
			{
				ushort allShown = (ushort)chance.Next(2);
				short[] ruined = Enumerable.Range(0, 64).Select(_ => (short)chance.Next(0, 30)).ToArray();

				return new Scenario($"RenderScoreListRecord(), all units shown {allShown}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd806, allShown);

						for (int idx = 0; idx < 64; idx++)
							game.World.Participants[idx >> 3].TroopsRuined[idx & 7] = ruined[idx];
					},
					game => new RivalOverviewReference(game).DrawScoreListRecord(),
					game => game.RealmInfo.DrawScoreListRecord(),
					Register.Kept, game => allShown != 0 ? "all players" : "the human only");
			}, output, ["all players", "the human only"], [0x0d]);
		}
	}
}
