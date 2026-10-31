using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The AI building a spaceship part (ComputeSpaceshipSuccess) against its original code: random layouts (empty, sparse or
	/// nearly full), the three kinds of parts, arrival years that make it start at the modules
	/// </summary>
	public class BatchSixtyFiveDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ComputeSpaceshipSuccessMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				Civilization1Remaster.GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(1, 8);
				short kind = (short)chance.Next(0, 3);
				int fullness = new[] { 0, 3, 10, 30 }[chance.Next(4)];
				byte[] layout = new byte[0xb4];

				for (int idx = 0; idx < layout.Length; idx++)
					layout[idx] = (byte)(chance.Next(32) < fullness ? chance.Next(0, 10) : 0xff);

				short season = engine.World.Season;
				short arrival = (short)(chance.Next(3) == 0 ? -1 : season + chance.Next(1, 25));
				short humanArrival = (short)(chance.Next(2) == 0 ? -1 : season + chance.Next(1, 25));

				// The technology of the part: -1 counts as known (the search gives up 4 places after the first free one)
				bool known = chance.Next(2) == 0;

				return new Scenario($"ComputeSpaceshipSuccess({participantIdx}, {kind}), fullness {fullness}, known {known}, arrival {arrival}, human {humanArrival}",
					game =>
					{
						for (int idx = 0; idx < layout.Length; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(0x16e0 + (0xb4 * participantIdx) + idx), layout[idx]);

						game.World.Participants[participantIdx].StarshipArrivalSeason = arrival;

						if (known)
							game.Processor.EmitGlobalWord((ushort)((0x1e * kind) + 0xe68), 0xffff);

						if (participantIdx != game.World.PersonParticipantIdx)
							game.World.Participants[game.World.PersonParticipantIdx].StarshipArrivalSeason = humanArrival;
					},
					game => new SpaceshipScreenReference(game).ComputeSpaceshipSuccess(participantIdx, kind),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.RocketScreen.ComputeSpaceshipSuccess(participantIdx, kind)),
					Register.Kept | Register.Acc,
					game => $"result {game.Processor.RegAcc.Value16}");
			}, output, ["result 0", "result 1", "result 2"], [0x0d]);
		}
	}
}
