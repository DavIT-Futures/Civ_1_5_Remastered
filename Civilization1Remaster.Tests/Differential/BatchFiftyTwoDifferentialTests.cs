using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Starting, stopping and calibrating the timer and setting a colour cycle against their original code
	/// </summary>
	public class BatchFiftyTwoDifferentialTests(ITestOutputHelper output)
	{
		// The timer variables (left out of the snapshot, GameSnapshot.IgnoredDataRanges)
		private static readonly (ushort, ushort, int)[] Timer = [(0x3b01, 0x42, 0x1a)];

		// The colour cycle records
		private static readonly (ushort, ushort, int)[] Cycles = [(0x35cf, 0x0, 0x800)];

		private const Register All = Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data;

		/// <summary>
		/// The calibration runs before the clock is allowed (LaunchTimer at the start of the game), so its memory accesses
		/// are not clock steps
		/// </summary>
		private static void WithoutClock(Civilization1Remaster.GameEngine game, System.Action call)
		{
			game.Processor.AllowClock = false;
			call();
			game.Processor.AllowClock = true;
		}

		[Fact]
		public void TimerAndColourCycleMatchOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				byte running = (byte)(chance.Next(2) == 0 ? 0 : chance.Next(1, 256));
				ushort rate = (ushort)(chance.Next(2) == 0 ? 1 : chance.Next(2, 9));
				uint time = (uint)chance.Next() * 3;

				void Setup(Civilization1Remaster.GameEngine game)
				{
					game.Processor.EmitGlobalByte(0x5a, running);
					game.Processor.EmitGlobalWord(0x4c, rate);
					game.Processor.EmitUInt32(0x3b01, 0x42, time);
				}

				string state = $"running {running}, 0x4c {rate}, time 0x{time:x8}";
				string Rate(Civilization1Remaster.GameEngine game) => rate == 1 ? "rate 1" : "rate other";

				switch (chance.Next(4))
				{
					case 0:
						return new Scenario($"LaunchTimer(), {state}", Setup,
							game => WithoutClock(game, () => new PlatformServicesReference(game).LaunchTimer()),
							game => WithoutClock(game, () => game.Services.LaunchTimer()),
							All, game => running != 0 ? "launch running" : "launch stopped", Timer);

					case 1:
						return new Scenario($"HaltTimer(), {state}", Setup,
							game => new PlatformServicesReference(game).HaltTimer(),
							game => game.Services.HaltTimer(),
							All, game => "halt", Timer);

					case 2:
						return new Scenario($"PrepareTimer(), {state}", Setup,
							game => WithoutClock(game, () => new PlatformServicesReference(game).PrepareTimer()),
							game => WithoutClock(game, () => game.Services.PrepareTimer()),
							All, game => "prepare " + Rate(game), Timer);

					default:
						ushort cycle = (ushort)chance.Next(0, 10);
						ushort delay = (ushort)chance.Next(1, 301);
						ushort first = (ushort)chance.Next(0, 240);
						ushort last = (ushort)(chance.Next(4) == 0 ? first - chance.Next(0, 3) : first + chance.Next(1, 16));

						return new Scenario($"AssignHueCycleSpan({cycle}, {delay}, {first}, {last})", game => { },
							game => new PlatformServicesReference(game).AssignHueCycleSpan(cycle, delay, first, last),
							game => game.Services.AssignHueCycleSpan(cycle, delay, first, last),
							All, game => cycle < 1 || cycle > 8 ? "cycle out of range" : last <= first ? "empty span" : "span", Cycles);
				}
			}, output, ["launch running", "launch stopped", "halt", "prepare rate 1", "prepare rate other", "cycle out of range", "empty span", "span"]);
		}
	}
}
