using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The demographics (DisplayCensus) against its original code: any player, the relations that show the best players,
	/// the numbers of units and technologies (the annual income divides first for many technologies), the living players
	/// </summary>
	public class BatchSixtyTwoDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayCensusMatchesOriginalCode()
		{
			Check(3, 16, (chance, pair) =>
			{
				Civilization1Remaster.GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)(chance.Next(2) == 0 ? engine.World.PersonParticipantIdx : chance.Next(1, 8));
				short living = (short)(chance.Next(0x100) | 1 | (1 << engine.World.PersonParticipantIdx));
				ushort[] relations = new ushort[8];
				short[] troops = new short[8];
				short[] sciences = new short[8];

				// The pollution of the population (0 in the saved games before industry)
				ushort populationPollution = (ushort)chance.Next(0, 4);

				for (int idx = 0; idx < 8; idx++)
				{
					relations[idx] = (ushort)(chance.Next(2) == 0 ? 0x40 : 0);
					troops[idx] = (short)chance.Next(0, 120);
					sciences[idx] = (short)(chance.Next(3) == 0 ? chance.Next(40, 0x48) : chance.Next(0, 20));
				}

				return new Scenario($"DisplayCensus({participantIdx}), living 0x{living:x2}, 0xb882 {populationPollution}",
					game =>
					{
						game.World.LiveRealms = living;
						game.Processor.EmitGlobalWord(0xb882, populationPollution);

						for (int idx = 0; idx < 8; idx++)
						{
							game.World.Participants[game.World.PersonParticipantIdx].Relations[idx] =
								(ushort)((game.World.Participants[game.World.PersonParticipantIdx].Relations[idx] & ~0x40) | relations[idx]);
							game.World.Participants[idx].TroopTally = troops[idx];
							game.World.Participants[idx].FoundScienceTally = sciences[idx];
						}
					},
					game => new WorldMapReference(game).DisplayCensus(participantIdx),
					game => game.Atlas.DisplayCensus(participantIdx),
					Register.Kept,
					game => participantIdx == engine.World.PersonParticipantIdx ? "human" : "other");
			}, output, ["human", "other"], [0x0d]);
		}
	}
}
