using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The continents screen, the Wonders of the World, a tile of the city and global warming against their original code
	/// </summary>
	public class BatchTwentyFourDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayGameFiguresMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				bool armed = chance.Next(4) != 0;
				short[][] figures = Enumerable.Range(0, 8 * 16).Select(_ => new[]
				{
					(short)(armed && chance.Next(3) == 0 ? chance.Next(0, 300) : 0), (short)chance.Next(0, 300), (short)chance.Next(0, 9),
				}).ToArray();
				ushort[] values = Enumerable.Range(0, 16).Select(_ => (ushort)chance.Next(-50, 3000)).ToArray();

				return new Scenario($"DisplayGameFigures(), armed {armed}",
					game =>
					{
						for (int participantIdx = 0; participantIdx < 8; participantIdx++)
						{
							for (int continentIdx = 0; continentIdx < 16; continentIdx++)
							{
								short[] figure = figures[(participantIdx * 16) + continentIdx];
								game.World.Participants[participantIdx].Landmasses[continentIdx].Assault = figure[0];
								game.World.Participants[participantIdx].Landmasses[continentIdx].Guard = figure[1];
								game.World.Participants[participantIdx].Landmasses[continentIdx].TownTally = figure[2];
							}
						}

						for (int continentIdx = 0; continentIdx < 16; continentIdx++)
							game.Processor.EmitGlobalWord((ushort)(0xdcfe + (continentIdx << 1)), values[continentIdx]);
					},
					game => new WorldStatisticsReference(game).ShowGameFigures(),
					game => game.Atlas.ShowGameFigures(),
					Register.Kept,
					game => armed ? "armies" : "no armies");
			}, output, ["armies", "no armies"]);
		}

		[Fact]
		public void DisplayWondersOfTheWorldMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				// Wonders of existing cities (a destroyed one, city 128, makes both codes read Towns[128] and throw)
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				int builtChance = chance.Next(0, 5);
				short[] wonders = Enumerable.Range(0, 22).Select(_ => chance.Next(5) < builtChance ? towns[chance.Next(towns.Length)] : (short)-1).ToArray();
				int built = wonders.Skip(1).Count(townIdx => townIdx != -1);

				return new Scenario($"DisplayWondersOfTheWorld(), wonders {string.Join(" ", wonders)}",
					game => wonders.CopyTo(game.World.MarvelTownIdx, 0),
					game => new WorldStatisticsReference(game).ShowWondersOfTheWorld(),
					game => game.Atlas.ShowWondersOfTheWorld(),
					Register.Kept,
					game => built > 7 ? "pages" : built > 0 ? "one page" : "none");
			}, output, ["pages", "one page", "none"]);
		}

		[Fact]
		public void RenderCityTileMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				ushort townTile = (ushort)chance.Next(21);
				short marker = (short)(chance.Next(3) == 0 ? 0 : chance.Next(2) == 0 ? 1 : 2);

				// Sometimes the city moved next to the pole: the arctic tiles there yield nothing
				bool polar = chance.Next(4) == 0;
				int horizLoc = engine.World.Towns[townIdx].Place.X;
				int vertLoc = polar ? 2 : engine.World.Towns[townIdx].Place.Y;

				return new Scenario($"RenderCityTile({townIdx}, {townTile}, {marker}), city at {horizLoc}, {vertLoc}",
					game => game.World.Towns[townIdx].Place = new System.Drawing.Point(horizLoc, vertLoc),
					game => new CitiesReference(game).DrawCityTile(townIdx, townTile, marker),
					game => game.CityOps.DrawCityTile(townIdx, townTile, marker),
					Register.Kept,
					game => marker != 1 ? "map layer" : polar && vertLoc + engine.TownDeltas[townTile].Y == 0 ? "polar tile shown" : "shown");
			}, output, ["map layer", "polar tile shown", "shown"]);
		}

		[Fact]
		public void EnactGlobalWarmingMatchesOriginalCode()
		{
			Check(3, 8, (chance, pair) =>
			{
				short tally = (short)chance.Next(0, 8);

				return new Scenario($"EnactGlobalWarming({tally})", game => { },
					game => new WorldBuilderReference(game).EnactGlobalWarming(tally),
					game => game.WorldSetup.EnactGlobalWarming(tally),
					Register.Kept,
					game => tally < 3 ? "coasts" : "more");
			}, output, ["coasts", "more"]);
		}
	}
}
