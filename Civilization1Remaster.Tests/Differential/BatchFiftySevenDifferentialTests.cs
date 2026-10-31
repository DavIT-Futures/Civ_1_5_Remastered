using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Making a civilization anew (ConstructCivilization) against its original code
	/// </summary>
	public class BatchFiftySevenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ConstructCivilizationMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				Civilization1Remaster.GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(0, 8);
				bool start = chance.Next(3) == 0;
				short hardness = (short)chance.Next(0, 5);

				// Also exactly at the last round a new civilization may come for the difficulty (350 - 50 * difficulty)
				short latestRound = (short)(0x15e - (0x32 * hardness));
				short round = (short)(start ? 0 : chance.Next(3) == 0 ? latestRound + chance.Next(-1, 2) : chance.Next(1, 400));
				bool alive = chance.Next(4) != 0;
				ushort earth = (ushort)chance.Next(2);
				short identity = (short)chance.Next(0, 0x100);
				short season = (short)(chance.Next(2) == 0 ? chance.Next(-4000, 0) : chance.Next(1, 2000));
				bool person = participantIdx == engine.World.PersonParticipantIdx;

				// Barbarian units (player 0) all over the map: those within 8 tiles of the start place are removed
				(byte Horiz, byte Vert)[] barbarians = new (byte, byte)[0x30];

				for (int idx = 0; idx < barbarians.Length; idx++)
					barbarians[idx] = ((byte)chance.Next(4, 76), (byte)chance.Next(4, 46));

				return new Scenario($"ConstructCivilization({participantIdx}), round {round}, alive {alive}, EARTH {earth}, " +
					$"difficulty {hardness}, identity 0x{identity:x2}, year {season}",
					game =>
					{
						game.World.RoundTally = round;
						game.World.HardnessTier = hardness;
						game.World.ParticipantIdentMarkers = identity;
						game.World.Season = season;
						game.Processor.EmitGlobalWord(0xd76a, earth);

						for (int idx = 0; idx < barbarians.Length; idx++)
						{
							ushort troop = (ushort)(0x81d4 + (0xc * (0x40 + idx)));

							game.Processor.EmitGlobalByte(troop, 0);
							game.Processor.EmitGlobalByte((ushort)(troop + 1), barbarians[idx].Horiz);
							game.Processor.EmitGlobalByte((ushort)(troop + 2), barbarians[idx].Vert);
							game.Processor.EmitGlobalByte((ushort)(troop + 3), 2);
						}

						if (alive)
							game.World.LiveRealms |= (short)(1 << participantIdx);
						else
							game.World.LiveRealms &= (short)~(1 << participantIdx);
					},
					game => new NewGameSetupReference(game).ConstructCivilization(participantIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.NewGameWizard.ConstructCivilization(participantIdx)),
					// (the human in a running game: the original left AX as it was, nobody reads it; the rewritten code returns 0)
					person && round != 0 ? Register.Kept : Register.Kept | Register.Acc,
					game => person && round != 0 ? "human in a running game" :
						participantIdx == 0 || !alive ? "wiped only" :
						game.Processor.RegAcc.Value16 == 0 ? "does not come" :
						person ? "human at the start" : round == 0 ? (earth != 0 ? "made, EARTH" : "made at the start") : "made in a running game");
			}, output, ["human in a running game", "wiped only", "does not come", "made at the start", "made, EARTH", "made in a running game"]);
		}
	}
}
