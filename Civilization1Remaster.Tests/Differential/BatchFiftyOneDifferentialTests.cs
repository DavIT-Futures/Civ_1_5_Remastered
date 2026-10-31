using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The colour fades (to a palette, to one colour, the start of a fade) against their original code
	/// </summary>
	public class BatchFiftyOneDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ColourFadesMatchOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				// The measured speed (0 = measure now, the game always measures 65530): below 0xe00 fewer than 256 colours a beat,
				// then the interrupt (AssignColors) runs past colour 255 and throws wherever the timer beats
				ushort speed = (ushort)(chance.Next(3) == 0 ? 0 : chance.Next(0xe00, 0x10000));
				ushort steps = (ushort)new[] { 5, 8, 10, 15, 30 }[chance.Next(5)];
				// A colour cycle changes the colours every beat: the beats must come at the same points as in the original
				bool cycling = chance.Next(2) == 0;
				string state = $"speed 0x{speed:x4}, colour cycle {cycling}";

				void Setup(Civilization1Remaster.GameEngine game)
				{
					game.Processor.EmitGlobalWord(0x66, speed);
					game.Processor.EmitGlobalWord(0x68, 0);

					if (cycling)
					{
						game.Services.AssignHueCycleSpan(1, 1, 16, 31);
						game.Services.ActivateHueCycle(1);
					}
					else
					{
						game.Services.DeactivateHueCycle(1);
					}
				}

				string Speed(Civilization1Remaster.GameEngine game) => speed == 0 ? "measured" : "known";

				switch (chance.Next(3))
				{
					case 0:
						byte red = (byte)chance.Next(64), green = (byte)chance.Next(64), blue = (byte)chance.Next(64);

						return new Scenario($"FloodColors({steps}, {red}, {green}, {blue}), {state}", Setup,
							game => new PlatformServicesReference(game).FloodColors(steps, red, green, blue),
							game => game.Services.FloodColors(steps, red, green, blue),
							Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data, game => "flood " + Speed(game));

					case 1:
						ushort swatch = (ushort)(chance.Next(2) == 0 ? 0xc5be : 0xbdee);
						byte[] palette = new byte[0x300];
						chance.NextBytes(palette);

						return new Scenario($"ImportColorsFromBuffer({steps}, 0x{swatch:x4}), {state}",
							game =>
							{
								Setup(game);

								for (int idx = 0; idx < palette.Length; idx++)
									game.Processor.EmitGlobalByte((ushort)(swatch + 6 + idx), (byte)(palette[idx] & 0x3f));
							},
							game => new PlatformServicesReference(game).ImportColorsFromBuffer(steps, swatch),
							game => game.Services.ImportColorsFromBuffer(steps, swatch),
							Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data, game => "import " + Speed(game));

					default:
						return new Scenario($"FadeColors({steps}), {state}", Setup,
							game => new PlatformServicesReference(game).FadeColors(steps),
							game => game.Services.FadeColors(steps),
							Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data, game => "fade " + Speed(game));
				}
			}, output, ["flood measured", "flood known", "import measured", "import known", "fade measured", "fade known"]);
		}
	}
}
