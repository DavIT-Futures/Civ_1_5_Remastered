using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The parades of the city view against their original code (the scripted keyboard ends them after two frames)</summary>
	public class BatchThirtySevenDifferentialTests(ITestOutputHelper output)
	{
		private static void SetTechnology(GameEngine game, int participantIdx, int scienceIdx, bool known)
		{
			ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
			science[scienceIdx >> 4] = (ushort)((science[scienceIdx >> 4] & ~(1 << (scienceIdx & 0xf))) | (known ? 1 << (scienceIdx & 0xf) : 0));
		}

		[Fact]
		public void RenderCitySceneryForPlayerMatchesOriginalCode()
		{
			Check(3, 12, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				bool[] technologies = { chance.Next(2) == 0, chance.Next(2) == 0 };
				ushort colours = (ushort)chance.Next(2);

				return new Scenario($"RenderCitySceneryForPlayer({participantIdx}), technologies {string.Join(" ", technologies)}, colours {colours}",
					game =>
					{
						SetTechnology(game, participantIdx, 0x22, technologies[0]);
						SetTechnology(game, participantIdx, 0x3a, technologies[1]);
						game.Processor.EmitGlobalWord(0xd762, colours);
					},
					game => new CityPanoramaReference(game).RenderCitySceneryForPlayer(participantIdx),
					game => game.CityScene.RenderCitySceneryForPlayer(participantIdx),
					Register.Kept,
					game => !technologies[0] ? "first pictures" : technologies[1] ? "last pictures" : "middle pictures");
			}, output, ["first pictures", "middle pictures", "last pictures"], [0x0d]);
		}

		[Fact]
		public void RenderCitySceneryForHumanMatchesOriginalCode()
		{
			Check(3, 8, (chance, pair) =>
			{
				bool known = chance.Next(2) == 0;
				// The columns of the 4 figures (0x4f42): the parade starts at 0x1a4 and ends after two frames, so they are moved
				// onto the screen (3 * column / 2 + 0x1a4)
				short[] columns = Enumerable.Range(0, 4).Select(_ => (short)chance.Next(-330, -40)).ToArray();

				return new Scenario($"RenderCitySceneryForHuman(), technology 0x23 {known}, columns {string.Join(" ", columns)}",
					game =>
					{
						SetTechnology(game, game.World.PersonParticipantIdx, 0x23, known);

						for (int idx = 0; idx < 4; idx++)
							game.Processor.EmitGlobalWord((ushort)(0x4f42 + (idx << 1)), (ushort)columns[idx]);
					},
					game => new CityPanoramaReference(game).RenderCitySceneryForHuman(),
					game => game.CityScene.RenderCitySceneryForHuman(),
					Register.Kept,
					game => known ? "later pictures" : "first pictures");
			}, output, ["first pictures", "later pictures"], [0x0d]);
		}
	}
}
