using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The citizens of the city view, a new spaceship part, the foreign minister's report and the navigation keys against
	/// their original code
	/// </summary>
	public class BatchThirtyNineDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The segment 0x3772 up to the data segment (its clock words differ with the number of memory accesses)</summary>
		private static readonly (ushort, ushort, int)[] Spaceships = [(0x3772, 0, 0x38f0)];

		/// <summary>The mouse state and the click (left out of the snapshot)</summary>
		private static readonly (ushort, ushort, int)[] Mouse = [(0x3b01, 0x586e, 8)];

		[Fact]
		public void RenderCitySceneryMatchesOriginalCode()
		{
			Check(3, 70, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				sbyte size = (sbyte)chance.Next(1, 30);
				ushort breadth = (ushort)(chance.Next(2) == 0 ? 0x100 : chance.Next(0x40, 0x110));
				ushort happy = (ushort)(chance.Next(4) == 0 ? chance.Next(-3, 0) : chance.Next(0, size + 4));
				ushort unhappy = (ushort)(chance.Next(4) == 0 ? chance.Next(-3, 0) : chance.Next(0, size + 4));
				ushort specialists = (ushort)(chance.Next(3) == 0 ? 0 : chance.Next(0, Math.Min((int)size, 8) + 1));
				ushort laborers = (ushort)chance.Next(0x10000);
				bool temple = chance.Next(2) == 0;
				short person = engine.World.PersonParticipantIdx;
				bool narrow = 12 * size > (short)breadth;

				return new Scenario($"RenderCityScenery({townIdx}, 0x18, 0x8c, {breadth}), size {size}, happy {(short)happy}, " +
					$"unhappy {(short)unhappy}, specialists {specialists}, technology 0x23 {temple}",
					game =>
					{
						game.World.Towns[townIdx].TrueExtent = size;
						game.World.Towns[townIdx].LaborerMarkers2 = laborers;
						game.Processor.EmitGlobalWord(0x653e, (ushort)townIdx);
						game.Processor.EmitGlobalWord(0x70e2, happy);
						game.Processor.EmitGlobalWord(0x70e4, unhappy);
						game.Processor.EmitGlobalWord(0xe8b8, specialists);
						ushort[] science = game.World.Participants[person].FoundScienceMarkers;
						science[2] = (ushort)((science[2] & ~8) | (temple ? 8 : 0));
					},
					game => new CityPanoramaReference(game).DrawCityScenery(townIdx, 0x18, 0x8c, breadth),
					game => game.CityScene.DrawCityScenery(townIdx, 0x18, 0x8c, breadth),
					Register.Acc | Register.Kept,
					game =>
					{
						int frameHappy = Math.Clamp((int)(short)happy, 0, (int)size), frameUnhappy = Math.Clamp((int)(short)unhappy, 0, (int)size);
						bool clamped = frameHappy + frameUnhappy > Math.Clamp(size - specialists, 0, 99);

						return $"{(narrow ? "narrow" : "wide")}{(clamped ? ", clamped" : "")}";
					});
			}, output, ["narrow", "wide", "narrow, clamped", "wide, clamped"]);
		}

		[Fact]
		public void ObtainNavigationKeyMatchesOriginalCode()
		{
			ushort[] mapped =
			{
				0x4800, 0x487e, 0x4900, 0x4d00, 0xf400, 0x5100, 0x5000, 0x5060, 0x4f00, 0x4b00, 0x4b7c, 0x4700, 0x475c, 0x4838,
				0x4939, 0x4d36, 0x4d46, 0x5133, 0x5032, 0x4f31, 0x4b34, 0x4b43, 0x4737
			};

			Check(3, 120, (chance, pair) =>
			{
				ushort key = chance.Next(3) switch
				{
					0 => mapped[chance.Next(mapped.Length)],
					1 => (ushort)(chance.Next(0x100) << 8),
					_ => (ushort)chance.Next(0x10000)
				};

				return new Scenario($"ObtainNavigationKey(), key 0x{key:x4}",
					game =>
					{
						lock (game.VideoCard.VideoLatch)
						{
							game.VideoCard.Tokens.Enqueue(key);
						}
					},
					game => new DialogPainterReference(game).ReadNavigationKey(),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Dialogs.ReadNavigationKey()),
					Register.Acc | Register.Kept,
					game => mapped.Contains(key) ? "navigation" : (key & 0xff) != 0 ? "character" : "other scan code");
			}, output, ["navigation", "character", "other scan code"]);
		}

		[Fact]
		public void DisplayMilitaryReportMatchesOriginalCode()
		{
			ushort[] relations = { 0, 1, 2, 3, 6, 0x41, 0x42, 0x46, 0x43, 0x81 };

			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short live = (short)(chance.Next(0x100) | 1 | (1 << person));
				ushort[,] relation = new ushort[8, 8];

				for (int first = 0; first < 8; first++)
				{
					for (int second = 0; second < 8; second++)
						relation[first, second] = relations[chance.Next(relations.Length)];
				}

				short hardness = (short)chance.Next(0, 5);
				bool mouse = chance.Next(2) == 0;
				ushort horizLoc = (ushort)chance.Next(0x100, 0x140), vertLoc = (ushort)chance.Next(0x10, 0xc8);
				// The list of the rows' players in the stack frame: rows without contact keep what is there
				ushort sp = engine.Processor.RegStack.Value16;
				ushort[] leftover = Enumerable.Range(0, 8).Select(idx => (ushort)chance.Next(8)).ToArray();
				int reports = Calls(pair.Rewritten.Engine, pair, "DisplayIntelligenceReport");

				return new Scenario($"DisplayMilitaryReport(), alive 0x{live:x2}, level {hardness}, mouse {mouse} at ({horizLoc}, {vertLoc})",
					game =>
					{
						game.World.LiveRealms = live;
						game.World.HardnessTier = hardness;

						for (int first = 0; first < 8; first++)
						{
							for (int second = 0; second < 8; second++)
								game.World.Participants[first].Relations[second] = relation[first, second];
						}

						game.Processor.EmitGlobalWord(0x1a3c, (ushort)(mouse ? 1 : 0));
						game.Processor.EmitGlobalWord(0x586e, horizLoc);
						game.Processor.EmitGlobalWord(0x5870, vertLoc);
						game.Processor.EmitGlobalWord(0x5872, 0);
						game.Processor.EmitGlobalWord(0x5874, (ushort)(mouse ? 1 : 0));

						for (int idx = 0; idx < 8; idx++)
							game.Processor.EmitGlobalWord((ushort)(sp - 2 - 0x16 + (idx << 1)), leftover[idx]);
					},
					game => new AdvisorScreensReference(game).ShowMilitaryReport(),
					game => game.Briefings.ShowMilitaryReport(),
					Register.Acc | Register.Kept,
					game => $"{(mouse ? "mouse" : "keys")}, {(Calls(game, pair, "DisplayIntelligenceReport") > reports ? "report" : "no report")}",
					Mouse);
			}, output, ["mouse, report", "mouse, no report", "keys, report", "keys, no report"], [0x33, 0x0d, 0x32, 0x1b, 0x20, 0x35, 0x0d, 0x37]);
		}

		[Fact]
		public void AttachSpaceshipPartMatchesOriginalCode()
		{
			Check(3, 50, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				List<(ushort Offset, byte Kind)> slots = new List<(ushort, byte)>();

				for (ushort slotIdx = 0; engine.Processor.PullUInt8(0x375a, (ushort)(3 * slotIdx)) != 0xff; slotIdx++)
				{
					ushort slot = (ushort)(3 * slotIdx);
					slots.Add(((ushort)((12 * (sbyte)engine.Processor.PullUInt8(0x375a, slot)) + (sbyte)engine.Processor.PullUInt8(0x375a, (ushort)(slot + 1))),
						engine.Processor.PullUInt8(0x375a, (ushort)(slot + 2))));
				}

				short participantIdx = (short)(chance.Next(3) == 0 ? engine.World.PersonParticipantIdx : chance.Next(8));
				ushort kind = (ushort)chance.Next(3);
				int fullness = chance.Next(4);
				bool[] built = slots.Select(slot => chance.Next(3) < fullness).ToArray();
				ushort components = (ushort)chance.Next(0, 4), modules = (ushort)chance.Next(0, 8);
				short markers = (short)(chance.Next(0x100) << 8);
				ushort grid = (ushort)(0x16e0 + (0xb4 * participantIdx));
				byte[] before = new byte[0x200];

				return new Scenario($"AttachSpaceshipPart({participantIdx}, {kind}), {built.Count(part => part)} of {slots.Count} built, " +
					$"components {components}, modules {modules}, spaceships 0x{(ushort)markers:x4}",
					game =>
					{
						for (int slotIdx = 0; slotIdx < slots.Count; slotIdx++)
							game.Processor.EmitUInt8(0x3772, (ushort)(grid + slots[slotIdx].Offset), built[slotIdx] ? slots[slotIdx].Kind : (byte)0xff);

						game.Processor.EmitGlobalWord(0x687c, components);
						game.Processor.EmitGlobalWord(0x687e, modules);
						game.World.StarshipMarkers = markers;

						for (int idx = 0; idx < before.Length; idx++)
							before[idx] = game.Processor.PullUInt8(0x3772, (ushort)(grid - 0x100 + idx));
					},
					game => new SpaceshipBayReference(game).AttachSpaceshipPart(participantIdx, kind),
					game => game.RocketScreen.AttachSpaceshipPart(participantIdx, kind),
					Register.Acc | Register.Kept,
					game =>
					{
						List<int> changed = Enumerable.Range(0, before.Length)
							.Where(idx => game.Processor.PullUInt8(0x3772, (ushort)(grid - 0x100 + idx)) != before[idx]).ToList();

						if (changed.Count == 0)
							return kind == 1 && components == 3 || kind == 2 && modules == 7 ? "complete" : "cancelled";

						bool slot = slots.Any(entry => entry.Offset == changed[0] - 0x100);
						bool first = (markers & (1 << (participantIdx + 8))) == 0;

						return $"{(kind == 0 ? "structure" : kind == 1 ? "component" : "module")}{(slot ? "" : " outside the table")}" +
							$"{(first ? ", first part" : "")}";
					},
					Spaceships);
			}, output, ["structure", "component", "module", "structure, first part", "complete", "cancelled", "module outside the table", "structure outside the table, first part"],
				[0x0d, 0x5000, 0x0d, 0x1b, 0x0d, 0x4800]);
		}
	}
}
