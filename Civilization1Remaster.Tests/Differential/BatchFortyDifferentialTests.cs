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
	/// The speaking king, the screen of a destroyed civilization, the links between the blocks of the new map and the
	/// barbarians against their original code
	/// </summary>
	public class BatchFortyDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ChoreographKingSpeakingMatchesOriginalCode()
		{
			int made = 0;

			Check(3, 150, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				bool load = made++ == 0;
				ushort step = (ushort)chance.Next(0x12);
				ushort speaking = (ushort)(chance.Next(2) == 0 ? 0 : chance.Next(1, 9));
				ushort pause = (ushort)(chance.Next(2) == 0 ? 0 : chance.Next(1, 5));
				ushort mood = (ushort)chance.Next(4);
				ushort colours = (ushort)chance.Next(2);
				ushort skip = (ushort)(chance.Next(3) == 0 ? 0 : 1);
				sbyte next = (sbyte)engine.Processor.PullGlobalByte((ushort)(step + 0x3ab8));

				return new Scenario($"ChoreographKingSpeaking(), step {step} (next {next}), speaking {speaking}, pause {pause}, mood {mood}, " +
					$"0x6b8e {colours}, 0x67f0 {skip}",
					game =>
					{
						if (load)
						{
							game.Libc.CopyText(0xbb80, "govt0a.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);

							for (int idx = 0; idx < 12; idx++)
								game.Processor.EmitGlobalWord((ushort)(0x804e + (idx << 1)),
									game.VideoCard.ScreenToPicture(1, (ushort)((0x50 * (idx & 3)) + 1), 0x65, 0x4e, 0x63));

							game.Libc.CopyText(0xbb80, "king00.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);

							for (int idx = 0; idx < 16; idx++)
								game.Processor.EmitGlobalWord((ushort)(0x7efe + (idx << 1)),
									game.VideoCard.ScreenToPicture(1, (ushort)(0xb5 + ((idx & 3) * 0x10)), 0x43, 0x20, 0x10));
						}

						game.Processor.EmitGlobalWord(0x67f4, step);
						game.Processor.EmitGlobalWord(0xd76c, speaking);
						game.Processor.EmitGlobalWord(0x67f6, pause);
						game.Processor.EmitGlobalWord(0x3936, mood);
						game.Processor.EmitGlobalWord(0x6b8e, colours);
						game.Processor.EmitGlobalWord(0x67f0, skip);
						game.Processor.EmitGlobalWord(0x67f2, (ushort)game.World.PersonParticipantIdx);
					},
					game => new RulerAudienceReference(game).ChoreographKingSpeaking(),
					game => game.Audience.ChoreographKingSpeaking(),
					Register.Acc | Register.Kept,
					game => speaking != 0 && step == 0 ? "speaking" : next > 0 ? "next step" : next < 0 ? "back" : $"gesture ends, mood {Math.Min((int)mood, 3)}");
			}, output, ["speaking", "next step", "back", "gesture ends, mood 0", "gesture ends, mood 1", "gesture ends, mood 2", "gesture ends, mood 3"]);
		}

		[Fact]
		public void DisplayCivilizationDestroyedScreenMatchesOriginalCode()
		{
			Check(3, 10, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(1, 8);
				short leader = (short)chance.Next(14);
				ushort season = (ushort)chance.Next(-4000, 2100);
				ushort cycling = (ushort)chance.Next(2);
				byte fallen = (byte)chance.Next(0, 14);
				byte[] wall = Enumerable.Range(0, 14).Select(_ => chance.Next(3) == 0 ? (byte)0xff : (byte)chance.Next(14)).ToArray();

				return new Scenario($"DisplayCivilizationDestroyedScreen({participantIdx}, {(short)season}), leader {leader}, fallen {fallen}, " +
					$"colour cycles {cycling}",
					game =>
					{
						game.World.Participants[participantIdx].ChiefVisuals = leader;
						game.Processor.EmitGlobalWord(0xd762, cycling);
						game.Processor.EmitGlobalByte(0xb1d4, fallen);

						for (int idx = 0; idx < wall.Length; idx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(0x37c4 + idx), wall[idx]);
					},
					game => new ReplayViewerReference(game).DisplayCivilizationDestroyedScreen(participantIdx, season),
					game => game.Replayer.DisplayCivilizationDestroyedScreen(participantIdx, season),
					Register.Kept,
					game => $"{(cycling != 0 ? "VGA" : "EGA")}, {(fallen >= 7 ? "second row" : "first row")}",
					[(0x3772, 0x37c4, 0xe)]);
			}, output, ["VGA, first row", "VGA, second row", "EGA, first row", "EGA, second row"], [0x0d, 0x20, 0x1b]);
		}

		[Fact]
		public void LabelLandmassesMatchesOriginalCode()
		{
			Check(3, 10, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				byte[] colours = Enumerable.Range(0, 16).Select(colour => (byte)colour).ToArray();
				byte[] ocean = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) == 0xa).ToArray();
				byte[] land = colours.Where(colour => engine.Processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1))) != 0xa).ToArray();
				int density = chance.Next(1, 10);
				byte[] dots = Enumerable.Range(0, 82 * 54).Select(_ => chance.Next(10) < density ? land[chance.Next(land.Length)] : ocean[chance.Next(ocean.Length)]).ToArray();

				return new Scenario($"LabelLandmasses(), land {density} / 10",
					game =>
					{
						ushort displayIdx = game.Processor.PullGlobalWord(game.Processor.PullGlobalWord(0xaa));

						for (int idx = 0; idx < dots.Length; idx++)
							game.VideoCard.AssignDot(displayIdx, idx % 82, idx / 82, dots[idx], 0);
					},
					game => new WorldBuilderReference(game).LabelLandmasses(),
					game => game.WorldSetup.LabelLandmasses(),
					Register.Kept,
					game => Enumerable.Range(0, 0x104).Any(idx => game.Processor.PullGlobalByte((ushort)(0xdb44 + idx)) != 0) ? "linked" : "no links");
			}, output, ["linked"]);
		}

		[Fact]
		public void DisplayTravellersReportMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short hardness = (short)chance.Next(0, 5);
				short round = (short)(chance.Next(4) == 0 ? chance.Next(0, 150) : chance.Next(150, 500) | (chance.Next(2) == 0 ? 7 : 0));
				short[] found = { (short)chance.Next(2), (short)chance.Next(2), (short)chance.Next(2) };
				short gold = (short)(chance.Next(3) == 0 ? chance.Next(0, 200) : chance.Next(500, 5000));
				ushort leftover = (ushort)chance.Next(8);
				ushort sp = engine.Processor.RegStack.Value16;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short[] taken = towns.Where(_ => chance.Next(2) == 0).ToArray();
				int units = Calls(pair.Rewritten.Engine, pair, "ConstructUnit");
				int dialogs = Calls(pair.Rewritten.Engine, pair, "DisplayDialog");
				bool early = (ushort)((5 - hardness) * 0x1e) > round;
				bool ship = !early && ((round + 1) & 7) == 0 && found[0] == 0;

				return new Scenario($"DisplayTravellersReport(), level {hardness}, turn {round}, technologies {string.Join(" ", found)}, gold {gold}, frame word 0x2 {leftover}",
					game =>
					{
						game.World.HardnessTier = hardness;
						game.World.RoundTally = round;
						game.World.ScienceLeadingFoundVia[58] = found[0];
						game.World.ScienceLeadingFoundVia[56] = found[1];
						game.World.ScienceLeadingFoundVia[35] = found[2];
						game.World.Participants[person].Gold = gold;

						foreach (short townIdx in taken)
							game.World.Towns[townIdx].ParticipantIdx = person;

						game.Processor.EmitGlobalWord((ushort)(sp - 2 - 2), leftover);
					},
					game => new WorldEventsReference(game).DisplayTravellersReport(),
					game => game.TurnHooks.DisplayTravellersReport(),
					Register.Kept,
					game =>
					{
						int built = Calls(game, pair, "ConstructUnit") - units;
						bool raid = built > (ship ? 1 : 0) && (!ship || built > 5);

						return early ? "too early" : $"{(ship ? "ship" : "no ship")}, {(Calls(game, pair, "DisplayDialog") > dialogs ? "reported raid" : raid ? "raid" : "no raid")}";
					});
			}, output, ["too early", "ship, no raid", "no ship, no raid", "no ship, raid", "no ship, reported raid"], [0x0d]);
		}
	}
}
