using System;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The story told while the world is made (DisplayStoryIntro, called again and again) against its original code
	/// </summary>
	public class BatchFiftyFourDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayStoryIntroMatchesOriginalCode()
		{
			ushort? cycleRecords = null;

			Check(3, 60, (chance, pair) =>
			{
				// The first colour cycle record of the story pictures, as the game starts with it
				cycleRecords ??= pair.Original.Engine.Processor.Storage.PullUInt16(0x3b01, 0x3b66);

				ushort stage = (ushort)(chance.Next(3) == 0 ? chance.Next(2, 7) : 0);
				ushort colours = (ushort)chance.Next(2);
				ushort keyEnds = (ushort)(chance.Next(3) == 0 ? 1 : 0);
				int steps = stage != 0 ? 1 : chance.Next(3) == 0 ? 80 : chance.Next(1, 30);
				ushort records = cycleRecords.Value;

				int pictures = Calls(pair.Rewritten.Engine, pair, "ImportPictureOrColors");

				// Like the map generator between the steps: the music (one position a timer beat) goes on while it waits
				void Tell(Civilization1Remaster.GameEngine game, Action step)
				{
					for (int call = 0, told = 0; call < 40000 && told < steps; call++)
					{
						step();

						if (game.Processor.RegAcc.Value16 == 1)
							game.Processor.Idle();
						else
							told++;
					}
				}

				return new Scenario($"DisplayStoryIntro() {steps} steps, stage {stage}, VGA {colours}, key ends {keyEnds}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x3b62, stage);
						game.Processor.EmitGlobalWord(0x3b64, 0);
						game.Processor.EmitGlobalWord(0x3b66, records);
						game.Processor.EmitGlobalWord(0x3b68, 0);
						game.Processor.EmitGlobalWord(0xd762, colours);
						game.Processor.EmitGlobalWord(0x67fc, keyEnds);

						// The scripted keyboard gives a key at every second poll (and the story never reads them): with the queue
						// empty right after a given key, the first poll of the step finds no key
						lock (game.VideoCard.VideoLatch)
							game.VideoCard.Tokens.Clear();

						if (game.Libc.KeyAvailable() == 0)
							game.Libc.KeyAvailable();

						lock (game.VideoCard.VideoLatch)
							game.VideoCard.Tokens.Clear();
					},
					game => Tell(game, () => new GameInitAndIntroReference(game).DisplayStoryIntro()),
					game => Tell(game, () => game.WorldSetup.DisplayStoryIntro()),
					Register.Kept | Register.Acc,
					game => stage != 0 ? $"told, stage {Math.Min((int)game.Processor.Storage.PullUInt16(0x3b01, 0x3b62), 5)}" :
						(Calls(game, pair, "ImportPictureOrColors") > pictures ? (colours != 0 ? "picture VGA" : "picture EGA") : "lines") +
						(game.Processor.Storage.PullUInt16(0x3b01, 0x3b62) == 2 ? (keyEnds != 0 ? ", ended by a key" : ", ended") : ""));
			}, output, ["told, stage 5", "picture VGA, ended", "picture EGA, ended", "picture VGA, ended by a key", "picture EGA, ended by a key"]);
		}
	}
}
