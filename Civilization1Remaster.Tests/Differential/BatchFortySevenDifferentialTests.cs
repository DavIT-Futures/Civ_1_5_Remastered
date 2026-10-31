using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The numbering of the continents and seas of a new map against its original code</summary>
	public class BatchFortySevenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ProduceTerrainMatchesOriginalCode()
		{
			Check(3, 6, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				byte[] colours = Enumerable.Range(0, 16).Select(colour => (byte)colour).ToArray();
				byte[] ocean = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) == 0xa).ToArray();
				byte[] land = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) != 0xa && engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) < 0xc).ToArray();
				int density = chance.Next(2, 9);
				byte[] dots = Enumerable.Range(0, 80 * 50).Select(_ => chance.Next(10) < density ? land[chance.Next(land.Length)] : ocean[chance.Next(ocean.Length)]).ToArray();
				ushort[] sizes = Enumerable.Range(0, 0x80).Select(_ => (ushort)(chance.Next(3) == 0 ? chance.Next(1, 50) : 0)).ToArray();

				return new Scenario($"ProduceTerrain(), land {density} / 10",
					game =>
					{
						ushort displayIdx = game.Processor.PullGlobalWord(game.Processor.PullGlobalWord(0xaa));

						for (int idx = 0; idx < dots.Length; idx++)
							game.VideoCard.AssignDot(displayIdx, idx % 80, idx / 80, dots[idx], 0);

						for (int idx = 0; idx < sizes.Length; idx++)
							game.Processor.EmitGlobalWord((ushort)(0xdcfe + (idx << 1)), sizes[idx]);

						// The story stays where it is (its next step at 0x7fff, no song): it doesn't depend on the clock of the tests,
						// which ticks with the memory accesses
						game.Processor.EmitGlobalWord(0x3b62, 1);
						game.Processor.EmitGlobalWord(0x6802, 0);
						game.Processor.EmitGlobalWord(0x6806, 0x7fff);
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					game =>
					{
						new WorldBuilderReference(game).ProduceTerrain();
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					game =>
					{
						game.WorldSetup.ProduceTerrain();
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					Register.Kept,
					game => "map");
			}, output, ["map"], fresh: true);
		}
	}
}
