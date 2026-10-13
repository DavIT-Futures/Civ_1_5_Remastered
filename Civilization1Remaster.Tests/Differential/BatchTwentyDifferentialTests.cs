using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The text block, the small map of the city screen, the throne room and the AI's landing tile against their original code
	/// </summary>
	public class BatchTwentyDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderTextChunkMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				string text = string.Concat(Enumerable.Range(0, chance.Next(0, 40)).Select(_ =>
					chance.Next(6) switch
					{
						0 => " ",
						1 => chance.Next(3) == 0 ? "\n" : "^",
						_ => new string('a', chance.Next(1, 12)).Replace('a', "AbcmW.il"[chance.Next(8)]),
					}));
				int peakBreadth = chance.Next(1, 40);
				int horizLoc = chance.Next(0, 200), vertLoc = chance.Next(0, 193);
				ushort style = (ushort)chance.Next(16);

				return new Scenario($"RenderTextChunk({peakBreadth}, {horizLoc}, {vertLoc}, {style}), text \"{text.Replace("\n", "\\n")}\"",
					game => game.Libc.CopyText(0xba06, text),
					game => new TextBlockRendererReference(game).RenderTextChunk(peakBreadth, horizLoc, vertLoc, style),
					game => game.BlockText.RenderTextChunk(peakBreadth, horizLoc, vertLoc, style),
					Register.Acc | Register.Kept,
					game => (short)game.Processor.RegAcc.Value16 > 192 ? "page full" :
						text.Contains('^') ? "with ^" : text.Length > peakBreadth ? "lines" : "one line");
			}, output, ["page full", "with ^", "lines", "one line"]);
		}

		[Fact]
		public void RenderMiniMapMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				short humanIdx = pair.Original.Engine.World.PersonParticipantIdx;
				short horizBegin = (short)(chance.Next(3) == 0 ? 40 : chance.Next(80));

				return new Scenario($"RenderMiniMap(), view from column {horizBegin}",
					game => game.World.Participants[humanIdx].HorizBegin = horizBegin,
					game => new CityManagementReference(game).RenderMiniMap(),
					game => game.CityOps.RenderMiniMap(),
					Register.Kept,
					game => horizBegin < 40 ? "wraps left" : horizBegin > 40 ? "wraps right" : "no wrap");
			}, output, ["wraps left", "wraps right", "no wrap"]);
		}

		[Fact]
		public void ImportKingSceneryMatchesOriginalCode()
		{
			int made = 0;

			Check(3, 40, (chance, pair) =>
			{
				bool load = made++ == 0;
				short participantIdx = (short)chance.Next(8);
				short regime = (short)chance.Next(6);
				short standing = (short)chance.Next(9);
				ushort technology = (ushort)chance.Next(2);
				ushort king = (ushort)chance.Next(14);
				ushort skip = (ushort)(chance.Next(6) == 0 ? 1 : 0);

				return new Scenario($"ImportKingScenery({participantIdx}), government {regime}, standing {standing}, " +
					$"0x67ee {technology}, king {king}, 0x67f0 {skip}",
					game =>
					{
						// The pictures the audience loads before (MeetWithKing, L1e3e)
						if (load)
						{
							game.Libc.CopyText(0xbb80, "govt0a.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);

							for (int idx = 0; idx < 4; idx++)
								game.Processor.EmitGlobalWord((ushort)(0x804e + (idx * 6)),
									game.VideoCard.ScreenToPicture(1, (ushort)((0x50 * idx) + 1), 0x65, 0x4e, 0x63));

							game.Libc.CopyText(0xbb80, "king00.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);
							game.Processor.EmitGlobalWord(0xb1e6, game.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85));
						}

						game.World.Participants[participantIdx].RegimeKind = regime;
						game.World.Participants[participantIdx].Standing = standing;
						game.Processor.EmitGlobalWord(0x67ee, technology);
						game.Processor.EmitGlobalWord(0x67f8, king);
						game.Processor.EmitGlobalWord(0x67f0, skip);
					},
					game => new MeetWithKingReference(game).ImportKingScenery(participantIdx),
					game => game.Audience.ImportKingScenery(participantIdx),
					Register.Kept,
					game => skip != 0 ? "skipped" : regime == 3 ? "government 3" : technology != 0 ? "m" : "a");
			}, output, ["skipped", "government 3", "m", "a"]);
		}

		[Fact]
		public void MayAIEnterTileMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short humanIdx = engine.World.PersonParticipantIdx;
				short participantIdx = (short)chance.Next(8);
				(int horizLoc, int vertLoc) = RandomTile(engine, chance);
				short[] humanTowns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx == humanIdx).ToArray();

				// Often near a city of the human player (the human land on map layer 2)
				if (humanTowns.Length > 0 && chance.Next(2) == 0)
				{
					short townIdx = humanTowns[chance.Next(humanTowns.Length)];
					horizLoc = Math.Clamp(engine.World.Towns[townIdx].Place.X + chance.Next(-3, 4), 0, MapRules.Width - 1);
					vertLoc = Math.Clamp(engine.World.Towns[townIdx].Place.Y + chance.Next(-3, 4), 2, MapRules.Height - 3);
				}

				bool contact = chance.Next(2) == 0;

				return new Scenario($"MayAIEnterTile({participantIdx}, {horizLoc}, {vertLoc}), relation bit 2 {contact}",
					game =>
					{
						ref ushort relation = ref game.World.Participants[participantIdx].Relations[humanIdx];
						relation = (ushort)(contact ? relation | 2 : relation & ~2);
					},
					game => new AIPlayerReference(game).MayAIEnterTile(participantIdx, (ushort)horizLoc, (ushort)vertLoc),
					game => game.Bot.MayAIEnterTile(participantIdx, (ushort)horizLoc, (ushort)vertLoc),
					Register.Acc | Register.Kept,
					game => (short)game.Processor.RegAcc.Value16 switch { -1 => "no ocean", 0 => "the tile itself", _ => "around" });
			}, output, ["no ocean", "the tile itself", "around"]);
		}
	}
}
