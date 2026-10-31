using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The top cities, the pictures of the city view, the graph of the civilizations and the table of units against their
	/// original code
	/// </summary>
	public class BatchTwentyEightDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>Wonders given to random cities (or none)</summary>
		private static short[] RandomWonders(Random chance, GameEngine engine, short[] towns, int wonderChance)
		{
			return Enumerable.Range(0, 22).Select(_ => chance.Next(10) < wonderChance ? towns[chance.Next(towns.Length)] : (short)-1).ToArray();
		}

		private static short[] Towns(GameEngine engine)
		{
			return Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
		}

		[Fact]
		public void DisplayTopCitiesMatchesOriginalCode()
		{
			Check(3, 12, (chance, pair) =>
			{
				short[] wonders = RandomWonders(chance, pair.Original.Engine, Towns(pair.Original.Engine), chance.Next(0, 6));

				return new Scenario($"DisplayTopCities(), wonders {string.Join(" ", wonders)}",
					game => wonders.CopyTo(game.World.MarvelTownIdx, 0),
					game => new FameRecordsReference(game).DisplayTopCities(),
					game => game.Legends.DisplayTopCities(),
					Register.Kept,
					game => wonders.Skip(1).Any(townIdx => townIdx != -1) ? "wonders" : "no wonders");
			}, output, ["wonders", "no wonders"]);
		}

		[Fact]
		public void RenderCityBuildingsMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				short[] towns = Towns(pair.Original.Engine);
				short townIdx = towns[chance.Next(towns.Length)];
				short participantIdx = (short)chance.Next(8);
				ushort arg1 = (ushort)chance.Next(0, 24);
				ushort structures0 = (ushort)chance.Next(0x10000), structures1 = (ushort)chance.Next(0x10000);
				ushort[] science = Enumerable.Range(0, 5).Select(_ => (ushort)chance.Next(0x10000)).ToArray();
				bool built = chance.Next(3) != 0;
				short[] wonders = Enumerable.Range(0, 22).Select(_ => built && chance.Next(4) == 0 ? townIdx : (short)-1).ToArray();

				return new Scenario($"RenderCityBuildings({arg1}, {participantIdx}, {townIdx}), buildings 0x{structures0:x4} 0x{structures1:x4}",
					game =>
					{
						game.World.Towns[townIdx].StructureMarkers0 = structures0;
						game.World.Towns[townIdx].StructureMarkers1 = structures1;
						science.CopyTo(game.World.Participants[participantIdx].FoundScienceMarkers, 0);
						wonders.CopyTo(game.World.MarvelTownIdx, 0);
					},
					game => new CityPanoramaReference(game).RenderCityBuildings(arg1, participantIdx, townIdx),
					game => game.CityScene.RenderCityBuildings(arg1, participantIdx, townIdx),
					Register.Kept,
					game => Enumerable.Range(1, 21).Any(idx => wonders[idx] == townIdx && ((1 << idx) & 0x808a) != 0) ? "wonder in the mask" :
						Enumerable.Range(1, 21).Any(idx => wonders[idx] == townIdx) ? "other wonder" : "no wonder");
			}, output, ["wonder in the mask", "other wonder", "no wonder"]);
		}

		[Fact]
		public void DisplayCivilizationGraphMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				short rounds = (short)(chance.Next(5) == 0 ? chance.Next(0, 4) : chance.Next(0, 700));
				byte[] history = Enumerable.Range(0, 0x4b8).Select(_ => (byte)(chance.Next(3) == 0 ? chance.Next(256) : chance.Next(40))).ToArray();
				short realms = (short)chance.Next(0x200);
				short rivals = (short)chance.Next(1, 8);

				return new Scenario($"DisplayCivilizationGraph(), turn {rounds}, living 0x{realms:x3}, rivals {rivals}",
					game =>
					{
						game.World.RoundTally = rounds;
						game.World.LiveRealms = realms;
						game.World.BotRivalTally = rivals;

						for (int idx = 0; idx < history.Length; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)idx, history[idx]);
					},
					game => new WorldStatisticsReference(game).DisplayCivilizationGraph(),
					game => game.Atlas.DisplayCivilizationGraph(),
					Register.Kept,
					game => rounds > 600 ? "long game" : rounds >= 4 ? "game" : "start",
					[(0x3772, 0, 0x4b8)]);
			}, output, ["long game", "game", "start"]);
		}

		[Fact]
		public void DisplayCivilizationScoreListMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				// Units of at most 20 types: with more the rows go below the screen and both codes throw
				ushort shown = (ushort)chance.Next(2);
				int[] kinds = Enumerable.Range(0, 28).OrderBy(_ => chance.Next()).Take(chance.Next(0, 21)).ToArray();
				short[][] units = Enumerable.Range(0, 16).Select(_ => Enumerable.Range(0, 28)
					.Select(kind => (short)(kinds.Contains(kind) && chance.Next(3) == 0 ? chance.Next(0, 30) : 0)).ToArray()).ToArray();

				return new Scenario($"DisplayCivilizationScoreList(), 0xd806 {shown}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd806, shown);

						for (int participantIdx = 0; participantIdx < 8; participantIdx++)
						{
							units[participantIdx].CopyTo(game.World.Participants[participantIdx].LiveTroops, 0);
							units[participantIdx + 8].CopyTo(game.World.Participants[participantIdx].TroopsInsideMaking, 0);
						}
					},
					game => new RivalOverviewReference(game).DisplayCivilizationScoreList(),
					game => game.RealmInfo.DisplayCivilizationScoreList(),
					Register.Kept,
					game => shown != 0 ? "all columns" : "human column");
			}, output, ["all columns", "human column"]);
		}
	}
}
