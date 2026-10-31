using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Showing a screen buffer and drawing a picture with the pointer latch against their original code
	/// </summary>
	public class BatchFortyNineDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayScreenAndAlternatePictureMatchOriginalCode()
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
					ushort displayIdx = (ushort)chance.Next(3);

					return new Scenario($"DisplayScreen({displayIdx}), {state}", Setup,
						game => new PlatformServicesReference(game).ShowScreen(displayIdx),
						game => game.Services.ShowScreen(displayIdx),
						Register.Kept, game => "screen, " + Latch(game));
				}

				int horizLoc = chance.Next(-20, 300), vertLoc = chance.Next(-20, 190);
				ushort source = (ushort)chance.Next(0, 280), sourceVert = (ushort)chance.Next(0, 160);
				ushort picture = 0;

				return new Scenario($"RenderPictureToScreenAlternate(0xaa box, {horizLoc}, {vertLoc}) of ({source}, {sourceVert}), {state}",
					game =>
					{
						Setup(game);
						picture = game.VideoCard.ScreenToPicture(1, source, sourceVert, 0x2c, 0x2c);
					},
					game => new PlatformServicesReference(game).DrawPictureToScreenAlternate(game.Processor.PullGlobalWord(0xaa), horizLoc, vertLoc, picture),
					game => game.Services.DrawPictureToScreenAlternate(game.Processor.PullGlobalWord(0xaa), horizLoc, vertLoc, picture),
					Register.Kept, game => "picture, " + Latch(game));
			}, output, ["screen, latch cleared", "screen, latch kept", "picture, latch cleared", "picture, latch kept"]);
		}
	}
}
