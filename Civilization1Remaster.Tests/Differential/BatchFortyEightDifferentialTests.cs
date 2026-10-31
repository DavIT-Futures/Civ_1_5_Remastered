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
	/// The civil war of a civilization against its original code (only 5 scenarios: splitting the same civilizations again
	/// makes the original loop for ever when no city is left to give, see the backlog)
	/// </summary>
	public class BatchFortyEightDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DivideCivilizationMatchesOriginalCode()
		{
			Check(3, 5, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] owners = Enumerable.Range(1, 7).Select(idx => (short)idx).Where(idx => engine.World.Participants[idx].TownTally > 0).ToArray();

				if (owners.Length == 0)
					return null;

				short participantIdx = owners[chance.Next(owners.Length)];
				short freed = (short)chance.Next(1, 8);
				bool free = freed != participantIdx && chance.Next(4) != 0;
				short rivals = (short)chance.Next(0, 8);

				return new Scenario($"DivideCivilization({participantIdx}), freed slot {(free ? freed : -1)}, rivals {rivals}",
					game =>
					{
						if (free)
						{
							game.World.Participants[freed].TroopTally = 0;
							game.World.Participants[freed].TownTally = 0;
						}
						else
						{
							// No free slot for a new civilization
							for (int slotIdx = 1; slotIdx < 8; slotIdx++)
							{
								if (game.World.Participants[slotIdx].TroopTally == 0 && game.World.Participants[slotIdx].TownTally == 0)
									game.World.Participants[slotIdx].TroopTally = 1;
							}
						}

						game.World.BotRivalTally = rivals;
					},
					game => new CivilWarReference(game).DivideCivilization(participantIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Secession.DivideCivilization(participantIdx)),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 != 0 ? "divided" : "not divided");
			}, output, ["divided", "not divided"], [0x0d]);
		}
	}
}
