using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// A beat of the timer (OnClockBeat) against its original code, called directly: the rate 4 with its skip counter, other
	/// rates; the timer variables (left out of the snapshot) are watched. The music step of the emulator is always 0, so the
	/// speed changes after it are not reached.
	/// </summary>
	public class BatchSixtySixDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void OnClockBeatMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort rate = (ushort)(chance.Next(2) == 0 ? 4 : chance.Next(1, 8));
				byte skip = (byte)(chance.Next(3) == 0 ? chance.Next(0x7e, 0x82) : chance.Next(0, 9));

				return new Scenario($"OnClockBeat(), rate {rate}, skip 0x{skip:x2}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x4e, rate);
						game.Processor.EmitGlobalByte(0x5e, skip);
					},
					game => new PlatformServicesReference(game).OnClockBeat(),
					game => game.Services.OnClockBeat(),
					Register.Kept,
					game => rate != 4 ? "music" : (sbyte)skip - 1 > 0 ? "music" : "skipped",
					[(0x3b01, 0x42, 0x1e)]);
			}, output, ["music", "skipped"]);
		}
	}
}
