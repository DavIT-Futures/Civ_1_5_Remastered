using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// A step of a colour fade (ColorsFadeStep, ColorsFadeStepInterrupt) against its original code: random palettes, the first
	/// colour, the number of colours (wrapping past colour 255 and the end of the palette), the mix and its growth (up to the
	/// limit 0xffff); the timer variables (0x62 - 0x6d) are watched
	/// </summary>
	public class BatchSixtySevenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ColorsFadeStepMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				ushort first = (ushort)chance.Next(0, 0x100);
				ushort count = (ushort)(chance.Next(2) == 0 ? 0x100 : chance.Next(1, 0x101));
				ushort mix = (ushort)chance.Next(0, 0x10000);
				ushort growth = (ushort)(chance.Next(3) == 0 ? chance.Next(0xf000, 0x10000) : chance.Next(0, 0x800));
				byte[] from = new byte[0x300], to = new byte[0x300];

				for (int idx = 0; idx < 0x300; idx++)
				{
					from[idx] = (byte)(chance.Next(4) == 0 ? 0 : chance.Next(0, 64));
					to[idx] = (byte)(chance.Next(4) == 0 ? 0 : chance.Next(0, 64));
				}

				bool timer = chance.Next(2) == 0;

				return new Scenario($"{(timer ? "ColorsFadeStepInterrupt" : "ColorsFadeStep")}(), first {first}, count {count}, mix 0x{mix:x4}, growth 0x{growth:x4}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x62, first);
						game.Processor.EmitGlobalWord(0x64, count);
						game.Processor.EmitGlobalWord(0x6a, mix);
						game.Processor.EmitGlobalWord(0x6c, growth);
						game.Processor.EmitGlobalWord(0x68, 0);

						for (int idx = 0; idx < 0x300; idx++)
						{
							game.Processor.EmitGlobalByte((ushort)(0xbd06 + idx), from[idx]);
							game.Processor.EmitGlobalByte((ushort)(0xba06 + idx), to[idx]);
						}
					},
					game => { if (timer) new PlatformServicesReference(game).ColorsFadeStepInterrupt(); else new PlatformServicesReference(game).ColorsFadeStep(); },
					game => { if (timer) game.Services.ColorsFadeStepInterrupt(); else game.Services.ColorsFadeStep(); },
					Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data,
					game => ((first + count) & 0xff) * 3 + (3 * count) > 0x300 ? "wraps" : "inside",
					[(0x3b01, 0x62, 0xc)]);
			}, output, ["wraps", "inside"]);
		}
	}
}
