using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The citizen icons against their original code</summary>
	public class BatchThirtyEightDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderCitizenIconsMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				short horizLoc = (short)chance.Next(0, 200), vertLoc = (short)chance.Next(10, 180);
				sbyte size = (sbyte)chance.Next(1, 25);
				ushort specialists = (ushort)chance.Next(0, 5);
				ushort breadth = (ushort)(chance.Next(2) == 0 ? chance.Next(20, 200) : 0xffff);
				ushort happy = (ushort)chance.Next(0, 8), unhappy = (ushort)chance.Next(0, 8), marked = (ushort)chance.Next(0, 5);
				// The frame word 0x2 of the function (never written by it) under the stack pointer
				ushort leftover = (ushort)(chance.Next(2) == 0 ? 0 : chance.Next(1, 0x10000));
				ushort sp = engine.Processor.RegStack.Value16;

				return new Scenario($"RenderCitizenIcons({townIdx}, {horizLoc}, {vertLoc}, {specialists}, {breadth}), size {size}, happy {happy}, " +
					$"unhappy {unhappy} ({marked} marked), frame word 0x{leftover:x4}",
					game =>
					{
						game.World.Towns[townIdx].TrueExtent = size;
						game.Processor.EmitGlobalWord(0x70e2, happy);
						game.Processor.EmitGlobalWord(0x70e4, unhappy);
						game.Processor.EmitGlobalWord(0x6542, marked);
						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 2), leftover);
					},
					game => new CityManagementReference(game).RenderCitizenIcons(townIdx, horizLoc, vertLoc, specialists, breadth),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.CityOps.RenderCitizenIcons(townIdx, horizLoc, vertLoc, specialists, breadth)),
					Register.Acc | Register.Kept,
					game => $"{(7 * size > (short)breadth ? "narrow" : "wide")}{(leftover != 0 ? ", gap" : "")}");
			}, output, ["narrow", "wide", "narrow, gap", "wide, gap"]);
		}
	}
}
