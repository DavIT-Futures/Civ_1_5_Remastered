using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The disasters of a city and the rating of the start locations against their original code</summary>
	public class BatchFortyFiveDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void HandleCityDisastersMatchesOriginalCode()
		{
			Check(3, 250, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				ushort structures = (ushort)chance.Next(0x10000);
				sbyte size = (sbyte)chance.Next(4, 22);
				bool seen = chance.Next(2) == 0;
				int[] before = new[] { "DisplayTextDialog", "HandleCity", "DisplayDialog", "ObtainCityLabel" }.Select(name => Calls(pair.Rewritten.Engine, pair, name)).ToArray();

				return new Scenario($"HandleCityDisasters({townIdx}), structures 0x{structures:x4}, size {size}, seen {seen}",
					game =>
					{
						City town = game.World.Towns[townIdx];
						town.StructureMarkers0 = structures;
						town.TrueExtent = size;
						game.World.ChartSight[town.Place.X, town.Place.Y] = (sbyte)((game.World.ChartSight[town.Place.X, town.Place.Y] & ~(1 << person)) | (seen ? 1 << person : 0));
					},
					game => new CityFoundingAndScoreReference(game).HandleCityDisasters(townIdx),
					game => game.Founding.HandleCityDisasters(townIdx),
					Register.Kept,
					game => Calls(game, pair, "HandleCity") > before[1] ? (Calls(game, pair, "ObtainCityLabel") > before[3] + 1 ? "demand" : "revolt") : Calls(game, pair, "DisplayTextDialog") > before[0] ? "disaster" :
						Calls(game, pair, "DisplayDialog") > before[2] ? "message" : "quiet");
			}, output, ["demand", "revolt", "disaster", "message", "quiet"], [0x0d]);
		}

		[Fact]
		public void CharacterizeStartLocationMatchesOriginalCode()
		{
			Check(3, 12, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				byte[] alive = Enumerable.Range(0, 8).Select(_ => (byte)(chance.Next(5) == 0 ? 0xff : 0)).ToArray();
				(int, int)[] places = Enumerable.Range(0, 8).Select(_ => RandomTile(engine, chance, true)).ToArray();
				int learned = Calls(pair.Rewritten.Engine, pair, "UncoverTechnology");

				return new Scenario($"CharacterizeStartLocation(), first units {string.Join(" ", alive.Select(kind => kind == 0xff ? "-" : "+"))}",
					game =>
					{
						game.World.RoundTally = 0;

						for (int participantIdx = 1; participantIdx < 8; participantIdx++)
						{
							ushort troop = (ushort)(0x81d4 + (0x600 * participantIdx));
							game.Processor.EmitGlobalByte((ushort)(troop + 3), alive[participantIdx]);
							game.Processor.EmitGlobalByte((ushort)(troop + 1), (byte)places[participantIdx].Item1);
							game.Processor.EmitGlobalByte((ushort)(troop + 2), (byte)places[participantIdx].Item2);
						}
					},
					game => new StartGameMenuReference(game).CharacterizeStartLocation(),
					game => game.NewGameWizard.CharacterizeStartLocation(),
					Register.Acc | Register.Kept,
					game => Calls(game, pair, "UncoverTechnology") > learned ? "compensated" : "even");
			}, output, ["compensated"]);
		}
	}
}
