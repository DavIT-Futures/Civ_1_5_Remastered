using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The spaceship screen (RenderSpaceship) against its original code: random layouts of parts, shown or not, launched or
	/// not, with the keys of the star animation (also "l" and the launch confirmation)
	/// </summary>
	public class BatchSixtyOneDifferentialTests(ITestOutputHelper output)
	{
		[Theory]
		[InlineData(new[] { 0x0d }, "shown")]
		[InlineData(new[] { 0x0d, 0x6c, 0x0d }, "shown")]
		[InlineData(new[] { 0x0d, 0x6c, 0x5000, 0x0d }, "launched now")]
		public void RenderSpaceshipMatchesOriginalCode(int[] script, string branch)
		{
			Check(3, 12, (chance, pair) =>
			{
				Civilization1Remaster.GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(2) == 0 ? person : chance.Next(1, 8));
				bool option = chance.Next(2) == 0;
				bool launched = chance.Next(4) == 0;
				short arg2 = (short)new[] { -1, -1, 15, 3 }[chance.Next(4)];
				short arg3 = (short)chance.Next(2);
				byte[] layout = new byte[0xb4];

				// Sometimes few propulsion (3) and fuel (4) parts: a long flight lowers the chance of success
				bool slow = chance.Next(3) == 0;

				for (int idx = 0; idx < layout.Length; idx++)
				{
					int kind = chance.Next(0, 10);

					if (slow && (kind == 3 || kind == 4) && chance.Next(8) != 0)
						kind = 0;

					layout[idx] = (byte)(chance.Next(3) == 0 ? 0xff : kind);
				}

				int launches = Calls(pair.Rewritten.Engine, pair, "FireSpaceship");

				return new Scenario($"RenderSpaceship({participantIdx}, {arg2}, {arg3}), option {option}, launched {launched}, slow {slow}",
					game =>
					{
						if (option)
							game.World.SessionPrefMarkers |= 0x20;
						else
							game.World.SessionPrefMarkers &= ~0x20;

						if (launched)
							game.World.StarshipMarkers |= (short)(1 << participantIdx);
						else
							game.World.StarshipMarkers &= (short)~(1 << participantIdx);

						for (int idx = 0; idx < layout.Length; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(0x16e0 + (0xb4 * participantIdx) + idx), layout[idx]);
					},
					game => new SpaceshipBayReference(game).RenderSpaceship(participantIdx, arg2, arg3),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.RocketScreen.RenderSpaceship(participantIdx, arg2, arg3)),
					Register.Kept | Register.Acc,
					game => Calls(game, pair, "FireSpaceship") > launches ? "launched now" :
						participantIdx == person || option ? "shown" : "not shown");
			}, output, [branch, "not shown"], script);
		}
	}
}
