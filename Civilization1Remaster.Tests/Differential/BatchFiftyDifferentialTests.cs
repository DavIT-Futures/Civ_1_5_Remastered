using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Drawing the mouse pointer and putting back the screen under it against their original code
	/// </summary>
	public class BatchFiftyDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void CursorScreenMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				byte drawn = (byte)chance.Next(2);
				byte latch = (byte)chance.Next(256);
				string state = $"drawn {drawn}, latch {latch}";

				void Setup(Civilization1Remaster.GameEngine game)
				{
					game.Processor.EmitGlobalByte(0x5403, drawn);
					game.Processor.EmitGlobalByte(0x5402, latch);
				}

				string Latch(Civilization1Remaster.GameEngine game) => drawn != 0 ? "latch cleared" : "latch kept";

				if (chance.Next(2) == 0)
				{
					return new Scenario($"RecoverScreenUnderCursor(), {state}", Setup,
						game => new PlatformServicesReference(game).RecoverScreenUnderCursor(),
						game => game.Services.RecoverScreenUnderCursor(),
						Register.Kept, game => "recover, " + Latch(game));
				}

				short horizLoc = (short)chance.Next(-16, 320), vertLoc = (short)chance.Next(-16, 200);
				ushort picture = (ushort)chance.Next(0x400);

				return new Scenario($"RenderCursorToScreen({horizLoc}, {vertLoc}, {picture}), {state}", Setup,
					game => new PlatformServicesReference(game).DrawCursorToScreen(horizLoc, vertLoc, picture),
					game => game.Services.DrawCursorToScreen(horizLoc, vertLoc, picture),
					Register.Kept, game => "render, " + Latch(game));
			}, output, ["recover, latch cleared", "recover, latch kept", "render, latch cleared", "render, latch kept"]);
		}
	}
}
