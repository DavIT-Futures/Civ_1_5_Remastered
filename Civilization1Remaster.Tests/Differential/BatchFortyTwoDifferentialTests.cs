using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The long way of a unit, the treaties of a player, the palace picture and the sea links of a new map against their
	/// original code
	/// </summary>
	public class BatchFortyTwoDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ChartRouteMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				(short, short)? unit = RandomUnit(pair.Original.Engine, chance);

				if (unit == null)
					return null;

				(short participantIdx, short troopIdx) = unit.Value;
				(int goalHoriz, int goalVert) = RandomTile(pair.Original.Engine, chance);
				int density = chance.Next(0, 9);
				byte[] land = Enumerable.Range(0, 0x104).Select(_ => (byte)Enumerable.Range(0, 8).Sum(bit => chance.Next(10) < density ? 1 << bit : 0)).ToArray();
				byte[] sea = Enumerable.Range(0, 0x104).Select(_ => (byte)Enumerable.Range(0, 8).Sum(bit => chance.Next(10) < density ? 1 << bit : 0)).ToArray();
				// The local 0xa of the frame, compared before it is set when the goal is in the unit's block
				ushort leftover = (ushort)chance.Next(0, 40);
				ushort sp = pair.Original.Engine.Processor.RegStack.Value16;
				int steps = Calls(pair.Rewritten.Engine, pair, "LocateRouteStep");

				return new Scenario($"ChartRoute({participantIdx}, {troopIdx}), goal ({goalHoriz}, {goalVert}), links {density} / 10",
					game =>
					{
						ushort troop = (ushort)(0x81d4 + (0x600 * participantIdx) + (0xc * troopIdx));
						game.Processor.EmitGlobalByte((ushort)(troop + 6), (byte)goalHoriz);
						game.Processor.EmitGlobalByte((ushort)(troop + 7), (byte)goalVert);

						for (int idx = 0; idx < 0x104; idx++)
						{
							game.Processor.EmitGlobalByte((ushort)(0xdb44 + idx), land[idx]);
							game.Processor.EmitGlobalByte((ushort)(0x7f38 + idx), sea[idx]);
						}

						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 0xa), leftover);
					},
					game => new PathfindingReference(game).ChartRoute(participantIdx, troopIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Routing.ChartRoute(participantIdx, troopIdx)),
					Register.Acc | Register.Kept,
					game => Calls(game, pair, "LocateRouteStep") - steps == 1 ? "no step" : game.Processor.RegAcc.Value16 != 0 ? "found" : "not found");
			}, output, ["no step", "found", "not found"]);
		}

		[Fact]
		public void HandlePeaceTreatiesMatchesOriginalCode()
		{
			ushort[] flags = { 0, 1, 2, 3, 4, 0x10, 0x11, 0x20, 0x22, 0x40, 0x41, 0x42, 0x100, 0x101, 0x61 };

			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(4) == 0 ? person : chance.Next(0, 8));
				short regime = (short)(chance.Next(3) == 0 ? 0 : chance.Next(1, 6));
				short round = (short)(chance.Next(3) switch { 0 => 32 * chance.Next(1, 10) + (participantIdx << 2), 1 => 16 * chance.Next(1, 20), _ => chance.Next(1, 400) });
				ushort[,] relations = new ushort[8, 8];

				for (int first = 0; first < 8; first++)
				{
					for (int second = 0; second < 8; second++)
						relations[first, second] = flags[chance.Next(flags.Length)];
				}

				short[] might = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(0, 200)).ToArray();
				short gold = (short)chance.Next(0, 2000);
				int emissaries = Calls(pair.Rewritten.Engine, pair, "WelcomeEmissary");

				return new Scenario($"HandlePeaceTreaties({participantIdx}), government {regime}, turn {round}, might {string.Join(" ", might)}",
					game =>
					{
						game.World.Participants[participantIdx].RegimeKind = regime;
						game.World.Participants[participantIdx].Gold = gold;
						game.World.RoundTally = round;

						for (int first = 0; first < 8; first++)
						{
							game.World.Participants[first].MartialMight = might[first];

							for (int second = 0; second < 8; second++)
								game.World.Participants[first].Relations[second] = relations[first, second];
						}
					},
					game => new GovernmentAndDiplomacyReference(game).HandlePeaceTreaties(participantIdx),
					game => game.Politics.HandlePeaceTreaties(participantIdx),
					Register.Kept,
					game => Calls(game, pair, "WelcomeEmissary") > emissaries ? "emissary" : regime == 0 ? "anarchy" : (round & 0xf) == 0 ? "16th turn" : "other turn");
			}, output, ["emissary", "anarchy", "16th turn", "other turn"], [0x0d, 0x1b, 0x0d]);
		}

		[Fact]
		public void ImportPalaceGraphicsMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort marker = (ushort)chance.Next(2);
				ushort[] levels = Enumerable.Range(0, 9).Select(_ => chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 6)).ToArray();
				ushort[] styles = Enumerable.Range(0, 9).Select(_ => (ushort)chance.Next(0, 3)).ToArray();
				ushort garden = (ushort)chance.Next(-1, 4), left = (ushort)chance.Next(0, 4), right = (ushort)chance.Next(0, 4);
				ushort wide = (ushort)chance.Next(2), cycling = (ushort)chance.Next(2), palette = (ushort)chance.Next(4, 12);
				// The locals 0x12 and 0x18 of the frame, read even when this call doesn't set them
				ushort[] leftover = { (ushort)chance.Next(0, 8), (ushort)chance.Next(0, 9) };
				ushort sp = pair.Original.Engine.Processor.RegStack.Value16;

				return new Scenario($"ImportPalaceGraphics({marker}), levels {string.Join(" ", levels.Select(level => (short)level))}, " +
					$"styles {string.Join("", styles)}, garden {(short)garden}, bushes {left} {right}, 0xb22a {wide}, frame {leftover[0]} {leftover[1]}",
					game =>
					{
						for (int idx = 0; idx < 9; idx++)
						{
							game.Processor.EmitGlobalWord((ushort)(0xd91e + (idx << 1)), levels[idx]);
							game.Processor.EmitGlobalWord((ushort)(0xb222 + (idx << 1)), styles[idx]);
						}

						game.Processor.EmitGlobalWord(0xd932, garden);
						game.Processor.EmitGlobalWord(0xd930, left);
						game.Processor.EmitGlobalWord(0xd934, right);
						game.Processor.EmitGlobalWord(0xb22a, wide);
						game.Processor.EmitGlobalWord(0xd762, cycling);
						game.Processor.EmitGlobalWord(0xdeba, palette);
						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 0x12), leftover[0]);
						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 0x18), leftover[1]);
					},
					game => new PalaceReference(game).ImportPalaceGraphics(marker),
					game => game.Throne.ImportPalaceGraphics(marker),
					Register.Kept,
					game => $"{(marker != 0 ? "palace screen" : "picture")}{(left != 0 || right != 0 ? ", bushes" : "")}");
			}, output, ["palace screen", "picture", "palace screen, bushes", "picture, bushes"], [0x0d]);
		}

		[Fact]
		public void ReckonContinentSizesMatchesOriginalCode()
		{
			Check(3, 8, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				ushort marker = (ushort)(chance.Next(3) == 0 ? 1 : 0);
				byte[] colours = Enumerable.Range(0, 16).Select(colour => (byte)colour).ToArray();
				byte[] ocean = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) == 0xa).ToArray();
				byte[] land = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) != 0xa).ToArray();
				int density = chance.Next(1, 10);
				byte[] dots = Enumerable.Range(0, 86 * 54).Select(_ => chance.Next(10) < density ? ocean[chance.Next(ocean.Length)] : land[chance.Next(land.Length)]).ToArray();
				// The local 0xe of the frame, compared before it is set when a neighbour has no sea
				ushort leftover = (ushort)chance.Next(0, 4);
				ushort sp = engine.Processor.RegStack.Value16;

				return new Scenario($"ReckonContinentSizes({marker}), sea {density} / 10, frame word 0xe {leftover}",
					game =>
					{
						ushort displayIdx = game.Processor.PullGlobalWord(game.Processor.PullGlobalWord(0xaa));

						for (int idx = 0; idx < dots.Length; idx++)
							game.VideoCard.AssignDot(displayIdx, idx % 86, idx / 86, dots[idx], 0);

						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 0xe), leftover);
					},
					game =>
					{
						new GameInitAndIntroReference(game).ReckonContinentSizes(marker);
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					game =>
					{
						game.WorldSetup.ReckonContinentSizes(marker);
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					Register.Kept,
					game => marker != 0 ? "with the story" : "linked");
			}, output, ["linked", "with the story"]);
		}
	}
}
